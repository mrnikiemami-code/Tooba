using System.Globalization;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Localization.Contracts.Ports;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Domain.Checkout;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-100 — اثبات «فقط برچسب» در پیش‌نمایش ارسال فروشگاه.
/// </summary>
/// <remarks>
/// دروازهٔ خاموش باید برچسب/کد/قیمت قدیمی را بایت‌به‌بایت نگه دارد. دروازهٔ روشن فقط نام نمایشیِ
/// همان روشی را عوض می‌کند که ترجمهٔ یک‌سطحی دقیق زبان درخواستی دارد؛ واجد شرایط بودن، کد روش،
/// کد سرویس، کد گزینه و مبلغ دست‌نخورده می‌مانند. هیچ DML/دیتابیسی لمس نمی‌شود.
/// </remarks>
public sealed class StorefrontShippingOneLevelLabelTests
{
    private static readonly Guid FaLanguageId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid EnLanguageId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-10-16T06:00:00Z", CultureInfo.InvariantCulture);

    private const string ServiceCode = "post";
    private const string StandardCode = "post:standard";
    private const string ExpressCode = "post:express";
    private const string CourierCode = "snapp_courier";

    private const string LegacyPostName = "پست قدیمی";
    private const string LegacyStandardName = "استاندارد قدیمی";
    private const string LegacyExpressName = "اکسپرس قدیمی";
    private const string LegacyCourierName = "اسنپ قدیمی";
    private const string OneLevelStandardName = "پست استاندارد یک‌سطحی ۰۹۴";

    [Fact]
    public async Task Gate_off_keeps_legacy_labels_codes_and_prices_byte_identical()
    {
        var harness = Harness.Create(oneLevelNames: null);

        var projection = await harness.Service.ProjectAsync(
            harness.CartId, guestSecret: null, provinceName: null, methodCode: null, language: null,
            CancellationToken.None);

        Assert.Equal(
            [StandardCode, ExpressCode, CourierCode],
            projection.Methods.Select(m => m.MethodCode).ToArray());
        Assert.Equal(
            [$"{LegacyPostName} — {LegacyStandardName}", $"{LegacyPostName} — {LegacyExpressName}", LegacyCourierName],
            projection.Methods.Select(m => m.Label).ToArray());

        // درز برچسب یک‌بار و با کدهای همان فهرست (بدون تغییر ترتیب/ترکیب) صدا زده می‌شود.
        Assert.Equal(1, harness.Labels.CallCount);
        Assert.Equal(FaLanguageId, harness.Labels.LastLanguageId);
        Assert.Equal([StandardCode, ExpressCode, CourierCode], harness.Labels.LastMethodCodes.ToArray());

        var standard = projection.Methods.Single(m => m.MethodCode == StandardCode);
        Assert.Equal(120_000m, standard.PriceAmount);
        Assert.Equal(4, standard.LeadDays);
        Assert.Equal(ServiceCode, standard.ServiceCode);
        Assert.Equal("standard", standard.OptionCode);
    }

    [Fact]
    public async Task Gate_on_replaces_only_the_matching_label_and_preserves_identity_and_price()
    {
        var offHarness = Harness.Create(oneLevelNames: null);
        var off = await offHarness.Service.ProjectAsync(
            offHarness.CartId, null, null, null, null, CancellationToken.None);
        Assert.NotEmpty(off.Methods);

        var harness = Harness.Create(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [StandardCode] = OneLevelStandardName,
        });

        var projection = await harness.Service.ProjectAsync(
            harness.CartId, null, null, null, null, CancellationToken.None);

        Assert.Equal(
            [StandardCode, ExpressCode, CourierCode],
            projection.Methods.Select(m => m.MethodCode).ToArray());
        Assert.Equal(
            [OneLevelStandardName, $"{LegacyPostName} — {LegacyExpressName}", LegacyCourierName],
            projection.Methods.Select(m => m.Label).ToArray());

