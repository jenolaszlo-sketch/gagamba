import hashlib
import json
from pathlib import Path
import subprocess
import sys

root = Path(__file__).resolve().parent
repo = root.parent.parent / 'Gagamba'
candidate = '3c16b53b288b39de3fb36964eecc8d5cccbc62b6'
authorization = 'db44b7e50c6ce449fdb6b596ad52744cf10443c0'
r2 = '8be61181e804b2dfafb0797eb54c92d2f234ff86'
base = 'rc2b-06-b6-alt-b-slice-e-04-'
producer = f'docs/evidence/{base}codec-implementation-01'
assessment = f'docs/decisions/{base}entry-owner-01'
auth = f'docs/decisions/{base}entry-auth-01'


def git(*args):
    return subprocess.check_output(['git', '--no-optional-locks', *args], cwd=repo)


def g(*args):
    return git(*args).decode().strip()


def sha(content):
    return hashlib.sha256(content).hexdigest()


def blob(revision, path):
    return git('cat-file', 'blob', f'{revision}:{path}')


def assert_checkout(path, content):
    actual = (repo / path).read_bytes()
    assert actual == content or actual.replace(b'\r\n', b'\n') == content, path


def manifest(stem, count, revision):
    folder = Path(f'{stem}-evidence')
    path = (folder / 'SHA256SUMS.txt').as_posix()
    content = blob(revision, path)
    assert blob('HEAD', path) == content, path
    assert_checkout(path, content)
    rows = [row.split('  ', 1) for row in content.decode().splitlines()]
    assert len(rows) == len({name for _, name in rows}) == count, path
    for digest, name in rows:
        member = (repo / folder / name).resolve().relative_to(repo).as_posix()
        raw = blob(revision, member)
        assert sha(raw) == digest and blob('HEAD', member) == raw, member
        assert_checkout(member, raw)
    return {'count': count, 'manifestSha256': sha(content), 'revision': revision}


head = g('rev-parse', 'HEAD')
if '--post' in sys.argv:
    assert g('rev-parse', 'HEAD^') == candidate
else:
    assert head == candidate
assert g('rev-parse', f'{candidate}^{{tree}}') == '2f30e0dc72d1565ac312c0c052ef820a03a3dea0'
assert g('rev-parse', f'{candidate}^') == authorization
git('merge-base', '--is-ancestor', r2, authorization)
assert Path(g('rev-parse', '--show-toplevel')).resolve() == repo.resolve()
assert g('symbolic-ref', 'HEAD') == 'refs/heads/main'
assert g('worktree', 'list', '--porcelain').splitlines() == [f'worktree {repo.as_posix()}', f'HEAD {head}', 'branch refs/heads/main']
assert not g('status', '--porcelain=v1', '--untracked-files=no')
assert not g('diff', '--cached', '--name-only')

inventory_path = 'docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json'
inventory = blob(candidate, inventory_path)
assert sha(inventory) == '7d6abbd93c9703626d07455e1e68c18b2538d45847549cb3d39a4318637fc7e1'
assert_checkout(inventory_path, inventory)
previous = {row['path']: row['sha256'] for row in json.loads(inventory)}
untracked = {p: sha((repo / p).read_bytes()) for p in g('ls-files', '--others', '--exclude-standard', '-z').split('\0') if p}
assert len(untracked) == 44 and untracked == previous
source_dirs = ('src/Gagamba.Execution.Linux.M1/Bootstrap', 'tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap')
allowed = {f'{source_dirs[0]}/{p}.cs' for p in ('Gava1ContextCodec', 'Gava1ContextData')} | {
    f'{source_dirs[1]}/{p}.cs' for p in ('Gava1ContextCodecTests', 'Gava1ContextVectors')}
assert set(g('diff', '--name-status', authorization, candidate, '--', '.', ':(exclude)docs/**').splitlines()) == {f'A\t{p}' for p in allowed}
assert not g('diff', '--name-only', r2, authorization, '--', '.', ':(exclude)docs/**')
delta = g('diff', '--name-only', authorization, candidate).splitlines()
status_docs = {'docs/PROJECT-STATUS.md', 'docs/handoff.md', 'docs/work-queue.md'}
assert all(p in allowed | status_docs or p == producer + '.md' or p.startswith(producer + '-evidence/') for p in delta)
if head != candidate:
    q1 = f'docs/reviews/{base}codec-q1'
    delta = g('diff', '--name-status', candidate, head).splitlines()
    assert delta and all(row.startswith('A\t') and (row[2:] == q1 + '.md' or row[2:].startswith(q1 + '-evidence/')) for row in delta)

