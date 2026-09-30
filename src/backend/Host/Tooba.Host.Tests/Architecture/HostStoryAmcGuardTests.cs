using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Durable guards for TB-TMAR-HOST-STORY-AMC-001 / R1 — Host Story HOST_ZERO + Endpoints boundary.</summary>
public sealed class HostStoryAmcGuardTests
{
    [Fact]
    public void Host_Story_folder_is_absent_and_module_owns_http()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Story")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapStoryModuleEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapStoryEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("AddStoryEndpointPresentation", program, StringComparison.Ordinal);
        Assert.Contains("HostStorySellerAuthorizer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Story", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Story/Tooba.Story.Endpoints/StoryEndpointModule.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Story/Tooba.Story.Application/Presentation/StoryPresentationComposer.cs")));
    }

    [Fact]
    public void Story_endpoints_do_not_reference_host_access_helpers()
    {
        var root = FindRepoRoot();
        foreach (var file in EnumerateStoryEndpointSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Host.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SellerPanelAccess.RequireAuthorizedAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CurrentAuthenticatedSession", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Story_endpoints_do_not_reference_domain_or_message_classify_errors()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Story/Tooba.Story.Endpoints/Tooba.Story.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Story.Domain", csproj, StringComparison.Ordinal);

        foreach (var file in EnumerateStoryEndpointSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Story.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("StoryReviewStatus", text, StringComparison.Ordinal);
            Assert.DoesNotContain("message.Contains", text, StringComparison.Ordinal);
            Assert.DoesNotContain("یافت نشد", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ناامن", text, StringComparison.Ordinal);
        }

        var httpErrors = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Story/Tooba.Story.Endpoints/StoryHttpErrors.cs"));
        Assert.Contains("ApiResponseFactory", httpErrors, StringComparison.Ordinal);
        Assert.Contains("FromSemanticException", httpErrors, StringComparison.Ordinal);

        var admin = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Story/Tooba.Story.Endpoints/Admin/StoryAdminEndpoints.cs"));
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("string? reviewStatus", admin, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_hostStoryAmc_present()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostStoryAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-STORY-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostStoryAmcR1\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-STORY-AMC-001-R1", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_STORY_AMC_001_R1_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateStoryEndpointSources(string root) =>
        Directory.EnumerateFiles(
                Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Endpoints"),
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
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
