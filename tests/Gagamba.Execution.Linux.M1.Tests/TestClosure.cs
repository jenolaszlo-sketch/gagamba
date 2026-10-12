// Test helper: an empty PreparedClosure for WP01 capability-semantics
// tests that exercise minting/lookup/consume without staged objects.
using Gagamba.Execution.Linux.M1.Preparation;

namespace Gagamba.Execution.Linux.M1.Tests;

internal static class TestClosure
{
    internal static PreparedClosure Empty() =>
        new(Array.Empty<StagedObject>(), null, 0, Array.Empty<KeyValuePair<string, string>>());
}
