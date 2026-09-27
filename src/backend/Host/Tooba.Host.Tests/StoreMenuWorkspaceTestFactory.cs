using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Tests;

/// <summary>ساخت StoreMenuWorkspace با MediatR + Directory.</summary>
internal static class StoreMenuWorkspaceTestFactory
{
    /// <summary>Workspace و Catalog در-حافظه.</summary>
    public static StoreMenuWorkspace Create(out CatalogDbContext catalog)
    {
        catalog = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var commerce = new FixedMenuCommerce(OutboxTestContextFactory.SingleStore("store-a", "conn-a"));
        var directory = new StoreMenuDirectory(catalog, new SystemUtcClock());
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IStoreMenuDirectory>(directory);
        services.AddValidatorsFromAssembly(typeof(CreateStoreMenuCommand).Assembly);
        services.AddToobaCqrsFoundation(typeof(CreateStoreMenuCommand).Assembly);
        var provider = services.BuildServiceProvider();
        return new StoreMenuWorkspace(
            catalog,
            commerce,
            new MemoryCache(new MemoryCacheOptions()),
            provider.GetRequiredService<ISender>());
    }

    private sealed class FixedMenuCommerce : ICurrentCommerceContext
    {
        public FixedMenuCommerce(CommerceContext current) => Current = current;

        public CommerceContext? Current { get; }
    }
}
