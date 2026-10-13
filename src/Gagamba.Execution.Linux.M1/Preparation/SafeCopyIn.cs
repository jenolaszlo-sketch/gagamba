// WP02 protected copy-in engine: descriptor-relative acquisition,
// exclusive staging, bounded descriptor-to-descriptor copy, digesting,
// read-only reopen verification and immutable object binding. The engine
// never touches pathnames, descriptors or syscalls directly: every
// filesystem act goes through the injected ICopyHost, so deterministic
// portable tests substitute faults while the production path uses libc.
// Single-threaded; one owner; no launch, no transport, no recovery.
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Gagamba.Execution.Linux.M1.Preparation;

/// <summary>Failure classes for copy/prepare refusals. They map native
/// and validation failures into truthful WP01-style outcomes.</summary>
internal enum CopyFailureKind : byte
{
    None = 0,
    Unsupported = 1,
    InvalidSource = 2,
    ResolutionRefused = 3,
    SymlinkRefused = 4,
    BoundsExceeded = 5,
    ObjectReplaced = 6,
    DigestMismatch = 7,
    StagingFailure = 8,
    PublicationFailure = 9,
    Cancelled = 10,
    InvariantFailure = 11,
    SparseRefused = 12,
}

/// <summary>One object to stage: logical role, single-component source
/// name under the source root, per-object byte budget and an optional
/// caller-claimed digest that the copy must match.</summary>
internal sealed record CopyObjectPlan(
    string Role,
    string SourceName,
    ulong MaxBytes,
    byte[]? ClaimedDigest);

/// <summary>Deterministic test seam. Invoked synchronously by the engine:
/// OnChunkCopied after each copied chunk, OnBeforeReopen after the
/// writable destination closes and before the read-only reopen. Null by
/// default; production never sets them.</summary>
internal sealed record CopyTestHooks(Action<int>? OnChunkCopied, Action<string>? OnBeforeReopen);

/// <summary>Simulated actor death for deterministic recovery barriers.
/// Thrown by test hooks to abandon preparation immediately without any
/// failure cleanup, modeling a dead owner: staged files stay on disk,
/// durable records stay as written, and only independently retained
/// authority plus durable state may be used afterward. Never thrown by
/// production paths; the engine lets it propagate instead of converting
/// it into a normal failure.</summary>
internal sealed class BarrierDeathException : Exception
{
    internal BarrierDeathException(string barrier)
        : base($"simulated death at barrier '{barrier}'")
    {
        Barrier = barrier;
    }

    internal string Barrier { get; }
}

/// <summary>Immutable preparation request. All collections are copied on
/// use; mutating caller arrays afterwards cannot change preparation.
/// OnObjectAccounted, when set, is invoked synchronously after each
/// successful object bind with the staging directory handle and the bound
/// object, so recovery accounting can durably record staged allocations
/// incrementally. It must not take ownership of either handle.</summary>
internal sealed record CopyPlan(
    string SourceRootPath,
    string StagingParentPath,
    string StagingDirectoryName,
    IReadOnlyList<CopyObjectPlan> Objects,
    ulong TotalBudgetBytes,
    int MaxObjects,
    IReadOnlyList<KeyValuePair<string, string>>? Environment,
    Func<CancellationToken, bool>? Acknowledge,
    CopyTestHooks? Hooks,
    Action<SafeHandle, StagedObject>? OnObjectAccounted = null,
    Action<SafeHandle>? OnStagingDirectoryCreated = null)
{
    internal const ulong EnvironmentBudgetBytes = 16384;
    internal const int CopyChunkBytes = 65536;
}

/// <summary>Prepare outcome. A null closure means nothing is retained;
/// a non-null closure on a failed outcome carries accounted staged
/// objects for explicit cleanup/reconciliation, never a capability.
/// <see cref="Cleanup"/> reports identity-checked partial-cleanup results
/// (G2-07); a non-confirmed report retains cleanup debt.</summary>
internal sealed record PreparationResult(
    bool Succeeded,
    CopyFailureKind Failure,
    string Detail,
    PreparedClosure? Closure,
    StagingCleanupReport? Cleanup = null);

