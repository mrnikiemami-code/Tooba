using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-LOCALIZATION-AMC-001-W1 — Solution Explorer /Modules/Localization/ grouping.</summary>
public sealed class LocalizationModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void Localization_projects_are_grouped_under_modules_localization()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/Localization/", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Localization/Tooba.Localization.Domain/Tooba.Localization.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Localization/Tooba.Localization.Contracts/Tooba.Localization.Contracts.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Localization/Tooba.Localization.Application/Tooba.Localization.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Localization/Tooba.Localization.Infrastructure/Tooba.Localization.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Localization/Tooba.Localization.Endpoints/Tooba.Localization.Endpoints.csproj", slnx, StringComparison.Ordinal);

        // Must not remain as flat /Modules/ siblings without the Localization folder entry.
        var flatOnly = slnx.Contains("<Folder Name=\"/Modules/\">", StringComparison.Ordinal)
            && !slnx.Contains("<Folder Name=\"/Modules/Localization/\">", StringComparison.Ordinal);
        Assert.False(flatOnly);
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
