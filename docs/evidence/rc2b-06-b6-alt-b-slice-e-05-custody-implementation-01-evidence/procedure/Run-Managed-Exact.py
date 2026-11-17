import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

repo, scratch = (Path(arg).resolve() for arg in sys.argv[1:3])
configuration = sys.argv[3]
assert configuration in {"Debug", "Release"}
config = configuration.lower()
m1 = "Gagamba.Execution.Linux.M1"
test = m1 + ".Tests"
candidate = scratch / "candidate-exact" / "bin"
results = scratch / "results" / config
results.mkdir(parents=True, exist_ok=True)
layout = scratch / "layouts" / config
broad = layout / "tests" / config
focused = layout / "focused"
release_discovery = layout / "src" / m1 / "bin" / "Release" / "net10.0"
release_discovery.mkdir(parents=True, exist_ok=True)
shutil.copyfile(repo / "Gagamba.sln", layout / "Gagamba.sln")
for dll in (candidate / m1 / "release").glob("*.dll"):
    shutil.copyfile(dll, release_discovery / dll.name)
for output in (focused, broad):
    shutil.copytree(candidate / test / config, output, dirs_exist_ok=True)
for dll in (m1 + ".dll", "Gagamba.Execution.dll"):
    shutil.copyfile(candidate / m1 / config / dll, focused / dll)

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

bindings = []
for selected, reference in (
    (focused / (m1 + ".dll"), candidate / m1 / config / (m1 + ".dll")),
    (focused / "Gagamba.Execution.dll", candidate / m1 / config / "Gagamba.Execution.dll"),
    (focused / (test + ".dll"), candidate / test / config / (test + ".dll")),
    (broad / (test + ".dll"), candidate / test / config / (test + ".dll")),
    (broad / (m1 + ".dll"), candidate / m1 / "debug" / (m1 + ".dll")),
    (release_discovery / (m1 + ".dll"), candidate / m1 / "release" / (m1 + ".dll")),
):
    value = digest(selected)
    assert value == digest(reference), (selected, reference)
    bindings.append(dict(path=str(selected), reference=str(reference), sha256=value))
(results / "bindings.json").write_text(json.dumps(bindings, indent=2) + "\n", encoding="utf-8")

env = os.environ.copy()
env.update(VSTEST_CONNECTION_TIMEOUT="15", DOTNET_CLI_TELEMETRY_OPTOUT="1")
suites = {
    "e5": ["ValidationCustodyModelTests", "ValidationCustodyModelLifecycleTests",
           "ValidationCustodyModelCompositionTests"],
    "e4": ["Gava1ContextCodecTests"],
    "e3": ["ValidationPreCodecBootstrapTests", "ValidationPreCodecBinaryTests", "InstallationWireTests",
           "ValidationPreCodecRemediationTests", "ValidationPreCodecR2Tests"],
    "broad": [],
}
selection = sys.argv[4:] or list(suites)
expected = {"e3": 185, "e4": 11}
returncode = 0
for suite in selection:
    classes = suites[suite]
    dll = (focused if classes else broad) / (test + ".dll")
    trx = results / (suite + ".trx")
    vstest = ["dotnet", "vstest", str(dll), "/Logger:trx;LogFileName=" + trx.name,
              "/ResultsDirectory:" + str(results)]
    if classes:
        vstest.append("/TestCaseFilter:" + "|".join("FullyQualifiedName~" + cls for cls in classes))
    with (results / ("vstest-" + suite + ".log")).open("wb") as log:
        log.write((json.dumps(dict(command=vstest, cwd=str(layout))) + "\n").encode())
        try:
            code = subprocess.run(vstest, cwd=layout, env=env, stdout=log, stderr=subprocess.STDOUT,
                                  timeout=120).returncode
        except subprocess.TimeoutExpired:
            code = 124
        log.write(f"\nEXIT {code}\n".encode())
    print(configuration, suite, "VSTest", code, flush=True)
    args = ["dotnet", str(dll), "-noColor"]
    for cls in classes:
        args.extend(["-class", "*" + cls])
    xml = results / (suite + ".xml")
    args.extend(["-xml", str(xml)])
    with (results / ("xunit-" + suite + ".log")).open("wb") as log:
        log.write((json.dumps(dict(command=args, cwd=str(layout))) + "\n").encode())
        code = subprocess.run(args, cwd=layout, env=env, stdout=log, stderr=subprocess.STDOUT).returncode
        log.write(f"\nEXIT {code}\n".encode())
    if not xml.exists():
        print(configuration, suite, "no XML", flush=True)
        returncode = 1
        continue
    counts = ET.parse(xml).getroot().find("assembly").attrib
    print(configuration, suite, {key: counts[key] for key in ("total", "passed", "failed", "skipped", "errors")},
          "xUnit exit", code, flush=True)
    if code or int(counts["failed"]) or int(counts["errors"]):
        returncode = 1
    if suite in expected and (int(counts["passed"]) != expected[suite] or int(counts["skipped"])):
        returncode = 1
sys.exit(returncode)
