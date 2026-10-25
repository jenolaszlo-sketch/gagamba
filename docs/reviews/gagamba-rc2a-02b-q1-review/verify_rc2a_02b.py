#!/usr/bin/env python3
"""Recheck committed RC2A-02B evidence and the independent Windows probe TRXs."""

from __future__ import annotations

import collections
import hashlib
import json
import pathlib
import subprocess
import xml.etree.ElementTree as ET


BASE = "e5fff9e8d5a31520f8b5a2c235a05b666a08e16c"
EXECUTION = "5b4292d0fefc576664df61692dfad5cef6362879"
FINAL = "dd16baec48663d40cd29504d98e57e30dd0855df"
EVIDENCE = "docs/evidence/restricted-consumer-contract-rc2a-02b-portability"
EXPECTED_TREES = {
    BASE: "8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e",
    EXECUTION: "3f76b861e689d04a17c64f8a3e71e57851c21f58",
    FINAL: "96e86aac2d284637805809b54da72e76bf46f1fc",
}
EXPECTED_SUBTREES = {
    "src": "b8c167e3d7340a562221591bdc2b6df7adb372d1",
    "tests": "5c3268286575cb55891fae9288627657f2d30ddf",
    "native": "85631d2f04512376621d3d09f51d67fd90f45335",
}
NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def run_git(root: pathlib.Path, *args: str) -> str:
    return subprocess.check_output(["git", "-C", str(root), *args], text=True).strip()


def blob(root: pathlib.Path, revision: str, path: str) -> bytes:
    return subprocess.check_output(["git", "-C", str(root), "show", f"{revision}:{path}"])


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def trx_multiset(data: bytes) -> collections.Counter[tuple[str, str]]:
    root = ET.fromstring(data)
    return collections.Counter(
        (item.attrib.get("testName", ""), item.attrib.get("outcome", ""))
        for item in root.findall(".//t:UnitTestResult", NS)
    )


def outcome_counts(items: collections.Counter[tuple[str, str]]) -> dict[str, int]:
    counts = collections.Counter(outcome for _, outcome in items.elements())
    return {
        "total": sum(counts.values()),
        "passed": counts.get("Passed", 0),
        "failed": counts.get("Failed", 0),
        "skipped_or_not_executed": counts.get("NotExecuted", 0) + counts.get("Skipped", 0),
    }


def read_trx_file(path: pathlib.Path) -> collections.Counter[tuple[str, str]]:
    return trx_multiset(path.read_bytes())


