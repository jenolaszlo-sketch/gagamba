# AR-4 invocation, capability and issuance evidence

Date: 2026-10-09 (Asia/Manila)  
Slice: AR-4  
Baseline SHA: `5ec1561ff03fc85e26e0dda16f42c17cd2c3c54c`  
Tested source SHA: `f96f885137e4287866c87d50f76122904f492921`  
Branch: `main`; source checkpoint working tree: clean after commit. This evidence/handoff update follows as a documentation commit.  
Hosts: Windows 10.0.26200 x64, .NET SDK 10.0.401/runtime 10.0.12; Ubuntu 26.04.1 LTS on WSL2 Linux 6.6.114.1 x86-64, SDK 10.0.112/runtime 10.0.12.  
Native prerequisites: Windows Job Objects locally available; WSL2 cgroup v2, glibc 2.43 and atomic spawn placement passed the existing live provider probe. No native macOS launchd host was available.

Status: **Implemented and locally tested; Windows-native and stated WSL2/Linux-native provider regressions passed. Native macOS qualification pending.** Package-qualified: no. Release-qualified: no. AR-4 does not implement a restricted-execution profile. The existing `EscapeResistant=Full` Windows claim remains expressly bounded to ordinarily created direct descendants; broker-created work, filesystem/network/IPC and privileged interference are not included.

## Baseline to fix

| Concern | Baseline source observation | Repair | Fixed observation |
| --- | --- | --- | --- |
| Mutable advertised capabilities/evidence | Public `IReadOnlyDictionary` and negotiation lists were backed by mutable dictionaries/lists. A cast could alter process-wide grants or a preparation's `Met` receipt. This was identified by source inspection; the new regression was not run on the baseline checkout. | Read-only wrappers for all well-known and composed maps (including nested maps), and read-only copies of negotiation result lists. | Mutation attempts fail; subsequent Windows requirement negotiation remains accepted with the original grant. |
| Mutable invocation and malformed input | Providers read caller-owned environment and requirements at different times, after preparation could be consumed. Linux environment validation also collapsed `PATH` and `Path`. | Copy requirements once before negotiation and copy/validate executable, cwd, environment and optional vector before consuming a preparation. Invalid input leaves the token usable. Linux keys use ordinal case-sensitive comparison. | Snapshot mutation and invalid-cwd/NUL/environment tests passed; macOS fake-provider test launched with the same token after an invalid request; native Linux test preserved distinct `PATH` and `Path`. |
| Lossy portable arguments | The raw string is passed to Windows CreateProcess but reparsed by Unix compatibility splitters. | Add `ProcessStartSpec.Vector(...)` and `ArgumentVector`; Unix dispatch takes the ordered vector directly, Windows uses explicit CRT-compatible quote/backslash serialization. Existing positional constructor and `Arguments` string retain their raw compatibility behavior. | Vector tests cover empty, spaces, quote, trailing backslash and Unicode. Windows exact serialization passed; native WSL `/bin/sh` observed the full ordered argv in a file; macOS fake transport verified separate plist array elements. Windows child-observed CRT argv was not measured. |
| False Discard success | A same-platform token from another instance or forged GUID returned `Discarded` if no active ID was found. | Exact issued-object provenance per provider, strong active-token ownership, weak spent-token state for bounded idempotence; launch/discard linearized under provider gate. | Windows-native, WSL-native and macOS fake tests rejected foreign/reconstructed tokens; repeated valid discard passed; macOS fake concurrent launch/discard had exactly one winner. |
| Describe after disposal | Providers/runtime returned capabilities after disposal despite the SPI's disposed-method rule. | `Describe` now checks disposal; an already entered launch whose snapshot races with disposal reports refusal without releasing a target. | Windows disposal regression and post-disposal Describe test passed. |

No baseline test result is fabricated: the baseline observations above are direct source inspection. The Windows lifecycle race regression first failed during this AR-4 change because snapshotting moved a blocking test dictionary before admission; the repair preserved its no-release outcome and restored the test to green.

## Exact checks on tested source

| Check | Result |
| --- | --- |
| Windows full solution build | Passed, 0 warnings, 0 errors. |
| Windows core contract tests | 17 passed, 0 failed, 0 skipped. |
| Windows provider tests | 25 passed, 0 failed, 0 skipped. |
| Windows macOS parser/fake tests | 44 passed, 0 failed, 4 Unix-only skips; no native launchd qualification. |
| Windows runtime and conformance tests | 5 + 5 passed, 0 failed, 0 skipped. |
| Ubuntu WSL Linux provider suite | 40 passed, 0 failed, 0 skipped, including native argv and foreign-token cases. |
| Ubuntu WSL macOS parser/fake/helper suite | 48 passed, 0 failed, 0 skipped; no native launchd. |
| Installed NuGet package / release gate | Not run. |

The WSL tests ran from a disposable source copy at `/tmp/gagamba-ar3-20261009` without Git metadata; SourceLink warnings there reflect that copy and are not package evidence. No `gl2-*` workspace remained under `/tmp` after final tests. The shared legacy `/sys/fs/cgroup/gagamba-gl2-tests` parent still contained ten pre-existing empty test directories; this slice does not claim to remove them. No AR-4 service or durable host configuration was installed.

## Compatibility, identity and limitations

The public SPI method signatures, positional `ProcessStartSpec` constructor and raw `Arguments` route remain. `Vector`/`ArgumentVector` and snapshot helpers are additive. `ArgumentVector` adds a property to record equality/printing, so consumers relying on those incidental behaviors should review them; no source or binary migration was needed for the in-repo callers. Windows vector serialization targets CRT-compatible parsing, not arbitrary application parsers. A relative executable retains ambient platform lookup; an absolute path and cwd are path strings, not stable file identities. Replacement between validation and dispatch remains possible. Security-sensitive future profiles need a separate identity guarantee; AR-4 does not claim executable replacement resistance.

Luna inspected the baseline and proposed tests. Its final read-only review found caller-mutable negotiation receipts, which were frozen, and weak-only active token registration, which gained a strong active reference while a preparation remains live. It also flagged path replacement and Windows custom-parser limits; these are documented limitations rather than silently claimed protections. The reviewed fixes passed the checks above. Model review is not native or package qualification.
