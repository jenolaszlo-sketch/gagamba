# AR-5 feasibility gate — output contract unresolved

Date: 2026-10-09 (Asia/Manila)  
Slice: AR-5, stopped before implementation  
Source baseline and tested source SHA: `674691b64b58c1e387ff2b0af549e7573b7894b7` (`main`, clean tree at gate). No AR-5 source change or test result is claimed.  
Host: Windows 10.0.26200 x64, .NET SDK 10.0.401/runtime 10.0.12; Ubuntu 26.04.1 LTS under WSL2, Linux 6.6.114.1 x86-64, SDK 10.0.112/runtime 10.0.12. Native macOS launchd was unavailable.  
Qualification status: **not implemented; not locally/native/package/release-qualified**. AR-3 and AR-4 qualification limits remain as recorded in their evidence.

## Stop condition and observed source facts

The user instruction's Section 12 requires stopping if a security guarantee cannot be demonstrated, a breaking redesign appears necessary, or a platform solution would silently weaken semantics. The AR-5 required bounded-output and portable run/timeout/collect/stop/discard flow cannot be demonstrated from this provider contract and implementation:

| Platform / layer | Current observable behavior | Why the required claim fails |
| --- | --- | --- |
| `IExecutionProvider` / `ProcessStartSpec` | Prepare, Launch, Terminate, Wait and Dispose carry no output stream, route, per-stream budget or overflow status. `CompletionResult` has no output/cleanup field. | A runtime helper can request termination on deadline and make a redacted receipt, but cannot inspect or bound target stdout/stderr. Waiting with cancellation only cancels observation. |
| Windows | `CreateProcessW` uses `bInheritHandles=false` and no provider-owned capture handles in startup info. | The provider cannot return or drain child stdout/stderr. Capturing through a separate `Process` outside the Job Object would lose the proven ownership semantics. |
| Linux | Spawn deliberately preserves trusted inherited fd 0–2 and closes unrelated fd >=3. No capture pipes are installed. | Output goes to host-supplied stdio; the provider cannot enforce byte budgets or a descendant-held-pipe deadline. |
| macOS | The launchd plist assigns `StandardOutPath` and `StandardErrorPath` to ordinary private per-job files. Their directory is 0700 after AR-3, but file growth has no bound until cleanup. | A stdout/stderr flood can consume unbounded disk in hidden files. Privacy of the directory does not impose a quota or protect against a same-identity hostile target. Redirecting to `/dev/null` would remove capture and overflow semantics, so it is not a full AR-5 repair. |

These are static source findings, not a fabricated flood experiment. No resource-flood test was run against the current uncontrolled path. The source already demonstrates why it cannot satisfy a finite output budget. AR-5 source/test changes were intentionally not started after this gate. Luna independently inspected the provider paths and reached the same feasibility finding.

## Concrete continuation design for a separate decision

The smallest safe extension is an **opt-in output-capable provider contract** with explicit per-stream byte budgets, concurrent draining, overflow status and a finite completion/pipe deadline. It must keep the existing raw and vector invocation routes and ordinary `IExecutionProvider` calls compatible. The extension should return a bounded output result (or explicit unsupported/refusal before target dispatch), so the orchestration helper never guesses whether output was captured. It must specify binary bytes rather than decoded characters for limits; zero-length budgets and combined/per-stream limits must be unambiguous. Raw output is excluded from default receipts, with explicit opt-in retention and cleanup.

Native provider work is required: Windows needs provider-owned inheritable stdio handles with inheritance confined to intended handles; Linux needs pipe/file-action setup and independent concurrent drains while retaining atomic cgroup placement; macOS needs a measured launchd-compatible bounded stream topology rather than ordinary unbounded `StandardOutPath` files. An alternate root/trampoline design would need proof that it preserves root exit, descendants, ownership and cleanup on each OS. No such macOS behavior has been measured at this revision, and AR-3 native qualification is still pending. A future design must use independent outer budgets in flood and descendant-held-pipe tests, and must treat cleanup uncertainty as Unknown or Failed, never Confirmed.

Once bounded capture exists, an additive runtime orchestration helper can snapshot the AR-4 invocation, compute a redacted canonical fingerprint, launch through the selected provider, and distinguish natural/nonzero exit, caller cancellation, deadline, explicit stop, launch failure, observation failure and cleanup failure. At deadline it must explicitly call `Terminate`, then wait under a separate cleanup bound. Its receipt can include correlation ID, platform/capability receipt, timestamps, duration, stop cause, root result, output status and Confirmed/Failed/Unknown cleanup. It must not reveal arguments, environment or output by default. This helper alone is feasible, but it does not solve bounded output, so implementing it now would leave the required AR-5 flow incomplete.

The proposed extension is additive in intent, but no public signature is adopted by this document. A concrete consumer/compatibility review and native macOS availability are needed before implementing it. AR-5 remains **blocked at the user-specified guarantee gate**, and AR-6 is not started. AR-5 would not provide filesystem confinement, network confinement, identity isolation, arbitrary disk or CPU quotas, or full hostile-code isolation even after output is repaired; AR-7 remains separate.