        // مبلغ/روز/کد سرویس/کد گزینه عیناً مثل حالت دروازهٔ خاموش و مثل محاسبهٔ قدیمی است.
        var standard = projection.Methods.Single(m => m.MethodCode == StandardCode);
        Assert.Equal(120_000m, standard.PriceAmount);
        Assert.Equal(off.Methods.Single(m => m.MethodCode == StandardCode).PriceAmount, standard.PriceAmount);
        Assert.Equal(4, standard.LeadDays);
        Assert.Equal(ServiceCode, standard.ServiceCode);
        Assert.Equal("standard", standard.OptionCode);
        Assert.False(standard.IsFree);

        var express = projection.Methods.Single(m => m.MethodCode == ExpressCode);
        Assert.Equal(off.Methods.Single(m => m.MethodCode == ExpressCode).PriceAmount, express.PriceAmount);
        Assert.Equal("express", express.OptionCode);
    }

    [Fact]
    public async Task Requested_language_reaches_the_seam_and_untranslated_language_keeps_legacy_label()
    {
        // درخواست انگلیسی: درز با شناسهٔ زبان انگلیسی صدا زده می‌شود و چون نگاشت خالی برمی‌گردد
        // (نبود ترجمهٔ همان زبان) برچسب قدیمی نمایش داده می‌شود، نه برچسب زبان دیگر.
        var harness = Harness.Create(
            oneLevelNames: new Dictionary<string, string>(StringComparer.Ordinal) { [StandardCode] = OneLevelStandardName },
            emptyForLanguage: EnLanguageId);

        var projection = await harness.Service.ProjectAsync(
            harness.CartId, null, null, null, "en", CancellationToken.None);

        Assert.Equal(EnLanguageId, harness.Labels.LastLanguageId);
        Assert.Equal(
            [$"{LegacyPostName} — {LegacyStandardName}", $"{LegacyPostName} — {LegacyExpressName}", LegacyCourierName],
            projection.Methods.Select(m => m.Label).ToArray());
    }

    [Fact]
    public async Task Unknown_and_optionless_codes_fall_back_to_exact_legacy_label()
    {
        // نگاشت فقط برای کد ناشناخته و سرویس بدون گزینه است؛ هیچ برچسبی نباید بازسازی/جایگزین شود
        // مگر کلید دقیق همان methodCode موجود باشد.
        var harness = Harness.Create(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["post:economic"] = "اقتصادی یک‌سطحی",
            [ServiceCode] = "پست یک‌سطحی بدون گزینه",
        });

        var projection = await harness.Service.ProjectAsync(
            harness.CartId, null, null, null, null, CancellationToken.None);

        Assert.Equal(CourierCode, projection.Methods.Single(m => m.MethodCode == CourierCode).MethodCode);
        Assert.Equal(LegacyCourierName, projection.Methods.Single(m => m.MethodCode == CourierCode).Label);
        Assert.Equal(
            $"{LegacyPostName} — {LegacyStandardName}",
            projection.Methods.Single(m => m.MethodCode == StandardCode).Label);
    }

    private sealed class Harness
    {
        public required Guid CartId { get; init; }

        public required StorefrontShippingService Service { get; init; }

        public required OneLevelShippingLabelFake Labels { get; init; }

        public static Harness Create(
            IReadOnlyDictionary<string, string>? oneLevelNames,
            Guid? emptyForLanguage = null)
        {
            var cartId = Guid.NewGuid();
            var labels = new OneLevelShippingLabelFake(oneLevelNames, emptyForLanguage);
            var service = new StorefrontShippingService(
                new CartsFake(cartId),
                checkouts: null!,
                addresses: null!,
                shippingCatalog: new ShippingCatalogFake(),
                oneLevelLabels: labels,
                drafts: new DraftStoreFake(),
                languages: new LanguagesFake(),
                shippingOptions: new ShippingMethodsOptions(),
                shippingSeed: new ShippingSeedFake(),
                actor: new ActorFake(),
                clock: new FixedClock(Now));
            return new Harness { CartId = cartId, Service = service, Labels = labels };
        }
    }

    private sealed class CartsFake(Guid cartId) : ICartPresentationGateway
    {
        private const decimal SellerSubtotal = 2_000_000m;
        private const int CartVersion = 7;

        public Task<CartPage?> GetAsync(Guid id, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult(id == cartId ? Cart(id) : null);

        public Task<CartPage?> TryGetForOwnershipAsync(Guid id, string? guestSecret, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        private static CartPage Cart(Guid id) =>
            new(
                id,
                CartVersion,
                "IR",
                "IRR",
                "Marketplace",
                2m,
                [new CartCurrencyTotal("IRR", SellerSubtotal)],
                [
                    new CartLineView(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        null,
                        null,
                        "کالای تست",
                        "فروشندهٔ تست",
                        null,
                        2m,
                        1_000_000m,
                        SellerSubtotal,
                        "IRR",
                        false),
                ],
                null,
                "Active");
    }

    private sealed class ShippingCatalogFake : IShippingCatalogReader
    {
        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>(
            [
                new ShippingCatalogServiceSnapshot(
                    Guid.NewGuid(),
                    ServiceCode,
                    ProviderKind: "post",
                    IconKey: "post",
                    ColorKey: "#000",
                    IsActive: true,
                    SortOrder: 0,
                    Translations: [new ShippingCatalogTranslationSnapshot(FaLanguageId, LegacyPostName, null)],
                    Options:
                    [
                        new ShippingCatalogOptionSnapshot(
                            Guid.NewGuid(), Guid.NewGuid(), "standard", IsActive: true, SortOrder: 0,
                            Translations: [new ShippingCatalogOptionTranslationSnapshot(FaLanguageId, LegacyStandardName)]),
                        new ShippingCatalogOptionSnapshot(
                            Guid.NewGuid(), Guid.NewGuid(), "express", IsActive: true, SortOrder: 1,
                            Translations: [new ShippingCatalogOptionTranslationSnapshot(FaLanguageId, LegacyExpressName)]),
                    ]),
                new ShippingCatalogServiceSnapshot(
                    Guid.NewGuid(),
                    CourierCode,
                    ProviderKind: "courier",
                    IconKey: "courier",
                    ColorKey: "#111",
                    IsActive: true,
                    SortOrder: 1,
                    Translations: [new ShippingCatalogTranslationSnapshot(FaLanguageId, LegacyCourierName, null)],
                    Options: []),
            ]);

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class DraftStoreFake : IStorefrontShippingDraftStore
    {
        public Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken) =>
            Task.FromResult<CartShippingDraft?>(null);

        public Task SaveAsync(CartShippingDraft value, bool isNew, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ReplaceCapturedSelectionAsync(
            CartShippingDraft value,
            IReadOnlyList<CartShippingDraftGroup> groups,
            bool isNew,
            long expectedSelectionRevision,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class LanguagesFake : ILanguageLookup
    {
        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LanguageLookupSnapshot>>(
            [
                new LanguageLookupSnapshot(FaLanguageId, "fa", "fa-IR", "", IsDefault: true),
                new LanguageLookupSnapshot(EnLanguageId, "en", "en-US", "en", IsDefault: false),
            ]);
    }

    private sealed class ShippingSeedFake : IShippingCatalogSeedPort
    {
        public Task EnsureSeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ActorFake : IOrderStorefrontActor
    {
        public Guid GuestActorId => Guid.Empty;

        public bool IsAuthenticated => false;

        public Guid? AuthenticatedUserId => null;

        public Guid ResolvePlacementActor(bool usingSavedAddress) => throw new NotSupportedException();

        public Guid? TryResolveListActor() => throw new NotSupportedException();

        public CartAccess BuildCartAccess(string? guestSecret) => new(null, guestSecret);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
