#!/usr/bin/env python3
"""Fail-closed gate for promoting one exact Gagamba package candidate.

Qualification evidence is an external CI artifact, never a manifest copied
forward inside a later source commit. No network or publishing occurs here.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

SCHEMA = "gagamba-release-qualification/v1"
IDS = (
    "Gagamba.Execution",
    "Gagamba.Execution.Windows",
    "Gagamba.Execution.Linux",
    "Gagamba.Execution.MacOS",
    "Gagamba.Runtime",
)
BASE_LEGS = {
    "capability-matrix", "opaque-handles", "prepare", "single-use",
    "working-directory", "no-ambient-inherit", "unit-termination",
    "root-exit", "dispose-cleanup", "completion",
}
PLATFORMS = {
    "windows": ("windows-job", "job-object"),
    "linux": ("linux-cgroup-v2", "cgroup-v2"),
    "macos": ("macos-launchd-pg", "launchd-process-group"),
}


class QualificationError(ValueError):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise QualificationError(message)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def package_metadata(path: Path) -> tuple[str, str, str | None, dict[str, str]]:
    try:
        with zipfile.ZipFile(path) as archive:
            names = [name for name in archive.namelist() if name.endswith(".nuspec")]
            require(len(names) == 1, f"{path.name}: exactly one nuspec required")
            root = ET.fromstring(archive.read(names[0]))
    except (OSError, zipfile.BadZipFile, ET.ParseError) as exc:
        raise QualificationError(f"{path.name}: invalid NuGet archive: {type(exc).__name__}") from exc
    metadata = root.find("{*}metadata")
    require(metadata is not None, f"{path.name}: missing metadata")
    def value(tag: str) -> str:
        node = metadata.find("{*}" + tag)
        return (node.text or "") if node is not None else ""
    repository = metadata.find("{*}repository")
    commit = repository.get("commit") if repository is not None else None
    dependencies: dict[str, str] = {}
    for item in metadata.findall(".//{*}dependency"):
        dependencies[item.get("id", "")] = item.get("version", "")
    return value("id"), value("version"), commit, dependencies


def validate(manifest: dict, source_sha: str, version: str, packages_dir: Path,
             repo: Path | None = None) -> None:
    require(re.fullmatch(r"[0-9a-f]{40}", source_sha) is not None, "invalid requested source SHA")
    require(re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?", version) is not None,
            "invalid requested package version")
    require(manifest.get("schema") == SCHEMA, "qualification schema missing or wrong")
    require(manifest.get("sourceSha") == source_sha, "stale or wrong source SHA")
    require(manifest.get("sourceTreeClean") is True, "qualified source tree was dirty")
    require(manifest.get("version") == version, "wrong package version")
    if repo is not None:
        try:
            head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=repo, text=True).strip()
            dirty = subprocess.check_output(["git", "status", "--porcelain", "--untracked-files=all"], cwd=repo, text=True).strip()
        except (OSError, subprocess.CalledProcessError) as exc:
            raise QualificationError("checkout source identity unavailable") from exc
        require(head == source_sha, "checkout HEAD differs from qualified source")
        require(not dirty, "checkout is dirty")

    rows = manifest.get("packages")
    require(isinstance(rows, list) and len(rows) == len(IDS), "exact five-package inventory required")
    require({row.get("id") for row in rows if isinstance(row, dict)} == set(IDS),
            "missing, duplicate or unexpected package ID")
    require(len({row.get("id") for row in rows}) == len(IDS), "duplicate package ID")
    expected_names: set[str] = set()
    package_hashes: dict[str, str] = {}
    for row in rows:
        package_id = row["id"]
        filename = f"{package_id}.{version}.nupkg"
        expected_names.add(filename)
        require(row.get("file") == filename and row.get("version") == version,
                f"{package_id}: filename/version mismatch")
        path = packages_dir / filename
        require(path.is_file(), f"{package_id}: package candidate absent")
        actual_hash = sha256(path)
        require(row.get("sha256") == actual_hash, f"{package_id}: package SHA-256 mismatch")
        contained_id, contained_version, commit, dependencies = package_metadata(path)
        require((contained_id, contained_version) == (package_id, version),
                f"{package_id}: nuspec identity/version mismatch")
        require(commit == source_sha, f"{package_id}: nuspec source commit missing or stale")
        for dep_id, dep_version in dependencies.items():
            if dep_id in IDS:
                require(dep_version == version, f"{package_id}: internal dependency version drift")
        package_hashes[package_id] = actual_hash
        symbol_name = f"{package_id}.{version}.snupkg"
        symbol_path = packages_dir / symbol_name
        require(symbol_path.is_file() and row.get("symbolSha256") == sha256(symbol_path),
                f"{package_id}: symbol package absent or SHA-256 mismatch")
    actual_names = {p.name for p in packages_dir.glob("*.nupkg")}
    require(actual_names == expected_names, "package directory has missing or extra candidates")
    require({p.name for p in packages_dir.glob("*.snupkg")} ==
            {f"{package_id}.{version}.snupkg" for package_id in IDS},
            "symbol package directory has missing or extra candidates")

    hosts = manifest.get("hosts")
    require(isinstance(hosts, dict) and set(hosts) == set(PLATFORMS),
            "Windows, Linux and macOS qualification required")
    run_ids: set[str] = set()
    for key, (platform, mechanism) in PLATFORMS.items():
        host = hosts[key]
        require(isinstance(host, dict), f"{key}: host evidence invalid")
        require(host.get("sourceSha") == source_sha, f"{key}: stale host source SHA")
        require(host.get("platform") == platform, f"{key}: wrong provider platform")
        require(host.get("usable") is True, f"{key}: provider was not usable")
        require(host.get("nativeMechanism") == mechanism and host.get("native") is True,
                f"{key}: native mechanism unqualified")
        require(host.get("cleanup") == "Confirmed", f"{key}: cleanup not confirmed")
        require(all(isinstance(host.get(field), str) and host[field]
                    for field in ("os", "architecture", "runtime", "runId")),
                f"{key}: host identity incomplete")
        os_name = host["os"].lower()
        require((key == "windows" and "windows" in os_name)
                or (key == "linux" and ("linux" in os_name or "ubuntu" in os_name))
                or (key == "macos" and ("mac" in os_name or "darwin" in os_name)),
                f"{key}: host OS identity mismatch")
        require(host["runId"] not in run_ids, f"{key}: reused conformance run ID")
        run_ids.add(host["runId"])
        if key == "linux":
            require(host.get("cgroupVersion") == 2 and host.get("delegated") is True,
                    "linux: cgroup v2 delegation not qualified")
        if key == "macos":
            require(bool(host.get("launchdDomain")), "macos: launchd domain missing")
        legs = host.get("legs")
        require(isinstance(legs, dict), f"{key}: test legs missing")
        required = BASE_LEGS | ({"setsid-escape"} if key != "windows" else set())
        require(required <= set(legs), f"{key}: mandatory test leg absent")
        require(all(legs[name] == "Passed" for name in required),
                f"{key}: mandatory leg skipped, unsupported or failed")
        if key == "windows":
            require(legs.get("setsid-escape") == "Skipped",
                    "windows: setsid non-applicability not explicit")
        require(host.get("skippedMandatory") == 0 and host.get("unsupportedMandatory") == 0,
                f"{key}: mandatory skip/unsupported count nonzero")

    consumers = manifest.get("installedConsumers")
    require(isinstance(consumers, dict) and set(consumers) == set(PLATFORMS),
            "installed-package consumers missing")
    for key in PLATFORMS:
        consumer = consumers[key]
        require(isinstance(consumer, dict), f"{key}: installed consumer evidence invalid")
        require(consumer.get("sourceSha") == source_sha and consumer.get("version") == version,
                f"{key}: installed consumer source/version mismatch")
        require(consumer.get("platform") == key, f"{key}: installed consumer ran on wrong host")
        require(consumer.get("result") == "Passed" and consumer.get("lifecycle") == "Passed",
                f"{key}: installed consumer failed or lifecycle not run")
        require(consumer.get("packageSha256") == package_hashes,
                f"{key}: installed consumer used different packages")


def seal(source_sha: str, version: str, packages_dir: Path,
         evidence_dir: Path, destination: Path, repo: Path | None = None) -> None:
    hosts: dict[str, dict] = {}
    consumers: dict[str, dict] = {}
    for key, (platform, mechanism) in PLATFORMS.items():
        path = evidence_dir / f"conformance-{key}.json"
        consumer_path = evidence_dir / f"consumer-{key}.json"
        require(path.is_file() and consumer_path.is_file(), f"{key}: evidence artifact absent")
        report = json.loads(path.read_text(encoding="utf-8"))
        consumer = json.loads(consumer_path.read_text(encoding="utf-8"))
        require(report.get("schema") == "gagamba-conformance/v1", f"{key}: conformance schema wrong")
        legs = report.get("legs")
        require(isinstance(legs, list), f"{key}: conformance legs invalid")
        outcomes = {row["Name"]: row["outcome"] for row in legs}
        require(len(outcomes) == len(legs), f"{key}: duplicate conformance leg")
        hosts[key] = {
            "sourceSha": report.get("sourceSha"), "platform": report.get("platform"),
            "nativeMechanism": mechanism, "native": True,
            "usable": report.get("usable"),
            "cleanup": report.get("cleanup", "Unknown"),
            "os": report.get("host"), "architecture": report.get("architecture"),
            "runtime": report.get("runtime"), "runId": report.get("runId"),
            "legs": outcomes,
            "skippedMandatory": report.get("skippedMandatory"),
            "unsupportedMandatory": report.get("unsupportedMandatory"),
        }
        if key == "linux":
            hosts[key].update(cgroupVersion=report.get("cgroupVersion"),
                              delegated=report.get("delegated"))
        if key == "macos":
            hosts[key]["launchdDomain"] = report.get("launchdDomain")
        consumers[key] = consumer
    packages = [{"id": package_id, "version": version,
                 "file": f"{package_id}.{version}.nupkg",
                 "sha256": sha256(packages_dir / f"{package_id}.{version}.nupkg"),
                 "symbolSha256": sha256(packages_dir / f"{package_id}.{version}.snupkg")}
                for package_id in IDS]
    manifest = {"schema": SCHEMA, "sourceSha": source_sha, "sourceTreeClean": True,
                "version": version, "packages": packages, "hosts": hosts,
                "installedConsumers": consumers}
    validate(manifest, source_sha, version, packages_dir, repo)
    destination.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    verify = sub.add_parser("verify")
    verify.add_argument("--manifest", type=Path, required=True)
    verify.add_argument("--source-sha", required=True)
    verify.add_argument("--version", required=True)
    verify.add_argument("--packages-dir", type=Path, required=True)
    verify.add_argument("--repo", type=Path)
    make = sub.add_parser("seal")
    make.add_argument("--source-sha", required=True)
    make.add_argument("--version", required=True)
    make.add_argument("--packages-dir", type=Path, required=True)
    make.add_argument("--evidence-dir", type=Path, required=True)
    make.add_argument("--output", type=Path, required=True)
    make.add_argument("--repo", type=Path)
    args = parser.parse_args()
    try:
        if args.command == "verify":
            require(args.manifest.is_file(), "qualification manifest absent")
            manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
            validate(manifest, args.source_sha, args.version, args.packages_dir, args.repo)
        else:
            seal(args.source_sha, args.version, args.packages_dir,
                 args.evidence_dir, args.output, args.repo)
    except (QualificationError, OSError, json.JSONDecodeError) as exc:
        print(f"QUALIFICATION REJECTED: {exc}", file=sys.stderr)
        return 1
    print("Qualification evidence and exact package candidate accepted")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
