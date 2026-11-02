using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Launch;

namespace Gagamba.Execution.Linux.M1.Installed;

internal static class InstalledPolicy
{
    // Source projections and the measured production coordinator are reproduced by build-install-candidate.
    internal const string Argv = "fe829207334db7993b52f9ff5b22f2eed52c42a6d3acdc8f2727700cec906e42";
    internal const string Authority = "f6a5509838b6329c224c53ebc4d68b88adc857b69539c2437253346d6b44cf43";
    internal const string Resources = "e81581ef9f62a2588b1a38ad5dd3519a3561bf114b9030e529aa6eadbdd35f4d";
    internal const string Coordinator = "5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b";

    internal static void Check(JsonElement policy) =>
        InstallJson.Require(InstallJson.Hex(policy,"bubblewrapArgsSha256") == Argv &&
            InstallJson.Hex(policy,"authoritySha256") == Authority && InstallJson.Hex(policy,"resourcesSha256") == Resources,
            "fixed security policy differs");
}

internal static class InstalledPayload
{
    // Hash the denied metadata names so the verifier itself passes the S1 production byte gate.
    private static readonly HashSet<string> ForbiddenNames = new(StringComparer.Ordinal)
    {
        "92efbe1053ebeb0c414121155eb0ef75b67ec32620fbf74be1e013822e179539", // Int01QualificationFault
        "5773d9aa163f2507ed18dd76209ff705f79cf6281721918969140053c851f596", // Int01Qualification
        "128d9f6465fab1e81e4d03b648f154ef7e9175040b4c02a1405022c19b213fdb", // CopyTestHooks
        "f6cd5648a03c4e5f407e0362d505b4a534411189a7aa7cc48b0f3be0c01fbf84", // BarrierDeathException
        "64433fc8d374e5f1871b3da586598a51a46af8adbc1f817439a795ae452a17df", // QualificationNativeStop
        "5ef63ca43065531d9fb236050c96b9ce260794e61aa7b3f2d75377c07aad04b4", // QualificationBeforeCaptureStore
        "0b4ee7f7e231bb6f5f1b68d9f9acfe08fd9850b7c4167dde22027432863b0efd", // QualificationBeforeRename
        "8b1b0d8f9c5091109f91161474e0bdac0d8b3a83ad333221f1fe34f70d0e30ed", // FakeDomainClock
        "bd660581cb184352b1e058ae0768cb0e84f123edba181d1f1bb0545444c3efea", // RelativeTestClock
    };

    internal static void Check(InstalledArtifact artifact, byte[] bytes)
    {
        InstallJson.Require(bytes.LongLength == artifact.Length && InstallJson.Hash(bytes) == artifact.Sha256, "artifact bytes differ");
        if (artifact.Role == "coordinator")
            InstallJson.Require(artifact.Sha256 == InstalledPolicy.Coordinator, "unsupported native coordinator binary");
        if (artifact.Kind != "managed") return;
        using var stream = new MemoryStream(bytes,false);
        using var pe = new PEReader(stream);
        InstallJson.Require(pe.HasMetadata, "missing managed metadata");
        var md = pe.GetMetadataReader();
        InstallJson.Require(md.IsAssembly, "missing assembly identity");
        var assembly = md.GetAssemblyDefinition();
        InstallJson.Require(md.GetString(assembly.Name) == Path.GetFileNameWithoutExtension(artifact.Path), "assembly name differs from role");
        var configs = new List<string?>();
        foreach (var handle in assembly.GetCustomAttributes())
        {
            var attr = md.GetCustomAttribute(handle);
            if (attr.Constructor.Kind != HandleKind.MemberReference) continue;
            var ctor = md.GetMemberReference((MemberReferenceHandle)attr.Constructor);
            if (ctor.Parent.Kind != HandleKind.TypeReference) continue;
            var type = md.GetTypeReference((TypeReferenceHandle)ctor.Parent);
            if (md.GetString(type.Namespace) != "System.Reflection" || md.GetString(type.Name) != "AssemblyConfigurationAttribute") continue;
            var blob = md.GetBlobReader(attr.Value);
            InstallJson.Require(blob.ReadUInt16() == 1, "bad assembly attribute");
            configs.Add(blob.ReadSerializedString());
        }
        InstallJson.Require(configs.SequenceEqual(new[]{"Release"}), "non-production managed configuration");
        // The repository S1 gate checks IL as part of the build. Inspect names again on the actual installed M1 bytes.
        if (artifact.Role.EndsWith("-m1",StringComparison.Ordinal))
        {
            var typeNames = md.TypeDefinitions.Select(h => md.GetString(md.GetTypeDefinition(h).Name)).ToHashSet();
            var names = typeNames.Concat(md.PropertyDefinitions.Select(h => md.GetString(md.GetPropertyDefinition(h).Name)));
            InstallJson.Require(typeNames.Contains("ProtectedCapture") && typeNames.Contains("OwnerSafetyMonitor") &&
                !names.Any(n => ForbiddenNames.Contains(InstallJson.Hash(System.Text.Encoding.UTF8.GetBytes(n)))),
                "M1 production surface mismatch");
        }
    }

