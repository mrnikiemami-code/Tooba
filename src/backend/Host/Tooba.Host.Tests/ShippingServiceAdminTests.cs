using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Application.Commands.CreateShippingService;
using Tooba.Fulfillment.Application.Commands.UpdateShippingService;
using Tooba.Fulfillment.Application.Commands.DeactivateShippingService;
using Tooba.Fulfillment.Application.Commands.EnsureShippingCatalogSeed;
using Tooba.Fulfillment.Application.Queries.ListShippingServices;
using Tooba.Fulfillment.Application.Queries.GetShippingService;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Localization.Contracts;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-TMAR-NEXT-MODULE-BATCH-004-R2 — ShippingService Application CQRS + Host thin transport.</summary>
public sealed class ShippingServiceAdminTests
{
    private static readonly Guid LangA = Guid.Parse("01900000-0000-7000-8000-00000000cc01");
    private static readonly Guid LangUnknown = Guid.Parse("01900000-0000-7000-8000-00000000cc99");

    [Fact]
    public async Task Create_persists_service_translation_and_option()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA]);
        var result = await sender.Send(new CreateShippingServiceCommand(Model("post", LangA)), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var service = await db.ShippingServices.SingleAsync();
        Assert.Equal(result.Value.ShippingServiceId, service.ShippingServiceId);
        Assert.Equal("post", service.Code);
        Assert.Single(db.ShippingServiceTranslations);
        Assert.Single(db.ShippingServiceOptions);
        Assert.Single(db.ShippingServiceOptionTranslations);
    }

    [Fact]
    public async Task Duplicate_code_is_rejected_as_semantic_result()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA]);
        Assert.True((await sender.Send(new CreateShippingServiceCommand(Model("post", LangA)), CancellationToken.None)).IsSuccess);
        var result = await sender.Send(new CreateShippingServiceCommand(Model("POST", LangA)), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.ShippingServiceCodeDuplicate, result.FirstError.Code);
    }

    [Fact]
    public async Task Unknown_language_is_rejected_as_semantic_result()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA]);
        var result = await sender.Send(new CreateShippingServiceCommand(Model("tipax", LangUnknown)), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.ShippingServiceLanguageInvalid, result.FirstError.Code);
        Assert.Empty(db.ShippingServices);
    }

    [Fact]
    public async Task Get_missing_returns_not_found_semantic()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA]);
        var result = await sender.Send(new GetShippingServiceQuery(Guid.NewGuid()), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.ShippingServiceNotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task List_uses_language_fallback_to_code()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA]);
        Assert.True((await sender.Send(new CreateShippingServiceCommand(Model("post", LangA)), CancellationToken.None)).IsSuccess);
        var list = await sender.Send(new ListShippingServicesQuery("fa"), CancellationToken.None);
        Assert.True(list.IsSuccess);
        Assert.Contains(list.Value, x => x.Code == "post" && x.Name == "post");
    }

    [Fact]
    public async Task Update_and_deactivate()
    {
        await using var db = CreateDb();
        var sender = CreateSender(db, known: [LangA]);
        var created = await sender.Send(new CreateShippingServiceCommand(Model("courier", LangA)), CancellationToken.None);
        Assert.True(created.IsSuccess);
        var id = created.Value.ShippingServiceId;
        Assert.True((await sender.Send(new UpdateShippingServiceCommand(id, Model("store_courier", LangA, sort: 40)), CancellationToken.None)).IsSuccess);
        var service = await db.ShippingServices.SingleAsync();
        Assert.Equal("store_courier", service.Code);
        Assert.Equal(40, service.SortOrder);
        Assert.True((await sender.Send(new DeactivateShippingServiceCommand(id), CancellationToken.None)).IsSuccess);
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
        Assert.True((await sender.Send(new EnsureShippingCatalogSeedCommand(), CancellationToken.None)).IsSuccess);
        Assert.Equal(5, await db.ShippingServices.CountAsync());
        Assert.True((await sender.Send(new EnsureShippingCatalogSeedCommand(), CancellationToken.None)).IsSuccess);
        Assert.Equal(5, await db.ShippingServices.CountAsync());
    }

    [Fact]
    public void Admin_shipping_endpoints_are_host_thin_transport()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs"));
        Assert.Contains("ApiResponseFactory", source, StringComparison.Ordinal);
        Assert.Contains("ListShippingServicesQuery", source, StringComparison.Ordinal);
        Assert.Contains("GetShippingServiceQuery", source, StringComparison.Ordinal);
        Assert.Contains("CreateShippingServiceCommand", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(new { title", source, StringComparison.Ordinal);
        Assert.DoesNotContain("errorCode = ex.Message", source, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (InvalidOperationException ex)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Localization.Application", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadDetailAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HostShippingServiceLanguageGate", source, StringComparison.Ordinal);
        Assert.DoesNotContain(": IShippingServiceLanguageGate", source, StringComparison.Ordinal);
        Assert.DoesNotContain(", IShippingServiceLanguageGate", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Language_gate_implementation_is_fulfillment_infrastructure_owned()
    {
        var root = FindRepoRoot();
        var hostEndpoints = Path.Combine(root, "src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs");
        var gate = Path.Combine(root, "src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/ShippingServiceLanguageGate.cs");
        var program = Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs");
        Assert.True(File.Exists(gate));
        Assert.DoesNotContain("HostShippingServiceLanguageGate", File.ReadAllText(hostEndpoints), StringComparison.Ordinal);
        Assert.DoesNotContain("HostShippingServiceLanguageGate", File.ReadAllText(program), StringComparison.Ordinal);
        Assert.DoesNotContain("IShippingServiceLanguageGate", File.ReadAllText(program), StringComparison.Ordinal);
        Assert.Contains("IShippingServiceLanguageGate", File.ReadAllText(gate), StringComparison.Ordinal);
        Assert.Contains("Localization.Contracts", File.ReadAllText(gate), StringComparison.Ordinal);
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
        var lookup = new FixedLanguageLookup(known, seedLangs);
        var directory = new ShippingServiceDirectory(db, new SystemUtcClock(), new UuidV7IdGenerator(), gate);
        var catalog = new ShippingCatalogReader(db);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IShippingServiceDirectory>(directory);
        services.AddSingleton<IShippingServiceLanguageGate>(gate);
        services.AddSingleton<IShippingCatalogReader>(catalog);
        services.AddSingleton<ILanguageLookup>(lookup);
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

    private sealed class FixedLanguageLookup : ILanguageLookup
    {
        private readonly IReadOnlyList<LanguageLookupSnapshot> _langs;

        public FixedLanguageLookup(IReadOnlyList<Guid> known, IReadOnlyList<ShippingServiceSeedLanguage>? seed)
        {
            _langs = (seed ?? known.Select(id => new ShippingServiceSeedLanguage(id, "fa", "fa-IR", true)).ToList())
                .Select(x => new LanguageLookupSnapshot(x.LanguageId, x.Code, x.Culture, x.Code, x.IsDefault))
                .ToList();
        }

        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult(_langs);
    }
}


