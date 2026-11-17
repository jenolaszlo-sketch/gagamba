import csv
from collections import Counter
import gzip
import hashlib
import json
from pathlib import Path
import re
import shutil
import sys
import xml.etree.ElementTree as ET

repo, run, evidence = (Path(x).resolve() for x in sys.argv[1:4])
out = evidence / "raw"
out.mkdir(exist_ok=True)
stem = "rc2b-06-b6-alt-b-slice-e-"
producer = repo / "docs/evidence" / (stem + "05-custody-implementation-01-evidence/raw")
previous = repo / "docs/reviews" / (stem + "04-codec-q1-evidence/raw")
p4 = list(csv.DictReader((run/"p4-controls.tsv").open(encoding="utf-8"), delimiter="\t"))
snapshot_cases = list(csv.DictReader((producer/"managed-membership.tsv").open(encoding="utf-8"), delimiter="\t"))
baseline_counts = {"e3": 185, "e4": 11, "e5": 42, "broad": 1731}
summary = {}

def xml_cases(path):
    assembly = ET.parse(path).getroot().find("assembly")
    tests = list(assembly.iter("test"))
    ids = Counter((item.get("name"), item.get("result"), item.findtext("reason", default="")) for item in tests)
    return assembly.attrib, ids, tests

def hash_file(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def keep(relative, compress=False):
    path = run / relative
    target = out / Path(relative)
    target.parent.mkdir(parents=True, exist_ok=True)
    if compress:
        target = target.with_name(target.name + ".gz")
        target.write_bytes(gzip.compress(path.read_bytes(), mtime=0))
    else:
        shutil.copyfile(path,target)

members = []
p4_review = []
timed_out = []
for cfg in ("debug","release"):
    cfg_summary = {}
    summary[cfg] = cfg_summary
    paths = run / "results" / cfg
    memberships = {}
    for suite, baseline in baseline_counts.items():
        xml = paths / (suite + ".xml")
        ctrs, ids, tests = xml_cases(xml)
        memberships[suite] = ids
        expect_skips = 331 if suite == "broad" else 0
        assert {k:int(ctrs[k]) for k in ("passed","skipped","failed","errors","not-run","total")} == dict(
            passed=baseline, skipped=expect_skips, failed=0, errors=0, total=baseline+expect_skips, **{"not-run":0})
        trx = paths / (suite + ".trx")
        actual = ET.parse(trx).find(".//{*}Counters")
        assert actual is not None and actual.get("executed") == "0"
        vstest_log = (paths/f"vstest-{suite}.log").read_text(encoding="utf-8")
        testhost_pid = re.search(r"process with id (\d+)", vstest_log).group(1)
        assert "failed to connect to testhost process after 15 seconds" in vstest_log and vstest_log.rstrip().endswith("EXIT 1")
        timed_out.append(dict(config=cfg,suite=suite,pid=int(testhost_pid),exit=1,executed=0))
        xunit_log = (paths/f"xunit-{suite}.log").read_text(encoding="utf-8")
        assert xunit_log.rstrip().endswith("EXIT 0")
        cfg_summary[suite] = dict(xunit_exit=0, passed=baseline, failed=0, skipped=expect_skips, vstest_exit=1,
                                  vstest_executed=0, runner=ctrs["test-framework"], xml_sha256=hash_file(xml),
                                  assembly=ctrs["name"])
        for name in (suite+".xml", suite+".trx", f"vstest-{suite}.log", f"xunit-{suite}.log"):
            keep(f"results/{cfg}/{name}", compress=name.endswith(".xml"))

    broad = memberships["broad"]
    e5 = memberships["e5"]
    assert not (e5-broad) and not (memberships["e4"]-broad) and not (memberships["e3"]-broad)
    for suite, actual in [("broad", broad - e5), ("e3", memberships["e3"]), ("e4", memberships["e4"])]:
        _, accepted, _ = xml_cases(previous/f"{suite}-{cfg}.xml")
        assert actual == accepted, (cfg,suite,list((actual-accepted).items())[:4], list((accepted-actual).items())[:4])
    pg = Counter()
    expected_focused = {}
    for row in snapshot_cases:
        if row["configuration"] == cfg:
            key = row["case"],row["result"],row["skip_reason"]
            pg[key] += int(row["multiplicity"])
            expected_focused[key] = row["focus_membership"]
    assert broad == pg
    for (name,result,reason), number in sorted(broad.items()):
        focused = [s for s in ["e3","e4","e5"] if memberships[s][name,result,reason]]
        assert expected_focused[name,result,reason] == ",".join(focused)
        members.append([cfg,name,result,reason,number,",".join(focused)])
    keep(f"results/{cfg}/bindings.json")

    targets = {row["executed_case"] for row in p4 if row["configuration"] == cfg}
    for name,result,reason in broad:
        selected = name in targets or ".OwnerReleaseGenerationGateArbiterTests." in name or ".ReleaseArbiterTests." in name
        if selected:
            p4_review.append(dict(configuration=cfg,case=name,result=result,skip_reason=reason))
    assert len(targets) == 15 and all(broad[name,"Pass",""] for name in targets)
    e4 = list(csv.DictReader((run/f"logs/supplement-{cfg}.tsv").open(encoding="utf-8"), delimiter="\t"))
    assert len(e4) == 1945 and all(row["status"]=="PASS" for row in e4)
    derivative = [r for r in e4 if r["case"]=="sensitivity/disposable-omitted-catalog-check"]
    assert len(derivative)==1
    cfg_summary["E4_supplement"] = dict(exact=len(e4)-len(derivative), historical_omission_control=len(derivative), exit=0)
    frozen = list(csv.DictReader((run/f"sensitivity-{cfg.title()}.tsv").open(encoding="utf-8"), delimiter="\t"))
    assert len(frozen)==17 and all(r["decision"]=="PASS" for r in frozen)
    cfg_summary["E5_sensitivity"] = dict(reproduced=17, exit=0)
    q1_lines = (run/f"independent-{cfg}.tsv").read_text(encoding="utf-8").splitlines()
    outcomes = dict(line.split("\t") for line in q1_lines[2:-1])
    assert len(outcomes)==696 and list(outcomes.values())==["PASS"]*696 and q1_lines[-1]=="TOTAL\t696\t0"
    cfg_summary["independent"] = dict(passed=696, failed=0, exit=0)
    mut_lines = (run/f"independent-derivative-{cfg}.tsv").read_text(encoding="utf-8").splitlines()
    source = json.loads((run/"independent-derivative-source.json").read_bytes())
    assert len(source)==20 and len(mut_lines)==160
    for i,item in enumerate(source):
        nominal, omitted = mut_lines[i*8:i*8+4], mut_lines[i*8+4:i*8+8]
        assert nominal[0]==f"VARIANT\t{item['id']}\tnominal" and nominal[2]==item["nominal"]+"\tPASS"
        assert omitted[0]==f"VARIANT\t{item['id']}\tnegative" and omitted[2].startswith(item["witness"]+"\tFAIL\t")
        assert outcomes[item["witness"]]=="PASS", item["id"]
    assert "duplicate outcomes None/IssuedModel, None/IssuedModel" in "\n".join(mut_lines)
    cfg_summary["independent_sensitivity"] = dict(nominal_positive=20, detected_omissions=20, exit=0)
    summary[cfg]["P4_reproduction_witnesses"] = len(targets)
    summary[cfg]["P4_additional_broad_witnesses"] = sum(r["configuration"]==cfg and r["case"] not in targets
                                                        and r["result"]=="Pass" for r in p4_review)

with (out/"managed-membership.tsv").open("w",encoding="utf-8",newline="") as f:
    w = csv.writer(f,delimiter="\t",lineterminator="\n")
    w.writerow(["configuration","case","result","skip_reason","multiplicity","focus_membership"])
    w.writerows(members)
with (out/"p4-executed.tsv").open("w",encoding="utf-8",newline="") as f:
    w = csv.DictWriter(f,fieldnames=["configuration","case","result","skip_reason"],delimiter="\t",lineterminator="\n")
    w.writeheader()
    w.writerows(p4_review)

for folder in ("build-exact","logs","audit","independent-scan"):
    for path in sorted((run/folder).iterdir()):
        assert path.is_file()
        keep(str(path.relative_to(run)), compress=path.stat().st_size>200_000)
for path in sorted(run.iterdir()):
    if path.is_file() and path.suffix in {".log",".tsv",".json"}:
        keep(path.name)
policy = json.loads((run/"policy-exact/results.json").read_bytes())
assert len(policy)==51 and all(r["Passed"] for r in policy)
for path in sorted((run/"policy-exact").iterdir()):
    if path.is_file() and path.suffix in {".log",".json",".txt"}:
        keep(str(path.relative_to(run)), compress=path.stat().st_size>200_000)
assert (run/"preparation-tests.log").read_text().rstrip().endswith("EXIT 0")
assert (run/"preparation.log").read_text().rstrip().endswith("EXIT 0")
summary["build_policy"] = dict(probes=51, passed=51, exit=0, preparation_regressions=9, preparation_exit=0)
summary["runner_receipts"] = timed_out
(out/"execution-summary.json").write_text(json.dumps(summary,indent=2)+"\n",encoding="utf-8")
print("Debug/Release exact runner membership, E-4 historical regressions, Q1 cases, 20 omissions and policy counts verified.")
