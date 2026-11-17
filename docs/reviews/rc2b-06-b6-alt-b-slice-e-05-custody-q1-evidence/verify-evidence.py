import hashlib
from pathlib import Path
import subprocess
import sys

REPORT = "rc2b-06-b6-alt-b-slice-e-05-custody-q1.md"
ANNEX = REPORT.removesuffix(".md") + "-evidence"
START = "0a77422c1349b99e518292d46314606b383527e4"
MANIFEST = "SHA256SUMS.txt"

def sha(data):
    return hashlib.sha256(data).hexdigest()

def members(annex):
    return {"../" + REPORT: annex.parent / REPORT} | {
        p.relative_to(annex).as_posix():p for p in sorted(annex.rglob("*"))
        if p.is_file() and p != annex / MANIFEST
    }

def manifest(annex):
    return "".join(f"{sha(p.read_bytes())}  {name}\n" for name,p in members(annex).items()).encode()

def verify_git(annex, mode):
    repo = annex.parents[2]
    assert annex.relative_to(repo).as_posix() == "docs/reviews/" + ANNEX

    def git(*args, data=None):
        return subprocess.run(["git","--no-optional-locks",*args],cwd=repo,input=data,capture_output=True,check=True).stdout

    items = members(annex) | {MANIFEST:annex/MANIFEST}
    paths = {p.relative_to(repo).as_posix():p for p in items.values()}
    expected = {"A\t"+p for p in paths}
    if mode == "stage":
        assert git("rev-parse","HEAD").decode().strip() == START
        prefix = ":"
        diff = git("diff","--cached","--name-status")
    else:
        assert git("rev-parse","HEAD^").decode().strip() == START
        prefix = "HEAD:"
        diff = git("diff","--name-status",START,"HEAD")
    assert set(diff.decode().splitlines()) == expected, "Unexpected file delta"
    requests = "".join(prefix+p+"\n" for p in paths).encode()
    values = git("cat-file","--batch",data=requests)
    offset = 0
    for name,p in paths.items():
        end = values.index(b"\n",offset)
        oid,kind,size = values[offset:end].decode().split()
        assert kind == "blob", name
        data = values[end+1:end+1+int(size)]
        assert data == p.read_bytes(), (name,oid,"Git/working bytes differ")
        offset = end+2+int(size)
    assert offset == len(values)

def main():
    annex = Path(sys.argv[1]).resolve()
    mode = sys.argv[2]
    expected = manifest(annex)
    path = annex / MANIFEST
    if mode == "write":
        path.write_bytes(expected)
    else:
        assert expected == path.read_bytes(), "SHA-256 manifest mismatch or unlisted member"
        if mode in {"stage","head"}:
            verify_git(annex,mode)
    print(f"Evidence {mode}: {len(expected.splitlines())} manifest members, SHA-256 {sha(expected)}")

if __name__ == "__main__":
    main()
