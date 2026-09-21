using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Fulfillment.Infrastructure.Gateways;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-TMAR-HOST-W6 — characterization for ShippingService write via CQRS Directory.</summary>
public sealed class ShippingServiceAdminTests
{
    private static readonly Guid LangA = Guid.Parse("01900000-0000-7000-8000-00000000cc01");
    private static readonly Guid LangUnknown = Guid.Parse("01900000-0000-7000-8000-00000000cc99");

    [Fact]
    public async Task Create_persists_service_translation_and_option()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA]);
        var id = await sender.Send(new CreateShippingServiceCommand(Model("post", LangA)), CancellationToken.None);
        var service = await db.ShippingServices.SingleAsync();
        Assert.Equal(id, service.ShippingServiceId);
        Assert.Equal("post", service.Code);
        Assert.Single(db.ShippingServiceTranslations);
        Assert.Single(db.ShippingServiceOptions);
        Assert.Single(db.ShippingServiceOptionTranslations);
    }

    [Fact]
    public async Task Duplicate_code_is_rejected()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA]);
        await sender.Send(new CreateShippingServiceCommand(Model("post", LangA)), CancellationToken.None);
        var ex = await Assert.ThrowsAsync<PlatformHttpException>(
            () => sender.Send(new CreateShippingServiceCommand(Model("POST", LangA)), CancellationToken.None));
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("shipping_service.code_duplicate", ex.ErrorCode);
    }

    [Fact]
    public async Task Unknown_language_is_rejected()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA]);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new CreateShippingServiceCommand(Model("tipax", LangUnknown)), CancellationToken.None));
        Assert.Equal("shipping_service.language_invalid", ex.Message);
        Assert.Empty(db.ShippingServices);
    }

    [Fact]
    public async Task Update_and_deactivate()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA]);
        var id = await sender.Send(new CreateShippingServiceCommand(Model("courier", LangA)), CancellationToken.None);
        await sender.Send(new UpdateShippingServiceCommand(id, Model("store_courier", LangA, sort: 40)), CancellationToken.None);
        var service = await db.ShippingServices.SingleAsync();
        Assert.Equal("store_courier", service.Code);
        Assert.Equal(40, service.SortOrder);
        await sender.Send(new DeactivateShippingServiceCommand(id), CancellationToken.None);
        Assert.False((await db.ShippingServices.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task Ensure_seed_is_idempotent_when_empty_then_filled()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA], seedLangs:
        [
            new ShippingServiceSeedLanguage(LangA, "fa", "fa-IR", true),
        ]);
        await sender.Send(new EnsureShippingCatalogSeedCommand(), CancellationToken.None);
        Assert.Equal(5, await db.ShippingServices.CountAsync());
        await sender.Send(new EnsureShippingCatalogSeedCommand(), CancellationToken.None);
        Assert.Equal(5, await db.ShippingServices.CountAsync());
    }

    [Fact]
    public void Admin_write_endpoints_use_cqrs_without_savechanges()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin/ShippingServiceEndpoints.cs"));
        Assert.Contains("CreateShippingServiceCommand", source, StringComparison.Ordinal);
        Assert.Contains("UpdateShippingServiceCommand", source, StringComparison.Ordinal);
        Assert.Contains("DeactivateShippingServiceCommand", source, StringComparison.Ordinal);
        Assert.Contains("EnsureShippingCatalogSeedCommand", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Add(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RemoveRange", source, StringComparison.Ordinal);
    }

    private static ShippingServiceWriteModel Model(string code, Guid lang, int sort = 10) =>
        new(
            code,
            code,
            "truck",
            "blue",
            true,
            sort,
            [new ShippingServiceTranslationWriteModel(lang, code, null)],
            [
                new ShippingServiceOptionWriteModel(
                    null,
                    "express",
                    true,
                    10,
                    [new ShippingServiceOptionTranslationWriteModel(lang, "Express")]),
            ]);

    private static FulfillmentDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new FulfillmentDbContext(options);
    }

    private static ISender CreateSender(
        FulfillmentDbContext db,
        IReadOnlyList<Guid> known,
        IReadOnlyList<ShippingServiceSeedLanguage>? seedLangs = null)
    {
        var gate = new FixedLanguageGate(known, seedLangs);
        var directory = new ShippingServiceDirectory(db, new SystemUtcClock(), new UuidV7IdGenerator(), gate);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IShippingServiceDirectory>(directory);
        services.AddSingleton<IShippingServiceLanguageGate>(gate);
        services.AddValidatorsFromAssembly(typeof(CreateShippingServiceCommand).Assembly);
        services.AddToobaCqrsFoundation(typeof(CreateShippingServiceCommand).Assembly);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs/architecture/TOOBA-LOCKS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class FixedLanguageGate : IShippingServiceLanguageGate
    {
        private readonly HashSet<Guid> _known;
        private readonly IReadOnlyList<ShippingServiceSeedLanguage> _seed;

        public FixedLanguageGate(IReadOnlyList<Guid> known, IReadOnlyList<ShippingServiceSeedLanguage>? seed)
        {
            _known = known.ToHashSet();
            _seed = seed ?? known.Select(id => new ShippingServiceSeedLanguage(id, "fa", "fa-IR", true)).ToList();
        }

        public Task EnsureKnownAsync(IReadOnlyList<Guid> languageIds, CancellationToken cancellationToken)
        {
            if (languageIds.Any(id => !_known.Contains(id)))
            {
                throw new InvalidOperationException("shipping_service.language_invalid");
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ShippingServiceSeedLanguage>> ListForSeedAsync(CancellationToken cancellationToken)
            => Task.FromResult(_seed);
    }
}
