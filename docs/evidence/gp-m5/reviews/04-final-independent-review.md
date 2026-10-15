# GP-M5 macOS bounded capture — final independent review

Date: 2026-10-16. Reviewed source: clean commit `0c1f05e199588622a5987f95a9149374f5bd858b` on `codex/gp-m5-macos-capture`. This review supplements the preserved [initial source review](01-source-review.md), [candidate review](02-candidate-review.md), and [reader-join correction review](03-reader-join-correction.md). Evidence was inspected from the exact-source native macOS run 38818825522 and final-source Windows/Linux qualification bundles under `artifacts/gp-m5/`.

## Review disposition

**No remaining blocking implementation or qualification issue identified for the bounded macOS capture slice at this exact source revision.** The lifecycle gap from R6 was fixed by joining both drain tasks on the macOS path and passed a controlled delayed-reader regression plus the corrected native run. This review does not resume or qualify the later Marang M5 workflow; that remains a separate unchanged consumer qualification.

## Implementation findings and disposition

| Finding | Status | Review result |
| --- | --- | --- |
| Pre-dispatch safety | Fixed and tested | Capture settings, invocation/environment validation, issued token validation, and both private FIFO endpoint pairs are prepared before token consumption and launchd release. The partial-second-endpoint failure regression verifies the first FIFO is removed, the captured target marker remains absent, the sentinel remains intact, and the same preparation can launch an ordinary target. |
| Bounded concurrent streams | Native-qualified within tested scope | Separate stdout/stderr FIFOs feed concurrent bounded drains. Retained bytes respect per-stream and combined budgets; excess data continues draining and reports Overflow. Native ARM64 cases cover exact limits, each stream overflowing, and simultaneous high-pressure writes. |
| Reader/descriptor lifecycle | Fixed and tested | Native reads are nonblocking and serialized against disposal under the same gate; cancellation and quiescence are owned by the reader. Abort/incomplete completion now joins both `DrainAsync` tasks before capture cleanup completes. The delayed-reader test verifies completion waits for both finalizers. Native read-failure and extra-writer-after-grace tests pass. |
| Combined result integrity | Passed | Lifecycle status and capture status remain distinct. Complete output is required for ordinary successful combined results; read failure or incomplete drain yields observation failure/unknown cleanup, and overflow stays explicit. Native cases cover natural/nonzero exit, deadline, caller cancellation, explicit stop, overflow, and observation-only waiter cancellation. |
| Descendant semantics | Passed only for stated boundary | launchd same-process-group root-exit and stop behavior passed. The native conformance setsid case positively observed an escapee survive, matching `EscapeResistant=Absent`. No setsid cleanup guarantee is claimed. |
| PID evidence | Adequately cross-checked | Probe helpers record process identity, but cleanup conclusions also use independent challenge/response effects and provider lifecycle results; PID-query unavailability is not used alone as cleanup proof. |

## Exact-source evidence reviewed

- **Native macOS ARM64:** macOS 15.7.9, .NET 10.0.13, `gui/501`, source SHA matches, source-start/source-end records empty. Capture qualification: 14/14 passed, 0 failed; all 14 workspaces report confirmed cleanup; provider disposal succeeded. macOS provider tests: 55/55; shared execution: 20/20; runtime: 14/14; strict conformance: 6/6; each 0 failed and 0 skipped. Native setup-failure, forced-read-failure, extra-writer grace, and delayed-reader join tests are present in the passing provider TRX. The FIFO mechanism report is marked mechanism-only and independently records exact bytes, EOF after keeper closure, descriptor closure, recognized service absence, and scratch removal.
- **Windows exact-source regression:** Windows 10.0.26200 x64, .NET 10.0.12. Provider: 30/30; shared execution: 20/20; runtime: 14/14; conformance tests: 6/6, all with 0 failed. The conformance JSON binds to the reviewed SHA, reports confirmed cleanup, and has 0 skipped/unsupported mandatory legs.
- **Linux exact-source regression:** Ubuntu 26.04.1 LTS under WSL2, kernel 6.6.114.1, delegated cgroup v2, .NET 10.0.12. Linux provider suite: 39 passed, 0 failed, 3 skipped; shared execution: 20/20; runtime: 14/14; strict conformance: 6/6. The three recorded skips are two AR-5 capture tests in the ordinary provider-suite invocation outside the required delegated subtree plus the root-WSL sibling-migration scope test. The focused repeat inside the delegated cgroup passed both AR-5 capture tests (2/2, 0 skipped). The delegated test cgroup and installed-consumer cgroup both reached `populated 0` and were removed. The initial focused invocation failed preparation because it ran outside that delegated setup; its failure logs are preserved, and the exact-source scoped rerun passed.
- **Installed packages:** the native macOS job produced all five candidate packages tagged with this commit SHA and recorded their hashes. Separate Windows and Linux lifecycle consumers report `Passed`; each reports the same five package SHA256 values as the native package manifest. On macOS, the isolated installed-package capture consumer passed all 14 mandatory capture cases with confirmed cleanup and provider disposal. It used the local candidate feed, an isolated NuGet cache, and package references without project references.

## Qualification boundary

This establishes bounded captured launch for the exercised .NET 10.0.13 macOS ARM64 launchd configuration and exact package set, plus the recorded cross-platform regression scope. It does not establish .NET 8 support for Gagamba (the producer projects target net10.0), arbitrary macOS versions/domains, hostile same-user isolation, filesystem/network confinement, resource quotas, owner-death cleanup, setsid containment, or general sandbox security. The 14-case installed capture consumer is macOS-only by its host guard; Windows/Linux package consumers establish the lifecycle/package path, while their provider capture regressions run from the exact source tree.

The Linux runner retained an initial failed focused attempt and subsequent successful delegated rerun; both are kept in the raw artifacts and distinguished above. No failed result was converted to a pass. The final source tree was clean at the qualified SHA; later evidence-document additions are outside that source SHA and require their own commit identity before any package/release claim.

## Reviewer result

Final independent review for GP-M5 bounded macOS capture: **PASS for the stated qualification boundary at `0c1f05e199588622a5987f95a9149374f5bd858b`. No blocking findings remain.** Do not extend this verdict to the separate Marang M5 semantic qualification or to general macOS sandboxing.