def main() -> None:
    report_dir = pathlib.Path(__file__).resolve().parent
    root = report_dir.parents[2]
    review_artifacts = report_dir.parent / "gagamba-rc2a-02b-q1-artifacts"
    evidence_prefix = EVIDENCE + "/"

    identities = {}
    for commit, expected_tree in EXPECTED_TREES.items():
        identities[commit] = {
            "tree": run_git(root, "rev-parse", f"{commit}^{{tree}}"),
            "expectedTree": expected_tree,
            "treeMatches": run_git(root, "rev-parse", f"{commit}^{{tree}}") == expected_tree,
            "parents": run_git(root, "show", "-s", "--format=%P", commit),
        }

    subtree_ids = {
        subtree: run_git(root, "rev-parse", f"{BASE}:{subtree}")
        for subtree in EXPECTED_SUBTREES
    }
    changed_paths = run_git(root, "diff-tree", "--no-commit-id", "--name-only", "-r", BASE, FINAL).splitlines()
    final_delta = run_git(root, "diff-tree", "--no-commit-id", "--name-only", "-r", EXECUTION, FINAL).splitlines()
    source_changes = [p for p in changed_paths if p.startswith(("src/", "tests/", "native/"))]

    sum_blob = blob(root, FINAL, f"{EVIDENCE}/SHA256SUMS")
    sum_entries = {}
    for line in sum_blob.decode("ascii").splitlines():
        expected, relative_path = line.split("  ", 1)
        sum_entries[relative_path] = expected
    sum_mismatches = []
    for relative_path, expected in sum_entries.items():
        actual = sha256(blob(root, FINAL, evidence_prefix + relative_path))
        if actual != expected:
            sum_mismatches.append({"path": relative_path, "expected": expected, "actual": actual})

    document_manifest = json.loads(blob(root, FINAL, f"{EVIDENCE}/document-sha256.json"))
    document_mismatches = []
    for entry in document_manifest["files"]:
        normalized = blob(root, FINAL, entry["path"]).replace(b"\r\n", b"\n")
        actual = sha256(normalized)
        if len(normalized) != entry["bytes"] or actual != entry["sha256"]:
            document_mismatches.append(
                {"path": entry["path"], "expectedBytes": entry["bytes"], "actualBytes": len(normalized),
                 "expectedSha256": entry["sha256"], "actualSha256": actual}
            )

    results = json.loads(blob(root, FINAL, f"{EVIDENCE}/results.json"))
    result_hash_refs: list[tuple[str, str]] = []
    for run in results.get("runs", []):
        for artifact in run.get("artifacts", {}).values():
            result_hash_refs.append((artifact["path"], artifact["sha256"]))
    for attempt in results.get("environmentSetupAttempts", []):
        for artifact in attempt.get("artifacts", {}).values():
            result_hash_refs.append((artifact["path"], artifact["sha256"]))
    for build in results.get("builds", []):
        result_hash_refs.append((build["log"]["path"], build["log"]["sha256"]))
    for environment in results.get("environments", {}).values():
        result_hash_refs.append((environment["file"], environment["sha256"]))
    result_hash_mismatches = []
    for path, expected in result_hash_refs:
        actual = sha256(blob(root, FINAL, evidence_prefix + path))
        if actual != expected:
            result_hash_mismatches.append(
                {"path": path, "resultsJsonSha256": expected, "committedBlobSha256": actual,
                 "matchesExecutionCommitManifest": expected == _manifest_hash(root, EXECUTION, path)}
            )

    producer_names = {}
    producer_paths = {
        "windowsPublic": "runs/windows/windows-public.trx",
        "windowsRequired": "runs/windows/windows-required-set.trx",
        "windowsRequiredRerun": "runs/windows/windows-required-set-rerun.trx",
        "windowsBroadPlanFilter": "runs/windows/windows-plan-filter.trx",
        "wsl2Public": "runs/wsl2/wsl2-public.trx",
        "wsl2Required": "runs/wsl2/wsl2-required-set.trx",
        "wsl2RequiredRerun": "runs/wsl2/wsl2-required-set-rerun.trx",
    }
    producer_counts = {}
    for label, path in producer_paths.items():
        items = trx_multiset(blob(root, FINAL, evidence_prefix + path))
        producer_names[label] = items
        producer_counts[label] = outcome_counts(items)

    required_names = producer_names["windowsRequired"]
    broad_names = producer_names["windowsBroadPlanFilter"]
    extras = broad_names - required_names
    required_test_names = [name for name, _outcome in required_names.elements()]
    precise_filter_components = {
        "Tests.Qualification.": sum("Tests.Qualification." in name for name in required_test_names),
        "Contracts.ManifestTests": sum("Contracts.ManifestTests" in name for name in required_test_names),
        "Contracts.RestrictedPublicProjectionTests": sum(
            "Contracts.RestrictedPublicProjectionTests" in name for name in required_test_names
        ),
        "PreparationIdentityTests": sum("PreparationIdentityTests" in name for name in required_test_names),
    }
    extras_by_class = collections.Counter()
    extras_by_outcome = collections.Counter()
    for (test_name, outcome), count in extras.items():
        class_name = test_name.rsplit(".", 1)[0].rsplit(".", 1)[-1]
        extras_by_class[f"{class_name}/{outcome}"] += count
        extras_by_outcome[outcome] += count

    independent_dir = review_artifacts / "windows"
    independent = {
        "public": independent_dir / "q1-review-windows-public-elevated.trx",
        "required": independent_dir / "q1-review-windows-required-captured.trx",
        "requiredRerun": independent_dir / "q1-review-windows-required-captured-rerun.trx",
    }
    independent_names = {label: read_trx_file(path) for label, path in independent.items()}
    independent_counts = {label: outcome_counts(items) for label, items in independent_names.items()}

    result = {
        "reviewedCommits": identities,
        "commitChainMatches": (
            identities[EXECUTION]["parents"] == BASE
            and identities[FINAL]["parents"] == EXECUTION
            and identities[BASE]["treeMatches"]
            and identities[EXECUTION]["treeMatches"]
            and identities[FINAL]["treeMatches"]
        ),
        "sourceSubtreesAtAcceptedBaseline": {
            name: {"actual": subtree_ids[name], "expected": EXPECTED_SUBTREES[name],
                  "matches": subtree_ids[name] == EXPECTED_SUBTREES[name]}
            for name in subtree_ids
        },
        "sourceChangesFromBaselineToEvidence": source_changes,
        "evidencePathsChangedByFinalBindingCommit": final_delta,
        "changedPathCountFromBaselineToFinal": len(changed_paths),
        "gitEvidence": {
            "executionParentIsBaseline": identities[EXECUTION]["parents"] == BASE,
            "finalParentIsExecution": identities[FINAL]["parents"] == EXECUTION,
            "noSrcTestsNativeChanges": not source_changes,
        },
        "sha256sums": {
            "entryCount": len(sum_entries),
            "allCommittedBlobHashesMatch": len(sum_entries) == 43 and not sum_mismatches,
            "mismatches": sum_mismatches,
        },
        "documentBindings": {
            "entryCount": len(document_manifest["files"]),
            "allLfNormalizedBindingsMatch": len(document_manifest["files"]) == 9 and not document_mismatches,
            "mismatches": document_mismatches,
        },
        "resultsJsonArtifactSha256": {
            "referenceCount": len(result_hash_refs),
            "mismatchCount": len(result_hash_mismatches),
            "mismatches": result_hash_mismatches,
        },
        "producerRawTrx": {
            "counts": producer_counts,
            "windowsRequiredEqualsRerun": producer_names["windowsRequired"] == producer_names["windowsRequiredRerun"],
            "wsl2RequiredEqualsRerun": producer_names["wsl2Required"] == producer_names["wsl2RequiredRerun"],
            "windowsPublicEqualsWsl2Public": producer_names["windowsPublic"] == producer_names["wsl2Public"],
            "windowsRequiredEqualsWsl2Required": producer_names["windowsRequired"] == producer_names["wsl2Required"],
            "broadFilterIncludesAllPreciseTests": not (required_names - broad_names),
            "broadFilterExtraCount": sum(extras.values()),
            "broadFilterExtraOutcomes": dict(extras_by_outcome),
            "broadFilterExtraClassesAndOutcomes": dict(extras_by_class),
            "preciseFilterComponentCounts": precise_filter_components,
            "preciseFilterComponentsCoverAllRequiredTests": sum(precise_filter_components.values())
            == len(required_names),
        },
        "independentWindowsRuns": {
            "counts": independent_counts,
            "publicEqualsProducerWindows": independent_names["public"] == producer_names["windowsPublic"],
            "requiredEqualsProducerWindows": independent_names["required"] == producer_names["windowsRequired"],
            "requiredRerunEqualsRequired": independent_names["requiredRerun"] == independent_names["required"],
        },
    }
    output = report_dir / "verification-results.json"
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, indent=2))


def _manifest_hash(root: pathlib.Path, revision: str, artifact_path: str) -> str | None:
    lines = blob(root, revision, f"{EVIDENCE}/SHA256SUMS").decode("ascii").splitlines()
    for line in lines:
        digest, path = line.split("  ", 1)
        if path == artifact_path:
            return digest
    return None


if __name__ == "__main__":
    main()
