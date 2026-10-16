using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-114 — the Development legacy Catalog bootstrap DI lifetime boundary.
/// <para>
/// TB-113 observed the empty-scratch startup aborting with
/// <c>Cannot resolve scoped service 'Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext'
/// from root provider</c> at <c>FashionTemplateCatalogSeed.ApplyAsync</c>, reached from
/// <c>Program.cs</c> -> <c>CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync</c>.
/// </para>
/// <para>
/// These tests pin the three things the repair must guarantee: the root-provider hand-off really
/// fails on a real validating container; a scope-hosted hand-off succeeds and disposes its scoped
/// services (on success, on exception and on cancellation); and the real composition call site in
/// <c>Program.cs</c> creates an async scope and passes <c>scope.ServiceProvider</c> — not
/// <c>app.Services</c> — to the Catalog-owned seed seam whose whole sequence needs that scope.
/// </para>
/// </summary>
public sealed class CatalogLegacyBootstrapsDiScopeTests
{
    [Fact]
    public void Root_provider_handoff_reproduces_the_tb113_lifetime_failure()
    {
        Assert.False(
            Root_source_contains_scope_validation_suppression(),
            "scope validation must not be disabled to hide a root-provider hand-off");

        using var provider = BuildValidatingProvider();

        // The exact failure observed at startup: resolving a scoped service from the root provider.
        var failure = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<AsyncScopedProbe>());

