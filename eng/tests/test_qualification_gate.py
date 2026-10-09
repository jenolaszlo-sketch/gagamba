"""Negative controls for the exact-source release gate (stdlib only)."""
import copy
import json
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from qualification_gate import (BASE_LEGS, IDS, PLATFORMS, SCHEMA,
                                QualificationError, sha256, validate)

SHA = "a" * 40
VERSION = "1.2.3-preview.4"


class QualificationGateTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.packages = Path(self.temp.name)
        rows = []
        hashes = {}
        for package_id in IDS:
            name = f"{package_id}.{VERSION}.nupkg"
            path = self.packages / name
            dependencies = ""
            if package_id == "Gagamba.Runtime":
                dependencies = f'<dependencies><group targetFramework="net10.0"><dependency id="Gagamba.Execution" version="{VERSION}" /></group></dependencies>'
            nuspec = (f'<package><metadata><id>{package_id}</id><version>{VERSION}</version>'
                      f'<repository type="git" commit="{SHA}" />{dependencies}</metadata></package>')
            with zipfile.ZipFile(path, "w") as archive:
                archive.writestr(package_id + ".nuspec", nuspec)
            symbol = self.packages / f"{package_id}.{VERSION}.snupkg"
            symbol.write_bytes(b"symbol fixture " + package_id.encode())
            hashes[package_id] = sha256(path)
            rows.append({"id": package_id, "version": VERSION,
                         "file": name, "sha256": hashes[package_id],
                         "symbolSha256": sha256(symbol)})
        hosts = {}
        for key, (platform, mechanism) in PLATFORMS.items():
            legs = {name: "Passed" for name in BASE_LEGS}
            legs["setsid-escape"] = "Skipped" if key == "windows" else "Passed"
            host = {"sourceSha": SHA, "platform": platform, "nativeMechanism": mechanism,
                    "native": True, "cleanup": "Confirmed", "os": key + " host",
                    "usable": True,
                    "architecture": "X64", "runtime": ".NET 10", "runId": key + "-run",
                    "legs": legs, "skippedMandatory": 0, "unsupportedMandatory": 0}
            if key == "linux": host.update(cgroupVersion=2, delegated=True)
            if key == "macos": host["launchdDomain"] = "gui/501"
            hosts[key] = host
        consumers = {key: {"sourceSha": SHA, "version": VERSION,
                           "result": "Passed", "lifecycle": "Passed",
                           "platform": key,
                           "packageSha256": hashes.copy()} for key in PLATFORMS}
        self.manifest = {"schema": SCHEMA, "sourceSha": SHA,
                         "sourceTreeClean": True, "version": VERSION,
                         "packages": rows, "hosts": hosts,
                         "installedConsumers": consumers}

    def check_rejects(self, modify):
        broken = copy.deepcopy(self.manifest)
        modify(broken)
        with self.assertRaises(QualificationError):
            validate(broken, SHA, VERSION, self.packages)

    def test_complete_exact_candidate_accepts(self):
        validate(self.manifest, SHA, VERSION, self.packages)

    def test_stale_source_rejects(self):
        self.check_rejects(lambda m: m.update(sourceSha="b" * 40))

    def test_dirty_source_rejects(self):
        self.check_rejects(lambda m: m.update(sourceTreeClean=False))

    def test_wrong_version_rejects(self):
        self.check_rejects(lambda m: m.update(version="1.2.4"))

    def test_missing_platform_rejects(self):
        self.check_rejects(lambda m: m["hosts"].pop("macos"))

    def test_missing_required_leg_rejects(self):
        self.check_rejects(lambda m: m["hosts"]["linux"]["legs"].pop("root-exit"))

    def test_skipped_required_leg_rejects(self):
        self.check_rejects(lambda m: m["hosts"]["linux"]["legs"].update({"root-exit": "Skipped"}))

    def test_unsupported_host_rejects(self):
        self.check_rejects(lambda m: m["hosts"]["linux"].update(delegated=False))

    def test_cleanup_unknown_rejects(self):
        self.check_rejects(lambda m: m["hosts"]["windows"].update(cleanup="Unknown"))

    def test_failed_installed_consumer_rejects(self):
        self.check_rejects(lambda m: m["installedConsumers"]["macos"].update(result="Failed"))

    def test_wrong_package_hash_rejects(self):
        self.check_rejects(lambda m: m["packages"][0].update(sha256="0" * 64))

    def test_missing_package_rejects(self):
        path = self.packages / f"{IDS[0]}.{VERSION}.nupkg"
        path.unlink()
        with self.assertRaises(QualificationError):
            validate(self.manifest, SHA, VERSION, self.packages)

    def test_extra_package_rejects(self):
        (self.packages / "evil.1.0.0.nupkg").write_bytes(b"not-a-package")
        with self.assertRaises(QualificationError):
            validate(self.manifest, SHA, VERSION, self.packages)

    def test_modified_symbol_package_rejects(self):
        (self.packages / f"{IDS[0]}.{VERSION}.snupkg").write_bytes(b"tampered")
        with self.assertRaises(QualificationError):
            validate(self.manifest, SHA, VERSION, self.packages)

    def test_mismatched_installed_package_hash_rejects(self):
        self.check_rejects(lambda m: m["installedConsumers"]["windows"]["packageSha256"].update({IDS[0]: "0" * 64}))

    def test_absent_evidence_artifact_rejects(self):
        gate = Path(__file__).resolve().parents[1] / "qualification_gate.py"
        result = subprocess.run([sys.executable, str(gate), "verify",
                                 "--manifest", str(self.packages / "absent.json"),
                                 "--source-sha", SHA, "--version", VERSION,
                                 "--packages-dir", str(self.packages)],
                                capture_output=True, text=True)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("qualification manifest absent", result.stderr)

    def test_manual_and_tag_publish_share_gate_before_credentials(self):
        workflow = (Path(__file__).resolve().parents[2] / ".github/workflows/publish.yml").read_text()
        self.assertIn("workflow_dispatch:", workflow)
        self.assertIn("tags:", workflow)
        self.assertIn("--commit \"$GITHUB_SHA\"", workflow)
        self.assertIn("qualified-release-candidate", workflow)
        self.assertNotIn("dotnet pack", workflow)
        self.assertLess(workflow.index("qualification_gate.py verify"),
                        workflow.index("NuGet/login@v1"))
        self.assertLess(workflow.index("qualification_gate.py verify"),
                        workflow.index("dotnet nuget push"))


if __name__ == "__main__":
    unittest.main()
