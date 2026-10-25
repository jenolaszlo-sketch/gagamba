#!/usr/bin/env python3
"""Independent, read-only verification of RC2A-02B-R1."""

from __future__ import annotations

import hashlib
import json
import posixpath
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path
from urllib.parse import unquote, urlsplit


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE = "docs/evidence/restricted-consumer-contract-rc2a-02b-portability"
PLAN = "docs/plans/restricted-consumer-contract-rc2a-02-qualification.md"
Q1_REPORT = "docs/reviews/gagamba-rc2a-02b-q1-review.md"
Q2_REPORT = "docs/reviews/gagamba-rc2a-02b-q2-review/report.md"
Q2_VERIFIER = "docs/reviews/gagamba-rc2a-02b-q2-review/verify_q2.py"

OWNER = "e5fff9e8d5a31520f8b5a2c235a05b666a08e16c"
ACCEPTED_02A_EVIDENCE = "c1a01761f8d58136a381ad87269635c6c14ad093"
TESTED_IMPLEMENTATION = "71a28afe2623516fb4176e6a4ce7988a3d70df06"
EXECUTION = "5b4292d0fefc576664df61692dfad5cef6362879"
QUALIFICATION = "dd16baec48663d40cd29504d98e57e30dd0855df"
Q1_REVIEW = "5bc396c2e5619906ae202afe0a4244dfd6024d9e"
R1_FIX = "12d914d319b1df3eedafc5439bc547dacb578651"
R1_TIP = "1cd22b9b109ddba4673b11487bceea62d2e92117"
Q2_BRANCH = "review/gagamba-rc2a-02b-q2"
Q1_BRANCH = "review/gagamba-rc2a-02b-q1"
R1_BRANCH = "remediation/gagamba-rc2a-02b-r1"
QUALIFICATION_BRANCH = "qualification/gagamba-rc2a-02b"


def run_git(*args: str, check: bool = True) -> subprocess.CompletedProcess[bytes]:
    result = subprocess.run(
        ["git", *args], cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.PIPE
    )
    if check and result.returncode != 0:
        raise RuntimeError(result.stderr.decode("utf-8", "replace").strip())
    return result


def out_git(*args: str) -> str:
    return run_git(*args).stdout.decode("utf-8").strip()


def blob(revision: str, path: str) -> bytes:
    return run_git("show", f"{revision}:{path}").stdout


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def commit_identity(commit: str) -> dict[str, str]:
    fields = out_git("show", "-s", "--format=%H%n%T%n%P", commit).splitlines()
    return {"commit": fields[0], "tree": fields[1], "parents": " ".join(fields[2:])}


def is_ancestor(older: str, newer: str) -> bool:
    return run_git("merge-base", "--is-ancestor", older, newer, check=False).returncode == 0


def collect_refs(value: object, refs: dict[str, str]) -> None:
    if isinstance(value, dict):
        path = value.get("path")
        if not isinstance(path, str):
            path = value.get("file")
        digest_value = value.get("sha256")
        if isinstance(path, str) and isinstance(digest_value, str):
            require(path not in refs, f"duplicate artifact reference: {path}")
            refs[path] = digest_value
        for child in value.values():
            collect_refs(child, refs)
    elif isinstance(value, list):
        for child in value:
            collect_refs(child, refs)


def checksum_entries(revision: str) -> dict[str, str]:
    raw = blob(revision, f"{EVIDENCE}/SHA256SUMS").decode("utf-8")
    entries: dict[str, str] = {}
    for line in raw.splitlines():
        digest_value, path = line.split("  ", 1)
        require(path not in entries, f"duplicate SHA256SUMS path: {path}")
        entries[path] = digest_value
    return entries


def parse_trx(revision: str, relative_path: str) -> tuple[
    Counter[tuple[str, str]], Counter[tuple[str, str]]
]:
    raw = blob(revision, f"{EVIDENCE}/{relative_path}")
    root = ET.fromstring(raw)
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    classes_by_id: dict[str, str] = {}
    for unit in root.findall(".//t:TestDefinitions/t:UnitTest", ns):
        test_id = unit.attrib.get("id")
        method = unit.find("t:TestMethod", ns)
        class_name = method.attrib.get("className") if method is not None else None
        require(test_id is not None and class_name is not None,
                f"TRX test definition lacks id/class: {relative_path}")
        classes_by_id[test_id] = class_name

    names: Counter[tuple[str, str]] = Counter()
    class_outcomes: Counter[tuple[str, str]] = Counter()
    for result in root.findall(".//t:Results/t:UnitTestResult", ns):
        test_id = result.attrib.get("testId")
        name = result.attrib.get("testName")
        outcome = result.attrib.get("outcome")
        require(test_id in classes_by_id and name and outcome,
                f"TRX result lacks mapped test id/name/outcome: {relative_path}")
        names[(name, outcome)] += 1
        class_outcomes[(classes_by_id[test_id], outcome)] += 1
    require(bool(names), f"TRX had no test result rows: {relative_path}")
    return names, class_outcomes