        Assert.Contains("Cannot resolve scoped service", failure.Message, StringComparison.Ordinal);
        Assert.Contains("from root provider", failure.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(AsyncScopedProbe), failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Scoped_provider_handoff_succeeds_and_disposes_scoped_services()
    {
        await using var provider = BuildValidatingProvider();
        AsyncScopedProbe? asyncProbe = null;
        SyncScopedProbe? syncProbe = null;

        // Mirrors Program.cs: create the scope, hand its ServiceProvider to the bootstrap sequence.
        await using (var scope = provider.CreateAsyncScope())
        {
            var scopedProvider = scope.ServiceProvider;
            asyncProbe = scopedProvider.GetRequiredService<AsyncScopedProbe>();
            syncProbe = scopedProvider.GetRequiredService<SyncScopedProbe>();

            // Scoped identity is stable inside the scope and never resolved from the root.
            Assert.Same(asyncProbe, scopedProvider.GetRequiredService<AsyncScopedProbe>());
            Assert.False(asyncProbe.Disposed);
            Assert.False(syncProbe.Disposed);
        }

        Assert.True(asyncProbe!.Disposed, "IAsyncDisposable scoped service must be disposed with the scope");
        Assert.True(syncProbe!.Disposed, "IDisposable scoped service must be disposed with the scope");

        // A second scope gets fresh instances: the sequence is not sharing state across scopes.
        await using var secondScope = provider.CreateAsyncScope();
        Assert.NotSame(asyncProbe, secondScope.ServiceProvider.GetRequiredService<AsyncScopedProbe>());
    }

    [Fact]
    public async Task Scope_disposes_when_the_bootstrap_throws_or_is_cancelled()
    {
        await using var provider = BuildValidatingProvider();
        AsyncScopedProbe? thrownProbe = null;
        AsyncScopedProbe? cancelledProbe = null;

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunLegacyBootstrapSequenceAsync(provider, scopedProvider =>
            {
                thrownProbe = scopedProvider.GetRequiredService<AsyncScopedProbe>();
                throw new InvalidOperationException("bootstrap failed");
            }));
        Assert.Equal("bootstrap failed", thrown.Message);
        Assert.True(thrownProbe!.Disposed, "scope must dispose while the bootstrap exception unwinds");

        var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            RunLegacyBootstrapSequenceAsync(provider, scopedProvider =>
            {
                cancelledProbe = scopedProvider.GetRequiredService<AsyncScopedProbe>();
                throw new OperationCanceledException();
            }));
        Assert.NotNull(cancelled);
        Assert.True(cancelledProbe!.Disposed, "scope must dispose on cancellation");
    }

    [Fact]
    public void Program_hands_the_legacy_catalog_bootstraps_a_scoped_provider()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));

        // The composition root still owns the call and the Catalog-owned seam name is unchanged.
        Assert.Contains("CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync", program, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(app.Services",
            program,
            StringComparison.Ordinal);
        // The root provider must never be the seed seam's own provider argument anywhere.
        Assert.DoesNotMatch(
            new Regex(@"CatalogDevelopmentSeed\.EnsureLegacyBootstrapsAsync\s*\(\s*app\.Services", RegexOptions.CultureInvariant),
            program);

        // TB-116: the scope itself is owned by the canonical Host Development gateway, and the legacy
        // Catalog sequence is reachable ONLY as the delegate handed to that gateway. The gateway creates
        // the async scope (asserted in CatalogLegacyBootstrapsCommerceContextTests) and assigns the
        // trusted CommerceContext before invoking the delegate, so the sequence still runs on a scoped,
        // assigned provider rather than on the root provider.
        var handoff = Regex.Match(
            program,
            @"DevelopmentTenantCommerceContext\.RunForDevelopmentTenantAsync\s*\(\s*app\.Services\s*," +
            @"[\s\S]{0,240}?CatalogDevelopmentSeed\.EnsureLegacyBootstrapsAsync\s*\)",
            RegexOptions.CultureInvariant);
        Assert.True(
            handoff.Success,
            "Program.cs must run the legacy Catalog bootstraps through " +
            "DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(app.Services, ...) so the " +
            "sequence executes on a scoped, commerce-assigned provider");

        // Scope validation stays enabled: the forbidden workarounds are absent from Host source.
        Assert.DoesNotContain("ValidateScopes", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ValidateOnBuild", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Legacy_catalog_bootstrap_sequence_resolves_scoped_services_from_the_single_provider()
    {
        var root = FindRepoRoot();
        var catalogDev = Path.Combine(root, "src", "backend", "Modules", "Catalog", "Tooba.Catalog.Infrastructure", "Development");
        var seam = File.ReadAllText(Path.Combine(catalogDev, "CatalogDevelopmentSeed.cs"));

        Assert.Contains("EnsureLegacyBootstrapsAsync(IServiceProvider provider", seam, StringComparison.Ordinal);

        // Only the legacy sequence body is inspected, and every one of its six bootstraps receives
        // that same single provider, so ONE scope must cover the whole sequence rather than only the
        // first DbContext resolution.
        var legacyBody = Regex.Match(
            seam,
            @"EnsureLegacyBootstrapsAsync\(IServiceProvider provider, CancellationToken cancellationToken\)[\s\S]*$",
            RegexOptions.CultureInvariant).Value;
        Assert.NotEmpty(legacyBody);
        foreach (var seed in new[]
                 {
                     "FashionTemplateCatalogSeed",
                     "IndustryBatchATemplateCatalogSeed",
                     "IndustryBatchBTemplateCatalogSeed",
                     "IndustryBatchCTemplateCatalogSeed",
                     "LandingPageDevelopmentSeed",
                     "StoreMenuDevelopmentSeed",
                 })
        {
            Assert.Contains($"{seed}.ApplyAsync(provider, cancellationToken)", legacyBody, StringComparison.Ordinal);
        }

        Assert.Equal(6, Regex.Matches(legacyBody, @"ApplyAsync\(provider, cancellationToken\)").Count);

        // The boundary stays at the composition root: the module seam does not create its own scope,
        // so the module keeps ownership of seeding while the Host keeps ownership of the lifetime.
        Assert.DoesNotContain("CreateScope", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateAsyncScope", seam, StringComparison.Ordinal);

        foreach (var seedFile in new[]
                 {
                     "FashionTemplateCatalogSeed.cs",
                     "IndustryBatchATemplateCatalogSeed.cs",
                     "IndustryBatchBTemplateCatalogSeed.cs",
                     "IndustryBatchCTemplateCatalogSeed.cs",
                     "LandingPageDevelopmentSeed.cs",
                     "StoreMenuDevelopmentSeed.cs",
                 })
        {
            var seed = File.ReadAllText(Path.Combine(catalogDev, seedFile));
            Assert.Contains("provider.GetRequiredService<CatalogDbContext>()", seed, StringComparison.Ordinal);
        }
    }

    private static async Task RunLegacyBootstrapSequenceAsync(
        IServiceProvider rootProvider,
        Action<IServiceProvider> bootstrap)
    {
        // Identical shape to the repaired Program.cs call site.
        await using var scope = rootProvider.CreateAsyncScope();
        bootstrap(scope.ServiceProvider);
        await Task.CompletedTask;
    }

    private static bool Root_source_contains_scope_validation_suppression()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        return program.Contains("ValidateScopes", StringComparison.Ordinal)
            || program.Contains("ValidateOnBuild", StringComparison.Ordinal);
    }

    private static ServiceProvider BuildValidatingProvider() =>
        new ServiceCollection()
            .AddScoped<AsyncScopedProbe>()
            .AddScoped<SyncScopedProbe>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    private sealed class AsyncScopedProbe : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SyncScopedProbe : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
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
}
