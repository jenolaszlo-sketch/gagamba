import collections
import csv
import hashlib
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

repo, scratch = (Path(x).resolve() for x in sys.argv[1:3])
historical = repo / "docs/reviews/rc2b-06-b6-alt-b-slice-e-04-codec-q1-evidence/raw"
ledger = scratch / "managed-membership.tsv"
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
def load(path):
    root = ET.parse(path)
    assembly = root.getroot().find("assembly")
    cases = [(t.get("name"), t.get("result"), t.findtext("reason", default="").strip())
             for t in assembly.findall(".//test")]
    counts = {key: int(assembly.get(key)) for key in ("total", "passed", "failed", "skipped", "errors")}
    assert counts["total"] == len(cases)
    assert all(name and result in ("Pass", "Skip") for name, result, _ in cases)
    assert counts["failed"] == counts["errors"] == 0
    return counts, collections.Counter(cases)

totals = {}
with ledger.open("w", encoding="utf-8", newline="") as out:
    writer = csv.writer(out, delimiter="\t", lineterminator="\n")
    writer.writerow(["configuration", "case", "result", "skip_reason", "multiplicity", "focus_membership"])
    for config in ("debug", "release"):
        res = scratch / "results" / config
        totals[config] = {}
        cases = {}
        for suite in ("e5", "e4", "e3", "broad"):
            path = res / (suite + ".xml")
            counts, case_set = load(path)
            cases[suite] = case_set
            v_log = (res / f"vstest-{suite}.log").read_text(encoding="utf-8")
            v_trx = ET.parse(res / (suite + ".trx"))
            counter = v_trx.find(".//{*}Counters")
            assert counter is not None and counter.get("executed") == "0"
            assert "VSTest version 18.7.0" in v_log and "failed to connect to testhost process after 15 seconds" in v_log
            assert "Test Run Aborted" in v_log and "EXIT 1" in v_log
            totals[config][suite] = dict(**counts, xml_sha256=sha(path), vstest_executed=0, vstest_exit=1,
                                         vstest_trx_sha256=sha(res / (suite + ".trx")))
            if suite in ("e4", "e3"):
                hist_counts, hist_cases = load(historical / f"{suite}-{config}.xml")
                assert counts == hist_counts and case_set == hist_cases, (suite, config)
        hist_counts, hist_cases = load(historical / f"broad-{config}.xml")
        assert hist_counts["passed"] == 1689 and hist_counts["skipped"] == 331
        assert cases["e4"].total() == 11 and cases["e3"].total() == 185
        assert cases["e5"].total() == 42 and all(case[1] == "Pass" for case in cases["e5"])
        assert (cases["broad"] == hist_cases + cases["e5"] and
                not hist_cases.keys() & cases["e5"].keys()), config
        assert not any(cases[n].keys() & cases[m].keys()
                       for n, m in [("e3", "e4"), ("e4", "e5"), ("e3", "e5")])
        assert all(cases[name] <= cases["broad"] for name in ("e3", "e4", "e5"))
        for case, count in sorted(cases["broad"].items()):
            writer.writerow([config, *case, count, ",".join(n for n in ("e3", "e4", "e5") if case in cases[n])])
        totals[config]["historical_xml"] = {
            suite: sha(historical / f"{suite}-{config}.xml") for suite in ("e4", "e3", "broad")
        }
        supplement = scratch / "logs" / f"supplement-{config}.tsv"
        with supplement.open(encoding="utf-8-sig") as f:
            supp = list(csv.DictReader(f, delimiter="\t"))
        assert len(supp) == 1945 and all(row["status"] == "PASS" for row in supp)
        disposable = [row for row in supp if row["case"] == "sensitivity/disposable-omitted-catalog-check"]
        assert len(disposable) == 1
        assert disposable[0]["observed"] == "None" and disposable[0]["output"] == "True"
        totals[config]["supplement"] = dict(exact=1944, disposable=1, sha256=sha(supplement))
        sens = scratch / f"sensitivity-{config.title()}.tsv"
        with sens.open(encoding="utf-8") as f:
            variants = list(csv.DictReader(f, delimiter="\t"))
        assert len(variants) == 17 and all(v["decision"] == "PASS" for v in variants)
        assert all(v["exact"] == v["expected_exact"] and v["derivative"] == v["expected_derivative"]
                   for v in variants)
        totals[config]["sensitivity"] = dict(behavioral=16, exception=1, sha256=sha(sens))
controls = json.loads((scratch / "policy-exact" / "results.json").read_text(encoding="utf-8-sig"))
accepted = json.loads((historical / "production-controls.json").read_text(encoding="utf-8-sig"))
identities = lambda rows: sorted((row["Name"], row["Exit"], row["Expected"]) for row in rows)
assert len(controls) == len({row["Name"] for row in controls}) == 51
assert all(row["Passed"] for row in controls) and identities(controls) == identities(accepted)
totals["policy_probes"] = len(controls)
(scratch / "suite-summary.json").write_text(json.dumps(totals, indent=2) + "\n", encoding="utf-8", newline="\n")
for config in ("debug", "release"):
    print(config, {suite: totals[config][suite]["passed"] for suite in ("e3", "e4", "e5", "broad")},
          "331 unchanged skips, 1944 exact E-4 supplement, 17 sensitivity controls")
print("51 unchanged build-policy controls matched accepted identity, exit and expected diagnostic.")
