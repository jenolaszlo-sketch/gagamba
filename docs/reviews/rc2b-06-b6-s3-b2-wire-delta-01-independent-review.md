# Independent security review of the B2 owner echo amendment

**DESIGN REVIEW PASSES, subject to the stated trusted-launcher boundary and future qualification gates.** Owner acceptance is pending.

2026-11-03 · GAGAMBA-RC2B-06-B6-S3-B2-WIRE-DELTA-01 · Independent reviewer GPT-6.1 Terra, high reasoning. GPT-6 Astra, high reasoning, participated in the architecture design. The reviewer inspected the actual canonical source, challenged the proposal and verified the two final draft hashes from disk. This record preserves the reviewer's disposition of the exact texts below.

| Reviewed input | Identity |
|---|---|
| Canonical source commit | `ccc7e0b316d5ad5d9da645108798aee28cbc8fc1` |
| Canonical source tree | `ff96f4753dc4a08d83827b7bf05fd567d8bfad0f` |
| [Normative amendment](../design/rc2b-06-b6-s3-b2-wire-delta-01.md), SHA-256 of UTF-8 bytes with LF | `88beb1a2e40658c19f14acc96b3fd9215c7bc5b94e59f3a961c3ee27f63e3ad6` |
| [Evidence and qualification contract](../evidence/rc2b-06-b6-s3-b2-wire-delta-01.md), SHA-256 of UTF-8 bytes with LF | `6c7daf1d3763da234b65cdf44451fff94316c8c254bafc6164b59bbb0fca0fbc` |

## Findings and resolution

The initial draft required a clean Ready snapshot but left its predicate implicit. The revised text defines exact JSON fields and values, distinct session identifiers, original live process checks, and separate R-send and C-return decisions under their respective stop latches. The reviewer closed this finding against the hashes above.

The reviewer also challenged the post-spawn pidfd acquisition, whether R can nominate another live child, and whether a synchronous hash after 24 would delay stopping. The contract binds the retained spawn reference to the subsequent private lifeline exchange, states its dependence on a trusted launcher and requires independent stop servicing while verification runs. The native probes for these guarantees remain mandatory.

## Ten-question disposition

| Question | Independent determination |
|---|---|
| 1. Does C authenticate the owner? | **Pass within the stated model.** One-shot trusted spawn custody, the exclusive original lifeline, the live retained pidfd, fresh connection peer and per-record credentials bind O. Matching bytes or parentage alone is insufficient. |
| 2. Can R forge or substitute success? | **Pass for R as sender or relay.** R cannot produce O's credentialled 36 on the pinned connection. A Byzantine launcher choosing a different designated child remains outside the guarantee. Pidfd and lineage alone cannot establish launcher honesty. Preserve this limit in the owner decision. |
| 3. Is the lifeline preserved? | **Pass as designed.** Demonstrate continuous 23–26 servicing, nonblocking stop latching and bounded workers. A synchronous hash/connect on the stop reader after 24 would fail review. |
| 4. Are direction and sequence clear? | **Pass.** Control, lifeline and evidence counters have distinct owners, allowed roles, phases and shapes. Integrated sequencing remains separate. |
| 5. Are descriptor lifetimes sound? | **Pass as designed.** Native tests must prove original-spawn custody, exclusive inheritance, actual packet credentials, CLOEXEC, ancillary rejection/cleanup and the empty-record versus drained-half-close distinction. Unsupported behavior refuses. |
| 6. Is RELEASE authority expanded? | **Pass.** The evidence socket and new pidfd roles carry no RELEASE writer or operation. Owner-only physical RELEASE and its one-attempt limit remain in force. |
| 7. Do failures remain conservative? | **Pass as designed.** Continuous stop, once-only obligations, post-spawn R survival, the precise Ready gate and conservative Unknown treatment are specified. Final liveness is necessarily a local observation. |
| 8. Is legacy B2 preserved? | **Pass as designed.** Exercise v1 with and without delegated parent. The explicit refusal of qualified delegated v2 before allocation is a scope choice for the owner. |
| 9. Is integrated B3 preserved? | **Pass as designed.** Integrated keeps rendezvous 8 and GID1. There is no B2-only dispatch on this path. Actual regression and qualified integration remain separate gates. |
| 10. Can S3 implement this without another wire decision? | **Pass.** The revised snapshot and commit rules close the outstanding ambiguity. The reviewer found no remaining architectural or wire choice in these bytes. OS behavior and faithful loaded-closure checks still require proof. |

The evidence matrix addresses all 28 requested cases with controls, outcomes and prerequisites. None were executed for this design review. The current WSL2 installation gate, actual loader closure and production topology qualification remain open. Only the owner can accept the amendment and separately authorize S3.
