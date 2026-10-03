using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-WISHLIST-AMC-001-W3 — Result pipeline, Contracts catalog, thin endpoints.</summary>
public sealed class WishlistModuleAmcW3CqrsGuardTests
{
    [Fact]
    public void Wishlist_w3_owns_catalog_in_contracts_and_result_pipeline()
    {
        var root = Repo();
        var contracts = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Contracts");
        var app = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Application");
        var endpoints = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Endpoints");
        var infra = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Infrastructure");

        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "WishlistErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "WishlistErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Resources", "WishlistErrors.resx")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.True(File.Exists(Path.Combine(app, "Composition", "WishlistOperation.cs")));

        var module = File.ReadAllText(Path.Combine(infra, "WishlistModule.cs"));
        Assert.Contains("WishlistErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("WishlistErrorResourceSet", module, StringComparison.Ordinal);

        foreach (var file in Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (SemanticException", text, StringComparison.Ordinal);
        }

        var validators = File.ReadAllText(Path.Combine(app, "Customer", "Validators", "WishlistValidators.cs"));
        Assert.Contains("WishlistErrorCodes.", validators, StringComparison.Ordinal);
        Assert.DoesNotContain("\"customer.wishlist.", validators, StringComparison.Ordinal);

        var add = File.ReadAllText(Path.Combine(app, "Customer", "Commands", "AddWishlistItemCommand.cs"));
        Assert.Contains("IRequest<Result<", add, StringComparison.Ordinal);
        Assert.Contains("WishlistOperation.ExecuteAsync", add, StringComparison.Ordinal);

        var remove = File.ReadAllText(Path.Combine(app, "Customer", "Commands", "RemoveWishlistItemCommand.cs"));
        Assert.Contains("IRequest<Result>", remove, StringComparison.Ordinal);
        Assert.Contains("WishlistOperation.ExecuteAsync", remove, StringComparison.Ordinal);
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
