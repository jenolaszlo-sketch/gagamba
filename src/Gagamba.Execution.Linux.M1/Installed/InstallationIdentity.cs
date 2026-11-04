using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Gagamba.Execution.Linux.M1.Installed;

// A wire value, never an authority. Only argument-free VerifyActive supplies trusted provenance.
internal sealed record InstallationIdentity(string ArtifactSetSha256, string OwnerClosureSha256,
    string RecoveryClosureSha256, string CoordinatorSha256, string SeccompFilterSha256,
    string RuntimeInventorySha256, ushort ManifestSchema, ushort OwnerProtocol,
    ushort CoordinatorProtocol, ushort Bootstrap, string ProfileId, string ProfileVersion,
    string Generation, bool Qualified)
{
    internal void Validate()
    {
        foreach (var hash in new[] { ArtifactSetSha256, OwnerClosureSha256, RecoveryClosureSha256,
            CoordinatorSha256, SeccompFilterSha256, RuntimeInventorySha256, Generation })
            InstallJson.Require(hash is not null && InstallJson.IsHex(hash,64), "installation digest");
        InstallJson.Require(Generation == ArtifactSetSha256 && Qualified && ManifestSchema == 1 &&
            OwnerProtocol == 1 && CoordinatorProtocol == 1 && Bootstrap == 1 &&
            ProfileId == InstalledManifest.Profile && ProfileVersion == InstalledManifest.Version &&
            ProfileId.Length <= 64 && ProfileVersion.Length <= 64, "installation constants");
    }

    internal static InstallationIdentity Project(InstalledManifest manifest)
    {
        var result = new InstallationIdentity(manifest.Digest, RoleHash(manifest,1), RoleHash(manifest,0),
            manifest.Artifacts.Single(a => a.Role == "coordinator").Sha256,
            manifest.Artifacts.Single(a => a.Role == "seccomp-filter").Sha256,
            InstallJson.Hex(manifest.Platform,"runtimeInventorySha256"),1,1,1,1,
            InstalledManifest.Profile,InstalledManifest.Version,manifest.Digest,true);
        result.Validate(); return result;
    }

    internal static string RoleHash(InstalledManifest manifest, byte role)
    {
        InstallJson.Require(role <= 1,"installation role");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.ASCII.GetBytes("gagamba-b6-role-v1\0"));
        hash.AppendData([role]);
        string prefix = role == 0 ? "recovery-" : "owner-";
        foreach (var suffix in new[] {"contract","deps","entry","m1","runtimeconfig"})
            hash.AppendData(Convert.FromHexString(manifest.Artifacts.Single(a => a.Role == prefix+suffix).Sha256));
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    internal static void ValidateJson(JsonElement value)
    {
        InstallJson.Keys(value,"ArtifactSetSha256 OwnerClosureSha256 RecoveryClosureSha256 CoordinatorSha256 SeccompFilterSha256 RuntimeInventorySha256 ManifestSchema OwnerProtocol CoordinatorProtocol Bootstrap ProfileId ProfileVersion Generation Qualified",false);
        foreach (string name in new[] {"ManifestSchema","OwnerProtocol","CoordinatorProtocol","Bootstrap"})
            InstallJson.Require(InstallJson.Int(value,name)==1,"installation number");
        (JsonSerializer.Deserialize<InstallationIdentity>(value) ?? throw new InvalidDataException("identity missing")).Validate();
    }
}
