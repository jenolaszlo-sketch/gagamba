// WP03 owner-bootstrap tests: exact versioned role/count allowlist.
using Gagamba.Execution.Linux.M1.Native;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class OwnerBootstrapTests
{
    private static BootstrapMap Valid() => new(
        OwnerBootstrap.CurrentVersion,
        new[]
        {
            new BootstrapDescriptor(0, BootstrapRole.ControlRead),
            new BootstrapDescriptor(1, BootstrapRole.ControlWrite),
            new BootstrapDescriptor(2, BootstrapRole.RecoveryEvidence),
        });

    [Fact]
    public void ValidMapAccepted()
    {
        Assert.True(OwnerBootstrap.TryValidate(Valid(), out var error), error);
    }

    [Fact]
    public void WrongVersionRefused()
    {
        var map = Valid() with { Version = 99 };
        Assert.False(OwnerBootstrap.TryValidate(map, out var error));
        Assert.Contains("version", error);
    }

    [Fact]
    public void MissingRoleRefused()
    {
        var map = new BootstrapMap(
            OwnerBootstrap.CurrentVersion,
            new[]
            {
                new BootstrapDescriptor(0, BootstrapRole.ControlRead),
                new BootstrapDescriptor(1, BootstrapRole.ControlWrite),
            });
        Assert.False(OwnerBootstrap.TryValidate(map, out var error));
        Assert.Contains("count", error);
    }

    [Fact]
    public void ExtraRoleRefused()
    {
        var map = new BootstrapMap(
            OwnerBootstrap.CurrentVersion,
            new[]
            {
                new BootstrapDescriptor(0, BootstrapRole.ControlRead),
                new BootstrapDescriptor(1, BootstrapRole.ControlWrite),
                new BootstrapDescriptor(2, BootstrapRole.RecoveryEvidence),
                new BootstrapDescriptor(3, BootstrapRole.RecoveryEvidence),
            });
        Assert.False(OwnerBootstrap.TryValidate(map, out _));
    }

    [Fact]
    public void DuplicateRoleRefused()
    {
        var map = new BootstrapMap(
            OwnerBootstrap.CurrentVersion,
            new[]
            {
                new BootstrapDescriptor(0, BootstrapRole.ControlRead),
                new BootstrapDescriptor(1, BootstrapRole.ControlRead),
                new BootstrapDescriptor(2, BootstrapRole.RecoveryEvidence),
            });
        Assert.False(OwnerBootstrap.TryValidate(map, out _));
    }

    [Fact]
    public void InvalidDescriptorRefused()
    {
        var map = new BootstrapMap(
            OwnerBootstrap.CurrentVersion,
            new[]
            {
                new BootstrapDescriptor(-1, BootstrapRole.ControlRead),
                new BootstrapDescriptor(1, BootstrapRole.ControlWrite),
                new BootstrapDescriptor(2, BootstrapRole.RecoveryEvidence),
            });
        Assert.False(OwnerBootstrap.TryValidate(map, out _));
    }

    [Fact]
    public void NullMapRefused()
    {
        Assert.False(OwnerBootstrap.TryValidate(null, out _));
    }
}
