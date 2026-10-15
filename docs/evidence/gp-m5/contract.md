# GP-M5: refusal analysis and captured-launch contract

Date: 2026-10-15. Slice: GP-M5.0–GP-M5.2. Status: analysis recorded before
provider implementation changes; native mechanism investigation pending.
Historical producer: `9fe127a98f682514138f4f5e333e47bcbeea8fe5`.
Isolated public GP base: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`.
Final implementation SHA: not yet implemented.
Branch: `codex/gp-m5-macos-capture`. Development host: Windows 11 x64;
native target: GitHub-hosted macOS ARM64, version/image/SDK to be observed.
Native prerequisites: usable current-user launchd GUI domain, trusted absolute
`/bin/launchctl`, private Unix temporary directory, supported native capture
endpoints. Gagamba currently targets .NET 10 only: .NET 8 producer tests are
not applicable, and GP-M5 does not add a target-framework support claim.
Marang's later .NET 8 consumer regression is a separate required result.
Scope: bounded capture integrated with the existing launchd/same-process-group
lifecycle. Exclusions: hostile same-user interference, setsid escape resistance,
owner-death cleanup, filesystem/network isolation, resource quotas, restricted
Linux M1 work, Hufu changes and new workflow semantics.

## Preserved baseline and source isolation

Marang M5 history through `85773fe` is immutable. Its supported ARM64 run
`38751625577` records native macOS 15.7.9, SDK 10.0.401, runtime 10.0.13,
provider `macos-launchd-pg`, an accepted preparation, captured-launch refusal,
absent target nonce marker and successful discard. Ordinary native launch/stop
was a separate positive control. Three M4/M5 scenarios could not dispatch.
The new provider-level baseline probe repeats a finite deterministic output
request independently of Marang; its actual native results must be retained.

The public AR-6 ref above was fetched before branching. Its Mac provider,
runtime, OutputCapture, Provider and Platforms files exactly match the historical
M5 producer. Unrelated later Restricted manifest/transaction and Linux M1
history are excluded from this branch, not transplanted into the GP remediation.
This new producer source identity must be explicit in replacement packages.

## Exact refusal and protected invariant

`ExecutionRuntime.LaunchCaptured` selects the actual Mac provider, whose
`MacOsExecutionProvider.LaunchCaptured` currently checks disposal and immediately
returns `Failed("bounded macOS launchd capture is unavailable; no target dispatched")`.
It does not consume the preparation or call bootstrap/kickstart. The current
AR-5 regression proves the same preparation can subsequently launch normally.
The orchestrator discards a refused preparation and returns LaunchFailure.

Ordinary `Launch` snapshots the invocation, then atomically consumes its issued
preparation and registers an in-flight OwnedJob. It writes a private plist,
bootstraps the launchd service with no RunAtLoad/KeepAlive, and kickstarts under
the admission/disposal gate. Kickstart is the intended target-release point.
Readiness then observes running or terminal status; MacExecutionState owns the
terminal observation. Stop uses bootout and recognized service absence. Failed
bootout/unknown observations retain uncertainty and cleanup ownership for retry.

The refusal protects the absence of a measured bounded launchd capture topology.
Ordinary output goes to `/dev/null`. Redirecting to ordinary files would allow
unbounded disk growth. Parent anonymous pipes are not inherited through a
separately launched launchd service merely by naming parent descriptor numbers.
Asynchronous reads alone do not solve pipe setup, deadlocks, EOF, or resource
ownership. Removing the refusal would therefore invent support.

## Observable contract to qualify

- Validate capture options, immutable invocation/environment and issued
  preparation before release. Invalid/unsupported capture or endpoint setup
  must fail before dispatch and leave a safely discardable preparation.
- Retain at most each stream's requested byte budget and the combined total.
  Shared OutputCaptureOptions ceilings remain unchanged (16 MiB per stream,
  32 MiB combined). Memory is bounded by those limits plus finite reader and
  copy buffers; this is not an RSS quota. No unbounded output files exist.
- Drain stdout and stderr concurrently, including after retained limits are
  exceeded. Exactly-at-limit output is Complete; excess seen bytes explicitly
  produce Overflow. Combined-stream interleaving is not an ordering promise.
  The existing orchestrator requests termination on overflow.
- A successful combined run requires recognized lifecycle completion, confirmed
  cleanup and Complete output. Nonzero exit, explicit stop, caller cancellation,
  deadline, overflow, launch/setup failure and observation/drain failure remain
  distinct through existing result types. CompletionResult is lifecycle-only;
  Capture.Completion is separately required. RunReceipt combines both.
- Preserve launchd as the sole target launcher, the same job label and process
  group, no AbandonProcessGroup, exact spec environment, and ordinary-launch
  behavior. Do not add a raw-process capture fallback.
- Own every endpoint/descriptor and reader from creation through terminal
  cleanup. Reader failure/incomplete EOF/unknown launchd state never becomes
  a successful combined run. Stop must not abandon active writers or readers.
  Waiter cancellation remains observation-only. Provider disposal must account
  for capture resources as well as the existing job ownership.
- Inherited output writers follow the existing same-PG cleanup guarantee.
  After confirmed job termination, allow finite configured PipeGrace for EOF;
  persistent escaped/out-of-scope writers produce DrainIncomplete, not success.
  Close capture descriptors on bounded failure. Do not claim broader containment.

## Candidate mechanism: must be measured before adoption

Test private named FIFOs, one per stream, created 0600 beneath the existing
atomic 0700 job directory. Open nonblocking read ends and keeper write ends
before bootstrap/kickstart; start both bounded drains before release. This
prevents blocking launchd opens and premature no-writer EOF. Endpoint setup
failures remain pre-admission refusals. All parent descriptors are close-on-exec.

Once lifecycle termination/bootout is observed, close the keeper write ends so
EOF reflects actual remaining writers. Ordinary unlink of an open FIFO does
not destroy its open descriptor; nevertheless job-directory removal, reader
lifetime and uncertain cleanup must be explicitly coordinated and tested.
If launchd cannot open these endpoints safely, retain the original refusal.
No unmeasured FIFO topology is claimed as implemented or qualified.

Native follow-up: [baseline and mechanism evidence](baseline.md) records the
actual current refusal plus successful finite FIFO experiment at `cc7b76c`.
Implementation may now proceed; pressure and lifecycle cases remain unqualified.
Darwin descriptor flags follow Apple's [fcntl header](https://github.com/apple/darwin-xnu/blob/main/bsd/sys/fcntl.h)
and [open manual](https://github.com/apple/darwin-xnu/blob/main/bsd/man/man2/open.2);
launchd endpoint keys are documented in Apple's [plist manual](https://github.com/apple-oss-distributions/launchd/blob/main/man/launchd.plist.5).
These sources explain the primitives; the native experiment, not the manuals,
establishes the tested launchd/FIFO behavior.

Required native evidence: baseline refusal plus ordinary positive control;
launchd/FIFO setup and full drain; below/exact/over-budget output for both
streams; simultaneous pressure; normal/nonzero exits; cancellation/deadline/
forced stop during active output; same-PG descendant behavior after root exit
and stop; bounded EOF/descriptor cleanup; native lifecycle/conformance suites;
cross-platform regression; package-only consumer; independent review. Record
all failures and skip/unavailable counts separately. GP must pass before the
unchanged Marang M5 scenario is resumed with new exact package hashes.
