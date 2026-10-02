using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-HEALTH-AMC-001-W1 — structure/DI/disclosure hygiene guard (not certification).
/// </summary>
public sealed class HostHealthAmcW1GuardTests
{
    private static readonly string[] ExpectedFiles =
    [
        "HostHealthEndpoints.cs",
        "HostReadinessEvaluator.cs",
    ];

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Persistence)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] MachineLabels =
    [
        "ok", "ready", "not-ready", "configured", "unconfigured", "disabled",
        "unhealthy", "bus-unavailable", "postgresql-sql", "n/a", "missing-reference",
    ];

    [Fact]
    public void Health_exact_two_files_exact_namespace_and_routes()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Health");
        Assert.True(Directory.Exists(dir));
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

        Assert.Contains("MapGet(\"/health/live\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/health\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/health/ready\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/ready\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("Results.Json", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiResponseFactory", endpoints, StringComparison.Ordinal);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Health;", program, StringComparison.Ordinal);
        Assert.Contains("HostHealthEndpoints.Map(app, enableCors: true);", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Health_zero_service_locator_explicit_bus_collection_di()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Health");
        foreach (var path in Directory.EnumerateFiles(dir, "*.cs"))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("IServiceProvider", text, StringComparison.Ordinal);
            Assert.DoesNotContain("RequestServices", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetService<IBusControl>", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetRequiredService<IBusControl>", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetService<", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetRequiredService<", text, StringComparison.Ordinal);
        }

        var endpoints = Read("src/backend/Host/Tooba.Host/Health/HostHealthEndpoints.cs");
        var evaluator = Read("src/backend/Host/Tooba.Host/Health/HostReadinessEvaluator.cs");
        Assert.Contains("IEnumerable<IBusControl>", endpoints, StringComparison.Ordinal);
        Assert.Contains("IEnumerable<IBusControl>", evaluator, StringComparison.Ordinal);
    }

    [Fact]
    public void Health_disclosure_sanitized_no_reference_interpolation_no_schema_key()
    {
        var evaluator = Read("src/backend/Host/Tooba.Host/Health/HostReadinessEvaluator.cs");
        Assert.Contains("checks[\"postgresql\"] = \"missing-reference\";", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("missing-reference:{", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("$\"missing-reference:", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("messaging-schema", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("messagingOptions.Schema", evaluator, StringComparison.Ordinal);
        Assert.Contains("checks[\"messaging-transport\"]", evaluator, StringComparison.Ordinal);
        Assert.Contains("\"postgresql-sql\"", evaluator, StringComparison.Ordinal);
    }

    [Fact]
    public void Health_accesscontrol_contracts_only_and_zero_foreign_layers()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Health");
        var violations = new List<string>();
        foreach (var path in Directory.EnumerateFiles(dir, "*.cs"))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("Tooba.AccessControl.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AccessControl.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AccessControl.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Npgsql", text, StringComparison.Ordinal);
            Assert.DoesNotContain("NpgsqlConnection", text, StringComparison.Ordinal);
            if (text.Contains("IAuthorizationReadinessProbe", StringComparison.Ordinal))
            {
                Assert.Contains("Tooba.AccessControl.Contracts.Readiness", text, StringComparison.Ordinal);
            }

            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (ForeignModuleLayer.IsMatch(line) && !line.Contains("Contracts.Readiness", StringComparison.Ordinal))
                    violations.Add(Path.GetFileName(path) + ": " + line);
            }
        }

        Assert.True(violations.Count == 0, string.Join("; ", violations));
    }

    [Fact]
    public void Health_machine_labels_present_user_facing_prose_absent()
    {
        var combined = Read("src/backend/Host/Tooba.Host/Health/HostHealthEndpoints.cs")
            + "\n" + Read("src/backend/Host/Tooba.Host/Health/HostReadinessEvaluator.cs");
        foreach (var label in MachineLabels)
            Assert.Contains(label, combined, StringComparison.Ordinal);

        Assert.DoesNotContain("\"Not Ready\"", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Service Unavailable\"", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("\"آماده نیست\"", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (Exception", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void Health_protected_certifications_untouched_in_production_folders()
    {
        foreach (var folder in new[] { "MultiTenancy", "Errors", "Security", "Admin" })
        {
            var dir = Dir($"src/backend/Host/Tooba.Host/{folder}");
            if (!Directory.Exists(dir))
                continue;
            // W1 must not rewrite those folders; presence alone is enough — no Health-owned edits.
            Assert.True(Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories).Any());
        }

        var state = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", state, StringComparison.Ordinal);
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
