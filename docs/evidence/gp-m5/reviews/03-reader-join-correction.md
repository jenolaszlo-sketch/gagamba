# GP-M5 corrective source review — reader-task join

Date: 2026-10-16. Review target: corrective diff on candidate `28ef762d88ff693d84e37e5747f996d3c27a0f97`; this diff was uncommitted at review time and has no independent source SHA. See [02-candidate-review.md](02-candidate-review.md) for the original open finding. Native requalification and the full exact-source workflow remain pending for the corrected revision.

## Finding R6 disposition

R6 identified that abort/pipe-grace completion could precede the local `DrainAsync` tasks' finalizers: waiting for the FifoReader read-quiescence signal alone did not prove that each async drain had completed and returned its pooled buffer.

The corrective diff adds an internal `OutputCaptureSession` overload with an opt-in `joinReadersOnCompletion` flag. The existing public constructor delegates with the option disabled, preserving Windows/Linux behavior. `MacCapturePipes` opts in. The collector's `finally` disposes both streams and then awaits `Task.WhenAll(stdout, stderr)`, swallowing drain exceptions only after the existing `ReadFailed`/`DrainIncomplete` result has been selected. Since Task.WhenAll completes only after both child tasks terminate, this closes the ordering gap for the macOS path, including abort and pipe-grace timeout.

`CaptureJoinTests` uses two controlled streams whose read tasks remain incomplete after disposal until explicitly released. It verifies session completion stays pending after only one read finishes and completes only after both have finished. This is a direct oracle for the managed task-join invariant.

Source review: **R6 addressed in the corrective diff; no new issue found in this narrow change.** `git diff --check` passed. I did not run tests or compile. This assessment is not native evidence and does not qualify the corrected source. The exact committed revision must rerun macOS native qualification, provider/shared/runtime/conformance suites, package consumers, and cleanup checks before the review can become final.
