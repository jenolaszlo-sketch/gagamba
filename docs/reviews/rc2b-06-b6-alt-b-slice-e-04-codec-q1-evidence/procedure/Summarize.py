import csv
from collections import Counter
import json
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent
run = root / 'run'
logs = run / 'logs'
repo = root.parent.parent / 'Gagamba'
q4 = next(repo.glob('docs/reviews/*03-codec-r2-q4-evidence/raw'))
metrics = ('passed', 'failed', 'skipped', 'errors', 'not-run', 'total')


def cases(assembly, result=None):
    return Counter((t.attrib['name'], t.findtext('reason', default='')) for t in assembly.iter('test')
                   if result is None or t.attrib['result'] == result)


def assembly(path):
    return ET.parse(path).getroot().find('assembly')


summary = {}
for config in ('debug', 'release'):
    verified = {}
    for suite, passed, skipped in (('e4', 11, 0), ('e3', 185, 0), ('broad', 1689, 331)):
        result = assembly(logs / f'{suite}-{config}.xml')
        counts = {m: int(result.attrib[m]) for m in metrics}
        assert tuple(counts.values()) == (passed, 0, skipped, 0, 0, passed + skipped), counts
        trx = ET.parse(logs / f'{suite}-{config}.trx').getroot()
        bad = trx.find('.//{*}Counters')
        assert int(bad.attrib['executed']) == 0, bad.attrib
        verified[suite] = {**counts, 'runner': result.attrib['test-framework'], 'vstestExecuted': 0}
    old_e3 = assembly(q4 / f'combined-focused-{config}.xml')
    assert cases(old_e3) == cases(assembly(logs / f'e3-{config}.xml'))
    old_broad = assembly(q4 / f'broad-{config}.xml')
    broad = assembly(logs / f'broad-{config}.xml')
    assert cases(old_broad, 'Skip') == cases(broad, 'Skip')
    assert cases(broad, 'Pass') == cases(old_broad, 'Pass') + cases(assembly(logs / f'e4-{config}.xml'), 'Pass')
    verified['e3']['membershipMatchesQ4'] = True
    verified['broad']['passMembershipIsQ4PlusE4'] = True
    verified['broad']['nativeSkipMembershipAndReasonsMatchQ4'] = True
    verified['e3']['classCounts'] = dict(Counter(t.attrib['type'].rsplit('.', 1)[-1] for t in assembly(logs / f'e3-{config}.xml').iter('test')))
    with (logs / f'supplement-{config}.tsv').open(newline='', encoding='utf-8') as handle:
        supplemental = list(csv.DictReader(handle, delimiter='\t'))
    assert len(supplemental) == len({r['case'] for r in supplemental}) == 1945
    assert {r['status'] for r in supplemental} == {'PASS'}
    assert supplemental[-1]['case'] == 'sensitivity/disposable-omitted-catalog-check'
    verified['supplemental'] = {'rows': len(supplemental), 'exactCandidateRows': len(supplemental) - 1,
                                'candidatePasses': len(supplemental) - 1, 'sensitivityControls': 1}
    original = (logs / f'replay-original-{config}.log').read_text(encoding='utf-8')
    cand = (logs / f'replay-candidate-{config}.log').read_text(encoding='utf-8')
    supplements = (logs / f'supplement-{config}.log').read_text(encoding='utf-8')
    assert 'counterexamples=17' in original and 'EXIT 1' in original
    assert 'Independent assertions=2383 passed=2383 counterexamples=0' in cand and 'EXIT 0' in cand
    assert 'Q2 supplemental assertions=876 matched=876 unexpected=0' in supplements
    assert 'Q3 supplemental assertions=56 matched=56 unexpected=0' in supplements
    original_outcomes = [json.loads(row) for row in (logs / f'q2-exact-{config}.log').read_text(encoding='utf-8').splitlines() if row.startswith('{')]
    assert len(original_outcomes) == 13 and all(row['result'] == 'Expectation' and not row['published'] and not row['stack'] for row in original_outcomes)
    verified['historicalReplays'] = {'predecessorCounterexamples': 17, 'candidateAssertions': 2383, 'exactQ2BoundedRefusals': 13,
                                     'Q2Supplement': 876, 'Q3Supplement': 56}
    summary[config] = verified

controls = json.loads((run / 'production-controls-retry/results.json').read_text(encoding='utf-8-sig'))
assert len(controls) == 51 and all(r['Passed'] for r in controls)
summary['buildPolicyControls'] = len(controls)
for family in ('e3', 'e4'):
    rows = [json.loads(row) for row in (logs / f'metadata-{family}.jsonl').read_text(encoding='utf-8').splitlines()]
    assert len(rows) == 36 and all(r['pass'] for r in rows)
    test = [r for r in rows if r['assembly'].endswith('.Tests')]
    production = [r for r in rows if not r['assembly'].endswith('.Tests')]
    assert len(test) == 2 and all(r['knownBootstrapCallers'] and r['knownBinaryCallers'] for r in test)
    assert all(not (r['hits'] or r['outbound'] or r['typeRefs'] or r['visibility'] or r['namedResources']) for r in production)
    summary[family + 'Metadata'] = {'inspectedPaths': len(rows), 'testAssembliesWithPositives': len(test),
                                    'productionPaths': len(production), 'productionInboundOrOutboundHits': 0}
assert (logs / 'verifier-debug.log').read_text(encoding='utf-8').strip().endswith('EXIT 1')
assert (logs / 'verifier-release.log').read_text(encoding='utf-8').strip().endswith('EXIT 0')
assert (logs / 'preparation-tests.log').read_text(encoding='utf-8').strip().endswith('EXIT 0')
assert (logs / 'preparation.log').read_text(encoding='utf-8').strip().endswith('EXIT 0')
assert len((logs / 'build-exits.txt').read_text(encoding='utf-8').splitlines()) == 10
print(json.dumps(summary, indent=2))
