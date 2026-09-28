using Microsoft.Extensions.Options;
using Tooba.AccessControl.Contracts.Readiness;
using Tooba.AccessControl.Infrastructure.Adapters;
using Tooba.AccessControl.Infrastructure.Authorization;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001 (DEBT A) — Host readiness must depend only on the
/// narrow neutral AccessControl.Contracts readiness seam. Host keeps ZERO source references to the
/// module authorization infrastructure surface, and the dependency direction stays Host →
/// Contracts (never Host → Infrastructure).
/// </summary>
public sealed class HostReadinessBoundaryGuardTests
{
    [Fact]
    public void Host_readiness_source_has_zero_authorization_infrastructure_coupling()
    {
        var hostRoot = RepoFile("src/backend/Host/Tooba.Host");
        var offenders = Directory.GetFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("Tooba.AccessControl.Infrastructure.Authorization", StringComparison.Ordinal)
                    || text.Contains("SpiceDbAuthorizationOptions", StringComparison.Ordinal)
                    || text.Contains("SpiceDbHealthProbe", StringComparison.Ordinal);
            })
            .Select(Path.GetFileName)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "Host must not reference AccessControl.Infrastructure.Authorization / SpiceDbAuthorizationOptions / SpiceDbHealthProbe: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void Authorization_readiness_seam_lives_in_contracts_without_foreign_layers()
    {
        var contractFile = RepoFile(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Contracts/Readiness/AuthorizationReadinessContracts.cs");
        Assert.True(File.Exists(contractFile), "narrow authorization readiness contract must live in AccessControl.Contracts");
        var text = File.ReadAllText(contractFile);
        Assert.Contains("namespace Tooba.AccessControl.Contracts.Readiness;", text, StringComparison.Ordinal);
        Assert.Contains("IAuthorizationReadinessProbe", text, StringComparison.Ordinal);
        Assert.DoesNotContain("string Endpoint", text, StringComparison.Ordinal);
        Assert.DoesNotContain("string Token", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Authzed", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl.Domain", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl.Infrastructure", text, StringComparison.Ordinal);

        var csproj = File.ReadAllText(RepoFile(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Contracts/Tooba.AccessControl.Contracts.csproj"));
        Assert.DoesNotContain("Application.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Domain.csproj", csproj, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Disabled", null, null, true, "disabled")]
    [InlineData("InMemory", null, null, true, "inmemory")]
    [InlineData("SpiceDb", "", "tok", false, "spicedb-endpoint-missing")]
    [InlineData("SpiceDb", "spicedb:50051", "", false, "spicedb-token-missing")]
    public async Task Readiness_contract_preserves_mode_and_precheck_semantics(
        string mode,
        string? endpoint,
        string? token,
        bool expectedReady,
        string expectedLabel)
    {
        var readiness = Create(
            mode,
            endpoint ?? string.Empty,
            token ?? string.Empty,
            readinessProbeEnabled: true);

        var result = await readiness.EvaluateAsync(CancellationToken.None);

        Assert.Equal(expectedReady, result.Ready);
        Assert.Equal(expectedLabel, result.CheckLabel);
    }

    [Fact]
    public async Task Readiness_probe_disabled_skips_remote_probe_and_stays_ready()
    {
        var readiness = Create(
            "SpiceDb",
            "spicedb.invalid:50051",
            "token-present",
            readinessProbeEnabled: false);

        var result = await readiness.EvaluateAsync(CancellationToken.None);

        Assert.True(result.Ready);
        Assert.Equal("spicedb", result.CheckLabel);
    }

    [Fact]
    public async Task Readiness_probe_enabled_reports_unreachable()
    {
        var readiness = Create(
            "SpiceDb",
            "127.0.0.1:59999",
            "token-present",
            readinessProbeEnabled: true,
            timeoutSeconds: 1);

        var result = await readiness.EvaluateAsync(CancellationToken.None);

        Assert.False(result.Ready);
        Assert.Equal("spicedb-unreachable", result.CheckLabel);
    }

    [Fact]
    public async Task Readiness_result_and_labels_carry_no_secret()
    {
        const string secret = "super-secret-token";
        var readiness = Create("SpiceDb", "127.0.0.1:59999", secret, readinessProbeEnabled: true, timeoutSeconds: 1);

        var result = await readiness.EvaluateAsync(CancellationToken.None);

        Assert.DoesNotContain(secret, result.CheckLabel, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1:59999", result.CheckLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_program_has_no_authorization_infrastructure_import()
    {
        var program = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain(
            "using Tooba.AccessControl.Infrastructure.Authorization;",
            program,
            StringComparison.Ordinal);
        Assert.Contains("HostHealthEndpoints.Map(app", program, StringComparison.Ordinal);
    }

    private static IAuthorizationReadinessProbe Create(
        string mode,
        string endpoint,
        string token,
        bool readinessProbeEnabled,
        int timeoutSeconds = 2) =>
        new AuthorizationReadinessProbe(Options.Create(new SpiceDbAuthorizationOptions
        {
            Mode = mode,
            SpiceDb = new SpiceDbConnectionOptions
            {
                Endpoint = endpoint,
                Token = token,
                UseTls = false,
                TimeoutSeconds = timeoutSeconds,
                ReadinessProbeEnabled = readinessProbeEnabled,
            },
        }));

    private static string RepoFile(string relative) =>
        Path.Combine(RepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
