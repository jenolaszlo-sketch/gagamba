from pathlib import Path
import hashlib,json,os,subprocess,sys,re
repo,run,prev=map(Path,sys.argv[1:])
base='8be61181e804b2dfafb0797eb54c92d2f234ff86'
q2='4cb46a6cdbcc52b7129dbd88c4eba2e62ae73469'
r1='4a97b79e0060b001a7c1d30ae93bc66cedc52418'
q1='7713ec58cc7c292862c8c80d6b57dcacd3420b44'
old='34eef4c8e7d814e15cb1bfce27df032e75766f19'
prefix='rc2b-06-b6-alt-b-slice-e-03-'
env={**os.environ,'GIT_OPTIONAL_LOCKS':'0'}
def git(*a): return subprocess.check_output(['git','-C',str(repo),*a],env=env)
def g(*a): return git(*a).decode().strip()
def sha(b): return hashlib.sha256(b).hexdigest()
def historical():
    return {f:sha((repo/f).read_bytes()) for f in git('ls-files','--others','--exclude-standard','-z').decode().split('\0') if f}
def evidence(root, revision):
    file=repo/root/'SHA256SUMS.txt'
    raw=file.read_bytes()
    assert raw==git('cat-file','blob',revision+':'+file.relative_to(repo).as_posix())
    entries=[]
    for line in raw.decode().splitlines():
        h,f=line.split('  ',1)
        path=(file.parent/f).resolve()
        relative=path.relative_to(repo.resolve()).as_posix()
        assert sha(path.read_bytes())==sha(git('cat-file','blob',revision+':'+relative))==h
        entries.append(relative)
    assert len(set(entries))==len(entries)
    return {'entries':len(entries),'manifestSha256':sha(raw)}
assert g('rev-parse','HEAD')==base and g('rev-parse','HEAD^{tree}')=='1c9307845858fb25ca0a9473d53d1fd1496018fc'
assert g('symbolic-ref','HEAD')=='refs/heads/main'
assert not g('status','--porcelain','-uno') and not g('stash','list')
assert git('worktree','list','--porcelain').count(b'worktree ')==1
assert not any((repo/'.git').glob('*.lock'))
assert len(historical())==44 and historical()==json.loads(prev.read_text())['untracked']
for child,parent in [(base,q2),(q2,r1),(r1,q1)]:
    assert g('rev-parse',child+'^')==parent
git('merge-base','--is-ancestor',old,q1)
manifests={}
for folder,stem,count,revision in [('reviews','codec-q1',156,q1),('reviews','codec-r1-q2',100,q2),('evidence','codec-r2',64,base)]:
    root=f'docs/{folder}/{prefix}{stem}-evidence'
    manifests[stem]=evidence(root,base)
    assert manifests[stem]['entries']==count
    assert not g('diff','--name-only',revision,base,'--',root,root.removesuffix('-evidence')+'.md')
sources={
   'evidence/v3-pre-frozen-record-candidate-01.md':'f6a71562845d2cfe0aee5495864a509c5caabe7a',
   'decisions/qe1-frozen-record-owner-01.md':'d2fea61b7db323319ee59033c8b1b12ded1ffe7d',
   'decisions/qe1-normative-owner-01.md':'f3639dc619dbfee39859afb7da553e2167d9b83d',
   'decisions/qe1-bootstrap-base-owner-01.md':'93fe2c9155a21b12fd3486b9da743260897d508e',
   'decisions/qe1-bootstrap-base-owner-01-evidence/schema.md':'a7248152d09e19d8f3775db397f340949fa9ee57',
   'decisions/entry-owner-01.md':'028a7c1858b5e7b506deff9d2d8d263afe3344ac'}
authority={}
for name,oid in sources.items():
    dir,leaf=name.split('/',1)
    path=f'docs/{dir}/{prefix}{leaf}'
    assert g('rev-parse',base+':'+path)==oid
    authority[path]={'blob':oid,'sha256':sha(git('cat-file','blob',oid))}
assert next(iter(authority.values()))['sha256']=='1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e'
src='src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationPreCodec.cs'
test='tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationPreCodecR2Tests.cs'
delta=g('diff','--name-status',q2,base).splitlines()
paths=[line.split('\t')[-1] for line in delta]
assert set(p for p in paths if not p.startswith('docs/'))=={src,test}
assert all(p in [src,test,'docs/handoff.md','docs/work-queue.md',f'docs/evidence/{prefix}codec-r2.md'] or p.startswith(f'docs/evidence/{prefix}codec-r2-evidence/') for p in paths)
pattern=r'StartOwnerPreparation\(|TrySpawnOwner\(|new PreparationOwnershipCoordinator\(|IntegratedRecoveryHost.Run\(|QualifiedRecoveryConnection.Start\(|RecoveryConnection.Start\(|new HeldReleasePhase\(|new B3PreparationOwner\(|OwnerPreparationTransaction.Run\('
def callers(rev):return g('grep','-n','-E',pattern,rev,'--','src','tests').replace(rev+':','')
assert callers(old)==callers(base)
run.mkdir(parents=True,exist_ok=True)
dump=lambda n,obj:(run/n).write_text(json.dumps(obj,indent=2)+'\n',encoding='utf8',newline='\n')
dump('preservation.json',dict(untracked=historical(),refs=g('for-each-ref','--format=%(refname) %(objectname)').splitlines()))
dump('identities.json',dict(commits={i:g('rev-parse',i+'^{tree}') for i in [old,q1,r1,q2,base]},
 ancestry=g('log','--format=%H %P %s',old+'..'+base).splitlines(),branch='main',worktrees=1,stashes=0,clean=True,
 historicalFiles=44,manifests=manifests,normative=authority,delta=delta,code={p:g('rev-parse',base+':'+p) for p in [src,test]}))
dump('scope-diff.json',{'rawGitDiff':git('diff',q2,base,'--',src,test).decode()})
(run/'p4.txt').write_text(callers(base)+'\n',encoding='utf8',newline='\n')
print('Baseline verified. Q1 156, Q2 100, R2 64, historical files 44.')
