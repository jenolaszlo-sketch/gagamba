#!/usr/bin/python3
"""Fail-closed Linux M1 install transactions. The invoking script is trusted code."""
from __future__ import annotations

import argparse
import ctypes
import datetime as dt
import fcntl
import hashlib
import json
import os
import re
import secrets
import stat
import subprocess
import sys
from typing import Any

ROOT = "/opt/gagamba"
INSTALLATIONS = "installations"
QUALIFICATIONS = "qualifications"
TRUSTED_TOOL = "/usr/lib/dotnet/dotnet"
TRUSTED_DLL = "/usr/local/libexec/gagamba-install/InstallTool.dll"
DECISIONS = "/etc/gagamba/accepted-qualifications"
MAX_META = 256 * 1024
MAX_COPY = 32 * 1024 * 1024
HEX64 = re.compile(r"[0-9a-f]{64}\Z")
ID = re.compile(r"[A-Za-z0-9._-]{1,128}\Z")


class Refuse(Exception):
    pass


def require(ok: bool, message: str) -> None:
    if not ok:
        raise Refuse(message)


def _pairs(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        require(key not in result, "duplicate JSON key")
        result[key] = value
    return result


def parse_json(raw: bytes) -> dict[str, Any]:
    require(0 < len(raw) <= MAX_META, "metadata size")
    try:
        value = json.loads(raw.decode("utf-8"), object_pairs_hook=_pairs)
    except (UnicodeError, json.JSONDecodeError) as exc:
        raise Refuse("invalid JSON") from exc
    require(type(value) is dict, "metadata must be an object")
    return value


def strict_keys(value: dict[str, Any], keys: set[str]) -> None:
    require(set(value) == keys, "metadata field set mismatch")


def _open_dir_path(path: str) -> int:
    require(path.startswith("/"), "absolute path required")
    fd = os.open("/", os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC)
    try:
        for part in path.split("/")[1:]:
            if not part:
                continue
            require(part not in (".", ".."), "path traversal")
            child = os.open(part, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=fd)
            os.close(fd)
            fd = child
        return fd
    except BaseException:
        os.close(fd)
        raise


def _check_trusted_dir(fd: int, path: str) -> None:
    st = os.fstat(fd)
    require(stat.S_ISDIR(st.st_mode) and st.st_uid == 0 and st.st_gid == 0 and
            (st.st_mode & 0o022) == 0, "untrusted directory ancestor: " + path)


def _open_trusted_dir(path: str) -> int:
    require(path.startswith("/"), "trusted absolute path required")
    fd = os.open("/", os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW)
    _check_trusted_dir(fd, "/")
    current = ""
    try:
        for part in path.split("/")[1:]:
            if not part:
                continue
            require(part not in (".", ".."), "trusted path traversal")
            child = os.open(part, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=fd)
            current += "/" + part
            _check_trusted_dir(child, current)
            os.close(fd)
            fd = child
        return fd
    except BaseException:
        os.close(fd)
        raise


def _ensure_trusted_root() -> int:
    optfd = _open_trusted_dir("/opt")
    try:
        try:
            os.mkdir("gagamba", 0o755, dir_fd=optfd)
            fd = os.open("gagamba", os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=optfd)
            os.fchmod(fd, 0o755)
            os.fsync(fd)
            os.fsync(optfd)
        except FileExistsError:
            fd = os.open("gagamba", os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=optfd)
        st = os.fstat(fd)
        require(st.st_uid == 0 and st.st_gid == 0 and stat.S_IMODE(st.st_mode) == 0o755,
                "install root must be root:root 0755")
        return fd
    finally:
        os.close(optfd)


def _secure_root() -> int:
    fd = _open_trusted_dir(ROOT)
    require(stat.S_IMODE(os.fstat(fd).st_mode) == 0o755, "install root mode")
    return fd


def _trusted_file(path: str, *, owner: int | None = 0, limit: int = MAX_META, exact_mode: int | None = None) -> bytes:
    parent, name = os.path.split(path)
    dfd = _open_trusted_dir(parent) if owner == 0 else _open_dir_path(parent)
    try:
        pin = os.open(name, os.O_PATH | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=dfd)
        try:
            before = os.fstat(pin)
            require(stat.S_ISREG(before.st_mode) and (owner is None or before.st_uid == owner) and
                    (exact_mode is None or stat.S_IMODE(before.st_mode) == exact_mode) and
                    (owner != 0 or before.st_gid == 0) and before.st_nlink == 1 and
                    (before.st_mode & 0o022) == 0 and before.st_size <= limit, "untrusted decision file")
            fd = os.open(f"/proc/self/fd/{pin}", os.O_RDONLY | os.O_CLOEXEC)
            try:
                opened = os.fstat(fd)
                require((opened.st_dev, opened.st_ino) == (before.st_dev, before.st_ino), "trusted file pin mismatch")
            except BaseException:
                os.close(fd)
                raise
            chunks, total = [], 0
            try:
                while True:
                    block = os.read(fd, min(65536, limit + 1 - total))
                    if not block:
                        break
                    chunks.append(block)
                    total += len(block)
                    require(total <= limit, "file size limit")
                after = os.fstat(fd)
                _same_file(before, after)
                return b"".join(chunks)
            finally:
                os.close(fd)
        finally:
            os.close(pin)
    finally:
        os.close(dfd)


def _same_file(a: os.stat_result, b: os.stat_result) -> None:
    require((a.st_dev, a.st_ino, a.st_nlink, a.st_size, a.st_mtime_ns, a.st_ctime_ns) ==
            (b.st_dev, b.st_ino, b.st_nlink, b.st_size, b.st_mtime_ns, b.st_ctime_ns), "file changed while reading")


def _manifest(rootfd: int) -> tuple[bytes, dict[str, Any], str, dict[str, dict[str, Any]]]:
    pin = os.open("manifest.json", os.O_PATH | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=rootfd)
    try:
        st = os.fstat(pin)
        require(stat.S_ISREG(st.st_mode) and st.st_nlink == 1 and st.st_size <= MAX_META, "manifest file type/size")
        fd = os.open(f"/proc/self/fd/{pin}", os.O_RDONLY | os.O_CLOEXEC)
        try:
            require((os.fstat(fd).st_dev, os.fstat(fd).st_ino) == (st.st_dev, st.st_ino), "manifest pin mismatch")
            raw = _read_fd(fd, MAX_META)
            _same_file(st, os.fstat(fd))
        finally:
            os.close(fd)
    finally:
        os.close(pin)
    obj = parse_json(raw)
    digest = hashlib.sha256(raw).hexdigest()
    artifacts = obj.get("artifacts")
    require(type(artifacts) is list and 1 <= len(artifacts) <= 17, "manifest artifact list")
    paths: dict[str, dict[str, Any]] = {"manifest.json": {"length": len(raw), "sha256": digest, "mode": "0444"}}
    for item in artifacts:
        require(type(item) is dict, "manifest artifact entry")
        path = item.get("path")
        parts = path.split("/") if type(path) is str else []
        require(len(parts) == 2 and all(re.fullmatch(r"[A-Za-z0-9._-]{1,128}", p) for p in parts) and
                parts[0] not in (".", "..") and parts[1] not in (".", ".."), "manifest artifact path")
        require(path not in paths, "duplicate artifact path")
        require(type(item.get("length")) is int and 0 <= item["length"] <= MAX_COPY, "artifact length")
        require(type(item.get("sha256")) is str and HEX64.fullmatch(item["sha256"]), "artifact digest")
        mode = item.get("mode")
        require(mode in ("0444", "0555"), "artifact mode")
        paths[path] = item
    require(sum(item["length"] for item in paths.values()) <= 128 * 1024 * 1024 + MAX_META, "aggregate limit")
    return raw, obj, digest, paths


def _read_fd(fd: int, limit: int) -> bytes:
    chunks, total = [], 0
    while True:
        block = os.read(fd, min(65536, limit + 1 - total))
        if not block:
            break
        chunks.append(block)
        total += len(block)
        require(total <= limit, "file size limit")
    return b"".join(chunks)


def _exact_names(fd: int, expected: set[str]) -> None:
    remaining = expected.copy()
    with os.scandir(fd) as entries:
        for entry in entries:
            require(entry.name in remaining, "candidate has an unexpected entry")
            remaining.remove(entry.name)
    require(not remaining, "candidate entries missing")


def _preflight_payload(rootfd: int, paths: dict[str, dict[str, Any]]) -> None:
    want_dirs = {p.split("/")[0] for p in paths if p != "manifest.json"}
    _exact_names(rootfd, want_dirs | {"manifest.json"})
    # First pass rejects all special entries and links before opening any artifact bytes.
    mstat = os.stat("manifest.json", dir_fd=rootfd, follow_symlinks=False)
    require(stat.S_ISREG(mstat.st_mode) and mstat.st_nlink == 1 and mstat.st_size == paths["manifest.json"]["length"],
            "manifest special file/link/size")
    for dirname in want_dirs:
        dfd = os.open(dirname, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=rootfd)
        try:
            expected = {p.split("/")[1] for p in paths if p.startswith(dirname + "/")}
            _exact_names(dfd, expected)
            for path in paths:
                if not path.startswith(dirname + "/"):
                    continue
                name = path.split("/")[1]
                st = os.stat(name, dir_fd=dfd, follow_symlinks=False)
                item = paths[path]
                require(stat.S_ISREG(st.st_mode) and st.st_nlink == 1 and st.st_size == item["length"],
                        "candidate special file/link/size")
        finally:
            os.close(dfd)
    # Contents are read only later while copying from one opened fd per file.


def _tool(*args: str) -> None:
    _check_trusted_regular(TRUSTED_TOOL)
    _check_verifier_tree(os.path.dirname(TRUSTED_DLL))
    env = {"PATH": "", "HOME": "/", "LANG": "C", "LC_ALL": "C", "DOTNET_NOLOGO": "1"}
    cp = subprocess.run([TRUSTED_TOOL, TRUSTED_DLL, *args], env=env, stdin=subprocess.DEVNULL,
                        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False, close_fds=True,
                        cwd="/", timeout=60)
    require(cp.returncode == 0, "trusted verifier refused: " + " ".join(args[:1]))


def _ensure_child_dir(parentfd: int, name: str, mode: int = 0o755) -> int:
    try:
        os.mkdir(name, mode, dir_fd=parentfd)
        created = True
    except FileExistsError:
        created = False
    fd = os.open(name, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=parentfd)
    st = os.fstat(fd)
    if created:
        os.fchmod(fd, mode)
        os.fsync(fd)
        os.fsync(parentfd)
        st = os.fstat(fd)
    require(st.st_uid == 0 and st.st_gid == 0 and stat.S_IMODE(st.st_mode) == mode,
            "trusted directory ownership/mode")
    return fd


def _check_trusted_regular(path: str) -> None:
    parent, name = os.path.split(path)
    dfd = _open_trusted_dir(parent)
    try:
        pin = os.open(name, os.O_PATH | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=dfd)
        try:
            st = os.fstat(pin)
            require(stat.S_ISREG(st.st_mode) and st.st_uid == 0 and st.st_gid == 0 and st.st_nlink == 1 and
                    (st.st_mode & 0o022) == 0, "untrusted verifier executable")
        finally:
            os.close(pin)
    finally:
        os.close(dfd)


def _check_verifier_tree(path: str) -> None:
    fd = _open_trusted_dir(path)
    try:
        seen: set[str] = set()

        def walk(dirfd: int, prefix: str) -> None:
            for name in os.listdir(dirfd):
                st = os.stat(name, dir_fd=dirfd, follow_symlinks=False)
                relative = f"{prefix}/{name}" if prefix else name
                require(not stat.S_ISLNK(st.st_mode) and st.st_uid == 0 and st.st_gid == 0 and
                        (st.st_mode & 0o022) == 0, "untrusted verifier tree entry")
                if stat.S_ISDIR(st.st_mode):
                    child = os.open(name, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=dirfd)
                    try:
                        walk(child, relative)
                    finally:
                        os.close(child)
                else:
                    require(stat.S_ISREG(st.st_mode) and st.st_nlink == 1, "special verifier tree entry")
                    seen.add(relative)

        walk(fd, "")
        require("InstallTool.dll" in seen and "InstallTool.deps.json" in seen and
                "InstallTool.runtimeconfig.json" in seen and
                {"Gagamba.Execution.Linux.M1.dll", "Gagamba.Execution.dll"} <= seen,
                "incomplete trusted verifier closure")
    finally:
        os.close(fd)


def _lock(rootfd: int) -> int:
    fd = os.open(".install.lock", os.O_RDWR | os.O_CREAT | os.O_CLOEXEC | os.O_NOFOLLOW, 0o600, dir_fd=rootfd)
    st = os.fstat(fd)
    require(stat.S_ISREG(st.st_mode) and st.st_uid == 0 and st.st_nlink == 1 and (st.st_mode & 0o077) == 0,
            "trusted lock file")
    fcntl.flock(fd, fcntl.LOCK_EX)
    return fd


def _atomic_noreplace(parentfd: int, old: str, new: str) -> None:
    libc = ctypes.CDLL(None, use_errno=True)
    fn = getattr(libc, "renameat2", None)
    require(fn is not None, "renameat2 unavailable")
    rc = fn(parentfd, ctypes.c_char_p(os.fsencode(old)), parentfd, ctypes.c_char_p(os.fsencode(new)), 1)
    if rc != 0:
        err = ctypes.get_errno()
        raise OSError(err, os.strerror(err), new)


def _atomic_write(dirfd: int, name: str, raw: bytes, mode: int = 0o444) -> None:
    temp = ".tmp-" + secrets.token_hex(16)
    fd = os.open(temp, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_CLOEXEC | os.O_NOFOLLOW, mode, dir_fd=dirfd)
    try:
        _write_all(fd, raw)
        os.fchmod(fd, mode)
        os.fsync(fd)
    finally:
        os.close(fd)
    os.rename(temp, name, src_dir_fd=dirfd, dst_dir_fd=dirfd)
    os.fsync(dirfd)


def _write_all(fd: int, raw: bytes) -> None:
    view = memoryview(raw)
    while view:
        count = os.write(fd, view)
        require(count > 0, "short write")
        view = view[count:]


def _sync_tree(dirfd: int) -> None:
    for name in os.listdir(dirfd):
        st = os.stat(name, dir_fd=dirfd, follow_symlinks=False)
        require(not stat.S_ISLNK(st.st_mode), "symlink in staged generation")
        if stat.S_ISDIR(st.st_mode):
            child = os.open(name, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=dirfd)
            try:
                _sync_tree(child)
            finally:
                os.close(child)
    os.fsync(dirfd)


def _publish_generation(instfd: int, rootfd: int, stage: str, generation: str) -> None:
    require(HEX64.fullmatch(generation) is not None, "generation digest grammar")
    sfd = os.open(stage, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=instfd)
    try:
        _sync_tree(sfd)
    finally:
        os.close(sfd)
    os.fsync(instfd)
    _atomic_noreplace(instfd, stage, generation)
    os.fsync(instfd)
    # Publication of active is the final visible transaction step.
    _atomic_write(rootfd, "active", (generation + "\n").encode("ascii"), 0o444)
    os.fsync(rootfd)


def _copy_artifact(src_root: int, dst_root: int, path: str, item: dict[str, Any]) -> None:
    parts = path.split("/")
    sdir = os.open(parts[0], os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=src_root)
    ddir = os.open(parts[0], os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=dst_root)
    try:
        pin = os.open(parts[1], os.O_PATH | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=sdir)
        try:
            before = os.fstat(pin)
            require(stat.S_ISREG(before.st_mode) and before.st_nlink == 1 and before.st_size == item["length"], "source changed before copy")
            sfd = os.open(f"/proc/self/fd/{pin}", os.O_RDONLY | os.O_CLOEXEC)
            dfd = os.open(parts[1], os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_CLOEXEC | os.O_NOFOLLOW, 0o600, dir_fd=ddir)
            try:
                opened = os.fstat(sfd)
                require((opened.st_dev, opened.st_ino) == (before.st_dev, before.st_ino), "source pin mismatch")
                h, remaining = hashlib.sha256(), item["length"]
                while remaining:
                    data = os.read(sfd, min(65536, remaining))
                    require(data, "source truncated during copy")
                    remaining -= len(data)
                    h.update(data)
                    _write_all(dfd, data)
                _same_file(before, os.fstat(sfd))
                require(h.hexdigest() == item["sha256"], "source digest changed during copy")
                os.fchmod(dfd, int(item["mode"], 8))
                os.fsync(dfd)
            finally:
                os.close(sfd)
                os.close(dfd)
        finally:
            os.close(pin)
        os.fsync(ddir)
    finally:
        os.close(sdir)
        os.close(ddir)


def _active(rootfd: int) -> str | None:
    try:
        pin = os.open("active", os.O_PATH | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=rootfd)
    except FileNotFoundError:
        return None
    try:
        st = os.fstat(pin)
        require(stat.S_ISREG(st.st_mode) and stat.S_IMODE(st.st_mode) == 0o444 and
                st.st_uid == st.st_gid == 0 and st.st_nlink == 1, "active metadata")
        fd = os.open(f"/proc/self/fd/{pin}", os.O_RDONLY | os.O_CLOEXEC)
        try:
            raw = _read_fd(fd, 65)
            _same_file(st, os.fstat(fd))
        finally:
            os.close(fd)
        require(len(raw) == 65 and raw.endswith(b"\n") and HEX64.fullmatch(raw[:64].decode("ascii")), "invalid active")
        return raw[:64].decode("ascii")
    finally:
        os.close(pin)


def _check_existing(rootfd: int, instfd: int, generation: str) -> None:
    previous = _active(rootfd)
    if previous is None:
        with os.scandir(instfd) as entries:
            require(next(entries, None) is None, "missing active pointer with retained installations")
        return
    require(previous != generation, "generation already active")
    base = os.path.join(ROOT, INSTALLATIONS, previous)
    _tool("check-staged", base)
    receipt_path = os.path.join(base, "install-receipt.json")
    receipt = parse_json(_trusted_file(receipt_path))
    require(receipt["artifactSetSha256"] == previous, "existing generation digest differs")
    if receipt["qualificationId"] is not None:
        qid = receipt["qualificationId"]
        require(_valid_id(qid), "existing qualification id")
        record_path = os.path.join(ROOT, QUALIFICATIONS, qid + ".json")
        _trusted_file(record_path, exact_mode=0o444)
        _tool("check-association", os.path.join(base, "manifest.json"), receipt_path, record_path)


def _require_install_host() -> None:
    host = os.uname()
    require(host.sysname == "Linux" and host.machine == "x86_64" and "microsoft-standard-wsl2" in host.release.lower(),
            "experimental installation requires WSL2 x86-64")


def install(candidate: str, expected_digest: str, generation: str) -> None:
    require(os.geteuid() == 0, "installer requires uid 0")
    _require_install_host()
    require(HEX64.fullmatch(expected_digest) and generation == expected_digest, "digest/generation mismatch")
    srcfd = _open_dir_path(os.path.abspath(candidate))
    rootfd = _ensure_trusted_root()
    lockfd = _lock(rootfd)
    try:
        manifest_raw, manifest, digest, paths = _manifest(srcfd)
        require(digest == expected_digest, "expected artifact digest mismatch")
        _preflight_payload(srcfd, paths)
        _tool("check-payload", os.path.abspath(candidate))
        os.close(_ensure_child_dir(rootfd, QUALIFICATIONS))
        instfd = _ensure_child_dir(rootfd, INSTALLATIONS)
        try:
            _check_existing(rootfd, instfd, generation)
            stage = ".staging-" + secrets.token_hex(16)
            os.mkdir(stage, 0o700, dir_fd=instfd)
            sfd = os.open(stage, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=instfd)
            try:
                for dirname in sorted({p.split("/")[0] for p in paths if p != "manifest.json"}):
                    os.close(_ensure_child_dir(sfd, dirname))
                _atomic_write(sfd, "manifest.json", manifest_raw, 0o444)
                for path, item in paths.items():
                    if path != "manifest.json":
                        _copy_artifact(srcfd, sfd, path, item)
                now = dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
                receipt = {
                    "schemaVersion": 1, "artifactSetSha256": digest, "manifestFileSha256": digest,
                    "installedByUid": 0, "installedAtUtc": now, "candidatePath": os.path.abspath(candidate),
                    "installerVersion": "b6-s2", "expectedDigestSource": "unverified-candidate",
                    "qualificationId": None, "qualificationRecordSha256": None,
                }
                _atomic_write(sfd, "install-receipt.json", json.dumps(receipt, separators=(",", ":"), ensure_ascii=True).encode() + b"\n")
                os.fchmod(sfd, 0o755)
                _tool("check-staged", f"{ROOT}/{INSTALLATIONS}/{stage}")
                _publish_generation(instfd, rootfd, stage, generation)
            except BaseException:
                try:
                    _safe_remove_tree(instfd, stage)
                except Exception:
                    pass
                raise
            finally:
                os.close(sfd)
        finally:
            os.close(instfd)
    finally:
        os.close(lockfd)
        os.close(rootfd)
        os.close(srcfd)


def _safe_remove_tree(parentfd: int, name: str) -> None:
    fd = os.open(name, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=parentfd)
    try:
        for child in os.listdir(fd):
            st = os.stat(child, dir_fd=fd, follow_symlinks=False)
            if stat.S_ISDIR(st.st_mode):
                _safe_remove_tree(fd, child)
            else:
                require(stat.S_ISREG(st.st_mode), "refuse special staged file cleanup")
                os.unlink(child, dir_fd=fd)
        os.fsync(fd)
    finally:
        os.close(fd)
    os.rmdir(name, dir_fd=parentfd)
    os.fsync(parentfd)


def _valid_id(qid: Any) -> bool:
    return type(qid) is str and ID.fullmatch(qid) is not None and not qid.startswith(".") and not qid.endswith(".")


def _acceptance(qualification_id: str, digest: str, record_hash: str) -> None:
    require(_valid_id(qualification_id), "qualification identifier grammar")
    decision = parse_json(_trusted_file(os.path.join(DECISIONS, qualification_id + ".json"), exact_mode=0o444))
    strict_keys(decision, {"qualificationId", "artifactSetSha256", "qualificationRecordSha256", "accepted"})
    require(decision["qualificationId"] == qualification_id and decision["artifactSetSha256"] == digest and
            decision["qualificationRecordSha256"] == record_hash and decision["accepted"] is True,
            "no matching separately accepted root decision")


def qualify(record_path: str) -> None:
    require(os.geteuid() == 0, "qualification installer requires uid 0")
    _require_install_host()
    rootfd = _secure_root()
    lockfd = _lock(rootfd)
    try:
        digest = _active(rootfd)
        require(digest is not None, "no active generation")
        instfd = os.open(INSTALLATIONS, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=rootfd)
        genfd = os.open(digest, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC | os.O_NOFOLLOW, dir_fd=instfd)
        try:
            manifest = os.path.join(ROOT, INSTALLATIONS, digest, "manifest.json")
            receipt_path = os.path.join(ROOT, INSTALLATIONS, digest, "install-receipt.json")
            record = _trusted_file(os.path.abspath(record_path), owner=None)
            robj = parse_json(record)
            qid = robj.get("qualificationId")
            require(type(qid) is str and ID.fullmatch(qid), "qualification identifier")
            record_hash = hashlib.sha256(record).hexdigest()
            _acceptance(qid, digest, record_hash)
            generation_path = os.path.dirname(manifest)
            _tool("check-staged", generation_path)
            _tool("check-platform", manifest)
            receipt = parse_json(_trusted_file(receipt_path))
            require(receipt["artifactSetSha256"] == digest, "active and receipt differ")
            already_bound = (receipt["qualificationId"] == qid and
                             receipt["qualificationRecordSha256"] == record_hash)
            require(already_bound or receipt["expectedDigestSource"] == "unverified-candidate",
                    "generation already associated with a different decision")
            receipt.update(expectedDigestSource="qualification-record", qualificationId=qid,
                           qualificationRecordSha256=record_hash)
            raw_receipt = json.dumps(receipt, separators=(",", ":"), ensure_ascii=True).encode()
            qfd = _ensure_child_dir(rootfd, QUALIFICATIONS)
            receipt_tmp = ".receipt-" + secrets.token_hex(16)
            record_tmp = ".record-" + secrets.token_hex(16)
            try:
                # Prepare outside the generation. Until the last rename, its exact file set is unchanged.
                _atomic_write(rootfd, receipt_tmp, raw_receipt)
                _atomic_write(qfd, record_tmp, record)
                _tool("check-association", manifest, os.path.join(ROOT, receipt_tmp),
                      os.path.join(ROOT, QUALIFICATIONS, record_tmp))
                try:
                    _atomic_noreplace(qfd, record_tmp, qid + ".json")
                except FileExistsError:
                    require(_trusted_file(os.path.join(ROOT, QUALIFICATIONS, qid + ".json"), exact_mode=0o444) == record,
                            "qualification id collision")
                os.fsync(qfd)
                if not already_bound:
                    os.rename(receipt_tmp, "install-receipt.json", src_dir_fd=rootfd, dst_dir_fd=genfd)
                    os.fsync(genfd)
                    os.fsync(rootfd)
            finally:
                for directory, temp in ((qfd, record_tmp), (rootfd, receipt_tmp)):
                    try:
                        os.unlink(temp, dir_fd=directory)
                        os.fsync(directory)
                    except FileNotFoundError:
                        pass
                os.close(qfd)
        finally:
            os.close(genfd)
            os.close(instfd)
    finally:
        os.close(lockfd)
        os.close(rootfd)


def remove_generation(digest: str) -> None:
    require(os.geteuid() == 0, "removal tool requires uid 0")
    require(HEX64.fullmatch(digest) is not None, "generation digest grammar")
    rootfd = _secure_root()
    lockfd = _lock(rootfd)
    try:
        retention_refusal(digest, _active(rootfd), "Unknown")
    finally:
        os.close(lockfd)
        os.close(rootfd)


def retention_refusal(digest: str, active: str | None, holders: str) -> None:
    require(HEX64.fullmatch(digest) is not None, "generation digest grammar")
    require(active != digest, "refuse to remove active generation")
    require(holders == "None", f"holder state {holders}; generation retained")
    raise Refuse("independent cleanup and ownership evidence unavailable; generation retained")


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("install")
    p.add_argument("--candidate", required=True)
    p.add_argument("--expected-digest", required=True)
    p.add_argument("--generation", required=True)
    p = sub.add_parser("qualify")
    p.add_argument("--qualification", required=True)
    p = sub.add_parser("remove")
    p.add_argument("digest")
    args = parser.parse_args(argv)
    try:
        if args.command == "install":
            install(args.candidate, args.expected_digest, args.generation)
        elif args.command == "qualify":
            qualify(args.qualification)
        else:
            remove_generation(args.digest)
        return 0
    except (Refuse, OSError, ValueError, KeyError, TypeError) as exc:
        print(f"REFUSED: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
