#!/usr/bin/env python3
"""Verify the producer's sha256.txt evidence index against extracted files."""
import hashlib, os, sys

ROOT = r"C:\Users\Laszlos\source\repos\gagamba-b6-s2-q1-review\evidence\verification"
INDEX = os.path.join(ROOT, "sha256.txt")

def sha256_file(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for c in iter(lambda: f.read(1 << 20), b""):
            h.update(c)
    return h.hexdigest()

listed = {}
for line in open(INDEX, encoding="utf-8"):
    line = line.rstrip("\n")
    if not line.strip():
        continue
    digest, rel = line.split("  ", 1)
    listed[rel] = digest

print("indexed files:", len(listed))
missing, mismatch, extra = [], [], []
on_disk = set()
for dirpath, dirnames, filenames in os.walk(ROOT):
    for fn in filenames:
        full = os.path.join(dirpath, fn)
        rel = os.path.relpath(full, ROOT).replace(os.sep, "/")
        if rel == "sha256.txt":
            continue
        on_disk.add(rel)
        if rel not in listed:
            extra.append(rel)
            continue
        actual = sha256_file(full)
        if actual != listed[rel]:
            mismatch.append((rel, listed[rel], actual))
for rel in listed:
    if rel not in on_disk:
        missing.append(rel)

print("missing (listed but absent):", len(missing))
print("mismatched:", len(mismatch))
print("unlisted on disk:", len(extra))
for m in mismatch[:10]:
    print("  MISMATCH", m)
for m in missing[:10]:
    print("  MISSING", m)
for m in extra[:10]:
    print("  EXTRA", m)
print("self-excluded:", "sha256.txt" not in listed)
print("RESULT:", "OK" if not (missing or mismatch) else "FAIL")
