using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-LOCALIZATION-AMC-001-W2 — Domain/Application/Infra layer structure without root dumps.</summary>
public sealed class LocalizationModuleAmcW2StructureGuardTests
{
    [Fact]
    public void Localization_layers_are_capability_cohesive_without_root_dumps()
    {
        var root = Repo();
        var domain = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Domain");
        var app = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Application");
        var infra = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Infrastructure");
        var contracts = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Contracts");

        Assert.False(File.Exists(Path.Combine(domain, "Language.cs")));
        Assert.False(File.Exists(Path.Combine(app, "LanguageContracts.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "Language.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Enums", "LanguageDirection.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Enums", "LanguageCalendarPolicy.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "ILanguageDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Models", "LanguageSnapshot.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Composition", "LanguageMappings.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Languages", "LanguageDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Adapters", "LanguageActivationBridge.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Adapters", "LanguageLookupBridge.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Bootstrap", "LanguageBootstrapHostedService.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "LocalizationOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Ports", "ILanguageActivationPort.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "LanguageErrorCodes.cs")));

        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "LocalizationModule.cs" },
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