def counts_from_names(names: Counter[tuple[str, str]]) -> dict[str, int]:
    outcome_counts: Counter[str] = Counter()
    for (_, outcome), amount in names.items():
        outcome_counts[outcome] += amount
    skipped = outcome_counts.get("NotExecuted", 0) + outcome_counts.get("Skipped", 0)
    return {
        "total": sum(outcome_counts.values()),
        "passed": outcome_counts.get("Passed", 0),
        "failed": outcome_counts.get("Failed", 0),
        "skipped": skipped,
    }


def markdown_targets(markdown: str) -> list[str]:
    found: list[str] = []
    for match in re.finditer(r"\]\(([^)]+)\)", markdown):
        target = match.group(1).strip()
        if target.startswith("<") and ">" in target:
            target = target[1 : target.index(">")]
        else:
            target = target.split()[0]
        parsed = urlsplit(target)
        if parsed.scheme or parsed.netloc or not parsed.path:
            continue
        found.append(unquote(parsed.path))
    for match in re.finditer(r"^\s*\[[^\]]+\]:\s*(\S+)", markdown, re.MULTILINE):
        target = match.group(1).strip()
        if target.startswith("<") and ">" in target:
            target = target[1 : target.index(">")]
        parsed = urlsplit(target)
        if parsed.scheme or parsed.netloc or not parsed.path:
            continue
        found.append(unquote(parsed.path))
    return found


def resolve_target(source: str, target: str) -> str:
    if target.startswith("/"):
        return posixpath.normpath(target.lstrip("/"))
    return posixpath.normpath(posixpath.join(posixpath.dirname(source), target))


def tree_path_exists(revision: str, path: str) -> bool:
    if path in ("", "."):
        return True
    return run_git("cat-file", "-e", f"{revision}:{path}", check=False).returncode == 0


def check_links(revision: str, explicit_paths: list[str]) -> list[dict[str, str]]:
    linked: list[dict[str, str]] = []
    for source in explicit_paths:
        content = blob(revision, source).decode("utf-8")
        for target in markdown_targets(content):
            resolved = resolve_target(source, target)
            require(tree_path_exists(revision, resolved),
                    f"unresolved relative link at {revision}:{source}: {target} -> {resolved}")
            linked.append({"from": source, "target": target, "resolved": resolved})
    return linked


def worktree_for_branch(branch: str) -> str:
    records = out_git("worktree", "list", "--porcelain").split("\n\n")
    for record in records:
        fields = dict(line.split(" ", 1) for line in record.splitlines() if " " in line)
        if fields.get("branch") == f"refs/heads/{branch}":
            return fields["worktree"]
    raise AssertionError(f"no worktree is attached to {branch}")


