using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-USERPREFERENCE-AMC-001-W2 — Domain/Application/Infra layer structure without root dumps.</summary>
public sealed class UserPreferenceModuleAmcW2StructureGuardTests
{
    [Fact]
    public void UserPreference_layers_are_capability_cohesive_without_root_dumps()
    {
        var root = Repo();
        var domain = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Domain");
        var app = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Application");
        var infra = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Infrastructure");
        var contracts = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Contracts");

        Assert.False(File.Exists(Path.Combine(domain, "UserPreference.cs")));
        Assert.False(File.Exists(Path.Combine(domain, "UiPreference.cs")));
        Assert.False(File.Exists(Path.Combine(app, "UserPreferenceContracts.cs")));
        Assert.False(File.Exists(Path.Combine(app, "UserPreferenceShapes.cs")));
        Assert.False(File.Exists(Path.Combine(infra, "UserPreferenceDirectory.cs")));
        Assert.False(File.Exists(Path.Combine(infra, "UiPreferenceDirectory.cs")));

        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "UserPreference.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "UiPreference.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IUserPreferenceDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IUiPreferenceDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Models", "UserPreferenceModels.cs")));
        Assert.True(File.Exists(Path.Combine(app, "LocalePreferences", "Commands", "UpsertUserPreferenceCommand.cs")));
        Assert.True(File.Exists(Path.Combine(app, "LocalePreferences", "Validators", "UpsertUserPreferenceCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(app, "UiPreferences", "Commands", "UpsertUiPreferenceCommand.cs")));
        Assert.True(File.Exists(Path.Combine(app, "UiPreferences", "Validators", "UpsertUiPreferenceCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "UserPreferenceDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "UiPreferenceDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "UserPreferenceOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "UserPreferenceErrorCodes.cs")));

        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "UserPreferenceModule.cs" },
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
