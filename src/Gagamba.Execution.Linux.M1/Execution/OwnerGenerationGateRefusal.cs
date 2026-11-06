// P3 (GAGAMBA-RC2B-06-B6-ALT-B) owner pre-RELEASE generation verification
// refusal payload. Produced only by the owner-side verification, consumed by
// ReleaseArbiter to retain the conservative post-intent refusal. The reason
// tag and error text are typed producer facts, never caller-supplied strings.
using Gagamba.Execution.Restricted.Control;

namespace Gagamba.Execution.Linux.M1.Execution;

internal sealed record OwnerGenerationGateRefusal(RestrictedRefusalReason Reason, string Error);
