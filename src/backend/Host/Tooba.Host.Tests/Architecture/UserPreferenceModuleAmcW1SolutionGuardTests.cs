using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-USERPREFERENCE-AMC-001-W1 — Solution Explorer /Modules/UserPreference/ grouping.</summary>
public sealed class UserPreferenceModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void UserPreference_projects_are_grouped_under_modules_userpreference()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/UserPreference/\">", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/UserPreference/Tooba.UserPreference.Domain/Tooba.UserPreference.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/UserPreference/Tooba.UserPreference.Contracts/Tooba.UserPreference.Contracts.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/UserPreference/Tooba.UserPreference.Application/Tooba.UserPreference.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/UserPreference/Tooba.UserPreference.Infrastructure/Tooba.UserPreference.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/UserPreference/Tooba.UserPreference.Endpoints/Tooba.UserPreference.Endpoints.csproj", slnx, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(slnx, "<Folder Name=\"/Modules/UserPreference/\">"));
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
