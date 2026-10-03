using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-PRODUCTQNA-AMC-001-W1 — Solution Explorer /Modules/ProductQnA/ grouping.</summary>
public sealed class ProductQnAModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void ProductQnA_projects_are_grouped_under_modules_productqna()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/ProductQnA/\">", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/ProductQnA/Tooba.ProductQnA.Domain/Tooba.ProductQnA.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/ProductQnA/Tooba.ProductQnA.Contracts/Tooba.ProductQnA.Contracts.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/ProductQnA/Tooba.ProductQnA.Application/Tooba.ProductQnA.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/ProductQnA/Tooba.ProductQnA.Infrastructure/Tooba.ProductQnA.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/ProductQnA/Tooba.ProductQnA.Endpoints/Tooba.ProductQnA.Endpoints.csproj", slnx, StringComparison.Ordinal);

        Assert.Equal(1, CountOccurrences(slnx, "<Folder Name=\"/Modules/ProductQnA/\">"));
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
