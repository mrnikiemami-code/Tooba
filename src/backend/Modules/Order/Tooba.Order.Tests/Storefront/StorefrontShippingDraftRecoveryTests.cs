using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Localization.Contracts.Ports;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.Recovery;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Domain.Checkout;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-134 — بازیابی پیش‌نویس پایدار ارسال در رفرش سرد مرحلهٔ ارسال.
///
/// ریشهٔ اشکال: پیش‌نویس چندفروشنده‌ای، shorthand <c>ShippingMethodCode</c> را خالی نگه می‌دارد
/// (چون هیچ روش واحدی بر همهٔ خطوط اعمال نمی‌شود) و انتخاب واقعی فقط در گروه‌های پایدار است.
/// اعتبارسنجی قدیمی فقط همان shorthand خالی را می‌سنجید، پس پیش‌نویس معتبر در رفرش دور ریخته
/// می‌شد: تاریخ/روش/آدرس ناپدید می‌شدند و به مشتری پیام «دیگر معتبر نیست» نشان داده می‌شد.
///
/// این تست‌ها فقط سوکت‌های واقعی مسیر projection را fake می‌کنند (کاتالوگ، زبان، پیش‌نویس، سبد) و
/// هیچ دیتابیس، پروسه یا فرانت‌اندی لازم نیست.
/// </summary>
public sealed class StorefrontShippingDraftRecoveryTests
{
    private static readonly Guid CartId = Guid.Parse("cccccccc-0000-7000-8000-000000000134");
    private static readonly Guid SellerA = Guid.Parse("aaaaaaaa-0000-7000-8000-0000000000a1");
    private static readonly Guid SellerB = Guid.Parse("bbbbbbbb-0000-7000-8000-0000000000b2");
    private static readonly Guid LineA = Guid.Parse("11111111-0000-7000-8000-00000000000a");
    private static readonly Guid LineB = Guid.Parse("11111111-0000-7000-8000-00000000000b");
    private static readonly Guid LanguageId = Guid.Parse("22222222-0000-7000-8000-000000000001");
    private static readonly Guid ServicePost = Guid.Parse("33333333-0000-7000-8000-000000000001");
    private static readonly Guid ServiceTipax = Guid.Parse("33333333-0000-7000-8000-000000000002");

    private const string GuestSecret = "guest-secret-134";
    private const int CartVersion = 5;
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-16T06:00:00Z", CultureInfo.InvariantCulture);
    private static readonly DateOnly GroupedSelectedDate = new(2026, 10, 21);

