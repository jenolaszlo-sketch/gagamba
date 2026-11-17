"""Review pinned owner-acceptance evidence without executing qualification suites."""
from collections import Counter
import csv
import gzip
import hashlib
import io
import json
from pathlib import Path
import re
import runpy
import subprocess
import sys
import xml.etree.ElementTree as ET

REPO = Path(r"C:/Users/Laszlos/source/repos/Gagamba")
STEM = "rc2b-06-b6-alt-b-slice-e-05"
START = "b6631367d39531b2c5af8a1917760d7c807f8e73"
START_TREE = "b62300eb6b3093ecd7857f8bb6c82eda529c2a15"
PUBLISHED = "1116c66d7858e6000f0001a0abe39319e4e694be"
CANDIDATE = "28895d55de98eea4bc402373b653f7216e092843"
QPATH = "docs/reviews/" + STEM + "-custody-q1-evidence"
Q1 = REPO / QPATH
RAW = Q1 / "raw"
QHELP = runpy.run_path(str(Q1 / "verify-integrity.py"))
GET = QHELP["get"]
BLOBS = QHELP["blobs"]
VERIFY = QHELP["verify_manifest"]

def git(*args):
    return subprocess.run(["git","--no-optional-locks","-C",str(REPO),*args],
        capture_output=True,check=True).stdout.decode().strip()

def digest(data):
    return hashlib.sha256(data).hexdigest()

def load(path):
    return json.loads(path.read_bytes())

def uncompressed(path):
    return gzip.decompress(path.read_bytes()) if path.suffix == ".gz" else path.read_bytes()

def rows(path):
    return list(csv.DictReader(io.StringIO(uncompressed(path).decode("utf-8-sig")),delimiter="\t"))

def suite(path):
    body = uncompressed(path)
    assembly = ET.fromstring(body).find("assembly")
    counts = {k:int(assembly.get(k)) for k in ["total","passed","failed","skipped","errors","not-run"]}
    cases = Counter((t.get("name"),t.get("result"),t.findtext("reason",default="")) for t in assembly.iter("test"))
    return body,counts,cases

def check_repository(expected_head, expected_remote):
    head = git("rev-parse","HEAD")
    assert head == expected_head and git("symbolic-ref","HEAD")=="refs/heads/main"
    assert git("rev-parse",f"{START}^{{tree}}")==START_TREE
    assert git("rev-parse",f"{CANDIDATE}^{{tree}}")=="e314f2e8a1259e2263c8494bb0c8ca9f7eb27016"
    assert git("remote","get-url","origin")==git("remote","get-url","--push","origin")=="https://github.com/jenolaszlo-sketch/gagamba.git"
    assert git("ls-remote","--exit-code","origin","refs/heads/main")==expected_remote+"\trefs/heads/main"
    subprocess.run(["git","--no-optional-locks","-C",str(REPO),"merge-base","--is-ancestor",START,head],check=True)
    assert git("show","-s","--format=%P",START)=="0a77422c1349b99e518292d46314606b383527e4"
    preflight = load(Q1/"preflight.json")
    summaries = []
    for spec in preflight["historical_manifests"]+[preflight["producer_manifest"]]+[
        dict(path=QPATH+"/SHA256SUMS.txt",revision=START,count=189,
             sha256="3e57695efbf302bf15bf50dd32f502f3b3832a7d37d93fde28e06e91a756731b")]:
        verified = VERIFY(spec,head)
        summaries.append({k:verified[k] for k in ["path","revision","count","sha256"]})
    assert sum(s["count"] for s in summaries)==1044
    candidate_files=[]
    for item in preflight["candidate_files"]:
        p=item["path"]
        data=GET(CANDIDATE,p)
        assert data==GET(head,p)==(REPO/p).read_bytes() and digest(data)==item["sha256"]
        candidate_files.append(item)
    for a in preflight["artifact_bindings"]:
        _,blob,data=next(BLOBS(head,[a["path"]]))
        assert blob==a["blob"] and digest(data)==a["sha256"]
    scope=git("diff","--name-status","7a30d0a05a7c898c4e90cc2ca01280db166ed6f2",CANDIDATE).splitlines()
    assert set(scope)=={"A\t"+x["path"] for x in candidate_files} and len(scope)==6
    assert not git("diff","--name-only",CANDIDATE,head,"--","src","tests","eng",".github","native","Gagamba.sln","Directory.Build.props","Directory.Build.targets")
    full_status = subprocess.run(["git","--no-optional-locks","-C",str(REPO),
        "status","--porcelain=v1","-z","--untracked-files=all"],capture_output=True,check=True).stdout.decode().split("\0")[:-1]
    untracked = preflight["historical_untracked"]
    assert len(untracked)==44 and {x["path"] for x in untracked}=={s[3:] for s in full_status}
    assert all(s.startswith("?? ") for s in full_status)
    for x in untracked:
        data=(REPO/x["path"]).read_bytes()
        assert len(data)==x["length"] and digest(data)==x["sha256"]
    assert not git("stash","list")
    worktrees=git("worktree","list","--porcelain")
    assert worktrees.count("worktree ")==1
    owner_delta=git("diff","--name-status",START,head).splitlines()
    if head!=START:
        assert git("show","-s","--format=%P",head)==START
        assert owner_delta and all(x.startswith("A\tdocs/decisions/"+STEM+"-owner-01") for x in owner_delta)
    return dict(head=head,tree=git("rev-parse","HEAD^{tree}"),branch="main",remote=expected_remote,
        origin=git("remote","get-url","origin"),tracked_checkout_and_index="clean",worktrees=1,stashes=0,
        manifest_entries=1044,manifest_sets=summaries,historical_untracked_count=44,historical_untracked_match=True,
        candidate_files=candidate_files,frozen_artifacts=preflight["artifact_bindings"],owner_delta=owner_delta)

