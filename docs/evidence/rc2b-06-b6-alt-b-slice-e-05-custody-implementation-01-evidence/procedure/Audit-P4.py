import csv
import json
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

repo, scratch = (Path(x).resolve() for x in sys.argv[1:3])
receipt = json.loads((scratch / "verified-source-receipt.json").read_text(encoding="utf-8"))
candidate = receipt["commit"]
baseline = receipt["parent"]
def git(*args, code=0):
    p = subprocess.run(["git", "--no-optional-locks", *args], cwd=repo, capture_output=True)
    assert p.returncode == code, (args, p.returncode, p.stderr)
    return p.stdout.decode().splitlines()

actual = git("diff", "--name-status", baseline, candidate)
assert sorted(actual) == sorted("A\t" + p["path"] for p in receipt["files"])
def callers(sha, pattern, *paths):
    return [line.removeprefix(sha + ":") for line in git("grep", "-nE", pattern, sha, "--", *paths)]
effect = ("StartOwnerPreparation\\(|TrySpawnOwner\\(|new PreparationOwnershipCoordinator\\(|"
          "IntegratedRecoveryHost.Run\\(|QualifiedRecoveryConnection.Start\\(|RecoveryConnection.Start\\(|"
          "new HeldReleasePhase\\(|new B3PreparationOwner\\(|OwnerPreparationTransaction.Run\\(")
b = callers(baseline, effect, "src", "tests")
c = callers(candidate, effect, "src", "tests")
assert b == c
new_source_refs = callers(candidate, "ValidationCustody", "src", "tests", "eng", "native")
assert all(line.split(":")[0] in {f["path"] for f in receipt["files"]} for line in new_source_refs)

base = "src/Gagamba.Execution.Linux.M1/"
sites = {
  base + "Bootstrap/IntegratedRecoveryHost.cs": "RequireIntegrated|VerifyPrivateDirectory|JournalFileStorage|StartOwnerPreparation",
  base + "Bootstrap/RecoveryHost.cs": "OpenDelegatedParent|QualifiedRecoveryEcho|IntegratedRecoveryHost.Run",
  base + "Bootstrap/QualifiedRecoveryEcho.cs": "RecoveryInitializationAdmission.Require|return .*launch",
  base + "Bootstrap/RecoveryConnection.cs": "QualifiedRecoveryConnection.Start",
  base + "Bootstrap/IntegratedB3Client.cs": "RecoveryConnection.Start|RequireBoundGenerationActive|ReleaseRequest",
  base + "Lifecycle/M1RecoveryScope.cs": "TrySpawnOwner|AllocationIntent|StartOwnerPreparation",
  base + "Lifecycle/OwnerPreparationTransaction.cs": "RunTransaction|JournalFileStorage|HeldReleasePhase|OwnerManifestAdmission",
  base + "Restricted/B3Qualification.cs": "B3ProviderAdmission.Require|Directory.CreateDirectory|StartOwnerPreparation",
  base + "Lifecycle/PreparationOwnershipCoordinator.cs": "PreparationOwnershipCoordinator\\(",
  base + "Lifecycle/OwnerAuthorityContext.cs": "class Validation|private Validation",
  "src/Gagamba.Runtime/RestrictedExecutionRuntime.cs": "UnavailableRestrictedExecutionProvider",
}
lines = ["Candidate " + candidate, "Authorization baseline " + baseline, "",
         "git diff --name-status <baseline> <candidate>", *actual, "",
         "git grep -nE " + repr(effect) + " <each revision> -- src tests",
         "Accepted reverse-caller inventory equals the candidate, with revision prefix removed.", *c, "",
         "First-effect and refusal source checks follow."]
for path, regex in sites.items():
    old = git("rev-parse", baseline + ":" + path)[0]
    new = git("rev-parse", candidate + ":" + path)[0]
    assert old == new, path
    source = git("show", candidate + ":" + path)
    lines.append(new + " " + path)
    lines.extend(f"{n}: {line.strip()}" for n, line in enumerate(source, 1) if re.search(regex, line))
model_source = []
for item in receipt["files"]:
    if item["path"].startswith("src/"):
        model_source.extend(git("show", candidate + ":" + item["path"]))
assert not re.search(r'\b(DllImport|LibraryImport|Activator|Assembly|Process|File|Socket|Environment|OwnerAuthorityContext|'
                     r'PrepublicationIntegrityProjection|FrozenContractVerifier|DetachedEvidenceVerifier|'
                     r'Gava1ContextCodec|ValidationPreCodec|ReleaseArbiter|ModuleInitializer)\b',
                     "\n".join(model_source))
lines.append("New model references are confined to the six additions by source inspection.")
(scratch / "p4-source.log").write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")

accepted = (repo / "docs/evidence/rc2b-06-b6-alt-b-p4-coverage-recon-01.md").read_text(encoding="utf-8")
rows = []
for line in accepted.splitlines():
    match = re.match(r'^\| \*\*(S\d{2})\*\* ', line)
    if match:
        disposition = re.search(r'\*\*([^*]+)\*\*', line.split("|")[-2]).group(1)
        rows.append((match[1], disposition, disposition))
assert len(rows) == 29 and [sum(row[1] == d for row in rows) for d in
                            ("COVERED", "AUTHORIZED CONTINUATION", "LEGACY / TEST-ONLY")] == [21, 5, 3]
with (scratch / "p4-dispositions.tsv").open("w", encoding="utf-8", newline="") as f:
    out = csv.writer(f, delimiter="\t", lineterminator="\n")
    out.writerow(["sink", "accepted", "producer_candidate"])
    out.writerows(rows)

controls = [
 "DirectIntegratedRunRefusesMissingRetainedContextBeforeInitializationEffects",
 "DirectIntegratedRunRejectsMismatchedRetainedIdentityBeforeInitializationEffects",
 "LocalGateDominatesJournalWriterAndLaunchInitializationOnActualRunWiring",
 "LegacyIntegratedContextRejectsInstalledSelectionAndDoesNotFallBack",
 "RecoveryEchoWorkerChecksAdmissionBeforeOwnerLaunchPlanAndParentAcquisition",
 "InitialFinalAdmissionPrecedesEveryProtectedInitializationEffect",
 "ValidationContextRemainsNonConstructibleAndCannotSatisfyTheGate",
 "ForeignIssuerRefused", "ForeignGenerationRefused",
 "QualifiedContextCannotSelectUngatedLegacyReleaseByOmittingWireIdentity",
 "ChangedActiveGenerationRefusesAsUnqualified", "ReplacedGenerationRefusesAsUnqualified",
 "StopPreservesOriginalC3ObligationAndCannotCreateReleaseAuthority",
 "StoppingReconciliationAndRecoveryDoNotRequireRenewedManifestAdmission",
]
with (scratch / "p4-controls.tsv").open("w", encoding="utf-8", newline="") as f:
    out = csv.writer(f, delimiter="\t", lineterminator="\n")
    out.writerow(["configuration", "executed_case", "result"])
    for config in ("debug", "release"):
        tree = ET.parse(scratch / "results" / config / "broad.xml")
        for control in controls:
            cases = [t.attrib for t in tree.findall(".//test") if control in t.get("method", t.get("name", ""))]
            assert cases and all(case["result"] == "Pass" for case in cases), (control, config)
            out.writerows((config, case["name"], case["result"]) for case in cases)
print("P4 source, 29 dispositions and executed refusal controls verified.")
