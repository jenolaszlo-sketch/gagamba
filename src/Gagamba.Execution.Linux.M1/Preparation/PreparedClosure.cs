// WP02 bound-object representation and atomic publication. A StagedObject
// binds semantic content identity (SHA-256, length, role) to filesystem
// object identity (device, inode) and live read-only descriptor
// ownership. Publication verifies staged bindings against the manifest,
// requires durable acknowledgement, then mints through the WP01 store:
// Mint remains the atomic capability-publication boundary. No capability
// exists before acknowledgement; accounted objects are retained, never
// silently deleted, when publication fails.
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Preparation;

/// <summary>One immutably bound staged object. A sealed class (never a
/// record): handle identity must not participate in value equality.</summary>
internal sealed class StagedObject
{
    public StagedObject(
        string role,
        byte[] sha256,
        ulong length,
        ulong device,
        ulong inode,
        uint mode,
        string stagingName,
        SafeHandle handle)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);
        ArgumentNullException.ThrowIfNull(sha256);
        ArgumentException.ThrowIfNullOrEmpty(stagingName);
        ArgumentNullException.ThrowIfNull(handle);
        if (sha256.Length != 32)
        {
            throw new ArgumentException("Staged digests must be exactly 32 bytes.", nameof(sha256));
        }

        Role = role;
        Sha256 = (byte[])sha256.Clone();
        Length = length;
        Device = device;
        Inode = inode;
        Mode = mode;
        StagingName = stagingName;
        Handle = handle;
    }

    public string Role { get; }

    public byte[] Sha256 { get; }

    public ulong Length { get; }

    public ulong Device { get; }

    public ulong Inode { get; }

    public uint Mode { get; }

    public string StagingName { get; }

    public SafeHandle Handle { get; }
}

/// <summary>Accounted staged allocation: bound objects plus the retained
/// staging-directory handle that keeps descriptor-relative cleanup
/// possible. Disposal closes every retained descriptor.</summary>
internal sealed class PreparedClosure : IDisposable
{
    private bool _disposed;

    public PreparedClosure(
        IReadOnlyList<StagedObject> objects,
        SafeHandle? stagingDirectory,
        ulong totalBytes,
        IReadOnlyList<KeyValuePair<string, string>> environment)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(environment);
        Objects = objects;
        StagingDirectory = stagingDirectory;
        TotalBytes = totalBytes;
        Environment = environment;
    }

    public IReadOnlyList<StagedObject> Objects { get; }

    public SafeHandle? StagingDirectory { get; }

    public ulong TotalBytes { get; }

    public IReadOnlyList<KeyValuePair<string, string>> Environment { get; }

    /// <summary>Explicit accounted cleanup: unlinks every staged object
    /// through the retained staging directory, then disposes. Never
    /// invoked implicitly by the engine on accounted objects.</summary>
    public void DiscardAccounted(ICopyHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (StagingDirectory is not null)
        {
            foreach (var staged in Objects)
            {
                host.Unlink(StagingDirectory, staged.StagingName);
            }
        }

        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var staged in Objects)
        {
            staged.Handle.Dispose();
        }

        StagingDirectory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Publication outcome. A null capability means none exists;
/// a non-null closure on failure carries accounted objects for explicit
/// cleanup, never a usable capability.</summary>
internal sealed record PublicationResult(
    bool Published,
    CopyFailureKind Failure,
    string Detail,
    PreparedTransaction? Capability,
    PreparedClosure Closure);

/// <summary>Atomic publication through the WP01 store: binding check,
/// durable acknowledgement, then Mint. No early publication.</summary>
internal static class PreparationPublisher
{
    internal static PublicationResult Publish(
        PreparedTransaction.PreparationStore store,
        PreparedClosure closure,
        RestrictedManifest manifest,
        PreparationIdentity identity,
        long expiresAtNs,
        Func<CancellationToken, bool> acknowledge,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(closure);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(acknowledge);

        if (cancellationToken.IsCancellationRequested)
        {
            return new PublicationResult(false, CopyFailureKind.Cancelled, "Cancelled before publication.", null, closure);
        }

        // Binding check first: every staged object must appear in the
        // manifest closure with equal digest and length, and every
        // manifest entry must have a staged object. A fabricated or
        // foreign manifest cannot substitute staged bindings.
        var stagedByRole = new Dictionary<string, StagedObject>(StringComparer.Ordinal);
        foreach (var staged in closure.Objects)
        {
            stagedByRole[staged.Role] = staged;
        }

        if (manifest.Closure.Objects.Length != closure.Objects.Count)
        {
            return new PublicationResult(false, CopyFailureKind.PublicationFailure, "Manifest closure does not match staged bindings.", null, closure);
        }

        foreach (var bound in manifest.Closure.Objects)
        {
            if (!stagedByRole.TryGetValue(bound.Role, out var staged)
                || !staged.Sha256.SequenceEqual(bound.Sha256.ToArray())
                || staged.Length != bound.Length)
            {
                return new PublicationResult(false, CopyFailureKind.PublicationFailure, "Manifest closure does not match staged bindings.", null, closure);
            }
        }

        bool acknowledged;
        try
        {
            acknowledged = acknowledge(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new PublicationResult(false, CopyFailureKind.Cancelled, "Cancelled during acknowledgement.", null, closure);
        }
        catch (Exception ex)
        {
            return new PublicationResult(false, CopyFailureKind.PublicationFailure, $"Durable acknowledgement failed: {ex.GetType().Name}.", null, closure);
        }

        if (!acknowledged)
        {
            return new PublicationResult(false, CopyFailureKind.PublicationFailure, "Durable acknowledgement refused.", null, closure);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new PublicationResult(false, CopyFailureKind.Cancelled, "Cancelled after acknowledgement; no late publication.", null, closure);
        }

        try
        {
            var capability = store.Mint(manifest, identity, expiresAtNs);
            return new PublicationResult(true, CopyFailureKind.None, "Preparation published.", capability, closure);
        }
        catch (OperationCanceledException)
        {
            return new PublicationResult(false, CopyFailureKind.Cancelled, "Cancelled during mint.", null, closure);
        }
        catch (Exception ex)
        {
            return new PublicationResult(false, CopyFailureKind.PublicationFailure, $"Mint failed: {ex.GetType().Name}; staged objects remain accounted.", null, closure);
        }
    }
}
