"""Verify the read-only Git identities and export the authorized delta."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

source, output = map(Path, sys.argv[1:])
prefix = 'rc2b-06-b6-alt-b-'
ep = prefix + 'slice-e-03-'
entry = '1c7fb672055cc3322eb1633d37e5e84d764cb66d'
candidate = '34eef4c8e7d814e15cb1bfce27df032e75766f19'
later = 'e8099e37c1243e81f173bf0248307cf7d2fe3875'
correction = '18c95f6'
docs = 'docs/decisions/'
reviews = 'docs/reviews/'
evidence = 'docs/evidence/'
source_prefix = 'src/Gagamba.Execution.Linux.M1/Bootstrap/'
test_prefix = 'tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/'
sources = {
    source_prefix + 'ValidationPreCodec.cs': 'e9ac5bfefdc59d3fda2e70c82bf902ccd7f6d361',
    source_prefix + 'ValidationPreCodecBinary.cs': '814c233d6290c111d751c73dbe65e1ec7dc0cc23',
    source_prefix + 'ValidationPreCodecData.cs': 'dbae1d7ed30ec84f1b6b4a78a803b4b43bc1aace',
    test_prefix + 'ValidationPreCodecBinaryTests.cs': '558c702fcb26a1ae4349c950dec4b3e1bc1b0d7e',
    test_prefix + 'ValidationPreCodecBootstrapTests.cs': '995e67f0b734a982399d011f4c6d0a46d68fc629',
    test_prefix + 'ValidationPreCodecVectors.cs': '4880945d6c6177351e929fde15b41babd1529298',
}
normative = {
    evidence + ep + 'v3-pre-frozen-record-candidate-01.md': 'f6a71562845d2cfe0aee5495864a509c5caabe7a',
    docs + ep + 'qe1-frozen-record-owner-01.md': 'd2fea61b7db323319ee59033c8b1b12ded1ffe7d',
    docs + ep + 'qe1-frozen-record-owner-01-evidence': '0d5d92fce4afdc5bc008d1de48aac688c4c9e7f4',
    docs + ep + 'qe1-normative-owner-01.md': 'f3639dc619dbfee39859afb7da553e2167d9b83d',
    docs + ep + 'qe1-bootstrap-base-owner-01.md': '93fe2c9155a21b12fd3486b9da743260897d508e',
    docs + ep + 'qe1-bootstrap-base-owner-01-evidence': 'a78ecb5040f0525d2047641b75ad48dd59cd23d6',
    docs + ep + 'qe1-bootstrap-base-owner-01-evidence/schema.md': 'a7248152d09e19d8f3775db397f340949fa9ee57',
    docs + ep + 'qe1-bootstrap-base-owner-01-evidence/witnesses.md': '19886332f3fb1bcf3149f4e173642a8673fbb746',
    reviews + ep + 'qe1-frozen-record-q1.md': '5f8b92e4874378bb365fcec80d794f964aa6f821',
    reviews + ep + 'qe1-frozen-record-q1-evidence': '1cb5ac2c2b467cbfb1c0d4ac85682fb979ffe961',
    docs + ep + 'entry-owner-01.md': '028a7c1858b5e7b506deff9d2d8d263afe3344ac',
    docs + ep + 'entry-owner-01-evidence': 'ebd4a064e4cbad696f60898940cc2c0f8f4ea6e9',
    docs + ep + 'entry-owner-01-evidence/surfaces-and-gates.md': None,
    reviews + ep + 'codec-implementation-01.md': None,
    reviews + ep + 'codec-implementation-01-evidence': None,
    docs + prefix + 'p4-coverage-owner-acceptance-01.md': '31ae1b08cb4663a6a9d9de657fd972fccb731cf2',
    evidence + prefix + 'p4-coverage-recon-01.md': None,
    docs + prefix + 'slice-e-02-closure-owner-01.md': '3388894f821a4a92aca7c2b2d79c832a0f70a450',
    docs + prefix + 'slice-e-01-owner-acceptance-01.md': None,
}

def git_bytes(*args):
    return subprocess.check_output(['git', '--no-optional-locks', '-C', str(source), *args])

def git(*args):
    return git_bytes(*args).decode('utf-8').strip()

def record(path, known, revisions):
    identities = {git('rev-parse', r): git('rev-parse', f'{r}:{path}') for r in revisions}
    assert len(set(identities.values())) == 1, (path, identities)
    blob = next(iter(identities.values()))
    assert known is None or known == blob, (path, known, blob)
    kind = git('cat-file', '-t', blob)
    result = {'path': path, 'object': blob, 'kind': kind, 'revisions': identities}
    if kind == 'blob':
        raw = git_bytes('cat-file', 'blob', blob)
        result.update(bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())
        checkout = (source / path).read_bytes()
        assert checkout == raw or checkout.replace(b'\r\n', b'\n') == raw, path
        result['checkoutBytesIdentical'] = checkout == raw
    return result

assert git('rev-parse', 'HEAD') == candidate
assert not git('status', '--porcelain')
assert git('rev-parse', candidate + '^') == entry
trees = {r: git('rev-parse', r + '^{tree}') for r in (entry, candidate, later)}
assert list(trees.values()) == ['cd3b29e8e5ad8790e0795418408b8d871e920f58',
                               '5b9bfa884b4cec86aab293b80ada1b7f73b58d11',
                               'a51ed41ee2cba8205a930749f63244c5491898c5']
chain = git('log', '--format=%H %P %s', '--ancestry-path', f'{entry}..{later}').splitlines()
assert len(chain) == 3 and chain[0].startswith(later) and chain[1].startswith(correction) and chain[2].startswith(candidate)
added = git('diff', '--name-status', entry, candidate).splitlines()
non_docs = [line for line in added if not line.split('\t')[-1].startswith('docs/')]
assert set(non_docs) == {f'A\t{path}' for path in sources}
post_delta = git('diff', '--name-status', candidate, later).splitlines()
assert {line.split('\t')[-1] for line in post_delta if not line.split('\t')[-1].startswith('docs/')} == {
    '.gitattributes', 'eng/tests/test_preparation_checkout.py', 'eng/verify-preparation.ps1'}
all_inputs = [record(path, oid, [candidate, later] if 'codec-implementation' in path else [entry, candidate, later])
              for path, oid in normative.items()]
assert all_inputs[0]['sha256'] == '1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e'
p4_recon = next(i for i in all_inputs if i['path'].endswith('p4-coverage-recon-01.md'))
assert p4_recon['sha256'] == 'b3f851aa878d64315af70902d5148819066b684f7d3d3e35e1c4bc49c2ec4d0e'
checked_sources = [record(path, oid, [candidate, correction, later]) for path, oid in sources.items()]
e2 = '21179680e7ae5d153c7ddeef5d209e5ac450cdd7'
assert not git('diff', '--name-only', e2, entry, '--', '.', ':(exclude)docs/**')
historical_chain = git('log', '--format=%H %P %s', '--ancestry-path', f'{e2}..{entry}').splitlines()
output.write_text(json.dumps(dict(trees=trees, chain=chain, entryDelta=added, postDelta=post_delta,
    sources=checked_sources, inputs=all_inputs, frozenAuthorityChain=historical_chain, farBoundary=e2), indent=2) + '\n',
    encoding='utf-8')
print(f'PASS: {len(checked_sources)} authorized code/test blobs, {len(all_inputs)} input objects, pinned record SHA-256, ancestry, and delta')
