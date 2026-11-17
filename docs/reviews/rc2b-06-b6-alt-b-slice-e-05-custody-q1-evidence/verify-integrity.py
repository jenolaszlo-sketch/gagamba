import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath
from posixpath import normpath

ROOT = Path(r"C:/Users/Laszlos/source/repos/Gagamba")
START = "0a77422c1349b99e518292d46314606b383527e4"
CANDIDATE = "28895d55de98eea4bc402373b653f7216e092843"
BASE = "7a30d0a05a7c898c4e90cc2ca01280db166ed6f2"
REMOTE = "1116c66d7858e6000f0001a0abe39319e4e694be"
PREFIX = "docs/decisions/rc2b-06-b6-alt-b-slice-e-05"
PRODUCER = "docs/evidence/rc2b-06-b6-alt-b-slice-e-05-custody-implementation-01"
MANIFEST = "-evidence/SHA256SUMS.txt"

def sha(data):
    return hashlib.sha256(data).hexdigest()

def git(*args, data=None):
    return subprocess.run(["git", "--no-optional-locks", "-C", str(ROOT), *args],
                          input=data, capture_output=True, check=True).stdout

def git_text(*args):
    return git(*args).decode().strip()

def blobs(revision, paths):
    requests = "".join(f"{revision}:{path}\n" for path in paths).encode()
    raw = git("cat-file", "--batch", data=requests)
    index = 0
    for path in paths:
        end = raw.index(b"\n", index)
        oid, kind, size = raw[index:end].decode().split()
        assert kind == "blob", (path, kind)
        index = end + 1
        data = raw[index:index + int(size)]
        index += int(size) + 1
        yield path, oid, data
    assert index == len(raw)

def get(revision, path):
    return next(blobs(revision, [path]))[2]

def disk(path):
    return (ROOT / path).read_bytes()

def verify_manifest(spec, head):
    path, revision = spec["path"], spec["revision"]
    data = get(revision, path)
    assert data == get(head, path) == disk(path), path
    assert sha(data) == spec.get("sha256", sha(data)), path
    rows = {}
    for line in data.decode("utf-8-sig").splitlines():
        match = re.fullmatch(r"([a-fA-F0-9]{64}) [ *](.+)", line)
        assert match, (path, line)
        digest, rel = match.groups()
        member = normpath(str(PurePosixPath(path).parent / rel))
        assert member != ".." and not member.startswith(("../", "/")) and member not in rows
        rows[member] = digest.lower()
    assert len(rows) == spec["count"], path
    original = dict((p, (oid, sha(b))) for p, oid, b in blobs(revision, rows))
    current = dict((p, (oid, sha(b))) for p, oid, b in blobs(head, rows))
    for p, digest in rows.items():
        assert original[p] == current[p] and digest == original[p][1] == sha(disk(p)), (path, p)
    git("merge-base", "--is-ancestor", revision, head)
    return dict(path=path, revision=revision, sha256=sha(data), count=len(rows),
                members=[dict(path=p, blob=original[p][0], sha256=d) for p, d in rows.items()])

