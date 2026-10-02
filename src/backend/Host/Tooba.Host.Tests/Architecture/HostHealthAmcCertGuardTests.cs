using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT — durable certification of Host/Health platform boundary.
/// </summary>
public sealed class HostHealthAmcCertGuardTests
{
    private static readonly string[] ExpectedFiles =
    [
        "HostHealthEndpoints.cs",
        "HostReadinessEvaluator.cs",
    ];

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Persistence|StoreContext)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Health_certified_exact_tree_namespace_routes_and_di()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Health");
        Assert.True(Directory.Exists(dir));
        Assert.Empty(Directory.GetDirectories(dir));
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, files);

        var endpoints = File.ReadAllText(Path.Combine(dir, "HostHealthEndpoints.cs"));
        var evaluator = File.ReadAllText(Path.Combine(dir, "HostReadinessEvaluator.cs"));

        Assert.Contains("namespace Tooba.Host.Health;", endpoints, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Host.Health;", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Host;", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Host;", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("TypeForwardedTo", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("TypeForwardedTo", evaluator, StringComparison.Ordinal);

        Assert.Contains("MapGet(\"/health/live\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/health\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/health/ready\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/ready\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("Results.Json", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiResponseFactory", endpoints, StringComparison.Ordinal);
        Assert.Contains("IEnumerable<IBusControl>", endpoints, StringComparison.Ordinal);
        Assert.Contains("IEnumerable<IBusControl>", evaluator, StringComparison.Ordinal);
        Assert.Contains("TryGetSingleBus", evaluator, StringComparison.Ordinal);
        Assert.Contains("checks[\"postgresql\"] = \"missing-reference\";", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("missing-reference:{", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("messaging-schema", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("messagingOptions.Schema", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (Exception", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (Exception", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("ActivitySource", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Meter", evaluator, StringComparison.Ordinal);

        foreach (var path in Directory.EnumerateFiles(dir, "*.cs"))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("IServiceProvider", text, StringComparison.Ordinal);
            Assert.DoesNotContain("RequestServices", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetService<", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetRequiredService<", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AccessControl.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AccessControl.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AccessControl.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("NpgsqlConnection", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Publish(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IConsumer", text, StringComparison.Ordinal);
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (ForeignModuleLayer.IsMatch(line) && !line.Contains("Contracts.Readiness", StringComparison.Ordinal))
                    Assert.Fail("foreign module layer: " + Path.GetFileName(path) + ": " + line);
            }
        }

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Health;", program, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(program, @"HostHealthEndpoints\.Map\(").Count);

        var middleware = Read("src/backend/Host/Tooba.Host/MultiTenancy/TenantResolutionMiddleware.cs");
        Assert.Contains("new(\"/health\")", middleware, StringComparison.Ordinal);
        Assert.Contains("new(\"/ready\")", middleware, StringComparison.Ordinal);

        var auth = Read("src/backend/Host/Tooba.Host/Authentication/SessionAuthenticationMiddleware.cs");
        Assert.Contains("StartsWithSegments(\"/health\")", auth, StringComparison.Ordinal);
        Assert.Contains("StartsWithSegments(\"/ready\")", auth, StringComparison.Ordinal);

        Assert.Contains("foreach (var tenant in registry.Tenants.Values)", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("TenantStatus.Active", evaluator, StringComparison.Ordinal);
    }

    [Fact]
    public void Sot_certifies_health_and_preserves_prior_host_certs()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_HEALTH_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_HEALTH_PLATFORM_BOUNDARY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostHealthAmc001W2Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"lastAcceptedCommit\": \"ba8db8c6bcb22f0ad4c386073d3b612e3d318e00\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"latestAcceptedImplementationWave\": \"TB-TMAR-HOST-HEALTH-AMC-001-W1\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_HEALTH_AMC_001_W2_CERT", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("CONFIGURED_NOT_CONNECTIVITY", sot, StringComparison.Ordinal);
        Assert.Contains("ALL_CONFIGURED", sot, StringComparison.Ordinal);
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
