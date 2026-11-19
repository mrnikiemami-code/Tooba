using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-282-R8 — skip-bootstrap safe read-only admin dev-context bind (no Ensure/Register/Write).
/// </summary>
public sealed class HostAdminDevContext282R8GuardTests
{
    private static string Read(string relative)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host", "Admin"));
        return File.ReadAllText(Path.Combine(root, relative));
    }

    [Fact]
    public void TryBindExistingSnapshot_is_read_only_find_only()
    {
        var bootstrap = Read("Development/AdminDevActorBootstrap.cs");
        Assert.Contains("TryBindExistingSnapshotAsync", bootstrap, StringComparison.Ordinal);
        var bindStart = bootstrap.IndexOf("TryBindExistingSnapshotAsync", StringComparison.Ordinal);
        var ensureStart = bootstrap.IndexOf("public static async Task EnsureAsync", StringComparison.Ordinal);
        Assert.True(bindStart > 0 && ensureStart > bindStart);
        var bindSlice = bootstrap[bindStart..ensureStart];
        Assert.Contains("FindUserIdByIdentifierAsync", bindSlice, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterAsync", bindSlice, StringComparison.Ordinal);
        Assert.DoesNotContain("IAuthorizationTupleWriter", bindSlice, StringComparison.Ordinal);
        Assert.DoesNotContain("WriteAsync", bindSlice, StringComparison.Ordinal);
    }

    [Fact]
    public void Dev_context_endpoint_binds_existing_snapshot_before_unavailable()
    {
        var endpoints = Read("Development/AdminDevContextEndpoints.cs");
        Assert.Contains("TryBindExistingSnapshotAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("IsDevelopment()", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureAsync", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterAsync", endpoints, StringComparison.Ordinal);
    }
}
