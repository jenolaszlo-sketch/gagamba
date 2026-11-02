#!/usr/bin/env python3
"""Generate SHA256SUMS.txt for the review package.

Exclusion rule (documented):
  * exclude the manifest itself (SHA256SUMS.txt);
  * exclude byte-identical preserved producer evidence under evidence/verification/
    (its identity is the verification.zip digest F4A09B1F... and its internal 148-entry index);
  * exclude the unprivileged candidate extraction under extract/
    (its identity is the candidate archive digest 0f4533b7...);
  * exclude the isolated source copy src-8b897e4/ and its extracted tar
    (its identity is the reviewed git tree af1dc67...);
  * exclude build outputs (bin/ and obj/ directories).
Everything else (reviewer documents, reviewer probe sources, reviewer evidence logs and TRX)
is listed.
"""
import hashlib, os

ROOT = r"C:\Users\Laszlos\source\repos\gagamba-b6-s2-q1-review"
EXCLUDE_DIRS_TOP = {"extract", "src-8b897e4"}
SELF = "SHA256SUMS.txt"

def sha256(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for c in iter(lambda: f.read(1 << 20), b""):
            h.update(c)
    return h.hexdigest()

rows = []
for dirpath, dirnames, filenames in os.walk(ROOT):
    rel = os.path.relpath(dirpath, ROOT).replace(os.sep, "/")
    top = rel.split("/")[0]
    if top in EXCLUDE_DIRS_TOP:
        dirnames[:] = []
        continue
    if rel == "evidence/verification" or rel.startswith("evidence/verification/"):
        dirnames[:] = []
        continue
    for d in list(dirnames):
        if d in ("bin", "obj"):
            dirnames.remove(d)
    for fn in filenames:
        full = os.path.join(dirpath, fn)
        r = os.path.relpath(full, ROOT).replace(os.sep, "/")
        if r == SELF:
            continue
        rows.append((r, sha256(full)))

rows.sort()
out = os.path.join(ROOT, SELF)
with open(out, "w", encoding="utf-8", newline="\n") as f:
    for r, h in rows:
        f.write(f"{h}  {r}\n")
print("wrote", out, "entries", len(rows))
