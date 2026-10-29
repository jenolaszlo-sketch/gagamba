namespace Gagamba.Execution.Linux.M1.Bootstrap;

/// <summary>M-01 lifecycle gate for one raw descriptor shared between
/// protocol callers and concurrent disposal. Every descriptor use runs under
/// the same monitor that performs the single close, so the disposed check and
/// the use cannot be split by a concurrent close (a check-then-use race could
/// otherwise let a closed, or OS-reused, descriptor number reach IPC). Once
/// disposal has set the closed state no operation callback runs again, and a
/// close never executes twice.</summary>
internal sealed class GuardedDescriptor : IDisposable
{
    private readonly object _gate = new();
    private readonly Action<int> _close;
    private int _descriptor;
    private bool _closed;

    internal GuardedDescriptor(int descriptor, Action<int>? close = null)
    {
        _descriptor = descriptor;
        _close = close ?? Preparation.Native.PreparationInterop.CloseFd;
    }

    /// <summary>The current descriptor number, or -1 once disposal completed.
    /// Callers must not use a descriptor obtained outside <see cref="TryUse"/>.</summary>
    internal int Descriptor
    {
        get
        {
            lock (_gate)
            {
                return _descriptor;
            }
        }
    }

    internal bool IsClosed
    {
        get
        {
            lock (_gate)
            {
                return _closed;
            }
        }
    }

    /// <summary>Runs one bounded descriptor operation under the lifecycle
    /// gate. Returns false, without invoking the operation, once disposal has
    /// started or completed. The gate stays held for the whole operation, so
    /// disposal waits for in-flight descriptor use before it closes.</summary>
    internal bool TryUse(Func<int, bool> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        lock (_gate)
        {
            if (_closed)
            {
                return false;
            }

            return operation(_descriptor);
        }
    }

    public void Dispose()
    {
        int descriptor;
        lock (_gate)
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            descriptor = _descriptor;
            _descriptor = -1;
        }

        // Safe outside the lock: the closed state is already visible to every
        // later TryUse, and in-flight users released the lock before this.
        _close(descriptor);
        GC.SuppressFinalize(this);
    }
}