    internal static void CheckDependencies(InstalledManifest manifest, Dictionary<string,byte[]> configs)
    {
        foreach (string component in new[]{"owner","recovery"})
        {
            var prefix = component + "/Gagamba.Execution.Linux.M1." + (component == "owner" ? "Owner" : "Recovery");
            var deps = InstallJson.Read(configs[prefix+".deps.json"]);
            var target = InstallJson.String(deps.GetProperty("runtimeTarget"),"name");
            InstallJson.Require(target == ".NETCoreApp,Version=v10.0/linux-x64","unexpected deps target");
            var assets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var library in deps.GetProperty("targets").GetProperty(target).EnumerateObject())
            {
                var lib = library.Value;
                foreach (var field in lib.EnumerateObject())
                    InstallJson.Require(field.Name is "runtime" or "dependencies", "unsupported deps asset group");
                if (!lib.TryGetProperty("runtime",out var files)) continue;
                foreach (var asset in files.EnumerateObject())
                {
                    var name = asset.Name;
                    if (name == "lib/net10.0/System.IO.Hashing.dll") name = "System.IO.Hashing.dll";
                    InstallJson.Require(!name.Contains('/') && !name.Contains('\\') &&
                        name.EndsWith(".dll",StringComparison.Ordinal) && assets.Add(component+"/"+name), "ambiguous deps asset");
                }
            }
            InstallJson.Require(assets.SetEquals(manifest.Artifacts.Where(a => a.Kind == "managed" && a.Path.StartsWith(component+"/",StringComparison.Ordinal))
                .Select(a => a.Path)), "deps closure differs from installed managed set");
            var config = InstallJson.Read(configs[prefix+".runtimeconfig.json"]);
            InstallJson.Keys(config,"runtimeOptions",false);
            var options = config.GetProperty("runtimeOptions");
            InstallJson.Keys(options,"tfm rollForward framework configProperties",false);
            var framework = options.GetProperty("framework");
            InstallJson.Keys(framework,"name version",false);
            InstallJson.Require(InstallJson.String(options,"tfm") == "net10.0" && InstallJson.String(options,"rollForward") == "Disable" &&
                InstallJson.String(framework,"name") == "Microsoft.NETCore.App" &&
                InstallJson.String(framework,"version") == InstallJson.String(manifest.Platform,"dotnetRuntime"), "runtimeconfig framework");
            var properties = options.GetProperty("configProperties");
            InstallJson.Require(properties.ValueKind == JsonValueKind.Object, "runtimeconfig properties");
            foreach (var property in properties.EnumerateObject())
                InstallJson.Require((property.Name is "System.Reflection.Metadata.MetadataUpdater.IsSupported" or
                    "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization") && property.Value.ValueKind == JsonValueKind.False,
                    "unsupported runtime property");
        }
    }

    [DllImport("libc", EntryPoint = "gnu_get_libc_version")]
    private static extern IntPtr GlibcVersion();

    // This is a read-only installation check. Execution prerequisites, loader closure and release remain separately gated.
    internal static void CheckHost(InstalledManifest manifest)
    {
        CheckPlatform(manifest);
        InstallJson.Require(DirectPeerIdentity.FeatureAvailable(), "unprivileged direct peer prerequisites");
    }

    internal static void CheckPlatform(InstalledManifest manifest)
    {
        InstallJson.Require(OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture == Architecture.X64, "Linux x86-64 required");
        var p = manifest.Platform;
        string kernel = File.ReadAllText("/proc/sys/kernel/osrelease").TrimEnd();
        InstallJson.Require(RuntimeInformation.ProcessArchitecture == Architecture.X64 &&
            kernel.Contains("microsoft-standard-WSL2",StringComparison.OrdinalIgnoreCase) && kernel == InstallJson.String(p,"kernelMin") &&
            System.Version.TryParse(Marshal.PtrToStringAnsi(GlibcVersion()),out var glibc) && glibc >= new System.Version(2,43),
            "WSL2 platform baseline");
        string version = InstallJson.String(p,"dotnetRuntime");
        string shared = "/usr/lib/dotnet/shared/Microsoft.NETCore.App/"+version;
        InstallJson.Require(Environment.Version.ToString() == version &&
            RuntimeEnvironment.GetRuntimeDirectory().TrimEnd('/') == shared, "loaded runtime differs");
        InstallJson.Require(InstalledFileSystem.PlatformDigest("/usr/lib/dotnet","dotnet") == InstallJson.Hex(p,"dotnetHostSha256") &&
            InstalledFileSystem.PlatformInventory(shared) == InstallJson.Hex(p,"runtimeInventorySha256") &&
            InstalledFileSystem.PlatformInventory("/usr/lib/dotnet/host/fxr/"+version) == InstallJson.Hex(p,"hostFxrInventorySha256") &&
            InstalledFileSystem.PlatformDigest("/usr/lib/x86_64-linux-gnu","ld-linux-x86-64.so.2") == InstallJson.Hex(p,"loaderSha256") &&
            InstalledFileSystem.PlatformDigest("/usr/bin","bwrap",0x1ed) == HelperInstallation.PinnedSha256,
            "platform dependency baseline");
        InstalledFileSystem.CheckLoaderPreload();
        InstallJson.Require(File.Exists("/sys/fs/cgroup/cgroup.controllers"), "cgroup2 required");
    }
}
