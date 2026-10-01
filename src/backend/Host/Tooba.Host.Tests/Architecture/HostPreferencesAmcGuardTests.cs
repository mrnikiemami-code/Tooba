using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Durable guards for TB-TMAR-HOST-PREFERENCES-AMC-001 — Host Preferences HOST_ZERO.</summary>
public sealed class HostPreferencesAmcGuardTests
{
    [Fact]
    public void Host_Preferences_folder_is_absent_and_module_owns_http()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Preferences")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapUserPreferenceModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddUserPreferenceEndpointPresentation", program, StringComparison.Ordinal);
        Assert.Contains("HostUserPreferenceAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapUserPreferenceEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapUiPreferenceEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Preferences", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints/UserPreferenceEndpointModule.cs")));
    }

    [Fact]
    public void Preferences_endpoints_boundaries_are_clean()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints/Tooba.UserPreference.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.UserPreference.Domain", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.UserPreference.Infrastructure", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Application", csproj, StringComparison.Ordinal);

        foreach (var file in EnumerateEndpointSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Host.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.UserPreference.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Order.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("title = ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("message.Contains", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SoT_hostPreferencesAmc_present()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostPreferencesAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-PREFERENCES-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_PREFERENCES_AMC_001_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateEndpointSources(string root) =>
        Directory.EnumerateFiles(
                Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(file =>
                !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
