from collections import Counter
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

scratch, q1 = (Path(arg).resolve() for arg in sys.argv[1:3])
logs = scratch/'run/logs'
stem = 'rc2b-06-b6-alt-b-slice-e-03-codec-r1'
evidence = scratch/'package/docs/evidence'/(stem+'-evidence')
raw = evidence/'raw'
raw.mkdir(parents=True, exist_ok=True)
old = q1/'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence'
configs = ('debug', 'release')

def load_rows(path):
    return {row['test']: row for row in map(json.loads, path.read_text(encoding='utf-8-sig').splitlines())}

def copy_text(src, dest):
    # Normalize producer evidence line endings for a portable checkout.
    dest.write_text(src.read_text(encoding='utf-8-sig'), encoding='utf-8', newline='\n')

def write_json(path, obj):
    path.write_text(json.dumps(obj, indent=2)+'\n', encoding='utf-8', newline='\n')

records, roster, outcomes = {}, {}, {}
for cfg in configs:
    original = load_rows(old/f'vectors-{cfg}/results.jsonl')
    before = load_rows(scratch/f'run/probe-before-{cfg}/results.jsonl')
    after = load_rows(scratch/f'run/probe-after-{cfg}/results.jsonl')
    assert original.keys() == before.keys() == after.keys()
    assert len(after) == 2383
    failing = [n for n in original if not original[n]['matched']]
    assert len(failing) == 17 and set(failing) == {n for n in before if not before[n]['matched']}
    assert all(row['matched'] for row in after.values())
    write_json(raw/f'q1-replay-{cfg}.json', {
        n: dict(original=original[n], before=before[n], after=after[n]) for n in failing})
    count = {}
    for group, numbers in (
        ('regression-before', (24, 3, 21, 0)),
        ('regression-after', (24, 24, 0, 0)),
        ('focused-original', (146, 146, 0, 0)),
        ('managed-broad', (1994, 1663, 0, 331)),
    ):
        path = logs/f'{group}-{cfg}.xml'
        assembly = ET.parse(path).find('assembly')
        summary = {key: int(assembly.get(key)) for key in ('total','passed','failed','skipped','not-run','errors')}
        tests = list(assembly.iter('test'))
        assert tuple(summary[key] for key in ('total','passed','failed','skipped')) == numbers
        assert summary['not-run'] == summary['errors'] == 0
        if group == 'focused-original':
            assert Counter(x.get('type').split('.')[-1] for x in tests) == {
                'ValidationPreCodecBootstrapTests': 64, 'ValidationPreCodecBinaryTests': 58, 'InstallationWireTests': 24}
        if group == 'managed-broad':
            prior = [x for x in tests if 'CompiledReleaseAssemblyHasNoProductionCaller' in x.get('name')]
            assert len(prior) == 2 and all(x.get('result') == 'Pass' for x in prior)
            assert all('repository root not found' not in (x.findtext('reason') or '') for x in tests)
        if group.startswith('regression'):
            roster[cfg,group] = tests
        count[group] = summary
        copy_text(path,raw/path.name)
    outcomes[cfg] = count
    for name in failing:
        assert original[name] == before[name], name
        assert before[name]['vector'] == after[name]['vector'], name
        records.setdefault(name,{})[cfg] = (original[name]['actual'], after[name]['actual'])
    copy_text(scratch/f'run/probe-after-{cfg}/results.jsonl',raw/f'q1-all-after-{cfg}.jsonl')

def regression(name):
    if '/expectation-' in name:
        _, _, _, member, change = name.split('/')
        missing = change.endswith('missing')
        kind = 'Missing' if missing else 'Contradictory'
        return ('F01', f'IntegratedRefuses{kind}PolicyExpectation', member,
                'Required expected member absent' if missing else 'Independent value contradicts the wire',
                'Expectation' if missing else 'Comparison')
    if 'ambiguous-original-session' in name:
        return ('F02','RefusesConflictingDecodedSessionExpectation','topology: '+('1' if name.startswith('B2/') else '2'),
                'Conflicting literal and escaped names decode to Session','Expectation')
    if 'disposed-original' in name:
        return ('F03','RefusesDisposedProjection','topology: '+('1' if name.startswith('B2/') else '2'),
                'Caller disposed the projection document','Expectation')
    assert name == 'binary-expectation-alias/replaced', name
    return ('F04','ConstructorBindsOriginalArtifactDigest',None,
            'Caller overwrote the retained artifact-digest array','Comparison')

