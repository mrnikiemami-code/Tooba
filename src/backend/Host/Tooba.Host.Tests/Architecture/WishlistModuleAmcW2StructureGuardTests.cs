using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-WISHLIST-AMC-001-W2 — Domain/Application/Infra layer structure without root dumps.</summary>
public sealed class WishlistModuleAmcW2StructureGuardTests
{
    [Fact]
    public void Wishlist_layers_are_capability_cohesive_without_root_dumps()
    {
        var root = Repo();
        var domain = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Domain");
        var app = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Application");
        var infra = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Infrastructure");
        var contracts = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Contracts");

        Assert.False(File.Exists(Path.Combine(domain, "WishlistItem.cs")));
        Assert.False(File.Exists(Path.Combine(contracts, "IWishlistCountPort.cs")));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.False(Directory.Exists(Path.Combine(app, "Presentation")));
        Assert.False(Directory.Exists(Path.Combine(app, "Validators")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.False(File.Exists(Path.Combine(infra, "WishlistDirectory.cs")));

        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "WishlistItem.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IWishlistDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Models", "WishlistModels.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Composition", "WishlistPresentationComposer.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Customer", "Commands", "AddWishlistItemCommand.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Customer", "Queries", "ListWishlistPageQuery.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Customer", "Validators", "WishlistValidators.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "WishlistDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "Migrations", "20260825162434_InitialWishlist.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Ports", "IWishlistCountPort.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "WishlistErrorCodes.cs")));

        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "WishlistModule.cs" },
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
