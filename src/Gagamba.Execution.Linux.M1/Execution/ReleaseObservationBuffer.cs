// WP05 bounded observation ordering. A coordinator status frame may arrive
// immediately after RELEASE, before the committed delivery snapshot is
// durable. Validated observations are buffered (at most one exec and one root)
// and persisted only after durable ReleaseCommitted; contradiction is refused.
using Gagamba.Execution.Linux.M1.Recovery;

namespace Gagamba.Execution.Linux.M1.Execution;

internal sealed class ReleaseObservationBuffer
{
    private readonly object _gate = new();
    private bool _durableCommitted;
    private ExecObservation? _exec;
    private RootObservation? _root;
    private bool _execPersisted;
    private bool _rootPersisted;

    internal void MarkDurableCommitted()
    {
        lock (_gate)
        {
            _durableCommitted = true;
        }
    }

    internal bool OfferExec(ExecObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        lock (_gate)
        {
            if (_exec is not null)
            {
                return _exec == observation;
            }

            if (_execPersisted)
            {
                return false;
            }

            _exec = observation;
            return true;
        }
    }

    internal bool OfferRoot(RootObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        lock (_gate)
        {
            if (_exec is null && !_execPersisted)
            {
                return false;
            }

            if (_root is not null)
            {
                return _root == observation;
            }

            if (_rootPersisted)
            {
                return false;
            }

            _root = observation;
            return true;
        }
    }

    /// <summary>Persists buffered observations in plan order. Before durable
    /// ReleaseCommitted this is a successful no-op; afterward it uses the
    /// existing journal writer and full-snapshot transitions.</summary>
    internal bool TryFlush(OwnershipJournal journal, JournalWriterLease writer, out string error)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(writer);
        ExecObservation? exec;
        RootObservation? root;
        bool execPersisted;
        bool rootPersisted;
        lock (_gate)
        {
            if (!_durableCommitted)
            {
                error = string.Empty;
                return true;
            }

            exec = _exec;
            root = _root;
            execPersisted = _execPersisted;
            rootPersisted = _rootPersisted;
        }

        error = string.Empty;
        if (exec is not null && !execPersisted)
        {
            var write = journal.WriteMutation(writer, current =>
            {
                if (!OwnershipTransitions.TryPlanExecObservation(current, exec, out var next, out var failure))
                {
                    throw new InvalidOperationException(failure);
                }

                return next!;
            });
            if (!write.Ok)
            {
                error = $"exec observation persistence failed: {write.Error}";
                return false;
            }

            lock (_gate)
            {
                _execPersisted = true;
            }
        }

        if (root is not null && !rootPersisted)
        {
            var write = journal.WriteMutation(writer, current =>
            {
                if (!OwnershipTransitions.TryPlanRootObservation(current, root, out var next, out var failure))
                {
                    throw new InvalidOperationException(failure);
                }

                return next!;
            });
            if (!write.Ok)
            {
                error = $"root observation persistence failed: {write.Error}";
                return false;
            }

            lock (_gate)
            {
                _rootPersisted = true;
            }
        }

        return true;
    }
}
