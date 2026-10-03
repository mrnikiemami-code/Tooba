using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-OPERATORPROFILE-AMC-001-W2 — Domain/Application/Infra layer structure without root dumps.</summary>
public sealed class OperatorProfileModuleAmcW2StructureGuardTests
{
    [Fact]
    public void OperatorProfile_layers_are_capability_cohesive_without_root_dumps()
    {
        var root = Repo();
        var domain = Path.Combine(root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Domain");
        var app = Path.Combine(root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Application");
        var infra = Path.Combine(root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Infrastructure");
        var contracts = Path.Combine(root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Contracts");

        Assert.False(File.Exists(Path.Combine(domain, "OperatorProfile.cs")));
        Assert.False(File.Exists(Path.Combine(app, "OperatorProfileContracts.cs")));
        Assert.False(File.Exists(Path.Combine(contracts, "ActorDisplayContracts.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "OperatorProfile.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IOperatorProfileDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Models", "OperatorProfileSnapshot.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Admin", "Commands", "UpsertOperatorProfileCommand.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Admin", "Queries", "GetOperatorProfileQuery.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Profiles", "OperatorProfileDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Adapters", "ActorDisplayLookupAdapter.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "OperatorProfileOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Ports", "ActorDisplayContracts.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "OperatorProfileErrorCodes.cs")));

        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "OperatorProfileModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
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
