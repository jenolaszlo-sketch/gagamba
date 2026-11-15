from pathlib import Path
import hashlib,json,os,re,subprocess,sys,xml.etree.ElementTree as ET
repo,work=(Path(x).resolve() for x in sys.argv[1:])
start='29da0ed60c18ed6364bf9366a64ab5aa3393db62'
r2='8be61181e804b2dfafb0797eb54c92d2f234ff86'
q2='4cb46a6cdbcc52b7129dbd88c4eba2e62ae73469'
r1='4a97b79e0060b001a7c1d30ae93bc66cedc52418'
q1='7713ec58cc7c292862c8c80d6b57dcacd3420b44'
original='34eef4c8e7d814e15cb1bfce27df032e75766f19'
entry='1c7fb672055cc3322eb1633d37e5e84d764cb66d'
p='rc2b-06-b6-alt-b-slice-e-03-'
env={**os.environ,'GIT_OPTIONAL_LOCKS':'0'}
def git(*args):return subprocess.check_output(['git','-C',str(repo),*args],env=env)
def g(*args):return git(*args).decode().strip()
def sha(b):return hashlib.sha256(b).hexdigest()
def b(path,rev=start): return git('cat-file','blob',rev+':'+path)
def read(path):return b(path).decode('utf-8-sig')
def jsonf(path):return json.loads(read(path))
def rows(path):return [json.loads(x) for x in read(path).splitlines()]
def write(path,text):path.write_text(text,encoding='utf8',newline='\n')
def dump(path,v):write(path,json.dumps(v,indent=2)+'\n')
assert g('rev-parse','--show-toplevel')==repo.as_posix()
assert g('rev-parse','HEAD')==start and g('rev-parse','HEAD^{tree}')=='52364b4c62098fc0fc75fdda26bd2bd8f099c760'
assert g('rev-parse',r2+'^{tree}')=='1c9307845858fb25ca0a9473d53d1fd1496018fc'
assert g('symbolic-ref','HEAD')=='refs/heads/main' and not g('status','--porcelain','-uno')
assert git('worktree','list','--porcelain').count(b'worktree ')==1 and not g('stash','list')
assert not any((repo/'.git').glob('*.lock'))
for child,parent in [(start,r2),(r2,q2),(q2,r1),(r1,q1),(original,entry)]:assert g('rev-parse',child+'^')==parent
git('merge-base','--is-ancestor',original,q1)
files={k:sha((repo/k).read_bytes()) for k in git('ls-files','--others','--exclude-standard','-z').decode().split('\0') if k}
prior=json.loads((work.parent/'gagamba-e03-codec-r2-q3/run/preservation.json').read_text())
assert len(files)==44 and files==prior['untracked']
refs=g('for-each-ref','--format=%(refname) %(objectname)').splitlines()
assert [x for x in refs if not x.startswith('refs/heads/main ')]==[x for x in prior['refs'] if not x.startswith('refs/heads/main ')]
roots={}; statuses={}; authority={}
for kind,stem,n,rev in [('reviews','codec-q1',156,q1),('evidence','codec-r1',None,r1),
                       ('reviews','codec-r1-q2',100,q2),('evidence','codec-r2',64,r2),('reviews','codec-r2-q3',77,start)]:
    report=f'docs/{kind}/{p}{stem}.md'
    root=report.removesuffix('.md')+'-evidence'
    roots[stem]=root
    assert not g('diff','--name-only',rev,start,'--',report,root)
    statuses[stem]={'reportBlob':g('rev-parse',start+':'+report),'reportSha256':sha(b(report)), 'tree':g('rev-parse',start+':'+root)}
    if stem in ['codec-q1','codec-r1-q2']: assert 'REMEDIATION REQUIRED' in read(report)
    if n:
        file=repo/root/'SHA256SUMS.txt'
        manifest=b(root+'/SHA256SUMS.txt')
        assert file.read_bytes()==manifest
        items=manifest.decode().splitlines()
        assert len(items)==n and len(set(items))==n
        for h,f in [line.split('  ',1) for line in items]:
            path=(file.parent/f).resolve()
            assert sha(path.read_bytes())==sha(b(path.relative_to(repo).as_posix(),rev))==h, path
        statuses[stem].update(entries=n,manifestSha256=sha(manifest))
