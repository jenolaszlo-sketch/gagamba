using System.Text;
using System.Text.Json;

namespace Gagamba.Execution.Linux.M1.Validation;

/// <summary>Deterministic structural and identity-binding verification for the
/// accepted M/F/S/K frozen contracts (P0 record sections 7-10). Results are
/// content-consistency values only. Successful verification does not establish
/// review, owner acceptance, approval, eligibility, invocation, qualification
/// association, or execution authority, and no result can be promoted into one.</summary>
internal static class FrozenContractVerifier
{
    private const string ActionHashDomain = "gagamba-validation-action-v1";

    private static readonly string[] Phases = ["pre", "post"];
    private static readonly string[] Topologies = ["b2-lifeline", "b3-integrated"];
    private static readonly string[] PolicyKinds = ["b2-lifeline-v1", "b3-static-fixture-v1"];

    private static readonly string[] ManifestFields = ["caseId", "objects", "schemaVersion", "topology"];
    private static readonly string[] ManifestObjectFields =
        ["destination", "length", "mode", "path", "role", "sha256"];
    private static readonly string[] B3ObjectRoles = ["coordinator", "filter", "input", "tool"];
    private static readonly string[] B3ObjectModes = ["0555", "0444", "0444", "0555"];
    private static readonly string[] B3ObjectDestinations = ["/coordinator", "fd:6", "/input", "/tool"];

    private static readonly string[] PolicyFields =
    [
        "allowRelease", "bootId", "caseId", "copy", "fixtureManifestSha256", "gid", "hostId",
        "hostPaths", "inputEnrollmentId", "invocation", "maximumSessionNs", "phase", "policyKind",
        "resources", "sandbox", "schemaVersion", "toolEnrollmentId", "topology", "uid",
    ];
    private static readonly string[] PolicyNullFields =
        ["copy", "hostPaths", "inputEnrollmentId", "invocation", "resources", "sandbox", "toolEnrollmentId"];
    private static readonly string[] HostPathFields = ["delegatedParent", "journalParent", "stagingParent"];
    private static readonly string[] HostPathValueFields = ["device", "gid", "inode", "mode", "path", "uid"];
    private static readonly string[] CopyFields = ["aggregateBytes", "hooks", "maximumObjectBytes", "maximumObjects"];
    private static readonly string[] InvocationFields =
        ["argv", "environment", "inputPath", "mode", "stdinEof", "toolPath", "workDirectory"];
    private static readonly string[] EnvironmentFields = ["name", "value"];
    private static readonly string[] SandboxFields =
    [
        "capabilities", "namespaces", "networkQualification", "noAmbientEnvironment",
        "noExtraInheritedFds", "noIpcEndpoints", "noNewPrivs", "readOnlyDestinations",
        "scratchNoexec", "toolExecutable", "writableStores",
    ];
    private static readonly string[] WritableStoreFields = ["bytes", "hasInodeBudget", "inodeBudget", "path"];
    private static readonly string[] ResourceFields =
    [
        "captureAggregateBytes", "configBytes", "coordinatorStatusBytes", "coreDisabled",
        "corePatternObserved", "expiresLeadNs", "fileDescriptors", "finalDrainNs",
        "helperStatusBytes", "inodeQualification", "journalBytes", "memoryBytes",
        "overallTimeoutNs", "receiptBytes", "runtimeDumpsDisabled", "setupTimeoutNs",
        "shmBytes", "stagingBytes", "stagingObjects", "stderrBytes", "stdoutBytes",
        "stopAllowanceNs", "swapBytes", "tasks", "tmpBytes",
    ];

    private static readonly string[] ScriptFields =
        ["actions", "caseId", "fixtureManifestSha256", "fixturePolicySha256", "phase", "schemaVersion", "topology"];
    private static readonly string[] ActionFields = ["index", "op", "timeoutNs"];
    private static readonly string[] ActionLiterals =
        ["connect-b2", "prepare-b3", "inspect", "release", "stop", "cleanup"];
    private static readonly string[] B2Template = ["connect-b2", "inspect", "stop", "cleanup"];
    private static readonly string[] B3Template = ["prepare-b3", "inspect", "stop", "cleanup"];
    private static readonly string[] B3ReleaseTemplate =
        ["prepare-b3", "inspect", "release", "inspect", "stop", "cleanup"];

