using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT — durable certification of Host/Observability platform boundary.
/// </summary>
public sealed class HostObservabilityAmcCertGuardTests
{
    private static readonly string[] ExpectedFiles =
    [
        "RequestObservabilityEnrichmentMiddleware.cs",
    ];

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Inventory|Cart|Persistence|StoreContext)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Observability_certified_exact_tree_privacy_and_boundaries()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Observability");
        Assert.True(Directory.Exists(dir));
        Assert.Empty(Directory.GetDirectories(dir));
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, files);

        var text = File.ReadAllText(Path.Combine(dir, "RequestObservabilityEnrichmentMiddleware.cs"));
        Assert.Contains("namespace Tooba.Host.Observability;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Host;", text, StringComparison.Ordinal);
        Assert.Contains("internal sealed class RequestObservabilityEnrichmentMiddleware", text, StringComparison.Ordinal);
        Assert.Contains("ObservabilityLogScope.CreateState", text, StringComparison.Ordinal);
        Assert.Contains("ObservabilityLogScope.Begin", text, StringComparison.Ordinal);
        Assert.Contains("ICorrelationIdProvider", text, StringComparison.Ordinal);
        Assert.Contains("CorrelationIdMiddleware.HttpContextItemKey", text, StringComparison.Ordinal);
        Assert.Contains("EnsureCorrelationId()", text, StringComparison.Ordinal);
        Assert.Contains("ICurrentCommerceContext", text, StringComparison.Ordinal);
        Assert.Contains("CurrentAuthenticatedSession", text, StringComparison.Ordinal);
        Assert.Contains("context.Request.Path.Value", text, StringComparison.Ordinal);
        Assert.Contains("context.TraceIdentifier", text, StringComparison.Ordinal);
        Assert.Contains("ToString(\"N\")", text, StringComparison.Ordinal);
        Assert.Contains("storeId = tenantId", text, StringComparison.Ordinal);
        Assert.DoesNotContain("RemoteIpAddress", text, StringComparison.Ordinal);
        Assert.DoesNotContain("clientIp", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ClientIp", text, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Forwarded-For", text, StringComparison.Ordinal);
        Assert.DoesNotContain("QueryString", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ActivitySource", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Meter", text, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (", text, StringComparison.Ordinal);
        Assert.DoesNotContain("TypeForwardedTo", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("_logger.Log", text, StringComparison.Ordinal);

        foreach (Match m in ForeignModuleLayer.Matches(text))
            Assert.Fail("foreign module layer: " + m.Value);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Observability;", program, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(program, @"UseMiddleware<RequestObservabilityEnrichmentMiddleware>\(\)"));
        var sessionIdx = program.IndexOf("UseMiddleware<SessionAuthenticationMiddleware>()", StringComparison.Ordinal);
        var obsIdx = program.IndexOf("UseMiddleware<RequestObservabilityEnrichmentMiddleware>()", StringComparison.Ordinal);
        var tenantIdx = program.IndexOf("UseMiddleware<TenantResolutionMiddleware>()", StringComparison.Ordinal);
        Assert.True(tenantIdx >= 0 && sessionIdx > tenantIdx && obsIdx > sessionIdx);
    }

    [Fact]
    public void Sot_certifies_observability_and_preserves_prior_host_certs()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_OBSERVABILITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_OBSERVABILITY_PLATFORM_BOUNDARY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostObservabilityAmc001W2Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"implementationCommit\": \"f1425fed94cc1a8354d3c9f9a013065d87cbe66c\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_OBSERVABILITY_AMC_001_W2_CERT", sot, StringComparison.Ordinal);
        Assert.Contains("OMITTED_PRIVACY_SAFE_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("PATH_ONLY_NO_QUERY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MESSAGING_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_HEALTH_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HostObservabilityAmcCertGuardTests", sot, StringComparison.Ordinal);
        Assert.Contains("HostObservabilityAmcW1GuardTests", sot, StringComparison.Ordinal);
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
