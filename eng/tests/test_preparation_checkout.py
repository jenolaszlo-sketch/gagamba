"""Exercise Git checkout conversion against the strict historical identity map."""
import hashlib
import json
from pathlib import Path
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


if __name__ == "__main__":
    unittest.main()
