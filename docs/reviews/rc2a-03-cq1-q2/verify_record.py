"""Q2 read-only Git/evidence verification; writes only its companion JSON.

No builds, test execution, native probes, package operations or producer writes.
Run from any directory with Python. Narrative conclusions require source review.
"""
import hashlib
import json
import pathlib
import platform
import posixpath
import re
import subprocess
import urllib.parse
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[3]
OUT = pathlib.Path(__file__).resolve().parent
PRODUCER = ROOT.parent / 'rc2a-03-cq1'
BASE = '9519e7cb612af6ff69c93c0d128553138533d9fd'
PRIOR = 'c946df0c93dde414c744f46cce054e7cd3f2c26e'
Q1 = 'dd7e47d2c5f75af126ce4a081013bf21182d73ee'
TARGET = '271e34b45fa504f6bb7ef7359c4f2f1a9649e49d'
PKG = 'docs/evidence/restricted-consumer-contract-rc2a-03-cq1/'
API = 'docs/design/restricted-consumer-contract-rc2a-01-api.md'
Q1DIR = 'docs/reviews/rc2a-03-cq1-q1/'


def git(*args, root=ROOT):
    return subprocess.check_output(['git', '-C', str(root), *args])


def text(*args, root=ROOT):
    return git(*args, root=root).decode('utf-8').strip()


def data(commit, path):
    return git('show', f'{commit}:{path}')


def sha(value):
    return hashlib.sha256(value).hexdigest()


def identity(commit):
    value = text('show', '-s', '--format=%H %T %P', commit).split()
    return dict(commit=value[0], tree=value[1], parents=value[2:])


def binding(commit, path):
    value = data(commit, path)
    return dict(commit=text('rev-parse', commit), path=path,
                object=text('rev-parse', f'{commit}:{path}'),
                type=text('cat-file', '-t', f'{commit}:{path}'),
                bytes=len(value), sha256=sha(value))


def section(document, heading):
    start = document.index(heading)
    end = document.find('\n## ', start + len(heading))
    return document[start:end if end >= 0 else None]


def table_rows(document, prefix):
    return {match[1]: match[0] for match in re.finditer(
        r'^\| (' + prefix + r'\d+) \|.*$', document, re.MULTILINE)}


def producer_state():
    return dict(commit=text('rev-parse', 'HEAD', root=PRODUCER),
                branch=text('branch', '--show-current', root=PRODUCER),
                status=text('status', '--porcelain=v1', '--untracked-files=all', root=PRODUCER))


result = dict(target=identity(TARGET), prior=identity(PRIOR), baseline=identity(BASE),
              q1=identity(Q1), reviewBranch=text('branch', '--show-current'),
              reviewWorktree=str(ROOT), producerBefore=producer_state(),
              environment=dict(os=platform.platform(), python=platform.python_version(),
                               git=text('--version')))
assert result['target']['tree'] == 'f3f63129be98c5c9f9efba3302da331ae65139ee'
assert result['target']['parents'] == [PRIOR]
assert result['prior']['parents'] == [BASE]
assert result['producerBefore']['commit'] == TARGET and not result['producerBefore']['status']
assert result['producerBefore']['branch'] == 'qualification/gagamba-rc2a-03-cq1'
assert result['reviewBranch'] == 'review/gagamba-rc2a-03-cq1-q1'

changed = text('diff', '--name-only', PRIOR, TARGET).splitlines()
expected = {PKG + name for name in ['README.md', 'SHA256SUMS', 'reviewer-handoff.md', 'traceability-matrix.md']}
expected.update(['docs/handoff.md', 'docs/work-queue.md',
                 'docs/plans/restricted-consumer-contract-implementation.md',
                 'docs/plans/restricted-consumer-contract-rc2a-02-qualification.md'])
assert set(changed) == expected
result['changedPaths'] = changed
result['rawDiff'] = text('diff', '--raw', '--no-abbrev', PRIOR, TARGET)
result['diffCheck'] = subprocess.run(['git', '-C', str(ROOT), 'diff', '--check', PRIOR, TARGET], capture_output=True).returncode
assert result['diffCheck'] == 0
result['protectedObjects'] = {}
for path in ['src', 'native', 'tests', 'docs/design', 'docs/decisions', 'docs/reviews', API]:
    objects = {commit: text('rev-parse', f'{commit}:{path}') for commit in [BASE, PRIOR, TARGET]}
    assert len(set(objects.values())) == 1
    result['protectedObjects'][path] = objects