def check_execution():
    expected={"e3":185,"e4":11,"e5":42,"broad":1731}
    summary=load(RAW/"execution-summary.json")
    observations={}
    producer = REPO/"docs/evidence"/(STEM+"-custody-implementation-01-evidence/raw")
    producer_members=rows(producer/"managed-membership.tsv")
    previous=REPO/"docs/reviews/rc2b-06-b6-alt-b-slice-e-04-codec-q1-evidence/raw"
    for cfg in ["debug","release"]:
        record={}
        memberships={}
        for name,count in expected.items():
            body,counts,tests=suite(RAW/f"results/{cfg}/{name}.xml.gz")
            assert counts==dict(total=count+(331 if name=="broad" else 0),passed=count,failed=0,
                               skipped=331 if name=="broad" else 0,errors=0,**{"not-run":0})
            receipt=summary[cfg][name]
            assert digest(body)==receipt["xml_sha256"] and receipt["xunit_exit"]==0
            assert receipt["runner"]=="xUnit.net v3 3.2.0+9e422989c1"
            trx=ET.fromstring((RAW/f"results/{cfg}/{name}.trx").read_bytes())
            assert trx.find(".//{*}Counters").get("executed")=="0"
            vstest=(RAW/f"results/{cfg}/vstest-{name}.log").read_text()
            assert "failed to connect to testhost process after 15 seconds" in vstest and vstest.rstrip().endswith("EXIT 1")
            assert (RAW/f"results/{cfg}/xunit-{name}.log").read_text().rstrip().endswith("EXIT 0")
            memberships[name]=tests
            record[name]=counts|{"xunit_exit":0,"vstest_exit":1,"vstest_executed":0}
        broad=memberships["broad"]
        for focus in ["e3","e4","e5"]:
            assert not (memberships[focus]-broad)
        for name in ["e3","e4","broad"]:
            _,_,historical=suite(previous/f"{name}-{cfg}.xml")
            current=memberships[name] if name!="broad" else broad-memberships["e5"]
            assert current==historical,(cfg,name)
        preserved=Counter()
        for item in producer_members:
            if item["configuration"]==cfg:
                preserved[item["case"],item["result"],item["skip_reason"]]+=int(item["multiplicity"])
        assert preserved==broad
        supplemental=rows(RAW/f"logs/supplement-{cfg}.tsv.gz")
        assert len(supplemental)==1945 and all(x["status"]=="PASS" for x in supplemental)
        assert sum(x["case"]=="sensitivity/disposable-omitted-catalog-check" for x in supplemental)==1
        frozen=rows(RAW/f"sensitivity-{cfg.title()}.tsv")
        assert len(frozen)==17 and all(x["decision"]=="PASS" for x in frozen)
        assert frozen[-1]["derivative"]=="Threw/Null/Transition"
        independent=(RAW/f"independent-{cfg}.tsv").read_text().splitlines()
        assert independent[-1]=="TOTAL\t696\t0" and len(independent)==699
        exact_cases=dict(line.split("\t") for line in independent[2:-1])
        assert len(exact_cases)==696 and set(exact_cases.values())=={"PASS"}
        variants=load(RAW/"independent-derivative-source.json")
        altered=(RAW/f"independent-derivative-{cfg}.tsv").read_text().splitlines()
        assert len(variants)==20 and len(altered)==160
        for i,v in enumerate(variants):
            chunk=altered[i*8:i*8+8]
            assert chunk[0]==f"VARIANT\t{v['id']}\tnominal" and chunk[2]==v["nominal"]+"\tPASS"
            assert chunk[3]=="TOTAL\t1\t0"
            assert chunk[4]==f"VARIANT\t{v['id']}\tnegative" and chunk[6].startswith(v["witness"]+"\tFAIL\t")
            assert chunk[7]=="TOTAL\t0\t1" and exact_cases[v["witness"]]=="PASS"
        verifier=(RAW/f"audit/verifier-{cfg}.log").read_text()
        assert ("RESULT: FAIL" in verifier and "expected Release" in verifier and verifier.rstrip().endswith("EXIT 1")) if cfg=="debug" else (
            "RESULT: PASS" in verifier and verifier.rstrip().endswith("EXIT 0"))
        record.update(E4_supplement_exact_passes=1944,E4_disposable_omission_detected=1,
            frozen_sensitivity_passes=17,independent_passes=696,independent_omissions_detected=20,
            nominal_omission_controls_passed=20,production_verifier_exit=1 if cfg=="debug" else 0)
        observations[cfg]=record
    policies=load(RAW/"policy-exact/results.json")
    assert len(policies)==51 and all(x["Passed"] for x in policies)
    prep=(RAW/"preparation-tests.log").read_text()
    assert "Ran 9 tests" in prep and "OK" in prep and prep.rstrip().endswith("EXIT 0")
    assert "Passed" in (RAW/"preparation.log").read_text() and (RAW/"preparation.log").read_text().rstrip().endswith("EXIT 0")
    observations["policy"]={"controls_passed":51,"preparation_regressions_passed":9,"preparation_passed":True}
    return observations

