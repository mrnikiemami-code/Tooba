using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-PAGECOMPOSITION-AMC-001-W3 — Result pipeline, Contracts catalog, no message parse.</summary>
public sealed class PageCompositionModuleAmcW3CqrsGuardTests
{
    [Fact]
    public void PageComposition_w3_owns_catalog_in_contracts_and_result_pipeline()
    {
        var root = Repo();
        var contracts = Path.Combine(root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Contracts");
        var app = Path.Combine(root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Application");
        var endpoints = Path.Combine(root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Endpoints");
        var infra = Path.Combine(root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Infrastructure");
        var domain = Path.Combine(root, "src/backend/Modules/PageComposition/Tooba.PageComposition.Domain");

        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "PageCompositionErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "PageCompositionErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Resources", "PageCompositionErrors.resx")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.False(File.Exists(Path.Combine(endpoints, "PageCompositionHttpErrors.cs")));
        Assert.False(File.Exists(Path.Combine(app, "Composition", "PageCompositionFailureMapper.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Composition", "PageCompositionOperation.cs")));

        var module = File.ReadAllText(Path.Combine(infra, "PageCompositionModule.cs"));
        Assert.Contains("PageCompositionErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("PageCompositionErrorResourceSet", module, StringComparison.Ordinal);

        foreach (var file in Directory.EnumerateFiles(domain, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("InvalidOperationException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("message.Contains", text, StringComparison.Ordinal);
        }

        foreach (var file in Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (SemanticException", text, StringComparison.Ordinal);
        }

        Assert.True(File.Exists(Path.Combine(app, "Admin", "Validators", "PageCompositionValidators.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Storefront", "Validators", "StorefrontCompositionValidators.cs")));
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
