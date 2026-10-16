using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Host.Configuration;
using Tooba.Host.Development;
using Tooba.Host.MultiTenancy;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-116 — the Development legacy Catalog bootstrap commerce-context boundary.
/// <para>
/// TB-115 observed the empty-scratch startup crossing the TB-114 scoped-provider repair and then failing
/// on the very next step: the Catalog DbContext options factory resolved
/// <c>ICurrentCommerceContext.Current</c> inside the legacy-bootstrap scope, found it <c>null</c> and
/// <c>ToobaNpgsql.ResolveForContext</c> threw <c>503 platform.edition.unconfigured</c>. The migrator never
/// hit this because it assigns a trusted <see cref="CommerceContext"/> in its own scope first.
/// </para>
/// <para>
/// These tests pin the repair through the REAL canonical Host Development gateway
/// (<see cref="DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync"/>): the trusted context is
/// assigned INSIDE the same scope before the seed resolves any scoped database dependency, the production
/// <see cref="ToobaNpgsql.ResolveForContext"/> then succeeds, the non-Active/absent tenant path stays
/// fail-closed with no assignment and no seed, each invocation owns a disposed scope, and an unassigned
/// scope still reproduces the exact TB-115 failure (negative control).
/// </para>
/// </summary>
public sealed class CatalogLegacyBootstrapsCommerceContextTests
{
    private const string TraceLabel = "legacy-catalog-dev-seed";

    [Fact]
    public async Task Gateway_assigns_trusted_context_inside_the_same_scope_before_the_seed_resolves_its_db_dependency()
    {
        await using var provider = BuildHostDevelopmentProvider(TenantStatus.Active);
        var seedRan = false;
        CommerceContext? observed = null;
        string? resolvedConnection = null;

        await DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(
            provider,
            TraceLabel,
            (scopedProvider, _) =>
            {
                seedRan = true;

                // Mirrors CatalogModule's CatalogDbContext options factory, which resolves the scoped
                // ICurrentCommerceContext and hands it to the same production function that threw
                // platform.edition.unconfigured in TB-115.
                observed = scopedProvider.GetRequiredService<ICurrentCommerceContext>().Current;
                resolvedConnection = ToobaNpgsql.ResolveForContext(
                    scopedProvider.GetRequiredService<ICurrentCommerceContext>(),
                    scopedProvider.GetRequiredService<IDatabaseConnectionResolver>());

                return Task.CompletedTask;
            });

        Assert.True(seedRan, "the legacy Catalog sequence must run when the Development tenant is Active");
        Assert.NotNull(observed);
        Assert.Equal(ToobaEdition.SingleStore, observed!.Edition.Edition);
        Assert.Equal("dev-test", observed.Edition.DeploymentId);
        Assert.Equal(TenantStatus.Active, observed.Tenant!.Status);
        Assert.Equal(DevelopmentTenantCommerceContext.DevelopmentTenantId, observed.Tenant.TenantId.Value);
        Assert.Equal(
            $"{ConnectionReferencePrefix}{DevelopmentTenantCommerceContext.DevelopmentTenantId}",
            observed.Tenant.ConnectionReference.Value);
        // The DbContext resolves the tenant's own connection reference, never a marketplace fallback.
        Assert.Equal(observed.Tenant.ConnectionReference, observed.DatabaseConnectionReference);
        Assert.Equal(TraceLabel, observed.TraceId);
        Assert.NotNull(resolvedConnection);
    }

    [Theory]
    [InlineData(TenantStatus.Disabled)]
    [InlineData(TenantStatus.Suspended)]
    public async Task Gateway_is_fail_closed_when_the_development_tenant_is_not_active(TenantStatus status)
    {
        await using var provider = BuildHostDevelopmentProvider(status);
        var seedRan = false;
        var contextAssigned = false;

        await DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(
            provider,
            TraceLabel,
            (scopedProvider, _) =>
            {
                seedRan = true;
                contextAssigned = scopedProvider.GetRequiredService<ICurrentCommerceContext>().Current is not null;
                return Task.CompletedTask;
            });

        Assert.False(seedRan, "a non-Active Development tenant must not run the legacy Catalog sequence");
        Assert.False(contextAssigned, "a non-Active Development tenant must not receive a CommerceContext");
    }

    [Fact]
    public async Task Gateway_is_fail_closed_when_the_development_tenant_is_absent()
    {
        await using var provider = BuildHostDevelopmentProvider(tenantStatus: null);
        var seedRan = false;

        await DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(
            provider,
            TraceLabel,
            (_, _) =>
            {
                seedRan = true;
                return Task.CompletedTask;
            });

        Assert.False(seedRan, "an absent Development tenant must not run the legacy Catalog sequence");
    }

