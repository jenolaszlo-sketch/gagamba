#!/usr/bin/env python3
"""Verify the committed RC2A-02B-R1 evidence remediation."""

from __future__ import annotations

import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path, PurePosixPath
from urllib.parse import unquote, urlsplit


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE = "docs/evidence/restricted-consumer-contract-rc2a-02b-portability"
REPORT = "docs/reviews/gagamba-rc2a-02b-r1/report.md"
VERIFIER = "docs/reviews/gagamba-rc2a-02b-r1/verify_evidence.py"
QUALIFICATION = "dd16baec48663d40cd29504d98e57e30dd0855df"
OWNER_ACCEPTANCE = "e5fff9e8d5a31520f8b5a2c235a05b666a08e16c"
EXECUTION = "5b4292d0fefc576664df61692dfad5cef6362879"
Q1_REVIEW = "5bc396c2e5619906ae202afe0a4244dfd6024d9e"
Q1_TREE = "8ebbccb7b3787ce8e57bc74bbc93a8c1edb9e650"
REMEDIATION_COMMIT = "12d914d319b1df3eedafc5439bc547dacb578651"
BRANCH = "remediation/gagamba-rc2a-02b-r1"


def git(*args: str, check: bool = True) -> bytes:
    result = subprocess.run(
        ["git", *args], cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.PIPE
    )
    if check and result.returncode:
        raise RuntimeError(result.stderr.decode("utf-8", "replace").strip())
    return result.stdout


def blob(revision: str, path: str) -> bytes:
    return git("show", f"{revision}:{path}")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def collect_artifact_refs(value: object, refs: dict[str, str]) -> None:
    if isinstance(value, dict):
        path = value.get("path", value.get("file"))
        sha = value.get("sha256")
        if isinstance(path, str) and isinstance(sha, str):
            require(path not in refs, f"duplicate results.json artifact path: {path}")
            refs[path] = sha
        for child in value.values():
            collect_artifact_refs(child, refs)
    elif isinstance(value, list):
        for child in value:
            collect_artifact_refs(child, refs)


def parse_trx(data: bytes) -> tuple[Counter[tuple[str, str]], Counter[str]]:
    root = ET.fromstring(data)
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    names: Counter[tuple[str, str]] = Counter()
    classes: Counter[str] = Counter()
    for result in root.findall(".//t:UnitTestResult", ns):
        name = result.attrib.get("testName", "")
        outcome = result.attrib.get("outcome", "")
        require(bool(name) and bool(outcome), "TRX result lacks a test name or outcome")
        names[(name, outcome)] += 1
        class_name = name.rsplit(".", 1)[0] if "." in name else name
        classes[f"{class_name}|{outcome}"] += 1
    return names, classes


def local_markdown_targets(path: str, markdown: str) -> list[str]:
    targets: list[str] = []
    patterns = [r"\]\(([^)]+)\)", r"^\s*\[[^\]]+\]:\s*(\S+)"]
    for pattern in patterns:
        for match in re.finditer(pattern, markdown, re.MULTILINE):
            target = match.group(1).strip()
            if target.startswith("<") and ">" in target:
                target = target[1 : target.index(">")]
            else:
                target = target.split()[0]
            parsed = urlsplit(target)
            if parsed.scheme or parsed.netloc or not parsed.path:
                continue
            raw_path = unquote(parsed.path)
            if raw_path.startswith("/"):
                resolved = PurePosixPath(raw_path.lstrip("/"))
            else:
                resolved = PurePosixPath(path).parent / raw_path
            targets.append(str(resolved))
    return targets


