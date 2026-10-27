using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Localization.Contracts.Ports;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-SHIPPING-GROUP-ID-CONTINUITY-153 — ناوردایی «ShippingGroupId پایدار».
///
/// ریشهٔ اشکال پیش از این تسک: هر ذخیرهٔ پذیرفته‌شدهٔ انتخاب ارسال، همهٔ گروه‌ها را با شناسهٔ تازهٔ
/// <c>UuidV7.New</c> بازمی‌ساخت (Clear + Add)، پس حتی تلاش تکراری یا تغییر فقط زمان‌بندی یک گروه،
/// شناسهٔ همهٔ گروه‌ها را عوض می‌کرد.
///
/// این تست‌ها دو لایه را می‌سنجند:
/// ۱) قاعدهٔ خالص دامنه (بازاستفاده از هویت فقط برای همان تعهد منطقی)،
/// ۲) مسیر واقعی <see cref="StorefrontShippingService.SaveSelectionAsync"/> با پورت‌های fake و
///    پیش‌نویس درحافظه‌ای که همان معنای دو‌مرحله‌ای حذف/درج را دارد.
/// هیچ Host، دیتابیس مشترک، PSP، پرداخت یا سرویس بیرونی لمس نمی‌شود.
/// </summary>
public sealed class ShippingGroupIdentityContinuityTests
{
    private static readonly Guid SellerA = Guid.Parse("aaaaaaaa-0000-7000-8000-0000000000a1");
    private static readonly Guid SellerB = Guid.Parse("bbbbbbbb-0000-7000-8000-0000000000b2");
    private static readonly Guid Cart = Guid.Parse("cccccccc-0000-7000-8000-000000000153");
    private static readonly Guid LineA = Guid.Parse("11111111-0000-7000-8000-00000000000a");
    private static readonly Guid LineB = Guid.Parse("11111111-0000-7000-8000-00000000000b");
    private static readonly Guid LineA2 = Guid.Parse("11111111-0000-7000-8000-00000000000c");
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 16);
    private static readonly DateOnly MinA = new(2026, 10, 21);
    private static readonly DateOnly MinB = new(2026, 10, 22);
    private const string Tipax = "tipax:standard";
    private const string Post = "post:standard";
    private const string GuestSecret = "guest-secret-153";

    private static readonly ShippingMethodsOptions Options = new()
    {
        DefaultSellerPreparationDays = 1,
        SellerPreparationDaysByPartyId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [SellerA.ToString("D")] = 2,
            [SellerB.ToString("D")] = 3,
        },
        DeliveryHorizonDays = 7,
    };

    // ---------------------------------------------------------------- قاعدهٔ خالص دامنه

    [Fact]
    public void A_repeat_plan_for_the_same_obligation_reuses_the_persisted_ids()
    {
        var first = Plan(NewIds(), null);
        var second = Plan(NewIds(), first);

        Assert.Equal(IdsOf(first), IdsOf(second));
        // یک تعهد، یک شناسه: هرگز دو گروه شناسهٔ یکسان نمی‌گیرند.
        Assert.Equal(2, second.Select(g => g.ShippingGroupId).Distinct().Count());
    }

    [Fact]
    public void Changing_only_a_schedule_keeps_both_persisted_ids()
    {
        var first = Plan(NewIds(), null);
        var replanned = Plan(
            NewIds(),
            first,
            groupAWindow: "15-18",
            groupADate: MinA.AddDays(2));

        Assert.Equal(IdsOf(first), IdsOf(replanned));
        var a = replanned.Single(g => g.SellerPartyId == SellerA);
        Assert.Equal(MinA.AddDays(2), a.SelectedDeliveryDate);
        Assert.Equal("15-18", a.SelectedDeliveryTimeWindow);
    }

    [Fact]
    public void A_changed_membership_allocates_a_new_id_instead_of_reusing_the_old_one()
    {
        var first = Plan(NewIds(), null);
        var idA = first.Single(g => g.SellerPartyId == SellerA).ShippingGroupId;

        // همان فروشنده و همان روش، ولی عضویت خطوط عوض شده: تعهد منطقی دیگری است.
        var replanned = Plan(NewIds(), first, extraSellerALine: true);

        var newA = replanned.Single(g => g.SellerPartyId == SellerA);
        Assert.NotEqual(idA, newA.ShippingGroupId);
        Assert.Equal(2, newA.Lines.Count);
        Assert.Contains(newA.Lines, l => l.CartLineId == LineA2);
    }

    [Fact]
    public void Another_seller_or_method_never_borrows_a_persisted_id()
    {
        var first = Plan(NewIds(), null);
        var idA = first.Single(g => g.SellerPartyId == SellerA).ShippingGroupId;
        var idB = first.Single(g => g.SellerPartyId == SellerB).ShippingGroupId;

        // B روش دیگری می‌خواهد: شناسهٔ B پایدار نمی‌ماند، ولی شناسهٔ A دست‌نخورده است.
        var switched = Plan(NewIds(), first, groupBMethod: Post);
        var switchedA = switched.Single(g => g.SellerPartyId == SellerA);
        var switchedB = switched.Single(g => g.SellerPartyId == SellerB);

        Assert.Equal(idA, switchedA.ShippingGroupId);
        Assert.NotEqual(idB, switchedB.ShippingGroupId);
        Assert.NotEqual(idA, switchedB.ShippingGroupId);
        Assert.Equal(Post, switchedB.MethodCode);
    }

    [Fact]
    public void A_group_that_belongs_to_another_cart_is_never_reused()
    {
        var otherCart = Guid.Parse("cccccccc-0000-7000-8000-000000000999");
        var foreign = Plan(NewIds(), null, cartId: otherCart);
        var idA = foreign.Single(g => g.SellerPartyId == SellerA).ShippingGroupId;

        var planned = Plan(NewIds(), foreign);

        Assert.DoesNotContain(idA, planned.Select(g => g.ShippingGroupId));
    }

    [Fact]
    public void Legacy_plan_without_reuse_candidates_is_unchanged()
    {
        var groups = Plan(NewIds(), null);

        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Equal(Tipax, g.MethodCode));
        Assert.Equal(MinA, groups.Single(g => g.SellerPartyId == SellerA).MinimumDeliveryDate);
        Assert.Equal(MinB, groups.Single(g => g.SellerPartyId == SellerB).MinimumDeliveryDate);
    }

    // ------------------------------------------------- مسیر واقعی SaveSelectionAsync

    [Fact]
    public async Task First_save_creates_two_groups_and_identical_save_returns_exact_same_ids()
    {
        var harness = Harness.Create();

        var first = await harness.SaveTwoSellersAsync();
        var firstGroups = Groups(first);
        Assert.Equal(2, firstGroups.Count);
        Assert.Equal(2, firstGroups.Select(g => g.ShippingGroupId).Distinct().Count());
        Assert.Equal(new[] { Tipax, Tipax }, firstGroups.Select(g => g.MethodCode).ToArray());
        Assert.All(firstGroups, g => Assert.Equal(1, g.LineCount));

        var second = await harness.SaveTwoSellersAsync();
        var secondGroups = Groups(second);

        Assert.Equal(IdsOf(firstGroups), IdsOf(secondGroups));
        Assert.Equal(SchedulesOf(firstGroups), SchedulesOf(secondGroups));
        // سبد هرگز به سفارش/پرداخت تبدیل نشده است.
        Assert.Equal(0, harness.Directory.SubmitCalls);
    }

    [Fact]
    public async Task Changing_only_group_a_keeps_both_ids_and_leaves_group_b_identical()
    {
        var harness = Harness.Create();
        var first = Groups(await harness.SaveTwoSellersAsync());

        var second = Groups(await harness.SaveTwoSellersAsync(groupAWindow: "15-18", groupADate: MinA.AddDays(1)));

        Assert.Equal(IdsOf(first), IdsOf(second));
        var a = second.Single(g => g.SellerPartyId == SellerA);
        var b = second.Single(g => g.SellerPartyId == SellerB);
        Assert.Equal("15-18", a.SelectedDeliveryTimeWindow);
        Assert.Equal(MinA.AddDays(1).ToString("yyyy-MM-dd"), a.SelectedDeliveryDate);
        // B بیت‌به‌بیت همان وعدهٔ پیشین است: زمان‌بندی، مبلغ، حداقل، عضویت و هویت.
        Assert.Equal(Describe(first.Single(g => g.SellerPartyId == SellerB)), Describe(b));
    }

    [Fact]
    public async Task A_genuine_method_switch_allocates_a_new_id_only_for_that_group()
    {
        var harness = Harness.Create();
        var first = Groups(await harness.SaveTwoSellersAsync());

        var second = Groups(await harness.SaveTwoSellersAsync(
            groupBMethod: Post,
            groupBDate: MinB.AddDays(2)));

        var a = second.Single(g => g.SellerPartyId == SellerA);
        var b = second.Single(g => g.SellerPartyId == SellerB);
        Assert.Equal(first.Single(g => g.SellerPartyId == SellerA).ShippingGroupId, a.ShippingGroupId);
        Assert.NotEqual(first.Single(g => g.SellerPartyId == SellerB).ShippingGroupId, b.ShippingGroupId);
        Assert.Equal(Post, b.MethodCode);
        // هیچ خطی دو بار عضو نشده و هیچ خطی جا نیفتاده است.
        Assert.Equal(2, second.SelectMany(g => g.CartLineIds!).Distinct().Count());
    }

    [Fact]
    public async Task Fail_closed_invalid_date_or_window_never_touches_persisted_ids_or_schedules()
    {
        var harness = Harness.Create();
        var saved = Groups(await harness.SaveTwoSellersAsync());
        var savesBefore = harness.Drafts.SaveCalls;

        var tooEarly = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            harness.SaveTwoSellersAsync(groupADate: MinA.AddDays(-1)));
        Assert.Equal(StorefrontOrderErrors.ShippingDeliveryTooEarly, tooEarly.Code);

        var badWindow = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            harness.SaveTwoSellersAsync(groupAWindow: "23-24"));
        Assert.Equal(StorefrontOrderErrors.ShippingDeliverySlotUnavailable, badWindow.Code);

        // هیچ نوشتنی رخ نداده و هویت/زمان‌بندی ذخیره‌شده دست‌نخورده است.
        Assert.Equal(savesBefore, harness.Drafts.SaveCalls);
        var after = Groups(await harness.SaveTwoSellersAsync());
        Assert.Equal(IdsOf(saved), IdsOf(after));
        Assert.Equal(SchedulesOf(saved), SchedulesOf(after));
        Assert.Equal(0, harness.Directory.SubmitCalls);
    }

    [Fact]
    public async Task Legacy_single_group_shorthand_still_works_and_keeps_its_identity()
    {
        var harness = Harness.Create(singleSellerOnly: true);

        var first = await harness.SaveLegacyAsync("9-12");
        var firstGroup = Assert.Single(Groups(first));
        Assert.Equal(SellerA, firstGroup.SellerPartyId);
        Assert.Equal(Tipax, firstGroup.MethodCode);
        Assert.Equal("9-12", firstGroup.SelectedDeliveryTimeWindow);
        Assert.Equal(MinA.ToString("yyyy-MM-dd"), firstGroup.SelectedDeliveryDate);

        var second = await harness.SaveLegacyAsync("12-15");
        var secondGroup = Assert.Single(Groups(second));
        Assert.Equal(firstGroup.ShippingGroupId, secondGroup.ShippingGroupId);
        Assert.Equal("12-15", secondGroup.SelectedDeliveryTimeWindow);
    }

    // ------------------------------------------------------------------------ helpers

    private static IReadOnlyList<CartShippingDraftGroup> Plan(
        Func<Guid> newIds,
        IReadOnlyList<CartShippingDraftGroup>? reusable,
        string groupAWindow = "9-12",
        DateOnly? groupADate = null,
        bool extraSellerALine = false,
        string groupBMethod = Tipax,
        Guid? cartId = null)
    {
        var cart = cartId ?? Cart;
        var lines = new List<CartShippingGroupCartLine>
        {
            new(LineA, SellerA, 1m),
            new(LineB, SellerB, 1m),
        };
        var groupALines = new List<Guid> { LineA };
        if (extraSellerALine)
        {
            lines.Add(new CartShippingGroupCartLine(LineA2, SellerA, 1m));
            groupALines.Add(LineA2);
        }

        var requests = new[]
        {
            new CartShippingGroupRequest(
                Tipax, null, null, null, groupALines, groupADate ?? MinA, groupAWindow),
            new CartShippingGroupRequest(Tipax, null, null, null, [LineB], MinB, "9-12"),
        };
        if (groupBMethod != Tipax)
        {
            requests[1] = requests[1] with { MethodCode = groupBMethod };
        }

        return CartShippingGroupPlanner.Plan(
            cart, lines, requests, Eligible(), null, null, Now, newIds, reusable);
    }

    private static IReadOnlyList<CartShippingGroupEligibleMethod> Eligible() =>
    [
        new(Tipax, "tipax", "standard", "تیپاکس معمولی", 180_000m, "IRR", 3, MinA, SellerA),
        new(Tipax, "tipax", "standard", "تیپاکس معمولی", 180_000m, "IRR", 3, MinB, SellerB),
        new(Post, "post", "standard", "پست معمولی", 120_000m, "IRR", 4, MinA, SellerA),
        new(Post, "post", "standard", "پست معمولی", 120_000m, "IRR", 4, MinB, SellerB),
    ];

    private static int _idFactorySequence;

    private static Func<Guid> NewIds()
    {
        // هر کارخانه شناسهٔ متفاوت می‌سازد تا «شناسهٔ تازه» و «شناسهٔ بازاستفاده‌شده» قابل تفکیک باشند.
        var sequence = Interlocked.Increment(ref _idFactorySequence);
        var next = 0;
        return () => Guid.Parse($"000000{sequence:D2}-0000-7000-9000-{++next:D12}");
    }

    private static Guid[] IdsOf(IReadOnlyList<CartShippingDraftGroup> groups) =>
        groups.Select(g => g.ShippingGroupId).OrderBy(x => x).ToArray();

    private static Guid[] IdsOf(IReadOnlyList<StorefrontShippingGroupView> groups) =>
        groups.Select(g => g.ShippingGroupId).OrderBy(x => x).ToArray();

    private static string[] SchedulesOf(IReadOnlyList<StorefrontShippingGroupView> groups) =>
        groups
            .OrderBy(g => g.SellerPartyId)
            .Select(g => $"{g.SellerPartyId:N}|{g.SelectedDeliveryDate}|{g.SelectedDeliveryTimeWindow}")
            .ToArray();

    private static string Describe(StorefrontShippingGroupView group) =>
        $"{group.ShippingGroupId:N}|{group.SellerPartyId:N}|{group.MethodCode}|{group.ServiceCode}|" +
        $"{group.OptionCode}|{group.ShippingAmount}|{group.Currency}|{group.MinimumDeliveryDate}|" +
        $"{group.SelectedDeliveryDate}|{group.SelectedDeliveryTimeWindow}|{group.LeadDays}|" +
        $"{string.Join(',', (group.CartLineIds ?? []).Select(x => x.ToString("N")).OrderBy(x => x, StringComparer.Ordinal))}";

    private static IReadOnlyList<StorefrontShippingGroupView> Groups(StorefrontShippingDraftView view) =>
        view.ShippingGroups ?? [];

    private sealed class Harness
    {
        public required Guid CartId { get; init; }
        public required StorefrontShippingService Service { get; init; }
        public required DraftStoreFake Drafts { get; init; }
        public required DirectoryFake Directory { get; init; }
        public required int CartVersion { get; init; }
        public required bool SingleSellerOnly { get; init; }

        public static Harness Create(bool singleSellerOnly = false)
        {
            var cartId = Guid.NewGuid();
            var directory = new DirectoryFake();
            var drafts = new DraftStoreFake();
            var actor = new ActorFake();
            var carts = new CartsFake(cartId, singleSellerOnly);
            var checkout = new StorefrontCheckoutService(carts, directory, addresses: null!, actor);
            var service = new StorefrontShippingService(
                carts,
                checkout,
                addresses: null!,
                shippingCatalog: new ShippingCatalogFake(),
                oneLevelLabels: new OneLevelLabelFake(),
                drafts,
                languages: new LanguagesFake(),
                shippingOptions: Options,
                shippingSeed: new ShippingSeedFake(),
                actor,
                clock: new FixedClock(Now));
            return new Harness
            {
                CartId = cartId,
                Service = service,
                Drafts = drafts,
                Directory = directory,
                CartVersion = CartsFake.Version,
                SingleSellerOnly = singleSellerOnly,
            };
        }

        /// <summary>ذخیرهٔ واقعی دو‌گروهی: هر گروه یک خط، هر دو با همان روش canonical.</summary>
        public Task<StorefrontShippingDraftView> SaveTwoSellersAsync(
            string groupAWindow = "9-12",
            DateOnly? groupADate = null,
            string groupBMethod = Tipax,
            DateOnly? groupBDate = null) =>
            Service.SaveSelectionAsync(
                new StorefrontShippingSelectionRequest(
                    CartId,
                    CartVersion,
                    "حمید آزمون",
                    "09111111111",
                    "تهران",
                    "تهران",
                    "خیابان آزادی پلاک ۱",
                    "1234567890",
                    null,
                    Tipax,
                    groupADate?.ToString("yyyy-MM-dd") ?? MinA.ToString("yyyy-MM-dd"),
                    groupAWindow,
                    null,
                    "حمید",
                    "آزمون",
                    [
                        new StorefrontShippingGroupSelection(
                            Tipax,
                            [LineA],
                            null,
                            null,
                            null,
                            (groupADate ?? MinA).ToString("yyyy-MM-dd"),
                            groupAWindow),
                        new StorefrontShippingGroupSelection(
                            groupBMethod,
                            [LineB],
                            null,
                            null,
                            null,
                            (groupBDate ?? MinB).ToString("yyyy-MM-dd"),
                            "9-12"),
                    ]),
                GuestSecret,
                CancellationToken.None);

        /// <summary>شکل قدیمی تک‌روشی: بدون ShippingGroups، فقط اسکالر.</summary>
        public Task<StorefrontShippingDraftView> SaveLegacyAsync(string window) =>
            Service.SaveSelectionAsync(
                new StorefrontShippingSelectionRequest(
                    CartId,
                    CartVersion,
                    "حمید آزمون",
                    "09111111111",
                    "تهران",
                    "تهران",
                    "خیابان آزادی پلاک ۱",
                    "1234567890",
                    null,
                    Tipax,
                    MinA.ToString("yyyy-MM-dd"),
                    window,
                    null,
                    "حمید",
                    "آزمون",
                    null),
                GuestSecret,
                CancellationToken.None);
    }

    private sealed class CartsFake(Guid cartId, bool singleSellerOnly) : ICartPresentationGateway
    {
        public const int Version = 4;

        public Task<CartPage?> GetAsync(Guid id, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult(id == cartId ? Cart(id) : null);

        public Task<CartPage?> TryGetForOwnershipAsync(Guid id, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult(id == cartId ? Cart(id) : null);

        private CartPage Cart(Guid id)
        {
            var lines = new List<CartLineView>
            {
                Line(LineA, SellerA, "کالای الف", "فروشندهٔ الف"),
            };
            if (!singleSellerOnly)
            {
                lines.Add(Line(LineB, SellerB, "کالای ب", "فروشندهٔ ب"));
            }

            var subtotal = lines.Sum(x => x.LineAmountExclusiveOfTax ?? 0m);
            return new CartPage(
                id,
                Version,
                "IR",
                "IRR",
                "Marketplace",
                lines.Sum(x => x.Quantity),
                [new CartCurrencyTotal("IRR", subtotal)],
                lines,
                null,
                "Active");
        }

        private static CartLineView Line(Guid lineId, Guid sellerId, string title, string sellerName) =>
            new(
                lineId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                sellerId,
                null,
                null,
                title,
                sellerName,
                null,
                1m,
                1_000_000m,
                1_000_000m,
                "IRR",
                false);
    }

    /// <summary>پیش‌نویس درحافظه‌ای با همان معنای تولیدی: با اولین ذخیره ساخته و سپس درجا جایگزین می‌شود.</summary>
    private sealed class DraftStoreFake : IStorefrontShippingDraftStore
    {
        private CartShippingDraft? _draft;

        public int SaveCalls { get; private set; }

        public Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken) =>
            Task.FromResult(_draft is not null && _draft.CartId == cartId ? _draft : null);

        public Task SaveAsync(CartShippingDraft value, bool isNew, CancellationToken cancellationToken)
        {
            SaveCalls++;
            if (isNew)
            {
                _draft = value;
            }

            return Task.CompletedTask;
        }

        public Task<CartShippingDraft?> GetForCapturedSelectionAsync(Guid cartId, CancellationToken cancellationToken) =>
            GetByCartIdAsync(cartId, cancellationToken);

        public Task<CartShippingDraft> ReplaceCapturedSelectionAsync(
            CartShippingDraft value,
            IReadOnlyList<CartShippingDraftGroup> groups,
            bool isNew,
            long expectedSelectionRevision,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class ActorFake : IOrderStorefrontActor
    {
        public Guid GuestActorId { get; } = Guid.NewGuid();

        public bool IsAuthenticated => false;

        public Guid? AuthenticatedUserId => null;

        public Guid ResolvePlacementActor(bool usingSavedAddress) => GuestActorId;

        public Guid? TryResolveListActor() => null;

        public CartAccess BuildCartAccess(string? guestSecret) => new(null, guestSecret);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class ShippingSeedFake : IShippingCatalogSeedPort
    {
        public Task EnsureSeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class LanguagesFake : ILanguageLookup
    {
        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LanguageLookupSnapshot>>([]);
    }

    /// <summary>کاتالوگ واقعی‌نما: دو سرویس فعال با گزینهٔ standard ⇒ دو روش canonical.</summary>
    private sealed class ShippingCatalogFake : IShippingCatalogReader
    {
        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            var tipaxId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            return Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>(
            [
                new ShippingCatalogServiceSnapshot(
                    tipaxId,
                    "tipax",
                    "tipax",
                    "tipax",
                    "#000",
                    true,
                    0,
                    [],
                    [new ShippingCatalogOptionSnapshot(Guid.NewGuid(), tipaxId, "standard", true, 0, [])]),
                new ShippingCatalogServiceSnapshot(
                    postId,
                    "post",
                    "post",
                    "post",
                    "#000",
                    true,
                    1,
                    [],
                    [new ShippingCatalogOptionSnapshot(Guid.NewGuid(), postId, "standard", true, 0, [])]),
            ]);
        }

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    /// <summary>برچسب یک‌سطحی خالی: برچسب قدیمی کاتالوگ دست‌نخورده می‌ماند (fail-closed سازگار).</summary>
    private sealed class OneLevelLabelFake : IShippingOneLevelStorefrontLabelReader
    {
        public Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
            IReadOnlyCollection<string> methodCodes,
            Guid languageId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
    }

    /// <summary>هر Submit باید تست را بشکند: مسیر ارسال حق ندارد سفارش/رزرو/پرداخت بسازد.</summary>
    private sealed class DirectoryFake : ICheckoutDirectory
    {
        public int SubmitCalls { get; private set; }

        public Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
        {
            SubmitCalls++;
            throw new NotSupportedException();
        }

        /// <summary>پیش‌نمایش خواندنی سبد پرداخت capture هم در این تست‌ها بی‌مصرف است.</summary>
        public Task<CapturedPaymentDraftSnapshot> ProjectCapturedPaymentDraftAsync(
            SubmitCheckoutCommand command,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutSnapshot?> GetCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<SellerOrderSnapshot?> GetSellerOrderByNumberAsync(string orderNumber, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task CancelSellerOrderAsync(Guid sellerOrderId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RestoreCancelledCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CheckoutOperationalNoteSnapshot>> ListNotesAsync(
            Guid checkoutId,
            Guid viewerUserId,
            int take,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutOperationalNoteSnapshot> AddNoteAsync(
            Guid checkoutId,
            Guid actorUserId,
            string body,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutNoteDeleteOutcome> DeleteNoteAsync(
            Guid checkoutId,
            Guid noteId,
            Guid actorUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordAdminViewAsync(Guid checkoutId, Guid viewerUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