/// <summary>Protected copy-in engine. Implements the Section 7
/// acquisition/copy/bind sequence with partial-vs-accounted cleanup.</summary>
internal static class SafeCopyIn
{
    internal static PreparationResult Prepare(
        CopyPlan plan,
        ICopyHost host,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(host);

        var environment = ValidateEnvironment(plan, out var environmentFailure);
        if (environment is null)
        {
            return Fail(environmentFailure, "Environment snapshot is not acceptable for preparation.", null);
        }

        if (plan.Objects.Count == 0 || plan.Objects.Count > plan.MaxObjects)
        {
            return Fail(CopyFailureKind.BoundsExceeded, "Object count is outside the allowed range.", null);
        }

        // Fail fast before any allocation: every object name and role is
        // validated up front so a refused request never creates staging.
        foreach (var request in plan.Objects)
        {
            if (!IsSingleComponentName(request.SourceName) || !IsRoleName(request.Role))
            {
                return Fail(CopyFailureKind.InvalidSource, "Source name or role is not acceptable.", null);
            }

            if (request.Role is not ("tool" or "input"))
            {
                return Fail(CopyFailureKind.InvalidSource, $"Role '{request.Role}' is not a WP02-staged role.", null);
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Fail(CopyFailureKind.Cancelled, "Cancelled before any allocation.", null);
        }

        SafeHandle? sourceRoot = null;
        SafeHandle? stagingParent = null;
        SafeHandle? stagingDir = null;
        var created = new List<CreatedEntry>();
        var bound = new List<StagedObject>();
        ulong totalBytes = 0;
        try
        {
            var opened = OpenAuthority(
                host, plan.SourceRootPath, plan.StagingParentPath, plan.StagingDirectoryName,
                out sourceRoot, out stagingParent, out stagingDir);
            if (opened is not null)
            {
                return opened;
            }

            // G2-03: the staging directory now exists. Recovery ownership
            // must be established here, before any dependent object copy,
            // so a death between creation and the first completed object
            // still leaves retained recovery authority plus a durable
            // identity. A refusal from this callback (or a barrier death)
            // aborts preparation before any object content is copied.
            plan.OnStagingDirectoryCreated?.Invoke(stagingDir!);

            for (var index = 0; index < plan.Objects.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var staged = CopyOneObject(
                    host, sourceRoot!, stagingDir!, plan.Objects[index], index,
                    plan.TotalBudgetBytes, ref totalBytes,
                    plan.Hooks, plan.OnObjectAccounted, cancellationToken, created);
                if (!staged.Succeeded)
                {
                    return FinishFailed(host, ref stagingDir, created, bound, totalBytes, environment, staged.Failure, staged.Detail);
                }

                bound.Add(staged.Bound!);
            }

            var syncErrno = host.SyncDirectory(stagingDir!);
            if (syncErrno != 0)
            {
                return FinishFailed(host, ref stagingDir, created, bound, totalBytes, environment,
                    CopyFailureKind.StagingFailure, $"Staging directory sync failed with errno {syncErrno}.");
            }

            var closure = new PreparedClosure(bound, stagingDir, totalBytes, environment);
            stagingDir = null;
            sourceRoot?.Dispose();
            stagingParent?.Dispose();
            sourceRoot = null;
            stagingParent = null;
            return new PreparationResult(true, CopyFailureKind.None, "Preparation staged and bound.", closure);
        }
        catch (OperationCanceledException)
        {
            return FinishFailed(host, ref stagingDir, created, bound, totalBytes, environment,
                CopyFailureKind.Cancelled, "Cancelled during preparation.");
        }
        catch (BarrierDeathException)
        {
            // Simulated actor death for deterministic recovery barriers:
            // propagate without any failure cleanup so durable state plus
            // independently retained authority is all recovery may use.
            throw;
        }
        catch (Exception ex)
        {
            return FinishFailed(host, ref stagingDir, created, bound, totalBytes, environment,
                CopyFailureKind.InvariantFailure, $"Internal preparation failure: {ex.GetType().Name}.");
        }
        finally
        {
            sourceRoot?.Dispose();
            stagingParent?.Dispose();
            stagingDir?.Dispose();
        }
    }

    private static PreparationResult Fail(
        CopyFailureKind failure, string detail, PreparedClosure? retained, StagingCleanupReport? cleanup = null) =>
        new(false, failure, detail, retained, cleanup);

    /// <summary>A staging entry created by this preparation, with the exact
    /// creation identity captured while its writable FD was open.</summary>
    private readonly record struct CreatedEntry(string Name, ulong Device, ulong Inode, bool IdentityKnown);

    private static PreparationResult FinishFailed(
        ICopyHost host,
        ref SafeHandle? stagingDir,
        List<CreatedEntry> created,
        List<StagedObject> bound,
        ulong totalBytes,
        IReadOnlyList<KeyValuePair<string, string>> environment,
        CopyFailureKind failure,
        string detail)
    {
        // Partial (never bound) objects are removed only through the
        // retained staging directory and only when their current entry
        // still matches the captured creation identity (G2-07). Bound
        // objects are accounted: the staging directory transfers into a
        // retained closure for explicit cleanup, never silent deletion.
        var boundNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var staged in bound)
        {
            boundNames.Add(staged.StagingName);
        }

        var partialResults = new List<CleanupItemResult>();
        if (stagingDir is not null)
        {
            foreach (var entry in created)
            {
                if (boundNames.Contains(entry.Name))
                {
                    continue;
                }

                if (!entry.IdentityKnown)
                {
                    partialResults.Add(new CleanupItemResult(
                        CleanupItemOutcome.InspectionFailed, entry.Name, "creation identity was not captured"));
                    continue;
                }

                partialResults.Add(StagingCleanup.CleanOne(host, stagingDir, entry.Name, entry.Device, entry.Inode));
            }
        }

        var report = new StagingCleanupReport(partialResults);

        if (bound.Count == 0)
        {
            return Fail(failure, detail, null, report);
        }

        var retained = new PreparedClosure(bound, stagingDir, totalBytes, environment);
        stagingDir = null;
        return Fail(failure, detail, retained, report);
    }