    private static readonly string[] CatalogFields =
    [
        "actionScriptSha256", "allowRelease", "artifactSetSha256", "caseId", "controllerClosureSha256",
        "driverClosureSha256", "fixtureManifestSha256", "fixturePolicySha256", "hostId",
        "maximumSessionNs", "phase", "platformInventorySha256", "profileId", "profileVersion",
        "qualificationId", "qualificationRecordSha256", "rid", "schemaVersion", "scope",
        "sourceCommit", "sourceTree", "topology",
    ];

    internal static bool TryVerifyManifest(byte[] bytes, out FixtureManifestContract? manifest, out string failure)
    {
        try
        {
            manifest = VerifyManifest(bytes);
            failure = string.Empty;
            return true;
        }
        catch (InvalidDataException exception)
        {
            manifest = null;
            failure = exception.Message;
            return false;
        }
    }

    internal static bool TryVerifyPolicy(byte[] bytes, out FixturePolicyContract? policy, out string failure)
    {
        try
        {
            policy = VerifyPolicy(bytes);
            failure = string.Empty;
            return true;
        }
        catch (InvalidDataException exception)
        {
            policy = null;
            failure = exception.Message;
            return false;
        }
    }

    internal static bool TryVerifyActionScript(byte[] bytes, out ActionScriptContract? script, out string failure)
    {
        try
        {
            script = VerifyActionScript(bytes);
            failure = string.Empty;
            return true;
        }
        catch (InvalidDataException exception)
        {
            script = null;
            failure = exception.Message;
            return false;
        }
    }

    internal static bool TryVerifyCatalog(byte[] bytes, out CatalogContract? catalog, out string failure)
    {
        try
        {
            catalog = VerifyCatalog(bytes);
            failure = string.Empty;
            return true;
        }
        catch (InvalidDataException exception)
        {
            catalog = null;
            failure = exception.Message;
            return false;
        }
    }

    internal static bool TryVerifyChain(
        byte[] manifestBytes,
        byte[] policyBytes,
        byte[] scriptBytes,
        byte[] catalogBytes,
        out FrozenContractChain? chain,
        out string failure)
    {
        try
        {
            var manifest = VerifyManifest(manifestBytes);
            var policy = VerifyPolicy(policyBytes);
            var script = VerifyActionScript(scriptBytes);
            var catalog = VerifyCatalog(catalogBytes);
            var hm = CanonicalJson.Hash(manifestBytes);
            var hf = CanonicalJson.Hash(policyBytes);
            var hs = CanonicalJson.Hash(scriptBytes);
            var hk = CanonicalJson.Hash(catalogBytes);

            Require(policy.FixtureManifestSha256 == hm, "F fixtureManifestSha256 does not bind M");
            Require(script.FixtureManifestSha256 == hm, "S fixtureManifestSha256 does not bind M");
            Require(script.FixturePolicySha256 == hf, "S fixturePolicySha256 does not bind F");
            Require(catalog.FixtureManifestSha256 == hm, "K fixtureManifestSha256 does not bind M");
            Require(catalog.FixturePolicySha256 == hf, "K fixturePolicySha256 does not bind F");
            Require(catalog.ActionScriptSha256 == hs, "K actionScriptSha256 does not bind S");

            Require(catalog.CaseId == manifest.CaseId && catalog.CaseId == policy.CaseId &&
                    catalog.CaseId == script.CaseId, "case id disagreement");
            Require(catalog.Phase == policy.Phase && catalog.Phase == script.Phase, "phase disagreement");
            Require(catalog.Topology == manifest.Topology && catalog.Topology == policy.Topology &&
                    catalog.Topology == script.Topology, "topology disagreement");
            Require(catalog.HostId == policy.HostId, "host id disagreement");
            Require(catalog.AllowRelease == policy.AllowRelease, "allow release disagreement");
            Require(catalog.AllowRelease == script.HasRelease, "release template disagreement");
            Require(catalog.MaximumSessionNs == policy.MaximumSessionNs, "session limit disagreement");
            foreach (var action in script.Actions)
                Require(action.TimeoutNs <= policy.MaximumSessionNs,
                    "S action timeout exceeds the F maximum session");

            chain = new FrozenContractChain(manifest, policy, script, catalog, hm, hf, hs, hk);
            failure = string.Empty;
            return true;
        }
        catch (InvalidDataException exception)
        {
            chain = null;
            failure = exception.Message;
            return false;
        }
    }

