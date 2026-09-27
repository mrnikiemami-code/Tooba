using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-CONTENT-AMC-001 — Host/Content production ZERO + Content.Endpoints wiring.
/// </summary>
public sealed class HostContentAmcGuardTests
{
    [Fact]
    public void Host_Content_folder_has_zero_csharp_files()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Content");
        if (!Directory.Exists(folder))
        {
            return;
        }

        var files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
        Assert.Empty(files);
    }

    [Fact]
    public void Program_maps_Content_module_endpoints_once()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("MapContentModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapContentEndpoints();", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapContentCategoryEndpoints();", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapContentAuthorEndpoints();", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapContentTagEndpoints();", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapContentArticleMediaEndpoints();", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapContentArticleCommentEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains("AddContentEndpointPresentation()", program, StringComparison.Ordinal);
        Assert.True(
            Regex.IsMatch(program, @"using\s+Tooba\.Content\.Endpoints\s*;", RegexOptions.CultureInvariant),
            "Program must import Tooba.Content.Endpoints");
    }

    [Fact]
    public void Content_Endpoints_project_exists()
    {
        var csproj = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Content",
            "Tooba.Content.Endpoints",
            "Tooba.Content.Endpoints.csproj");
        Assert.True(File.Exists(csproj), $"missing {csproj}");
        var module = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Content",
            "Tooba.Content.Endpoints",
            "ContentEndpointModule.cs");
        Assert.True(File.Exists(module));
        var text = File.ReadAllText(module);
        Assert.Contains("MapContentModuleEndpoints", text, StringComparison.Ordinal);
        Assert.Contains("AddContentEndpointPresentation", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_grid_engines_live_in_Infrastructure_not_Host()
    {
        var hostGrid = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Grid");
        Assert.False(File.Exists(Path.Combine(hostGrid, "AdminContentGridQueryEngine.cs")));
        Assert.False(File.Exists(Path.Combine(hostGrid, "AdminContentAuthorGridQueryEngine.cs")));

        var infraGrid = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Content",
            "Tooba.Content.Infrastructure",
            "Grid");
        Assert.True(File.Exists(Path.Combine(infraGrid, "AdminContentGridQueryEngine.cs")));
        Assert.True(File.Exists(Path.Combine(infraGrid, "AdminContentAuthorGridQueryEngine.cs")));
    }

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
