"""Authoritative cleanup classification shared by the native qualification matrices.

The durable journal verdict (``OwnershipCleanupState``) is authoritative.
A successfully persisted ``Unknown`` verdict is not Confirmed cleanup, even
when a post-reconciliation physical fallback removed the retained resource
(B4Q1-F01). Confirmed also requires the final physical/reaping/fencing
component flags, so a live or final-flag observation never outranks the
durable record.

This module is stdlib-only so the classification contract can be regression
tested offline without native prerequisites.
"""
import json
import pathlib

DURABLE_EVIDENCE = 'integrated-journal-final.json'
DURABLE_NOT_STARTED = 0
DURABLE_IN_PROGRESS = 1
DURABLE_CONFIRMED = 2
DURABLE_FAILED = 3
DURABLE_UNKNOWN = 4
DURABLE_VALUES = frozenset((DURABLE_NOT_STARTED, DURABLE_IN_PROGRESS,
                            DURABLE_CONFIRMED, DURABLE_FAILED, DURABLE_UNKNOWN))
REQUIRED_FINAL_FLAGS = ('OwnerReaped', 'WriterFence', 'DomainEmpty',
                        'StagingRemoved', 'JournalFinalized')
PHYSICAL_EVIDENCE = ('DomainEmpty', 'DomainRemoved', 'StagingRemoved')


def _verdict(value):
    """Accept only the exact-integer on-wire enum values."""
    return value if type(value) is int and value in DURABLE_VALUES else None


def durable_verdict(evidence_directory):
    """Return the production-decoded durable cleanup verdict, or None.

    ``IntegratedRecoveryHost`` writes ``integrated-journal-final.json`` from
    the final journal record after reconciliation. Missing, malformed or
    out-of-contract evidence is unknown, never Confirmed.
    """
    try:
        record = json.loads((pathlib.Path(evidence_directory) / DURABLE_EVIDENCE).read_text())
        value = record['Cleanup']
    except (OSError, ValueError, TypeError, KeyError):
        return None
    return _verdict(value)


def status_verdict(status):
    """Return the durable verdict from a controller ``journal-status`` reply."""
    try:
        if status.get('Ok') is not True:
            return None
        value = status['Snapshot']['Cleanup']
    except (AttributeError, TypeError, KeyError):
        return None
    return _verdict(value)


def final_flags(recovery_final):
    """Strict component flags; only literal ``true`` counts as confirmed."""
    source = recovery_final if isinstance(recovery_final, dict) else {}
    return {name: source.get(name) is True for name in REQUIRED_FINAL_FLAGS}


def classify_final_record(recovery_final, durable_cleanup):
    """Classify a recovery final record against the durable verdict."""
    flags = final_flags(recovery_final)
    source = recovery_final if isinstance(recovery_final, dict) else {}
    physical = any(source.get(name) is True for name in PHYSICAL_EVIDENCE)
    confirmed = durable_cleanup == DURABLE_CONFIRMED and all(flags.values())
    return {
        'DurableVerdict': durable_cleanup,
        'FinalFlags': flags,
        'PhysicalCleanupObserved': physical,
        'PhysicalCleanupImproved': physical and durable_cleanup != DURABLE_CONFIRMED,
        'State': 'Confirmed' if confirmed else 'Unknown',
    }


def classify_dispose(dispose_cleanup, durable_cleanup):
    """Classify the live-owner dispose observation against the durable verdict."""
    live = isinstance(dispose_cleanup, dict) and dispose_cleanup.get('State') == 1
    confirmed = live and durable_cleanup == DURABLE_CONFIRMED
    return {
        'DurableVerdict': durable_cleanup,
        'DisposeState': dispose_cleanup.get('State') if isinstance(dispose_cleanup, dict) else None,
        'LiveCleanupConfirmed': live,
        'State': 'Confirmed' if confirmed else 'Unknown',
    }