    private static FixtureManifestContract VerifyManifest(byte[] bytes)
    {
        var root = CanonicalJson.Parse(bytes);
        RequireFields(root, "M", ManifestFields);
        Long(root, "schemaVersion", 1, 1);
        var caseId = ValidId(root, "caseId", "M caseId");
        var topology = Choice(root, "topology", Topologies, "M topology");
        var objects = Array(root, "objects");
        var contracts = new List<FixtureManifestObjectContract>();
        if (topology == "b2-lifeline")
        {
            Require(objects.GetArrayLength() == 0, "M B2 objects");
        }
        else
        {
            Require(objects.GetArrayLength() == 4, "M B3 object count");
            long aggregate = 0;
            for (var index = 0; index < 4; index++)
            {
                var item = objects[index];
                Require(item.ValueKind == JsonValueKind.Object, "M object kind");
                RequireFields(item, "M object", ManifestObjectFields);
                var role = Exact(item, "role", B3ObjectRoles[index], "M object role order");
                var path = Exact(item, "path", role, "M object path");
                var length = Long(item, "length", 1, 16_777_216);
                aggregate = checked(aggregate + length);
                Require(aggregate <= 16_777_216, "M aggregate length");
                var sha256 = Hex(item, "sha256");
                var mode = Exact(item, "mode", B3ObjectModes[index], "M object mode");
                var destination = Exact(item, "destination", B3ObjectDestinations[index], "M object destination");
                contracts.Add(new FixtureManifestObjectContract(role, path, length, sha256, mode, destination));
            }
        }

        return new FixtureManifestContract(1, caseId, topology, contracts);
    }

    private static FixturePolicyContract VerifyPolicy(byte[] bytes)
    {
        var root = CanonicalJson.Parse(bytes);
        RequireFields(root, "F", PolicyFields);
        Long(root, "schemaVersion", 1, 1);
        var policyKind = Choice(root, "policyKind", PolicyKinds, "F policy kind");
        var caseId = ValidId(root, "caseId", "F caseId");
        var phase = Choice(root, "phase", Phases, "F phase");
        var topology = Choice(root, "topology", Topologies, "F topology");
        var hostId = Hex(root, "hostId");
        var bootIdValue = root.GetProperty("bootId");
        Require(bootIdValue.ValueKind == JsonValueKind.String &&
                FrozenSchemaRules.IsCanonicalUuid(bootIdValue.GetString()!), "F bootId");
        var bootId = bootIdValue.GetString()!;
        var manifestHash = Hex(root, "fixtureManifestSha256");
        var allowRelease = Boolean(root, "allowRelease");
        var maximumSessionNs = Long(root, "maximumSessionNs", 1, 600_000_000_000);
        var uid = Long(root, "uid", 1, int.MaxValue);
        var gid = Long(root, "gid", 1, int.MaxValue);

        if (policyKind == "b2-lifeline-v1")
        {
            Require(topology == "b2-lifeline", "F B2 topology");
            Require(!allowRelease, "F B2 release");
            foreach (var key in PolicyNullFields)
                Require(root.GetProperty(key).ValueKind == JsonValueKind.Null, $"F B2 {key}");
        }
        else
        {
            Require(topology == "b3-integrated", "F B3 topology");
            VerifyHostPaths(Object(root, "hostPaths"), uid, gid);
            ValidId(root, "toolEnrollmentId", "F toolEnrollmentId");
            ValidId(root, "inputEnrollmentId", "F inputEnrollmentId");
            VerifyCopy(Object(root, "copy"));
            VerifyInvocation(Object(root, "invocation"));
            VerifySandbox(Object(root, "sandbox"));
            VerifyResources(Object(root, "resources"), maximumSessionNs);
        }

        return new FixturePolicyContract(1, policyKind, caseId, phase, topology, hostId, bootId,
            manifestHash, allowRelease, maximumSessionNs, uid, gid);
    }

