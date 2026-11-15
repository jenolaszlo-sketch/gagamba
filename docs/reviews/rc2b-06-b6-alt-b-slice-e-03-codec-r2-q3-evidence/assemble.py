from collections import Counter
import hashlib,json,re,sys
from pathlib import Path
import xml.etree.ElementTree as ET

repo,run,out=map(Path,sys.argv[1:])
logs=run/'logs'
raw=out/'raw';raw.mkdir(parents=True,exist_ok=True)
q1=repo/'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence'
q2=repo/'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence'
def read(f):return f.read_text(encoding='utf-8-sig')
def rows(f):return [json.loads(r) for r in read(f).splitlines()]
def dump(f,v):f.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
def copy(f,t):t.write_text('\n'.join(line.rstrip() for line in read(f).splitlines()).rstrip()+'\n',encoding='utf-8',newline='\n')
def index(v,k):return {r[k]:r for r in v}
def get_replay(side,c):return index(rows(run/f'replay-{side}-{c}'/'results.jsonl'),'test')
historical=json.loads(read(q2/'raw/q2-exceptions.json'))
hist_names={v['debug']['stem']:name for name,v in historical.items()}
q1_table,q2_table,test_totals,supp={},{},{},{}
for cfg in ['debug','release']:
    frozen=index(rows(q1/f'vectors-{cfg}'/'results.jsonl'),'test')
    original,candidate=get_replay('original',cfg),get_replay('candidate',cfg)
    assert frozen.keys()==original.keys()==candidate.keys()
    failing=[k for k in frozen if not frozen[k]['matched']]
    assert len(failing)==17 and set(failing)=={k for k,v in original.items() if not v['matched']}
    assert len(candidate)==2383 and all(r['matched'] for r in candidate.values())
    for k in failing:
        assert frozen[k]==original[k] and original[k]['vector']==candidate[k]['vector']
        q1_table.setdefault(k,{})[cfg]={'original':original[k],'corrected':candidate[k]}
    copy(run/f'replay-original-{cfg}'/'results.jsonl',raw/f'q1-original-{cfg}.jsonl')
    copy(run/f'replay-candidate-{cfg}'/'results.jsonl',raw/f'q1-candidate-{cfg}.jsonl')
    before=index(rows(logs/f'q2-exact-r1-{cfg}.jsonl'),'id')
    after=index(rows(logs/f'q2-exact-r2-{cfg}.jsonl'),'id')
    assert before.keys()==after.keys()==hist_names.keys() and len(before)==13
    assert Counter(x['result'] for x in before.values())=={
        'throw InvalidOperationException':5,'throw ArgumentOutOfRangeException':8}
    for k,b in before.items():
        a=after[k]; h=historical[hist_names[k]][cfg]['corrected']
        assert b['result']==h['result'] and 'JsonElement.DeepEquals' in b['stack'] and 'CompareProjectionObject' in b['stack']
        for key in ('wireSha256','projectionSha256'):
            assert b[key]==a[key]==h[key]
        assert a['result']=='Expectation' and not b['published'] and not a['published']
        q2_table.setdefault(k,{})[cfg]={'r1':b,'r2':a}
    for kind,n in [('q2',876),('q3',56)]:
        source=run/f'supplement-{cfg}'/f'{kind}-results.jsonl'
        a=rows(source)
        assert len(a)==n and all(r['pass'] for r in a)
        copy(source,raw/f'{kind}-supplement-{cfg}.jsonl')
        supp.setdefault(cfg,{})[kind]={'assertions':n,'pass':n}
    counts={}
    for label,want in [('original-focused',(146,146,0,0)),('r1-focused',(24,24,0,0)),
                       ('r2-focused',(15,15,0,0)),('combined-focused',(185,185,0,0)),('broad',(2009,1678,0,331))]:
        a=ET.parse(logs/f'{label}-{cfg}.xml').find('assembly')
        keys=('total','passed','failed','skipped')
        vals=tuple(int(a.get(k)) for k in keys)
        assert vals==want and a.get('not-run')==a.get('errors')=='0'
        tests=list(a.iter('test'))
        assert len(tests)==want[0]
        if label=='combined-focused':
            assert Counter(t.get('type').split('.')[-1] for t in tests)=={
                'ValidationPreCodecBootstrapTests':64,'ValidationPreCodecBinaryTests':58,
                'InstallationWireTests':24,'ValidationPreCodecRemediationTests':24,'ValidationPreCodecR2Tests':15}
        if label=='broad':
            matching=[t for t in tests if 'CompiledReleaseAssemblyHasNoProductionCaller' in t.get('name')]
            assert len(matching)==2 and all(t.get('result')=='Pass' for t in matching)
        counts[label]=dict(zip(keys,vals))
    test_totals[cfg]=counts

tick=chr(96)
m=['# Seventeen original Q1 cases','','| Q1 case | Contract basis | Original Debug | Original Release | R2 Debug | R2 Release | Result |',
   '|---|---|---|---|---|---|---|']