prefix = 'rc2b-06-b6-alt-b-slice-e-03-'
history = {
    ('reviews', 'codec-q1'): ('7713ec58cc7c292862c8c80d6b57dcacd3420b44', 156),
    ('reviews', 'codec-r1-q2'): ('4cb46a6cdbcc52b7129dbd88c4eba2e62ae73469', 100),
    ('evidence', 'codec-r2'): (r2, 64),
    ('reviews', 'codec-r2-q3'): ('29da0ed60c18ed6364bf9366a64ab5aa3393db62', 77),
    ('decisions', 'owner-01'): ('ffdad5d05fefb6d260c884d61c791c6d9be91df5', 9),
    ('reviews', 'codec-r2-q4'): ('0d64fcd199997bf3bede3b328f44aaf211048daa', 132),
    ('decisions', 'owner-02'): ('6892ab0e235b2eaab943f218545cbe35ca95fd1b', 6),
}
manifests = {f'docs/{folder}/{prefix}{stem}': (count, revision) for (folder, stem), (revision, count) in history.items()}
manifests.update({assessment: (9, g('rev-parse', authorization + '^')), auth: (6, authorization), producer: (129, candidate)})
verified = {stem: manifest(stem, count, revision) for stem, (count, revision) in manifests.items()}
historical_trees = {}
for directory, stem, revision in (('reviews', 'codec-implementation-01', '34eef4c8e7d814e15cb1bfce27df032e75766f19'),
                                  ('evidence', 'codec-r1', '4a97b79e0060b001a7c1d30ae93bc66cedc52418')):
    # The original implementation report and annex are under reviews.
    for suffix in ('.md', '-evidence'):
        p = f'docs/{directory}/{prefix}{stem}{suffix}'
        identity = g('rev-parse', f'{revision}:{p}')
        assert g('rev-parse', f'HEAD:{p}') == identity
        historical_trees[p] = identity

normative = {
    f'{assessment}-evidence/gava1-frozen-excerpt.txt': '23d6ed7d95e4dd532de7e7f3a0f3a6de665b50a1',
    f'docs/evidence/{prefix}v3-pre-frozen-record-candidate-01.md': 'f6a71562845d2cfe0aee5495864a509c5caabe7a',
}
normative_binding = {}
for p, oid in normative.items():
    assert g('rev-parse', f'HEAD:{p}') == oid
    content = blob(head, p)
    assert_checkout(p, content)
    normative_binding[p] = {'blob': oid, 'sha256': sha(content)}
governing = json.loads(blob(candidate, f'{assessment}-evidence/governing-identities.json'))
for p, pinned in governing['governingGitRecords'].items():
    content = blob(candidate, p)
    assert g('rev-parse', f'{head}:{p}') == pinned['blob'] and sha(content) == pinned['sha256'], p
    assert_checkout(p, content)
f02_root = root.parent / 'gagamba-f02-authority-mechanism-decision-01'
f02_manifest = (f02_root / 'recheck-input/SHA256SUMS.txt').read_bytes()
assert sha(f02_manifest) == governing['F02FrozenManifestSha256']
assert f02_manifest == (f02_root / 'recheck-input-SHA256SUMS.txt').read_bytes()
f02_rows = {name: digest for digest, name in (row.split('  ', 1) for row in f02_manifest.decode().splitlines())}
assert f02_rows == governing['F02VerifiedManifestEntries'] and len(f02_rows) == 15
for p, digest in f02_rows.items():
    assert digest == sha((f02_root / p).read_bytes()) == sha((f02_root / 'recheck-input' / p).read_bytes()), p
start, end = governing['GAVA1Excerpt']['sourceLinesInclusive']
excerpt = b''.join((f02_root / 'actor-and-descriptor-binding.md').read_bytes().splitlines(keepends=True)[start - 1:end])
assert excerpt == blob(candidate, f'{assessment}-evidence/gava1-frozen-excerpt.txt')
sources = {}
producer_bound = json.loads(blob(candidate, f'{producer}-evidence/identity-and-results.json'))['testedSourceSha256']
for p in sorted(allowed):
    content = blob(candidate, p)
    assert_checkout(p, content)
    assert sha(content) == producer_bound[p]
    sources[p] = {'blob': g('rev-parse', f'HEAD:{p}'), 'sha256': sha(content)}

result = {
    'path': repo.as_posix(), 'branch': 'main', 'worktrees': 1, 'trackedAndIndexClean': True,
    'head': head, 'tree': g('rev-parse', 'HEAD^{tree}'), 'authorization': authorization, 'acceptedE3': r2,
    'historicalUntrackedCount': len(untracked), 'untrackedInventoryReferenceSha256': sha(inventory),
    'untrackedPathAndHashFingerprint': sha(json.dumps(untracked, sort_keys=True).encode()),
    'remoteTracking': g('rev-parse', 'refs/remotes/origin/main'), 'normative': normative_binding,
    'manifests': verified, 'historicalTrees': historical_trees, 'source': sources,
    'governingRecordCount': len(governing['governingGitRecords']),
    'f02VerifiedEntriesPerCopy': len(f02_rows), 'f02ManifestSha256': sha(f02_manifest),
}
receipt = root / 'run' / ('postflight.json' if head != candidate else 'preflight.json')
receipt.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result, indent=2))
