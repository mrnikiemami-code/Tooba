using Tooba.BuildingBlocks;
using Tooba.Host.Configuration;
using Tooba.Host.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT — focused resolver fail-closed / secret-safety behavior.
/// </summary>
public sealed class PersistenceAmcCertBehaviorTests
{
    private const string ValidCs = "Host=127.0.0.1;Username=tooba;Password=secret-token;Database=tooba_alpha";

    [Fact]
    public void Valid_reference_returns_exact_configured_string_unchanged()
    {
        var resolver = Create(("tenant-alpha", ValidCs));
        var result = resolver.Resolve(new ConnectionReference("tenant-alpha"));
        Assert.Equal(ValidCs, result);
        Assert.Same(ValidCs, result);
    }

    [Fact]
    public void Blank_reference_fails_closed_without_leaking_secrets()
    {
        var resolver = Create(("tenant-alpha", ValidCs));
        AssertFailClosed(resolver, new ConnectionReference("   "), "tenant-alpha", ValidCs);
    }

    [Fact]
    public void Missing_reference_fails_closed_without_leaking_secrets()
    {
        var resolver = Create(("tenant-alpha", ValidCs));
        AssertFailClosed(resolver, new ConnectionReference("missing-ref"), "missing-ref", ValidCs);
    }

    [Fact]
    public void Blank_configured_value_fails_closed_without_leaking_secrets()
    {
        var resolver = Create(("blank-ref", "   "));
        AssertFailClosed(resolver, new ConnectionReference("blank-ref"), "blank-ref", ValidCs);
    }

    [Fact]
    public void Malformed_connection_string_fails_closed_without_parser_leakage()
    {
        const string malformed = "Host=127.0.0.1;Port=not-a-number;Password=secret-token";
        var resolver = Create(("bad-ref", malformed));
        var ex = AssertFailClosed(resolver, new ConnectionReference("bad-ref"), "bad-ref", malformed);
        Assert.DoesNotContain("not-a-number", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("not-a-number", ex.Title, StringComparison.Ordinal);
    }

    [Fact]
    public void Case_insensitive_reference_lookup_resolves()
    {
        var resolver = Create(("Tenant-Alpha", ValidCs));
        Assert.Equal(ValidCs, resolver.Resolve(new ConnectionReference("tenant-alpha")));
    }

    [Fact]
    public void Missing_reference_fail_closed_does_not_leak_secrets_after_legacy_root_removal()
    {
        var options = new ToobaPlatformOptions
        {
            PostgreSQL = new PostgreSqlOptions
            {
                ConnectionReferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["other"] = "Host=127.0.0.1;Database=other;Password=other-secret",
                },
            },
        };
        var resolver = new DatabaseConnectionResolver(Microsoft.Extensions.Options.Options.Create(options));
        var ex = Assert.Throws<PlatformHttpException>(() => resolver.Resolve(new ConnectionReference("legacy")));
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("platform.connection.unconfigured", ex.ErrorCode);
        Assert.DoesNotContain("other-secret", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(
            typeof(PostgreSqlOptions).GetProperties(),
            p => p.Name == "ConnectionString");
    }

    private static DatabaseConnectionResolver Create(params (string Key, string Value)[] entries)
    {
        var refs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in entries)
            refs[key] = value;

        return new DatabaseConnectionResolver(Microsoft.Extensions.Options.Options.Create(new ToobaPlatformOptions
        {
            PostgreSQL = new PostgreSqlOptions { ConnectionReferences = refs },
        }));
    }

    private static PlatformHttpException AssertFailClosed(
        DatabaseConnectionResolver resolver,
        ConnectionReference reference,
        string mustNotLeakRef,
        string mustNotLeakCs)
    {
        var ex = Assert.Throws<PlatformHttpException>(() => resolver.Resolve(reference));
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("platform.connection.unconfigured", ex.ErrorCode);
        Assert.Equal("Service Unavailable", ex.Title);
        Assert.DoesNotContain(mustNotLeakRef, ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(mustNotLeakRef, ex.Title, StringComparison.Ordinal);
        Assert.DoesNotContain(mustNotLeakCs, ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(mustNotLeakCs, ex.Title, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", ex.Message, StringComparison.OrdinalIgnoreCase);
        return ex;
    }
}
