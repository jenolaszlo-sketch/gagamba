# GENERATION-RETENTION-REVIEW.md — HD-4 removal policy

Independent evidence: code inspection of `eng/linux-m1/install_common.py`;
`tests/test_install_common.py::test_retention_always_refuses_unknown_and_active`; the retention
section of `evidence/verification/verification/transactions-final.log`.

## 1. HD-4 policy (accepted)

No automatic garbage collection. Explicit removal only. Removal must refuse generations that are:
active; HELD; used by a live owner or recovery; associated with retained stop authority; of
uncertain ownership; or not safely verifiable.

## 2. Implementation

```
def remove_generation(digest):
    require(geteuid() == 0, ...)
    require(HEX64.fullmatch(digest), ...)
    rootfd = _secure_root()
    lockfd = _lock(rootfd)
    retention_refusal(digest, _active(rootfd), "Unknown")

def retention_refusal(digest, active, holders):
    require(HEX64.fullmatch(digest), ...)          # grammar before any filesystem use
    require(active != digest, "refuse to remove active generation")
    require(holders == "None", f"holder state {holders}; generation retained")
    raise Refuse("independent cleanup and ownership evidence unavailable; generation retained")
```

`remove_generation` always calls `retention_refusal` with `holders = "Unknown"`. Because
`holders != "None"`, control reaches the final unconditional `raise Refuse`. **Therefore production
removal always refuses**, regardless of the digest — including for an inactive generation with no
known holder.

## 3. Critical question: can removal prove a generation is no longer needed?

No. The implementation has no live-holder integration (no check of `/proc`, no mapping/cwd scan, no
recovery-authority query). It therefore **cannot** prove a generation is unused and **must** refuse,
which is exactly what it does. Failure to detect a controller process is never treated as proof that
all relevant authority is gone.

## 4. Required probes

`test_retention_always_refuses_unknown_and_active` (independently re-run: passed) asserts refusal for:

| Probe | Expected | Result |
|---|---|---|
| active generation | refuse | Pass |
| `holders="Unknown"` | refuse | Pass |
| `holders="HELD"` | refuse | Pass |
| `holders="live-owner"`, `"live-recovery"`, `"stopping-authority"`, `"cleanup-incomplete"`, `"controller-dead"` | refuse | Pass |
| `holders="None"` | refuse (no proof available) | Pass |
| `digest="../bad"` | refuse (grammar) | Pass |

Because `remove_generation` hard-codes `"Unknown"`, the code path for `"None"` is only reachable via
the pure helper. Reviewer inspection confirms the installer never calls `retention_refusal` with
`"None"`, so the fail-closed refusal cannot be bypassed through the CLI.

## 5. Residual

Generation removal beyond unconditional refusal is deferred: real holder integration (owner/recovery
liveness, mapping discovery, retained-stop authority) is a later obligation. This is a **safe**
deferral — the tool fails closed, so no active, HELD, live or Unknown generation can be removed
contrary to HD-4.

## 6. Verdict (F)

**An active, HELD, live or Unknown generation cannot be removed contrary to HD-4.** The removal
tool refuses every removal; the failure is closed and no automatic garbage collection exists. The
positive capability ("remove a provably unused generation") is not implemented and is recorded as a
qualification, not a defect.
