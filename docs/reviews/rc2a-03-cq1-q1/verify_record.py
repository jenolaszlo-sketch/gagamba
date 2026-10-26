"""Read-only Git/evidence audit. No builds, restore, tests, or native probes.

Run from the review worktree; output is independent review evidence only.
"""
import collections
import hashlib
import json
import pathlib
import posixpath
import re
import subprocess
import xml.etree.ElementTree as ET

TARGET = 'c946df0c93dde414c744f46cce054e7cd3f2c26e'
BASE = '9519e7cb612af6ff69c93c0d128553138533d9fd'
ROOT = pathlib.Path(__file__).resolve().parents[3]
OUT = pathlib.Path(__file__).resolve().parent
PKG = 'docs/evidence/restricted-consumer-contract-rc2a-03-cq1'

def git(*args):
    return subprocess.check_output(['git', '-C', str(ROOT), *args])

def txt(*args):
    return git(*args).decode('utf-8').strip()

def blob(commit, path):
    return git('show', f'{commit}:{path}')

def exists(commit, path):
    return subprocess.run(['git', '-C', str(ROOT), 'cat-file', '-e', f'{commit}:{path}'], capture_output=True).returncode == 0

def sha(data):
    return hashlib.sha256(data).hexdigest()

binding = json.loads(blob(TARGET, PKG + '/source-binding.json'))
expected = ['b8c167e3d7340a562221591bdc2b6df7adb372d1', '85631d2f04512376621d3d09f51d67fd90f45335', '2d6b9d12cab02f092c6b6e0b690138cf6726f12b']
paths = ['src', 'native', 'docs/design/restricted-consumer-contract-rc2a-01-api.md']
result = {'target': txt('show', '-s', '--format=%H %T %P', TARGET), 'baseline': txt('show', '-s', '--format=%H %T %P', BASE), 'origin': txt('remote', 'get-url', 'origin')}
result['checkpoints'] = []
for item in binding['noDriftVerification']['checkpoints'] + [{'commit': TARGET, 'role': 'review target'}]:
    commit = item['commit']
    objects = [{'path': p, 'oid': txt('rev-parse', f'{commit}:{p}'), 'type': txt('cat-file', '-t', f'{commit}:{p}')} for p in paths]
    result['checkpoints'].append({'role': item['role'], 'identity': txt('show', '-s', '--format=%H %T %P', commit), 'commitType': txt('cat-file', '-t', commit), 'objects': objects, 'match': [o['oid'] for o in objects] == expected})
result['chain'] = [{'role': x['role'], 'identity': txt('show', '-s', '--format=%H %T %P', x['commit']), 'type': txt('cat-file', '-t', x['commit'])} for x in binding['inheritedEvidenceChain']]
result['diff'] = txt('diff', '--name-status', BASE, TARGET)
result['rawDiff'] = txt('diff', '--raw', '--no-abbrev', BASE, TARGET)
check = subprocess.run(['git', '-C', str(ROOT), 'diff', '--check', BASE, TARGET], capture_output=True)
result['diffCheck'] = {'exit': check.returncode, 'output': check.stdout.decode()}
result['unchangedSurfaces'] = {p: txt('diff', '--name-only', BASE, TARGET, '--', p) == '' for p in ['src', 'native', 'tests', 'docs/design', 'docs/decisions', 'docs/reviews']}
packages = [('CQ1', TARGET, PKG), ('02A', 'c1a01761', 'docs/evidence/restricted-consumer-contract-rc2a-02a-windows'), ('02B-R1', '1cd22b9', 'docs/evidence/restricted-consumer-contract-rc2a-02b-portability'), ('02C', '62b75f58', 'docs/evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04')]
result['manifests'] = {}
for label, commit, directory in packages:
    rows = []
    for line in blob(commit, directory + '/SHA256SUMS').decode().splitlines():
        if not line.strip():
            continue
        digest, path = line.split(maxsplit=1)
        path = path.lstrip('*')
        path = path if path.startswith('docs/') else directory + '/' + path
        data = blob(commit, path)
        rows.append({'path': path, 'expected': digest, 'gitSHA256': sha(data), 'match': sha(data) == digest, 'checkoutSHA256': sha((ROOT / path).read_bytes()), 'bytes': len(data)})
    result['manifests'][label] = {'commit': txt('rev-parse', commit), 'count': len(rows), 'matched': sum(r['match'] for r in rows), 'rows': rows}
    doc_path = directory + '/document-sha256.json'
    if exists(commit, doc_path):
        doc = json.loads(blob(commit, doc_path))
        doc_rows = []
        for row in doc['files']:
            data = blob(commit, row['path']).replace(b'\r\n', b'\n')
            doc_rows.append({'path': row['path'], 'expected': row['sha256'], 'actual': sha(data), 'match': sha(data) == row['sha256'] and len(data) == row['bytes']})
        result['manifests'][label]['documentBindings'] = doc_rows

