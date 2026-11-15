import collections, hashlib, json, re, shutil, sys, xml.etree.ElementTree as ET
from pathlib import Path

run, output, old = (Path(arg).resolve() for arg in sys.argv[1:])
logs = run/'logs'
raw = output/'raw'
raw.mkdir(parents=True,exist_ok=True)
payloads = output/'findings-inputs'
payloads.mkdir(exist_ok=True)
def read_jsonl(p):
    return [json.loads(x) for x in p.read_text(encoding='utf-8-sig').splitlines()]
def write_json(p,v):
    p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
def sha(b):
    return hashlib.sha256(b).hexdigest()
def copy_text(source, target):
    # Normalize presentation whitespace in textual logs. Exact fixture bytes
    # are copied by copyfile separately and checked against the probe ledger.
    lines = source.read_text(encoding='utf-8-sig').splitlines()
    target.write_text('\n'.join(line.rstrip() for line in lines).rstrip()+'\n',encoding='utf-8',newline='\n')
def replay(p):
    return {r['test']:r for r in read_jsonl(p)}
def bind(name):
    return name.replace('\\','/').lower()

closure, supplemental, stats, exceptions = {}, {}, {}, {}
for cfg in ('debug','release'):
    original = replay(old/f'vectors-{cfg}/results.jsonl')
    before = replay(run/f'replay-original-{cfg}/results.jsonl')
    after = replay(run/f'replay-candidate-{cfg}/results.jsonl')
    miss = [k for k in original if not original[k]['matched']]
    assert len(miss)==17 and set(miss)=={k for k,r in before.items() if not r['matched']}
    assert original.keys()==before.keys()==after.keys() and len(after)==2383 and all(r['matched'] for r in after.values())
    for k in miss:
        assert original[k]==before[k] and before[k]['vector']==after[k]['vector'], k
        closure.setdefault(k,{})[cfg]={'original':before[k],'corrected':after[k]}
    copy_text(run/f'replay-original-{cfg}/results.jsonl',raw/f'replay-original-{cfg}.jsonl')
    copy_text(run/f'replay-candidate-{cfg}/results.jsonl',raw/f'replay-candidate-{cfg}.jsonl')
    extra=read_jsonl(run/f'supplement-{cfg}/q2-results.jsonl')
    assert len(extra)==876 and len({r['id'] for r in extra})==876
    bad=[r for r in extra if not r['pass']]
    assert len(bad)==13 and all(r['actual'].startswith('throw ') for r in bad)
    copy_text(run/f'supplement-{cfg}/q2-results.jsonl',raw/f'supplement-{cfg}.jsonl')
    general=collections.Counter('wireDuplicate' if '/wire-duplicate/' in r['id'] else
        'projectionDuplicate' if '/projection-duplicate/' in r['id'] else
        'policy' if r['id'].startswith('Integrated/policy/') else
        'binaryOwnership' if r['id'].startswith('binary/') else 'other' for r in extra)
    assert general['wireDuplicate']==387 and general['projectionDuplicate']==306
    supplemental[cfg]={'checks':len(extra),'matched':len(extra)-len(bad),'unexpected':len(bad),'families':dict(general)}
    traces={side:{r['id']:r for r in read_jsonl(logs/f'finding-stack-{side}-{cfg}.jsonl')}
        for side in ('original','candidate')}
    for r in bad:
        stem=re.sub('[^a-zA-Z0-9]','_',r['id'])
        for extension,k in (('wire','wireSha256'),('projection','projectionSha256')):
            source=run/f'supplement-{cfg}'/(stem+'.'+extension)
            assert sha(source.read_bytes())==r[k]
            dst=payloads/source.name
            if dst.exists(): assert dst.read_bytes()==source.read_bytes()
            else: shutil.copyfile(source,dst)
            assert all(trace[stem][k]==r[k] for trace in traces.values())
        assert traces['candidate'][stem]['result']==r['actual'] and not traces['candidate'][stem]['published']
        assert 'CompareProjectionObject' in traces['candidate'][stem]['stack']
        exceptions.setdefault(r['id'],{})[cfg]={'stem':stem,'original':traces['original'][stem],
            'corrected':traces['candidate'][stem]}
    assert collections.Counter(r['result'] for r in traces['original'].values())=={
        'None':5,'throw InvalidOperationException':2,'throw ArgumentOutOfRangeException':6}
    assert all(r['published']==(r['result']=='None') for r in traces['original'].values())
    counts={}
    for label,expected in [('original-focused',(146,146,0,0)),('r1-focused',(24,24,0,0)),
                           ('combined-focused',(170,170,0,0)),('broad',(1994,1663,0,331))]:
        path=logs/f'{label}-{cfg}.xml'
        a=ET.parse(path).find('assembly')
        c={k:int(a.get(k)) for k in ('total','passed','failed','skipped','not-run','errors')}
        assert tuple(c[k] for k in ('total','passed','failed','skipped'))==expected and c['not-run']==c['errors']==0
        tests=list(a.iter('test'))
        if label=='combined-focused':
            assert collections.Counter(t.get('type').split('.')[-1] for t in tests)=={
                'ValidationPreCodecBootstrapTests':64,'ValidationPreCodecBinaryTests':58,
                'InstallationWireTests':24,'ValidationPreCodecRemediationTests':24}
        if label=='broad':
            prior=[x for x in tests if 'CompiledReleaseAssemblyHasNoProductionCaller' in x.get('name','')]
            assert len(prior)==2 and all(t.get('result')=='Pass' for t in prior)
            assert sum(t.get('result')=='Skip' for t in tests)==331
        counts[label]=c
    stats[cfg]=counts

