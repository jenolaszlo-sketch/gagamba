# Gagamba contributor guidance

Read docs/handoff.md and docs/work-queue.md first. docs/security-model.md is the
offline-process-v1 implementation baseline; docs/fixture-protocol.md defines the
next private test-tooling slice. docs/implementation-plan.md and
docs/testing-and-ci.md define delivery and qualification. Update the queue and
handoff with actual evidence after a coherent slice.

Keep Gagamba independently usable. No Penghou, Hufu, Luban, workflow or
authorization-engine dependencies. Windows, Linux and macOS are in scope; start
with local Windows. Do not implement the archived proposal's integration roadmap.

Unsupported policy, missing mechanisms and setup failure reject before target
dispatch. Never add an unrestricted fallback. Preserve explicit runtime grants,
immutable bindings, process-tree ownership, bounded I/O and real stop evidence.
Public APIs remain provisional until backend spikes qualify their semantics.

Use controlled disposable fixtures and reliable positive controls. Fixture
self-tests, mocked providers, upstream CI and outer Docker denial do not qualify
the backend. Missing/skipped required cases block qualification. Record exact
source/environment identity and cleanup state; avoid secrets in telemetry.

Add useful code rather than empty package scaffolding. Do not install sibling
staging trees or change sibling repositories. Keep native helpers and test-only
unsandboxed controls outside public production execution paths. Preserve the
original proposal verbatim. Run appropriate focused tests and keep planned work
distinct from measured guarantees.
