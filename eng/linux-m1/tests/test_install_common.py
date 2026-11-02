import hashlib
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import threading
from unittest.mock import patch
import unittest

MODULE = Path(__file__).resolve().parents[1] / "install_common.py"
spec = importlib.util.spec_from_file_location("install_common", MODULE)
install = importlib.util.module_from_spec(spec)
spec.loader.exec_module(install)


class InstallToolingTests(unittest.TestCase):
    def test_duplicate_json_key_refuses(self):
        with self.assertRaises(install.Refuse):
            install.parse_json(b'{"a":1,"a":2}')

    def test_manifest_path_traversal_refuses(self):
        payload = {"artifacts": [{"path": "../escape.bin", "length": 1, "sha256": "0" * 64, "mode": "0444"}]}
        with tempfile.TemporaryDirectory() as temp:
            root = os.open(temp, os.O_RDONLY | os.O_DIRECTORY)
            try:
                raw = json.dumps(payload).encode()
                fd = os.open("manifest.json", os.O_CREAT | os.O_WRONLY, 0o600, dir_fd=root)
                os.write(fd, raw)
                os.close(fd)
                with self.assertRaises(install.Refuse):
                    install._manifest(root)
            finally:
                os.close(root)

    def test_preflight_rejects_symlink_before_artifact_content_read(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "owner").mkdir()
            (root / "recovery").mkdir()
            (root / "owner" / "good.bin").write_bytes(b"safe")
            (root / "recovery" / "bad.bin").symlink_to(root / "owner" / "good.bin")
            paths = {
                "manifest.json": {"length": 1, "sha256": "0" * 64, "mode": "0444"},
                "owner/good.bin": {"length": 4, "sha256": hashlib.sha256(b"safe").hexdigest(), "mode": "0444"},
                "recovery/bad.bin": {"length": 4, "sha256": hashlib.sha256(b"safe").hexdigest(), "mode": "0444"},
            }
            (root / "manifest.json").write_bytes(b"x")
            fd = os.open(temp, os.O_RDONLY | os.O_DIRECTORY)
            try:
                with self.assertRaises(install.Refuse):
                    install._preflight_payload(fd, paths)
            finally:
                os.close(fd)

    def test_preflight_rejects_extra_candidate_entry(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "owner").mkdir()
            (root / "owner" / "one.bin").write_bytes(b"one")
            (root / "extra").write_text("unexpected")
            paths = {"manifest.json": {"length": 1, "sha256": "0" * 64, "mode": "0444"},
                     "owner/one.bin": {"length": 3, "sha256": hashlib.sha256(b"one").hexdigest(), "mode": "0444"}}
            (root / "manifest.json").write_bytes(b"x")
            fd = os.open(temp, os.O_RDONLY | os.O_DIRECTORY)
            try:
                with self.assertRaises(install.Refuse):
                    install._preflight_payload(fd, paths)
            finally:
                os.close(fd)

    def test_copy_hashes_bounded_bytes_from_same_open_file(self):
        with tempfile.TemporaryDirectory() as temp:
            src = Path(temp) / "src"
            dst = Path(temp) / "dst"
            src.mkdir()
            dst.mkdir()
            (src / "owner").mkdir()
            (dst / "owner").mkdir()
            content = b"bounded content"
            (src / "owner" / "a.bin").write_bytes(content)
            sfd = os.open(src, os.O_RDONLY | os.O_DIRECTORY)
            dfd = os.open(dst, os.O_RDONLY | os.O_DIRECTORY)
            try:
                install._copy_artifact(sfd, dfd, "owner/a.bin", {
                    "length": len(content), "sha256": hashlib.sha256(content).hexdigest(), "mode": "0444"})
                self.assertEqual((dst / "owner" / "a.bin").read_bytes(), content)
                self.assertEqual((dst / "owner" / "a.bin").stat().st_mode & 0o777, 0o444)
            finally:
                os.close(sfd)
                os.close(dfd)

    def test_generation_publication_is_noreplace_and_active_is_final(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            inst = root / "installations"
            inst.mkdir()
            stage = inst / ".stage"
            stage.mkdir()
            (stage / "marker").write_bytes(b"ready")
            rootfd = os.open(root, os.O_RDONLY | os.O_DIRECTORY)
            instfd = os.open(inst, os.O_RDONLY | os.O_DIRECTORY)
            try:
                digest = "a" * 64
                install._publish_generation(instfd, rootfd, ".stage", digest)
                self.assertEqual((root / "active").read_bytes(), (digest + "\n").encode())
                with self.assertRaises(FileExistsError):
                    install._atomic_noreplace(instfd, digest, digest)
            finally:
                os.close(rootfd)
                os.close(instfd)

    def test_failed_before_publication_leaves_active_unchanged(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            inst = root / "installations"
            inst.mkdir()
            (root / "active").write_bytes(("b" * 64 + "\n").encode())
            (inst / ".stage").mkdir()
            rootfd = os.open(root, os.O_RDONLY | os.O_DIRECTORY)
            instfd = os.open(inst, os.O_RDONLY | os.O_DIRECTORY)
            try:
                with self.assertRaises(FileNotFoundError):
                    install._publish_generation(instfd, rootfd, ".missing", "a" * 64)
                self.assertEqual((root / "active").read_bytes(), ("b" * 64 + "\n").encode())
            finally:
                os.close(rootfd)
                os.close(instfd)

    def test_interruption_after_rename_retains_both_generations(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            installations = root / "installations"
            installations.mkdir()
            rootfd = os.open(root, os.O_RDONLY | os.O_DIRECTORY)
            instfd = os.open(installations, os.O_RDONLY | os.O_DIRECTORY)
            old, new = "a" * 64, "b" * 64
            try:
                (installations / ".first").mkdir()
                install._publish_generation(instfd, rootfd, ".first", old)
                (installations / ".next").mkdir()
                with patch.object(install, "_atomic_write", side_effect=OSError("interruption before active rename")):
                    with self.assertRaisesRegex(OSError, "interruption"):
                        install._publish_generation(instfd, rootfd, ".next", new)
                self.assertEqual((root / "active").read_bytes(), (old + "\n").encode())
                self.assertTrue((installations / old).is_dir())
                self.assertTrue((installations / new).is_dir())
                (installations / ".retry").mkdir()
                with self.assertRaises(FileExistsError):
                    install._publish_generation(instfd, rootfd, ".retry", new)
            finally:
                os.close(rootfd)
                os.close(instfd)

    def test_reader_sees_complete_generation_on_both_sides_of_pointer_swap(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            inst = root / "installations"
            inst.mkdir()
            rootfd = os.open(root, os.O_RDONLY | os.O_DIRECTORY)
            instfd = os.open(inst, os.O_RDONLY | os.O_DIRECTORY)
            old, new = "a" * 64, "b" * 64
            ready, observed = threading.Event(), threading.Event()
            failures = []
            def read_existing():
                try:
                    self.assertTrue(ready.wait(5))
                    selected = (root / "active").read_text().rstrip("\n")
                    self.assertEqual(selected, old)
                    self.assertEqual((inst / selected / "payload").read_text(), selected)
                    self.assertEqual((inst / new / "payload").read_text(), new)
                except BaseException as error:
                    failures.append(error)
                finally:
                    observed.set()
            write = install._atomic_write
            def pause_before_swap(*args):
                ready.set()
                self.assertTrue(observed.wait(5))
                write(*args)
            try:
                for stage, value in ((".first", old), (".next", new)):
                    (inst / stage).mkdir()
                    (inst / stage / "payload").write_text(value)
                install._publish_generation(instfd, rootfd, ".first", old)
                reader = threading.Thread(target=read_existing)
                reader.start()
                with patch.object(install, "_atomic_write", side_effect=pause_before_swap):
                    install._publish_generation(instfd, rootfd, ".next", new)
                reader.join(5)
                self.assertEqual([], failures)
                self.assertEqual((root / "active").read_bytes(), (new + "\n").encode())
            finally:
                os.close(rootfd)
                os.close(instfd)

    def test_pinned_source_changed_while_copying_refuses(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source, dest = root / "src", root / "dst"
            for directory in (source, dest):
                (directory / "owner").mkdir(parents=True)
            content = b"x" * 70000
            path = source / "owner" / "a.bin"
            path.write_bytes(content)
            sfd = os.open(source, os.O_RDONLY | os.O_DIRECTORY)
            dfd = os.open(dest, os.O_RDONLY | os.O_DIRECTORY)
            meta = {"length": len(content), "sha256": hashlib.sha256(content).hexdigest(), "mode": "0444"}
            try:
                install._copy_artifact(sfd, dfd, "owner/a.bin", meta)
                (dest / "owner" / "a.bin").unlink()
                read = os.read
                def touch_after_read(fd, count):
                    data = read(fd, count)
                    os.utime(path, ns=(1, 1))
                    return data
                with patch.object(os, "read", side_effect=touch_after_read):
                    with self.assertRaisesRegex(install.Refuse, "file changed"):
                        install._copy_artifact(sfd, dfd, "owner/a.bin", meta)
            finally:
                os.close(sfd)
                os.close(dfd)

    def test_retention_always_refuses_unknown_and_active(self):
        digest = "a" * 64
        with self.assertRaisesRegex(install.Refuse, "active"):
            install.retention_refusal(digest, digest, "Unknown")
        with self.assertRaisesRegex(install.Refuse, "Unknown"):
            install.retention_refusal(digest, "b" * 64, "Unknown")
        with self.assertRaisesRegex(install.Refuse, "cleanup"):
            install.retention_refusal(digest, "b" * 64, "None")
        for state in ("HELD", "live-owner", "live-recovery", "stopping-authority", "cleanup-incomplete", "controller-dead"):
            with self.subTest(state=state), self.assertRaises(install.Refuse):
                install.retention_refusal(digest, "b" * 64, state)
        with self.assertRaisesRegex(install.Refuse, "grammar"):
            install.retention_refusal("../bad", None, "Unknown")


if __name__ == "__main__":
    unittest.main(verbosity=2)
