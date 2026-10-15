# GP-M5 first-candidate evidence (not final qualification)

Date: 2026-10-16. Slice: GP-M5 bounded macOS launchd capture, first candidate. Source revision: `28ef762d88ff693d84e37e5747f996d3c27a0f97` (`28ef762`). Branch: `codex/gp-m5-macos-capture`. Native workflow run: [38818430476](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/38818430476). Host: GitHub Actions `macos-15-arm64`, macOS 15.7.9 (24G830), ARM64; launchd user domain `gui/501`; .NET SDK 10.0.401 and runtime 10.0.13. Native provider: `macos-launchd-pg`.

**Status: first candidate only; not final qualification.** The full run below passed on source `28ef762`, but independent source review `02-candidate-review.md` identified a reader-task join race on abort/pipe-grace completion. Corrective source review `03-reader-join-correction.md` records the fix at `0c1f05e` and says exact-source native requalification remains pending. These observations apply only to the first candidate and cannot qualify `0c1f05e` or a later revision.

## Observed results

The exact-source native capture probe reported 14 mandatory cases, 14 passed, 0 failed; all 14 recorded cleanup confirmations. The installed-package capture consumer independently reported 14 mandatory cases, 14 passed, 0 failed; all 14 recorded cleanup confirmations. Provider and contract suites reported 54 macOS tests, 20 shared execution tests, 14 runtime tests, and 6 strict conformance tests; each had 0 failures and 0 skips. The capture provider suite covered finite normal streams, individual and combined bounds, overflow, dual-stream pressure, overflow-triggered stop, nonzero exit, deadline/cancellation, extra FIFO writers beyond pipe grace, reader failures, and repeated lifecycle cycles. The exact per-case records and raw TRX files are in the archive.

The native JSON records provider disposal as succeeded and every probe workspace as absent after tracked executions completed. The GitHub workflow log records successful upload of the raw artifact (artifact ID `12012171675`, uploaded archive digest `2a2ed101591b3f6af3e5d617f8dee169c589e798cd702504f8a80e5d091bb818`). The package consumer report and five exact-source packages are preserved with the same native run. These are observations for the first candidate only.

The Windows source-regression evidence from `windows-28ef762` and `windows-0c1f05e` is preserved in separate archive roots. It includes the available TRX, conformance and consumer reports, package files, and any source/runtime inventory present in each snapshot. Those Windows records are distinct from native macOS capture qualification.

## Baseline and review boundary

The immutable GP-M5 baseline remains in `baseline.md` and `baseline-native.zip`; neither was modified. Reviews `01-source-review.md`, `02-candidate-review.md`, and `03-reader-join-correction.md` remain preserved unchanged. Review 02 found that on abort/incomplete drain, waiting for FifoReader quiescence did not necessarily prove completion of both `DrainAsync` tasks and their pooled-buffer finalizers. Review 03 assessed the corrective diff as addressing that source-level concern and requires full native requalification of the corrected committed revision.

This record does not establish qualification for the corrected source, other macOS versions or architectures, hostile same-user interference, launchd same-process-group escape via `setsid`, or resource restrictions beyond the tested bounded capture behavior. No source edits, new test execution, or native qualification were performed while packaging this evidence.

## Preserved raw artifact

[`first-candidate-native.zip`](first-candidate-native.zip) contains the original `native-28ef762` directory, `native-28ef762.log`, and independent roots `windows-28ef762/` and `windows-0c1f05e/`. `MANIFEST.sha256.txt` inside the archive lists SHA-256 and byte length for each included original file; it excludes itself. Archive SHA-256: `d871b4adc25342cefb1e1717d20575ceb29774d57b036f5b19cef1c05fe2e1e9`.