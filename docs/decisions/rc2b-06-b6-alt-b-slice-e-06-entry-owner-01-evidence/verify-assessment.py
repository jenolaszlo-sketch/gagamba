"""Read-only assessment integrity audit; no implementation qualification."""
import hashlib
import json
from pathlib import Path
import runpy
import subprocess
import sys

ROOT = Path('C:/Users/Laszlos/source/repos/Gagamba')
BASE = '4dc0ee0998f0def6cb1cba304a76b3a9f59ac904'
TREE = '3136117ecdcbd7df91693db2fbc14a148d47607a'
IMPL = '28895d55de98eea4bc402373b653f7216e092843'
PREFIX = 'docs/decisions/rc2b-06-b6-alt-b-slice-e-06-entry-owner-01'
OWNER = 'docs/decisions/rc2b-06-b6-alt-b-slice-e-05-owner-01-evidence'
Q = runpy.run_path(str(ROOT / 'docs/reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/verify-integrity.py'))
git, txt, get, sha = Q['git'], Q['git_text'], Q['get'], Q['sha']

def audit(final=False):
    head = txt('rev-parse', 'HEAD')
    assert txt('symbolic-ref','HEAD') == 'refs/heads/main'
    assert txt('rev-parse', '--show-toplevel').casefold() == ROOT.as_posix().casefold()
    assert txt('rev-parse', BASE+'^{tree}') == TREE
    assert head == BASE if not final else txt('show','-s','--format=%P',head) == BASE
    assert txt('remote','get-url','origin') == txt('remote','get-url','--push','origin') == 'https://github.com/jenolaszlo-sketch/gagamba.git'
    remote = txt('ls-remote','--exit-code','origin','refs/heads/main')
    assert remote == BASE+'\trefs/heads/main'
    assert txt('rev-parse','refs/remotes/origin/main') == BASE
    git('merge-base','--is-ancestor',IMPL,head)
    assert txt('rev-parse',IMPL+'^{tree}') == 'e314f2e8a1259e2263c8494bb0c8ca9f7eb27016'
    accepted=json.loads((ROOT/OWNER/'acceptance-audit.json').read_bytes())['repository']
    specs=accepted['manifest_sets']+[dict(path=OWNER+'/SHA256SUMS.txt',revision=BASE,count=10)]
    manifests=[Q['verify_manifest'](s,head) for s in specs]
    assert sum(s['count'] for s in manifests)==1054
    for item in accepted['candidate_files']:
        p=item['path']
        assert get(IMPL,p)==get(head,p)==(ROOT/p).read_bytes()
        assert sha(get(head,p))==item['sha256']
    assert not txt('diff','--name-only',IMPL,head,'--','src','tests','eng','.github','native','Gagamba.sln','Directory.Build.props','Directory.Build.targets')
    assert not txt('diff','--name-only') and not txt('diff','--cached','--name-only')
    untracked=json.loads((ROOT/'docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json').read_bytes())
    actual=set(git('ls-files','--others','--exclude-standard','-z').decode().split('\0')[:-1])
    assert len(untracked)==44
    assert actual=={x['path'] for x in untracked}
    for x in untracked:
        b=(ROOT/x['path']).read_bytes()
        assert len(b)==x['length'] and sha(b)==x['sha256']
    worktrees=txt('worktree','list','--porcelain')
    assert worktrees.count('worktree ')==1 and not txt('stash','list')
    design=json.loads((ROOT/'docs/decisions/rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/source-identities.json').read_bytes())
    for copy in design['externalCopies']:
        for item in copy['entries']:
            assert sha((Path(copy['directory'])/item['path']).read_bytes())==item['sha256']
    ab=design['alternativeBCompatibility']
    assert sha(Path(ab['path']).read_bytes())==ab['sha256']
    changes=[]
    new=None
    if final:
        changes=txt('diff','--name-status',BASE,head).splitlines()
        assert changes and all(x.startswith('A\t'+PREFIX) for x in changes)
        new=Q['verify_manifest'](dict(path=PREFIX+'-evidence/SHA256SUMS.txt',revision=head,count=int(sys.argv[3])),head)
        assert {s.split('\t')[1] for s in changes}=={x['path'] for x in new['members']}|{PREFIX+'-evidence/SHA256SUMS.txt'}
    return dict(scope='Documentation assessment integrity only; no suites or native execution',
        head=head,tree=txt('rev-parse','HEAD^{tree}'),branch='main',baseline=BASE,baseline_tree=TREE,
        published_main=remote,origin=txt('remote','get-url','origin'),worktrees=worktrees,stashes=[],
        tracked_checkout_and_index='clean',historical_manifest_entries=1054,manifests=manifests,
        historical_untracked=untracked,candidate_files=accepted['candidate_files'],
        externalCopies=design['externalCopies'],alternativeBCompatibility=ab,
        external_ancestry='Not independently established by local byte comparison',
        new_manifest=new,changes=changes)

if __name__=='__main__':
    result=audit(sys.argv[1]=='final')
    Path(sys.argv[2]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print('PASS: 1,054 historical manifest entries; 44 exact untracked files; E-5 unchanged; clean tracked/index; published main unchanged.')