def check_isolation():
    summary=load(RAW/"independent-scan/summary.json")
    results={}
    for cfg,expected in [("debug",31),("release",39)]:
        s=summary["configurations"][cfg]
        assert s["paths"]==expected and s["production"]==expected-3 and s["test"]==3
        assert s["exact_exit"]==0 and not s["exact_errors"] and s["control_exit"]==1
        lines=uncompressed(RAW/f"independent-scan/{cfg}.jsonl.gz").decode().splitlines()
        inspected=[json.loads(x) for x in lines]
        assert len(inspected)==expected and all(not x["errors"] for x in inspected)
        reproduced=[json.loads(x) for x in uncompressed(RAW/f"audit/metadata-{cfg}.jsonl.gz").decode().splitlines()]
        assert {(x["path"],x["sha256"]) for x in inspected}=={(x["file"],x["sha256"]) for x in reproduced}
        negative=load(RAW/f"independent-scan/{cfg}-deliberate-inbound.jsonl")
        assert negative["sha256"]==s["control_sha256"] and any("IncomingReference::MissingInputs IL call" in e for e in negative["errors"])
        assert all(x.startswith("Gagamba.Execution.Linux.M1.Tests.Bootstrap.ValidationCustodyModel") for x in s["authorized_test_references"])
        p4=rows(RAW/"p4-executed.tsv")
        assert sum(x["configuration"]==cfg and x["result"]=="Pass" for x in p4)==47
        results[cfg]={"assembly_paths":expected,"production_paths":expected-3,"test_paths":3,
                      "unauthorized_model_edges":0,"new_production_codec_consumers":0,
                      "independent_incoming_control_exit":1,"P4_witnesses_passed":47}
    source=load(RAW/"independent-p4-source.json")
    assert source["candidate"]==CANDIDATE and source["compared_to_published"]==PUBLISHED
    assert [sum(x["candidate"]==value for x in source["p4_dispositions"]) for value in
            ["COVERED","AUTHORIZED CONTINUATION","LEGACY / TEST-ONLY"]]==[21,5,3]
    for path,record in source["first_effect_bindings"].items():
        assert git("rev-parse",CANDIDATE+":"+path)==git("rev-parse",PUBLISHED+":"+path)==record["blob"]
    results["P4"]={"covered":21,"authorized_continuation":5,"legacy_test_only":3,
                  "disposition":"Neutral within the existing refusal-only scope"}
    return results

if __name__=="__main__":
    output=Path(sys.argv[1])
    head=sys.argv[2] if len(sys.argv)>2 else START
    remote=sys.argv[3] if len(sys.argv)>3 else PUBLISHED
    record={"repository":check_repository(head,remote),"execution":check_execution(),"isolation":check_isolation()}
    output.write_text(json.dumps(record,indent=2)+"\n",encoding="utf-8")
    print("Owner evidence PASS: 1,044 manifest entries, 44 preserved files, all execution and isolation gates.")