    private static PreparationResult? OpenAuthority(
        ICopyHost host,
        string sourceRootPath,
        string stagingParentPath,
        string stagingDirectoryName,
        out SafeHandle? sourceRoot,
        out SafeHandle? stagingParent,
        out SafeHandle? stagingDir)
    {
        sourceRoot = null;
        stagingParent = null;
        stagingDir = null;

        var errno = host.OpenDirectory(sourceRootPath, out var root);
        if (errno != 0 || root is null)
        {
            return Fail(MapBootstrapErrno(errno), $"Source-root bootstrap failed with errno {errno}.", null);
        }

        sourceRoot = root;
        if (host.StatHandle(sourceRoot, out var rootIdentity) != 0
            || !Native.PreparationInterop.IsDirectory(rootIdentity.Mode))
        {
            return Fail(CopyFailureKind.InvalidSource, "Source root is not a directory.", null);
        }

        errno = host.OpenDirectory(stagingParentPath, out var parent);
        if (errno != 0 || parent is null)
        {
            return Fail(MapBootstrapErrno(errno), $"Staging-parent bootstrap failed with errno {errno}.", null);
        }

        stagingParent = parent;
        if (host.StatHandle(stagingParent, out var parentIdentity) != 0
            || !Native.PreparationInterop.IsDirectory(parentIdentity.Mode))
        {
            return Fail(CopyFailureKind.StagingFailure, "Staging parent is not a directory.", null);
        }

        if (!IsSingleComponentName(stagingDirectoryName))
        {
            return Fail(CopyFailureKind.StagingFailure, "Staging directory name is not a single-component name.", null);
        }

        errno = host.MakeDirectory(stagingParent, stagingDirectoryName, Native.PreparationInterop.Mode0700);
        if (errno != 0)
        {
            return Fail(CopyFailureKind.StagingFailure, $"Staging directory creation failed with errno {errno}.", null);
        }

        errno = host.OpenSubdirectory(stagingParent, stagingDirectoryName, out var subdir);
        if (errno != 0 || subdir is null)
        {
            host.Unlink(stagingParent, stagingDirectoryName);
            return Fail(CopyFailureKind.StagingFailure, $"Staging directory open failed with errno {errno}.", null);
        }

        stagingDir = subdir;
        return null;
    }

    private static CopyFailureKind MapBootstrapErrno(int errno) =>
        errno switch
        {
            Native.PreparationInterop.ENOSYS => CopyFailureKind.Unsupported,
            Native.PreparationInterop.ELOOP => CopyFailureKind.SymlinkRefused,
            _ => CopyFailureKind.InvalidSource,
        };

