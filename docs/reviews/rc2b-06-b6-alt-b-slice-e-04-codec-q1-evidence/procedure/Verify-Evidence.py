import hashlib
from pathlib import Path
import subprocess
import sys

evidence = Path(__file__).resolve().parents[1]
repo = evidence.parents[2]
revision = sys.argv[1] if len(sys.argv) > 1 else 'HEAD'


def git(*args):
    return subprocess.check_output(['git', '--no-optional-locks', *args], cwd=repo)


def rel(path):
    return path.resolve().relative_to(repo).as_posix()


manifest = evidence / 'SHA256SUMS.txt'
raw = git('cat-file', 'blob', f'{revision}:{rel(manifest)}')
report = evidence.with_name(evidence.name.removesuffix('-evidence') + '.md')
expected = {rel(report)} | {p for p in git('ls-tree', '-r', '--name-only', revision, '--', rel(evidence)).decode().splitlines()
                            if p != rel(manifest)}
verified = set()
for row in raw.decode().splitlines():
    digest, name = row.split('  ', 1)
    path = rel(evidence / name)
    assert path in expected and path not in verified, path
    content = git('cat-file', 'blob', f'{revision}:{path}')
    assert hashlib.sha256(content).hexdigest() == digest, path
    verified.add(path)
assert verified == expected
print(f'{len(verified)} manifest entries verified against {revision}')
