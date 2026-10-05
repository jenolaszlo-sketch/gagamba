# GQ-1 Windows Job Object ownership (L5-JOB-OWNERSHIP)

Date: 2026-10-05. Spike: `spikes/Gw1bLaunch/JobOwnership.cs` (plain Win32,
not the sandbox API) + `L5-JOB-OWNERSHIP` leg (manifest v9 = 23 IDs).
One Job Object per activity, `KILL_ON_JOB_CLOSE`, no breakaway flags,
suspend-assign-resume (no escape window), handle held for the domain.
No quotas, completion ports, UI restrictions, or telemetry.

## Acceptance matrix (all pass, ~1s total)

- J1 root only: suspend-launch + assign + resume, in-job verified,
  `TerminateJobObject` kills it.
- J2 depth (root→mid→ping, real parenthood via nested cmd calls):
  all 3 in job, terminate kills all.
- J3 close kills the survivor: no terminate call at all — closing the
  owner handle kills the sleeper (kill-on-close, distinct mechanism).
- J4 exit-code proof: `TerminateJobObject(h, 99)` → root exit code 99
  (proves OUR terminate did it, not natural exit).
- J5 dispose framing: closing the owner handle kills the whole tree.
- J6 crashed supervisor leaves none: helper creates the job, launches a
  quick-exit root + sleeper, reports PIDs, then terminates ITSELF with no
  cleanup. Root exited naturally pre-crash, sleeper alive pre-crash, both
  confirmed dead after. The kernel property holds.
- J7 nested under existing job: helper placed in an outer job creates an
  inner kill-on-close job + sleeper, verified in-inner; helper exits
  normally and the inner close kills the sleeper. Notably the spike
  itself runs `self-jobbed` (host shell job) — nesting works routinely.
- J8 incompatible assignment: a plain process assigned to job A, then to
  job B — permitted here as nesting (verified member of BOTH), contained
  via job A regardless. The deny path (terminate the target, record the
  classified failure, no silent fallback) is implemented in
  `JobOwnership.LaunchIntoJob` and covered by a unit test forcing
  assignment failure with an invalid job handle; this host did not expose
  a genuinely incompatible nesting configuration, so real Windows
  rejection behavior is classified-but-unobserved.

## Contract (verified, was direction)

Kernel-backed descendant ownership: after successful launch, no
descendant outlives the execution domain merely because its parent
exited (J3/J5) or the supervisor crashed (J6). `KILL_ON_JOB_CLOSE` is
the mechanism; the supervisor remains the Gagamba abstraction above it.
Breakaway flags are never set (would defeat ownership). Quotas, ports,
UI limits, and telemetry are explicitly out of GQ-1 scope.
