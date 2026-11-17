import hashlib
import json
import subprocess
import sys
from pathlib import Path

run = Path(sys.argv[1]).resolve()
core = "Gagamba.Execution.Linux.M1"
scanner = run / "independent-metadata/bin/Scan/release/Scan.dll"
logs = run / "independent-scan"
logs.mkdir(exist_ok=True)
projects = {core, core+".Owner", core+".Recovery", "InstallTool", "Gagamba.Execution",
            "Gagamba.Runtime", "B2Controller", core+".Tests"}

def assemblies(folder, names=("Gagamba.*.dll", "*.Tool.dll", "InstallTool.dll", "B2Controller.dll")):
    assert folder.is_dir(), folder
    matched = {p for spec in names for p in folder.glob(spec)}
    assert matched, folder
    return matched

def scan(label, candidates):
    paths = logs / (label + "-paths.txt")
    paths.write_text("".join(f"{p}\n" for p in sorted(candidates)), encoding="utf-8")
    command = ["dotnet", str(scanner), str(paths)]
    output = logs / (label + ".jsonl")
    error = logs / (label + ".log")
    with output.open("wb") as data, error.open("wb") as log:
        log.write((json.dumps(command) + "\n").encode())
        log.flush()
        proc = subprocess.run(command, stdout=data, stderr=log)
        log.write(f"EXIT {proc.returncode}\n".encode())
    rows = [json.loads(line) for line in output.read_text(encoding="utf-8").splitlines()]
    assert len(rows) == len(candidates), (label, len(rows))
    return proc.returncode, rows

summary = {
    "scanner_sha256": hashlib.sha256(scanner.read_bytes()).hexdigest(),
    "configurations": {},
}
bad = []
for cfg in ("debug", "release"):
    bins = run / "candidate-exact/bin"
    layout = run / "layouts" / cfg
    build = set().union(*(assemblies(bins / component / cfg) for component in projects))
    copies = set().union(*(assemblies(folder) for folder in
        (layout / "focused", layout / "tests" / cfg, layout / f"src/{core}/bin/Release/net10.0")))
    published = {
        assembly
        for component in ["m1", "owner", "recovery"]
        for assembly in (run / "policy-exact" / f"publish-{component}" / "publish").rglob("Gagamba.*.dll")
    } if cfg == "release" else set()
    assert cfg != "release" or len(published) == 8
    all_paths = build | copies | published
    expected_count = 31 if cfg == "debug" else 39
    assert len(all_paths) == expected_count
    code, rows = scan(cfg, all_paths)
    producer = [json.loads(line) for line in
                (run / "audit" / f"metadata-{cfg}.jsonl").read_text(encoding="utf-8").splitlines()]
    assert {(Path(r["path"]), r["sha256"]) for r in rows} == {(Path(r["file"]), r["sha256"]) for r in producer}
    for row in rows:
        bad.extend(row["errors"])
    external = {
        item["From"].split("::")[0].split("+")[0]
        for row in rows for item in row["edges"]
        if "ValidationCustody" in item["To"] and "ValidationCustody" in item["From"]
        and row["assembly"].endswith(".Tests") and item["Kind"].startswith("IL ")
    }
    assert any("ValidationCustodyModelCompositionTests" in s for s in external)
    assert any("ValidationCustodyModelLifecycleTests" in s for s in external)
    assert any("ValidationCustodyModelTests" in s for s in external)
    for entry in ("ValidationPreCodec::DecodeBootstrap", "ValidationPreCodecBinary::ValidatePayload",
                  "Gava1ContextCodec::Decode", "Gava1ContextCodec::MatchPreBootstrap"):
        assert any(entry in e["To"] for r in rows if r["assembly"].endswith(".Tests") for e in r["edges"]), entry
    expected_p4 = ("RecoveryInitializationAdmission::Require", "IntegratedRecoveryHost::Run",
                   "ReleaseGenerationGate::", "ReleaseArbiter::")
    for sink in expected_p4:
        assert any(sink in e["To"] for x in rows if not x["assembly"].endswith(".Tests") for e in x["edges"]), sink
    exact = bins / core / cfg / (core+".dll")
    variant = run / "independent-il-control" / cfg / f"bin/{core}/{cfg}/{core}.dll"
    control_exit, control = scan(cfg+"-deliberate-inbound", {variant})
    assert control_exit == 1 and control[0]["sha256"] != hashlib.sha256(exact.read_bytes()).hexdigest()
    assert any("CustodyQualification.Control.IncomingReference::MissingInputs IL call" in e
        and "ValidationCustodyModel::TryCreate" in e for e in control[0]["errors"]), control[0]["errors"]
    summary["configurations"][cfg] = {
        "exact_exit": code,
        "paths": len(all_paths),
        "production": sum(not row["assembly"].endswith(".Tests") for row in rows),
        "test": sum(row["assembly"].endswith(".Tests") for row in rows),
        "assembly_hash_identity_types": len({row["sha256"] for row in rows}),
        "matched_reproduction_inventory": True,
        "authorized_test_references": sorted(external),
        "control_exit": control_exit,
        "control_errors": control[0]["errors"],
        "control_sha256": control[0]["sha256"],
        "exact_errors": sorted({e for row in rows for e in row["errors"]}),
    }
(logs / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
if bad:
    print("\n".join(sorted(set(bad))[:25]))
    sys.exit(1)
print("70 exact assembly paths; authorized test references and both independent control detections verified.")
