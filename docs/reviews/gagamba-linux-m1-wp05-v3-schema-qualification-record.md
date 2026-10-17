# WP05 V3 Durable Schema — Composite Qualification Record

Date: 2026-10-17 (Asia/Manila)

Revision binding:

| Item | Revision |
|---|---|
| Branch | `qualification/ar7-feasibility` |
| Accepted pre-implementation baseline | `84a6aac92647b1c09bd729fe6171d62b34063047` |
| Production under qualification | `c075841bcccc31e186719c9e81261285332fe8ac` |
| Schema/codec implementation | `3ffb843f5382e914b7d51325644424f98a361c00` |
| Transitions/recovery implementation | `959bac1fad1c3ec8a5a5834ceb94712461f60edb` |
| Observation idempotency/negative controls | `21a04a1d35d3eea4d81c388903e30bdc7c85b921` |
| Cleanup-state monotonicity | `e004bc4b12d20fbf674c2aef3f0adaed205b0bb8` |
| Implementation evidence/status | `fa093a9`, `a16c2c9`, `c922eda`, `c075841` |
| Independent qualification probes | `df7522e4c20f33a5e9879eb56a5744598a3a6642` |
| Evidence completion document | committed with this record |

This record separates the independent Luna qualification from the later
DeepSeek Pro evidence completion. DeepSeek Pro is the implementation author and
the evidence completer; it is **not** the independent final acceptor. Luna is the
independent reviewer.

## Part I — Independent Luna qualification

Luna reviewed the production tree at `c075841` and established:

- clean branch `qualification/ar7-feasibility`; accepted base
  `84a6aac` present in linear ancestry; production diff confined to the
  authorized v3 schema/recovery boundary; no release/native/target expansion;
  clarified ADR checked against the implementation.
- Raw wait status agrees with the decoded exit/signal fields and remains
  diagnostic, not independent target authority; the journal writer is
  thread-affine; the single active fenced writer remains; full-snapshot
  stale-baseline checks are present.
- No product, ADR, G2 or WP04 counterexample was found.

Fresh Linux non-root results as UID 1000 (`gaga`):

| Focused area | Result |
|---|---:|
| v3 codec/transition | 38 passed |
| recovery classification | 11 passed |
| journal fencing | 6 passed |
| owner protocol | 21 passed |

Luna authored five independent hostile probes:

1. hostile encoded-record mutation and re-checksummed malformed bytes;
2. stale snapshot behavior across intent/commit/stop/exec/root/cleanup;
3. durable-fact disappearance;
4. contradictory observation and invalid delivery/observation combination;
5. v2→v3 identity preservation.

The first probe execution exposed a qualification-only comparison defect: a
preparation digest byte array was compared by reference rather than structural
byte equality. The product produced the expected upgraded record; only the probe
was corrected. No production code changed. The corrected retained probe is
committed at `df7522e` and passes 5/5.

Luna also recorded a WSL environment issue: a non-root rebuild encountered a
root-owned `.Up2Date` artifact left by an earlier WSL build on the Windows-mounted
checkout. It was classified as an environment/build-artifact ownership issue, not
a product defect; the qualification assembly was built with the appropriate
operator context and executed as `gaga`.

Luna's fresh Windows full Linux-M1 run before quota exhaustion:

```text
Total: 478
Passed: 322
Skipped: 156
Failed: 0
```

The session ended during skip classification, freeze/hash checks and final
evidence assembly.

## Part II — DeepSeek Pro evidence completion

DeepSeek Pro completed the remaining objective/mechanical checks against
`c075841`. It is explicitly an evidence completer and the implementation author,
not the independent acceptor. Its completion document is
[wp05-v3-qualification-evidence-completion-2026-10-17.md](../evidence/linux-m1/wp05-v3-qualification-evidence-completion-2026-10-17.md).

DeepSeek Pro reported and recorded:

- fresh Windows Linux-M1 run: 322 passed, 156 skipped, 0 failed / 478, including
  Luna's five independent probes;
- all 156 skips classified as pre-existing Linux-native/environment families;
  no v3-specific test skipped;
- canonical manifest vector: 873 bytes, SHA-256
  `82161029848B1A821241AF112CA806BE8029D3CF2E7B95C6B23CE3E35850D5F3`;
- seccomp freeze: 672 bytes, SHA-256
  `4EE63C48010D74118F74DDEC44B48ACC16CABC3E7E5693E83D941F82BBB3A2BC`;
