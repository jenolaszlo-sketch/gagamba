import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

repo, scratch = (Path(x).resolve() for x in sys.argv[1:3])
where = scratch / "sensitivity-source"
where.mkdir(exist_ok=True)
source = repo / "src/Gagamba.Execution.Linux.M1/Bootstrap"
model = (source / "ValidationCustodyModel.cs").read_text(encoding="utf-8")
data = (source / "ValidationCustodyModelData.cs").read_text(encoding="utf-8")
ns = "namespace Gagamba.Execution.Linux.M1.Bootstrap;"
def replace(text, find, changed):
    assert text.count(find) == 1, find
    return text.replace(find, changed)

mutations = []
def omit(data_text, needle):
    return replace(data_text, needle + " &&\n", "")
for name, field, case in (
    ("LauncherClosure", "launcherClosure", "LauncherClosure"),
    ("ControllerClosure", "controllerClosure", "ControllerClosure"),
    ("DriverClosure", "driverClosure", "DriverClosure"),
    ("Catalog", "catalog", "Catalog"),
    ("Approval", "approval", "Approval"),
):
    expr = f"        {field}.AsSpan().SequenceEqual(observed.{field})"
    mutations.append((name, case, model, omit(data, expr), "omit " + expr.strip()))
mutations.extend([
    ("OriginalC", "OriginalC", model, omit(data, "        ControllerOriginal == observed.ControllerOriginal"),
     "omit original-controller comparison"),
    ("Provenance", "Provenance", model, omit(data, "        Provenance == ValidationCustodyFact.Consistent"),
     "omit required provenance classification"),
    ("Drop", "Drop", model, omit(data,
        "        PermanentDrop == FactDue(stage, ValidationCustodyState.DroppedModel)"), "omit drop fact"),
    ("BypassDrop", "BypassDrop", replace(model,
        "            _ => null",
        "            (ValidationCustodyState.OfferedModel, ValidationCustodyEvent.ObserveConsume) =>\n"
        "                ValidationCustodyState.ConsumedModel,\n            _ => null"), data, "allow OFFER to skip DROP"),
    ("OneUse", "OneUse", replace(model, "            _ => null",
        "            (ValidationCustodyState.IssuedModel, ValidationCustodyEvent.ObserveIssue) =>\n"
        "                ValidationCustodyState.IssuedModel,\n            _ => null"), data, "allow repeat issue"),
    ("Terminal", "Terminal", replace(model,
        "                if (snapshot is null)\n                {\n                    Close();\n                }",
        "                // Disposable derivative deliberately retains a failed state."), data, "omit failed Advance cleanup"),
    ("Expiry", "Expiry", replace(model, "nowNs >= deadline", "nowNs > deadline"), data,
     "allow equality at original deadline"),
    ("Stop", "Stop", replace(model, "        if (stopObserved)\n        {\n            return ValidationCustodyRefusal.Stopped;\n        }\n\n",
        ""), data, "omit supplied stop check"),
    ("Intake", "Intake", model, replace(data, "this.launcherExecutable = CopyDigest(launcherExecutable);",
        "this.launcherExecutable = launcherExecutable is { Length: 32 } ? launcherExecutable : [];"),
     "alias an intake digest"),
    ("Output", "Output", model, replace(data, "LauncherExecutable => launcherExecutable.ToArray();",
        "LauncherExecutable => launcherExecutable;"), "alias a returned digest"),
    ("Precedence", "Precedence", replace(model, "        if (!expected.Matches(observed))",
        "        if (!evidence.MatchesStage(destination))\n        {\n            return ValidationCustodyRefusal.Evidence;\n        }\n\n"
        "        if (!expected.Matches(observed))"), data, "check evidence before comparison"),
    ("Exception", "Exception", replace(model,
        "                var result = new ValidationCustodyModelSnapshot(state, original);",
        "                if (next == ValidationCustodyEvent.ObserveIssue)\n"
        "                    throw new InvalidOperationException(\"synthetic snapshot fault\");\n"
        "                var result = new ValidationCustodyModelSnapshot(state, original);"), data,
      "inject managed exception before snapshot publication to exercise unchanged finally"),
])
def hash_file(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
receipt = []
for name, case, m, d, variation in mutations:
    files = []
    for role, code in (("Model", m), ("Data", d)):
        target = where / f"{name}.{role}.cs"
        target.write_text(replace(code, ns, "using Gagamba.Execution.Linux.M1.Bootstrap;\n\n"
                                  f"namespace ProducerSensitivity.{name};"), encoding="utf-8", newline="\n")
        files.append(dict(path=target.name, sha256=hash_file(target)))
    receipt.append(dict(variant=name, case=case, deviation=variation, files=files))
(scratch / "sensitivity-variants.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
shutil.copyfile(scratch / "SensitivityProgram.cs", where / "Program.cs")
shutil.copyfile(scratch / "Sensitivity.csproj", where / "Sensitivity.csproj")
env = os.environ.copy()
env.update(NuGetAudit="false", DOTNET_CLI_TELEMETRY_OPTOUT="1")
for config in sys.argv[3:] or ["Debug", "Release"]:
    core = scratch / "candidate-exact" / "bin" / "Gagamba.Execution.Linux.M1" / config.lower()
    output = scratch / "sensitivity-build" / config.lower()
    project = where / "Sensitivity.csproj"
    args = ["dotnet", "build", str(project), "-c", config, "--artifacts-path", str(output), "--nologo",
            "-p:UseSharedCompilation=false", "-p:Core=" + str(core)]
    with (scratch / f"sensitivity-build-{config}.log").open("wb") as log:
        log.write((json.dumps(args) + "\n").encode())
        code = subprocess.run(args, env=env, stdout=log, stderr=subprocess.STDOUT).returncode
        log.write(f"\nBUILD EXIT {code}\n".encode())
    if code:
        sys.exit(code)
    binary = output / "bin" / "Sensitivity" / config.lower() / "Gagamba.Execution.Linux.M1.Tests.dll"
    loaded = binary.with_name("Gagamba.Execution.Linux.M1.dll")
    assert hash_file(loaded) == hash_file(core / loaded.name)
    args = ["dotnet", str(binary)]
    with (scratch / f"sensitivity-{config}.tsv").open("wb") as log:
        code = subprocess.run(args, env=env, stdout=log, stderr=subprocess.STDOUT).returncode
    path = scratch / f"sensitivity-build-{config}.log"
    with path.open("ab") as log:
        log.write((json.dumps(dict(command=args, exit=code, probe_sha256=hash_file(binary),
                                  actual_core_sha256=hash_file(loaded))) + "\n").encode())
    if code:
        sys.exit(code)