    [Fact]
    public async Task Cold_projection_without_method_code_recovers_the_grouped_draft_and_its_groups()
    {
        var harness = Harness.Create();

        var projection = await harness.ProjectAsync();

        var draft = Assert.IsType<StorefrontShippingDraftView>(projection.Draft);

        // آدرس authoritative و پایدار همان پیش‌نویس بازمی‌گردد (نه یک انتخاب تازهٔ دوباره‌ساخته).
        Assert.Equal(CartId, draft.CartId);
        Assert.Equal("گیرندهٔ تست", draft.RecipientName);
        Assert.Equal("09120000000", draft.ContactMobile);
        Assert.Equal("تهران", draft.ProvinceName);
        Assert.Equal("ایران‌شهر", draft.CityName);
        Assert.Equal("خیابان تست پلاک ۱", draft.PostalAddress);
        Assert.Equal("1234567890", draft.PostalCode);

        // shorthand چندگروهی عمداً خالی است؛ حقیقت انتخاب فقط در گروه‌های پایدار است.
        Assert.Equal(string.Empty, draft.ShippingMethodCode);
        Assert.Equal(2, draft.ShippingGroups!.Count);
        Assert.Equal(
            [SellerA, SellerB],
            draft.ShippingGroups.Select(g => g.SellerPartyId).ToArray());
        Assert.Equal(
            ["post:express", "tipax:standard"],
            draft.ShippingGroups.Select(g => g.MethodCode).ToArray());
        Assert.Equal(
            [200_000m, 180_000m],
            draft.ShippingGroups.Select(g => g.ShippingAmount).ToArray());

        // تاریخ/بازهٔ ذخیره‌شدهٔ هر گروه بازیابی می‌شود تا مشتری انتخابش را از دست ندهد؛
        // در سبد چندگروهی هیچ تاریخ/پنجرهٔ سراسریِ authoritative وجود ندارد و اسکالر قدیمی خالی است.
        Assert.Null(draft.SelectedDeliveryDate);
        Assert.Null(draft.SelectedDeliveryTimeWindow);
        Assert.All(draft.ShippingGroups, g =>
        {
            Assert.Equal("2026-10-21", g.SelectedDeliveryDate);
            Assert.Equal("09-12", g.SelectedDeliveryTimeWindow);
        });
        // حداقل تاریخ اسکالر قدیمی فقط کران محافظه‌کارانهٔ دیرترین گروه است (نه حقیقت زمان‌بندی گروه‌ها).
        Assert.Equal("2026-10-20", draft.MinimumDeliveryDate);

        // هیچ پیام «نامعتبر»ی وجود ندارد: انتخاب ذخیره‌شده واقعاً معتبر است.
        Assert.Null(projection.RevalidationMessage);

        // و تصویر، وعدهٔ محافظه‌کارانهٔ سبد چندگروهی را از دادهٔ سرور می‌سازد (دیرترین وعدهٔ گروه‌ها).
        Assert.Null(projection.SelectedMethodCode);
        Assert.Equal("2026-10-20", projection.MinimumDeliveryDate);
        Assert.NotEmpty(projection.DeliveryDates);
        Assert.Equal("2026-10-20", projection.DeliveryDates[0].Value);

        // فقط-خواندنی: هیچ نوشتنی روی پیش‌نویس رخ نداد و مبلغ سفارش ساخته نشد.
        Assert.Equal(1, harness.Drafts.ReadCalls);
        Assert.Equal(0, harness.Drafts.SaveCalls);
    }

