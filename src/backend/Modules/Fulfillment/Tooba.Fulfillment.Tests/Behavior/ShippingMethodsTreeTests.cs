using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Localization.Contracts;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>TB-TMAR-NEXT-MODULE-BATCH-004-R3 — shipping methods tree Application ownership.</summary>
public sealed class ShippingMethodsTreeTests
{
    private static readonly Guid LangFa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");
    private static readonly Guid LangEn = Guid.Parse("01900000-0000-7000-8000-00000000aa02");

    [Fact]
    public async Task Catalog_backed_result_filters_inactive_options_and_uses_language()
    {
        var catalog = new StubCatalog(
        [
            new ShippingCatalogServiceSnapshot(
                Guid.NewGuid(),
                "post",
                "postal",
                "truck",
                "blue",
                true,
                10,
                [
                    new ShippingCatalogTranslationSnapshot(LangFa, "پست", null),
                    new ShippingCatalogTranslationSnapshot(LangEn, "Post", null),
                ],
                [
                    new ShippingCatalogOptionSnapshot(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "express",
                        true,
                        1,
                        [
                            new ShippingCatalogOptionTranslationSnapshot(LangFa, "پیشتاز"),
                            new ShippingCatalogOptionTranslationSnapshot(LangEn, "Express"),
                        ]),
                    new ShippingCatalogOptionSnapshot(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "retired",
                        false,
                        2,
                        [new ShippingCatalogOptionTranslationSnapshot(LangFa, "بازنشسته")]),
                ]),
        ]);
        var sender = CreateSender(catalog, languages:
        [
            new LanguageLookupSnapshot(LangFa, "fa", "fa-IR", "fa", true),
            new LanguageLookupSnapshot(LangEn, "en", "en-US", "en", false),
        ]);

        var result = await sender.Send(new ListEnabledShippingMethodsTreeQuery("en"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var post = Assert.Single(result.Value, x => x.Code == "post");
        Assert.Equal("Post", post.Name);
        Assert.Equal("Post", post.LabelFa);
        Assert.Equal("blue", post.ColorKey);
        Assert.Equal("truck", post.IconKey);
        var option = Assert.Single(post.Options);
        Assert.Equal("express", option.Code);
        Assert.Equal("Express", option.Name);
    }

    [Fact]
    public async Task Empty_catalog_falls_back_to_registry_defaults()
    {
        var sender = CreateSender(new StubCatalog([]), languages:
        [
            new LanguageLookupSnapshot(LangFa, "fa", "fa-IR", "fa", true),
        ]);
        var result = await sender.Send(new ListEnabledShippingMethodsTreeQuery(null), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value, x => x.Code == "post" && x.ColorKey == "blue");
        var post = result.Value.First(x => x.Code == "post");
        Assert.Equal(2, post.Options.Count);
        Assert.Contains(post.Options, o => o.Code == "express" && o.LabelFa == "پیشتاز");
        Assert.Contains(result.Value, x => x.Code == "in_person" && x.Options.Count == 0);
    }

    [Fact]
    public async Task Language_fallback_uses_first_translation_then_code()
    {
        var catalog = new StubCatalog(
        [
            new ShippingCatalogServiceSnapshot(
                Guid.NewGuid(),
                "tipax",
                "courier",
                "box",
                "amber",
                true,
                5,
                [new ShippingCatalogTranslationSnapshot(LangEn, "Tipax EN", null)],
                []),
        ]);
        var sender = CreateSender(catalog, languages:
        [
            new LanguageLookupSnapshot(LangFa, "fa", "fa-IR", "fa", true),
            new LanguageLookupSnapshot(LangEn, "en", "en-US", "en", false),
        ]);
        var result = await sender.Send(new ListEnabledShippingMethodsTreeQuery("fa"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var tipax = Assert.Single(result.Value, x => x.Code == "tipax");
        Assert.Equal("Tipax EN", tipax.Name);
    }

    private static ISender CreateSender(
        IShippingCatalogReader catalog,
        IReadOnlyList<LanguageLookupSnapshot> languages)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(catalog);
        services.AddSingleton<ILanguageLookup>(new FixedLanguages(languages));
        services.AddSingleton<IShippingServiceDirectory>(new NoopSeedDirectory());
        services.AddSingleton(new ShippingMethodsOptions());
        services.AddToobaCqrsFoundation(typeof(ListEnabledShippingMethodsTreeQuery).Assembly);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private sealed class StubCatalog(IReadOnlyList<ShippingCatalogServiceSnapshot> rows) : IShippingCatalogReader
    {
        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(rows);

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            Task.FromResult(rows.FirstOrDefault(x => x.ShippingServiceId == serviceId));
    }

    private sealed class FixedLanguages(IReadOnlyList<LanguageLookupSnapshot> langs) : ILanguageLookup
    {
        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(langs);
    }

    private sealed class NoopSeedDirectory : IShippingServiceDirectory
    {
        public Task EnsureSeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Guid> CreateAsync(ShippingServiceWriteModel model, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<Guid> UpdateAsync(Guid serviceId, ShippingServiceWriteModel model, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task DeactivateAsync(Guid serviceId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