tick = chr(96)
matrix = ['# Q1 counterexample to R1 regression matrix', '',
    'Each row replays the identical Q1 vector against both original and corrected binaries. '
    'The same new xUnit test DLL exercised each failure mechanism against the original and corrected core M1 DLLs.',
    '',
    '| Q1 finding and ledger identifier | Failure mechanism | Original actual (D/R) | Expected R1 refusal | Actual R1 (D/R) | Regression test | Debug before/after | Release before/after |',
    '|---|---|---|---|---|---|---|---|']
for name in sorted(records, key=lambda x: (regression(x)[0],x)):
    finding, method, parameter, mechanism, expected = regression(name)
    observed = records[name]
    assert observed['debug'][1] == observed['release'][1] == expected
    statuses = []
    for cfg in configs:
        for group in ('regression-before','regression-after'):
            match = [t for t in roster[cfg,group] if t.get('method') == method
                     and (parameter is None or re.search(r'\b'+re.escape(parameter)+r'\b', t.get('name')))]
            assert len(match) == 1, (cfg,group,method,parameter)
            statuses.append(match[0].get('result'))
    assert statuses == ['Fail','Pass','Fail','Pass']
    test = f'{method}({parameter or ""})'
    row = [f'CODEC-Q1-{finding} {tick}{name}{tick}', mechanism, ' / '.join(observed[c][0] for c in configs),
           expected, ' / '.join(observed[c][1] for c in configs), f'{tick}{test}{tick}',
           '/'.join(statuses[:2]), '/'.join(statuses[2:])]
    matrix.append('| '+' | '.join(row)+' |')
matrix += ['', 'Adjacent controls cover matching policy values, one valid escaped session member in both topologies, '
    'wrong-kind policy data, ambiguous nested profile members, and all six digest inputs and returned memory views. '
    'The ownership tests each corrupt caller-visible bytes and verify that the original binding still succeeds and the replacement refuses. '
    '[Debug exact replay](raw/q1-replay-debug.json) · [Release exact replay](raw/q1-replay-release.json)']
(evidence/'closure-matrix.md').write_text('\n'.join(matrix)+'\n', encoding='utf-8', newline='\n')

rows = [json.loads(x) for x in (logs/'compiled-scan.jsonl').read_text(encoding='utf-8-sig').splitlines()]
assert len(rows) == 36 and all(x['pass'] for x in rows)
production = [x for x in rows if not x['assembly'].endswith('.Tests')]
positives = [x for x in rows if x not in production]
assert len(positives) == 2 and all(x['knownBootstrapCallers'] and x['knownBinaryCallers'] for x in positives)
assert all(not x['hits'] and not x['typeRefs'] and not x['outbound'] for x in production)
core = 'Gagamba.Execution.Linux.M1'
for cfg in configs:
    suffix = f'/bin/{core}/{cfg}/{core}.dll'
    chosen = [x for x in rows if x['file'].replace('\\','/').lower().endswith(suffix.lower())]
    assert len(chosen) == 1
    core_sha = chosen[0]['sha256']
    for caller in ('InstallTool',core+'.Owner',core+'.Recovery'):
        suffix = f'/bin/{caller}/{cfg}/{core}.dll'
        matches = [x for x in rows if x['file'].replace('\\','/').lower().endswith(suffix.lower())]
        assert len(matches) == 1 and matches[0]['sha256'] == core_sha
    outcomes[cfg]['standaloneCoreSha256'] = core_sha
    positive = next(x for x in positives if f'/{cfg}/' in x['file'].replace('\\','/'))
    outcomes[cfg]['testPositiveCallerCounts'] = {
        'bootstrap':len(positive['knownBootstrapCallers']), 'binary':len(positive['knownBinaryCallers'])}
    assert core_sha.upper() in (logs/f'verifier-{cfg}.log').read_text()
write_json(evidence/'verification-summary.json',outcomes)

for path in logs.iterdir():
    if path.is_file() and path.suffix != '.xml' and path.name != 'production-build-controls.log':
        copy_text(path,raw/path.name)
for name in ('Build-R1.ps1','Verify-R1.ps1','stage_r1_source.py','assemble_evidence.py'):
    copy_text(scratch/name,evidence/name)
copy_text(scratch/'run/q1-import.json', evidence/'q1-import.json')
copy_text(scratch/'run/preparation-before-report.log',raw/'preparation-after-q1-import.log')
settings = json.loads((scratch/'run/preflight.json').read_text())
assert len(settings['untracked']) == 44
receipt = {k:settings[k] for k in ('head','tree','origin','ancestry')}
receipt.update(historicalFiles=len(settings['untracked']), refs=len(settings['refs']), trackingAheadBehind=[0,0],
               worktrees=1, stashes=0, cleanTracked=True)
write_json(evidence/'canonical-preflight.json',receipt)
print(f'PASS: {len(records)} exact counterexamples in two configurations, xUnit results, 36 DLLs and Q1 import receipt')
