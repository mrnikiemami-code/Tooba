using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-PARTY-AMC-001-W1 — Solution Explorer /Modules/Party/ grouping.</summary>
public sealed class PartyModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void Party_projects_are_grouped_under_modules_party()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/Party/\">", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Party/Tooba.Party.Domain/Tooba.Party.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Party/Tooba.Party.Contracts/Tooba.Party.Contracts.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Party/Tooba.Party.Application/Tooba.Party.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Party/Tooba.Party.Infrastructure/Tooba.Party.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Party/Tooba.Party.Endpoints/Tooba.Party.Endpoints.csproj", slnx, StringComparison.Ordinal);

        // Exactly one folder entry for Party projects under /Modules/Party/.
        Assert.Equal(1, CountOccurrences(slnx, "<Folder Name=\"/Modules/Party/\">"));
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
