using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ROOT-FINAL-CERT-001 — durable final Host root / Program composition certification.
/// </summary>
public sealed class HostRootFinalCertGuardTests
{
    private static readonly string[] ApprovedRootFiles =
    [
        "Program.cs",
        "Tooba.Host.csproj",
        "appsettings.json",
        "appsettings.Development.json",
        "appsettings.Production.json",
    ];

    private static readonly string[] PlatformDiagnosticRoutes =
    [
        "\"/__platform-error\"",
        "\"/__platform-conflict\"",
        "\"/__platform-commerce\"",
    ];

    [Fact]
    public void Host_root_is_thin_composition_shell_with_hygiene_locked()
    {
        var hostRoot = Dir("src/backend/Host/Tooba.Host");
        Assert.True(Directory.Exists(hostRoot));

        var rootCs = Directory.GetFiles(hostRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "Program.cs" }, rootCs);

        var rootFiles = Directory.GetFiles(hostRoot, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(n => n is not null && !n.EndsWith(".user", StringComparison.OrdinalIgnoreCase))
            .Cast<string>()
            .Where(n => ApprovedRootFiles.Contains(n, StringComparer.OrdinalIgnoreCase)
                || n.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)
                || n.Equals("Program.cs", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        foreach (var required in ApprovedRootFiles)
            Assert.Contains(required, rootFiles, StringComparer.OrdinalIgnoreCase);

        Assert.Empty(Directory.GetFiles(hostRoot, "*.log", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.GetFiles(hostRoot, "*.err.log", SearchOption.TopDirectoryOnly));

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("public partial class Program;", program, StringComparison.Ordinal);
        Assert.Contains("AddOptions<ToobaPlatformOptions>()", program, StringComparison.Ordinal);
        Assert.Contains("ValidateOnStart()", program, StringComparison.Ordinal);
        Assert.Contains("IValidateOptions<ToobaPlatformOptions>, PlatformOptionsValidator", program, StringComparison.Ordinal);
        Assert.Contains("PlatformOptionsValidator.BuildRegistry", program, StringComparison.Ordinal);
        Assert.Contains("IPAddress.Parse(proxy)", program, StringComparison.Ordinal);
        Assert.DoesNotContain("IPAddress.TryParse(proxy", program, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(program, @"using Tooba\.Offer\.Infrastructure\.Adapters\.Tracing;").Count);

        Assert.DoesNotContain(".SaveChanges(", program, StringComparison.Ordinal);
        Assert.DoesNotContain(".SaveChangesAsync(", program, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteSql", program, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet<", program, StringComparison.Ordinal);
        Assert.DoesNotContain("new NpgsqlConnection", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ISender", program, StringComparison.Ordinal);
        Assert.DoesNotContain(".Send(", program, StringComparison.Ordinal);

        Assert.Contains("UseToobaCorrelationId()", program, StringComparison.Ordinal);
        Assert.Contains("UseExceptionHandler()", program, StringComparison.Ordinal);
        Assert.Contains("UseCors(\"ToobaCors\")", program, StringComparison.Ordinal);
        Assert.Contains("SecurityHeadersMiddleware", program, StringComparison.Ordinal);
        Assert.Contains("TenantResolutionMiddleware", program, StringComparison.Ordinal);
        Assert.Contains("SessionAuthenticationMiddleware", program, StringComparison.Ordinal);
        Assert.Contains("RequestObservabilityEnrichmentMiddleware", program, StringComparison.Ordinal);
        Assert.Contains("HostHealthEndpoints.Map", program, StringComparison.Ordinal);

        Assert.Contains("IsDevelopment() || app.Environment.IsEnvironment(\"Testing\")", program, StringComparison.Ordinal);
        foreach (var route in PlatformDiagnosticRoutes)
            Assert.Contains(route, program, StringComparison.Ordinal);

        var diagBlock = program[program.IndexOf("IsDevelopment() || app.Environment.IsEnvironment(\"Testing\")", StringComparison.Ordinal)..];
        Assert.Contains("/__platform-error", diagBlock, StringComparison.Ordinal);
        Assert.Contains("/__platform-conflict", diagBlock, StringComparison.Ordinal);
        Assert.Contains("/__platform-commerce", diagBlock, StringComparison.Ordinal);

        Assert.Contains("if (app.Environment.IsDevelopment())", program, StringComparison.Ordinal);
        Assert.Contains("AddToobaCqrsFoundation(", program, StringComparison.Ordinal);
        Assert.Contains("MapOrderEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapCartEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapOfferModule()", program, StringComparison.Ordinal);

        foreach (var settings in new[]
                 {
                     "src/backend/Host/Tooba.Host/appsettings.json",
                     "src/backend/Host/Tooba.Host/appsettings.Production.json",
                     "src/backend/Host/Tooba.Host/appsettings.Development.json",
                 })
        {
            var json = Read(settings);
            using var doc = JsonDocument.Parse(json);
            var pg = doc.RootElement.GetProperty("Tooba").GetProperty("PostgreSQL");
            Assert.False(pg.TryGetProperty("ConnectionString", out _), settings + " still has ConnectionString");
            Assert.True(pg.TryGetProperty("ConnectionReferences", out _), settings + " missing ConnectionReferences");
        }

        var postgresOptions = Read("src/backend/Host/Tooba.Host/Configuration/PostgreSqlOptions.cs");
        Assert.DoesNotContain("ConnectionString", postgresOptions, StringComparison.Ordinal);
    }

    [Fact]
    public void Sot_records_host_root_final_certification_and_preserves_folder_certs()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_ROOT_FINAL_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_PROGRAM_COMPOSITION_ROOT_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostRootFinalCert001\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_ROOT_FINAL_CERT_001", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_CONFIGURATION_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_CONFIGURATION_PLATFORM_BOUNDARY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_PERSISTENCE_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_OUTBOX_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_OBSERVABILITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MESSAGING_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_HEALTH_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HostRootFinalCertGuardTests", sot, StringComparison.Ordinal);
        Assert.Contains("32719977bc6408490fe5945d75dedaa5c2f7af4c", sot, StringComparison.Ordinal);
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