assert statuses['codec-r2-q3']['manifestSha256']=='2ca94c21c993e2923bdafb017775fc20143bd8d0c5174502b61d4bbf6b7af16b'
q3=roots['codec-r2-q3']
diff=g('diff','--name-only',r2,start).splitlines()
assert diff and all(x==q3.removesuffix('-evidence')+'.md' or x.startswith(q3+'/') for x in diff)
bound=jsonf(q3+'/raw/identities.json')
for path,x in bound['normative'].items():
    assert g('rev-parse',start+':'+path)==x['blob'] and sha(b(path))==x['sha256']
    authority[path]=x
freeze=f'docs/evidence/{p}v3-pre-frozen-record-candidate-01.md'
assert authority[freeze]=={'blob':'f6a71562845d2cfe0aee5495864a509c5caabe7a','sha256':'1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e'}
adoption=read(f'docs/decisions/{p}qe1-frozen-record-owner-01.md')
adopted_objects={oid:g('cat-file','-t',oid) for oid in dict.fromkeys(re.findall(r'\x60([0-9a-f]{40})\x60',adoption.split('## 2.')[0]))}
manifest=f'docs/reviews/{p}qe1-frozen-record-prep-01-evidence/source-manifest.md'
assert g('rev-parse',start+':'+manifest)=='1c217485c8e8406bbfffad61fd39d7ec5b6dd549'
historical_local={}
for path,n,digest in re.findall(r'^\| [A-Z0-9-]+ \| \x60([^\x60]+)\x60 \| (\d+) \| \x60([0-9a-f]{64})\x60',read(manifest),re.M):
    data=(work.parent/path).read_bytes()
    assert len(data)==int(n) and sha(data)==digest
    historical_local[path]=digest
assert len(historical_local)==12
for path,blob in bound['code'].items():
    assert g('rev-parse',r2+':'+path)==g('rev-parse',start+':'+path)==blob
r2_source=jsonf(roots['codec-r2']+'/raw/source-binding.json')
assert all(g('rev-parse',r2+':'+path)==obj for path,obj in r2_source['blobs'].items())
hist_q1=jsonf(q3+'/raw/q1-cases.json')
hist_q2=jsonf(q3+'/raw/q2-cases.json')
assert len(hist_q1)==17 and len(hist_q2)==13
core={}; summary={}
for cfg in ['debug','release']:
    corpus={row['test']:row for row in rows(roots['codec-q1']+f'/vectors-{cfg}/results.jsonl')}
    for key,each in hist_q1.items():
        h=each[cfg]
        expected='Expectation' if key.endswith(('expectation-missing','ambiguous-original-session','disposed-original')) else 'Comparison'
        assert h['original']==corpus[key] and not h['original']['matched']
        assert h['corrected']['matched'] and h['corrected']['actual']==expected
        assert h['corrected']['vector']==h['original']['vector']
    for key,each in hist_q2.items():
        h=each[cfg]
        assert h['r1']['result'] in ['throw InvalidOperationException','throw ArgumentOutOfRangeException']
        assert 'JsonElement.DeepEquals' in h['r1']['stack']
        assert h['r2']['result']=='Expectation' and not h['r2']['published']
        path=roots['codec-r1-q2']+'/findings-inputs/'+key
        for suffix,prop in [('.wire','wireSha256'),('.projection','projectionSha256')]:
            assert sha(b(path+suffix))==h['r1'][prop]==h['r2'][prop]
    for name,num in [('q1-candidate',2383),('q2-supplement',876),('q3-supplement',56)]:
        a=rows(q3+f'/raw/{name}-{cfg}.jsonl')
        assert len(a)==num and all(x.get('matched',x.get('pass')) for x in a)
    for name,expected in [('original-focused',(146,146,0,0)),('r1-focused',(24,24,0,0)),('r2-focused',(15,15,0,0)),
                          ('combined-focused',(185,185,0,0)),('broad',(2009,1678,0,331))]:
        x=ET.fromstring(b(q3+f'/raw/{name}-{cfg}.xml')).find('assembly')
        assert tuple(int(x.get(a)) for a in ('total','passed','failed','skipped'))==expected
        assert x.get('not-run')==x.get('errors')=='0'
    summary[cfg]={'original':146,'r1':24,'r2':15,'focused':185,'broadPassed':1678,'broadSkipped':331,'q3':56}
    verify=read(q3+f'/raw/verifier-{cfg}.log')
    core[cfg]=re.search('SHA-256: ([0-9A-F]{64})',verify).group(1).lower()
    assert '14/14 present' in verify
    assert ('RESULT: PASS' in verify and verify.rstrip().endswith('EXIT 0')) if cfg=='release' else (
        "assembly configuration is 'Debug', expected Release" in verify and verify.rstrip().endswith('EXIT 1'))
