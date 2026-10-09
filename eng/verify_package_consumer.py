#!/usr/bin/env python3
"""Restore and execute a fresh consumer using only candidate nupkg files."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import platform
import re
import subprocess
import sys
import tempfile
from pathlib import Path

IDS = (
    "Gagamba.Execution", "Gagamba.Execution.Windows",
    "Gagamba.Execution.Linux", "Gagamba.Execution.MacOS", "Gagamba.Runtime",
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--packages-dir", type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if not re.fullmatch(r"[0-9a-f]{40}", args.source_sha):
        raise ValueError("exact source SHA required")
    root = Path(__file__).resolve().parent
    template = (root / "package-consumer" / "PackageConsumer.csproj.template").read_text()
    program = (root / "package-consumer" / "Program.cs").read_text()
    if "ProjectReference" in template or "ProjectReference" in program:
        raise ValueError("consumer must not reference project source")
    hashes = {}
    for package_id in IDS:
        path = args.packages_dir / f"{package_id}.{args.version}.nupkg"
        if not path.is_file():
            raise ValueError(f"missing candidate: {path.name}")
        hashes[package_id] = hashlib.sha256(path.read_bytes()).hexdigest()
    system = platform.system()
    key = {"Windows": "windows", "Linux": "linux", "Darwin": "macos"}.get(system)
    if key is None:
        raise ValueError("unsupported installed-consumer host")
    with tempfile.TemporaryDirectory(prefix="gagamba-installed-consumer-") as directory:
        ws = Path(directory)
        (ws / "PackageConsumer.csproj").write_text(
            template.replace("@CANDIDATE_VERSION@", args.version), encoding="utf-8")
        (ws / "Program.cs").write_text(program, encoding="utf-8")
        # Stop MSBuild's parent-directory search at this fresh project. A
        # machine-wide Directory.Build.props must not alter the consumer.
        (ws / "Directory.Build.props").write_text("<Project/>\n", encoding="utf-8")
        (ws / "Directory.Build.targets").write_text("<Project/>\n", encoding="utf-8")
        (ws / "NuGet.Config").write_text(
            '<configuration><packageSources><clear/><add key="candidate" value="'
            + str(args.packages_dir.resolve()).replace("&", "&amp;").replace('"', "&quot;")
            + '"/></packageSources></configuration>', encoding="utf-8")
        environment = os.environ.copy()
        environment["NUGET_PACKAGES"] = str(ws / "nuget-cache")
        subprocess.run(["dotnet", "restore", "PackageConsumer.csproj", "--configfile", "NuGet.Config"],
                       cwd=ws, env=environment, check=True, timeout=180)
        subprocess.run(["dotnet", "run", "--project", "PackageConsumer.csproj", "--no-restore"],
                       cwd=ws, env=environment, check=True, timeout=90)
    payload = {"sourceSha": args.source_sha, "version": args.version,
               "result": "Passed", "lifecycle": "Passed",
               "platform": key, "packageSha256": hashes}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print("Independent installed-package consumer passed on " + key)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
