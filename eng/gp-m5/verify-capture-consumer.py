#!/usr/bin/env python3
"""Run GP-M5 capture qualification from exact-version NuGet packages only."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import platform
import re
import subprocess
import tempfile
import zipfile
from pathlib import Path
from xml.etree import ElementTree

PACKAGE_IDS = (
    "Gagamba.Execution",
    "Gagamba.Execution.Windows",
    "Gagamba.Execution.Linux",
    "Gagamba.Execution.MacOS",
    "Gagamba.Runtime",
)
SHA_PATTERN = re.compile(r"[0-9a-f]{40}")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def package_metadata(path: Path) -> tuple[str, str, str | None]:
    with zipfile.ZipFile(path) as package:
        nuspecs = [name for name in package.namelist() if name.lower().endswith(".nuspec")]
        if len(nuspecs) != 1:
            raise ValueError(f"expected one nuspec in {path.name}; found {len(nuspecs)}")
        root = ElementTree.fromstring(package.read(nuspecs[0]))
    metadata = root.find("{*}metadata")
    if metadata is None:
        raise ValueError(f"nuspec metadata is missing in {path.name}")
    package_id = metadata.findtext("{*}id")
    version = metadata.findtext("{*}version")
    repository = metadata.find("{*}repository")
    commit = repository.get("commit") if repository is not None else None
    if not package_id or not version:
        raise ValueError(f"package id/version is missing in {path.name}")
    return package_id, version, commit


def run_logged(command: list[str], *, cwd: Path, env: dict[str, str],
               timeout: int, log_path: Path) -> None:
    try:
        completed = subprocess.run(command, cwd=cwd, env=env, text=True,
                                   stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                   timeout=timeout, check=False)
        log_path.write_text(completed.stdout or "", encoding="utf-8")
    except subprocess.TimeoutExpired as exc:
        output = exc.stdout or ""
        if isinstance(output, bytes):
            output = output.decode("utf-8", errors="replace")
        log_path.write_text(str(output) + f"\nTIMEOUT after {timeout}s\n", encoding="utf-8")
        raise TimeoutError(f"command timed out after {timeout}s: {' '.join(command)}") from exc
    if completed.returncode != 0:
        raise RuntimeError(f"command exited {completed.returncode}: {' '.join(command)}")


def write_json(path: Path, payload: object) -> None:
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--packages-dir", type=Path, required=True,
                        help="directory containing the five candidate nupkg files")
    parser.add_argument("--version", required=True)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--output-directory", type=Path, required=True)
    args = parser.parse_args()

    if not SHA_PATTERN.fullmatch(args.source_sha):
        raise ValueError("--source-sha must be a lowercase 40-character Git SHA")
    packages_dir = args.packages_dir.resolve(strict=True)
    if not packages_dir.is_dir():
        raise ValueError("--packages-dir must be a directory")
    output_dir = args.output_directory.resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    manifest_path = output_dir / "capture-package-manifest.json"
    result_path = output_dir / "capture-consumer.json"
    command_log = output_dir / "capture-consumer.log"
    manifest: dict[str, object] = {
        "sourceSha": args.source_sha,
        "version": args.version,
        "packagesDirectory": str(packages_dir),
        "hostSystem": platform.platform(),
        "packages": [],
        "consumerResult": "NotRun",
    }
    write_json(manifest_path, manifest)

    packages: list[dict[str, str]] = []
    try:
        for package_id in PACKAGE_IDS:
            package_path = packages_dir / f"{package_id}.{args.version}.nupkg"
            if not package_path.is_file():
                raise ValueError(f"missing exact candidate package: {package_path.name}")
            actual_id, actual_version, package_commit = package_metadata(package_path)
            if actual_id != package_id or actual_version != args.version:
                raise ValueError(f"nuspec identity mismatch in {package_path.name}: {actual_id} {actual_version}")
            if package_commit != args.source_sha:
                raise ValueError(
                    f"repository commit mismatch in {package_path.name}: {package_commit!r} != {args.source_sha}")
            packages.append({
                "id": package_id,
                "version": args.version,
                "repositoryCommit": package_commit,
                "file": package_path.name,
                "sha256": sha256(package_path),
            })
    except Exception as exc:
        manifest["packages"] = packages
        manifest["consumerResult"] = "PackageVerificationFailed"
        manifest["error"] = f"{type(exc).__name__}: {exc}"
        write_json(manifest_path, manifest)
        raise
    manifest["packages"] = packages
    manifest["consumerResult"] = "PackageSetVerified"
    write_json(manifest_path, manifest)

    source_root = Path(__file__).resolve().parents[2]
    project_source = source_root / "tests" / "Gagamba.CaptureProbe"
    project_file = project_source / "Gagamba.CaptureProbe.csproj"
    program_file = project_source / "Program.cs"
    qualification_file = project_source / "CaptureQualification.cs"
    for required in (project_file, program_file, qualification_file):
        if not required.is_file():
            raise FileNotFoundError(f"consumer source is missing: {required}")

    try:
        with tempfile.TemporaryDirectory(prefix="gagamba-capture-consumer-") as temp_name:
            workspace = Path(temp_name)
            (workspace / "Program.cs").write_bytes(program_file.read_bytes())
            (workspace / "CaptureQualification.cs").write_bytes(qualification_file.read_bytes())
            # This project deliberately has a PackageReference and no source project reference.
            project = (
                '<Project Sdk="Microsoft.NET.Sdk">\n'
                '  <PropertyGroup>\n'
                '    <OutputType>Exe</OutputType>\n'
                '    <TargetFramework>net10.0</TargetFramework>\n'
                '    <Nullable>enable</Nullable>\n'
                '    <ImplicitUsings>enable</ImplicitUsings>\n'
                '    <UseSharedCompilation>false</UseSharedCompilation>\n'
                '    <RestorePackagesPath>$(NUGET_PACKAGES)</RestorePackagesPath>\n'
                '  </PropertyGroup>\n'
                '  <ItemGroup>\n'
                f'    <PackageReference Include="Gagamba.Runtime" Version="[{args.version}]" />\n'
                '  </ItemGroup>\n'
                '</Project>\n'
            )
            (workspace / "CaptureConsumer.csproj").write_text(project, encoding="utf-8")
            # Stop parent-directory MSBuild customization from affecting the consumer.
            (workspace / "Directory.Build.props").write_text("<Project />\n", encoding="utf-8")
            (workspace / "Directory.Build.targets").write_text("<Project />\n", encoding="utf-8")

            nuget_config = ElementTree.Element("configuration")
            sources = ElementTree.SubElement(nuget_config, "packageSources")
            ElementTree.SubElement(sources, "clear")
            ElementTree.SubElement(sources, "add", {"key": "candidate", "value": str(packages_dir)})
            ElementTree.ElementTree(nuget_config).write(
                workspace / "NuGet.Config", encoding="utf-8", xml_declaration=True)

            isolated_cache = workspace / "nuget-packages"
            isolated_cli_home = workspace / "dotnet-home"
            environment = os.environ.copy()
            environment.update({
                "NUGET_PACKAGES": str(isolated_cache),
                "NUGET_HTTP_CACHE_PATH": str(workspace / "nuget-http-cache"),
                "DOTNET_CLI_HOME": str(isolated_cli_home),
                "UseSharedCompilation": "false",
                "MSBUILDDISABLENODEREUSE": "1",
                "DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER": "1",
                "GITHUB_SHA": args.source_sha,
                "CI": "true",
            })
            restore = ["dotnet", "restore", "CaptureConsumer.csproj", "--configfile", "NuGet.Config",
                       "-p:UseSharedCompilation=false", "-nodeReuse:false"]
            run_logged(restore, cwd=workspace, env=environment, timeout=240,
                       log_path=output_dir / "capture-restore.log")
            qualify = ["dotnet", "run", "--project", "CaptureConsumer.csproj", "--no-restore",
                       "--configuration", "Release", "--", "--mode", "qualify",
                       "--output", str(result_path.resolve())]
            run_logged(qualify, cwd=workspace, env=environment, timeout=1200,
                       log_path=command_log)

        if not result_path.is_file():
            raise RuntimeError("qualification consumer exited successfully without its JSON report")
        result = json.loads(result_path.read_text(encoding="utf-8"))
        if result.get("SourceSha") != args.source_sha:
            raise RuntimeError("consumer report source SHA does not match the requested source")
        if result.get("ExitCode") != 0 or result.get("FailedCaseCount") != 0:
            raise RuntimeError("native qualification consumer reported a failed result or case")
        if result.get("MandatoryCaseCount") != 14 or result.get("PassedCaseCount") != 14:
            raise RuntimeError("native qualification consumer did not pass all 14 mandatory cases")
        cases = result.get("Cases")
        if not isinstance(cases, list) or len(cases) != 14:
            raise RuntimeError("native qualification consumer did not emit exactly 14 case records")
        if any(case.get("Status") != "Passed" or case.get("CleanupConfirmed") is not True
               for case in cases):
            raise RuntimeError("one or more mandatory cases failed or lacked confirmed cleanup")
        if result.get("ProviderDispose") != "Succeeded":
            raise RuntimeError("native provider disposal was not confirmed")
        if result.get("NativeHostGuardPassed") is not True:
            raise RuntimeError("qualification report did not confirm its native-host guard")
        manifest["consumerResult"] = "Passed"
        manifest["consumerReport"] = result_path.name
        manifest["consumerReportSha256"] = sha256(result_path)
        write_json(manifest_path, manifest)
        print(f"Exact-package capture consumer passed; artifacts: {output_dir}")
        return 0
    except Exception as exc:
        manifest["consumerResult"] = "Failed"
        manifest["error"] = f"{type(exc).__name__}: {exc}"
        manifest["consumerReport"] = result_path.name if result_path.exists() else None
        manifest["consumerLog"] = command_log.name
        write_json(manifest_path, manifest)
        if result_path.exists():
            try:
                consumer_result = json.loads(result_path.read_text(encoding="utf-8"))
                if consumer_result.get("SourceSha") != args.source_sha:
                    manifest["error"] += "; consumer report source SHA mismatch"
                    write_json(manifest_path, manifest)
            except (OSError, json.JSONDecodeError):
                pass
        raise


if __name__ == "__main__":
    raise SystemExit(main())
