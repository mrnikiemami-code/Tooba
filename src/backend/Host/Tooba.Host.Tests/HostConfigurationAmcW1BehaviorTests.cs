using Tooba.Host.Configuration;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-TMAR-HOST-CONFIGURATION-AMC-001-W1 focused behavior: TrustedProxies + PrimaryDomain fail-fast.
/// </summary>
public sealed class HostConfigurationAmcW1BehaviorTests
{
    [Fact]
    public void TrustedProxies_empty_list_is_valid()
    {
        var options = Sample();
        options.TrustedProxies = [];
        var result = new PlatformOptionsValidator().Validate(null, options);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void TrustedProxies_valid_ipv4_is_accepted()
    {
        var options = Sample();
        options.TrustedProxies = ["127.0.0.1"];
        var result = new PlatformOptionsValidator().Validate(null, options);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void TrustedProxies_valid_ipv6_is_accepted()
    {
        var options = Sample();
        options.TrustedProxies = ["::1"];
        var result = new PlatformOptionsValidator().Validate(null, options);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void TrustedProxies_invalid_entry_fails_validation()
    {
        var options = Sample();
        options.TrustedProxies = ["not-an-ip"];
        var result = new PlatformOptionsValidator().Validate(null, options);
        Assert.False(result.Succeeded);
        Assert.Contains("TrustedProxies", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void TrustedProxies_blank_configured_element_fails_validation()
    {
        var options = Sample();
        options.TrustedProxies = ["127.0.0.1", "  "];
        var result = new PlatformOptionsValidator().Validate(null, options);
        Assert.False(result.Succeeded);
        Assert.Contains("TrustedProxies", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimaryDomain_null_is_optional_and_defaults_to_first_host()
    {
        var options = Sample();
        options.SingleStore.Tenants[0].PrimaryDomain = null;
        var registry = PlatformOptionsValidator.BuildRegistry(options);
        var tenant = registry.Tenants["store-alpha"];
        Assert.Equal("alpha.localhost", tenant.PrimaryDomain);
        Assert.Contains("alpha.localhost", tenant.Hosts);
    }

    [Fact]
    public void PrimaryDomain_valid_normalized_and_added_when_absent_from_hosts()
    {
        var options = Sample();
        options.SingleStore.Tenants[0].PrimaryDomain = "PRIMARY.ALPHA.LOCALHOST";
        options.SingleStore.Tenants[0].Hosts = ["alpha.localhost"];
        var registry = PlatformOptionsValidator.BuildRegistry(options);
        var tenant = registry.Tenants["store-alpha"];
        Assert.Equal("primary.alpha.localhost", tenant.PrimaryDomain);
        Assert.Contains("primary.alpha.localhost", tenant.Hosts);
        Assert.True(registry.Hosts.ContainsKey("primary.alpha.localhost"));
    }

    [Fact]
    public void PrimaryDomain_invalid_nonempty_fails_fast()
    {
        var options = Sample();
        options.SingleStore.Tenants[0].PrimaryDomain = "*";
        var ex = Assert.Throws<InvalidOperationException>(() => PlatformOptionsValidator.BuildRegistry(options));
        Assert.Contains("invalid PrimaryDomain", ex.Message, StringComparison.Ordinal);

        var result = new PlatformOptionsValidator().Validate(null, options);
        Assert.False(result.Succeeded);
        Assert.Contains("PrimaryDomain", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimaryDomain_duplicate_across_tenants_fails()
    {
        var options = Sample();
        options.SingleStore.Tenants[0].PrimaryDomain = "shared.localhost";
        options.SingleStore.Tenants.Add(new TenantRecordOptions
        {
            TenantId = "store-beta",
            ConnectionReference = "Tenant:beta",
            Hosts = ["beta.localhost"],
            PrimaryDomain = "shared.localhost",
        });

        var ex = Assert.Throws<InvalidOperationException>(() => PlatformOptionsValidator.BuildRegistry(options));
        Assert.Contains("Duplicate host", ex.Message, StringComparison.Ordinal);
    }

    private static ToobaPlatformOptions Sample() => new()
    {
        Edition = "SingleStore",
        SingleStore =
        {
            Tenants =
            [
                new TenantRecordOptions
                {
                    TenantId = "store-alpha",
                    ConnectionReference = "Tenant:alpha",
                    Hosts = ["alpha.localhost"],
                },
            ],
        },
        PostgreSQL =
        {
            ConnectionReferences =
            {
                ["Tenant:alpha"] = "Host=localhost;Database=alpha;Username=u;Password=p",
                ["Tenant:beta"] = "Host=localhost;Database=beta;Username=u;Password=p",
            },
        },
    };
}
