import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

repo, run = map(lambda x: Path(x).resolve(), sys.argv[1:3])
candidate = "28895d55de98eea4bc402373b653f7216e092843"
published = "1116c66d7858e6000f0001a0abe39319e4e694be"
source = "src/Gagamba.Execution.Linux.M1/"
new_files = {source + "Bootstrap/ValidationCustodyModel.cs", source + "Bootstrap/ValidationCustodyModelData.cs"}

def git(*args, status=(0,)):
    p = subprocess.run(["git", "--no-optional-locks", *args], cwd=repo, capture_output=True)
    assert p.returncode in status, (args,p.returncode,p.stderr.decode(errors="replace"))
    return p.stdout.decode()

allowed = {
    "allowlisted model and regression source": (
        r"\bValidationCustody\w*", ["src","tests","eng","native",".github"]),
    "recovery ingress": (
        r"(QualifiedRecoveryConnection|RecoveryConnection)\.Start\(|IntegratedRecoveryHost\.Run\(", ["src","tests"]),
    "spawn and owner preparation": (
        r"\.StartOwnerPreparation\(|\.TrySpawnOwner\(|\bRunTransaction\(", ["src","tests"]),
    "protected preparation callers": (
        r"new (HeldReleasePhase|B3PreparationOwner|PreparationOwnershipCoordinator)\(", ["src","tests"]),
    "owner decision and writer": (
        r"ReleaseGenerationGate\.RequireBoundGenerationActive|commitTransport\.|\.TryCommitRelease\(|\.WriteRelease\(",
        ["src","tests"]),
    "retained stop and reconciliation": (
        r"\b(TerminateAndReap|StopCore|RequireBoundGenerationActive|StoppingReconcile)\(",
        ["src","tests"]),
    "existing P2 to P1 verifier edge": (r"FrozenContractVerifier\.TryVerifyCatalog\(", ["src","tests"]),
}
caller_evidence = {}
for title, (pattern, pathspec) in allowed.items():
    before, current = [
        [line.removeprefix(rev + ":") for line in
         git("grep","-nE",pattern,rev,"--",*pathspec,status=(0,1)).splitlines()]
        for rev in (published, candidate)
    ]
    if title.startswith("allowlisted"):
        assert not before and all("/Bootstrap/ValidationCustodyModel" in line for line in current)
    else:
        assert before and before == current, title
    caller_evidence[title] = dict(expression=pattern, source_scope=pathspec, published_same=before==current, lines=current)

def show(path):
    return git("show", candidate + ":" + path).splitlines()

selectors = {
    source + "Bootstrap/IntegratedRecoveryHost.cs":
        r"RequireIntegrated|VerifyPrivateDirectory|JournalFileStorage|StartOwnerPreparation",
    source + "Bootstrap/RecoveryHost.cs":
        r"OpenDelegatedParent|QualifiedRecoveryEcho|IntegratedRecoveryHost.Run",
    source + "Bootstrap/QualifiedRecoveryEcho.cs":
        r"RecoveryInitializationAdmission.Require|return .*launch",
    source + "Bootstrap/IntegratedB3Client.cs":
        r"RecoveryConnection.Start|RequireBoundGenerationActive|ReleaseRequest|Stop",
    source + "Lifecycle/M1RecoveryScope.cs":
        r"TrySpawnOwner|AllocationIntent|StartOwnerPreparation|TerminateAndReap",
    source + "Lifecycle/OwnerPreparationTransaction.cs":
        r"RunTransaction|JournalFileStorage|HeldReleasePhase|OwnerManifestAdmission",
    source + "Lifecycle/HeldReleasePhase.cs":
        r"ReleaseArbiter|GenerationGate|ReleaseRequest|Stop",
    source + "Lifecycle/OwnerReleaseGenerationGate.cs":
        r"Require|Refus|Generation",
    source + "Execution/ReleaseArbiter.cs":
        r"Interlocked|commitTransport|\.Commit\(|physical|C3|stop",
    source + "Lifecycle/OwnerAuthorityContext.cs":
        r"class Validation|private Validation",
    source + "Lifecycle/PreparationOwnershipCoordinator.cs":
        r"PreparationOwnershipCoordinator\(",
    "src/Gagamba.Runtime/RestrictedExecutionRuntime.cs":
        r"UnavailableRestrictedExecutionProvider",
}
locations = {}
for path, pattern in selectors.items():
    first, last = [git("rev-parse", rev + ":" + path).strip() for rev in (published,candidate)]
    assert first == last, path
    matches = {str(i): line.strip() for i,line in enumerate(show(path), 1) if re.search(pattern,line)}
    assert matches, path
    locations[path] = dict(blob=last, line_evidence=matches)

existing = git("diff","--name-status",published,candidate,"--","src","tests","eng",".github","native",
               "Directory.Build.targets","Directory.Build.props","Gagamba.sln").splitlines()
assert len(existing) == 6 and all(x.startswith("A\t") and "ValidationCustodyModel" in x for x in existing)
assert {x.split("\t")[1] for x in existing if x.startswith("A\tsrc/")} == new_files
fixed = [x for x in git("ls-tree","-r","--name-only",candidate,"src","native","eng","tests").splitlines()
         if re.search(r"(validation[-_.]?launch|validation[-_.]?driver|\bQualifiedLauncher)", x, re.I)]
assert fixed and all("QualifiedLauncher" in x for x in fixed), fixed
for path in sorted(new_files):
    content = "\n".join(show(path))
    assert not re.search(r"Reflect|GetType\(|GetMethod\(|Activator|Marshal|NativeLibrary|\bSocket\b|"
                         r"\bFile\b|DllImport|LibraryImport|ModuleInitializer|TimeProvider|Stopwatch|"
                         r"AuthorityContext|DecodeBootstrap|Gava1ContextCodec|RestrictedExecutionRuntime", content)
    assert "internal sealed class" in content

p4_doc = "docs/evidence/rc2b-06-b6-alt-b-p4-coverage-recon-01.md"
before, after = [git("show",r+":"+p4_doc) for r in (published,candidate)]
assert before == after
dispositions = []
for line in after.splitlines():
    match = re.match(r"^\| \*\*(S\d{2})\*\* ", line)
    if match:
        disposition = re.search(r"\*\*([^*]+)\*\*",line.split("|")[-2]).group(1)
        dispositions.append(dict(row=match[1], candidate=disposition, original=line))
assert [r["row"] for r in dispositions] == [f"S{i:02d}" for i in range(1,30)]
assert [sum(r["candidate"]==v for r in dispositions) for v in
        ["COVERED","AUTHORIZED CONTINUATION","LEGACY / TEST-ONLY"]] == [21,5,3]

record = dict(candidate=candidate, compared_to_published=published, protected_surface_diff=existing,
    caller_review=caller_evidence, first_effect_bindings=locations, fixed_launcher_path_candidates=fixed,
    accepted_p4_sha256=hashlib.sha256(after.encode()).hexdigest(), p4_dispositions=dispositions)
(run/"independent-p4-source.json").write_text(json.dumps(record,indent=2)+"\n",encoding="utf-8")
print("P4 published-to-candidate protected delta, unchanged call sites and all 29 row bindings verified.")
