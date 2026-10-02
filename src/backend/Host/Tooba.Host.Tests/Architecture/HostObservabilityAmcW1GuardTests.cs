using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1 — structure/privacy hygiene guard (not certification).
/// </summary>
public sealed class HostObservabilityAmcW1GuardTests
{
    private static readonly string[] ExpectedFiles =
    [
        "RequestObservabilityEnrichmentMiddleware.cs",
    ];

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Inventory|Cart|Persistence)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Observability_exact_one_file_exact_namespace_and_program_order()
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
        Assert.Contains("context.Request.Path.Value", text, StringComparison.Ordinal);
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

        foreach (Match m in ForeignModuleLayer.Matches(text))
            Assert.Fail("foreign module layer: " + m.Value);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Observability;", program, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(program, @"UseMiddleware<RequestObservabilityEnrichmentMiddleware>\(\)"));
        var sessionIdx = program.IndexOf("UseMiddleware<SessionAuthenticationMiddleware>()", StringComparison.Ordinal);
        var obsIdx = program.IndexOf("UseMiddleware<RequestObservabilityEnrichmentMiddleware>()", StringComparison.Ordinal);
        Assert.True(sessionIdx >= 0 && obsIdx > sessionIdx);
    }

    [Fact]
    public void Observability_protected_certifications_present_in_sot()
    {
        var state = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_MESSAGING_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_HEALTH_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("\"hostObservabilityAmc001W1\"", state, StringComparison.Ordinal);
        Assert.DoesNotContain("\"certificationState\": \"HOST_OBSERVABILITY_AMC_CERTIFIED\"", state, StringComparison.Ordinal);
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
