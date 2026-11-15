import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

repo, run = (Path(argument).resolve() for argument in sys.argv[1:3])
qualified = '34eef4c8e7d814e15cb1bfce27df032e75766f19'
start = 'e8099e37c1243e81f173bf0248307cf7d2fe3875'
imported = '7713ec58cc7c292862c8c80d6b57dcacd3420b44'
scope = [
    'src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationPreCodec.cs',
    'src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationPreCodecData.cs',
    'tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationPreCodecRemediationTests.cs',
]
env = {**os.environ, 'GIT_OPTIONAL_LOCKS': '0'}

def git(*args):
    return subprocess.check_output(['git', '-C', str(repo), *args], env=env)

def sha(data):
    return hashlib.sha256(data).hexdigest()

assert git('rev-parse', 'HEAD').decode().strip() == imported
assert git('symbolic-ref', 'HEAD').decode().strip() == 'refs/heads/main'
git('add', '--', *scope)
assert sorted(git('diff', '--cached', '--name-only').decode().splitlines()) == sorted(scope)
git('diff', '--cached', '--check')
tree = git('write-tree').decode().strip()
assert sorted(git('diff', '--name-only', imported, tree).decode().splitlines()) == sorted(scope)
def roster(rev, pattern, paths):
    args = ['grep', '-n', '-E', pattern]
    if rev:
        args.append(rev)
    else:
        args.insert(1, '--cached')
    args += ['--', *paths]
    process = subprocess.run(['git', '-C', str(repo), *args], env=env, capture_output=True, check=False)
    assert process.returncode in (0, 1), process.stderr.decode()
    return process.stdout.decode() if not rev else process.stdout.decode().replace(rev+':','')

pattern = (r'StartOwnerPreparation\(|TrySpawnOwner\(|new PreparationOwnershipCoordinator\(|'
           r'IntegratedRecoveryHost.Run\(|QualifiedRecoveryConnection.Start\(|RecoveryConnection.Start\(|'
           r'new HeldReleasePhase\(|new B3PreparationOwner\(|OwnerPreparationTransaction.Run\(')
original = roster(qualified, pattern, ['src'])
assert original == roster(None, pattern, ['src'])
s28 = roster(None, r'new PreparationOwnershipCoordinator\(', ['tests'])
assert 'PreparationOwnershipTests.cs:61' in s28 and s28.count('\n') == 1
codec = roster(None, r'\bValidationPreCodec(Binary)?\b', ['src', 'eng', 'native'])
assert len(codec.splitlines()) == 2 and all(line.split(':',1)[0] in (
    scope[0], 'src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationPreCodecBinary.cs') for line in codec.splitlines())
(run/'logs'/'p4-source.txt').write_text(
    f'Original and candidate caller lists are identical.\n{original}\nS28 test caller\n{s28}\nCodec production mentions\n{codec}',
    encoding='utf-8')
record = {'qualifiedCommit': qualified, 'canonicalStart': start, 'importCommit': imported,
    'implementationTreeBeforeEvidence': tree,
    'changedPaths': {path: {'gitBlob': git('rev-parse', ':'+path).decode().strip(),
                           'gitSha256': sha(git('cat-file', 'blob', ':'+path)),
                           'unfilteredWorkingCopySha256': sha((repo/path).read_bytes())} for path in scope},
    'protectedSourceChanges': [],
    'normativeRecordBlob': git('rev-parse',
       tree+':docs/evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md').decode().strip()}
assert record['normativeRecordBlob'] == 'f6a71562845d2cfe0aee5495864a509c5caabe7a'
(run/'logs'/'source-identities.json').write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8')
print(record)
