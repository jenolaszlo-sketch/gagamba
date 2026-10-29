using Gagamba.Execution.Restricted.Control;

namespace Gagamba.Runtime;

/// <summary>Separate, default-disabled restricted selection. Ordinary execution
/// capabilities never satisfy this contract. B1 has no qualified installation.</summary>
public static class RestrictedExecutionRuntime
{
    public static IRestrictedExecutionProvider Create() => new UnavailableRestrictedExecutionProvider();
}
