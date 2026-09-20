using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;

namespace Tooba.Host.Tests;

/// <summary>ساخت StoreMenuComposer با MediatR + Directory.</summary>
internal static class StoreMenuComposerTestFactory
{
    /// <summary>Composer و Catalog در-حافظه.</summary>
    public static StoreMenuComposer Create(out CatalogDbContext catalog)
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
        return new StoreMenuComposer(catalog, commerce, new MemoryCache(new MemoryCacheOptions()), provider.GetRequiredService<ISender>());
    }

    private sealed class FixedMenuCommerce : ICurrentCommerceContext
    {
        public FixedMenuCommerce(CommerceContext current) => Current = current;

        public CommerceContext? Current { get; }
    }
}