    private static ActionScriptContract VerifyActionScript(byte[] bytes)
    {
        var root = CanonicalJson.Parse(bytes);
        RequireFields(root, "S", ScriptFields);
        Long(root, "schemaVersion", 1, 1);
        var caseId = ValidId(root, "caseId", "S caseId");
        var phase = Choice(root, "phase", Phases, "S phase");
        var topology = Choice(root, "topology", Topologies, "S topology");
        var manifestHash = Hex(root, "fixtureManifestSha256");
        var policyHash = Hex(root, "fixturePolicySha256");
        var actions = Array(root, "actions");
        Require(actions.GetArrayLength() is >= 1 and <= 7, "S action count");
        var contracts = new List<ActionContract>();
        var hashes = new List<string>();
        var ops = new List<string>();
        for (var index = 0; index < actions.GetArrayLength(); index++)
        {
            var item = actions[index];
            Require(item.ValueKind == JsonValueKind.Object, "S action kind");
            RequireFields(item, "S action", ActionFields);
            Require(Long(item, "index", index, index) == index, "S action index");
            var op = Choice(item, "op", ActionLiterals, "S action op");
            var timeoutNs = Long(item, "timeoutNs", 1, 600_000_000_000);
            ops.Add(op);
            contracts.Add(new ActionContract(index, op, timeoutNs));
            hashes.Add(ActionHash(item));
        }

        var selected = topology == "b2-lifeline"
            ? B2Template
            : ops.Contains("release") ? B3ReleaseTemplate : B3Template;
        Require(ops.SequenceEqual(selected), "S action template");
        return new ActionScriptContract(1, caseId, phase, topology, manifestHash, policyHash, contracts, hashes);
    }

    private static CatalogContract VerifyCatalog(byte[] bytes)
    {
        var root = CanonicalJson.Parse(bytes);
        RequireFields(root, "K", CatalogFields);
        Long(root, "schemaVersion", 1, 1);
        var caseId = ValidId(root, "caseId", "K caseId");
        var phase = Choice(root, "phase", Phases, "K phase");
        var topology = Choice(root, "topology", Topologies, "K topology");
        var artifactSetSha256 = Hex(root, "artifactSetSha256");
        var sourceCommit = Hex(root, "sourceCommit", 40);
        var sourceTree = Hex(root, "sourceTree", 40);
        var profileId = Text(root, "profileId");
        var profileVersion = Text(root, "profileVersion");
        var rid = Text(root, "rid");
        var scope = Text(root, "scope");
        var hostId = Hex(root, "hostId");
        var platformInventorySha256 = Hex(root, "platformInventorySha256");
        string? qualificationId;
        string? qualificationRecordSha256;
        if (phase == "pre")
        {
            Require(root.GetProperty("qualificationId").ValueKind == JsonValueKind.Null,
                "K pre qualificationId");
            Require(root.GetProperty("qualificationRecordSha256").ValueKind == JsonValueKind.Null,
                "K pre qualificationRecordSha256");
            qualificationId = null;
            qualificationRecordSha256 = null;
        }
        else
        {
            qualificationId = ValidId(root, "qualificationId", "K post qualificationId");
            qualificationRecordSha256 = Hex(root, "qualificationRecordSha256");
        }

        var controllerClosureSha256 = Hex(root, "controllerClosureSha256");
        var driverClosureSha256 = Hex(root, "driverClosureSha256");
        var manifestHash = Hex(root, "fixtureManifestSha256");
        var policyHash = Hex(root, "fixturePolicySha256");
        var scriptHash = Hex(root, "actionScriptSha256");
        var maximumSessionNs = Long(root, "maximumSessionNs", 1, 600_000_000_000);
        var allowRelease = Boolean(root, "allowRelease");

        return new CatalogContract(1, caseId, phase, topology, artifactSetSha256, sourceCommit, sourceTree,
            profileId, profileVersion, rid, scope, hostId, platformInventorySha256, qualificationId,
            qualificationRecordSha256, controllerClosureSha256, driverClosureSha256, manifestHash,
            policyHash, scriptHash, maximumSessionNs, allowRelease);
    }

    private static string ActionHash(JsonElement action)
    {
        var domain = Encoding.ASCII.GetBytes(ActionHashDomain);
        var body = CanonicalJson.Encode(action);
        var input = new byte[domain.Length + 1 + body.Length];
        domain.CopyTo(input, 0);
        input[domain.Length] = 0;
        body.CopyTo(input, domain.Length + 1);
        return CanonicalJson.Hash(input);
    }

    private static void VerifyHostPaths(JsonElement hostPaths, long uid, long gid)
    {
        RequireFields(hostPaths, "F hostPaths", HostPathFields);
        VerifyHostPath(hostPaths, "delegatedParent", "0755", uid, gid);
        VerifyHostPath(hostPaths, "journalParent", "0700", uid, gid);
        VerifyHostPath(hostPaths, "stagingParent", "0700", uid, gid);
    }

