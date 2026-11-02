#!/usr/bin/env python3
"""Independent verification of the B6-S2 candidate artifact set.

- Recomputes SHA-256 of every artifact in the extracted tree.
- Compares to the canonical manifest.
- Recomputes artifact-set identity from manifest bytes.
- Checks role/kind/path/length/rid/buildIdentity.
- Reports duplicate paths / duplicate roles / ordering.
"""
import json, hashlib, os, sys

MANIFEST = r"C:\Users\Laszlos\source\repos\Gagamba\docs\evidence\rc2b-06-b6-impl-01-s2\candidate-manifest.json"
EXTRACT = r"C:\Users\Laszlos\source\repos\gagamba-b6-s2-q1-review\extract\candidate"
EXPECTED_SET = "31d8ebc21c227e8ed6cf9e6952b69365a71f6ec02786c0cf0fa8a0ab8906e133"

def sha256_file(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()

def main():
    raw = open(MANIFEST, "rb").read()
    manifest_set_sha = hashlib.sha256(raw).hexdigest()
    m = json.loads(raw.decode("utf-8"))
    arts = m["artifacts"]
    print("PROFILE:", m["profileId"], "SCHEMA:", m["schemaVersion"])
    print("SOURCE.commit:", m["source"]["commit"])
    print("SOURCE.tree  :", m["source"]["tree"])
    print()
    # duplicate path / role checks
    paths = [a["path"] for a in arts]
    roles = [a["role"] for a in arts]
    print("duplicate paths:", len(paths) != len(set(paths)))
    print("duplicate roles:", len(roles) != len(set(roles)))
    ordered = paths == sorted(paths)
    print("paths sorted ascending:", ordered)
    print("artifact count:", len(arts))
    print()
    all_match = True
    for a in arts:
        rel = a["path"]
        full = os.path.join(EXTRACT, rel.replace("/", os.sep))
        exists = os.path.exists(full)
        actual_len = os.path.getsize(full) if exists else None
        actual_sha = sha256_file(full) if exists else None
        ok = exists and actual_sha == a["sha256"] and actual_len == a["length"]
        all_match = all_match and ok
        print(f"{'OK ' if ok else 'BAD'} {rel}")
        print(f"     role={a['role']} kind={a['kind']} rid={a['rid']} build={a['buildIdentity']} consumer={a['consumer']}")
        print(f"     exp_sha={a['sha256']}")
        print(f"     act_sha={actual_sha}")
        print(f"     exp_len={a['length']} act_len={actual_len} mode={a['mode']} uid={a['uid']} gid={a['gid']} nlink={a['nlink']}")
    print()
    print("ALL ARTIFACTS MATCH:", all_match)
    print("artifact-set sha256 (manifest bytes):", manifest_set_sha)
    print("expected artifact-set sha256        :", EXPECTED_SET)
    print("ARTIFACT-SET MATCH:", manifest_set_sha == EXPECTED_SET)
    # verify manifest bytes are canonical: no BOM, no trailing newline?
    print("manifest starts with BOM:", raw[:3] == b"\xef\xbb\xbf")
    print("manifest final byte:", raw[-1:], "len:", len(raw))
    print("manifest is ascii:", all(b < 128 for b in raw))

if __name__ == "__main__":
    main()
