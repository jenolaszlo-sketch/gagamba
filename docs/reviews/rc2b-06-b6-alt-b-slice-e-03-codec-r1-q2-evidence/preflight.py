import hashlib, json, os, re, subprocess, sys
from pathlib import Path

repo, run, prev = (Path(p).resolve() for p in sys.argv[1:])
old='34eef4c8e7d814e15cb1bfce27df032e75766f19'
remote='e8099e37c1243e81f173bf0248307cf7d2fe3875'
imp='7713ec58cc7c292862c8c80d6b57dcacd3420b44'
candidate='4a97b79e0060b001a7c1d30ae93bc66cedc52418'
adopted='docs/evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md'
r1='docs/evidence/rc2b-06-b6-alt-b-slice-e-03-codec-r1'
q1='docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-q1'
code_paths={
 'src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationPreCodec.cs':'a8e2f1d09698bdf00fd899597cc0b637abae72f4',
 'src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationPreCodecData.cs':'ee8a7f00b160043dbd6bdad9b2bec945a21377a5',
 'tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationPreCodecRemediationTests.cs':'c5f8c763b5002c6bee3ebd908cb96b0edc910cce',
}
env={**os.environ,'GIT_OPTIONAL_LOCKS':'0'}
def git(*args):
    return subprocess.check_output(['git','-C',str(repo),*args],env=env)
def g(*args):
    return git(*args).decode().strip()
def sha(b):
    return hashlib.sha256(b).hexdigest()
def inventory():
    return {x:sha((repo/x).read_bytes()) for x in git('ls-files','--others','--exclude-standard','-z').decode().split('\0') if x}
def verify_q1(base):
    annex=q1+'-evidence'
    path=annex+'/SHA256SUMS.txt'
    manifest=(repo/path).read_bytes()
    assert sha(manifest)=='9739a964e1300444e9377bdd08c1f1e952b8aaf9f2feae9129b0c510c623ece1'
    assert git('cat-file','blob',base+':'+path)==manifest
    rows=manifest.decode().splitlines()
    assert len(rows)==156
    for line in rows:
        digest,name=line.split('  ',1)
        f=(repo/annex/name).resolve()
        assert sha(f.read_bytes())==sha(git('cat-file','blob',base+':'+f.relative_to(repo).as_posix()))==digest
    return len(rows)
assert g('rev-parse','--show-toplevel')==repo.as_posix()
assert g('symbolic-ref','HEAD')=='refs/heads/main'
assert g('rev-parse','HEAD')==candidate
assert g('rev-parse','HEAD^{tree}')=='dfd9519b7799155a0fd6f99a8edad27618c38a6a'
assert g('rev-parse','refs/remotes/origin/main')==remote
assert g('rev-list','--left-right','--count','HEAD...refs/remotes/origin/main').split()==['2','0']
assert g('rev-parse','HEAD^')==imp and g('rev-parse',imp+'^')==remote
git('merge-base','--is-ancestor',old,remote)
assert git('worktree','list','--porcelain').count(b'worktree ')==1
assert not g('stash','list') and not g('status','--porcelain','--untracked-files=no')
assert not any((repo/'.git').glob('*.lock'))
untracked=inventory()
assert len(untracked)==44 and untracked==json.loads(prev.read_text())['untracked']
q1_count=verify_q1(candidate)
assert not g('diff','--name-only',imp,candidate,'--',q1+'.md',q1+'-evidence')
for path,oid in code_paths.items(): assert g('rev-parse',candidate+':'+path)==oid
delta=g('diff','--name-status',imp,candidate).splitlines()
def allowed(p):
    return p in code_paths or p in [r1+'.md','docs/handoff.md','docs/work-queue.md'] or p.startswith(r1+'-evidence/')
assert all(allowed(line.split('\t')[-1]) for line in delta)
assert {line.split('\t')[-1] for line in delta if not line.split('\t')[-1].startswith('docs/')}==code_paths.keys()
assert not g('diff','--name-only',old,imp,'--','src','tests','native')
sources=[adopted,'docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-owner-01.md',
 'docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-normative-owner-01.md',
 'docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01.md',
 'docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/schema.md',
 'docs/decisions/rc2b-06-b6-alt-b-slice-e-03-entry-owner-01.md',
 'docs/decisions/rc2b-06-b6-alt-b-slice-e-03-entry-owner-01-evidence/surfaces-and-gates.md',
 'docs/decisions/rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md',
 'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-implementation-01.md',
 q1+'.md', r1+'.md', r1+'-evidence/closure-matrix.md']
records={x:{'blob':g('rev-parse',candidate+':'+x),'rawSha256':sha(git('cat-file','blob',candidate+':'+x))} for x in sources}
assert records[adopted]=={'blob':'f6a71562845d2cfe0aee5495864a509c5caabe7a','rawSha256':'1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e'}
assert not g('diff','--name-only',old,candidate,'--',*[x for x in sources[:-3]])
assert '**REMEDIATION REQUIRED.' in (repo/(q1+'.md')).read_text(encoding='utf8')
pattern=r'StartOwnerPreparation\(|TrySpawnOwner\(|new PreparationOwnershipCoordinator\(|IntegratedRecoveryHost.Run\(|QualifiedRecoveryConnection.Start\(|RecoveryConnection.Start\(|new HeldReleasePhase\(|new B3PreparationOwner\(|OwnerPreparationTransaction.Run\('
def callers(rev): return g('grep','-n','-E',pattern,rev,'--','src').replace(rev+':','')
assert callers(old)==callers(candidate)
s28=g('grep','-n','-F','new PreparationOwnershipCoordinator(',candidate,'--','tests')
assert len(s28.splitlines())==1 and 'PreparationOwnershipTests.cs:61' in s28
refs=g('for-each-ref','--format=%(refname) %(objectname)').splitlines()
run.mkdir(parents=True,exist_ok=True)
dump=lambda f,v:(run/f).write_text(json.dumps(v,indent=2)+'\n',encoding='utf8',newline='\n')
dump('preflight-private.json',{'refs':refs,'untracked':untracked})
dump('identities.json',dict(repo=str(repo),commits={x:g('rev-parse',x+'^{tree}') for x in [old,remote,imp,candidate]},
 ancestry=g('log','--format=%H %P %s',old+'..'+candidate).splitlines(),tracking=[2,0],soleWorktree=True,
 stashes=0,trackedClean=True,untrackedFiles=len(untracked),q1Checksums=q1_count,codeBlobs=code_paths,authorityAndEvidence=records,r1Delta=delta))
(run/'scope-implementation.patch.txt').write_bytes(git('diff',imp,candidate,'--',*code_paths))
(run/'p4-callers.txt').write_text('Original and R1 production inventories match.\n'+callers(candidate)+'\n\n'+s28+'\n',encoding='utf8',newline='\n')
print(f'Preflight passed. {q1_count} Q1 hashes, {len(untracked)} historical files matched R1 preflight, {len(delta)} authorized R1 paths.')
