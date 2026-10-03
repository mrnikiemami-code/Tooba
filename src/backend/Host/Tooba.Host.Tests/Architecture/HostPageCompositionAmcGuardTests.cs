using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Durable guards for TB-TMAR-HOST-PAGECOMPOSITION-AMC-001 — Host PageComposition HOST_ZERO.</summary>
public sealed class HostPageCompositionAmcGuardTests
{
    [Fact]
    public void Host_PageComposition_folder_is_absent_and_module_owns_http()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/PageComposition")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapPageCompositionModuleEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPageCompositionEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("AddPageCompositionEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.PageComposition", program, StringComparison.Ordinal);
        Assert.DoesNotContain("PageCompositionPanelComposer", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Endpoints/PageCompositionEndpointModule.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/PageComposition/Tooba.PageComposition.Application/Composition/PageCompositionPresentationComposer.cs")));
    }

    [Fact]
    public void PageComposition_endpoints_do_not_reference_host_or_domain()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/PageComposition/Tooba.PageComposition.Endpoints/Tooba.PageComposition.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.PageComposition.Domain", csproj, StringComparison.Ordinal);

        foreach (var file in EnumerateEndpointSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Host.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.PageComposition.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CurrentAuthenticatedSession", text, StringComparison.Ordinal);
            Assert.DoesNotContain("message.Contains", text, StringComparison.Ordinal);
            Assert.DoesNotContain("یافت نشد", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        }

        var httpErrors = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/PageComposition/Tooba.PageComposition.Endpoints/PageCompositionHttpErrors.cs"));
        Assert.Contains("ApiResponseFactory", httpErrors, StringComparison.Ordinal);
        Assert.Contains("FromSemanticException", httpErrors, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_hostPageCompositionAmc_present()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostPageCompositionAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-PAGECOMPOSITION-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_PAGECOMPOSITION_AMC_001_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateEndpointSources(string root) =>
        Directory.EnumerateFiles(
                Path.Combine(root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Endpoints"),
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