    [Fact]
    public void Unassigned_scope_still_reproduces_the_tb115_platform_edition_unconfigured_failure()
    {
        // Negative control: without the gateway's assignment the SAME production resolve path fails, so the
        // focused test above is not vacuous and the repaired hand-off is what changed the outcome.
        using var provider = BuildHostDevelopmentProvider(TenantStatus.Active);
        using var unassignedScope = provider.CreateScope();

        var failure = Assert.Throws<PlatformHttpException>(() => ToobaNpgsql.ResolveForContext(
            unassignedScope.ServiceProvider.GetRequiredService<ICurrentCommerceContext>(),
            unassignedScope.ServiceProvider.GetRequiredService<IDatabaseConnectionResolver>()));

        Assert.Equal(503, failure.StatusCode);
        Assert.Equal("platform.edition.unconfigured", failure.ErrorCode);
    }

    [Fact]
    public async Task Gateway_uses_a_fresh_scope_per_invocation_and_disposes_it_on_success_and_throw()
    {
        await using var provider = BuildHostDevelopmentProvider(TenantStatus.Active, registerScopedProbe: true);
        var probes = new List<ScopedProbe>();

        for (var invocation = 0; invocation < 2; invocation++)
        {
            await DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(
                provider,
                TraceLabel,
                (scopedProvider, _) =>
                {
                    probes.Add(scopedProvider.GetRequiredService<ScopedProbe>());
                    return Task.CompletedTask;
                });
        }

        Assert.Equal(2, probes.Count);
        Assert.NotSame(probes[0], probes[1]);
        Assert.True(probes[0].Disposed && probes[1].Disposed, "each invocation's scope must be disposed");
        ScopedProbe? thrownProbe = null;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(
                provider,
                TraceLabel,
                (scopedProvider, _) =>
                {
                    thrownProbe = scopedProvider.GetRequiredService<ScopedProbe>();
                    probes.Add(thrownProbe);
                    throw new InvalidOperationException("seed failed");
                }));

        Assert.Equal(3, probes.Count);
        Assert.True(thrownProbe!.Disposed, "the scope must dispose while a seed exception unwinds");
        Assert.All(probes, probe => Assert.True(probe.Disposed, "every invocation scope must be disposed"));
    }

    [Fact]
    public void Program_uses_the_canonical_gateway_for_the_legacy_catalog_bootstraps()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));

        // The Host composition root must not build a tenant tuple or assign a context by hand here; it
        // delegates to the single allowlisted Development tenant/commerce seam.
        Assert.Contains(
            "DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync",
            program,
            StringComparison.Ordinal);
        Assert.DoesNotContain("new CommerceContext(", program, StringComparison.Ordinal);

        // The scope still belongs to that seam, which is what makes the assignment and the seed share one
        // scoped lifetime in the real Host.
        var gateway = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Development", "DevelopmentTenantCommerceContext.cs"));
        Assert.Contains("CreateAsyncScope", gateway, StringComparison.Ordinal);
        Assert.Contains("ICommerceContextAssigner", gateway, StringComparison.Ordinal);
        Assert.Contains("DevelopmentTenantId = \"store-alpha\"", gateway, StringComparison.Ordinal);
    }

    private const string ConnectionReferencePrefix = "tenant-";

    private static ServiceProvider BuildHostDevelopmentProvider(
        TenantStatus? tenantStatus,
        bool registerScopedProbe = false)
    {
        var services = new ServiceCollection();
        // Mirrors the Program.cs Development registrations for the commerce context accessor.
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<HttpCommerceContextAccessor>();
        services.AddScoped<ICurrentCommerceContext>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICurrentEdition>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICommerceContextAssigner>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddSingleton<IDatabaseConnectionResolver, RecordingConnectionResolver>();
        services.AddSingleton(BuildRegistry(tenantStatus));
        if (registerScopedProbe)
        {
            services.AddScoped<ScopedProbe>();
        }

        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private static ControlPlaneRegistry BuildRegistry(TenantStatus? tenantStatus)
    {
        var tenantId = DevelopmentTenantCommerceContext.DevelopmentTenantId;
        var tenants = new Dictionary<string, TenantRecord>(StringComparer.Ordinal);
        if (tenantStatus is { } status)
        {
            tenants[tenantId] = new TenantRecord
            {
                TenantId = new TenantId(tenantId),
                DisplayName = "Host composition test store",
                Status = status,
                ConnectionReference = new ConnectionReference(ConnectionReferencePrefix + tenantId),
                ThemeReference = "theme-test",
                DefaultMarketReference = "IR",
                PrimaryDomain = "alpha.localhost",
                Hosts = ["127.0.0.1"],
            };
        }

        return new ControlPlaneRegistry
        {
            Edition = ToobaEdition.SingleStore,
            DeploymentId = "dev-test",
            Hosts = new Dictionary<string, TenantRecord>(StringComparer.Ordinal),
            Tenants = tenants,
        };
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    /// <summary>Test resolver seam; returns a non-secret placeholder rather than a real credential.</summary>
    private sealed class RecordingConnectionResolver : IDatabaseConnectionResolver
    {
        public string Resolve(ConnectionReference reference) =>
            $"Host=127.0.0.1;Database={reference.Value};Username=test;Password=placeholder";
    }

    private sealed class ScopedProbe : IAsyncDisposable, IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