    [Fact]
    public async Task Two_sellers_with_the_same_method_get_independent_projection_horizons()
    {
        // TB-150: آماده‌سازی ۲ و ۳ روز برای دو فروشنده با روش canonical یکسان.
        var harness = Harness.Create(preparationDays: new Dictionary<Guid, int>
        {
            [SellerA] = 2,
            [SellerB] = 3,
        });

        var projection = await harness.ProjectAsync();
        var horizons = Assert.IsAssignableFrom<IReadOnlyList<StorefrontShippingGroupHorizonView>>(
            projection.ShippingGroupHorizons);

        var a = horizons.Single(h => h.SellerPartyId == SellerA && h.MethodCode == "tipax:standard");
        var b = horizons.Single(h => h.SellerPartyId == SellerB && h.MethodCode == "tipax:standard");

        Assert.Equal(2, a.SellerPreparationDays);
        Assert.Equal(3, b.SellerPreparationDays);
        Assert.Equal("2026-10-21", a.MinimumDeliveryDate);
        Assert.Equal("2026-10-22", b.MinimumDeliveryDate);
        // هیچ تلاقی/دیرترین-حداقلِ سراسری: تاریخ‌های مجاز هر ردیف از حداقل خودش آغاز می‌شود.
        Assert.Equal(a.MinimumDeliveryDate, a.EligibleDates[0].Value);
        Assert.Equal(b.MinimumDeliveryDate, b.EligibleDates[0].Value);
        Assert.DoesNotContain(a.EligibleDates, d => string.CompareOrdinal(d.Value, a.MinimumDeliveryDate) < 0);
        Assert.True(string.CompareOrdinal(a.MinimumDeliveryDate, b.MinimumDeliveryDate) < 0);
        Assert.NotEqual(a.GroupKey, b.GroupKey);
        Assert.StartsWith($"{SellerA:N}|", a.GroupKey, StringComparison.Ordinal);
        // پیش از نخستین ذخیره هیچ شناسهٔ گروهی وجود ندارد؛ ردیفی که پیش‌نویس ذخیره‌شده دارد شناسهٔ
        // پایدار خودش را ضمیمه می‌کند (بازconciliation صریح با کلید مرحلهٔ پیش‌نویس).
        Assert.Null(a.ShippingGroupId);
        Assert.NotNull(b.ShippingGroupId);
        Assert.Equal($"{(SellerB.ToString("N"))}|tipax:standard", b.GroupKey, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(0, harness.Drafts.SaveCalls);
    }

    [Fact]
    public async Task Recovered_draft_keeps_the_server_snapshot_the_payment_step_will_charge()
    {
        var harness = Harness.Create();

        var projection = await harness.ProjectAsync();
        var draft = Assert.IsType<StorefrontShippingDraftView>(projection.Draft);

        // جمع گروه‌های بازیابی‌شده باید همان مبلغ authoritative ذخیره‌شده باشد (۳۸۰٬۰۰۰ ریال در
        // سنجش واقعی دو فروشنده) تا رفت‌وبرگشت /fa/payment -> /fa/shipping مبلغ را عوض نکند.
        Assert.Equal(380_000m, draft.ShippingAmount);
        Assert.Equal(380_000m, draft.ShippingGroups!.Sum(g => g.ShippingAmount));
        // TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149: در سبد چندگروهی زمان‌بندی روی خود گروه‌ها است
        // و اسکالر قدیمی حقیقت سراسری جا نمی‌زند.
        Assert.Null(draft.SelectedDeliveryDate);
        Assert.Null(draft.SelectedDeliveryTimeWindow);
        Assert.All(draft.ShippingGroups!, g =>
            Assert.Equal(GroupedSelectedDate.ToString("yyyy-MM-dd"), g.SelectedDeliveryDate));

        // افق هر (فروشنده × روش واجد شرایط) جداگانه برمی‌گردد: هیچ اشتراک/دیرترین-حداقل سراسری روی
        // این بخش اعمال نمی‌شود. ۲ فروشنده × ۲ روش واجد شرایط = ۴ ردیف مستقل.
        var horizons = Assert.IsAssignableFrom<IReadOnlyList<StorefrontShippingGroupHorizonView>>(
            projection.ShippingGroupHorizons);
        Assert.Equal(4, horizons.Count);
        Assert.Equal(4, horizons.Select(h => h.GroupKey).Distinct().Count());
        Assert.All(horizons, h => Assert.False(string.IsNullOrWhiteSpace(h.GroupKey)));
        Assert.All(horizons, h => Assert.NotEmpty(h.EligibleDates));
        Assert.All(horizons, h => Assert.NotEmpty(h.EligibleTimeWindows));
        Assert.All(horizons, h => Assert.Equal(h.MinimumDeliveryDate, h.EligibleDates[0].Value));
        Assert.All(horizons, h => Assert.True(h.EligibleDates[0].IsEarliest));
        // گروه‌های ذخیره‌شده شناسهٔ پایدار خود را ضمیمه دارند و ردیف‌های مرحلهٔ پیش‌نویس شناسه ندارند.
        var persistedHorizons = horizons.Where(h => h.ShippingGroupId is not null).ToArray();
        Assert.Equal(2, persistedHorizons.Length);
        Assert.All(persistedHorizons, h => Assert.NotEqual(Guid.Empty, h.ShippingGroupId!.Value));
        Assert.Equal(0, harness.Drafts.SaveCalls);
    }

    [Fact]
    public async Task Legacy_single_group_draft_is_still_recovered_without_method_code()
    {
        var harness = Harness.Create(legacySingleGroup: true);

        var projection = await harness.ProjectAsync();

        var draft = Assert.IsType<StorefrontShippingDraftView>(projection.Draft);
        Assert.Equal("post:express", draft.ShippingMethodCode);
        Assert.Equal("post:express", projection.SelectedMethodCode);
        Assert.Null(projection.RevalidationMessage);
        Assert.Equal("2026-10-21", draft.SelectedDeliveryDate);
        Assert.Equal(0, harness.Drafts.SaveCalls);
    }

    [Fact]
    public async Task Draft_fails_closed_when_its_method_is_no_longer_eligible()
    {
        // tipax در فروشگاه دیگر فعال نیست: انتخاب ذخیره‌شده قابل بازآزمایی نیست.
        var harness = Harness.Create(enabledServiceCodes: ["post"]);

        var projection = await harness.ProjectAsync();

        Assert.Null(projection.Draft);
        Assert.Equal(StorefrontShippingDraftRecovery.RevalidationMessage, projection.RevalidationMessage);
        Assert.Equal(0, harness.Drafts.SaveCalls);
    }

    [Fact]
    public async Task Draft_fails_closed_when_a_persisted_group_no_longer_belongs_to_the_cart()
    {
        // یک ردیف سبد (و در نتیجه فروشندهٔ آن) حذف شده است: گروه ذخیره‌شدهٔ فروشندهٔ رفته معتبر نیست.
        var harness = Harness.Create(cartSellers: [SellerA]);

        var projection = await harness.ProjectAsync();

        Assert.Null(projection.Draft);
        Assert.Equal(StorefrontShippingDraftRecovery.RevalidationMessage, projection.RevalidationMessage);
        Assert.Equal(0, harness.Drafts.SaveCalls);
    }

    [Fact]
    public async Task Foreign_guest_secret_never_receives_the_persisted_draft()
    {
        var harness = Harness.Create();

        var projection = await harness.ProjectAsync(guestSecret: "other-secret");

        // نه پیش‌نویس و نه حتی نشانه‌ای از وجود/نبود آن لو می‌رود.
        Assert.Null(projection.Draft);
        Assert.Null(projection.RevalidationMessage);
        Assert.Equal(0, harness.Drafts.SaveCalls);
    }

    [Fact]
    public void Recovery_decision_is_fail_closed_when_no_method_code_is_resolvable()
    {
        var draft = CartShippingDraft.Create(
            CartId,
            Hash(GuestSecret),
            CartVersion,
            "گیرنده",
            "09120000000",
            "تهران",
            "ایران‌شهر",
            "خیابان",
            "1234567890",
            null,
            string.Empty,
            string.Empty,
            0m,
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 21),
            "09-12",
            null,
            Now);

        var methods = Methods();

        Assert.Empty(StorefrontShippingDraftRecovery.SelectedMethodCodes(draft));
        Assert.False(StorefrontShippingDraftRecovery.IsStillValid(draft, methods, null, [SellerA, SellerB], Options()));
        Assert.Null(StorefrontShippingDraftRecovery.ResolveProjectedLeadDays(draft, methods));
    }