    private sealed record ObjectStage(bool Succeeded, CopyFailureKind Failure, string Detail, StagedObject? Bound);

    private static ObjectStage CopyOneObject(
        ICopyHost host,
        SafeHandle sourceRoot,
        SafeHandle stagingDir,
        CopyObjectPlan request,
        int index,
        ulong totalBudget,
        ref ulong totalBytes,
        CopyTestHooks? hooks,
        Action<SafeHandle, StagedObject>? onObjectAccounted,
        CancellationToken cancellationToken,
        List<CreatedEntry> created)
    {
        if (!IsSingleComponentName(request.SourceName) || !IsRoleName(request.Role))
        {
            return new ObjectStage(false, CopyFailureKind.InvalidSource, "Source name or role is not acceptable.", null);
        }

        var finalMode = request.Role switch
        {
            "tool" => Native.PreparationInterop.Mode0500,
            "input" => Native.PreparationInterop.Mode0400,
            _ => 0u,
        };
        if (finalMode == 0)
        {
            return new ObjectStage(false, CopyFailureKind.InvalidSource, $"Role '{request.Role}' is not a WP02-staged role.", null);
        }

        var errno = host.OpenSource(sourceRoot, request.SourceName, out var source);
        if (errno != 0 || source is null)
        {
            return new ObjectStage(false, MapOpenErrno(errno), $"Source open failed with errno {errno}.", null);
        }

        using (source)
        {
            if (host.StatHandle(source, out var preIdentity) != 0)
            {
                return new ObjectStage(false, CopyFailureKind.InvalidSource, "Source stat failed.", null);
            }

            if (!Native.PreparationInterop.IsRegularFile(preIdentity.Mode))
            {
                return new ObjectStage(false, CopyFailureKind.InvalidSource, "Source is not a regular file.", null);
            }

            // Plan Section 7: reject sparse objects instead of allocating
            // from attacker-controlled lengths. Streamed-byte accounting
            // stays authoritative for budgets; this is the separate
            // admission check on filesystem allocation facts.
            if (!HasDenseAccounting(preIdentity, out var densityReason))
            {
                return new ObjectStage(false, CopyFailureKind.SparseRefused, $"Source block accounting is not dense: {densityReason}.", null);
            }

            if (preIdentity.Size < 0 || (ulong)preIdentity.Size > request.MaxBytes)
            {
                return new ObjectStage(false, CopyFailureKind.BoundsExceeded, "Source exceeds the per-object budget.", null);
            }

            var stagingName = $"{request.Role}-{index:D3}.staged";
            errno = host.CreateStaging(stagingDir, stagingName, out var destination);
            if (errno != 0 || destination is null)
            {
                return new ObjectStage(false, CopyFailureKind.StagingFailure, $"Staging create failed with errno {errno}.", null);
            }

            created.Add(new CreatedEntry(stagingName, 0, 0, IdentityKnown: false));
            Native.FileIdentity createdIdentity = default;
            byte[] digest = Array.Empty<byte>();
            ulong count = 0;
            using (destination)
            {
                if (host.StatHandle(destination, out createdIdentity) != 0)
                {
                    return new ObjectStage(false, CopyFailureKind.StagingFailure, "Staging stat failed.", null);
                }

                created[^1] = new CreatedEntry(stagingName, createdIdentity.Device, createdIdentity.Inode, IdentityKnown: true);

                CopyFailureKind streamFailure;
                string streamDetail;
                var copied = CopyStreamed(
                    host, source, destination, request.MaxBytes,
                    totalBudget, ref totalBytes, hooks, cancellationToken,
                    out digest, out count, out streamFailure, out streamDetail);
                if (!copied)
                {
                    return new ObjectStage(false, streamFailure, streamDetail, null);
                }

                if (host.StatHandle(source, out var postIdentity) != 0
                    || postIdentity.Size != preIdentity.Size
                    || postIdentity.Size != (long)count)
                {
                    return new ObjectStage(false, CopyFailureKind.ObjectReplaced, "Source changed during copy.", null);
                }

                if (host.StatHandle(destination, out var postDestIdentity) != 0)
                {
                    return new ObjectStage(false, CopyFailureKind.StagingFailure, "Staging stat failed after copy.", null);
                }

                if (!HasDenseAccounting(postDestIdentity, out var destDensityReason))
                {
                    return new ObjectStage(false, CopyFailureKind.SparseRefused, $"Staged block accounting is not dense: {destDensityReason}.", null);
                }

                if (request.ClaimedDigest is not null && !digest.SequenceEqual(request.ClaimedDigest))
                {
                    return new ObjectStage(false, CopyFailureKind.DigestMismatch, "Claimed digest does not match copied bytes.", null);
                }

                if (host.SetMode(destination, finalMode) != 0)
                {
                    return new ObjectStage(false, CopyFailureKind.StagingFailure, "Staging fchmod failed.", null);
                }

                if (host.SyncFile(destination) != 0)
                {
                    return new ObjectStage(false, CopyFailureKind.StagingFailure, "Staging fsync failed.", null);
                }
            }

            hooks?.OnBeforeReopen?.Invoke(stagingName);
            var bound = BindReopened(host, stagingDir, stagingName, request.Role, digest, count, createdIdentity, finalMode);
            if (bound.Succeeded && bound.Bound is not null && onObjectAccounted is not null)
            {
                // Recovery accounting hook: runs after the bind, before the
                // object joins the bound list, so an accounting failure
                // treats the object as partial (synchronously cleaned via
                // FinishFailed) rather than accounted. BarrierDeath and
                // cancellation propagate for death barriers.
                try
                {
                    onObjectAccounted(stagingDir, bound.Bound);
                }
                catch (BarrierDeathException)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    return new ObjectStage(false, CopyFailureKind.PublicationFailure, $"Object accounting failed: {ex.GetType().Name}.", null);
                }
            }

            return bound;
        }
    }

    private static bool CopyStreamed(
        ICopyHost host,
        SafeHandle source,
        SafeHandle destination,
        ulong maxBytes,
        ulong totalBudget,
        ref ulong totalBytes,
        CopyTestHooks? hooks,
        CancellationToken cancellationToken,
        out byte[] digest,
        out ulong count,
        out CopyFailureKind failure,
        out string detail)
    {
        digest = Array.Empty<byte>();
        count = 0;
        failure = CopyFailureKind.None;
        detail = string.Empty;
        var buffer = new byte[CopyPlan.CopyChunkBytes];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var chunkIndex = 0;
        try
        {
            using var reader = host.OpenRead(source);
            using var writer = host.OpenWrite(destination);
            int read;
            while (true)
            {
                try
                {
                    read = reader.Read(buffer, 0, buffer.Length);
                }
                catch (IOException)
                {
                    failure = CopyFailureKind.InvalidSource;
                    detail = "Source read failed.";
                    return false;
                }

                if (read <= 0)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                // Budgets are enforced on streamed bytes before the first
                // excess write; st_size is never trusted for growth.
                if ((ulong)read > maxBytes - count || (ulong)read > totalBudget - totalBytes)
                {
                    failure = CopyFailureKind.BoundsExceeded;
                    detail = "Copy exceeds the object or total staged-content budget.";
                    return false;
                }

                try
                {
                    writer.Write(buffer, 0, read);
                }
                catch (IOException)
                {
                    failure = CopyFailureKind.StagingFailure;
                    detail = "Staging write failed.";
                    return false;
                }

                hash.AppendData(buffer, 0, read);
                count += (ulong)read;
                totalBytes += (ulong)read;
                hooks?.OnChunkCopied?.Invoke(chunkIndex);
                chunkIndex++;
            }

            digest = hash.GetHashAndReset();
            return true;
        }
        catch (OperationCanceledException)
        {
            failure = CopyFailureKind.Cancelled;
            detail = "Cancelled during copy.";
            return false;
        }
    }

    private static ObjectStage BindReopened(
        ICopyHost host,
        SafeHandle stagingDir,
        string stagingName,
        string role,
        byte[] digest,
        ulong count,
        Native.FileIdentity createdIdentity,
        uint finalMode)
    {
        var errno = host.OpenSource(stagingDir, stagingName, out var reopened);
        if (errno != 0 || reopened is null)
        {
            return new ObjectStage(false, CopyFailureKind.ObjectReplaced, $"Staging reopen failed with errno {errno}.", null);
        }

        try
        {
            if (host.StatHandle(reopened, out var identity) != 0
                || !Native.PreparationInterop.IsRegularFile(identity.Mode)
                || identity.Device != createdIdentity.Device
                || identity.Inode != createdIdentity.Inode
                || (identity.Mode & 0x1FFu) != finalMode
                || identity.Size != (long)count
                || identity.LinkCount != 1
                || !HasDenseAccounting(identity, out _))
            {
                return new ObjectStage(false, CopyFailureKind.ObjectReplaced, "Reopened staging identity does not match the created object.", null);
            }

            using var reader = host.OpenRead(reopened);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[CopyPlan.CopyChunkBytes];
            int read;
            try
            {
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    hash.AppendData(buffer, 0, read);
                }
            }
            catch (IOException)
            {
                return new ObjectStage(false, CopyFailureKind.StagingFailure, "Reopened staging rehash read failed.", null);
            }

            if (!hash.GetHashAndReset().SequenceEqual(digest))
            {
                return new ObjectStage(false, CopyFailureKind.ObjectReplaced, "Reopened staging digest does not match the copied digest.", null);
            }

            var staged = new StagedObject(
                role, (byte[])digest.Clone(), count,
                identity.Device, identity.Inode, identity.Mode,
                stagingName, reopened);
            reopened = null;
            return new ObjectStage(true, CopyFailureKind.None, "Object bound.", staged);
        }
        finally
        {
            reopened?.Dispose();
        }
    }

    private static IReadOnlyList<KeyValuePair<string, string>>? ValidateEnvironment(
        CopyPlan plan, out CopyFailureKind failure)
    {
        failure = CopyFailureKind.None;
        if (plan.Environment is null)
        {
            return Array.Empty<KeyValuePair<string, string>>();
        }

        ulong total = 0;
        var snapshot = new List<KeyValuePair<string, string>>(plan.Environment.Count);
        foreach (var pair in plan.Environment)
        {
            if (pair.Key.Contains('\0') || pair.Value.Contains('\0'))
            {
                failure = CopyFailureKind.InvalidSource;
                return null;
            }

            total += (ulong)Encoding.UTF8.GetByteCount(pair.Key)
                + (ulong)Encoding.UTF8.GetByteCount(pair.Value);
            if (total > CopyPlan.EnvironmentBudgetBytes)
            {
                failure = CopyFailureKind.BoundsExceeded;
                return null;
            }

            snapshot.Add(pair);
        }

        return snapshot;
    }

    internal static bool IsSingleComponentName(string name)
    {
        if (name.Length == 0 || name.Length > 255 || name.Contains('\0') || name.Contains('/'))
        {
            return false;
        }

        return name != "." && name != "..";
    }

    internal static bool IsRoleName(string role)
    {
        if (role.Length == 0 || role.Length > 32)
        {
            return false;
        }

        foreach (var c in role)
        {
            if (!(c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static CopyFailureKind MapOpenErrno(int errno) =>
        errno switch
        {
            Native.PreparationInterop.ENOSYS => CopyFailureKind.Unsupported,
            Native.PreparationInterop.ELOOP => CopyFailureKind.SymlinkRefused,
            Native.PreparationInterop.EINVAL => CopyFailureKind.ResolutionRefused,
            _ => CopyFailureKind.InvalidSource,
        };

    /// <summary>Fail-closed density admission: st_blocks counts 512-byte
    /// units, so fewer allocated bytes than the logical size means a
    /// sparse object. Absent block accounting for a non-empty file cannot
    /// prove density and is refused rather than trusted. Empty files are
    /// trivially dense.</summary>
    internal static bool HasDenseAccounting(Native.FileIdentity identity, out string reason)
    {
        reason = string.Empty;
        if (identity.Size < 0)
        {
            reason = "negative logical size";
            return false;
        }

        if (identity.Size == 0)
        {
            return true;
        }

        if (identity.Blocks512 <= 0)
        {
            reason = "block accounting absent for a non-empty file";
            return false;
        }

        if ((ulong)identity.Blocks512 > ((ulong)long.MaxValue / 512UL))
        {
            return true;
        }

        if ((ulong)identity.Blocks512 * 512UL < (ulong)identity.Size)
        {
            reason = "fewer bytes allocated than the logical size";
            return false;
        }

        return true;
    }
}
