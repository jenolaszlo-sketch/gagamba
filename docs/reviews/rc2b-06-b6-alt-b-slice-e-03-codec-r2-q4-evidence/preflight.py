import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys

repo = Path(sys.argv[1]).resolve()
work = Path(__file__).resolve().parent
baseline = 'ffdad5d05fefb6d260c884d61c791c6d9be91df5'
q3 = '29da0ed60c18ed6364bf9366a64ab5aa3393db62'
r2 = '8be61181e804b2dfafb0797eb54c92d2f234ff86'
prefix = 'rc2b-06-b6-alt-b-slice-e-03-'
commits = {
    baseline: '9aef7acf0f865fcc355b7e01c0e9b021eb0ceff0',
    q3: '52364b4c62098fc0fc75fdda26bd2bd8f099c760',
    r2: '1c9307845858fb25ca0a9473d53d1fd1496018fc',
    '4cb46a6cdbcc52b7129dbd88c4eba2e62ae73469': '0975cc5fbd13c6342c219333029e82e3d02c94aa',
    '4a97b79e0060b001a7c1d30ae93bc66cedc52418': 'dfd9519b7799155a0fd6f99a8edad27618c38a6a',
    '7713ec58cc7c292862c8c80d6b57dcacd3420b44': 'e7f0854d5365aef6533d6ba731eebadc8638bac0',
    '34eef4c8e7d814e15cb1bfce27df032e75766f19': '5b9bfa884b4cec86aab293b80ada1b7f73b58d11',
}


def git(*args):
    return subprocess.check_output(['git', '--no-optional-locks', '-C', str(repo), *args])


def g(*args):
    return git(*args).decode('utf-8').strip()


def sha(data):
    return hashlib.sha256(data).hexdigest()


def blob(revision, path):
    return git('cat-file', 'blob', f'{revision}:{path}')


def verify_manifest(root, revision, count):
    manifest = root / 'SHA256SUMS.txt'
    rel = manifest.relative_to(repo).as_posix()
    raw = blob(revision, rel)
    assert raw == manifest.read_bytes() == blob('HEAD', rel), rel
    rows = [line.split('  ', 1) for line in raw.decode().splitlines()]
    assert len(rows) == len({path for _, path in rows}) == count, rel
    for digest, path in rows:
        file = (root / path).resolve()
        rel_file = file.relative_to(repo).as_posix()
        assert sha(blob(revision, rel_file)) == sha(blob('HEAD', rel_file)) == sha(file.read_bytes()) == digest, rel_file
    return {'entries': count, 'manifestSha256': sha(raw)}


def verify():
    head = g('rev-parse', 'HEAD')
    assert Path(g('rev-parse', '--show-toplevel')).resolve() == repo
    assert g('symbolic-ref', 'HEAD') == 'refs/heads/main'
    assert g('worktree', 'list', '--porcelain').splitlines() == [f'worktree {repo.as_posix()}', f'HEAD {head}', 'branch refs/heads/main']
    assert not g('status', '--porcelain=v1', '-uno') and not g('stash', 'list')
    assert not list((repo / '.git').glob('*.lock'))
    for commit, tree in commits.items():
        assert g('rev-parse', f'{commit}^{{tree}}') == tree
        git('merge-base', '--is-ancestor', commit, head)
    chain = list(commits)
    for child, parent in zip(chain[:5], chain[1:6]):
        assert g('rev-parse', child + '^') == parent
    assert not g('diff', '--name-only', r2, head, '--', '.', ':(exclude)docs/**')
    untracked = {p: sha((repo / p).read_bytes()) for p in git('ls-files', '--others', '--exclude-standard', '-z').decode().split('\0') if p}
    previous = json.loads((work.parent / 'gagamba-e03-owner-01/run/private-state.json').read_bytes())
    assert untracked == previous['untracked'] and len(untracked) == 44
    refs = g('for-each-ref', '--format=%(refname) %(objectname)').splitlines()
    assert [x for x in refs if not x.startswith('refs/heads/main ')] == [x for x in previous['refs'] if not x.startswith('refs/heads/main ')]
    historical = {}
    for directory, name, revision, count in (
        ('reviews', 'codec-q1', chain[5], 156), ('evidence', 'codec-r1', chain[4], 0),
        ('reviews', 'codec-r1-q2', chain[3], 100), ('evidence', 'codec-r2', r2, 64),
        ('reviews', 'codec-r2-q3', q3, 77), ('decisions', 'owner-01', baseline, 9),
    ):
        path = f'docs/{directory}/{prefix}{name}'
        assert not g('diff', '--name-only', revision, head, '--', path + '.md', path + '-evidence')
        record = {'report': g('rev-parse', f'{head}:{path}.md'), 'evidenceTree': g('rev-parse', f'{head}:{path}-evidence')}
        if count:
            record.update(verify_manifest(repo / (path + '-evidence'), revision, count))
        historical[name] = record
    for before, after, allowed in (
        (r2, q3, 'docs/reviews/' + prefix + 'codec-r2-q3'),
        (q3, baseline, 'docs/decisions/' + prefix + 'owner-01'),
    ):
        allowed_status = set() if after == q3 else {'docs/PROJECT-STATUS.md', 'docs/handoff.md', 'docs/work-queue.md'}
        changes = g('diff', '--name-only', before, after).splitlines()
        assert changes and all(p in allowed_status or p == allowed + '.md' or p.startswith(allowed + '-evidence/') for p in changes)
    ledger = json.loads(blob(q3, f'docs/reviews/{prefix}codec-r2-q3-evidence/raw/identities.json'))
    for path, identity in ledger['normative'].items():
        raw = blob(head, path)
        assert g('rev-parse', f'{r2}:{path}') == g('rev-parse', f'{head}:{path}') == identity['blob']
        assert sha(raw) == identity['sha256']
        assert (repo / path).read_bytes().replace(b'\r\n', b'\n') == raw.replace(b'\r\n', b'\n')
    assert ledger['normative'][f'docs/evidence/{prefix}v3-pre-frozen-record-candidate-01.md']['blob'] == 'f6a71562845d2cfe0aee5495864a509c5caabe7a'
    return {'head': head, 'tree': g('rev-parse', 'HEAD^{tree}'), 'commits': commits,
            'branch': 'main', 'worktrees': 1, 'trackedClean': True, 'stashes': 0,
            'historicalUntrackedCount': len(untracked), 'historicalEvidenceChecksums': 397,
            'ownerEvidenceChecksums': 9, 'historical': historical, 'normative': ledger['normative'],
            'r2ToHeadChangesOutsideDocs': [], 'remoteTracking': g('rev-parse', 'refs/remotes/origin/main')}


if __name__ == '__main__':
    assert g('rev-parse', 'HEAD') == baseline
    result = verify()
    run = work / 'run'
    run.mkdir(exist_ok=True)
    (run / 'preflight.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print('Verified canonical main, exact ancestry and governing identities, clean tracked state, 44 historical files, 397 historical and 9 owner hashes, and zero non-documentation drift.')
