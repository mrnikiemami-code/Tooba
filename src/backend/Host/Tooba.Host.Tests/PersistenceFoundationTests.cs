using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.PlatformProbe.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class PersistenceFoundationTests
{
    [Fact]
    public void Tenant_a_and_b_contexts_use_distinct_connection_strings()
    {
        var a = CreateContext("Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_alpha");
        var b = CreateContext("Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_bravo");
        Assert.Contains("tooba_alpha", a.Database.GetConnectionString(), StringComparison.Ordinal);
        Assert.Contains("tooba_bravo", b.Database.GetConnectionString(), StringComparison.Ordinal);
        Assert.NotEqual(a.Database.GetConnectionString(), b.Database.GetConnectionString());
        Assert.NotSame(a, b);
        a.Dispose();
        b.Dispose();
    }

    [Fact]
    public void Marketplace_connection_is_distinct_from_store_connections()
    {
        var marketplace = CreateContext("Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_marketplace");
        var store = CreateContext("Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_alpha");
        Assert.Contains("tooba_marketplace", marketplace.Database.GetConnectionString(), StringComparison.Ordinal);
        Assert.DoesNotContain("tooba_alpha", marketplace.Database.GetConnectionString(), StringComparison.Ordinal);
        marketplace.Dispose();
        store.Dispose();
    }

    [Fact]
    public void Missing_connection_reference_fails_closed()
    {
        var resolver = new DatabaseConnectionResolver(Microsoft.Extensions.Options.Options.Create(new ToobaPlatformOptions
        {
            PostgreSQL = new PostgreSqlOptions(),
        }));

        var ex = Assert.Throws<PlatformHttpException>(() => resolver.Resolve(new ConnectionReference("missing")));
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("platform.connection.unconfigured", ex.ErrorCode);
    }

    [Fact]
    public void No_global_mega_dbcontext_type_exists()
    {
        var forbidden = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .Where(t => t.Name is "ToobaDbContext" or "AppDbContext")
            .ToArray();
        Assert.Empty(forbidden);
    }

    [Fact]
    public void Platform_probe_owns_dedicated_schema()
    {
        Assert.Equal("platform_probe", PlatformProbeDbContext.Schema);
    }

    [Fact]
    public void Uuid_v7_is_version_seven()
    {
        var id = UuidV7.New();
        Assert.Equal(7, (id.ToByteArray(bigEndian: true)[6] & 0xF0) >> 4);
    }

    private static PlatformProbeDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PlatformProbeDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            PlatformProbeDbContext.Schema,
            typeof(PlatformProbeDbContext));
        return new PlatformProbeDbContext(options.Options);
    }
}

internal static class PersistenceFoundationTestsHelpers
{
    public static PlatformProbeDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PlatformProbeDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            PlatformProbeDbContext.Schema,
            typeof(PlatformProbeDbContext));
        return new PlatformProbeDbContext(options.Options);
    }
}
