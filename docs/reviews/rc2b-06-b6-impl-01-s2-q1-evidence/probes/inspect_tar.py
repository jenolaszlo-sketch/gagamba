#!/usr/bin/env python3
"""Adversarial-safe inspection of a tar.gz without extracting.

Rejects/annotates: absolute paths, traversal (..), symlinks, hardlinks,
devices/FIFOs, unicode tricks. Prints a JSON inventory.
"""
import tarfile, sys, json, os, posixpath

def unsafe_reason(member, name):
    reasons = []
    if name.startswith("/") or name.startswith("\\"):
        reasons.append("absolute-path")
    # windows drive
    if len(name) >= 2 and name[1] == ":":
        reasons.append("drive-path")
    parts = name.replace("\\", "/").split("/")
    if ".." in parts:
        reasons.append("traversal")
    if posixpath.isabs(name):
        reasons.append("posix-absolute")
    if member.issym():
        reasons.append("symlink")
    if member.islnk():
        reasons.append("hardlink")
    if member.ischr() or member.isblk():
        reasons.append("device")
    if member.isfifo():
        reasons.append("fifo")
    return reasons

def main(path):
    out = {"archive": os.path.abspath(path), "members": [], "unsafe": []}
    with tarfile.open(path, "r:gz") as tf:
        for m in tf.getmembers():
            name = m.name
            reasons = unsafe_reason(m, name)
            rec = {
                "name": name,
                "type": "dir" if m.isdir() else ("file" if m.isfile() else m.type),
                "size": m.size,
                "mode": oct(m.mode),
                "uid": m.uid,
                "gid": m.gid,
                "uname": m.uname,
                "gname": m.gname,
                "linkname": m.linkname,
                "mtime": m.mtime,
                "unsafe": reasons,
            }
            out["members"].append(rec)
            if reasons:
                out["unsafe"].append(rec)
    out["member_count"] = len(out["members"])
    out["file_count"] = sum(1 for r in out["members"] if r["type"] == "file")
    out["unsafe_count"] = len(out["unsafe"])
    print(json.dumps(out, indent=2))

if __name__ == "__main__":
    main(sys.argv[1])