tick=chr(96)
matrix=['# Closure of exact Q1 counterexamples','',
    'Each row binds the same original Q1 vector hash or exact hex in four fresh runs. The normative outcomes require bounded refusal. '
    'The results below show the more specific measured diagnostics. All closures are specific to the original counterexamples.',
    '', '| Original Q1 identifier | Normative requirement | Original Debug | Original Release | R1 Debug | R1 Release | Disposition |',
    '|---|---|---|---|---|---|---|']
for case, record in sorted(closure.items()):
    if '/expectation-' in case:
        expected='Expectation' if case.endswith('missing') else 'Comparison'
        norm='Frozen §3 relationship 6 and its comparison rule require separately supplied matching policy'
        finding='F01'
    elif case.endswith('ambiguous-original-session'):
        expected='Expectation'; norm='Frozen §3 relationship 2 requires an unambiguous original session'; finding='F02'
    elif case.endswith('disposed-original'):
        expected='Expectation'; norm='Entry §3 requires bounded refusal for unavailable projection data'; finding='F03'
    else:
        expected='Comparison'; norm='Frozen §4 message 10 compares the original expected digest'; finding='F04'
    values=[record[c][k]['actual'] for k in ('original','corrected') for c in ('debug','release')]
    assert values[-2:]==[expected,expected]
    matrix.append('| '+' | '.join([f'{finding} {tick}{case}{tick}',norm,*values,'Exact case closed'])+' |')
matrix+=['','[Exact inputs and four executed outcomes](raw/q1-closure.json) · [Original independent Q1 evidence](../../rc2b-06-b6-alt-b-slice-e-03-codec-q1.md)']
# A sibling report is one directory above the annex.
matrix[-1]=matrix[-1].replace('../../rc2b-','../rc2b-')
(output/'closure-matrix.md').write_text('\n'.join(matrix)+'\n',encoding='utf-8',newline='\n')
write_json(raw/'q1-closure.json',closure)
write_json(raw/'q2-exceptions.json',exceptions)

scan=read_jsonl(logs/'compiled-scan.jsonl')
assert len(scan)==36 and all(r['pass'] for r in scan)
test=[r for r in scan if r['assembly'].endswith('.Tests')]
prod=[r for r in scan if r not in test]
assert len(test)==2 and all(r['knownBootstrapCallers'] and r['knownBinaryCallers'] for r in test)
assert all(not r['hits'] and not r['typeRefs'] and not r['outbound'] and not r['namedResources'] for r in prod)
e3='Gagamba.Execution.Linux.M1'
for cfg in ('debug','release'):
    def find(proj, dll):
        name=f'/bin/{proj}/{cfg}/{dll}.dll'.lower()
        return next(r for r in scan if bind(r['file']).endswith(name))
    core=find(e3,e3)
    assert all(find(proj,e3)['sha256']==core['sha256'] for proj in ('InstallTool',e3+'.Owner',e3+'.Recovery'))
    assert core['sha256'].upper() in (logs/f'verifier-{cfg}.log').read_text()
    stats[cfg]['coreSha256']=core['sha256']
    t=find(e3+'.Tests',e3+'.Tests')
    stats[cfg]['testPositiveCallerCounts']={'bootstrap':len(t['knownBootstrapCallers']),'binary':len(t['knownBinaryCallers'])}
assert "assembly configuration is 'Debug', expected Release" in (logs/'verifier-debug.log').read_text()
assert (logs/'verifier-release.log').read_text().rstrip().endswith('EXIT 0')
assert (logs/'verifier-debug.log').read_text().rstrip().endswith('EXIT 1')
control=json.loads((run/'production-controls/results.json').read_text(encoding='utf-8-sig'))
assert len(control)==51 and all(r['Passed'] for r in control)
assert 'Ran 9 tests' in (run/'preparation-tests.log').read_text()
assert 'e8099e37c1243e81f173bf0248307cf7d2fe3875' in (run/'live-origin.log').read_text()
write_json(output/'summary.json',dict(managed=stats,supplemental=supplemental,preservation={'q1ChecksumCount':156,
    'inheritedNativeUnqualified':331,'productionControls':51,'preparationRegressions':9}))
for p in logs.iterdir():
    if p.is_file() and not p.name.startswith('finding-stack-'):
        copy_text(p,raw/p.name)
for p in (run/'original-Debug.log',run/'original-Release.log',run/'production-controls.log',run/'preparation-tests.log',
          run/'live-origin.log',run/'identities.json',run/'p4-callers.txt'):
    copy_text(p,raw/p.name)
if (run/'advisory-restore.log').exists(): copy_text(run/'advisory-restore.log',raw/'advisory-restore.log')
write_json(raw/'production-controls.json',control)
write_json(raw/'scope-code-diff.json',{'rawGitDiff':(run/'scope-implementation.patch.txt').read_bytes().decode('utf-8')})
print('PASS audit of both configurations, all records, 17 closures, 13 new case files, 36 DLLs and 51 controls')
