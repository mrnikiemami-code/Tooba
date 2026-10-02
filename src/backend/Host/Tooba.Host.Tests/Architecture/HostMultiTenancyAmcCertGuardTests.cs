using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Host.MultiTenancy;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT — durable certification of Host/MultiTenancy.
/// </summary>
public sealed class HostMultiTenancyAmcCertGuardTests
{
    private static readonly string[] ExpectedFiles =
    [
        "HttpCommerceContextAccessor.cs",
        "TenantResolutionMiddleware.cs",
    ];

    private static readonly string[] PlatformCodes =
    [
        FoundationErrorCodes.PlatformEditionUnconfigured,
        FoundationErrorCodes.PlatformConnectionUnconfigured,
        FoundationErrorCodes.PlatformResolutionFailed,
    ];

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Persistence|StoreContext)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void MultiTenancy_certified_exact_tree_namespace_and_cohesion()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/MultiTenancy");
        Assert.True(Directory.Exists(dir));
        Assert.Empty(Directory.GetDirectories(dir));
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, files);

        var accessor = File.ReadAllText(Path.Combine(dir, "HttpCommerceContextAccessor.cs"));
        var middleware = File.ReadAllText(Path.Combine(dir, "TenantResolutionMiddleware.cs"));

        Assert.Contains("namespace Tooba.Host.MultiTenancy", accessor, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Host.MultiTenancy", middleware, StringComparison.Ordinal);
        Assert.Contains("internal sealed class HttpCommerceContextAccessor", accessor, StringComparison.Ordinal);
        Assert.Contains("internal sealed class TenantResolutionMiddleware", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("class TenantResolutionMiddleware", accessor, StringComparison.Ordinal);
        Assert.DoesNotContain("class HttpCommerceContextAccessor", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("TypeForwardedTo", accessor, StringComparison.Ordinal);
        Assert.DoesNotContain("TypeForwardedTo", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Host;", accessor, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Host;", middleware, StringComparison.Ordinal);

        Assert.Contains("ICurrentCommerceContext", accessor, StringComparison.Ordinal);
        Assert.Contains("ICurrentEdition", accessor, StringComparison.Ordinal);
        Assert.Contains("ICurrentTenant", accessor, StringComparison.Ordinal);
        Assert.Contains("ICommerceContextAssigner", accessor, StringComparison.Ordinal);
        Assert.Contains("ItemKey = \"Tooba.CommerceContext\"", accessor, StringComparison.Ordinal);
        Assert.Contains("_assigned ??", accessor, StringComparison.Ordinal);
        Assert.Contains("ArgumentNullException.ThrowIfNull", accessor, StringComparison.Ordinal);

        Assert.Contains("InvokeAsync(HttpContext httpContext, IStoreCommerceContextAssigner storeCommerceAssigner)", middleware, StringComparison.Ordinal);
        Assert.Contains("storeCommerceAssigner.Assign(storeCommerce)", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<IStoreCommerceContextAssigner>", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("IServiceProvider", middleware, StringComparison.Ordinal);
        Assert.Contains("IExceptionPresentationService", middleware, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.PlatformEditionUnconfigured", middleware, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.PlatformConnectionUnconfigured", middleware, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.PlatformResolutionFailed", middleware, StringComparison.Ordinal);
        Assert.Contains("FailClosed()", middleware, StringComparison.Ordinal);
        Assert.Contains("record.Status != TenantStatus.Active", middleware, StringComparison.Ordinal);
        Assert.Contains("new(\"/health\")", middleware, StringComparison.Ordinal);
        Assert.Contains("new(\"/ready\")", middleware, StringComparison.Ordinal);
        Assert.Contains("new(\"/__platform-error\")", middleware, StringComparison.Ordinal);
        Assert.Contains("new(\"/__platform-conflict\")", middleware, StringComparison.Ordinal);
        Assert.Contains("HostNormalizer.TryNormalize", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("Tenant-Id", middleware, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Tenant", middleware, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain("PlatformExceptionMapper", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("new ProblemDetails", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("IProblemDetailsService", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Not Found\"", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Service Unavailable\"", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("ActivitySource", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("Meter", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreContext.Application", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreContext.Infrastructure", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreContext.Domain", middleware, StringComparison.Ordinal);

        foreach (var path in Directory.EnumerateFiles(dir, "*.cs"))
        {
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (ForeignModuleLayer.IsMatch(line))
                    Assert.Fail("foreign module layer: " + Path.GetFileName(path) + ": " + line);
            }
        }
    }

    [Fact]
    public void Accessor_scoped_di_identity_same_instance_for_four_interfaces()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddScoped<HttpCommerceContextAccessor>();
        services.AddScoped<ICurrentCommerceContext>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICurrentEdition>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICommerceContextAssigner>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var concrete = scope.ServiceProvider.GetRequiredService<HttpCommerceContextAccessor>();
        Assert.Same(concrete, scope.ServiceProvider.GetRequiredService<ICurrentCommerceContext>());
        Assert.Same(concrete, scope.ServiceProvider.GetRequiredService<ICurrentEdition>());
        Assert.Same(concrete, scope.ServiceProvider.GetRequiredService<ICurrentTenant>());
        Assert.Same(concrete, scope.ServiceProvider.GetRequiredService<ICommerceContextAssigner>());

        using var scope2 = provider.CreateScope();
        var other = scope2.ServiceProvider.GetRequiredService<HttpCommerceContextAccessor>();
        Assert.NotSame(concrete, other);
    }

    [Fact]
    public void Program_and_platform_matrix_certified()
    {
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.MultiTenancy;", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<HttpCommerceContextAccessor>()", program, StringComparison.Ordinal);
        Assert.Contains("UseMiddleware<TenantResolutionMiddleware>()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSingleton<HttpCommerceContextAccessor>", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddTransient<HttpCommerceContextAccessor>", program, StringComparison.Ordinal);

        var foundation = new FoundationErrorCatalogContributor().Contribute()
            .GroupBy(d => d.Code, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        Assert.Equal(503, foundation[FoundationErrorCodes.PlatformEditionUnconfigured].Single().HttpStatus);
        Assert.Equal(503, foundation[FoundationErrorCodes.PlatformConnectionUnconfigured].Single().HttpStatus);
        Assert.Equal(404, foundation[FoundationErrorCodes.PlatformResolutionFailed].Single().HttpStatus);

        var resources = new FoundationErrorResourceSet();
        foreach (var code in PlatformCodes)
        {
            Assert.True(resources.Owns(code));
            Assert.False(string.IsNullOrWhiteSpace(resources.GetString(code, System.Globalization.CultureInfo.GetCultureInfo("en"))));
            Assert.False(string.IsNullOrWhiteSpace(resources.GetString(code, System.Globalization.CultureInfo.GetCultureInfo("fa"))));
        }
    }

    [Fact]
    public void Sot_certifies_multitenancy_and_preserves_errors_security_admin()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_PLATFORM_BOUNDARY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostMultiTenancyAmc001W2Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", sot, StringComparison.Ordinal);
        // Multitenancy W1 implementation SHA remains recorded in its own SoT block (top-level may advance).
        Assert.Contains("\"implementationCommit\": \"cfbc94d258de837fdc018ddb29db68233cc25783\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_MULTITENANCY_AMC_001_W2_CERT", sot, StringComparison.Ordinal);
    }

    private static string Dir(string relative) => Path.Combine(Repo(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string Read(string relative) => File.ReadAllText(Repo(relative));

    private static string Repo(string? relative = null)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return relative is null
                    ? directory.FullName
                    : Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
