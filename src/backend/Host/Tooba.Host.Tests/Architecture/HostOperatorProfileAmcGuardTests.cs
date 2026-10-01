using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Durable guards for TB-TMAR-HOST-OPERATORPROFILE-AMC-001 / R1 — Host OperatorProfile HOST_ZERO.</summary>
public sealed class HostOperatorProfileAmcGuardTests
{
    [Fact]
    public void Host_OperatorProfile_folder_is_absent_and_module_owns_http()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/OperatorProfile")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapOperatorProfileModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddOperatorProfileEndpointPresentation", program, StringComparison.Ordinal);
        Assert.Contains("HostOperatorProfileAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapOperatorProfileEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.OperatorProfile", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Endpoints/OperatorProfileEndpointModule.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/Access/Authorizers/HostOperatorProfileAdminAuthorizer.cs")));
    }

    [Fact]
    public void OperatorProfile_endpoints_boundaries_are_clean()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Endpoints/Tooba.OperatorProfile.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.OperatorProfile.Domain", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.OperatorProfile.Infrastructure", csproj, StringComparison.Ordinal);

        foreach (var file in EnumerateEndpointSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Host.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.OperatorProfile.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.OperatorProfile.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("title = ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("message.Contains", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (InvalidOperationException)", text, StringComparison.Ordinal);
        }

        var directory = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Infrastructure/OperatorProfileDirectory.cs"));
        Assert.DoesNotContain("catch (InvalidOperationException)", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", directory, StringComparison.Ordinal);
        Assert.Contains("OperatorProfileErrorCodes.ProfileRejected", directory, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_hostOperatorProfileAmc_r1_present()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostOperatorProfileAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostOperatorProfileAmcR1\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-OPERATORPROFILE-AMC-001-R1", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_OPERATORPROFILE_AMC_001_R1_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
        Assert.Contains("OPERATORPROFILE_CLOSED_HOST_ZERO_R1_USER_REVIEW_REQUIRED", sot, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateEndpointSources(string root) =>
        Directory.EnumerateFiles(
                Path.Combine(root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Endpoints"),
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