- full Release solution build: 0 warnings, 0 errors;
- v2 golden record: `5701CD4E2545CBF7FC8382D11B2F66C19D8C7C4D14592F00547BE68331EAF7AE`;
- v3 inventory, exact enum encodings, release/stop/exec/root invariants,
  delivery/observation compatibility, lazy v2→v3, full-snapshot/stale-baseline
  behavior, recovery classification, cleanup independence, WP04 and focused G2
  evidence.

No product defect, ADR counterexample, G2 counterexample or WP04 counterexample
was reported.

## Part III — Luna final audit and acceptance

Luna independently inspected the retained worktree and artifacts:

- verified branch, HEAD `c075841`, ancestry and the production freeze; no
  production file changed after `c075841`;
- inspected and accepted the corrected independent probe file; committed it at
  `df7522e` as qualification-only work with no production changes;
- audited the DeepSeek Pro completion document against the retained code/tests
  and the actual commands/results; the provenance split above is accurate;
- independently re-ran selected checks rather than treating the completion
  summary as self-authenticating:
  - Windows full Linux-M1: **322 passed, 156 skipped, 0 failed / 478**;
  - non-root Linux full test assembly as UID 1000 `gaga`: **405 passed, 73
    skipped, 0 failed / 478** (all v3 codec/transition, recovery classification,
    journal-fencing and owner-protocol rows executed; no v3/fence/owner-protocol
    skip);
  - independent hostile probe class: **5/5**;
  - canonical manifest vector: **1/1**, 873 bytes, SHA-256
    `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`;
  - seccomp freeze: 672 bytes, SHA-256
    `4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc`;
  - Release solution build: 0 warnings, 0 errors;
  - advertised v2 golden value recomputed from encoded v2 bytes and matched.
- audited the Windows skip classification: 156 rows across the documented
  pre-existing families; no v3 test skipped; no required v3 property hidden
  behind a skip;
- reviewed the v3 fields, exact enum encodings, release/stop/exec/root
  invariants, lazy v2→v3 full-snapshot preservation, stale-baseline/no-lost-fact
  rules, writer-fence use, recovery classification, cleanup/result independence,
  WP04 legacy boundary and focused G2 evidence.

Non-blocking evidence-coverage notes:

- The independent probe's v2→v3 allocation equality assertion uses an empty
  allocation inventory. Allocation preservation is structurally enforced by the
  full-snapshot `current with` transition and `IdentityEqual`, and allocation-
  bearing v2 round-trip/cleanup tests pass. This is an evidence-coverage note,
  not a product or ADR defect.
- The shared reconciliation cleanup writer now uses the latest-snapshot
  `WriteMutation` path so cleanup preserves v3 facts. WP04 HELD/abort behavior is
  unchanged; native WP04 HELD/owner-death tests were not rerun because no native
  WP04 behavior changed. The full portable suite and WSL owner-protocol tests
  pass.

## Qualification totals

| Check | Result |
|---|---|
| Windows full Linux-M1 | 322 passed, 156 skipped, 0 failed / 478 |
| Non-root Linux full test assembly (UID 1000) | 405 passed, 73 skipped, 0 failed / 478 |
| Luna independent hostile probes | 5/5 |
| Focused Linux non-root (Luna) | v3 38/38, classification 11/11, fencing 6/6, owner protocol 21/21 |
| Focused WP04/journal Windows | 64/64 |
| Canonical manifest vector | 1/1; 873 bytes / `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3` |
| Full Release build | 0 warnings, 0 errors |
| V2 golden | `5701CD4E2545CBF7FC8382D11B2F66C19D8C7C4D14592F00547BE68331EAF7AE` |
| Seccomp freeze | 672 bytes / `4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc` |

## Findings

No blocking product, ADR, G2 or WP04 finding. The implementation faithfully
implements the accepted v3 durable-schema contract within the authorized scope.

## Limitations

This qualification does not prove release-pipe correctness, release/stop runtime
arbitration, actual single-use release-capability consumption, coordinator
RELEASE parsing/consumption, native post-release behavior, post-release status
transport, target initialization/execution, integrated process-loss behavior
after real release, full WP05, or WP10. The deferred
wrong-audit-architecture seccomp-generator decision remains deferred.

**WP05 V3 SCHEMA ACCEPTED - RELEASE PROTOCOL IMPLEMENTATION MAY PROCEED**
