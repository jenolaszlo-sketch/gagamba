# Decision log

Updated 2026-10-03. Separate user-selected direction from review recommendations.

| ID | Status | Decision |
| --- | --- | --- |
| D01 | User-selected | Develop Gagamba independently; integrate with Hufu later. |
| D02 | User-selected, expanded | Windows, Linux, and macOS are target platforms; begin testing on local Windows. Linux may use WSL2/Docker and macOS may use GitHub CI. |
| D03 | User-selected location | Work under `C:\Users\Laszlos\source\repos`; project directory is `Gagamba`. |
| D04 | Implementation baseline | First executable profile uses immutable policy and one process tree per sandbox. |
| D05 | Implementation baseline | Begin with denied network, explicit filesystem grants and reviewed runtime dependencies. |
| D06 | Proposed | Probe the three platform mechanisms before freezing public APIs or creating the package family. |
| D07 | Implementation baseline | Keep output bounds and wall-clock stop in the initial runner; advertise other resource limits individually. |
| D08 | Accepted for prototyping | Windows production mechanism: experimental API supplemented by a Gagamba supervisor — see [ADR 0001](adr/0001-windows-provider.md). Restricted-token/dedicated-user remains a fallback; production qualification still open. |
| D09 | Open | Minimum supported OS/kernel/.NET versions, helper distribution and installation privileges. |
| D10 | Deferred | Mediated endpoints, persistent sandboxes, live narrowing and OpenShell. |
| D11 | Deferred | Hufu adapter and any replacement of Luban workflows. |
| D12 | Proposed test setup | Local Windows first; separate Ubuntu WSL2 for Linux iteration; Docker as a secondary environment; GitHub macOS capability probe before conformance. |

Preparation selected [offline-process-v1](security-model.md) for the first spike: no loopback network exception, explicit directory semantics, controlled root ancestry, and mandatory bounded I/O/stop evidence. .NET 10 is the fixture/spike target; eventual package TFMs remain a review decision. These are implementation baseline choices made within the authorized prep work, not backend qualification.

The user supplied the GitHub repository; local origin is connected and its initial license commit is preserved. The user requested committing and pushing the preparation baseline; Git history and remote state identify its delivery checkpoint.

The original proposal is preserved without edits. Its examples and broad roadmap are not frozen contracts. The working design corrects identified ambiguities but does not establish production readiness.
