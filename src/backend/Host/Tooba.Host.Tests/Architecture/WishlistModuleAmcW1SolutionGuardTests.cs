using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-WISHLIST-AMC-001-W1 — Solution Explorer /Modules/Wishlist/ grouping.</summary>
public sealed class WishlistModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void Wishlist_projects_are_grouped_under_modules_wishlist()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/Wishlist/\">", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Wishlist/Tooba.Wishlist.Domain/Tooba.Wishlist.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Wishlist/Tooba.Wishlist.Contracts/Tooba.Wishlist.Contracts.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Wishlist/Tooba.Wishlist.Application/Tooba.Wishlist.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Wishlist/Tooba.Wishlist.Infrastructure/Tooba.Wishlist.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Wishlist/Tooba.Wishlist.Endpoints/Tooba.Wishlist.Endpoints.csproj", slnx, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(slnx, "<Folder Name=\"/Modules/Wishlist/\">"));
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
