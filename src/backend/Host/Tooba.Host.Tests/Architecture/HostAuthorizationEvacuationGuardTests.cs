using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-AUTHORIZATION-AMC-001 — SpiceDB مجوز مالکیت ماژول AccessControl را دارد و Host دیگر هیچ
/// surface مجوز/SpiceDB ندارد. تنها استثنا: میزبان دسترسی لازم برای پیکربندی/readiness را مصرف می‌کند.
/// </summary>
public sealed class HostAuthorizationEvacuationGuardTests
{
    [Fact]
    public void Host_authorization_folder_is_evacuated()
    {
        Assert.False(
            Directory.Exists(RepoFile("src/backend/Host/Tooba.Host/Authorization")),
            "Host/Authorization must be fully evacuated into Tooba.AccessControl.Infrastructure/Authorization");
        Assert.False(
            File.Exists(RepoFile("src/backend/Host/Tooba.Host/Health/SpiceDbHealthProbe.cs")),
            "SpiceDB readiness probe is part of the authorization slice and must not live in Host/Health");
    }

    [Fact]
    public void Module_owns_the_spicedb_adapter_slice_with_exact_paths()
    {
        var slice = RepoFile("src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Authorization");
        foreach (var expected in new[]
                 {
                     "AuthorizationAdapters.cs",
                     "AuthorizationInstrumentation.cs",
                     "AuthorizationRegistration.cs",
                     "SpiceDbAuthorizationAdapter.cs",
                     "SpiceDbAuthorizationBootstrapper.cs",
                     "SpiceDbAuthorizationOptions.cs",
                     "authorization-foundation.zed",
                 })
        {
            Assert.True(File.Exists(Path.Combine(slice, expected)), $"missing module authorization file {expected}");
        }

        Assert.Equal(7, Directory.GetFiles(slice, "*", SearchOption.TopDirectoryOnly).Length);
    }

    [Fact]
    public void Host_has_no_spicedb_sdk_or_authorization_ngrpc_code()
    {
        var hostRoot = RepoFile("src/backend/Host/Tooba.Host");
        var offenders = Directory.GetFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("Authzed.Api.V1", StringComparison.Ordinal)
                    || text.Contains("SpiceDbAuthorizationAdapter", StringComparison.Ordinal);
            })
            .ToArray();
        Assert.True(offenders.Length == 0, "Host must not own SpiceDB adapter code: " + string.Join(", ", offenders));

        var hostProject = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Tooba.Host.csproj"));
        Assert.DoesNotContain("Authzed.Net", hostProject, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_composition_binds_the_module_authorization_slice()
    {
        var program = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("using Tooba.AccessControl.Infrastructure.Authorization;", program, StringComparison.Ordinal);
        Assert.Contains("AddToobaModules", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddToobaAuthorization()", program, StringComparison.Ordinal);

        var module = File.ReadAllText(RepoFile(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlModule.cs"));
        Assert.Contains("AddToobaAuthorization()", module, StringComparison.Ordinal);
        Assert.Contains("SpiceDbAuthorizationOptions.SectionName", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Authorization_configuration_section_and_modes_are_preserved()
    {
        var options = File.ReadAllText(RepoFile(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Authorization/SpiceDbAuthorizationOptions.cs"));
        Assert.Contains("Tooba:Authorization", options, StringComparison.Ordinal);
        Assert.Contains("\"Disabled\"", options, StringComparison.Ordinal);
        Assert.Contains("\"InMemory\"", options, StringComparison.Ordinal);
        Assert.Contains("\"SpiceDb\"", options, StringComparison.Ordinal);
        Assert.Contains("InMemory authorization is not allowed in Production.", options, StringComparison.Ordinal);
        Assert.Contains("SpiceDB TLS must be enabled in Production.", options, StringComparison.Ordinal);
    }

    [Fact]
    public void Schema_version_and_fail_closed_contract_are_preserved()
    {
        var adapters = File.ReadAllText(RepoFile(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Authorization/AuthorizationAdapters.cs"));
        Assert.Contains("public int SchemaVersion => 3;", adapters, StringComparison.Ordinal);
        Assert.Contains("definition capability", adapters, StringComparison.Ordinal);
        Assert.Contains("definition category", adapters, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecision.Unavailable(_reason)", adapters, StringComparison.Ordinal);
    }

    [Fact]
    public void Adapter_does_not_classify_failures_by_exception_message()
    {
        var adapter = File.ReadAllText(RepoFile(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Authorization/SpiceDbAuthorizationAdapter.cs"));
        Assert.DoesNotContain("ex.Message ==", adapter, StringComparison.Ordinal);
        Assert.Contains("SpiceDbUnavailableException", adapter, StringComparison.Ordinal);
    }

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