assert not text('diff', '--name-only', BASE, TARGET, '--', 'docs/assessments')

q1_receipt = json.loads(data(Q1, Q1DIR + 'git-binding.json'))
result['preservedQ1Artifacts'] = []
for row in q1_receipt['reportArtifacts']:
    observed = binding(Q1, row['path'])
    assert observed['object'] == row['gitBlob'] and observed['bytes'] == row['committedBytes']
    assert observed['sha256'] == row['committedSHA256']
    assert data('HEAD', row['path']) == data(Q1, row['path'])
    result['preservedQ1Artifacts'].append(observed)
assert data('HEAD', Q1DIR + 'git-binding.json') == data(Q1, Q1DIR + 'git-binding.json')
q1_results = json.loads(data(Q1, Q1DIR + 'verification.json'))
result['q1ResultsReused'] = dict(
    checkpointCount=len(q1_results['checkpoints']),
    checkpointsMatch=all(row['match'] for row in q1_results['checkpoints']),
    historicalManifests={name: [pkg['matched'], pkg['count']] for name, pkg in q1_results['manifests'].items()},
    requiredMultisetsEqual=q1_results['02BRequiredMultisetsEqual'],
    publicMultisetsEqual=q1_results['02BPublicMultisetsEqual'],
    note='Historical Q1 results, not new execution or a repeated full qualification audit.')
assert result['q1ResultsReused']['checkpointsMatch']
result['historicalPackagePreservation'] = {}
for name in ['restricted-consumer-contract-rc2a-02a-windows',
             'restricted-consumer-contract-rc2a-02b-portability',
             'restricted-consumer-contract-rc2a-02c-q2as-rcd04']:
    path = 'docs/evidence/' + name
    before = text('rev-parse', f'{PRIOR}:{path}')
    after = text('rev-parse', f'{TARGET}:{path}')
    assert before == after
    result['historicalPackagePreservation'][path] = after

original = data(PRIOR, PKG + 'traceability-matrix.md').decode()
current = data(TARGET, PKG + 'traceability-matrix.md').decode()
original_readme = data(PRIOR, PKG + 'README.md').decode()
readme = data(TARGET, PKG + 'README.md').decode()
result['rowAudit'] = {}
for prefix, count, changed_ids in [('C', 13, {'C4', 'C7', 'C10', 'C12'}), ('Q', 9, {'Q3'}), ('X', 5, set())]:
    old, new = table_rows(original, prefix), table_rows(current, prefix)
    assert len(old) == len(new) == count and set(old) == set(new)
    changed_rows = [key for key in old if old[key] != new[key]]
    assert set(changed_rows) == changed_ids
    result['rowAudit'][prefix] = dict(count=count, changed=changed_rows,
                                    unchanged=[key for key in old if old[key] == new[key]])
result['securityAndQualificationsSectionsUnchanged'] = all(
    section(original_readme, heading) == section(readme, heading)
    for heading in ['## 4. Security boundary', '## 5. Inherited qualifications'])
assert result['securityAndQualificationsSectionsUnchanged']
assert data(PRIOR, PKG + 'source-binding.json') == data(TARGET, PKG + 'source-binding.json')
source_binding = json.loads(data(TARGET, PKG + 'source-binding.json'))
result['inheritedCommits'] = [identity(row['commit']) for row in source_binding['inheritedEvidenceChain']]

