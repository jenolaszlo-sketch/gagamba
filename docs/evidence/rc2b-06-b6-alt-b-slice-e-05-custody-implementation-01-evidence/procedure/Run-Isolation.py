import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

repo, scratch = (Path(x).resolve() for x in sys.argv[1:3])
env = os.environ.copy()
env.update(NuGetAudit="false", DOTNET_CLI_TELEMETRY_OPTOUT="1", RestoreIgnoreFailedSources="true")
log_root = scratch / "audit"
log_root.mkdir(exist_ok=True)
bin_root = scratch / "candidate-exact" / "bin"
core = "Gagamba.Execution.Linux.M1"
core_project = repo / "src" / core / (core + ".csproj")
def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
def run(tag, args, *, expected=0, document=None):
    log_path = log_root / f"{tag}.log"
    with log_path.open("wb") as log:
        log.write((json.dumps(args) + "\n").encode())
        if document is None:
            code = subprocess.run(args, cwd=repo, env=env, stdout=log, stderr=subprocess.STDOUT).returncode
        else:
            with document.open("wb") as out:
                code = subprocess.run(args, cwd=repo, env=env, stdout=out, stderr=log).returncode
        log.write(f"\nEXIT {code}\n".encode())
    print(tag, code, flush=True)
    assert code == expected, str(log_path)
    return code

tool_root = scratch / "metadata-build"
run("build-audit", ["dotnet", "build", str(scratch / "metadata-tool" / "Audit.csproj"),
                    "-c", "Release", "--artifacts-path", str(tool_root), "--nologo"])
tool = tool_root / "bin" / "Audit" / "release" / "Audit.dll"
verifier = bin_root / "VerifyProductionAssembly" / "release" / "VerifyProductionAssembly.dll"
all_files = {}
for configuration in ["Debug", "Release"]:
    config = configuration.lower()
    assemblies = []
    for project in [core, core + ".Owner", core + ".Recovery", "InstallTool", "Gagamba.Execution",
                    "Gagamba.Runtime", "B2Controller", core + ".Tests"]:
        project_dir = bin_root / project / config
        if not project_dir.is_dir() and project == "B2Controller":
            project_dir = bin_root / (core + ".B2Controller") / config
        project_files = [p for p in sorted(project_dir.glob("*.dll"))
                         if p.name.startswith("Gagamba.") or p.name in ("InstallTool.dll", "B2Controller.dll")]
        assert project_files, project_dir
        if project in [core + ".Owner", core + ".Recovery", "InstallTool"]:
            assert sha(project_dir / (core + ".dll")) == sha(bin_root / core / config / (core + ".dll"))
        assemblies.extend(project_files)
    layout = scratch / "layouts" / config
    for name in ["focused", str(Path("tests") / config),
                 str(Path("src") / core / "bin" / "Release" / "net10.0")]:
        copies = sorted(p for p in (layout / name).glob("Gagamba.*.dll"))
        assert copies, name
        assemblies.extend(copies)
    if config == "release":
        for project in ("publish-m1", "publish-owner", "publish-recovery"):
            packages = scratch / "policy-exact" / project / "publish"
            copies = sorted(packages.rglob("Gagamba.*.dll"))
            assert copies and any(p.name == core + ".dll" for p in copies), project
            assemblies.extend(copies)
    all_files[config] = assemblies
    exact_output = log_root / f"metadata-{config}.jsonl"
    run("metadata-" + config, ["dotnet", str(tool), *map(str, assemblies)], document=exact_output)
    exact = [json.loads(line) for line in exact_output.read_text(encoding="utf-8").splitlines()]
    assert len(exact) == len(assemblies) and all(row["pass"] for row in exact)
    assert any(row["knownBootstrapCallers"] and row["knownBinaryCallers"] for row in exact)
    assert any(row["callableHistory"] for row in exact if row["assembly"].endswith(".Tests"))
    assert any(row["projectEffectRefs"] for row in exact if not row["assembly"].endswith(".Tests"))
    assembly = bin_root / core / config / (core + ".dll")
    run("verifier-" + config, ["dotnet", str(verifier), "--production", str(assembly)],
        expected=1 if config == "debug" else 0)
    verify_text = (log_root / f"verifier-{config}.log").read_text(encoding="utf-8")
    assert ("expected Release" in verify_text and "RESULT: FAIL" in verify_text if config == "debug"
            else "RESULT: PASS" in verify_text)

    derivative = scratch / "isolation-derivative" / config
    run("build-inbound-control-" + config,
        ["dotnet", "build", str(core_project), "-c", configuration, "--artifacts-path", str(derivative),
         "-p:CustomAfterMicrosoftCommonTargets=" + str(scratch / "IsolationProbe.targets"), "--nologo", "-m:1"])
    derivative_assembly = derivative / "bin" / core / config / (core + ".dll")
    assert sha(derivative_assembly) != sha(assembly)
    control_output = log_root / f"inbound-control-{config}.jsonl"
    run("inbound-control-" + config, ["dotnet", str(tool), str(derivative_assembly)], expected=1,
        document=control_output)
    control = json.loads(control_output.read_text(encoding="utf-8"))
    assert not control["pass"]
    assert any("ProducerIsolationControl.ForbiddenCaller::Probe" in hit and
               "ValidationCustodyModel::TryCreate" in hit for hit in control["hits"]), control["hits"]
(log_root / "audit-tool-hashes.json").write_text(json.dumps(
    {str(path): sha(path) for path in [tool, verifier, scratch / "metadata-tool" / "Program.cs",
                                      scratch / "IsolationReference.cs", scratch / "IsolationProbe.targets"]},
    indent=2) + "\n", encoding="utf-8")