result['trx'] = []
multisets = {}
for label, commit, directory in packages[1:]:
    files = txt('ls-tree', '-r', '--name-only', commit, '--', directory).splitlines()
    for path in files:
        if not path.endswith('.trx') or 'failed-attempts/' in path or '/iteration-' in path:
            continue
        tree = ET.fromstring(blob(commit, path))
        outcomes = [(r.attrib['testName'], r.attrib['outcome']) for r in tree.findall('.//{*}UnitTestResult')]
        multisets[path] = collections.Counter(outcomes)
        result['trx'].append({'package': label, 'path': path, 'count': len(outcomes), 'outcomes': dict(collections.Counter(o for _, o in outcomes)), 'counters': tree.find('.//{*}Counters').attrib})
bdir = packages[2][2]
reference = multisets[bdir + '/runs/windows/windows-required-set.trx']
result['02BRequiredMultisetsEqual'] = all(multisets[bdir + '/runs/' + p] == reference for p in ['windows/windows-required-set-rerun.trx', 'wsl2/wsl2-required-set.trx', 'wsl2/wsl2-required-set-rerun.trx'])
result['02BPublicMultisetsEqual'] = multisets[bdir + '/runs/windows/windows-public.trx'] == multisets[bdir + '/runs/wsl2/wsl2-public.trx']
result['relativeLinks'] = []
for path in [PKG + '/' + name for name in ['README.md', 'traceability-matrix.md', 'reviewer-handoff.md']] + ['docs/handoff.md', 'docs/work-queue.md', 'docs/plans/restricted-consumer-contract-implementation.md', 'docs/plans/restricted-consumer-contract-rc2a-02-qualification.md']:
    # Historical links in tracking files are not newly introduced CQ1 evidence.
    content = blob(TARGET, path).decode()
    if not path.startswith(PKG):
        content = '\n'.join(x[1:] for x in txt('diff', '--unified=0', BASE, TARGET, '--', path).splitlines() if x.startswith('+') and not x.startswith('+++'))
    for link in re.findall(r'\]\(([^)]+)\)', content):
        if '://' in link or link.startswith('#'):
            continue
        target = posixpath.normpath(posixpath.join(posixpath.dirname(path), link.split('#')[0]))
        result['relativeLinks'].append({'source': path, 'link': link, 'resolved': target, 'exists': exists(TARGET, target)})
wrong = 'docs/assessments/rc2a-roadmap-01-restricted-execution-dependency-review.md'
correct = 'docs/assessments/rc2a-02-full-stage-acceptance-gap-assessment.md'
result['crossBranchAssessment'] = {'commit': txt('rev-parse', '058a150'), 'citedPath': wrong, 'citedPathExists': exists('058a150', wrong), 'actualPath': correct, 'actualBlob': txt('rev-parse', '058a150:' + correct), 'roadmapPathExistsAtRoadmapCommit': exists('d417367', wrong)}
result['baselineAncestorOfTarget'] = subprocess.run(['git', '-C', str(ROOT), 'merge-base', '--is-ancestor', BASE, TARGET]).returncode == 0
result['assessmentAncestorOfTarget'] = subprocess.run(['git', '-C', str(ROOT), 'merge-base', '--is-ancestor', '058a150', TARGET]).returncode == 0
result['contractDesignIdentities'] = {c: txt('rev-parse', c + ':docs/design/restricted-consumer-contract-rc1.md') for c in ['68b01fbb', '71a28afe', 'dd16baec', '1cd22b9', 'c4837b0', '62b75f58', 'a36cb684', '058a150', BASE, TARGET]}
adir = packages[1][2]
vectors = json.loads(blob('c1a01761', adir + '/exports/vector-manifest.json'))['vectors']
result['vectors'] = [{'id': v['id'], 'bytes': v['bytes'], 'embedded': bool(v.get('hex')), 'match': sha(bytes.fromhex(v['hex'])) == v['sha256'] and len(bytes.fromhex(v['hex'])) == v['bytes'] if v.get('hex') else None} for v in vectors]
matrix = json.loads(blob('c1a01761', adir + '/exports/mutation-coverage-matrix.json'))
case_ids = [case for f in matrix['fields'] for ids in f['applicable'].values() for case in ids]
result['fieldMatrix'] = {'actualFieldCount': len(matrix['fields']), 'uniqueFieldIds': len(set(f['fieldId'] for f in matrix['fields'])), 'caseCount': len(case_ids), 'uniqueCases': len(set(case_ids)), 'allFieldsHaveCases': all(f['applicable'] for f in matrix['fields'])}
source = json.loads(blob('c1a01761', adir + '/source-binding.json'))
result['02ASourceFiles'] = []
for row in source['fileInventory']:
    data = blob('71a28afe', row['path'])
    result['02ASourceFiles'].append({'path': row['path'], 'blob': txt('rev-parse', '71a28afe:' + row['path']), 'match': sha(data) == row['sha256'] and len(data) == row['bytes'] and txt('rev-parse', '71a28afe:' + row['path']) == row['gitBlob']})