fsa_commit = '058a150130c5f112c7e9b8189bba8842cf0cec52'
fsa_path = 'docs/assessments/rc2a-02-full-stage-acceptance-gap-assessment.md'
fsa = data(fsa_commit, fsa_path).decode()
fsa_section = section(fsa, '## 4. Stage exit-criteria matrix')
decision_path = 'docs/decisions/restricted-consumer-contract-rc2a-02-full-stage-owner-acceptance.md'
decision = data(BASE, decision_path).decode()
fsa_rows = table_rows(fsa_section, 'E')
assert len(fsa_rows) == 8
assert all('SATISFIED WITH QUALIFICATIONS' in fsa_rows[key] for key in ['E2', 'E6'])
assert identity(fsa_commit)['tree'] == 'b2211a26c838f98f994602e5c56ea8bb95b2c1d8'
assert identity(fsa_commit)['parents'] == identity(BASE)['parents']
assert text('rev-parse', f'{fsa_commit}:{fsa_path}') == '5b4f559d71e8bb6b871a28f73be694b742887bdd'
assessment_ancestor = subprocess.run(['git', '-C', str(ROOT), 'merge-base', '--is-ancestor', fsa_commit, TARGET]).returncode
assert assessment_ancestor == 1
result['F01'] = dict(assessment=binding(fsa_commit, fsa_path), identity=identity(fsa_commit),
                    section4=fsa_section, ownerDecision=binding(BASE, decision_path),
                    decisionSections=[line for line in decision.splitlines() if re.match(r'## [5-8]\.', line)],
                    assessmentIsAncestor=False, siblingParent=identity(fsa_commit)['parents'][0])

a_commit = 'c1a01761f8d58136a381ad87269635c6c14ad093'
codec_path = 'src/Gagamba.Execution/Restricted/Contracts/RestrictedManifestCodec.cs'
test_path = 'tests/Gagamba.Execution.Tests/Restricted/Rc2a02PublicBoundaryTests.cs'
codec = data(TARGET, codec_path).decode()
tests = data(a_commit, test_path).decode()
method = codec[codec.index('public static bool VerifySha256'):]
assert method.index('ThrowIfNull(manifest)') < method.index('expectedSha256.Length != 32') < method.index('FixedTimeEquals')
assert 'FixedTimeEquals(ComputeSha256(manifest).AsSpan(), expectedSha256)' in method
assert 'changed[0] ^= 1' in tests
assert 'Assert.False(RestrictedManifestCodec.VerifySha256(View(), changed))' in tests
assert 'Assert.False(RestrictedManifestCodec.VerifySha256(View(), new byte[32]))' in tests
assert data(a_commit, codec_path) == data(TARGET, codec_path)
assert data(a_commit, test_path) == data(TARGET, test_path)
trx_path = 'docs/evidence/restricted-consumer-contract-rc2a-02a-windows/runs/windows-public.trx'
trx = ET.fromstring(data(a_commit, trx_path))
digest_results = [{'name': row.attrib['testName'], 'outcome': row.attrib['outcome']}
                  for row in trx.findall('.//{*}UnitTestResult')
                  if re.search(r'\.Q2A_B_2[0-3]_', row.attrib['testName'])]
assert len(digest_results) == 8 and all(row['outcome'] == 'Passed' for row in digest_results)
result['F02'] = dict(codec=binding(TARGET, codec_path), tests=binding(a_commit, test_path),
                    api=binding(TARGET, API), acceptedTRX=binding(a_commit, trx_path),
                    digestResults=digest_results, method=method,
                    testExecution='NOT RUN; read existing accepted test source and TRX')

review_path = 'docs/reviews/restricted-consumer-rc2a-01-glm-independent.md'
acceptance_path = 'docs/decisions/restricted-consumer-rc2a-01-owner-acceptance.md'
rc1_path = 'docs/decisions/restricted-consumer-rc1-owner-acceptance.md'
origin_review = data('8e41bab', review_path).decode()
origin_decision = data('49ebbfec', acceptance_path).decode()
rc1 = data('9a705335', rc1_path).decode()
assert 'R2A-F03' not in rc1
origin_row = next(line for line in section(origin_decision, '## 4. Finding dispositions').splitlines() if line.startswith('| R2A-F03 |'))
assert 'ACCEPT AS DISCLOSED' in origin_row and 'No binary reproducibility' in origin_row
assert 'historical citation caveat' in readme and 'inherited citation\nerror' in readme
assert 'RC1-A acceptance §4' in section(decision, '## 6. Carried-forward qualifications')
result['F03'] = dict(review=binding('8e41bab', review_path), acceptance=binding('49ebbfec', acceptance_path),
                    originalFinding=origin_review[origin_review.index('**R2A-F03'):origin_review.index('**R2A-F04')],
                    originalDisposition=origin_row, rc1=binding('9a705335', rc1_path), rc1ContainsR2AF03=False,
                    laterAcceptance=binding(BASE, decision_path), historicalAttributionErrorRetainedAndDisclosed=True)

