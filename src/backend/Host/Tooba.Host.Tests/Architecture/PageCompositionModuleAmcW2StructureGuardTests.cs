using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-PAGECOMPOSITION-AMC-001-W2 — Domain/Application/Infra layer structure without root dumps.</summary>
public sealed class PageCompositionModuleAmcW2StructureGuardTests
{
    [Fact]
    public void PageComposition_layers_are_capability_cohesive_without_root_dumps()
    {
        var root = Repo();
        var domain = Path.Combine(root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Domain");
        var app = Path.Combine(root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Application");
        var infra = Path.Combine(root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Infrastructure");
        var contracts = Path.Combine(root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Contracts");

        Assert.False(File.Exists(Path.Combine(domain, "PageCompositionEntities.cs")));
        Assert.False(File.Exists(Path.Combine(app, "PageCompositionContracts.cs")));
        Assert.False(File.Exists(Path.Combine(app, "PageCompositionFailureMapper.cs")));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.False(Directory.Exists(Path.Combine(app, "Validators")));
        Assert.False(Directory.Exists(Path.Combine(app, "Presentation")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.False(File.Exists(Path.Combine(infra, "PageCompositionDirectory.cs")));
        Assert.False(File.Exists(Path.Combine(infra, "PageCompositionDevelopmentSeed.cs")));

        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "PageDefinition.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "PageSection.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Catalog", "SectionCatalog.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Constants", "PageKeys.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IPageCompositionDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Models", "PageCompositionModels.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Admin", "Commands", "AdminHomeCompositionCommands.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Admin", "Validators", "PageCompositionValidators.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Storefront", "Queries", "HomeCompositionQueries.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Composition", "PageCompositionPresentationComposer.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "PageCompositionDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Development", "PageCompositionDevelopmentSeed.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "Migrations", "20260827021507_InitialPageComposition.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "PageCompositionErrorCodes.cs")));

        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "PageCompositionModule.cs" },
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
