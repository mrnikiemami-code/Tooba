using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-MULTITENANCY-AMC-001-W1 — structure/lifetime hygiene guard (not certification).
/// </summary>
public sealed class HostMultiTenancyAmcW1GuardTests
{
    private static readonly string[] ExpectedFiles =
    [
        "HttpCommerceContextAccessor.cs",
        "TenantResolutionMiddleware.cs",
    ];

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Persistence)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void MultiTenancy_exact_two_files_exact_namespace_split()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/MultiTenancy");
        Assert.True(Directory.Exists(dir));
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, files);

        var accessor = File.ReadAllText(Path.Combine(dir, "HttpCommerceContextAccessor.cs"));
        var middleware = File.ReadAllText(Path.Combine(dir, "TenantResolutionMiddleware.cs"));

        Assert.Contains("namespace Tooba.Host.MultiTenancy", accessor, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Host.MultiTenancy", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Host;", accessor, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Host;", middleware, StringComparison.Ordinal);

        Assert.Contains("internal sealed class HttpCommerceContextAccessor", accessor, StringComparison.Ordinal);
        Assert.Contains("ICurrentCommerceContext", accessor, StringComparison.Ordinal);
        Assert.Contains("ICurrentEdition", accessor, StringComparison.Ordinal);
        Assert.Contains("ICurrentTenant", accessor, StringComparison.Ordinal);
        Assert.Contains("ICommerceContextAssigner", accessor, StringComparison.Ordinal);
        Assert.Contains("ItemKey = \"Tooba.CommerceContext\"", accessor, StringComparison.Ordinal);
        Assert.DoesNotContain("class TenantResolutionMiddleware", accessor, StringComparison.Ordinal);

        Assert.Contains("internal sealed class TenantResolutionMiddleware", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("class HttpCommerceContextAccessor", middleware, StringComparison.Ordinal);
        Assert.Contains("InvokeAsync(HttpContext httpContext, IStoreCommerceContextAssigner storeCommerceAssigner)", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<IStoreCommerceContextAssigner>", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("IServiceProvider", middleware, StringComparison.Ordinal);
        Assert.Contains("storeCommerceAssigner.Assign(storeCommerce)", middleware, StringComparison.Ordinal);

        Assert.Contains("new(\"/health\")", middleware, StringComparison.Ordinal);
        Assert.Contains("new(\"/ready\")", middleware, StringComparison.Ordinal);
        Assert.Contains("new(\"/__platform-error\")", middleware, StringComparison.Ordinal);
        Assert.Contains("new(\"/__platform-conflict\")", middleware, StringComparison.Ordinal);

        Assert.Contains("IExceptionPresentationService", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformExceptionMapper", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("new ProblemDetails", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("IProblemDetailsService", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Not Found\"", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Service Unavailable\"", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", middleware, StringComparison.Ordinal);
    }

    [Fact]
    public void MultiTenancy_program_scoped_registrations_and_middleware_present()
    {
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.MultiTenancy;", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<HttpCommerceContextAccessor>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<ICurrentCommerceContext>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>())", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<ICurrentEdition>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>())", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>())", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<ICommerceContextAssigner>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>())", program, StringComparison.Ordinal);
        Assert.Contains("UseMiddleware<TenantResolutionMiddleware>()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void MultiTenancy_folder_zero_foreign_layers_and_requestservices()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/MultiTenancy");
        var violations = new List<string>();
        foreach (var path in Directory.EnumerateFiles(dir, "*.cs"))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("RequestServices", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetRequiredService<IStoreCommerceContextAssigner>", text, StringComparison.Ordinal);
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (ForeignModuleLayer.IsMatch(line))
                    violations.Add(Path.GetFileName(path) + ": " + line);
            }
        }

        Assert.True(violations.Count == 0, string.Join("; ", violations));
    }

    [Fact]
    public void MultiTenancy_mapper_absent_hostwide_still()
    {
        var hostRoot = Dir("src/backend/Host/Tooba.Host");
        foreach (var path in Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("PlatformExceptionMapper", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MappedPlatformError", text, StringComparison.Ordinal);
        }
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
