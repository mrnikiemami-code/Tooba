using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-PAGECOMPOSITION-AMC-001-W1 — Solution Explorer /Modules/PageComposition/ grouping.</summary>
public sealed class PageCompositionModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void PageComposition_projects_are_grouped_under_modules_pagecomposition()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/PageComposition/\">", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/PageComposition/Tooba.PageComposition.Domain/Tooba.PageComposition.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/PageComposition/Tooba.PageComposition.Contracts/Tooba.PageComposition.Contracts.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/PageComposition/Tooba.PageComposition.Application/Tooba.PageComposition.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/PageComposition/Tooba.PageComposition.Infrastructure/Tooba.PageComposition.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/PageComposition/Tooba.PageComposition.Endpoints/Tooba.PageComposition.Endpoints.csproj", slnx, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(slnx, "<Folder Name=\"/Modules/PageComposition/\">"));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static string Repo()
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
