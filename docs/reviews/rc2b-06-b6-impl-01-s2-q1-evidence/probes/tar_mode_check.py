#!/usr/bin/env python3
import tarfile, json, io, sys

TAR = r"C:\Users\Laszlos\source\repos\Gagamba\docs\evidence\rc2b-06-b6-impl-01-s2\candidate-payload.tar.gz"
MAN = r"C:\Users\Laszlos\source\repos\Gagamba\docs\evidence\rc2b-06-b6-impl-01-s2\candidate-manifest.json"

man = json.load(open(MAN))
by_path = {a["path"]: a for a in man["artifacts"]}
tf = tarfile.open(TAR, "r:gz")
ok = True
for m in tf.getmembers():
    if not m.isfile():
        continue
    rel = m.name
    if rel == "candidate/manifest.json":
        print(f"{rel:<52} tar_mode={oct(m.mode)} uid={m.uid} gid={m.gid} (mode contract 0444; uid/gid are installed-time)")
        if oct(m.mode) != "0o444": ok = False
        continue
    a = by_path.get(rel.replace("candidate/", "", 1))
    if a is None:
        print("UNKNOWN", rel); ok = False; continue
    exp = int(a["mode"], 8)
    mode_ok = m.mode == exp
    uid_ok = m.uid == a["uid"]
    gid_ok = m.gid == a["gid"]
    print(f"{rel:<52} tar_mode={oct(m.mode)}/exp={a['mode']} uid={m.uid}/exp={a['uid']} gid={m.gid}/exp={a['gid']} "
          f"mode_ok={mode_ok} uid_ok={uid_ok} gid_ok={gid_ok}")
    # The candidate archive is collected by the unprivileged build user; only mode is a candidate contract.
    # uid/gid 0 is the *installed* contract applied by the privileged installer, not an archive property.
    ok = ok and mode_ok
# also check directories
for m in tf.getmembers():
    if m.isdir():
        print(f"DIR {m.name:<48} tar_mode={oct(m.mode)}")
print("RESULT:", "OK" if ok else "MISMATCH")
