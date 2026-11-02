#!/usr/bin/env python3
"""Independent analysis of published .deps.json / .runtimeconfig.json in the candidate."""
import json, os

CAND = r"C:\Users\Laszlos\source\repos\gagamba-b6-s2-q1-review\extract\candidate"

for comp in ("owner", "recovery"):
    entry = "Gagamba.Execution.Linux.M1." + ("Owner" if comp == "owner" else "Recovery")
    deps_path = os.path.join(CAND, comp, entry + ".deps.json")
    rc_path = os.path.join(CAND, comp, entry + ".runtimeconfig.json")
    d = json.load(open(deps_path))
    target = d["runtimeTarget"]["name"]
    print("==", comp, "target:", target)
    runtime_assets = []
    for lib, libval in d["targets"][target].items():
        groups = list(libval.keys())
        for g in groups:
            print("   library", lib, "group", g)
        rt = libval.get("runtime", {})
        for asset in rt:
            runtime_assets.append(asset)
    print("   managed runtime assets:", runtime_assets)
    print("   contains System.IO.Hashing:", any("System.IO.Hashing" in a for a in runtime_assets))
    print("   contains SourceLink:", any("SourceLink" in a for a in runtime_assets))
    print("   native/runtimeTargets assets:", any("native" in libval or "runtimeTargets" in libval
          for libval in d["targets"][target].values()))
    rc = json.load(open(rc_path))
    print("   runtimeconfig:", json.dumps(rc))