    [Fact]
    public void Projected_lead_days_are_the_latest_authoritative_promise_of_the_saved_groups()
    {
        var harness = Harness.Create();

        var draft = harness.Draft!;
        var methods = Methods();

        Assert.Equal(3, StorefrontShippingDraftRecovery.ResolveProjectedLeadDays(draft, methods));

        // اگر روشی امروز واجد شرایط نباشد، هیچ وعدهٔ ساختگی ساخته نمی‌شود.
        Assert.Null(StorefrontShippingDraftRecovery.ResolveProjectedLeadDays(
            draft,
            methods.Where(m => m.MethodCode != "tipax:standard").ToArray()));
    }

    private static string Hash(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret.Trim())));

    private static ShippingMethodsOptions Options() =>
        new()
        {
            EnabledCodes = ["post", "tipax"],
            Rates =
            [
                new() { Code = "post:express", BasePrice = 200_000m, LeadDays = 2 },
                new() { Code = "tipax:standard", BasePrice = 180_000m, LeadDays = 3 },
            ],
        };

    private static IReadOnlyList<StorefrontShippingMethodView> Methods() =>
    [
        new("post:express", "پست پیشتاز", "post", "post", 200_000m, 2, false, "حدود ۲ روز", "express"),
        new("tipax:standard", "تیپاکس عادی", "tipax", "tipax", 180_000m, 3, false, "حدود ۳ روز", "standard"),
    ];

    /// <summary>سیم‌کشی تست: فقط سوکت‌های واقعی مسیر projection؛ بقیهٔ وابستگی‌ها null هستند تا اگر لمس شوند افشا کند.</summary>
    private sealed class Harness
    {
        public required DraftStoreFake Drafts { get; init; }
        public required CartShippingDraft? Draft { get; init; }
        public required StorefrontShippingService Service { get; init; }

        public Task<StorefrontShippingProjection> ProjectAsync(string? guestSecret = GuestSecret) =>
            Service.ProjectAsync(CartId, guestSecret, null, null, null, CancellationToken.None);

        public static Harness Create(
            bool legacySingleGroup = false,
            IReadOnlyList<string>? enabledServiceCodes = null,
            IReadOnlyList<Guid>? cartSellers = null,
            IReadOnlyDictionary<Guid, int>? preparationDays = null)
        {
            var sellers = cartSellers ?? [SellerA, SellerB];
            var carts = new CartsFake(sellers);
            var draft = legacySingleGroup ? LegacyDraft() : GroupedDraft();
            var drafts = new DraftStoreFake(draft);
            var options = legacySingleGroup
                ? new ShippingMethodsOptions
                {
                    EnabledCodes = ["post"],
                    Rates = [new() { Code = "post:express", BasePrice = 200_000m, LeadDays = 2 }],
                }
                : Options();
            if (enabledServiceCodes is not null)
            {
                options.EnabledCodes = [.. enabledServiceCodes];
            }

            if (preparationDays is not null)
            {
                foreach (var (seller, days) in preparationDays)
                {
                    options.SellerPreparationDaysByPartyId[seller.ToString("D")] = days;
                }
            }

            var service = new StorefrontShippingService(
                carts,
                checkouts: null!,
                addresses: null!,
                shippingCatalog: new CatalogFake(enabledServiceCodes ?? (legacySingleGroup ? ["post"] : ["post", "tipax"])),
                oneLevelLabels: new OneLevelLabelsFake(),
                drafts,
                languages: new LanguageFake(),
                shippingOptions: options,
                shippingSeed: new SeedFake(),
                actor: new ActorFake(),
                clock: new FixedClock(Now));

            return new Harness { Drafts = drafts, Draft = draft, Service = service };
        }

        private static CartShippingDraft BaseDraft(string shorthandCode, string shorthandLabel, decimal amount) =>
            CartShippingDraft.Create(
                CartId,
                Hash(GuestSecret),
                CartVersion,
                "گیرندهٔ تست",
                "09120000000",
                "تهران",
                "ایران‌شهر",
                "خیابان تست پلاک ۱",
                "1234567890",
                null,
                shorthandCode,
                shorthandLabel,
                amount,
                new DateOnly(2026, 10, 20),
                GroupedSelectedDate,
                "09-12",
                "یادداشت مشتری",
                Now);

        private static CartShippingDraft LegacyDraft() =>
            BaseDraft("post:express", "پست پیشتاز", 200_000m);

        private static CartShippingDraft GroupedDraft()
        {
            var draft = BaseDraft("post:express", "پست پیشتاز", 200_000m);
            var groups = CartShippingGroupPlanner.Plan(
                CartId,
                [new CartShippingGroupCartLine(LineA, SellerA, 1m), new CartShippingGroupCartLine(LineB, SellerB, 1m)],
                [
                    new CartShippingGroupRequest("post:express", null, null, null, [LineA]),
                    new CartShippingGroupRequest("tipax:standard", null, null, null, [LineB]),
                ],
                [
                    new CartShippingGroupEligibleMethod(
                        "post:express", "post", "express", "پست پیشتاز", 200_000m, "IRR", 2, new DateOnly(2026, 10, 19)),
                    new CartShippingGroupEligibleMethod(
                        "tipax:standard", "tipax", "standard", "تیپاکس عادی", 180_000m, "IRR", 3, new DateOnly(2026, 10, 20)),
                ],
                GroupedSelectedDate,
                "09-12",
                Now,
                NextIds());

            draft.AddShippingGroups(groups, Now);
            return draft;
        }

        private static Func<Guid> NextIds()
        {
            var next = 0;
            return () => Guid.Parse($"00000000-0000-7000-9000-0000000001{++next:D2}");
        }
    }

    private sealed class CartsFake(IReadOnlyList<Guid> sellers) : ICartPresentationGateway
    {
        public Task<CartPage?> GetAsync(Guid id, string? guestSecret, CancellationToken cancellationToken)
        {
            var lines = new List<CartLineView>();
            if (sellers.Contains(SellerA))
            {
                lines.Add(Line(LineA, SellerA, "کالای الف"));
            }

            if (sellers.Contains(SellerB))
            {
                lines.Add(Line(LineB, SellerB, "کالای ب"));
            }

            var total = lines.Count * 1_000_000m;
            return Task.FromResult<CartPage?>(new CartPage(
                CartId,
                CartVersion,
                "IR",
                "IRR",
                "Marketplace",
                lines.Count,
                [new CartCurrencyTotal("IRR", total)],
                lines,
                null,
                "Active"));
        }

        public Task<CartPage?> TryGetForOwnershipAsync(Guid id, string? guestSecret, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        private static CartLineView Line(Guid lineId, Guid seller, string title) =>
            new(lineId, Guid.NewGuid(), Guid.NewGuid(), seller, null, null, title, "فروشنده",
                null, 1m, 1_000_000m, 1_000_000m, "IRR", false);
    }

    private sealed class DraftStoreFake(CartShippingDraft? draft) : IStorefrontShippingDraftStore
    {
        public int ReadCalls { get; private set; }

        public int SaveCalls { get; private set; }

        public Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken)
        {
            ReadCalls++;
            return Task.FromResult(draft is not null && draft.CartId == cartId ? draft : null);
        }

        public Task SaveAsync(CartShippingDraft value, bool isNew, CancellationToken cancellationToken)
        {
            SaveCalls++;
            throw new NotSupportedException("projection must never write the shipping draft");
        }

        public Task<CartShippingDraft?> GetForCapturedSelectionAsync(Guid cartId, CancellationToken cancellationToken) =>
            GetByCartIdAsync(cartId, cancellationToken);

        public Task<CartShippingDraft> ReplaceCapturedSelectionAsync(
            CartShippingDraft value,
            IReadOnlyList<CartShippingDraftGroup> groups,
            bool isNew,
            long expectedSelectionRevision,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("projection must never write the shipping draft");
    }

    private sealed class LanguageFake : ILanguageLookup
    {
        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LanguageLookupSnapshot>>(
                [new LanguageLookupSnapshot(LanguageId, "fa", "fa-IR", "fa", true)]);
    }

    private sealed class OneLevelLabelsFake : IShippingOneLevelStorefrontLabelReader
    {
        public Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
            IReadOnlyCollection<string> methodCodes,
            Guid languageId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
    }

    private sealed class SeedFake : IShippingCatalogSeedPort
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
        public DateTimeOffset UtcNow { get; } = now;
    }

    /// <summary>کاتالوگ واقعی‌شکل: سرویس فعال + گزینه‌های فعال، دقیقاً همان چیزی که کد روش را می‌سازد.</summary>
    private sealed class CatalogFake(IReadOnlyList<string> serviceCodes) : IShippingCatalogReader
    {
        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>(
                [
                    .. serviceCodes.Select(code => Snapshot(
                        code == "post" ? ServicePost : ServiceTipax,
                        code,
                        code == "post" ? "express" : "standard",
                        code == "post" ? "پست" : "تیپاکس",
                        code == "post" ? "پست پیشتاز" : "تیپاکس عادی")),
                ]);

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        private static ShippingCatalogServiceSnapshot Snapshot(
            Guid serviceId,
            string code,
            string optionCode,
            string serviceName,
            string optionName) =>
            new(
                serviceId,
                code,
                "provider",
                code,
                "primary",
                true,
                1,
                [new ShippingCatalogTranslationSnapshot(LanguageId, serviceName, null)],
                [
                    new ShippingCatalogOptionSnapshot(
                        Guid.NewGuid(),
                        serviceId,
                        optionCode,
                        true,
                        1,
                        [new ShippingCatalogOptionTranslationSnapshot(LanguageId, optionName)]),
                ]);
    }
}