    private static void VerifyHostPath(JsonElement hostPaths, string key, string expectedMode, long uid, long gid)
    {
        var value = Object(hostPaths, key);
        RequireFields(value, $"F hostPaths {key}", HostPathValueFields);
        Long(value, "device", 1, long.MaxValue);
        Require(Long(value, "gid", 1, int.MaxValue) == gid, $"F hostPaths {key} gid");
        Long(value, "inode", 1, long.MaxValue);
        Exact(value, "mode", expectedMode, $"F hostPaths {key} mode");
        var path = value.GetProperty("path");
        Require(path.ValueKind == JsonValueKind.String &&
                FrozenSchemaRules.IsAbsolutePosixPath(path.GetString()!), $"F hostPaths {key} path");
        Require(Long(value, "uid", 1, int.MaxValue) == uid, $"F hostPaths {key} uid");
    }

    private static void VerifyCopy(JsonElement copy)
    {
        RequireFields(copy, "F copy", CopyFields);
        Long(copy, "aggregateBytes", 16_777_216, 16_777_216);
        Require(!Boolean(copy, "hooks"), "F copy hooks");
        Long(copy, "maximumObjectBytes", 16_777_216, 16_777_216);
        Long(copy, "maximumObjects", 4, 4);
    }

    private static void VerifyInvocation(JsonElement invocation)
    {
        RequireFields(invocation, "F invocation", InvocationFields);
        RequireSequence(Array(invocation, "argv"), ["--in", "/input"], "F invocation argv");
        var environment = Array(invocation, "environment");
        Require(environment.GetArrayLength() == 2, "F invocation environment length");
        RequireEnvironment(environment[0], "HOME", "/tmp", "F invocation environment 0");
        RequireEnvironment(environment[1], "LANG", "C", "F invocation environment 1");
        Exact(invocation, "inputPath", "/input", "F invocation inputPath");
        Exact(invocation, "mode", "work", "F invocation mode");
        Require(Boolean(invocation, "stdinEof"), "F invocation stdinEof");
        Exact(invocation, "toolPath", "/tool", "F invocation toolPath");
        Exact(invocation, "workDirectory", "/tmp", "F invocation workDirectory");
    }

    private static void VerifySandbox(JsonElement sandbox)
    {
        RequireFields(sandbox, "F sandbox", SandboxFields);
        Require(Array(sandbox, "capabilities").GetArrayLength() == 0, "F sandbox capabilities");
        RequireSequence(Array(sandbox, "namespaces"),
            ["ipc", "mount", "net", "pid", "user", "uts"], "F sandbox namespaces");
        Exact(sandbox, "networkQualification", "PendingQualification", "F sandbox networkQualification");
        Require(Boolean(sandbox, "noAmbientEnvironment"), "F sandbox noAmbientEnvironment");
        Require(Boolean(sandbox, "noExtraInheritedFds"), "F sandbox noExtraInheritedFds");
        Require(Boolean(sandbox, "noIpcEndpoints"), "F sandbox noIpcEndpoints");
        Require(Boolean(sandbox, "noNewPrivs"), "F sandbox noNewPrivs");
        RequireSequence(Array(sandbox, "readOnlyDestinations"),
            ["/coordinator", "/input", "/tool"], "F sandbox readOnlyDestinations");
        Require(!Boolean(sandbox, "scratchNoexec"), "F sandbox scratchNoexec");
        Require(Boolean(sandbox, "toolExecutable"), "F sandbox toolExecutable");
        var stores = Array(sandbox, "writableStores");
        Require(stores.GetArrayLength() == 2, "F sandbox writableStores length");
        VerifyWritableStore(stores[0], "/dev/shm", 1_048_576, "F sandbox store 0");
        VerifyWritableStore(stores[1], "/tmp", 7_340_032, "F sandbox store 1");
    }

    private static void VerifyWritableStore(JsonElement store, string path, long bytes, string what)
    {
        Require(store.ValueKind == JsonValueKind.Object, $"{what} kind");
        RequireFields(store, what, WritableStoreFields);
        Long(store, "bytes", bytes, bytes);
        Require(!Boolean(store, "hasInodeBudget"), $"{what} hasInodeBudget");
        Long(store, "inodeBudget", 0, 0);
        Exact(store, "path", path, $"{what} path");
    }