def main() -> None:
    current_head = out_git("rev-parse", "HEAD")
    require(out_git("branch", "--show-current") == Q2_BRANCH,
            f"Q2 worktree must be on {Q2_BRANCH}")
    require(out_git(f"rev-parse", f"refs/heads/{QUALIFICATION_BRANCH}") == QUALIFICATION,
            "qualification branch pointer changed")
    require(out_git("rev-parse", f"refs/heads/{Q1_BRANCH}") == Q1_REVIEW,
            "Q1 review branch pointer changed")
    require(out_git("rev-parse", f"refs/heads/{R1_BRANCH}") == R1_TIP,
            "R1 remediation branch pointer changed")
    require(is_ancestor(R1_TIP, current_head), "Q2 branch is not based on final R1")
    require(not is_ancestor(Q1_REVIEW, current_head), "Q1 review was merged into Q2")
    require(out_git("rev-parse", "HEAD^") == R1_TIP,
            "Q2 review commit must be a direct child of final R1")

    identities = {
        OWNER: {
            "tree": "8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e",
            "parent": ACCEPTED_02A_EVIDENCE,
        },
        EXECUTION: {
            "tree": "3f76b861e689d04a17c64f8a3e71e57851c21f58",
            "parent": OWNER,
        },
        QUALIFICATION: {
            "tree": "96e86aac2d284637805809b54da72e76bf46f1fc",
            "parent": EXECUTION,
        },
        Q1_REVIEW: {
            "tree": "8ebbccb7b3787ce8e57bc74bbc93a8c1edb9e650",
            "parent": QUALIFICATION,
        },
        R1_FIX: {
            "tree": "35cc347f8e436bd1fec9997cb50ede81e561767c",
            "parent": QUALIFICATION,
        },
        R1_TIP: {
            "tree": "2f2bae947dee368ae073dd3fe7877380ca8854ff",
            "parent": R1_FIX,
        },
    }
    identity_report = {}
    for commit, expected in identities.items():
        actual = commit_identity(commit)
        parent = actual["parents"].split()[0] if actual["parents"] else ""
        require(actual["tree"] == expected["tree"], f"tree mismatch for {commit}")
        require(parent == expected["parent"], f"parent mismatch for {commit}")
        identity_report[commit] = actual
    require(not is_ancestor(Q1_REVIEW, R1_TIP), "Q1 review is an ancestor of R1")

    final_results_path = f"{EVIDENCE}/results.json"
    base_results = json.loads(blob(QUALIFICATION, final_results_path))
    final_results = json.loads(blob(R1_TIP, final_results_path))
    result_refs_base: dict[str, str] = {}
    result_refs_final: dict[str, str] = {}
    collect_refs(base_results, result_refs_base)
    collect_refs(final_results, result_refs_final)
    require(len(result_refs_base) == 29 and len(result_refs_final) == 29,
            "expected 29 results.json artifact references in both versions")
    require(result_refs_base.keys() == result_refs_final.keys(),
            "results.json artifact path set changed")

    computed_artifacts = []
    corrected = []
    for path, old_digest in result_refs_base.items():
        old_blob_oid = out_git("rev-parse", f"{QUALIFICATION}:{EVIDENCE}/{path}")
        final_blob_oid = out_git("rev-parse", f"{R1_TIP}:{EVIDENCE}/{path}")
        require(old_blob_oid == final_blob_oid,
                f"raw qualification artifact changed: {path}")
        calculated = sha256(blob(R1_TIP, f"{EVIDENCE}/{path}"))
        recorded = result_refs_final[path]
        require(recorded == calculated, f"results.json does not match independently hashed bytes: {path}")
        item = {
            "path": path,
            "qualificationRecordedSha256": old_digest,
            "computedCommittedSha256": calculated,
            "finalResultsJsonSha256": recorded,
            "rawBlobOidUnchanged": True,
        }
        computed_artifacts.append(item)
        if old_digest != calculated:
            corrected.append(item)
    require(len(corrected) == 14, f"expected 14 R1-01 corrections, found {len(corrected)}")
    require(all(x["path"].startswith("runs/windows/") for x in corrected),
            "a non-Windows digest was corrected")

    execution_sums = checksum_entries(EXECUTION)
    require(all(execution_sums[x["path"]] == x["qualificationRecordedSha256"]
                for x in corrected),
            "stale values do not reproduce the execution-commit manifest")
    final_sums = checksum_entries(R1_TIP)
    base_sums = checksum_entries(QUALIFICATION)
    require(len(final_sums) == 43 and len(base_sums) == 43,
            "expected 43 SHA256SUMS entries")
    require(final_sums.keys() == base_sums.keys(), "SHA256SUMS path set changed")
    for path, expected in final_sums.items():
        require(sha256(blob(R1_TIP, f"{EVIDENCE}/{path}")) == expected,
                f"SHA256SUMS digest mismatch: {path}")

    base_doc_manifest = json.loads(blob(QUALIFICATION, f"{EVIDENCE}/document-sha256.json"))
    final_doc_manifest = json.loads(blob(R1_TIP, f"{EVIDENCE}/document-sha256.json"))
    base_docs = {x["path"]: x for x in base_doc_manifest["files"]}
    final_docs = {x["path"]: x for x in final_doc_manifest["files"]}
    require(len(base_docs) == 9 and len(final_docs) == 9,
            "expected nine applicable document bindings")
    require(base_docs.keys() == final_docs.keys(),
            "R1 changed the set of document digest bindings")
    for path, entry in final_docs.items():
        normalized = blob(R1_TIP, path).replace(b"\r\n", b"\n")
        require(len(normalized) == entry["bytes"], f"document byte-count mismatch: {path}")
        require(sha256(normalized) == entry["sha256"], f"document digest mismatch: {path}")

    require(final_results["baseline"]["startCommit"] == OWNER,
            "results source baseline commit changed")
    require(final_results["baseline"]["startTree"] == identities[OWNER]["tree"],
            "results source baseline tree changed")
    require(final_results["baseline"]["acceptedQualificationEvidence"] == ACCEPTED_02A_EVIDENCE,
            "accepted RC2A-02A evidence identity changed")
    require(final_results["baseline"]["testedImplementation"] == TESTED_IMPLEMENTATION,
            "tested implementation identity changed")
    expected_trees = {
        "src": "b8c167e3d7340a562221591bdc2b6df7adb372d1",
        "tests": "5c3268286575cb55891fae9288627657f2d30ddf",
        "native": "85631d2f04512376621d3d09f51d67fd90f45335",
    }
    for path, expected in expected_trees.items():
        for revision in (OWNER, QUALIFICATION, R1_TIP):
            require(out_git("rev-parse", f"{revision}:{path}") == expected,
                    f"{path} subtree changed at {revision}")
    require(out_git("show", "-s", "--format=%T", ACCEPTED_02A_EVIDENCE) ==
            "8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19",
            "accepted RC2A-02A evidence tree changed")
    require(out_git("show", "-s", "--format=%T", TESTED_IMPLEMENTATION) ==
            "70b932148cbfa59b0e424229879eebd36df2d80c",
            "tested implementation tree changed")

    runs_base = {x["id"]: x for x in base_results["runs"]}
    runs_final = {x["id"]: x for x in final_results["runs"]}
    require(runs_base.keys() == runs_final.keys(), "run ID set changed")
    require({k: v["counters"] for k, v in runs_base.items()} ==
            {k: v["counters"] for k, v in runs_final.items()},
            "R1 changed recorded test counts")
    def build_outcomes(builds: list[dict[str, object]]) -> list[tuple[object, ...]]:
        return [
            (x["platform"], x["command"], x["exitCode"], x["warnings"],
             x["errors"], x["log"]["path"])
            for x in builds
        ]

    require(build_outcomes(base_results["builds"]) ==
            build_outcomes(final_results["builds"]),
            "R1 changed recorded build commands or outcomes")
    run_names: dict[str, Counter[tuple[str, str]]] = {}
    class_outcomes: dict[str, Counter[tuple[str, str]]] = {}
    expected_counts = {
        "W1": (67, 67, 0, 0),
        "W2": (457, 457, 0, 0),
        "W3": (457, 457, 0, 0),
        "W4": (514, 473, 0, 41),
        "S1": (67, 67, 0, 0),
        "S2": (457, 457, 0, 0),
        "S3": (457, 457, 0, 0),
    }
    run_counts = {}
    for run_id, expected in expected_counts.items():
        run = runs_final[run_id]
        trx_path = run["artifacts"]["trx"]["path"]
        names, per_class = parse_trx(R1_TIP, trx_path)
        run_names[run_id] = names
        class_outcomes[run_id] = per_class
        counts = counts_from_names(names)
        recorded = run["counters"]
        require(counts == {
            "total": recorded["total"], "passed": recorded["passed"],
            "failed": recorded["failed"], "skipped": recorded["skipped"],
        }, f"TRX summary disagrees with results.json: {run_id}")
        require((counts["total"], counts["passed"], counts["failed"], counts["skipped"])
                == expected, f"unexpected raw test counts for {run_id}: {counts}")
        run_counts[run_id] = counts
        require(out_git("rev-parse", f"{QUALIFICATION}:{EVIDENCE}/{trx_path}") ==
                out_git("rev-parse", f"{R1_TIP}:{EVIDENCE}/{trx_path}"),
                f"R1 changed raw TRX bytes: {trx_path}")

    comparisons = [("W1", "S1"), ("W2", "S2"), ("W2", "W3"),
                   ("S2", "S3"), ("W3", "S3")]
    for left, right in comparisons:
        require(run_names[left] == run_names[right],
                f"test-name/outcome multiset differs: {left}/{right}")

    broad_delta = class_outcomes["W4"] - class_outcomes["W2"]
    expected_delta = Counter({
        ("Gagamba.Execution.Linux.M1.Tests.Launch.CoordinatorProtocolQualificationTests", "Passed"): 11,
        ("Gagamba.Execution.Linux.M1.Tests.Recovery.Wp05V3IndependentQualificationTests", "Passed"): 5,
        ("Gagamba.Execution.Linux.M1.Tests.Recovery.NonRootQualificationTests", "NotExecuted"): 31,
        ("Gagamba.Execution.Linux.M1.Tests.Recovery.OwnerDeathQualificationTests", "NotExecuted"): 7,
        ("Gagamba.Execution.Linux.M1.Tests.Recovery.Wp05ReleasePathIndependentQualificationTests", "NotExecuted"): 3,
    })
    require(broad_delta == expected_delta, f"class/outcome delta mismatch: {broad_delta}")
    require(len({class_name for class_name, _ in broad_delta}) == 5,
            "broad filter did not add exactly five classes")
    require(sum(broad_delta.values()) == 57, "broad filter delta is not 57 cases")
    w4 = runs_final["W4"]
    actual_passed_classes = {
        class_name: amount for (class_name, outcome), amount in broad_delta.items()
        if outcome == "Passed"
    }
    actual_skipped_classes = {
        class_name: amount for (class_name, outcome), amount in broad_delta.items()
        if outcome == "NotExecuted"
    }
    require(w4["passedClasses"] == actual_passed_classes,
            "results.json passedClasses does not match raw TRX")
    require(w4["skippedClasses"] == actual_skipped_classes,
            "results.json skippedClasses does not match raw TRX")

    test_sources = {
        "CoordinatorProtocolQualificationTests": "tests/Gagamba.Execution.Linux.M1.Tests/Launch/CoordinatorProtocolQualificationTests.cs",
        "Wp05V3IndependentQualificationTests": "tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05V3IndependentQualificationTests.cs",
        "NonRootQualificationTests": "tests/Gagamba.Execution.Linux.M1.Tests/Recovery/NonRootQualificationTests.cs",
        "OwnerDeathQualificationTests": "tests/Gagamba.Execution.Linux.M1.Tests/Recovery/OwnerDeathQualificationTests.cs",
        "Wp05ReleasePathIndependentQualificationTests": "tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05ReleasePathIndependentQualificationTests.cs",
    }
    source_text = {name: blob(R1_TIP, path).decode("utf-8") for name, path in test_sources.items()}
    require("public sealed class CoordinatorProtocolQualificationTests" in
            source_text["CoordinatorProtocolQualificationTests"], "managed test class missing")
    require("public sealed class Wp05V3IndependentQualificationTests" in
            source_text["Wp05V3IndependentQualificationTests"], "managed test class missing")
    for name in ("NonRootQualificationTests", "OwnerDeathQualificationTests",
                 "Wp05ReleasePathIndependentQualificationTests"):
        code = source_text[name]
        require("OperatingSystem.IsLinux()" in code and
                "SupportedOSPlatform(\"linux\")" in code,
                f"{name} no longer declares Linux-only skip guard")
        require("Assert.Skip(" in code, f"{name} no longer skips unsupported hosts")
    require("Assert.Skip(\"linux only\")" in source_text["NonRootQualificationTests"],
            "NonRoot Linux-only skip guard changed")
    require("requires delegated Linux cgroup v2" in source_text["OwnerDeathQualificationTests"],
            "OwnerDeath Linux prerequisite guard changed")
    require("Assert.Skip(\"linux only\")" in
            source_text["Wp05ReleasePathIndependentQualificationTests"],
            "Wp05ReleasePath Linux-only skip guard changed")

    readme = blob(R1_TIP, f"{EVIDENCE}/README.md").decode("utf-8")
    matrix = blob(R1_TIP, f"{EVIDENCE}/qualification-matrix.md").decode("utf-8")
    handoff = blob(R1_TIP, f"{EVIDENCE}/reviewer-handoff.md").decode("utf-8")
    readme_flat = " ".join(readme.split())
    matrix_flat = " ".join(matrix.split())
    scope_decision = final_results["runDefinitions"]["scopeDecision"]
    precise_filter = final_results["runDefinitions"]["preciseQualificationFilter"]
    expected_precise_filter = (
        "FullyQualifiedName~Contracts.ManifestTests|"
        "FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|"
        "FullyQualifiedName~PreparationIdentityTests|"
        "FullyQualifiedName~Tests.Qualification."
    )
    require("five classes" in readme and "two managed-code classes" in readme.lower(),
            "README broad-filter explanation incomplete")
    require("473 passed / 0 failed / 41 skipped" in readme_flat,
            "README broad-filter aggregate incorrect")
    require("16 passed" in matrix_flat and "41 skipped" in matrix_flat,
            "qualification matrix broad-filter accounting incomplete")
    require(precise_filter == expected_precise_filter and
            precise_filter == base_results["runDefinitions"]["preciseQualificationFilter"],
            "precise filter is not the unchanged qualification selection")
    require("provenance evidence only" in scope_decision and
            "provenance only" in runs_final["W4"]["role"],
            "precise filter or broad-filter role changed")
    require("NOT RUN" in readme and "not run" in final_results["scopeAndLimitations"]["nativeLinux"].lower()
            and "not run" in final_results["scopeAndLimitations"]["macos"].lower(),
            "native Linux/macOS boundary changed")
    for field in ("nativeConfinement", "packageCleanConsumerMixedGraph", "binaryReproducibility", "hufu"):
        require(any(word in final_results["scopeAndLimitations"][field].lower()
                    for word in ("not qualified", "not claimed", "open")),
                f"unsupported scope field changed: {field}")
    require("Q2A-S" in final_results["scopeAndLimitations"]["q2asRcd04"]
            and "OPEN" in final_results["scopeAndLimitations"]["q2asRcd04"],
            "Q2A-S / RC-D04 no longer OPEN")
    handoff_flat = " ".join(handoff.split())
    require("473/0/41" in handoff_flat,
            "reviewer handoff no longer records broad-filter totals")

    plan = blob(R1_TIP, PLAN).decode("utf-8").lower()
    require("when hosts are available" in plan and "recorded as \"not run\"" in plan,
            "qualification plan availability rule changed")
    require("linux/macos runs require real native hosts" in plan,
            "qualification plan no longer defines native host availability")

    r1_verifier_path = "docs/reviews/gagamba-rc2a-02b-r1/verify_evidence.py"
    r1_verifier = blob(R1_TIP, r1_verifier_path).decode("utf-8")
    verifier_review = {
        "sha256": sha256(blob(R1_TIP, r1_verifier_path)),
        "computesGitBlobHashes": 'hashlib.sha256' in r1_verifier and 'git("show"' in r1_verifier,
        "hardCodesSpecificCommitsAndBranch": all(x in r1_verifier for x in
            (QUALIFICATION, R1_FIX, Q1_BRANCH, R1_BRANCH)),
        "checksJsonPerClassMapsAgainstRawTrx": 'passedClasses"] ==' in r1_verifier or
            'skippedClasses"] ==' in r1_verifier,
        "checksDocumentCountButNotExactPathSet": 'len(documents) == 9' in r1_verifier and
            'base_docs' not in r1_verifier,
        "usesSimpleMarkdownLinkRegex": r'r"\]\(([^)]+)\)"' in r1_verifier,
        "invokesItself": bool(re.search(
            r"(?:python|sys\.executable)\s+[^\n]*verify_evidence\.py", r1_verifier
        )),
    }
    require(verifier_review["computesGitBlobHashes"],
            "R1 verifier no longer computes Git-blob hashes")
    require(not verifier_review["checksJsonPerClassMapsAgainstRawTrx"],
            "R1 verifier class-map coverage assessment changed")

    r1_names = set(run_git("diff", "--name-only", QUALIFICATION, R1_TIP).stdout.decode().splitlines())
    expected_r1_names = {
        f"{EVIDENCE}/README.md",
        f"{EVIDENCE}/qualification-matrix.md",
        f"{EVIDENCE}/results.json",
        f"{EVIDENCE}/document-sha256.json",
        f"{EVIDENCE}/SHA256SUMS",
        "docs/reviews/gagamba-rc2a-02b-r1/report.md",
        "docs/reviews/gagamba-rc2a-02b-r1/verify_evidence.py",
    }
    require(r1_names == expected_r1_names, f"unexpected R1 diff: {sorted(r1_names)}")
    changed_to_r1 = run_git(
        "diff", "--quiet", QUALIFICATION, R1_TIP, "--",
        "src", "tests", "native", f"{EVIDENCE}/runs", check=False
    )
    require(changed_to_r1.returncode == 0, "R1 changed source, tests, native, or raw artifacts")

    q2_changed = set(run_git("diff", "--name-only", R1_TIP, "HEAD").stdout.decode().splitlines())
    expected_q2_changed = {Q2_REPORT, Q2_VERIFIER}
    require(q2_changed == expected_q2_changed,
            f"unexpected Q2 review diff: {sorted(q2_changed)}")
    changed_to_q2 = run_git(
        "diff", "--quiet", R1_TIP, "HEAD", "--",
        "src", "tests", "native", f"{EVIDENCE}/runs", check=False
    )
    require(changed_to_q2.returncode == 0, "Q2 review changed qualified source/raw evidence")

    markdown_links = []
    relevant_paths = [
        f"{EVIDENCE}/README.md",
        f"{EVIDENCE}/qualification-matrix.md",
        f"{EVIDENCE}/reviewer-handoff.md",
        "docs/reviews/gagamba-rc2a-02b-r1/report.md",
    ]
    markdown_links.extend(check_links(R1_TIP, relevant_paths))
    markdown_links.extend(check_links(Q1_REVIEW, [Q1_REPORT]))
    markdown_links.extend(check_links("HEAD", [Q2_REPORT]))

    branch_worktrees = {}
    for branch in (QUALIFICATION_BRANCH, Q1_BRANCH, R1_BRANCH, Q2_BRANCH):
        path = worktree_for_branch(branch)
        status = run_git("-C", path, "status", "--porcelain").stdout.decode().strip()
        require(not status, f"{branch} worktree is dirty: {status}")
        branch_worktrees[branch] = {"path": path, "clean": True}
    require(run_git("status", "--porcelain").stdout.decode().strip() == "",
            "Q2 worktree is dirty")

    result = {
        "verdict": "VERIFIED WITH QUALIFICATIONS",
        "findingDispositions": {
            "R1-01": "resolved: 14 corrected; all 29 independently computed hashes match; raw artifacts unchanged",
            "R1-02": "resolved: five additional classes are two managed pass classes (16) and three Linux-only skip classes (41); 473/0/41",
        },
        "git": {
            "identities": identity_report,
            "qualificationBranch": out_git("rev-parse", f"refs/heads/{QUALIFICATION_BRANCH}"),
            "q1Branch": out_git("rev-parse", f"refs/heads/{Q1_BRANCH}"),
            "r1Branch": out_git("rev-parse", f"refs/heads/{R1_BRANCH}"),
            "q2Head": current_head,
            "q2Branch": Q2_BRANCH,
            "q1Merged": False,
            "worktrees": branch_worktrees,
        },
        "artifactHashes": {
            "referenceCount": len(result_refs_final),
            "correctionCount": len(corrected),
            "allReferencesMatchGitBlobs": True,
            "corrected": corrected,
        },
        "integrityManifests": {
            "sha256SumsEntries": len(final_sums),
            "sha256SumsAllMatch": True,
            "documentBindings": len(final_docs),
            "documentBindingsAllMatch": True,
        },
        "rawQualification": {
            "artifactBlobsUnchanged": True,
            "recordedCountsUnchanged": True,
            "runCounts": run_counts,
            "crossPlatformAndRerunComparisons": comparisons,
            "broadFilterDelta": {
                f"{name}|{outcome}": amount
                for (name, outcome), amount in broad_delta.items()
            },
        },
        "sourceProvenance": {
            "startCommit": OWNER,
            "acceptedQualificationEvidence": ACCEPTED_02A_EVIDENCE,
            "testedImplementation": TESTED_IMPLEMENTATION,
            "subtrees": expected_trees,
            "match": True,
        },
        "r1VerifierInspection": verifier_review,
        "scope": {
            "r1ChangedPaths": sorted(r1_names),
            "q2ChangedPaths": sorted(q2_changed),
            "productionTestNativeRawChanges": False,
            "nativeLinux": "NOT RUN; plan permits recording unavailable hosts",
            "macOS": "NOT RUN; no result inferred",
            "Q2A-S/RC-D04": "OPEN",
            "relativeLinksResolved": len(markdown_links),
        },
    }
    print(json.dumps(result, indent=2, sort_keys=True))


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        raise