inventory_refs = [
    ('62b75f5866d0c339816712baeb04baa386b06f85', 'docs/evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04/runs/windows/final/public-surface-inventory.json'),
    ('f8b692e04941462ea592d9653960191cafb937cc', 'docs/reviews/rc2a-02c-q2-evidence/windows/elevated/public-surface-inventory.json'),
    ('f8b692e04941462ea592d9653960191cafb937cc', 'docs/reviews/rc2a-02c-q2-evidence/windows/rerun-20261026/public-surface-inventory.json')]
inventories = [data(*ref) for ref in inventory_refs]
normalized = [value.replace(b'\r\n', b'\n') for value in inventories]
parsed = [json.loads(value) for value in inventories]
assert inventories[0] != inventories[1] and inventories[1] == inventories[2]
assert normalized[0] == normalized[1] == normalized[2]
assert parsed[0] == parsed[1] == parsed[2]
assert len(parsed[0]['exportedTypes']) == 18
result['F04'] = dict(inventories=[dict(binding(*ref), CRLF=value.count(b'\r\n'),
                                      normalizedBytes=len(norm), normalizedSHA256=sha(norm))
                                for ref, value, norm in zip(inventory_refs, inventories, normalized)],
                    qualificationRawDiffers=True, reviewRawEqual=True, normalizedEqual=True,
                    jsonEqual=True, exportedTypes=18, frozenAPI=binding(TARGET, API))

result['payloadHashes'] = []
manifest = data(TARGET, PKG + 'SHA256SUMS').decode().splitlines()
assert len(manifest) == 4
for line in manifest:
    expected_hash, path = line.split(maxsplit=1)
    value = data(TARGET, path)
    checkout = (PRODUCER / path).read_bytes()
    assert sha(value) == expected_hash and checkout == value
    result['payloadHashes'].append(dict(path=path, expected=expected_hash, gitSHA256=sha(value),
                                       producerCheckoutSHA256=sha(checkout), bytes=len(value), match=True))

result['explicitReferences'] = []
result['markdownLinks'] = []
seen = set()


def slug(heading):
    return re.sub(r'[^\w -]', '', heading.lower()).replace(' ', '-')


for path in changed:
    if not path.endswith('.md'):
        continue
    content = data(TARGET, path).decode()
    if not path.startswith(PKG):
        content = '\n'.join(line[1:] for line in text('diff', '--unified=0', PRIOR, TARGET, '--', path).splitlines()
                            if line.startswith('+') and not line.startswith('+++'))
    for commit, target_path in re.findall(r'([0-9a-f]{40}):((?:docs|src|tests|native)/[^\s`]+)', content):
        target_path = target_path.rstrip('.,;)')
        if (commit, target_path) not in seen:
            seen.add((commit, target_path))
            result['explicitReferences'].append(binding(commit, target_path))
    for link in re.findall(r'\]\(([^)]+)\)', content):
        url = urllib.parse.urlsplit(link)
        if url.scheme:
            assert url.netloc == 'github.com' and url.path.startswith('/jenolaszlo-sketch/gagamba/blob/')
            commit, target_path = url.path.split('/blob/', 1)[1].split('/', 1)
        else:
            commit = TARGET
            target_path = posixpath.normpath(posixpath.join(posixpath.dirname(path), url.path))
        target_content = data(commit, target_path).decode()
        if url.fragment:
            assert url.fragment in [slug(heading) for heading in re.findall(r'^#+ (.+)$', target_content, re.MULTILINE)]
        result['markdownLinks'].append(dict(source=path, link=link, commit=commit, path=target_path,
                                           anchor=url.fragment, resolved=True))

result['producerAfter'] = producer_state()
assert result['producerAfter'] == result['producerBefore']
result['result'] = 'PASS - focused mechanical checks; narrative finding dispositions in Q2 report'
(OUT / 'verification.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8', newline='\n')
print(json.dumps(dict(result=result['result'], changedPaths=len(changed),
                     rowAudit=result['rowAudit'], payloadHashes=len(result['payloadHashes']),
                     explicitReferences=len(result['explicitReferences']), markdownLinks=len(result['markdownLinks']),
                     rawInventories=[(row['bytes'], row['CRLF'], row['sha256']) for row in result['F04']['inventories']],
                     producerUnchanged=result['producerBefore'] == result['producerAfter']), indent=2))