def main(output, expected_head):
    branch = git_text("symbolic-ref", "HEAD")
    head = git_text("rev-parse", "HEAD")
    assert branch == "refs/heads/main" and head == expected_head
    assert git_text("rev-parse", "--show-toplevel").casefold() == str(ROOT).replace("\\", "/").casefold()
    worktrees = git_text("worktree", "list", "--porcelain")
    stashes = git_text("stash", "list")
    assert worktrees.count("worktree ") == 1 and not stashes
    status = git("status", "--porcelain=v1", "-z", "--untracked-files=all").decode().split("\0")[:-1]
    assert all(row.startswith("?? ") for row in status)
    origin = git_text("remote", "get-url", "origin")
    push_origin = git_text("remote", "get-url", "--push", "origin")
    assert origin == push_origin == "https://github.com/jenolaszlo-sketch/gagamba.git"
    remote_ref = git_text("ls-remote", "--exit-code", "origin", "refs/heads/main")
    assert remote_ref == REMOTE + "\trefs/heads/main", remote_ref
    assert git_text("rev-parse", "refs/remotes/origin/main") == REMOTE
    pinned = {START:"02353c07b031199c01d8750119a8514521052b86",
              CANDIDATE:"e314f2e8a1259e2263c8494bb0c8ca9f7eb27016",
              REMOTE:"86768fbd5da028fc271fda295a7da5c389aab8bc"}
    for revision, tree in pinned.items():
        assert git_text("rev-parse", f"{revision}^{{tree}}") == tree
    history = [START,CANDIDATE,BASE,"02ba7ad3c81e72de885349c2bd54b0b7ab16bc12",
               "1ff4d6e6d1f756646072790478921d9a64f15654",
               "b309a9ec1a9a0f160ea0c2dd513001c3561a36d9",REMOTE]
    for child, parent in zip(history, history[1:]):
        assert git_text("show", "-s", "--format=%P", child) == parent
    inventory = "docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json"
    original_inventory = get(START, inventory)
    assert original_inventory == disk(inventory) == get(head, inventory)
    untracked = json.loads(original_inventory)
    assert len(untracked) == 44 and {x["path"] for x in untracked} == {s[3:] for s in status}
    for entry in untracked:
        data = disk(entry["path"])
        assert len(data) == entry["length"] and sha(data) == entry["sha256"], entry["path"]
    design_preflight = json.loads(get(history[4], PREFIX + "-design-owner-01-evidence/preflight.json"))
    assert design_preflight["historicalUntracked"] == untracked
    specs = design_preflight["manifests"] + [
        dict(path=PREFIX+"-design-owner-01"+MANIFEST, revision=history[4], count=12, sha256="e48f0097802d130d6b96038795aa5cc2f91e1e88c6f19e4061f2fc279275f8a1"),
        dict(path=PREFIX+"-entry-owner-02"+MANIFEST, revision=history[3], count=5, sha256="a0109ec7c34f7df8a6a528fe75314f94a49226d7a3011c5a009830180dfc6bdc"),
        dict(path=PREFIX+"-entry-auth-01"+MANIFEST, revision=BASE, count=5),
    ]
    historical = [verify_manifest(spec, head) for spec in specs]
    assert sum(x["count"] for x in historical) == 814
    producer = verify_manifest(dict(path=PRODUCER+MANIFEST, revision=START, count=41), head)
    scope = git_text("diff", "--name-status", BASE, CANDIDATE).splitlines()
    allowed = {
        "src/Gagamba.Execution.Linux.M1/Bootstrap/" + name + ".cs"
        for name in ["ValidationCustodyModel", "ValidationCustodyModelData"]
    } | {
        "tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModel" + suffix + ".cs"
        for suffix in ["Tests", "LifecycleTests", "CompositionTests", "Vectors"]
    }
    assert len(scope) == 6 and set(scope) == {"A\t" + p for p in allowed}
    after = git_text("diff", "--name-status", CANDIDATE, START).splitlines()
    assert set(after) == {"A\t" + p for p in [PRODUCER+MANIFEST] + [m["path"] for m in producer["members"]]}
    candidate_files = []
    for p, oid, data in blobs(CANDIDATE, sorted(allowed)):
        assert data == get(head,p) == disk(p), p
        candidate_files.append(dict(path=p, blob=oid, sha256=sha(data), bytes=len(data)))
    frozen = []
    for name, oid, digest in [
        ("contract.md", "121443efb9e145e2413c5a2c40f59e45a55eaa55", "8d782d55544a1fcc65a7adbe035e21ce7d7cd10d31658274b1620527b8b02a54"),
        ("requirements.md", "63ebfb7afed2dfb2883fae237ed64472c2adad66", "ca9c64bbf85238859fbce8d07e260ab0cbd7940738d7a928d0f2da32ab3587f0"),
    ]:
        path = PREFIX + "-design-owner-01-evidence/" + name
        p, blob, data = next(blobs(CANDIDATE, [path]))
        assert blob == oid and sha(data) == digest and data == get(head,p) == disk(p)
        frozen.append(dict(path=p, blob=blob, sha256=digest))
    assert git_text("rev-parse", "HEAD") == head
    receipt = dict(branch=branch, head=head, tree=git_text("rev-parse","HEAD^{tree}"),
                   worktrees=worktrees, stashes=[], origin=origin, live_remote_ref=remote_ref,
                   pins=pinned, direct_ancestry=history, tracked_status="clean",
                   historical_manifests=historical, producer_manifest=producer,
                   historical_untracked=untracked, artifact_bindings=frozen,
                   candidate_files=candidate_files, evidence_delta=after)
    output.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(f"PASS checkout, live remote, six additions, 814 historical + 41 producer entries and 44 untracked files at {head}")

if __name__ == "__main__":
    main(Path(sys.argv[1]), sys.argv[2] if len(sys.argv) > 2 else START)
