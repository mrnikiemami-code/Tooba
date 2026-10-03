using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-OPERATORPROFILE-AMC-001-W1 — Solution Explorer /Modules/OperatorProfile/ grouping.</summary>
public sealed class OperatorProfileModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void OperatorProfile_projects_are_grouped_under_modules_operatorprofile()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/OperatorProfile/", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/OperatorProfile/Tooba.OperatorProfile.Domain/Tooba.OperatorProfile.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/OperatorProfile/Tooba.OperatorProfile.Contracts/Tooba.OperatorProfile.Contracts.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/OperatorProfile/Tooba.OperatorProfile.Application/Tooba.OperatorProfile.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/OperatorProfile/Tooba.OperatorProfile.Infrastructure/Tooba.OperatorProfile.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/OperatorProfile/Tooba.OperatorProfile.Endpoints/Tooba.OperatorProfile.Endpoints.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("<Folder Name=\"/Modules/OperatorProfile/\">", slnx, StringComparison.Ordinal);
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
