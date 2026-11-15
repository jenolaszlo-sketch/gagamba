"""Exercise Git checkout conversion against the strict historical identity map."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


REPOSITORY = Path(__file__).resolve().parents[2]


class PreparationCheckoutTests(unittest.TestCase):
    def check_checkout(self, autocrlf):
        inventory = json.loads((REPOSITORY / "eng/preparation-legacy-links.json").read_text(encoding="utf-8-sig"))
        documents = inventory["documents"]
        with tempfile.TemporaryDirectory(prefix="gagamba-preparation-") as temporary:
            destination = Path(temporary)
            subprocess.run(
                ["git", "-C", str(REPOSITORY), "-c", "core.autocrlf=" + autocrlf,
                 "checkout-index", "--prefix=" + destination.as_posix() + "/", "--"]
                + [document["source"] for document in documents],
                check=True, capture_output=True,
            )
            for document in documents:
                with self.subTest(autocrlf=autocrlf, source=document["source"]):
                    content = (destination / document["source"]).read_bytes()
                    blob = b"blob " + str(len(content)).encode("ascii") + b"\0" + content
                    self.assertEqual(document["expectedSha256"], hashlib.sha256(content).hexdigest())
                    self.assertEqual(document["expectedGitBlob"], hashlib.sha1(blob).hexdigest())

    def test_crlf_checkout_preserves_historical_source_identities(self):
        self.check_checkout("true")

    def test_lf_checkout_preserves_historical_source_identities(self):
        self.check_checkout("false")


class PreparationFenceTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="gagamba-preparation-fences-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        required = (
            "AGENTS.md", "README.md", "LICENSE", "docs/security-model.md",
            "docs/fixture-protocol.md", "docs/work-queue.md", "docs/handoff.md",
            "docs/implementation-plan.md", "docs/testing-and-ci.md",
            "docs/test-environments.md", "docs/decisions.md",
        )
        for name in required:
            self.write(name, "# offline-process-v1 GP-1A\n")
        proposal = "docs/proposals/2026-10-03-original-proposal.md"
        self.write(proposal, (REPOSITORY / proposal).read_bytes())
        self.write("docs/target.txt", "one\ntwo\nthree\n")
        self.legacy = "[original](missing-legacy.md)\n"
        self.legacy_mapping(self.legacy)

    def write(self, name, content):
        file = self.root / name
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_bytes(content.encode("utf-8") if isinstance(content, str) else content)

    def legacy_mapping(self, content, expected_occurrences=1):
        self.write("docs/archive.md", content)
        raw = content.encode("utf-8")
        blob = b"blob " + str(len(raw)).encode("ascii") + b"\0" + raw
        self.write("eng/preparation-legacy-links.json", json.dumps({
            "schemaVersion": 1,
            "documents": [{
                "source": "docs/archive.md",
                "expectedSha256": hashlib.sha256(raw).hexdigest(),
                "expectedGitBlob": hashlib.sha1(blob).hexdigest(),
                "references": [{
                    "reference": "missing-legacy.md",
                    "target": "docs/target.txt",
                    "expectedOccurrences": expected_occurrences,
                }],
            }],
        }))

    def check(self, expected_error=None):
        shell = shutil.which("pwsh") or shutil.which("powershell")
        self.assertIsNotNone(shell, "PowerShell is required for preparation tests")
        env = os.environ.copy()
        # A Python process started by pwsh inherits its module search path.
        env["PSModulePath"] = str(Path(shell).resolve().parent / "Modules")
        run = subprocess.run([
            shell, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
            str(REPOSITORY / "eng/verify-preparation.ps1"),
            "-RepositoryRoot", str(self.root),
        ], capture_output=True, text=True, encoding="utf-8", errors="replace", env=env)
        log = run.stdout + run.stderr
        if expected_error is None:
            self.assertEqual(run.returncode, 0, log)
            self.assertIn("Passed", log)
        else:
            self.assertNotEqual(run.returncode, 0, log)
            self.assertIn(expected_error, log)

    def test_reported_python_expression_and_valid_prose_across_line_endings(self):
        for eol in ("\n", "\r\n"):
            with self.subTest(eol=eol):
                self.write("docs/example.md", eol.join((
                    "[before](target.txt#heading)",
                    "```" + "python", "ns['check'](raw,form,e)",
                    "[fake](missing.md)", "```", "[after](target.txt:1-3)", "",
                )))
                self.check()

    def test_closers_must_match_character_width_and_have_no_info_string(self):
        self.write("docs/example.md", "\n".join((
            "   " + "````" + "python", "```", "~~~", "````" + "text",
            "[still code](missing.md)", "`````", "~~~ csharp", "```",
            "[still code](also-missing.md)", "~~~~  ", "[prose](target.txt)", "",
        )))
        self.check()

    def test_real_broken_link_after_fence_is_still_an_error(self):
        self.write("docs/example.md", "\n".join((
            "~~~lang", "[code](missing.md)", "~~~", "[outside](really-missing.md)", "",
        )))
        self.check("really-missing.md")

    def test_unclosed_root_fence_and_quoted_fence_end_at_their_container(self):
        self.write("docs/example.md", "\n".join((
            "> " + "```" + "py", "> [code](missing.md)", "[prose](target.txt)",
            "~~~", "[unclosed root fence](missing.md)",
        )))
        self.check()
        self.write("docs/example.md", "\n".join((
            "> ~~~", "> [code](missing.md)", "[prose](actual-broken.md)",
        )))
        self.check("actual-broken.md")

    def test_invalid_backtick_info_does_not_hide_a_genuine_link(self):
        self.write("docs/example.md", "```" + "py" + "`" + "\n[prose](actual-broken.md)\n")
        self.check("actual-broken.md")

    def test_legacy_identity_and_occurrences_apply_only_to_prose(self):
        self.legacy_mapping(self.legacy + "~~~md\n" + self.legacy + "~~~\n")
        self.check()
        self.legacy_mapping("~~~md\n" + self.legacy + "~~~\n")
        self.check("Historical reference occurrence mismatch")
        self.write("docs/archive.md", self.legacy)
        self.check("Historical source identity mismatch")

    def test_path_and_line_reference_checks_outside_fences(self):
        for link, diagnostic in (
            ("../docs/target.txt:4", "Unresolved line reference"),
            ("target.txt:3-1", "Invalid line range"),
            ("../../outside.md", "Local reference escapes repository"),
        ):
            with self.subTest(link=link):
                self.write("docs/example.md", f"[outside]({link})\n")
                self.check(diagnostic)


if __name__ == "__main__":
    unittest.main()