    private static void VerifyResources(JsonElement resources, long maximumSessionNs)
    {
        RequireFields(resources, "F resources", ResourceFields);
        Long(resources, "captureAggregateBytes", 131_072, 131_072);
        Long(resources, "configBytes", 16_384, 16_384);
        Long(resources, "coordinatorStatusBytes", 16_384, 16_384);
        Require(Boolean(resources, "coreDisabled"), "F resources coreDisabled");
        Require(resources.GetProperty("corePatternObserved").ValueKind == JsonValueKind.Null,
            "F resources corePatternObserved");
        var expiresLeadNs = Long(resources, "expiresLeadNs", 1, 120_000_000_000);
        Require(expiresLeadNs <= maximumSessionNs, "F resources expiresLeadNs session bound");
        Long(resources, "fileDescriptors", 64, 64);
        Long(resources, "finalDrainNs", 1_000_000_000, 1_000_000_000);
        Long(resources, "helperStatusBytes", 16_384, 16_384);
        Exact(resources, "inodeQualification", "PendingQualification", "F resources inodeQualification");
        Long(resources, "journalBytes", 65_536, 65_536);
        Long(resources, "memoryBytes", 134_217_728, 134_217_728);
        Long(resources, "overallTimeoutNs", 30_000_000_000, 30_000_000_000);
        Long(resources, "receiptBytes", 32_768, 32_768);
        Require(!Boolean(resources, "runtimeDumpsDisabled"), "F resources runtimeDumpsDisabled");
        Long(resources, "setupTimeoutNs", 10_000_000_000, 10_000_000_000);
        Long(resources, "shmBytes", 1_048_576, 1_048_576);
        Long(resources, "stagingBytes", 16_777_216, 16_777_216);
        Long(resources, "stagingObjects", 128, 128);
        Long(resources, "stderrBytes", 65_536, 65_536);
        Long(resources, "stdoutBytes", 65_536, 65_536);
        Long(resources, "stopAllowanceNs", 5_000_000_000, 5_000_000_000);
        Long(resources, "swapBytes", 0, 0);
        Long(resources, "tasks", 48, 48);
        Long(resources, "tmpBytes", 7_340_032, 7_340_032);
    }

    private static void RequireEnvironment(JsonElement entry, string name, string value, string what)
    {
        Require(entry.ValueKind == JsonValueKind.Object, $"{what} kind");
        RequireFields(entry, what, EnvironmentFields);
        Exact(entry, "name", name, $"{what} name");
        Exact(entry, "value", value, $"{what} value");
    }

    private static void RequireSequence(JsonElement array, string[] expected, string what)
    {
        Require(array.GetArrayLength() == expected.Length, $"{what} length");
        for (var index = 0; index < expected.Length; index++)
        {
            var item = array[index];
            Require(item.ValueKind == JsonValueKind.String && item.GetString() == expected[index],
                $"{what} value");
        }
    }

    private static void RequireFields(JsonElement element, string what, string[] expected)
    {
        // Canonical ascending order is already enforced by the codec; this checks
        // the exact accepted field set (no missing, duplicate, or extension field).
        Require(element.EnumerateObject().Select(property => property.Name).SequenceEqual(expected),
            $"{what} field set");
    }

    private static JsonElement Object(JsonElement parent, string key)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.Object, $"{key} object");
        return value;
    }

    private static JsonElement Array(JsonElement parent, string key)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.Array, $"{key} array");
        return value;
    }

    private static string Text(JsonElement parent, string key, int maximum = 128)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.String, $"{key} string");
        var text = value.GetString()!;
        Require(text.Length is > 0 && text.Length <= maximum, $"{key} text");
        return text;
    }

    private static string Exact(JsonElement parent, string key, string expected, string what)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.String && value.GetString() == expected, what);
        return expected;
    }

    private static string Choice(JsonElement parent, string key, string[] allowed, string what)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.String && allowed.Contains(value.GetString()!), what);
        return value.GetString()!;
    }

    private static string Hex(JsonElement parent, string key, int length = 64)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.String, $"{key} hex");
        var text = value.GetString()!;
        Require(CanonicalJson.IsLowerHex(text, length), $"{key} hex");
        return text;
    }

    private static long Long(JsonElement parent, string key, long minimum, long maximum)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.Number, $"{key} integer");
        Require(value.TryGetInt64(out var number), $"{key} integer");
        Require(number >= minimum && number <= maximum, $"{key} integer");
        return number;
    }

    private static bool Boolean(JsonElement parent, string key)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind is JsonValueKind.True or JsonValueKind.False, $"{key} boolean");
        return value.ValueKind == JsonValueKind.True;
    }

    private static string ValidId(JsonElement parent, string key, string what)
    {
        var value = Text(parent, key);
        Require(FrozenSchemaRules.IsValidId(value), what);
        return value;
    }

    private static void Require(bool condition, string diagnostic)
    {
        if (!condition) throw new InvalidDataException(diagnostic);
    }
}
