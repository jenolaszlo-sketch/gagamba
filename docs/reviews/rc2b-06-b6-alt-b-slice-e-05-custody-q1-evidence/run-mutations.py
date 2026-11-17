import csv
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

REPO = Path(sys.argv[1]).resolve()
RUN = Path(sys.argv[2]).resolve()
ANNEX = Path(__file__).resolve().parent
CORE_NAME = "Gagamba.Execution.Linux.M1"
NAMESPACE = CORE_NAME + ".Bootstrap"
SOURCE = REPO / "src" / CORE_NAME / "Bootstrap"
WORK = RUN / "independent-mutations"
WORK.mkdir()
SOURCE_MODEL = (SOURCE / "ValidationCustodyModel.cs").read_text(encoding="utf-8")
SOURCE_DATA = (SOURCE / "ValidationCustodyModelData.cs").read_text(encoding="utf-8")
PROGRAM = (ANNEX / "probe" / "Program.cs").read_text(encoding="utf-8")

def once(source, old, new):
    assert source.count(old) == 1, old
    return source.replace(old, new)

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def comparison(field):
    return f"        {field}.AsSpan().SequenceEqual(observed.{field}) &&\n"

mutations = []

def omit(name, row, old, witness):
    mutations.append((name, row, "Data", old, "", witness))

for clr, tag in (("launcherExecutable", "launcher-image"), ("launcherClosure", "launcher-closure"),
                 ("controllerExecutable", "controller-image"), ("controllerClosure", "controller-closure"),
                 ("driverExecutable", "driver-image"), ("driverClosure", "driver-closure")):
    omit(clr, "R03", comparison(clr), f"R03-R04/1/{tag}")
omit("catalog", "R04", comparison("catalog"), "R03-R04/1/catalog")
omit("approval", "R04", comparison("approval"), "R03-R04/1/approval")
omit("originalController", "R05", "        ControllerOriginal == observed.ControllerOriginal &&\n",
     "R05/1/controller")
omit("provenance", "R06", "        Provenance == ValidationCustodyFact.Consistent &&\n", "R06/0/provenance/0")
omit("dropPremise", "R06/R08", "        PermanentDrop == FactDue(stage, ValidationCustodyState.DroppedModel) &&\n",
     "R06/3/drop/0")
mutations += [
    ("oneUse", "R07", "Model", "            _ => null",
     "            (ValidationCustodyState.IssuedModel, ValidationCustodyEvent.ObserveIssue) =>\n"
     "                ValidationCustodyState.IssuedModel,\n            _ => null", "R07/repeated-issue-with-issue-facts"),
    ("skipDrop", "R08", "Model", "            _ => null",
     "            (ValidationCustodyState.OfferedModel, ValidationCustodyEvent.ObserveConsume) =>\n"
     "                ValidationCustodyState.ConsumedModel,\n            _ => null", "R08/consume-before-drop-with-all-premises"),
    ("deadline", "R09", "Model", "nowNs >= deadline", "nowNs > deadline", "R09/advance/0/exact-bound"),
    ("stop", "R10", "Model", "        if (stopObserved)", "        if (false && stopObserved)",
     "R09-R11/advance/stop-beats-expiry"),
    ("order", "R11", "Model", "        if (!expected.Matches(observed))",
     "        if (!evidence.MatchesStage(destination))\n            return ValidationCustodyRefusal.Evidence;\n"
     "        if (!expected.Matches(observed))", "R09-R11/advance/comparison-beats-evidence"),
    ("retainAfterFailure", "R12", "Model",
     "                if (snapshot is null)\n                {\n                    Close();\n                }",
     "                // Terminal cleanup deliberately omitted in this disposable derivative.",
     "R12/R24/null-observation-clears"),
    ("inputAlias", "R13", "Data", "this.controllerExecutable = CopyDigest(controllerExecutable);",
     "this.controllerExecutable = controllerExecutable is { Length: 32 } ? controllerExecutable : [];",
     "R13/all-owned-buffers"),
    ("outputAlias", "R13", "Data", "ControllerExecutable => controllerExecutable.ToArray();",
     "ControllerExecutable => controllerExecutable;", "R13/all-owned-buffers"),
    ("noGate", "R14", "Model", "        lock (gate)\n        {\n            try",
     "        {\n            try", "R14/duplicate-issue-race"),
]

def transform(id, model, data):
    if id == "noGate":
        model = once(model, "    private readonly object gate = new();",
                     "    private readonly object gate = new();\n"
                     "    private static readonly Barrier Race = new(2);")
        model = once(model, "                state = nextState.Value;",
                     "                if (nowNs == 1 && RaceEnabled && !Race.SignalAndWait(TimeSpan.FromSeconds(8)))\n"
                     "                    throw new TimeoutException(\"disposable contender barrier\");\n"
                     "                state = nextState.Value;")
        model = once(model, "    private long lastObservedNs;",
                     "    private long lastObservedNs;\n    internal static bool RaceEnabled;")
    return model, data

