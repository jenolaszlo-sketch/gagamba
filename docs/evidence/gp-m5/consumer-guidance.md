# GP-M5 captured execution guidance

The GP-M5 candidate uses the existing `IOutputCaptureProvider` and
`ExecutionRuntime.LaunchCaptured` API. No public API or capability level is
added. Qualification belongs to the exact source and package hashes in the
qualification evidence; a package version alone is insufficient provenance.

On supported macOS ARM64 hosts, preparation requires a usable current-user
launchd GUI domain. The provider remains a launchd, same-process-group lifecycle
provider. It does not establish hostile-code isolation, setsid escape cleanup,
owner-death cleanup, or filesystem/network confinement.

Set explicit stdout, stderr and total byte budgets with `OutputCaptureOptions`.
The existing ceilings are 16 MiB per stream and 32 MiB combined. `RetainBytes`
is opt-in. These budgets bound retained bytes, with additional finite buffers
and copies; they are not an RSS quota. Both streams continue draining after
overflow. The output status explicitly reports `Overflow`; there is no promise
of fairness or ordering between streams competing for the total budget.

Invalid options, invalid input or failure to establish private FIFO endpoints
refuse before dispatch and leave the issued preparation reusable or discardable.
After admission, a failed launch may have consumed the preparation; inspect the
failure and allow the owning provider to complete cleanup. Never retry a mutation
merely because an execution or observation failed.

Prefer `ExecutionOrchestrator.RunAsync` for a combined result. Require the
expected cause and exit code, `CleanupStatus.Confirmed`, and
`OutputCaptureStatus.Complete` before treating a run as successful. Overflow,
deadline, caller cancellation, explicit stop, observation failure and cleanup
uncertainty remain distinct. Receipts do not include retained output by default.

When using `LaunchCaptured` directly, await **both** the provider's
`WaitForCompletionAsync` and the capture session's `Completion`. A natural exit
only establishes lifecycle completion. It does not establish complete output.
`DrainIncomplete` and `ReadFailed` must never be converted into success. Stop
the execution on overflow when appropriate; the direct provider drains excess
output but does not itself implement the orchestrator's overflow-stop policy.

Waiter cancellation cancels observation only. Use `Terminate`, explicit stop,
or the owning provider's disposal to stop execution, then await cleanup.
Keep the provider alive through output completion and dispose it explicitly.
macOS completion joins both drain tasks, including failure unwinding.

Private 0600 FIFOs reside beneath the existing 0700 job directory. No ordinary
capture log files grow on disk. Parent reader/keeper descriptors are nonblocking
and close-on-exec. Both readers exist before launchd target release. Keeper
writers close after lifecycle completion, followed by the finite `PipeGrace`
(default five seconds, maximum one minute). Writers that outlive the supported
same-process-group lifecycle can cause `DrainIncomplete`; they do not extend
the guarantee or justify successful output completion.

The producer currently targets .NET 10 only. GP-M5 adds no .NET 8 producer
support. Marang's separate .NET 8 consumer regression does not change this.
The packages remain preview; this remediation is not a graduation or publication.