for id,v in sorted(q1_table.items()):
    if '/expectation-' in id:
        tag='F01';basis='Frozen §3 rule 6, independent matching policy'; expected='Expectation' if id.endswith('missing') else 'Comparison'
    elif id.endswith('ambiguous-original-session'):
        tag='F02';basis='Frozen §3 rule 2, unambiguous original session';expected='Expectation'
    elif id.endswith('disposed-original'):
        tag='F03';basis='Entry §3, bounded refusal for unusable expectation';expected='Expectation'
    else:
        tag='F04';basis='Frozen §4 message 10, original bound digest';expected='Comparison'
    actual=[v[c][typ]['actual'] for typ in ('original','corrected') for c in ('debug','release')]
    assert actual[-2:]==[expected,expected]
    m.append('| '+' | '.join([tag+' '+tick+id+tick,basis,*actual,'Closed'])+' |')
m+=['','Every row matches the committed Q1 input hash or binary bytes in all executed replays. [Exact rows](raw/q1-cases.json) · [Original Q1 report](../rc2b-06-b6-alt-b-slice-e-03-codec-q1.md)']
(out/'q1-matrix.md').write_text('\n'.join(m)+'\n',encoding='utf-8',newline='\n')
m=['# Thirteen original Q2 cases','','| Q2 case | Expected | R1 Debug | R1 Release | R2 Debug | R2 Release | Result |',
   '|---|---|---|---|---|---|---|']
for stem,v in sorted(q2_table.items()):
    tag='CODEC-Q2-F01' if v['debug']['r1']['result']=='throw InvalidOperationException' else 'CODEC-Q2-F02'
    actual=[v[c][typ]['result'] for typ in ('r1','r2') for c in ('debug','release')]
    m.append('| '+' | '.join([tag+' '+tick+hist_names[stem]+tick,'Expectation, no data',*actual,'Closed'])+' |')
m+=['','Frozen §3 exact semantic comparison and entry §3 bounded refusal govern each row. Both builds refused without a result object. Hashes match the original Q2 fixture bytes. [Exact rows and traces](raw/q2-cases.json) · [Original Q2 findings](../rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/findings.md)']
(out/'q2-matrix.md').write_text('\n'.join(m)+'\n',encoding='utf-8',newline='\n')
dump(raw/'q1-cases.json',q1_table)
dump(raw/'q2-cases.json',q2_table)

scan=rows(logs/'compiled-scan.jsonl')
assert len(scan)==36 and all(r['pass'] for r in scan)
pos=[r for r in scan if r['assembly'].endswith('.Tests')]
assert len(pos)==2 and all(len(r['knownBootstrapCallers'])>=2 and len(r['knownBinaryCallers'])>=4 for r in pos)
assert all(not r['hits'] and not r['typeRefs'] and not r['outbound'] for r in scan if r not in pos)
lib='Gagamba.Execution.Linux.M1'
for c in ['debug','release']:
    def dll(project):
        end=f'/bin/{project}/{c}/{lib}.dll'.lower()
        return next(r for r in scan if r['file'].replace('\\','/').lower().endswith(end))
    core=dll(lib)
    assert all(dll(p)['sha256']==core['sha256'] for p in [lib+'.Owner',lib+'.Recovery','InstallTool'])
    test_totals[c]['coreDll']=core['sha256']
    assert core['sha256'].upper() in read(logs/f'verifier-{c}.log')
    assert read(logs/f'verifier-{c}.log').rstrip().endswith('EXIT '+('0' if c=='release' else '1'))
assert "assembly configuration is 'Debug', expected Release" in read(logs/'verifier-debug.log')
assert 'RESULT: PASS' in read(logs/'verifier-release.log')
ctrl=json.loads(read(run/'production-controls/results.json'))
assert len(ctrl)==51 and all(x['Passed'] for x in ctrl) and read(run/'production-controls.log').rstrip().endswith('EXIT 0')
assert 'Ran 9 tests' in read(run/'preparation-tests.log') and read(run/'preparation-tests.log').rstrip().endswith('EXIT 0')
assert len(read(logs/'build-exits.txt').splitlines())==10 and all(s.startswith('0 ') for s in read(logs/'build-exits.txt').splitlines())
dump(out/'summary.json',dict(tests=test_totals,supplemental=supp,q1Cases=17,q2Cases=13,
    scannedFiles=36,productionControls=51,preparationRegressions=9))
for f in logs.iterdir():
    if f.is_file() and not f.name.startswith('q2-exact-r'):
        copy(f,raw/f.name)
for f in ('identities.json','scope-diff.json','p4.txt','production-controls.log','preparation-tests.log','environment.txt'):
    copy(run/f,raw/f)
dump(raw/'production-controls.json',ctrl)
print('Validated 17 Q1 and 13 Q2 rows, 56 Q3 controls, both managed suites, both binary gates and 51 controls.')