scan=rows(q3+'/raw/compiled-scan.jsonl')
assert len(scan)==36 and all(x['pass'] for x in scan)
positives=[x for x in scan if x['assembly'].endswith('.Tests')]
assert len(positives)==2 and all(x['knownBootstrapCallers'] and x['knownBinaryCallers'] for x in positives)
assert all(not x['hits'] and not x['typeRefs'] and not x['outbound'] for x in scan if x not in positives)
lib='Gagamba.Execution.Linux.M1'
for cfg,h in core.items():
    for parent in (lib,lib+'.Owner',lib+'.Recovery','InstallTool'):
        suffix=f'/bin/{parent}/{cfg}/{lib}.dll'.lower()
        assert [x['sha256'] for x in scan if x['file'].replace('\\','/').lower().endswith(suffix)]==[h]
ctrl=jsonf(q3+'/raw/production-controls.json')
assert len(ctrl)==51 and all(x['Passed'] for x in ctrl)
assert 'Ran 9 tests' in read(q3+'/raw/preparation-tests.log')
assert read(q3+'/raw/preparation-final.log').rstrip().endswith('EXIT 0')
pattern=r'StartOwnerPreparation\(|TrySpawnOwner\(|new PreparationOwnershipCoordinator\(|IntegratedRecoveryHost.Run\(|QualifiedRecoveryConnection.Start\(|RecoveryConnection.Start\(|new HeldReleasePhase\(|new B3PreparationOwner\(|OwnerPreparationTransaction.Run\('
def graph(rev):return g('grep','-n','-E',pattern,rev,'--','src','tests').replace(rev+':','')
assert graph(original)==graph(r2) and graph(r2)==read(q3+'/raw/p4.txt').strip()
work.joinpath('run').mkdir(parents=True,exist_ok=True)
dump(work/'run/private-state.json',dict(untracked=files,refs=refs))
e=work/f'package/docs/decisions/{p}owner-01-evidence'
e.mkdir(parents=True,exist_ok=True)
dump(e/'identity-and-audit.json',dict(commits={c:g('rev-parse',c+'^{tree}') for c in [entry,original,q1,r1,q2,r2,start]},
 ancestry=g('log','--format=%H %P %s',entry+'..'+start).splitlines(),manifestStatus=statuses,core=core,tests=summary,
 historicalCount=44,worktrees=1,stashes=0,trackedClean=True,authority=authority,adoptionObjects=adopted_objects,
 originalHistoricalRawInputs=historical_local,productionControls=51,scanCount=36,
 q1Cases=sorted(hist_q1),q2Cases=sorted(hist_q2),r2ToQ3ChangedPaths=diff))
e2='docs/reviews/rc2b-06-b6-alt-b-slice-e-02-closure-assessment-01.md'
assert g('rev-parse',start+':'+e2)=='b75e1a5e2a7b52c393b919ff54ef1dd68ca49779'
text=read(e2)
for sec,count,leaf in [(21,39,'inherited-qualifications.md'),(22,23,'downstream-obligations.md')]:
    part=re.split(rf'^## {sec+1}\.',re.split(rf'^## {sec}\.',text,flags=re.M)[1],flags=re.M)[0]
    lines=[x.rstrip() for x in part.splitlines() if x.startswith('|')]
    assert len(lines)==count+2, (sec,len(lines)-2)
    title='Retained qualification ownership matrix' if sec==21 else 'Downstream responsibility matrix'
    assert not any('](' in x for x in lines)
    write(e/leaf,'# '+title+'\n\nVerbatim table from accepted E-2 closure assessment §'+str(sec)+'. Its historical dispositions and closure requirements remain controlling.\n\n[Controlling assessment](../../reviews/rc2b-06-b6-alt-b-slice-e-02-closure-assessment-01.md)\n\n'+'\n'.join(lines)+'\n')
print('Verified baseline, exact Q3 case evidence, verifier identities, historical manifests, and inherited ledgers.')
