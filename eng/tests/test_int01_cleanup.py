"""Deterministic regression for authoritative cleanup classification.

B4Q1-F01: ``missing-staging-ack`` was promoted to Confirmed by the legacy
five-flag rule even though the durable journal verdict was
``OwnershipCleanupState.Unknown`` (4) after a post-reconciliation physical
fallback removal. The durable verdict is authoritative; a successfully
persisted Unknown verdict is not Confirmed cleanup.
"""
import json
import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
import int01_cleanup

FLAGS = {'OwnerReaped': True, 'WriterFence': True, 'DomainEmpty': True,
         'StagingRemoved': True, 'JournalFinalized': True}
FLAG_NAMES = ('OwnerReaped', 'WriterFence', 'DomainEmpty', 'StagingRemoved', 'JournalFinalized')


def legacy_five_flag(recovery_final):
    """The baseline ``int01-fault-matrix.py``/``b4-native-matrix.py`` rule."""
    return 'Confirmed' if all(recovery_final[k] for k in FLAG_NAMES) else 'Unknown'


class CleanupClassificationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.evidence = pathlib.Path(self.temp.name)

    def write_durable(self, payload):
        (self.evidence / int01_cleanup.DURABLE_EVIDENCE).write_text(
            payload if isinstance(payload, str) else json.dumps(payload))

    def assert_unknown(self, recovery_final, durable):
        record = int01_cleanup.classify_final_record(recovery_final, durable)
        self.assertEqual('Unknown', record['State'], record)
        return record

    def test_durable_confirmed_with_every_flag_is_confirmed(self):
        record = int01_cleanup.classify_final_record(dict(FLAGS), 2)
        self.assertEqual('Confirmed', record['State'])
        self.assertEqual(2, record['DurableVerdict'])
        self.assertTrue(all(record['FinalFlags'].values()))
        self.assertFalse(record['PhysicalCleanupImproved'])

    def test_persisted_unknown_with_every_flag_is_not_confirmed(self):
        # Exact B4Q1-F01 shape: all five flags true (physical fallback removed
        # staging) but the durable journal verdict is Unknown (4).
        fixture = dict(FLAGS, DomainRemoved=True)
        self.assertEqual('Confirmed', legacy_five_flag(fixture))  # failing-before rule
        record = int01_cleanup.classify_final_record(fixture, 4)
        self.assertEqual('Unknown', record['State'])
        self.assertEqual(4, record['DurableVerdict'])
        self.assertTrue(all(record['FinalFlags'].values()))
        self.assertTrue(record['PhysicalCleanupImproved'], 'physical removal must stay visible')

    def test_missing_durable_evidence_is_not_confirmed(self):
        self.assert_unknown(dict(FLAGS), int01_cleanup.durable_verdict(self.evidence))

    def test_malformed_durable_evidence_is_not_confirmed(self):
        self.write_durable('{"Cleanup": 2')
        self.assertIsNone(int01_cleanup.durable_verdict(self.evidence))
        self.assert_unknown(dict(FLAGS), int01_cleanup.durable_verdict(self.evidence))

    def test_non_object_durable_evidence_is_not_confirmed(self):
        self.write_durable([2])
        self.assertIsNone(int01_cleanup.durable_verdict(self.evidence))

    def test_out_of_contract_verdicts_are_not_confirmed(self):
        for value in (5, 255, -1, '2', True, 2.0, None):
            with self.subTest(value=value):
                self.write_durable({'Cleanup': value})
                self.assertIsNone(int01_cleanup.durable_verdict(self.evidence))
                self.assert_unknown(dict(FLAGS), int01_cleanup.durable_verdict(self.evidence))

    def test_every_required_final_flag_is_required(self):
        for name in FLAG_NAMES:
            with self.subTest(flag=name):
                fixture = dict(FLAGS)
                fixture[name] = False
                self.assert_unknown(fixture, 2)

    def test_non_boolean_flag_is_not_promoted(self):
        self.assert_unknown(dict(FLAGS, StagingRemoved=1), 2)

    def test_absent_final_record_is_not_confirmed(self):
        self.assert_unknown(None, 2)
        self.assert_unknown({}, 2)

    def test_unknown_domain_ack_keeps_physical_removal_visible(self):
        # Exact independent observation for missing-domain-ack: domain removed
        # physically, staging/finalization not established, verdict NotStarted.
        fixture = {'OwnerReaped': True, 'WriterFence': True, 'DomainEmpty': True,
                   'StagingRemoved': False, 'JournalFinalized': False,
                   'DomainRemoved': True}
        record = self.assert_unknown(fixture, 0)
        self.assertTrue(record['PhysicalCleanupImproved'])
        self.assertTrue(record['FinalFlags']['DomainEmpty'])
        self.assertFalse(record['FinalFlags']['StagingRemoved'])

    def test_dispose_requires_live_and_durable_confirmation(self):
        self.assertEqual('Confirmed',
                         int01_cleanup.classify_dispose({'State': 1}, 2)['State'])
        self.assertEqual('Unknown',
                         int01_cleanup.classify_dispose({'State': 1}, 4)['State'])
        self.assertEqual('Unknown',
                         int01_cleanup.classify_dispose({'State': 1}, None)['State'])
        self.assertEqual('Unknown',
                         int01_cleanup.classify_dispose({'State': 0}, 2)['State'])
        self.assertEqual('Unknown',
                         int01_cleanup.classify_dispose(None, 2)['State'])

    def test_status_verdict_reads_only_acknowledged_snapshots(self):
        self.assertEqual(2, int01_cleanup.status_verdict(
            {'Ok': True, 'Snapshot': {'Cleanup': 2}}))
        self.assertEqual(4, int01_cleanup.status_verdict(
            {'Ok': True, 'Snapshot': {'Cleanup': 4}}))
        for status in (None, {'Ok': False, 'Snapshot': {'Cleanup': 2}},
                       {'Ok': True, 'Snapshot': None},
                       {'Ok': True, 'Snapshot': {}},
                       {'Snapshot': {'Cleanup': 2}}):
            with self.subTest(status=status):
                self.assertIsNone(int01_cleanup.status_verdict(status))

    def test_runners_use_the_shared_authoritative_classifier(self):
        eng = pathlib.Path(__file__).resolve().parents[1]
        core = (eng / 'int01-native-matrix.py').read_text()
        self.assertIn("with_name('int01_cleanup.py')", core)
        self.assertIn('classify_final_record', core)
        self.assertIn('classify_dispose', core)
        for name in ('int01-native-matrix.py', 'int01-fault-matrix.py', 'b4-native-matrix.py'):
            with self.subTest(runner=name):
                source = (eng / name).read_text()
                self.assertIn('classify_final_record', source)
                self.assertNotIn("all(final[k] for k in ('OwnerReaped'", source)
                self.assertNotIn("all(facts['RecoveryFinal'][k] for k in ('OwnerReaped'", source)


if __name__ == '__main__':
    unittest.main()
