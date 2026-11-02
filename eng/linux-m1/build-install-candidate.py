#!/usr/bin/env python3
"""Create the S2 canonical manifest for a privately built candidate."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import re
import shutil
import subprocess
import sys

PROFILE_ID = "ae4-linux-fixed-fixture-v1"
COORDINATOR_SHA256 = "5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b"
FILTER_SHA256 = "2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108"
RUNTIME_VERSION = "10.0.14"
GLIBC_MIN = "2.43"
REQUIRED_FEATURES = ["cgroup2", "openat2", "pidfd", "SO_PEERPIDFD", "subreaper", "userns", "seccomp"]
AUTHORITY_PATHS = [
    "src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs",
    "src/Gagamba.Execution.Linux.M1/Lifecycle/HeldReleasePhase.cs",
    "src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs",
    "src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs",
]
ARGS_PATH = "src/Gagamba.Execution.Linux.M1/Launch/BubblewrapArguments.cs"
RESOURCES_PATH = "src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs"


def run(*args: str) -> str:
    return subprocess.check_output(args, text=True, stderr=subprocess.STDOUT).strip()


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def file_digest(path: Path) -> str:
    return digest(path.read_bytes())


def inventory(directory: Path) -> str:
    # Byte-for-byte equivalent to ExperimentalRuntimePin.Inventory: sorted
    # relative path + NUL + raw SHA256(file), then SHA256 of the concatenation.
    files = sorted((p for p in directory.rglob("*") if p.is_file()), key=lambda p: p.relative_to(directory).as_posix())
    if not files or len(files) > 2048:
        raise SystemExit(f"runtime inventory count refused: {directory}: {len(files)}")
    h = hashlib.sha256()
    for path in files:
        h.update(path.relative_to(directory).as_posix().encode("utf-8"))
        h.update(b"\0")
        h.update(hashlib.sha256(path.read_bytes()).digest())
    return h.hexdigest()


def projection(source: Path, paths: list[str]) -> str:
    h = hashlib.sha256()
    for relative in sorted(paths):
        h.update(relative.encode("ascii"))
        h.update(b"\0")
        h.update((source / relative).read_bytes())
        h.update(b"\0")
    return h.hexdigest()


def resource_projection(source: Path) -> str:
    data = (source / RESOURCES_PATH).read_bytes()
    start = b"private static readonly ResourceManifest FixedResources = new("
    end = b"    private static void VerifyStaged"
    a = data.index(start)
    b = data.index(end, a)
    return digest(data[a:b])


def os_release() -> str:
    values: dict[str, str] = {}
    for line in Path("/etc/os-release").read_text(encoding="utf-8").splitlines():
        if "=" in line:
            key, value = line.split("=", 1)
            values[key] = value.strip().strip('"')
    return f"{values.get('NAME', 'unknown')} {values.get('VERSION_ID', 'unknown')}"


def find_runtime_dir(kind: str, version: str) -> Path:
    output = run("dotnet", "--list-runtimes")
    for line in output.splitlines():
        match = re.fullmatch(rf"{re.escape(kind)} {re.escape(version)} \[(.+)\]", line.strip())
        if match:
            return Path(match.group(1)) / version
    raise SystemExit(f"exact runtime not installed: {kind} {version}")


def artifact(candidate: Path, role: str, relative: str, kind: str, consumer: str, survives: bool) -> dict:
    path = candidate / relative
    length = path.stat().st_size
    return {
        "role": role,
        "kind": kind,
        "path": relative,
        "sha256": file_digest(path),
        "length": length,
        "mode": "0555" if relative == "native/exec-coordinator" else "0444",
        "uid": 0,
        "gid": 0,
        "nlink": 1,
        "rid": "linux-x64",
        "buildIdentity": "Production",
        "required": True,
        "consumer": consumer,
        "survivesController": survives,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--source-commit", required=True)
    parser.add_argument("--source-tree", required=True)
    parser.add_argument("--staged-source", required=True, type=Path)
    parser.add_argument("--candidate", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    source = args.staged_source
    candidate = args.candidate
    output = args.output / "candidate"
    digest_output = args.output / "artifact-set.sha256"
    if platform.system() != "Linux" or platform.machine() != "x86_64" or "microsoft-standard-wsl2" not in platform.release().lower():
        raise SystemExit("candidate profile requires Linux x86_64")
    if run("dotnet", "--version") != "10.0.114":
        raise SystemExit("candidate build requires the pinned .NET SDK 10.0.114")
    if run("dotnet", "--list-runtimes").find(f"Microsoft.NETCore.App {RUNTIME_VERSION} ") < 0:
        raise SystemExit(f"candidate platform requires .NET runtime {RUNTIME_VERSION}")
    if run("gcc", "--version").splitlines()[0].find("15.2.0") < 0:
        raise SystemExit("candidate build requires the pinned GCC 15.2.0 toolchain")
    glibc = run("getconf", "GNU_LIBC_VERSION").split()[-1]
    if tuple(map(int, glibc.split(".")[:2])) < tuple(map(int, GLIBC_MIN.split(".")[:2])):
        raise SystemExit(f"glibc below required baseline {GLIBC_MIN}: {glibc}")
    if not args.source_commit or len(args.source_commit) != 40 or len(args.source_tree) != 40:
        raise SystemExit("source commit/tree must be full Git identities")

    for component in ("owner", "recovery", "provider"):
        directory = candidate / component
        if not directory.is_dir():
            raise SystemExit(f"candidate component missing: {component}")
    native = candidate / "native"
    if file_digest(native / "exec-coordinator") != COORDINATOR_SHA256:
        raise SystemExit("coordinator bytes differ from the reviewed WSL2 GCC pin")
    filter_bytes = (native / "seccomp-filter.bin").read_bytes()
    if len(filter_bytes) != 672 or digest(filter_bytes) != FILTER_SHA256:
        raise SystemExit("seccomp filter bytes do not match the accepted fixed policy")

    roles: list[tuple[str, str, str, str, bool]] = []
    for component in ("provider", "owner", "recovery"):
        roles.extend([
            (f"{component}-contract", f"{component}/Gagamba.Execution.dll", "managed", component, component != "provider"),
            (f"{component}-m1", f"{component}/Gagamba.Execution.Linux.M1.dll", "managed", component, component != "provider"),
        ])
        if component in ("owner", "recovery"):
            entry = f"Gagamba.Execution.Linux.M1.{component.capitalize()}"
            roles.extend([
                (f"{component}-entry", f"{component}/{entry}.dll", "managed", component, True),
                (f"{component}-deps", f"{component}/{entry}.deps.json", "config", component, True),
                (f"{component}-runtimeconfig", f"{component}/{entry}.runtimeconfig.json", "config", component, True),
            ])
    roles.extend([
        ("coordinator", "native/exec-coordinator", "native", "coordinator", False),
        ("seccomp-filter", "native/seccomp-filter.bin", "policy", "coordinator", False),
    ])
    artifacts = []
    for role, relative, kind, consumer, survives in roles:
        full = candidate / relative
        if not full.is_file() or full.is_symlink():
            raise SystemExit(f"required artifact missing or symlink: {relative}")
        artifacts.append(artifact(candidate, role, relative, kind, consumer, survives))
    artifacts.sort(key=lambda row: (row["role"], row["path"]))

    dotnet_host = Path(shutil.which("dotnet") or "").resolve(strict=True)
    runtime_dir = find_runtime_dir("Microsoft.NETCore.App", RUNTIME_VERSION)
    fxr_parent = dotnet_host.parent / "host" / "fxr"
    fxr_dir = fxr_parent / RUNTIME_VERSION
    if not fxr_dir.is_dir():
        versions = sorted(p for p in fxr_parent.iterdir() if p.is_dir())
        if len(versions) != 1:
            raise SystemExit("hostfxr inventory must resolve to one exact version")
        fxr_dir = versions[0]
    bwrap = Path(shutil.which("bwrap") or "").resolve(strict=True)
    bwrap_version = run("bwrap", "--version").split()[-1]
    if bwrap_version != "0.11.1":
        raise SystemExit(f"Bubblewrap version differs from accepted pin: {bwrap_version}")
    bwrap_package = run("dpkg-query", "-W", "-f=${Version}", "bubblewrap")
    loader = Path("/lib64/ld-linux-x86-64.so.2")
    if not loader.exists():
        loader = Path("/lib/x86_64-linux-gnu/ld-linux-x86-64.so.2")
    loader = loader.resolve(strict=True)
    kernel = run("uname", "-r")

    source_obj = {
        "commit": args.source_commit,
        "tree": args.source_tree,
        "toolchain": {
            "dotnet": run("dotnet", "--version"),
            "gcc": run("gcc", "--version").splitlines()[0],
            "python": run("python3", "--version"),
            "distro": os_release(),
            "kernel": kernel,
        },
        "buildCommandDigest": projection(source, ["eng/linux-m1/build-install-candidate.sh", "eng/linux-m1/build-install-candidate.py"]),
    }
    manifest = {
        "schemaVersion": 1,
        "profileId": PROFILE_ID,
        "profileVersion": "1",
        "source": source_obj,
        "platform": {
            "rid": "linux-x64",
            "architecture": "x86_64",
            "kernelMin": kernel,
            "glibcMin": GLIBC_MIN,
            "dotnetRuntime": RUNTIME_VERSION,
            "dotnetHostSha256": file_digest(dotnet_host),
            "runtimeInventorySha256": inventory(runtime_dir),
            "hostFxrInventorySha256": inventory(fxr_dir),
            "bubblewrap": {
                "path": str(bwrap),
                "version": bwrap_version,
                "sha256": file_digest(bwrap),
                "package": bwrap_package,
            },
            "loaderSha256": file_digest(loader),
            "requiredFeatures": REQUIRED_FEATURES,
        },
        "protocol": {
            "manifestSchema": 1,
            "implementationId": "linux-m1-native-transaction",
            "ownerProtocol": 1,
            "coordinatorProtocol": 1,
            "bootstrap": 1,
            "coordinatorHeaderBytes": 96,
            "releaseFrameBytes": 128,
            "releaseSequence": 1,
        },
        "policy": {
            "profileId": PROFILE_ID,
            "seccompFilterSha256": FILTER_SHA256,
            "seccompFilterLength": 672,
            "bubblewrapArgsSha256": file_digest(source / ARGS_PATH),
            "authoritySha256": projection(source, AUTHORITY_PATHS),
            "resourcesSha256": resource_projection(source),
        },
        "artifacts": artifacts,
    }
    manifest_bytes = json.dumps(manifest, ensure_ascii=True, separators=(",", ":")).encode("ascii")
    (candidate / "manifest.json").write_bytes(manifest_bytes)
    set_digest = digest(manifest_bytes)

    expected = {"manifest.json"} | {relative for _, relative, *_ in roles}
    actual = {p.relative_to(candidate).as_posix() for p in candidate.rglob("*") if p.is_file()}
    if actual != expected:
        raise SystemExit(f"candidate has unexpected/missing files: extra={sorted(actual-expected)}, missing={sorted(expected-actual)}")
    for forbidden in ("Tests", "TestFixture", "Fixture", "Qualification", ".pdb"):
        if any(forbidden.lower() in p.lower() for p in actual):
            raise SystemExit(f"test-only/debug artifact refused: {forbidden}")

    output.mkdir(parents=True, exist_ok=True)
    for path in sorted(candidate.rglob("*")):
        relative = path.relative_to(candidate)
        target = output / relative
        if path.is_dir():
            target.mkdir(parents=True, exist_ok=True)
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, target)
            os.chmod(target, 0o555 if relative.as_posix() == "native/exec-coordinator" else 0o444)
    digest_output.write_text(set_digest + "\n", encoding="ascii", newline="\n")
    os.chmod(digest_output, 0o444)
    print(f"source.commit={args.source_commit}")
    print(f"source.tree={args.source_tree}")
    print(f"artifactSetSha256={set_digest}")
    print(f"manifestBytes={len(manifest_bytes)}")
    print(f"runtimeInventorySha256={manifest['platform']['runtimeInventorySha256']}")
    print(f"hostFxrInventorySha256={manifest['platform']['hostFxrInventorySha256']}")
    for name, value in manifest["policy"].items():
        if name.endswith("Sha256"):
            print(f"{name}={value}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