cdir = packages[3][2]
surface_path = cdir + '/runs/windows/final/public-surface-inventory.json'
surface = blob('62b75f58', surface_path)
inventory = json.loads(surface)
result['surface'] = {'exportedTypeCount': len(inventory['exportedTypes']), 'viewCount': len([t for t in inventory['exportedTypes'] if t['fullName'].endswith('View')]), 'reference': next(t for t in inventory['exportedTypes'] if t['fullName'].endswith('RestrictedPreparationReference')), 'independentInventoriesEqual': {p: blob('f8b692e', p) == surface for p in ['docs/reviews/rc2a-02c-q2-evidence/windows/elevated/public-surface-inventory.json', 'docs/reviews/rc2a-02c-q2-evidence/windows/rerun-20261026/public-surface-inventory.json']}}
result['surface']['byteDomains'] = []
for commit, path in [('62b75f58', surface_path), ('f8b692e', 'docs/reviews/rc2a-02c-q2-evidence/windows/elevated/public-surface-inventory.json'), ('f8b692e', 'docs/reviews/rc2a-02c-q2-evidence/windows/rerun-20261026/public-surface-inventory.json')]:
    data = blob(commit, path)
    result['surface']['byteDomains'].append({'commit': commit, 'path': path, 'bytes': len(data), 'sha256': sha(data), 'CRLF': data.count(b'\r\n'), 'lfNormalizedEqual': data.replace(b'\r\n', b'\n') == surface.replace(b'\r\n', b'\n'), 'jsonEqual': json.loads(data) == inventory})
result['independentReviewTRX'] = []
for commit, directory in [('5bc396c2', 'docs/reviews/gagamba-rc2a-02b-q1-artifacts'), ('f8b692e', 'docs/reviews/rc2a-02c-q2-evidence')]:
    for path in txt('ls-tree', '-r', '--name-only', commit, '--', directory).splitlines():
        if path.endswith('.trx'):
            tree = ET.fromstring(blob(commit, path))
            rows = tree.findall('.//{*}UnitTestResult')
            result['independentReviewTRX'].append({'commit': commit, 'path': path, 'count': len(rows), 'outcomes': dict(collections.Counter(r.attrib['outcome'] for r in rows))})
result['Q3Provenance'] = {'citedRC1AContainsR2AF03': b'R2A-F03' in blob('9a705335', 'docs/decisions/restricted-consumer-rc1-owner-acceptance.md'), 'actualRC2A01DecisionBlob': txt('rev-parse', '49ebbfec:docs/decisions/restricted-consumer-rc2a-01-owner-acceptance.md'), 'actualDecisionContainsR2AF03': b'R2A-F03' in blob('49ebbfec', 'docs/decisions/restricted-consumer-rc2a-01-owner-acceptance.md')}
result['reviewWorktreeStatusAtAudit'] = txt('status', '--porcelain=v1', '--untracked-files=all')
(OUT / 'verification.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'checkpoints': len(result['checkpoints']), 'identitiesMatch': all(x['match'] for x in result['checkpoints']), 'chainCommits': len(result['chain']), 'manifests': {k: [v['matched'], v['count']] for k, v in result['manifests'].items()}, 'documentBindings': {k: [sum(r['match'] for r in v.get('documentBindings', [])), len(v.get('documentBindings', []))] for k, v in result['manifests'].items()}, 'trx': result['trx'], 'crossPlatformRequiredEqual': result['02BRequiredMultisetsEqual'], 'crossPlatformPublicEqual': result['02BPublicMultisetsEqual'], 'relativeLinks': [sum(r['exists'] for r in result['relativeLinks']), len(result['relativeLinks'])], 'assessment': result['crossBranchAssessment']}, indent=2))