def main() -> None:
    head = git("rev-parse", "HEAD").decode().strip()
    branch = git("branch", "--show-current").decode().strip()
    require(branch == BRANCH, f"expected branch {BRANCH}, found {branch}")
    require(
        git("rev-parse", f"{QUALIFICATION}^{{tree}}").decode().strip()
        == "96e86aac2d284637805809b54da72e76bf46f1fc",
        "qualification commit tree identity changed",
    )
    require(
        git("rev-parse", f"{OWNER_ACCEPTANCE}^{{tree}}").decode().strip()
        == "8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e",
        "owner-acceptance tree identity changed",
    )
    require(git("rev-parse", f"{QUALIFICATION}^").decode().strip() == EXECUTION,
            "qualification commit parent changed")
    require(git("rev-parse", f"{EXECUTION}^").decode().strip() == OWNER_ACCEPTANCE,
            "execution commit parent changed")
    require(
        git("rev-parse", "refs/heads/review/gagamba-rc2a-02b-q1").decode().strip()
        == Q1_REVIEW,
        "Q1 review branch moved",
    )
    require(git("rev-parse", f"{Q1_REVIEW}^{{tree}}").decode().strip() == Q1_TREE,
            "Q1 review tree changed")
    require(git("rev-parse", f"{Q1_REVIEW}^").decode().strip() == QUALIFICATION,
            "Q1 review no longer has the qualification commit as its parent")
    require(git("rev-parse", f"{QUALIFICATION}^{{tree}}").decode().strip()
            != git("rev-parse", f"{Q1_REVIEW}^{{tree}}").decode().strip(),
            "review and remediation histories unexpectedly share a tree")

    changed_paths = set(git("diff", "--name-only", QUALIFICATION, "HEAD").decode().splitlines())
    expected_paths = {
        f"{EVIDENCE}/README.md",
        f"{EVIDENCE}/qualification-matrix.md",
        f"{EVIDENCE}/results.json",
        f"{EVIDENCE}/document-sha256.json",
        f"{EVIDENCE}/SHA256SUMS",
        REPORT,
        VERIFIER,
    }
    require(changed_paths == expected_paths,
            f"unexpected changed-file inventory: {sorted(changed_paths)}")
    raw_and_source = subprocess.run(
        ["git", "diff", "--quiet", QUALIFICATION, "HEAD", "--",
         "src", "tests", "native", f"{EVIDENCE}/runs"],
        cwd=ROOT,
    )
    require(raw_and_source.returncode == 0,
            "source/test/native/raw qualification bytes changed")
    require(git("rev-parse", "HEAD^").decode().strip() == REMEDIATION_COMMIT,
            "verification report commit does not follow the remediation commit")
    require(git("status", "--porcelain").decode().strip() == "",
            "worktree is not clean")

    results_path = f"{EVIDENCE}/results.json"
    results = json.loads(blob("HEAD", results_path))
    base_results = json.loads(blob(QUALIFICATION, results_path))
    require(results["baseline"]["startCommit"] == OWNER_ACCEPTANCE,
            "results.json start baseline changed")
    require(results["baseline"]["startTree"] == "8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e",
            "results.json start tree changed")
    limitations = results["scopeAndLimitations"]
    require("OPEN" in limitations["q2asRcd04"], "Q2A-S / RC-D04 is not recorded OPEN")
    require("not run" in limitations["nativeLinux"].lower(),
            "native Linux run boundary changed")
    require("not run" in limitations["macos"].lower(), "macOS run boundary changed")
    require("not qualified" in limitations["nativeConfinement"].lower(),
            "native confinement claim changed")
    require("not qualified" in limitations["packageCleanConsumerMixedGraph"].lower(),
            "package/clean-consumer claim changed")
    require("not claimed" in limitations["binaryReproducibility"].lower(),
            "binary reproducibility claim changed")
    require("OPEN" in limitations["hufu"], "Hufu obligation changed")
    require("NOT YET RECORDED" in limitations["ownerAcceptance"],
            "owner acceptance boundary changed")
    require("NOT YET ACCEPTED" in limitations["ownerAcceptance"],
            "full RC2A-02 acceptance boundary changed")
    refs: dict[str, str] = {}
    base_refs: dict[str, str] = {}
    collect_artifact_refs(results, refs)
    collect_artifact_refs(base_results, base_refs)
    require(len(refs) == 29, f"expected 29 results.json artifact refs, found {len(refs)}")
    require(refs.keys() == base_refs.keys(), "results.json artifact path set changed")
    changed_hash_paths = {p for p in refs if refs[p] != base_refs[p]}
    require(len(changed_hash_paths) == 14,
            f"expected 14 corrected artifact digests, found {len(changed_hash_paths)}")
    require(all(p.startswith("runs/windows/") for p in changed_hash_paths),
            "a non-Windows evidence digest changed")
    for path, expected in refs.items():
        actual = digest(blob("HEAD", f"{EVIDENCE}/{path}"))
        require(actual == expected, f"results.json digest mismatch: {path}")
    print(f"PASS results.json: 29/29 artifact refs; 14 corrected, 15 retained")

    sums_path = f"{EVIDENCE}/SHA256SUMS"
    sum_lines = blob("HEAD", sums_path).decode("utf-8").splitlines()
    sums: dict[str, str] = {}
    for line in sum_lines:
        sha, path = line.split("  ", 1)
        require(path not in sums, f"duplicate SHA256SUMS path: {path}")
        sums[path] = sha
    require(len(sums) == 43, f"expected 43 SHA256SUMS entries, found {len(sums)}")
    for path, expected in sums.items():
        actual = digest(blob("HEAD", f"{EVIDENCE}/{path}"))
        require(actual == expected, f"SHA256SUMS mismatch: {path}")
    print("PASS SHA256SUMS: 43/43 entries")

    doc_manifest = json.loads(blob("HEAD", f"{EVIDENCE}/document-sha256.json"))
    documents = doc_manifest["files"]
    require(len(documents) == 9, f"expected 9 document bindings, found {len(documents)}")
    for item in documents:
        raw = blob("HEAD", item["path"])
        normalized = raw.replace(b"\r\n", b"\n")
        require(len(normalized) == item["bytes"], f"document byte count mismatch: {item['path']}")
        require(digest(normalized) == item["sha256"],
                f"document digest mismatch: {item['path']}")
    print("PASS document-sha256.json: 9/9 LF-normalized bindings")

    runs = {run["id"]: run for run in results["runs"]}
    run_names: dict[str, Counter[tuple[str, str]]] = {}
    for run_id in ("W1", "W2", "W3", "W4", "S1", "S2", "S3"):
        run = runs[run_id]
        trx_path = run["artifacts"]["trx"]["path"]
        names, classes = parse_trx(blob("HEAD", f"{EVIDENCE}/{trx_path}"))
        run_names[run_id] = names
        count = Counter()
        for (_, outcome), n in names.items():
            count[outcome] += n
        counters = run["counters"]
        actual_skips = count.get("NotExecuted", 0) + count.get("Skipped", 0)
        require(sum(count.values()) == counters["total"], f"TRX total mismatch: {run_id}")
        require(count.get("Passed", 0) == counters["passed"], f"TRX passed mismatch: {run_id}")
        require(count.get("Failed", 0) == counters["failed"], f"TRX failed mismatch: {run_id}")
        require(actual_skips == counters["skipped"], f"TRX skipped mismatch: {run_id}")

    expected_counts = {
        "W1": (67, 67, 0, 0), "W2": (457, 457, 0, 0),
        "W3": (457, 457, 0, 0), "W4": (514, 473, 0, 41),
        "S1": (67, 67, 0, 0), "S2": (457, 457, 0, 0),
        "S3": (457, 457, 0, 0),
    }
    for run_id, expected in expected_counts.items():
        actual = runs[run_id]["counters"]
        found = (actual["total"], actual["passed"], actual["failed"], actual["skipped"])
        require(found == expected, f"qualification count changed for {run_id}: {found}")
    for left, right in (("W1", "S1"), ("W2", "S2"), ("W2", "W3"),
                        ("S2", "S3"), ("W3", "S3")):
        require(run_names[left] == run_names[right],
                f"test-name/outcome multiset differs: {left} vs {right}")

    broad = runs["W4"]["artifacts"]["trx"]["path"]
    precise = runs["W2"]["artifacts"]["trx"]["path"]
    broad_names, broad_classes = parse_trx(blob("HEAD", f"{EVIDENCE}/{broad}"))
    precise_names, _ = parse_trx(blob("HEAD", f"{EVIDENCE}/{precise}"))
    require(all(broad_names[key] >= n for key, n in precise_names.items()),
            "broad filter result does not contain the precise set")
    additional = broad_names - precise_names
    expected_additional = {
        "Gagamba.Execution.Linux.M1.Tests.Launch.CoordinatorProtocolQualificationTests|Passed": 11,
        "Gagamba.Execution.Linux.M1.Tests.Recovery.Wp05V3IndependentQualificationTests|Passed": 5,
        "Gagamba.Execution.Linux.M1.Tests.Recovery.NonRootQualificationTests|NotExecuted": 31,
        "Gagamba.Execution.Linux.M1.Tests.Recovery.OwnerDeathQualificationTests|NotExecuted": 7,
        "Gagamba.Execution.Linux.M1.Tests.Recovery.Wp05ReleasePathIndependentQualificationTests|NotExecuted": 3,
    }
    actual_additional = Counter()
    for key, n in additional.items():
        class_name, outcome = key[0].rsplit(".", 1)[0], key[1]
        actual_additional[f"{class_name}|{outcome}"] += n
    require(actual_additional == Counter(expected_additional),
            f"broad filter class accounting mismatch: {actual_additional}")
    require(sum(additional.values()) == 57, "broad filter added-case total changed")
    require(results["runDefinitions"]["preciseQualificationFilter"].endswith(
        "FullyQualifiedName~Tests.Qualification."), "precise filter definition changed")
    require("provenance" in runs["W4"]["role"], "broad filter is not labeled provenance")
    print("PASS raw TRX counts/cross-platform multisets: Win 67/457/457/473-0-41; WSL2 67/457/457")
    print("PASS broad-filter extras: 2 managed classes / 16 passed; 3 Linux-native classes / 41 skipped")

    binding = json.loads(blob("HEAD", f"{EVIDENCE}/source-binding.json"))
    source_trees = binding["subtreeIdentities"]
    for path in ("src", "tests", "native"):
        expected = source_trees[path]
        require(git("rev-parse", f"{OWNER_ACCEPTANCE}:{path}").decode().strip() == expected,
                f"owner-acceptance {path} tree mismatch")
        require(git("rev-parse", f"{QUALIFICATION}:{path}").decode().strip() == expected,
                f"qualification {path} tree mismatch")
    require(binding["start"]["commit"] == OWNER_ACCEPTANCE,
            "source binding baseline commit mismatch")
    require(binding["executedSource"]["windows-native"]["commit"] == OWNER_ACCEPTANCE,
            "Windows executed-source binding mismatch")
    require(binding["executedSource"]["wsl2"]["commit"] == OWNER_ACCEPTANCE,
            "WSL2 executed-source binding mismatch")
    print("PASS source provenance: baseline/tree chain and src/tests/native subtree identities")

    markdown_paths = [
        p for p in git("ls-tree", "-r", "--name-only", "HEAD", EVIDENCE)
        .decode().splitlines() if p.endswith(".md")
    ]
    markdown_paths.append(REPORT)
    checked_links = 0
    for path in markdown_paths:
        text = blob("HEAD", path).decode("utf-8")
        for target in local_markdown_targets(path, text):
            require((ROOT / Path(*PurePosixPath(target).parts)).exists(),
                    f"relative Markdown link does not resolve: {path} -> {target}")
            checked_links += 1
    print(f"PASS relative evidence links: {checked_links} targets resolved")

    print(f"PASS scope and clean worktree: {len(changed_paths)} documentation/evidence files; HEAD {head}")


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:  # print a concise failing check for independent reviewers
        print(f"FAIL: {exc}", file=sys.stderr)
        raise
