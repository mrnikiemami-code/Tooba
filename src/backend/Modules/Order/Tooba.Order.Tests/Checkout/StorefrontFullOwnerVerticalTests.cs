using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Checkout.Persistence;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.Storefront;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-235-R1: a <b>real</b> multi-owner vertical (read planner → 232 selection writer → public payment
/// preview → public commit → persisted Order owners/evidence) on the task-owned disposable PostgreSQL, plus
/// the two per-line assignment regressions and the coherent payment-preview root.
/// </summary>
/// <remarks>
/// <para>
/// Controlled Contracts (Pricing/Promotion/Tax, 216 quote, 219 pointer, 228 assignment, Fulfillment catalog,
/// Geography, AddressBook, reservation/conversion) are <b>labelled fixtures</b>; the Order schema, migrations,
/// quote composition, captured acceptance, selection writer/store and persistence are real. Each fixture is
/// keyed by the <b>full</b> owner + binding and refuses an unknown owner instead of answering for everyone.
/// </para>
/// <para>
/// Nothing here claims HTTP, a live shopper, a PSP call or activation: the checked-in gates stay OFF and no
/// payment is started.
/// </para>
/// </remarks>
[Collection(OrderEvidenceRelationalCollection.Name)]
public sealed class StorefrontFullOwnerVerticalTests
{
    private static readonly Guid CartId = CapturedFinalSubmitIntegrationTests.CartId;
    private static readonly Guid LineA = CapturedFinalSubmitIntegrationTests.LineA;
    private static readonly Guid LineB = CapturedFinalSubmitIntegrationTests.LineB;
    private static readonly Guid LineC = Guid.Parse("01a17d10-0000-7000-8000-0000000000cc");
    private static readonly Guid OfferA = CapturedFinalSubmitIntegrationTests.OfferA;
    private static readonly Guid OfferB = CapturedFinalSubmitIntegrationTests.OfferB;
    private static readonly Guid OfferC = Guid.Parse("01a17d10-0000-7000-8000-0000000000c3");
    private static readonly Guid SellerA = CapturedFinalSubmitIntegrationTests.SellerA;
    private static readonly Guid SellerB = CapturedFinalSubmitIntegrationTests.SellerB;
    private static readonly Guid SellerC = Guid.Parse("01a17d10-0000-7000-8000-0000000000c2");
    private static readonly Guid TypeId = CapturedFinalSubmitIntegrationTests.ShippingTypeId;
    private static readonly Guid AreaGroup = CapturedFinalSubmitIntegrationTests.AreaGroup;
    private static readonly Guid Buyer = CapturedFinalSubmitIntegrationTests.Buyer;
    private const int CartVersion = 7;
    private const string Service = "post";
    private const string Option = "standard";

    /// <summary>کرایهٔ جابه‌جایی انتخاب در تست «ریشهٔ تازه»؛ هیچ سیاست تولیدی از آن خبر ندارد.</summary>
    private const decimal OwnerDelta = 40m;

    /// <summary>یک مالک واقعی با capture/ردیف/پول/تاریخ/ساعتِ <b>خودش</b>.</summary>
    internal sealed record OwnerFixture(
        ShippingCapturedQuoteOwnerKind Kind,
        Guid? Seller,
        Guid SnapshotId,
        Guid DraftId,
        long Revision,
        long SelectionVersion,
        string Label,
        Guid Row,
        decimal Freight,
        decimal Packaging,
        DateOnly Date,
        int Weekday,
        long OpenTicks,
        long CloseTicks)
    {
        internal decimal Payable => Freight + Packaging;

        internal string GroupKey => Kind switch
        {
            // افراز Platform مالکِ Marketplace است، اما کلید گروه روی <b>فروشندهٔ واقعی خط</b> ساخته
            // می‌شود؛ همان فروشنده‌ای که سبد و فکت‌های همان گروه دارند.
            ShippingCapturedQuoteOwnerKind.Marketplace =>
                $"cap|platform|marketplace|{SellerA:N}|{TypeId:N}",
            ShippingCapturedQuoteOwnerKind.Seller =>
                $"cap|seller|{Seller!.Value:N}|{TypeId:N}",
            _ => throw new InvalidOperationException($"unsupported fixture owner {Kind}"),
        };
    }

    private static readonly OwnerFixture MarketplaceOwner = new(
        ShippingCapturedQuoteOwnerKind.Marketplace,
        null,
        Guid.Parse("01a17d20-0000-7000-8000-0000000000a1"),
        Guid.Parse("01a17d21-0000-7000-8000-0000000000a1"),
        5,
        2,
        "مارکت‌پلیس",
        Guid.Parse("01a17d22-0000-7000-8000-0000000000a1"),
        120m,
        5m,
        new DateOnly(2026, 10, 27),
        2,
        new TimeOnly(9, 0).Ticks + 1_111_111L,
        new TimeOnly(17, 0).Ticks + 2_222_222L);

    private static readonly OwnerFixture SellerBOwner = new(
        ShippingCapturedQuoteOwnerKind.Seller,
        SellerB,
        Guid.Parse("01a17d20-0000-7000-8000-0000000000b1"),
        Guid.Parse("01a17d21-0000-7000-8000-0000000000b1"),
        6,
        3,
        "فروشندهٔ ب",
        Guid.Parse("01a17d22-0000-7000-8000-0000000000b1"),
        70m,
        3m,
        new DateOnly(2026, 10, 28),
        3,
        new TimeOnly(10, 0).Ticks + 3_333_333L,
        new TimeOnly(18, 0).Ticks + 4_444_444L);

    private static readonly OwnerFixture SellerCOwner = new(
        ShippingCapturedQuoteOwnerKind.Seller,
        SellerC,
        Guid.Parse("01a17d20-0000-7000-8000-0000000000c1"),
        Guid.Parse("01a17d21-0000-7000-8000-0000000000c1"),
        7,
        4,
        "فروشندهٔ ج",
        Guid.Parse("01a17d22-0000-7000-8000-0000000000c1"),
        90m,
        4m,
        new DateOnly(2026, 10, 29),
        4,
        new TimeOnly(11, 0).Ticks + 5_555_555L,
        new TimeOnly(19, 0).Ticks + 6_666_666L);

    internal static readonly OwnerFixture[] Owners = [MarketplaceOwner, SellerBOwner, SellerCOwner];

    /// <summary>مالک Domain که از همان سیاست مشترک برای این افراز انتظار می‌رود.</summary>
    private static ShippingGroupOwnerKind ExpectedOwnerKind(OwnerFixture owner) =>
        owner.Kind == ShippingCapturedQuoteOwnerKind.Marketplace
            ? ShippingGroupOwnerKind.Marketplace
            : ShippingGroupOwnerKind.Seller;

    /// <summary>
    /// <b>۱)</b> سبد Marketplace با <b>دو فروشندهٔ واقعی</b> و سه افراز (Platform با مالک Marketplace،
    /// Seller A، Seller B): planner واقعی → نویسندهٔ ۲۳۲ → پیش‌نمایش عمومی → ثبت عمومی → مالک/شاهد
    /// پایدارشدهٔ Order.
    /// </summary>
    [Fact]
    public async Task Marketplace_mixed_cart_is_read_written_previewed_and_committed_per_full_owner()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var vertical = new Vertical(db, marketplace: true);

        // --- خواندن/نوشتن واقعی از همان سرویس عمومی انتخاب، با همان قصدهای نامعتمد کلاینت.
        var saved = await vertical.Shipping.SaveSelectionAsync(
            vertical.SelectionRequest("0"),
            guestSecret: null,
            CancellationToken.None);

        Assert.Equal(3, saved.ShippingGroups!.Count);
        Assert.Equal(3, vertical.Core.Pointers.Calls);   // یک ۲۱۹ برای هر Binding کامل
        Assert.Equal(3, vertical.Core.Quotes.Calls);     // یک ۲۱۶ برای هر گروه منتخب

        var persisted = await db.ShippingDrafts
            .Include(x => x.ShippingGroups).ThenInclude(group => group.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(1, persisted.SelectionRevision);
        var storedGroups = persisted.ShippingGroups.ToArray();
        Assert.Equal(3, storedGroups.Length);

        // هر افراز شاهد <b>خودش</b> را دارد: مالک/ردیف/پول/تاریخ/تیک هرگز از گروه دیگر نمی‌آید.
        foreach (var owner in Owners)
        {
            var group = Assert.Single(storedGroups, x => x.AcceptedEvidence!.DraftId == owner.DraftId);
            var evidence = group.AcceptedEvidence!;
            Assert.Equal(ExpectedOwnerKind(owner), evidence.Owner.Kind);
            Assert.Equal(owner.Seller, evidence.Owner.SellerPartyId);
            Assert.Null(evidence.Owner.TenantKey);
            Assert.Equal(owner.SnapshotId, evidence.SnapshotId);
            Assert.Equal(owner.Row, evidence.RateRowId);
            Assert.Equal(owner.Freight, evidence.FreightAmount);
            Assert.Equal(owner.Packaging, evidence.PackagingAmount);
            Assert.Equal(owner.Payable, evidence.PayableAtCheckoutAmount);
            Assert.Equal(owner.Date, evidence.ReceivingDate);
            Assert.Equal(owner.OpenTicks, evidence.ReceivingOpenTicks);
            Assert.Equal(owner.CloseTicks, evidence.ReceivingCloseTicks);
        }

        // --- نمای «پیش‌نویس پرداخت» عمومی: همان گروه‌ها با برچسب/پول/تاریخ <b>خودشان</b>.
        var pointersBeforePreview = vertical.Core.Pointers.Calls;
        var quotesBeforePreview = vertical.Core.Quotes.Calls;
        var preview = await vertical.Shipping.ProjectPaymentDraftAsync(
            CartId,
            null,
            couponCode: null,
            CancellationToken.None);
        Assert.Equal(3, vertical.Core.Pointers.Calls - pointersBeforePreview);
        Assert.Equal(3, vertical.Core.Quotes.Calls - quotesBeforePreview);
        Assert.Null(preview.CheckoutId);
        Assert.Equal(3, preview.ShippingGroups!.Count);
        Assert.Equal("1", preview.SelectionRevision);
        Assert.Equal(Owners.Sum(owner => owner.Payable), preview.ShippingAmount);
        foreach (var owner in Owners)
        {
            var view = Assert.Single(preview.ShippingGroups!, x => x.MethodLabel == owner.Label);
            Assert.Equal(owner.Payable, view.ShippingAmount);
            Assert.Equal(Service, view.ServiceCode);
            Assert.Equal(Option, view.OptionCode);
            Assert.Equal(
                owner.Date,
                DateOnly.ParseExact(view.SelectedDeliveryDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture));
            Assert.Null(view.OrderLineIds);
            Assert.Single(view.CartLineIds!);
        }

        // برچسب‌ها هرگز جابه‌جا نمی‌شوند: هر برچسب دقیقاً یک گروه و همان گروهِ مالک خودش است.
        Assert.Equal(3, preview.ShippingGroups!.Select(group => group.MethodLabel).Distinct().Count());

        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(0, vertical.Core.Reservation.Calls);

        // --- ثبت عمومی: همان مالک/شاهد روی Order پایدار می‌نشیند.
        var pointersBeforeCommit = vertical.Core.Pointers.Calls;
        var quotesBeforeCommit = vertical.Core.Quotes.Calls;
        var committed = await vertical.Shipping.CommitAsync(
            CartId,
            null,
            CartVersion,
            "tb235r1-full-owner",
            couponCode: null,
            expectedSelectionRevision: "1",
            CancellationToken.None);

        Assert.NotNull(committed.CheckoutId);
        Assert.Equal(3, vertical.Core.Pointers.Calls - pointersBeforeCommit);
        Assert.Equal(3, vertical.Core.Quotes.Calls - quotesBeforeCommit);
        Assert.Equal(1, vertical.Core.Reservation.Calls);
        Assert.Equal(1, vertical.Core.Conversion.Calls);

        db.ChangeTracker.Clear();
        var checkout = await db.Checkouts
            .Include(x => x.ShippingGroups).ThenInclude(group => group.Lines)
            .SingleAsync(x => x.CartId == CartId);
        var orderGroups = checkout.ShippingGroups.ToArray();
        Assert.Equal(3, orderGroups.Length);
        foreach (var owner in Owners)
        {
            var group = Assert.Single(orderGroups, x => x.AcceptedEvidence!.DraftId == owner.DraftId);
            var evidence = group.AcceptedEvidence!;
            Assert.Equal(ExpectedOwnerKind(owner), evidence.Owner.Kind);
            Assert.Equal(owner.Seller, evidence.Owner.SellerPartyId);
            Assert.Equal(owner.Payable, group.ShippingChargeSnapshot);
            Assert.Equal(owner.Row, evidence.RateRowId);
            Assert.Equal(owner.Date, evidence.ReceivingDate);
            Assert.Equal(owner.OpenTicks, evidence.ReceivingOpenTicks);
            if (owner.Kind == ShippingCapturedQuoteOwnerKind.Seller)
            {
                // مالک Seller دقیقاً فروشندهٔ واقعی همان گروه است (هیچ مالکی به Store گِرد نمی‌شود).
                Assert.Equal(owner.Seller, group.SellerPartyId);
            }
        }
    }

    /// <summary>
    /// <b>۲)</b> پاسخ‌های اشاره‌گر Seller A ↔ Seller B جابه‌جا شوند: ثبت با کد مالک خودش رد می‌شود و هیچ
    /// رزرو/سفارش/گروهی ساخته نمی‌شود — شاهد ذخیره‌شده اعتبارسنج خودش نیست.
    /// </summary>
    [Fact]
    public async Task Swapped_owner_captures_are_refused_before_any_reservation_or_order()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var vertical = new Vertical(db, marketplace: true);
        await vertical.Shipping.SaveSelectionAsync(vertical.SelectionRequest("0"), guestSecret: null, CancellationToken.None);

        // فقط پاسخ کارگزار جابه‌جا می‌شود؛ خودِ شاهد ذخیره‌شده دست‌نخورده است.
        vertical.SwapSellerCaptures();

        var refused = await Assert.ThrowsAsync<Tooba.BuildingBlocks.ContractOperationException>(() =>
            vertical.Shipping.CommitAsync(
                CartId,
                null,
                CartVersion,
                "tb235r1-swapped",
                couponCode: null,
                expectedSelectionRevision: "1",
                CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.Conflict, refused.Code);
        Assert.Equal(0, vertical.Core.Reservation.Calls);
        Assert.Equal(0, vertical.Core.Conversion.Calls);
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(0, await db.Set<OrderShippingGroup>().CountAsync());
        Assert.Equal(1, (await db.ShippingDrafts.SingleAsync(x => x.CartId == CartId)).SelectionRevision);
    }

    /// <summary>
    /// <b>۳)</b> انتسابی که زیر کلید درست آفرِ <b>دیگر</b> را حمل می‌کند، انتسابِ این خط نیست: پیش از هر
    /// رزرو/سفارش رد می‌شود (پیش از این فقط کلید فرهنگ بررسی می‌شد).
    /// </summary>
    [Fact]
    public async Task Assignment_payload_for_another_offer_is_refused_per_line()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var harness = new CapturedFinalSubmitIntegrationTests.CapturedSubmitHarness(db);
        harness.UseSingleSellerTwoOfferCart();
        await CapturedFinalSubmitIntegrationTests.SeedSingleGroupDraftAsync(db, SellerA, [LineA, LineB]);

        harness.Assignments.Responsibility[OfferA] = OfferFulfillmentResponsibility.Seller;
        harness.Assignments.Responsibility[OfferB] = OfferFulfillmentResponsibility.Seller;
        harness.Assignments.SellerOf[OfferB] = SellerA;
        harness.Assignments.PayloadOfferId[OfferA] = OfferB;

        var refused = await Assert.ThrowsAsync<Tooba.BuildingBlocks.ContractOperationException>(() =>
            harness.Directory.SubmitAsync(harness.Command(expectedSelectionRevision: "1"), CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.Unavailable, refused.Code);
        Assert.Equal(0, harness.Reservation.Calls);
        Assert.Equal(0, harness.Conversion.Calls);
        db.ChangeTracker.Clear();
        Assert.False(await db.Checkouts.AnyAsync(x => x.CartId == CartId));
        Assert.Equal(0, await db.Set<OrderShippingGroup>().CountAsync());
    }

    /// <summary>
    /// <b>۴)</b> <c>null</c> مسئولیت در خط اول یک «انبارهٔ مقدارنشده» نیست و با خط دومِ معتبر جبران
    /// نمی‌شود؛ ترتیب معکوس هم همان رد را می‌دهد.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_responsibility_cannot_be_repaired_by_a_later_line_in_either_order(bool reverse)
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var harness = new CapturedFinalSubmitIntegrationTests.CapturedSubmitHarness(db);
        harness.UseSingleSellerTwoOfferCart();
        await CapturedFinalSubmitIntegrationTests.SeedSingleGroupDraftAsync(db, SellerA, [LineA, LineB]);

        harness.Assignments.SellerOf[OfferB] = SellerA;
        if (reverse)
        {
            harness.Assignments.Responsibility[OfferA] = OfferFulfillmentResponsibility.Seller;
            harness.Assignments.Responsibility[OfferB] = null;
        }
        else
        {
            harness.Assignments.Responsibility[OfferA] = null;
            harness.Assignments.Responsibility[OfferB] = OfferFulfillmentResponsibility.Seller;
        }

        var refused = await Assert.ThrowsAsync<Tooba.BuildingBlocks.ContractOperationException>(() =>
            harness.Directory.SubmitAsync(harness.Command(expectedSelectionRevision: "1"), CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.Unavailable, refused.Code);
        Assert.Equal(0, harness.Reservation.Calls);
        Assert.Equal(0, harness.Conversion.Calls);
        db.ChangeTracker.Clear();
        Assert.False(await db.Checkouts.AnyAsync(x => x.CartId == CartId));
        Assert.Equal(0, await db.Set<OrderShippingGroup>().CountAsync());
    }

    /// <summary>
    /// <b>۵)</b> پیش‌نمایش عمومی و ریشهٔ Order یک چیز را توصیف می‌کنند: انتخاب بین خواندن بیرونی و پذیرش
    /// تازهٔ داخلی <b>جابه‌جا</b> می‌شود و پاسخ همان ریشهٔ <b>تازه</b> است (پول/گروه‌ها/نسخهٔ یکسان)، نه
    /// مبلغ تازه با تاریخ/روش کهنه — و هیچ نوشتن/رزرو/سفارشی از خواندن حاصل نمی‌شود.
    /// </summary>
    [Fact]
    public async Task Payment_preview_describes_the_newly_accepted_root_not_the_stale_outer_read()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var vertical = new Vertical(db, marketplace: true);
        await vertical.Shipping.SaveSelectionAsync(vertical.SelectionRequest("0"), guestSecret: null, CancellationToken.None);

        // بین دو خواندن، همان نویسندهٔ واقعی انتخاب را جلو می‌برد: کرایهٔ هر مالک +۴۰ و نسخهٔ ریشه ۱ → ۲.
        await vertical.MoveSelectionToSecondRevisionAsync();

        var page = await vertical.Shipping.ProjectPaymentDraftAsync(
            CartId,
            null,
            couponCode: null,
            CancellationToken.None);

        Assert.Equal("2", page.SelectionRevision);
        Assert.Equal(Owners.Sum(owner => owner.Payable + OwnerDelta), page.ShippingAmount);
        Assert.Equal(3, page.ShippingGroups!.Count);
        foreach (var owner in Owners)
        {
            var view = Assert.Single(page.ShippingGroups!, x => x.MethodLabel == owner.Label);
            Assert.Equal(owner.Payable + OwnerDelta, view.ShippingAmount);
            Assert.Equal(
                owner.Date,
                DateOnly.ParseExact(view.SelectedDeliveryDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(0, await db.Set<OrderShippingGroup>().CountAsync());
        Assert.Equal(0, vertical.Core.Reservation.Calls);
        Assert.Equal(0, vertical.Core.Conversion.Calls);
        Assert.Equal(2, (await db.ShippingDrafts.SingleAsync(x => x.CartId == CartId)).SelectionRevision);
    }

    /// <summary>
    /// سیم‌کشی <b>واقعی</b> ویترین روی همان دایرکتوری پذیرفته‌شده: planner/writer/Checkout/Shipping واقعی و
    /// فقط Contracts کنترل‌شده.
    /// </summary>
    internal sealed class Vertical : IDisposable
    {
        internal readonly CapturedFinalSubmitIntegrationTests.CapturedSubmitHarness Core;
        internal readonly StorefrontShippingDraftStore Drafts;
        internal readonly StorefrontCapturedPreviewPlanner Planner;
        internal readonly StorefrontCapturedSelectionWriter Writer;
        internal readonly StorefrontCheckoutService Checkouts;
        internal readonly StorefrontShippingService Shipping;
        internal readonly Presentation Carts = new();
        private readonly FixedClock _clock = new();
        private readonly CapturedFinalSubmitIntegrationTests.FixedStorefrontActor _actor = new();
        private decimal _delta;

        internal Vertical(
            OrderDbContext db,
            bool marketplace,
            Func<IStorefrontShippingDraftStore, IStorefrontShippingDraftStore>? wrapDrafts = null)
        {
            Core = new CapturedFinalSubmitIntegrationTests.CapturedSubmitHarness(
                db,
                edition: marketplace ? Tooba.BuildingBlocks.ToobaEdition.Marketplace : Tooba.BuildingBlocks.ToobaEdition.SingleStore,
                tenantId: marketplace ? null : "store-alpha");

            // سبد سه‌خطی با دو فروشندهٔ واقعی: A/A/B و سه افراز مستقل.
            Core.Carts.Cart = Snapshot();
            // همان آفرهای واقعی سبد (B با فروشندهٔ الف، C با فروشندهٔ ب) واقعاً همان فروشنده را بیان
            // می‌کنند تا نقل‌قول اولیه به تناقض فروشنده نرسد.
            Core.Offers.Register(new OfferReference(
                OfferC, CapturedFinalSubmitIntegrationTests.VariantOf(OfferC), SellerC,
                SalesChannel.Direct, OfferStatus.Active, null));
            Core.Prices.Register(new PriceQuote(
                CapturedFinalSubmitIntegrationTests.PriceOf(OfferC), OfferC, "IR",
                SalesChannel.Direct, 300m, "IRR", true, true));
            Core.Assignments.Responsibility[OfferA] = OfferFulfillmentResponsibility.Platform;
            Core.Assignments.Responsibility[OfferB] = OfferFulfillmentResponsibility.Seller;
            Core.Assignments.Responsibility[OfferC] = OfferFulfillmentResponsibility.Seller;
            Core.Assignments.SellerOf[OfferA] = SellerA;
            Core.Assignments.SellerOf[OfferB] = SellerB;
            Core.Assignments.SellerOf[OfferC] = SellerC;

            // پاسخ‌های Contracts با کلید <b>مالک کامل + Binding</b>؛ مالک ناشناس پاسخ نمی‌گیرد.
            Core.Pointers.Respond = request => Owners
                .Where(owner => owner.Kind == request.OwnerKind
                    && owner.Seller == request.OwnerSellerPartyId
                    && string.Equals(request.ServiceCode, Service, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(request.OptionCode, Option, StringComparison.OrdinalIgnoreCase))
                .Select(owner => Pointer(owner, request))
                .SingleOrDefault();
            Core.Quotes.Respond = request => Owners
                .Where(owner => owner.DraftId == request.DraftId)
                .Select(owner => Quote(owner, request))
                .SingleOrDefault();

            Drafts = new StorefrontShippingDraftStore(db);
            var options = Options.Create(new StorefrontCapturedPreviewOptions
            {
                Enabled = true,
                SelectionEnabled = true,
                CheckoutEnabled = true,
                AvailableDateCount = 7,
            });
            Planner = new StorefrontCapturedPreviewPlanner(
                Core.Assignments,
                Core.ShippingCatalog,
                Core.Types,
                Core.Pointers,
                new StorefrontCapturedGroupProjectionComposer(Core.Directory, Core.Quotes, _clock),
                new CapturedFinalSubmitIntegrationTests.FakeEdition(
                    marketplace ? Tooba.BuildingBlocks.ToobaEdition.Marketplace : Tooba.BuildingBlocks.ToobaEdition.SingleStore),
                new CapturedFinalSubmitIntegrationTests.FakeTenant(marketplace ? null : "store-alpha"),
                options);
            Writer = new StorefrontCapturedSelectionWriter(Planner, Drafts, Core.Geography, _clock);
            Checkouts = new StorefrontCheckoutService(Carts, Core.Directory, Core.Addresses, _actor);
            Shipping = new StorefrontShippingService(
                Carts,
                Checkouts,
                Core.Addresses,
                Core.ShippingCatalog,
                null!,
                // TB-236: یک پورت تست می‌تواند <b>بین</b> خواندن پیش‌نویس توسط facade و خواندن تازهٔ هسته
                // عمل کند تا مرز واقعی خواندن بیرونی/پذیرش تازه اثبات شود (بدون hook تولیدی).
                wrapDrafts?.Invoke(Drafts) ?? Drafts,
                null!,
                new ShippingMethodsOptions(),
                null!,
                _actor,
                new FixedClock(),
                capturedPreview: Planner,
                capturedSelection: Writer);
        }

        /// <summary>همان قصد نامعتمد کلاینت برای سه افراز (کلیدهای سرور + بازتاب دقیق).</summary>
        internal StorefrontShippingSelectionRequest SelectionRequest(string expectedRevision) => new(
            CartId,
            CartVersion,
            "گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            "نشانی",
            "1234567890",
            null,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            FirstName: "گیرنده",
            LastName: "آزمایشی",
            CapturedGroups: Owners
                .Select(owner => new StorefrontCapturedGroupSelection(
                    owner.GroupKey,
                    Service,
                    Option,
                    owner.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ExpectedSnapshotId: owner.SnapshotId.ToString("D"),
                    ExpectedSourceRevision: owner.Revision.ToString(CultureInfo.InvariantCulture),
                    ExpectedSelectionVersion: owner.SelectionVersion.ToString(CultureInfo.InvariantCulture),
                    ExpectedAreaGroupId: AreaGroup.ToString("D"),
                    ExpectedRateRowId: owner.Row.ToString("D")))
                .ToArray(),
            ExpectedSelectionRevision: expectedRevision);

        /// <summary>شاهد ذخیره‌شده دست‌نخورده می‌ماند؛ فقط پاسخ مالک A/B جابه‌جا می‌شود.</summary>
        internal void SwapSellerCaptures()
        {
            var original = Core.Pointers.Respond!;
            Core.Pointers.Respond = request =>
            {
                if (request.OwnerKind == ShippingCapturedQuoteOwnerKind.Seller
                    && request.OwnerSellerPartyId is Guid seller
                    && (seller == SellerB || seller == SellerC))
                {
                    return Pointer(seller == SellerB ? SellerCOwner : SellerBOwner, request);
                }

                return original!(request);
            };
        }

        /// <summary>
        /// انتخاب بین «خواندن بیرونی» و «پذیرش تازه» با <b>همان نویسندهٔ واقعی</b> جابه‌جا می‌شود: کرایهٔ هر
        /// مالک جلو می‌رود و نسخهٔ ریشه ۱ → ۲ می‌شود، در حالی که فراخوان همچنان ریشهٔ کهنه را در دست دارد.
        /// </summary>
        internal async Task MoveSelectionToSecondRevisionAsync()
        {
            _delta = OwnerDelta;
            var prepared = await Checkouts.PrepareShippingAsync(
                new StorefrontCheckoutShippingInput(
                    "گیرنده",
                    "09120000000",
                    "تهران",
                    "تهران",
                    "نشانی",
                    "1234567890",
                    null,
                    string.Empty,
                    string.Empty),
                CancellationToken.None);
            var existing = await Drafts.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
            await Writer.SaveAsync(
                Carts.Page,
                new CartAccess(Buyer, null),
                prepared,
                existing,
                null,
                SelectionRequest("1"),
                CancellationToken.None);
        }

        private ShippingSelectedCapturedRevisionResult Pointer(
            OwnerFixture owner,
            ShippingSelectedCapturedRevisionRequest request) =>
            new(
                ShippingSelectedCaptureVerdict.Selected,
                null,
                ShippingSelectedCaptureReadState.Selected,
                owner.SelectionVersion,
                owner.SnapshotId,
                owner.DraftId,
                owner.Revision,
                1,
                request.ServiceCode,
                request.OptionCode,
                TypeId,
                [new ShippingSelectedCapturedTranslation(Guid.Empty, owner.Label, null)]);

        private ShippingCapturedQuoteResult Quote(OwnerFixture owner, ShippingCapturedQuoteLookupRequest request) =>
            new(
                ShippingCapturedQuoteVerdict.Quoted,
                ShippingCapturedQuoteVerdictCodes.Quoted,
                Service,
                Option,
                "IRR",
                owner.Freight + _delta,
                owner.Packaging,
                owner.Payable + _delta,
                owner.Payable + _delta,
                0m,
                Settings: null,
                SnapshotId: owner.SnapshotId,
                DraftId: request.DraftId,
                SourceRevision: request.SourceRevision,
                FormatVersion: 1,
                ShippingTypeId: TypeId,
                GroupId: AreaGroup,
                RateRowId: owner.Row,
                MinDeliveryDays: 2,
                MaxDeliveryDays: 7,
                ReceivingWeek: Week(owner));

        /// <summary>هفتهٔ <b>ذخیره‌شدهٔ هر مالک</b>: ساعت کسری خودش روی روزِ تاریخ منتخب خودش.</summary>
        private static ShippingCapturedReceivingWeek Week(OwnerFixture owner) => new(
            "Asia/Tehran",
            Enumerable.Range(1, 7)
                .Select(iso => iso == owner.Weekday
                    ? new ShippingCapturedReceivingWeekDay(iso, true, new TimeOnly(owner.OpenTicks), new TimeOnly(owner.CloseTicks))
                    : new ShippingCapturedReceivingWeekDay(iso, true, new TimeOnly(9, 0), new TimeOnly(17, 0)))
                .ToArray());

        private static CartSnapshot Snapshot() => new(
            CartId,
            CartStatus.Active,
            CartAccessKind.Authenticated,
            Buyer,
            "IR",
            "IRR",
            SalesChannel.Direct,
            new DateTimeOffset(2026, 10, 25, 11, 0, 0, TimeSpan.Zero),
            CartConversionIntent.OnlinePurchase,
            CartVersion,
            [
                Line(LineA, OfferA, SellerA, 100m),
                Line(LineB, OfferB, SellerB, 200m),
                Line(LineC, OfferC, SellerC, 300m),
            ]);

        private static CartLineSnapshot Line(Guid lineId, Guid offerId, Guid seller, decimal amount) => new(
            lineId,
            offerId,
            CapturedFinalSubmitIntegrationTests.VariantOf(offerId),
            seller,
            1m,
            ReservationId: null,
            QuotedAmount: amount,
            QuotedCurrency: "IRR",
            QuotedTaxExclusive: true,
            PriceId: CapturedFinalSubmitIntegrationTests.PriceOf(offerId),
            QuotedAt: new DateTimeOffset(2026, 10, 25, 10, 0, 0, TimeSpan.Zero));

        public void Dispose() => Core.Dispose();
    }

    /// <summary>صفحهٔ سبد ارائهٔ واقعی سرور با همان سه خط (عنوان/فروشنده/نسخه).</summary>
    internal sealed class Presentation : ICartPresentationGateway
    {
        internal CartPage Page => new(
            CartId,
            CartVersion,
            "IR",
            "IRR",
            "Direct",
            2m,
            [new CartCurrencyTotal("IRR", 600m)],
            [
                View(LineA, OfferA, SellerA, "کالای الف", "فروشندهٔ الف", 100m),
                View(LineB, OfferB, SellerB, "کالای ب", "فروشندهٔ ب", 200m),
                View(LineC, OfferC, SellerC, "کالای پ", "فروشندهٔ ج", 300m),
            ],
            GuestSecret: null);

        public Task<CartPage?> GetAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult<CartPage?>(Page);

        public Task<CartPage?> TryGetForOwnershipAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult<CartPage?>(Page);

        private static CartLineView View(
            Guid lineId,
            Guid offerId,
            Guid seller,
            string title,
            string sellerName,
            decimal amount) => new(
            lineId,
            offerId,
            CapturedFinalSubmitIntegrationTests.VariantOf(offerId),
            seller,
            null,
            null,
            title,
            sellerName,
            null,
            1m,
            amount,
            amount,
            "IRR",
            true);
    }

    private sealed class FixedClock : Tooba.BuildingBlocks.IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);
    }
}
