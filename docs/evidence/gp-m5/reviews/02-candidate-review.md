# GP-M5 exact-source review — candidate 28ef762

Date: 2026-10-16. Source: clean branch `codex/gp-m5-macos-capture`, commit `28ef762d88ff693d84e37e5747f996d3c27a0f97`. This supplements and preserves [the earlier preliminary source review](01-source-review.md). Native workflow run 38818430476 was in progress when this review was written. This is not final native or package qualification.

## Reviewed additions

The candidate adds `CapturePipeTests` for an extra FIFO writer surviving beyond pipe grace and for a forced bad reader handle; the latter now appears to reach the `read(2)` error path because the test disposes the SafeFileHandle directly while holding the same gate as the native syscall, without setting FifoReader's `_disposed` flag. The setup-failure test added before this candidate remains present. The workflow pins checkout/setup/upload actions, records exact source and host inventory, runs the provider/shared/runtime/conformance suites, packs all five packages from the candidate SHA, validates the lifecycle consumer and installs/runs a separate capture consumer with an isolated NuGet cache.

## Current assessment

The source continues to establish capture endpoints and start both stream drains before launchd dispatch. Retention budgets and overflow semantics are delegated to the shared `OutputCaptureSession`; the Mac reader serializes native reads and handle closure, polls only nonblocking descriptors, and reports native read errors distinctly. No immediate dispatch bypass, unbounded accumulation, or dual-stream deadlock defect was found by source inspection. The exact-source workflow has strong package identity checks: each package nuspec must carry the candidate commit, the consumer uses package references only, and it verifies the native report's SHA, mandatory case count, cleanup flags, and provider disposal.

## Open lifecycle question

On abort and pipe-grace timeout, `OutputCaptureSession.CollectAsync` disposes both streams and returns `DrainIncomplete` without awaiting its local stdout/stderr drain tasks. `MacCapturePipes.CloseAfterCaptureAsync` then waits for each reader's `_quiesced` task. That signal is set in `FifoReader.ReadAsync`'s `finally` before the `DrainAsync` continuation is guaranteed to finish its own `finally` (returning its pooled buffer) and complete. Therefore `CleanupCompletion` may finish just before both drain tasks have terminated on these incomplete paths. The new grace test waits for `CleanupCompletion`, but it cannot independently prove the local drain tasks have completed because the session does not expose/join them on this path.

This is a narrow asynchronous quiescence concern; native results may show it is benign in practice, but task completion ordering is not guaranteed by the code as written. Before final review, either join both drain tasks after cancellation/timeout or provide evidence that the cleanup completion's contract intentionally excludes their final managed continuation. The public promise and remediation evidence standard currently call for reader-task termination, so explicit joining is the stronger invariant.

## Qualification still pending

Do not treat the source review or test definitions as qualification. Await the exact-source ARM64 run and inspect raw capture reports, focused test/TRX results, strict conformance, installed-package manifests and cleanup outcomes. The same-PG tests remain bounded to launchd's documented same-process-group cleanup; `setsid` escape resistance and hostile same-user interference remain excluded.

Disposition at this checkpoint: **no immediate dispatch or bounds defect found; reader-task quiescence on abort/incomplete drain requires resolution or evidence; native and package qualification pending.**
