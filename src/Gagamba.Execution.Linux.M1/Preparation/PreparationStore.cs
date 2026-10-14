// WP01 capability store skeleton: mints preparation capabilities and
// validates exact object identity on consume. A matching GUID/digest in a
// fabricated object is insufficient: lookup requires reference equality
// with the minted instance plus issuer, generation, digest, expiry and
// unused state.
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Preparation;

internal sealed partial class PreparedTransaction
{
    /// <summary>Internal nested store. Declared here so it can invoke the
    /// private capability constructor without any public/internal minting
    /// factory on the capability itself.</summary>
    internal sealed class PreparationStore
    {
        private readonly Guid _issuerId;
        private readonly Guid _generationId;
        private readonly Execution.ITransactionClock _clock;
        private readonly Dictionary<Guid, Entry> _entries = new();

        /// <summary>Serializes Mint against concurrent claims and against
        /// entry mutation. Lock order is always store → closure, so the
        /// atomic claim can never deadlock.</summary>
        private readonly object _gate = new();

        private sealed class Entry
        {
            public required PreparedTransaction Capability;
            public PreparedClosure? Closure;
            public bool Consumed;
            public bool Aborted;
            private bool _closureReleased;

            /// <summary>Releases store-owned closure authority exactly once.</summary>
            public void ReleaseClosure()
            {
                if (_closureReleased)
                {
                    return;
                }

                _closureReleased = true;
                Closure?.ReleaseFromStore();
                Closure = null;
            }
        }

        public PreparationStore(Guid issuerId, Guid generationId, Execution.ITransactionClock clock)
        {
            if (issuerId == Guid.Empty)
            {
                throw new ArgumentException("Issuer ID must be non-empty.", nameof(issuerId));
            }

            if (generationId == Guid.Empty)
            {
                throw new ArgumentException("Generation ID must be non-empty.", nameof(generationId));
            }

            _issuerId = issuerId;
            _generationId = generationId;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        internal Guid IssuerId => _issuerId;

        internal Guid GenerationId => _generationId;

        public PreparedTransaction Mint(
            RestrictedManifest manifest,
            PreparationIdentity identity,
            long expiresAtNs,
            PreparedClosure closure)
        {
            ArgumentNullException.ThrowIfNull(manifest);
            ArgumentNullException.ThrowIfNull(identity);
            ArgumentNullException.ThrowIfNull(closure);
            lock (_gate)
            {
                if (!identity.IssuerId.Equals(_issuerId))
                {
                    throw new ArgumentException("Identity issuer does not match this store.", nameof(identity));
                }

                if (!identity.GenerationId.Equals(_generationId))
                {
                    throw new ArgumentException("Identity generation does not match this store.", nameof(identity));
                }

                if (!identity.ManifestDigest.SequenceEqual(ManifestCodec.ComputeDigestBytes(manifest)))
                {
                    throw new ArgumentException("Identity digest does not match the manifest.", nameof(identity));
                }

                if (expiresAtNs <= _clock.NowNanoseconds)
                {
                    throw new ArgumentException("Expiry must be in the future.", nameof(identity));
                }

                if (_entries.ContainsKey(identity.PreparationId))
                {
                    throw new ArgumentException("Preparation ID already minted.", nameof(identity));
                }

                // All validation precedes the irreversible ownership
                // transition, so a refusal leaves no stranded state: the
                // closure is untouched and the existing owner (if any) is
                // unaffected. The transfer itself is synchronized on the
                // closure, so concurrent claims yield exactly one success;
                // the remaining steps cannot fail (duplicate checked above
                // under the same lock).
                if (!closure.TryTransferToStore())
                {
                    throw new ArgumentException("Closure is already claimed or released; a concrete closure backs at most one preparation.", nameof(closure));
                }

                // The store entry owns the exact verified closure for the
                // capability's lifetime; ownership transferred here exactly once.
                var capability = new PreparedTransaction(manifest, identity, expiresAtNs, this);
                _entries.Add(identity.PreparationId, new Entry { Capability = capability, Closure = closure });
                return capability;
            }
        }

        /// <summary>Borrows the store-owned closure for a live capability.
        /// Never transfers ownership; a fabricated or disposed capability
        /// gets nothing. Defense in depth: the entry's closure must still
        /// hold live authority.</summary>
        public bool TryBorrowClosure(PreparedTransaction candidate, out PreparedClosure? closure)
        {
            closure = null;
            lock (_gate)
            {
                if (_entries.TryGetValue(candidate.Identity.PreparationId, out var entry)
                    && ReferenceEquals(entry.Capability, candidate)
                    && !entry.Aborted
                    && entry.Closure is not null
                    && entry.Closure.AuthorityLive)
                {
                    closure = entry.Closure;
                    return true;
                }

                return false;
            }
        }

        public bool TryLookup(Guid preparationId, out PreparedTransaction? capability)
        {
            lock (_gate)
            {
                if (_entries.TryGetValue(preparationId, out var entry)
                    && !entry.Consumed
                    && !entry.Aborted
                    && entry.Capability.ExpiresAtNs > _clock.NowNanoseconds)
                {
                    capability = entry.Capability;
                    return true;
                }

                capability = null;
                return false;
            }
        }

        public bool TryConsume(PreparedTransaction candidate, out InvariantViolation violation)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            lock (_gate)
            {
                if (!_entries.TryGetValue(candidate.Identity.PreparationId, out var entry)
                    || !ReferenceEquals(entry.Capability, candidate))
                {
                    violation = InvariantViolation.Foreign;
                    return false;
                }

                if (entry.Aborted)
                {
                    violation = InvariantViolation.Aborted;
                    return false;
                }

                if (entry.Consumed)
                {
                    violation = InvariantViolation.Consumed;
                    return false;
                }

                if (!candidate.Identity.IssuerId.Equals(_issuerId)
                    || !candidate.Identity.GenerationId.Equals(_generationId))
                {
                    violation = InvariantViolation.Foreign;
                    return false;
                }

                if (!candidate.Identity.ManifestDigest.SequenceEqual(ManifestCodec.ComputeDigestBytes(candidate.Manifest)))
                {
                    violation = InvariantViolation.Foreign;
                    return false;
                }

                if (candidate.ExpiresAtNs <= _clock.NowNanoseconds)
                {
                    violation = InvariantViolation.Expired;
                    return false;
                }

                entry.Consumed = true;
                violation = InvariantViolation.None;
                return true;
            }
        }

        public bool Invalidate(PreparedTransaction candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            lock (_gate)
            {
                if (_entries.TryGetValue(candidate.Identity.PreparationId, out var entry)
                    && ReferenceEquals(entry.Capability, candidate))
                {
                    entry.Aborted = true;
                    entry.ReleaseClosure();
                    return true;
                }

                return false;
            }
        }

        public bool Abort(Guid preparationId)
        {
            lock (_gate)
            {
                if (_entries.TryGetValue(preparationId, out var entry))
                {
                    entry.Aborted = true;
                    entry.ReleaseClosure();
                    return true;
                }

                return false;
            }
        }
    }
}