def put(name, text):
    path = WORK / name
    path.write_text(text, encoding="utf-8", newline="\n")
    return {"name": name, "sha256": sha(path)}

receipts = []
dispatch = []
for name, row, target, old, new, witness in mutations:
    namespace = "Q1Variants." + name
    model = once(SOURCE_MODEL, old, new) if target == "Model" else SOURCE_MODEL
    data = once(SOURCE_DATA, old, new) if target == "Data" else SOURCE_DATA
    model, data = transform(name, model, data)
    model = once(model, "namespace " + NAMESPACE + ";", "using " + NAMESPACE + ";\nnamespace " + namespace + ";")
    data = once(data, "namespace " + NAMESPACE + ";", "using " + NAMESPACE + ";\nnamespace " + namespace + ";")
    probe = re.sub(r"(using \w+ = )" + re.escape(NAMESPACE), r"\1" + namespace, PROGRAM)
    probe = once(probe, "internal static partial class Probe", "namespace " + namespace + ";\ninternal static partial class Probe")
    probe = once(probe, "    private static int passed, failed;", "    private static int passed, failed;\n    private static string? selected;")
    probe = once(probe, "        try\n        {\n            test();",
                 "        if (selected is not null && id != selected) return;\n        try\n        {\n            test();")
    probe = once(probe, "    private static int Main(string[] args)",
                 "    internal static int Run(string? selectedCase)")
    probe = once(probe, '        Console.WriteLine("id\\tresult\\tdetail");',
                 "        selected = selectedCase;\n        passed = failed = 0;")
    if name == "noGate":
        probe = once(probe, "            var left = Start();", "            Model.RaceEnabled = true;\n            var left = Start();")
    files = [put(f"{name}.Model.cs", model), put(f"{name}.Data.cs", data), put(f"{name}.Probe.cs", probe)]
    receipts.append(dict(id=name, requirement=row, witness=witness, nominal="R02/R07/1/nominal",
                         removed=old, inserted=new, files=files))
    dispatch.append(f'''Console.WriteLine("VARIANT\\t{name}\\tnominal");
Environment.ExitCode |= {namespace}.Probe.Run("R02/R07/1/nominal");
Console.WriteLine("VARIANT\\t{name}\\tnegative");
if ({namespace}.Probe.Run("{witness}") != 1) Environment.ExitCode |= 2;''')
put("Dispatch.cs", "\n".join(dispatch) + "\n")
shutil.copyfile(ANNEX / "probe" / "Probe.csproj", WORK / "Mutations.csproj")
(RUN / "independent-derivative-source.json").write_text(json.dumps(receipts, indent=2) + "\n", encoding="utf-8")
env = os.environ.copy()
env.update(NuGetAudit="false", DOTNET_CLI_TELEMETRY_OPTOUT="1", MSBUILDDISABLENODEREUSE="1")
for cfg in ("Debug", "Release"):
    label = cfg.lower()
    core = RUN / "candidate-exact" / "bin" / CORE_NAME / label
    out = RUN / "independent-derivative-build"
    command = ["dotnet", "build", str(WORK / "Mutations.csproj"), "-c", cfg,
               "--artifacts-path", str(out), "--nologo", "-p:DefineConstants=DERIVATIVE", "-p:Core=" + str(core)]
    dll = out / "bin" / "Mutations" / label / (CORE_NAME + ".Tests.dll")
    with (RUN / f"independent-derivative-build-{label}.log").open("wb") as log:
        log.write((json.dumps(command) + "\n").encode())
        log.flush()
        built = subprocess.run(command, env=env, stdout=log, stderr=subprocess.STDOUT, timeout=180)
        log.write(f"\nBUILD EXIT {built.returncode}\n".encode())
        if built.returncode:
            raise SystemExit(built.returncode)
        command = ["dotnet", str(dll)]
        assert sha(dll.with_name(CORE_NAME + ".dll")) == sha(core / (CORE_NAME + ".dll"))
        with (RUN / f"independent-derivative-{label}.tsv").open("wb") as result:
            tested = subprocess.run(command, env=env, stdout=result, stderr=subprocess.STDOUT, timeout=180)
        log.write((json.dumps(dict(command=command, exit=tested.returncode, probe_sha256=sha(dll),
            actual_core_sha256=sha(dll.with_name(CORE_NAME + ".dll")))) + "\n").encode())
        if tested.returncode:
            raise SystemExit(tested.returncode)
    print(f"{cfg}: {len(mutations)} nominal and omission-detection pairs passed")
