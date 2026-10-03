# Research sources and evidence limits

Checked 2026-10-03. These are primary-source documentation observations, not verification of runtime guarantees. Live branch URLs must be pinned to concrete versions/commits when implementing a backend.

| Source | Observation and consequence |
| --- | --- |
| [Microsoft Create Process in Sandbox](https://learn.microsoft.com/en-us/windows/win32/secauthz/createprocessinsandbox) | Experimental Windows 11 API; dynamic `processmodel.dll` loading and FlatBuffer schema 0.1.0. Inherited handles are unsupported. Unique identities matter because identical identities share a profile. Availability, I/O capture and lifetime need probes. |
| [Microsoft MXC](https://github.com/microsoft/mxc) | Relevant cross-platform orchestration prior art. Its early-preview warning says current profiles must not be treated as security boundaries. Research candidate only. |
| [Bubblewrap](https://github.com/containers/bubblewrap) | Sandbox construction toolkit with a caller-defined security model. Namespace and syscall mechanisms require correct composition; exposing host IPC can undermine containment. |
| [Codex Linux sandbox source documentation](https://github.com/openai/codex/blob/main/codex-rs/linux-sandbox/README.md) | Bubblewrap is documented as the default, with no-new-privileges/seccomp and mount overlays. Current documentation rejects legacy Landlock for filesystem-restricted execution because of Unix-socket isolation. The original proposal's legacy-fallback wording needs qualification. Its read-only host-root profile is not equivalent to Gagamba's proposed denied-read profile. |
| [OpenAI Windows sandbox engineering](https://openai.com/index/building-codex-windows-sandbox/) | Documents developer-tool compatibility issues and dedicated users, restricted tokens, ACLs and firewall setup. Evidence of implementation costs, not proof of a mechanism choice for Gagamba. |
| [Anthropic sandbox runtime](https://github.com/anthropic-experimental/sandbox-runtime) | Documents Linux bubblewrap, macOS Seatbelt and now Windows account/WFP confinement. Defaults include broadly allowed reads; do not import these defaults into a denied-read contract accidentally. |
| [NVIDIA OpenShell controls](https://docs.nvidia.com/openshell/latest/security/best-practices) | Filesystem/process controls are static; network controls support updates. Documents network isolation beyond environment-variable proxy hints. Deferred backend research. |
| [OpenShell prover](https://github.com/NVIDIA/OpenShell/blob/main/crates/openshell-prover/README.md) | Containment analysis has explicit supported coverage and unsupported/inconclusive outcomes. Policy analysis does not observe actual proxy enforcement. |

## Local context

The supplied proposal is [preserved here](proposals/2026-10-03-original-proposal.md).

Related working-tree sources were inspected read-only to assess the initial integration question: Hufu's authority model/request authorizer/start contract, README, AMLE guide and ADRs 0004/0011; Luban's README, contributor guidance and AMLE description; Penghou's resource/workflow plans and Zhinu's authority extension plan. No sibling code was changed. Git revision reads were unavailable from this restricted execution context, so these are working-tree observations rather than commit-pinned evidence.

The standalone project does not depend on these related repositories. Their evidence appears only in the deferred integration assessment.

Additional current platform/CI references and local inventory are recorded in [test environments](test-environments.md). The later user clarification adds macOS to scope.

## Not established

- Experimental Windows API availability or successful sandbox creation on this host.
- Any Windows/Linux escape resistance, workload compatibility, performance or cleanup guarantee.
- Minimum supported Linux kernel/distribution or a qualified Linux test environment.
- Package API stability, production readiness, or complete enforcement of arbitrary contextual authorization policies.

The next research gates exist to establish these facts rather than infer them from documentation.
