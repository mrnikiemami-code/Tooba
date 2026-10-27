using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Geography.Contracts.Places;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Checkout.Persistence;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.Storefront;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-SHIPPING-CAPTURED-COUPON-CONTEXT-PARITY-236: <b>یک</b> قصد کوپن از خواندن تا انتخاب، پیش‌نمایش و
/// ثبت نهایی، با مبلغ کالای تأییدشدهٔ هر گروه.
/// </summary>
/// <remarks>
/// این کلاس هیچ موتور تخفیف نمی‌سازد: قیمت/تخفیف/مالیات از همان مسیر واقعی ۲۱۷ (Promotion/Tax) می‌آید و
/// فقط <b>هم‌ارزی قصد و مبلغ</b> اثبات می‌شود. پایگاه‌داده فقط هدف دورانداختنی همین تسک است.
/// </remarks>
[Collection(OrderEvidenceRelationalCollection.Name)]
public sealed class StorefrontCapturedCouponParityTests
{
    private static readonly Guid CartId = CapturedFinalSubmitIntegrationTests.CartId;
    private const int CartVersion = 7;
    private const string Coupon = " SAVE10 ";
    private const string CanonicalCoupon = "SAVE10";

    private static Guid OfferA => CapturedFinalSubmitIntegrationTests.OfferA;

    /// <summary>
    /// <b>۱)</b> همان قصد کوپن به Promotion/خواندنِ واقعی می‌رسد، در انتخاب ذخیره می‌شود، پیش‌نمایش و ثبت
    /// نهایی هم با همان قصد انجام می‌شوند و مبلغ کالای هر گروه همان چیزی است که آن قصد ساخت.
    /// </summary>
    [Fact]
    public async Task One_coupon_reaches_read_selection_preview_and_commit_with_own_group_goods()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var vertical = new StorefrontFullOwnerVerticalTests.Vertical(db, marketplace: true);

        // کوپن فقط روی آفر گروه اول اثر دارد: «کاربرد جزئی» باید مجاز بماند (نه رد کل سبد).
        vertical.Core.Promotions.DiscountsByCoupon[CanonicalCoupon] = new Dictionary<Guid, decimal>
        {
            [OfferA] = 10m,
        };

        // --- خواندنِ واقعی با قصد کوپن.
        vertical.Core.Promotions.CouponsSeen.Clear();
        var read = await vertical.Planner.BuildAsync(
            vertical.Carts.Page,
            new CartAccess(CapturedFinalSubmitIntegrationTests.Buyer, null),
            await DestinationAsync(vertical),
            "IRR",
            Guid.Empty,
            CancellationToken.None,
            Coupon);

        Assert.True(read.Enabled);
        Assert.Null(read.NoticeReason);
        Assert.Equal(3, read.Groups.Count);
        // خود Promotion همان متن trim‌شده را دید (بدون case-fold/برش در Order).
        Assert.NotEmpty(vertical.Core.Promotions.CouponsSeen);
        Assert.All(vertical.Core.Promotions.CouponsSeen, seen => Assert.Equal(CanonicalCoupon, seen));

        // 100 − 10 + 9٪ = 99 و گروه‌های دیگر بدون تخفیف: 200 + 20 = 220 و 300 + 30 = 330.
        Assert.Equal(
            ["99.00", "220.00", "330.00"],
            read.Groups.Select(group => group.FinalGoodsAmountAfterDiscountAndTax).ToArray());

        var quotesBeforeSave = vertical.Core.Quotes.Calls;

        // --- انتخاب با همان قصد؛ بازتاب مبلغ کالای همان خواندن.
        var saved = await vertical.Shipping.SaveSelectionAsync(
            vertical.SelectionRequest("0") with
            {
                CouponCode = Coupon,
                CapturedGroups = vertical.SelectionRequest("0").CapturedGroups!
                    .Select((group, index) => group with
                    {
                        ExpectedGoodsAmount = index switch
                        {
                            0 => "99.00",
                            1 => "220.00",
                            _ => "330.00",
                        },
                    })
                    .ToArray(),
            },
            guestSecret: null,
            CancellationToken.None);

        Assert.Equal(3, saved.ShippingGroups!.Count);
        Assert.Equal(3, vertical.Core.Quotes.Calls - quotesBeforeSave);

        db.ChangeTracker.Clear();
        var persisted = await db.ShippingDrafts
            .Include(x => x.ShippingGroups).ThenInclude(group => group.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(CanonicalCoupon, persisted.SelectionCouponCode);
        Assert.Equal(1, persisted.SelectionRevision);
        Assert.Equal(
            [99m, 220m, 330m],
            persisted.ShippingGroups
                .OrderBy(group => group.ConfirmedGoodsAmount)
                .Select(group => group.ConfirmedGoodsAmount!.Value)
                .ToArray());

        // --- پیش‌نمایش پرداخت با همان قصد: پذیرفته می‌شود و مبلغ گروه‌ها همان است.
        var pointersBeforePreview = vertical.Core.Pointers.Calls;
        var preview = await vertical.Shipping.ProjectPaymentDraftAsync(
            CartId,
            null,
            Coupon,
            CancellationToken.None);
        Assert.Equal(3, preview.ShippingGroups!.Count);
        Assert.Equal(3, vertical.Core.Pointers.Calls - pointersBeforePreview);
        Assert.Equal(649m, preview.PayableAmount - preview.ShippingAmount);

        // قصد دیگر (پاک‌کردن) نمای تازه نمی‌سازد: هیچ تخفیفی بی‌تأیید عبور نمی‌کند.
        var cleared = await Assert.ThrowsAsync<ContractOperationException>(() =>
            vertical.Shipping.ProjectPaymentDraftAsync(CartId, null, couponCode: null, CancellationToken.None));
        Assert.Equal(StorefrontCapturedSelectionReasons.Conflict, cleared.Code);

        var other = await Assert.ThrowsAsync<ContractOperationException>(() =>
            vertical.Shipping.ProjectPaymentDraftAsync(CartId, null, "SAVE20", CancellationToken.None));
        Assert.Equal(StorefrontCapturedSelectionReasons.Conflict, other.Code);

        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(0, vertical.Core.Reservation.Calls);

        // --- ثبت نهایی با همان قصد: یک نقل‌قول کل سبد، یک رزرو، یک تبدیل.
        var quotesBeforeCommit = vertical.Core.Quotes.Calls;
        var committed = await vertical.Shipping.CommitAsync(
            CartId,
            null,
            CartVersion,
            "tb236-coupon",
            Coupon,
            expectedSelectionRevision: "1",
            CancellationToken.None);

        Assert.NotNull(committed.CheckoutId);
        Assert.Equal(3, vertical.Core.Quotes.Calls - quotesBeforeCommit);
        Assert.Equal(1, vertical.Core.Reservation.Calls);
        Assert.Equal(1, vertical.Core.Conversion.Calls);

        db.ChangeTracker.Clear();
        var checkout = await db.Checkouts
            .Include(x => x.ShippingGroups).ThenInclude(group => group.Lines)
            .Include(x => x.SellerOrders).ThenInclude(order => order.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(3, checkout.ShippingGroups.Count);
        Assert.Equal(3, checkout.SellerOrders.Sum(order => order.Lines.Count));
    }

    /// <summary>
    /// <b>۲)</b> قصد عوض‌شده بین خواندن و نوشتن با بازتاب مبلغ کالای خواندنِ کهنه رد می‌شود: هیچ ریشه/
    /// نسخه‌ای تغییر نمی‌کند و انتخاب بعدی با زمینهٔ تازه قانونی است.
    /// </summary>
    [Fact]
    public async Task Changed_coupon_with_stale_goods_echo_is_refused_without_any_write()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var vertical = new StorefrontFullOwnerVerticalTests.Vertical(db, marketplace: true);
        vertical.Core.Promotions.DiscountsByCoupon[CanonicalCoupon] = new Dictionary<Guid, decimal>
        {
            [OfferA] = 10m,
        };
        vertical.Core.Promotions.DiscountsByCoupon["SAVE20"] = new Dictionary<Guid, decimal>
        {
            [OfferA] = 20m,
        };

        await vertical.Shipping.SaveSelectionAsync(
            vertical.SelectionRequest("0") with { CouponCode = Coupon },
            guestSecret: null,
            CancellationToken.None);

        // همان بازتاب مبلغ خواندنِ کوپن قبلی ولی قصد تازه: وضعیت عوض شده است.
        var refused = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            vertical.Shipping.SaveSelectionAsync(
                vertical.SelectionRequest("1") with
                {
                    CouponCode = "SAVE20",
                    CapturedGroups = vertical.SelectionRequest("1").CapturedGroups!
                        .Select((group, index) => group with
                        {
                            ExpectedGoodsAmount = index switch
                            {
                                0 => "99.00",
                                1 => "220.00",
                                _ => "330.00",
                            },
                        })
                        .ToArray(),
                },
                guestSecret: null,
                CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.Conflict, refused.Code);

        db.ChangeTracker.Clear();
        var unchanged = await db.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(1, unchanged.SelectionRevision);
        Assert.Equal(CanonicalCoupon, unchanged.SelectionCouponCode);
        Assert.All(unchanged.ShippingGroups, group => Assert.NotNull(group.ConfirmedGoodsAmount));

        // انتخاب تازه با زمینه و بازتاب درست، قانونی است و نسخه را یک واحد جلو می‌برد.
        await vertical.Shipping.SaveSelectionAsync(
            vertical.SelectionRequest("1") with
            {
                CouponCode = "SAVE20",
                CapturedGroups = vertical.SelectionRequest("1").CapturedGroups!
                    .Select((group, index) => group with
                    {
                        ExpectedGoodsAmount = index switch
                        {
                            0 => "88.00",
                            1 => "220.00",
                            _ => "330.00",
                        },
                    })
                    .ToArray(),
            },
            guestSecret: null,
            CancellationToken.None);

        db.ChangeTracker.Clear();
        var reselected = await db.ShippingDrafts.SingleAsync(x => x.CartId == CartId);
        Assert.Equal(2, reselected.SelectionRevision);
        Assert.Equal("SAVE20", reselected.SelectionCouponCode);
    }

    /// <summary>
    /// <b>۳)</b> همان کد، ولی نتیجهٔ Promotion عوض شده (مبلغ کالا دیگر همان نیست) — حتی با کرایه/ردیف
    /// یکسان — پیش‌نمایش و ثبت را پیش از هر رزرو/سفارش رد می‌کند.
    /// </summary>
    [Fact]
    public async Task Same_code_with_changed_goods_refuses_preview_and_commit_before_any_reserve()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var vertical = new StorefrontFullOwnerVerticalTests.Vertical(db, marketplace: true);
        vertical.Core.Promotions.DiscountsByCoupon[CanonicalCoupon] = new Dictionary<Guid, decimal>
        {
            [OfferA] = 10m,
        };

        await vertical.Shipping.SaveSelectionAsync(
            vertical.SelectionRequest("0") with { CouponCode = Coupon },
            guestSecret: null,
            CancellationToken.None);

        // همان کد، همان کرایه/ردیف؛ فقط نتیجهٔ Promotion عوض می‌شود.
        vertical.Core.Promotions.DiscountsByCoupon[CanonicalCoupon][OfferA] = 20m;

        var preview = await Assert.ThrowsAsync<ContractOperationException>(() =>
            vertical.Shipping.ProjectPaymentDraftAsync(CartId, null, Coupon, CancellationToken.None));
        Assert.Equal(StorefrontCapturedSelectionReasons.Conflict, preview.Code);

        var commitRefused = await Assert.ThrowsAsync<ContractOperationException>(() =>
            vertical.Shipping.CommitAsync(
                CartId,
                null,
                CartVersion,
                "tb236-changed-goods",
                Coupon,
                expectedSelectionRevision: "1",
                CancellationToken.None));
        Assert.Equal(StorefrontCapturedSelectionReasons.Conflict, commitRefused.Code);

        Assert.Equal(0, vertical.Core.Reservation.Calls);
        Assert.Equal(0, vertical.Core.Conversion.Calls);
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(0, await db.Set<OrderShippingGroup>().CountAsync());
        Assert.Equal(1, (await db.ShippingDrafts.SingleAsync(x => x.CartId == CartId)).SelectionRevision);

        // بازگشت نتیجهٔ Promotion به همان مقدار تأییدشده: پیش‌نمایش دوباره معتبر است.
        vertical.Core.Promotions.DiscountsByCoupon[CanonicalCoupon][OfferA] = 10m;
        var healed = await vertical.Shipping.ProjectPaymentDraftAsync(CartId, null, Coupon, CancellationToken.None);
        Assert.Equal(3, healed.ShippingGroups!.Count);
    }

    /// <summary>
    /// <b>۴)</b> بازگشت idempotent پس از تغییر قصد کوپن: همان سفارش اصلی برمی‌گردد، بدون نقل‌قول/رزرو
    /// اضافه و بدون بازنویسی خط/گروه پایدار.
    /// </summary>
    [Fact]
    public async Task Idempotent_replay_after_coupon_change_returns_the_original_order()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var vertical = new StorefrontFullOwnerVerticalTests.Vertical(db, marketplace: true);
        vertical.Core.Promotions.DiscountsByCoupon[CanonicalCoupon] = new Dictionary<Guid, decimal>
        {
            [OfferA] = 10m,
        };

        await vertical.Shipping.SaveSelectionAsync(
            vertical.SelectionRequest("0") with { CouponCode = Coupon },
            guestSecret: null,
            CancellationToken.None);

        var first = await vertical.Shipping.CommitAsync(
            CartId,
            null,
            CartVersion,
            "tb236-replay",
            Coupon,
            expectedSelectionRevision: "1",
            CancellationToken.None);
        Assert.NotNull(first.CheckoutId);

        db.ChangeTracker.Clear();
        var orderLinesBefore = await db.Set<Tooba.Order.Domain.Aggregates.OrderLine>()
            .AsNoTracking()
            .Select(line => line.LineId)
            .OrderBy(id => id)
            .ToArrayAsync();
        Assert.NotEmpty(orderLinesBefore);

        var quotesBeforeReplay = vertical.Core.Quotes.Calls;
        var pointersBeforeReplay = vertical.Core.Pointers.Calls;
        var reservationsBeforeReplay = vertical.Core.Reservation.Calls;

        // قصد و ظرفیت کاملاً عوض‌شده: بازگشت idempotent نباید به زمینهٔ امروز گره بخورد.
        vertical.Core.Promotions.DiscountsByCoupon[CanonicalCoupon][OfferA] = 40m;

        var replay = await vertical.Shipping.CommitAsync(
            CartId,
            null,
            CartVersion,
            "tb236-replay",
            "DIFFERENT",
            expectedSelectionRevision: "1",
            CancellationToken.None);

        Assert.Equal(first.CheckoutId, replay.CheckoutId);
        Assert.Equal(quotesBeforeReplay, vertical.Core.Quotes.Calls);
        Assert.Equal(pointersBeforeReplay, vertical.Core.Pointers.Calls);
        Assert.Equal(reservationsBeforeReplay, vertical.Core.Reservation.Calls);

        db.ChangeTracker.Clear();
        Assert.Equal(
            orderLinesBefore,
            await db.Set<Tooba.Order.Domain.Aggregates.OrderLine>()
                .AsNoTracking()
                .Select(line => line.LineId)
                .OrderBy(id => id)
                .ToArrayAsync());
        Assert.Equal(1, await db.Checkouts.CountAsync(x => x.CartId == CartId));
    }

    /// <summary>
    /// <b>۵)</b> نبودِ عمدی تاریخ/پنجره در تصویر پذیرفته‌شده (تهی بودن فیلد یعنی «گروه‌ها توافق ندارند»)
    /// هرگز تاریخ کهنهٔ فرمان را بازنمی‌گرداند؛ انتخاب بر <b>حضور شیء</b> است نه بر مقدار nullable.
    /// </summary>
    [Fact]
    public void Explicit_absence_in_the_accepted_root_never_resurrects_the_command_dates()
    {
        var cart = new CartSnapshot(
            CartId,
            CartStatus.Active,
            CartAccessKind.Authenticated,
            CapturedFinalSubmitIntegrationTests.Buyer,
            "IR",
            "IRR",
            SalesChannel.Direct,
            new DateTimeOffset(2026, 10, 25, 11, 0, 0, TimeSpan.Zero),
            CartConversionIntent.OnlinePurchase,
            CartVersion,
            [
                new CartLineSnapshot(
                    Guid.Parse("01a17d12-0000-7000-8000-00000000cc01"),
                    CapturedFinalSubmitIntegrationTests.OfferA,
                    Guid.Parse("01a17d12-0000-7000-8000-00000000cc02"),
                    CapturedFinalSubmitIntegrationTests.SellerA,
                    1m,
                    ReservationId: null,
                    QuotedAmount: 100m,
                    QuotedCurrency: "IRR",
                    QuotedTaxExclusive: false,
                    PriceId: null,
                    QuotedAt: new DateTimeOffset(2026, 10, 25, 11, 0, 0, TimeSpan.Zero)),
            ]);

        var command = new SubmitCheckoutCommand(
            CartId,
            new CartAccess(CapturedFinalSubmitIntegrationTests.Buyer, null),
            CartVersion,
            OrderMode.OnlinePurchase,
            BuyerPartyId: null,
            PlacedByUserId: CapturedFinalSubmitIntegrationTests.Buyer,
            IdempotencyKey: "tb236-preview",
            TaxJurisdiction: StorefrontCheckoutService.StorefrontTaxJurisdiction,
            MinimumDeliveryDate: new DateOnly(2026, 10, 29),
            RequestedDeliveryDate: new DateOnly(2026, 10, 30),
            RequestedDeliveryTimeWindow: "09:00-17:00",
            ShippingMethodCode: "post:standard",
            ShippingMethodLabel: "پست");

        var withAbsence = CheckoutDirectory.ComposePreviewSnapshot(
            cart,
            command,
            Guid.Parse("01a17d10-0000-7000-8000-00000000abab"),
            new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero),
            [OrderTestData.SellerOrder(100m, 1m)],
            new CheckoutPreviewShippingOverride(125m, null, null, string.Empty, string.Empty, string.Empty));

        Assert.Null(withAbsence.RequestedDeliveryDate);
        Assert.Null(withAbsence.MinimumDeliveryDate);
        Assert.Equal(string.Empty, withAbsence.RequestedDeliveryTimeWindow);
        Assert.Equal(125m, withAbsence.ShippingAmount);

        // کنترل: نبودِ override یعنی همان مقدار فرمان (مسیر قدیمی دست‌نخورده).
        var legacy = CheckoutDirectory.ComposePreviewSnapshot(
            cart,
            command,
            Guid.Parse("01a17d10-0000-7000-8000-00000000acac"),
            new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero),
            [OrderTestData.SellerOrder(100m, 1m)]);
        Assert.Equal(new DateOnly(2026, 10, 30), legacy.RequestedDeliveryDate);
        Assert.Equal("09:00-17:00", legacy.RequestedDeliveryTimeWindow);
    }

    /// <summary>
    /// <b>۶)</b> مرز <b>واقعی</b> خواندن بیرونی/پذیرش تازه: مصرف‌کننده ریشه را <b>میان</b> خواندن پیش‌نویس
    /// توسط facade و خواندن تازهٔ هسته جلو می‌برد (از پورت تست، بدون hook تولیدی). پاسخ باید ریشهٔ
    /// <b>تازه</b> را توصیف کند؛ مبلغ هر گروه جلو رفته و تاریخ‌های گروه‌ها همچنان متفاوت است، پس
    /// تاریخ تجمیعی غایب می‌ماند و هیچ نوشتن/رزروی در این خواندن رخ نمی‌دهد.
    /// </summary>
    [Fact]
    public async Task Payment_preview_reads_the_root_advanced_between_the_facade_and_the_core()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        StorefrontFullOwnerVerticalTests.Vertical? vertical = null;
        var advanced = 0;
        using var ownerVertical = new StorefrontFullOwnerVerticalTests.Vertical(
            db,
            marketplace: true,
            wrapDrafts: inner => new AdvancingDraftStore(
                inner,
                () => Interlocked.Exchange(ref advanced, 1) == 0
                    ? vertical!.MoveSelectionToSecondRevisionAsync()
                    : Task.CompletedTask));
        vertical = ownerVertical;

        await ownerVertical.Shipping.SaveSelectionAsync(
            ownerVertical.SelectionRequest("0"),
            guestSecret: null,
            CancellationToken.None);

        var reservationsBefore = ownerVertical.Core.Reservation.Calls;
        var conversionsBefore = ownerVertical.Core.Conversion.Calls;

        var page = await ownerVertical.Shipping.ProjectPaymentDraftAsync(
            CartId,
            null,
            couponCode: null,
            CancellationToken.None);

        // همان خواندن بیرونی ریشهٔ ۱ را دیده بود؛ پاسخ از ریشهٔ ۲ می‌آید (نه از فرمان/کپی کهنه).
        Assert.Equal(1, advanced);
        Assert.Equal("2", page.SelectionRevision);
        Assert.Equal(3, page.ShippingGroups!.Count);
        var dates = page.ShippingGroups
            .Select(group => group.SelectedDeliveryDate)
            .ToArray();
        Assert.Equal(3, dates.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("2026-10-27", dates);
        Assert.Contains("2026-10-28", dates);
        Assert.Contains("2026-10-29", dates);
        // مبلغ هر گروه همان ریشهٔ تازه است (+۴۰ نسبت به ریشهٔ کهنه).
        Assert.Equal(
            StorefrontFullOwnerVerticalTests.Owners.Sum(owner => owner.Payable + 40m),
            page.ShippingAmount);

        // تاریخ تجمیعی گروه‌های ناهم‌تاریخ غایب است (نه تاریخ کهنهٔ فرمان).
        db.ChangeTracker.Clear();
        var draft = await db.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .SingleAsync(x => x.CartId == CartId);
        var derived = CheckoutPreviewShippingOverride.Derive(
            StorefrontShippingService.BuildOrderGroupPlans(draft));
        Assert.NotNull(derived);
        Assert.Null(derived!.RequestedDeliveryDate);
        Assert.Equal(string.Empty, derived.RequestedDeliveryTimeWindow);
        Assert.Equal(page.ShippingAmount, derived.ShippingAmount);

        Assert.Equal(reservationsBefore, ownerVertical.Core.Reservation.Calls);
        Assert.Equal(conversionsBefore, ownerVertical.Core.Conversion.Calls);
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(2, draft.SelectionRevision);
    }

    /// <summary>
    /// پورت تست که یک‌بار <b>پس از</b> خواندن پیش‌نویس توسط facade و <b>پیش از</b> خواندن تازهٔ هسته
    /// عمل می‌کند؛ بقیهٔ اعضا فقط عبور می‌دهند.
    /// </summary>
    private sealed class AdvancingDraftStore(
        IStorefrontShippingDraftStore inner,
        Func<Task> afterRead) : IStorefrontShippingDraftStore
    {
        public async Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken)
        {
            var draft = await inner.GetByCartIdAsync(cartId, cancellationToken);
            await afterRead();
            return draft;
        }

        public Task SaveAsync(CartShippingDraft draft, bool isNew, CancellationToken cancellationToken) =>
            inner.SaveAsync(draft, isNew, cancellationToken);

        public Task<CartShippingDraft?> GetForCapturedSelectionAsync(Guid cartId, CancellationToken cancellationToken) =>
            inner.GetForCapturedSelectionAsync(cartId, cancellationToken);

        public Task<CartShippingDraft> ReplaceCapturedSelectionAsync(
            CartShippingDraft draft,
            IReadOnlyList<CartShippingDraftGroup> groups,
            bool isNew,
            long expectedSelectionRevision,
            CancellationToken cancellationToken) =>
            inner.ReplaceCapturedSelectionAsync(draft, groups, isNew, expectedSelectionRevision, cancellationToken);
    }

    /// <summary>
    /// <b>۷)</b> شکل <b>افزایشی</b> ذخیره‌سازی: فقط دو ستون nullable اضافه شده، هیچ JSON/تاریخچه و
    /// هیچ ستون قبلی تغییر نکرده، و CHECK هر دو جدول مقدار خام/ناشمارش‌پذیر را در سطح SQL رد می‌کند.
    /// </summary>
    [Fact]
    public async Task Storage_columns_are_additive_and_the_shape_checks_refuse_raw_values()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var vertical = new StorefrontFullOwnerVerticalTests.Vertical(db, marketplace: true);

        // ستون‌های افزوده‌شده دقیقاً همان دو ستون nullable هستند و شکل قبلی تغییر نکرده است.
        var columns = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT table_name || '.' || column_name || ':' || data_type || ':' || is_nullable
                FROM information_schema.columns
                WHERE table_schema = 'order'
                  AND ((table_name = 'cart_shipping_drafts' AND column_name = 'selection_coupon_code')
                    OR (table_name = 'cart_shipping_groups' AND column_name = 'confirmed_goods_amount'))
                ORDER BY 1
                """)
            .ToArrayAsync();
        Assert.Equal(
            ["cart_shipping_drafts.selection_coupon_code:text:YES", "cart_shipping_groups.confirmed_goods_amount:numeric:YES"],
            columns);

        // هیچ ستون JSON/JSONB در این دو جدول نیست: شاهد «ثابت و کوچک» بودن، نه یک سند نویسنده‌محور.
        var jsonColumns = await db.Database
            .SqlQueryRaw<long>(
                """
                SELECT count(*)::bigint AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'order'
                  AND table_name IN ('cart_shipping_drafts', 'cart_shipping_groups')
                  AND data_type LIKE 'json%'
                """)
            .SingleAsync();
        Assert.Equal(0, jsonColumns);

        var constraints = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT conname
                FROM pg_constraint
                WHERE conname IN ('ck_cart_shipping_drafts_selection_coupon_shape',
                                  'ck_cart_shipping_groups_confirmed_goods_shape')
                ORDER BY 1
                """)
            .ToArrayAsync();
        Assert.Equal(
            ["ck_cart_shipping_drafts_selection_coupon_shape", "ck_cart_shipping_groups_confirmed_goods_shape"],
            constraints);

        // ردیف واقعی لازم است تا CHECK ارزیابی شود.
        await vertical.Shipping.SaveSelectionAsync(
            vertical.SelectionRequest("0"),
            guestSecret: null,
            CancellationToken.None);

        var untrimmed = await Assert.ThrowsAnyAsync<Exception>(() =>
            db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"order\".cart_shipping_drafts SET selection_coupon_code = ' SAVE10 ' WHERE cart_id = {CartId}"));
        Assert.Equal("23514", FindSqlState(untrimmed));
        Assert.Equal("ck_cart_shipping_drafts_selection_coupon_shape", FindConstraintName(untrimmed));

        var negativeGoods = await Assert.ThrowsAnyAsync<Exception>(() =>
            db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"order\".cart_shipping_groups SET confirmed_goods_amount = -1 WHERE cart_id = {CartId}"));
        Assert.Equal("23514", FindSqlState(negativeGoods));
        Assert.Equal("ck_cart_shipping_groups_confirmed_goods_shape", FindConstraintName(negativeGoods));

        // مقدار فاصله‌دار/منفی هیچ‌جا ننشست؛ همان انتخاب معتبر دست‌نخورده است.
        db.ChangeTracker.Clear();
        var unchanged = await db.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Null(unchanged.SelectionCouponCode);
        Assert.All(unchanged.ShippingGroups, group => Assert.True(group.ConfirmedGoodsAmount >= 0));
    }

    /// <summary>
    /// <b>۸)</b> کنترل «نبود کوپن»: قصد فاصله/تهی هرگز به Promotion نمی‌رسد و مبلغ کالا همان قیمت بدون
    /// تخفیف است؛ هیچ صفر/کد ساختگی جای نبودِ قصد نمی‌نشیند.
    /// </summary>
    [Fact]
    public async Task Blank_intent_is_absence_and_quotes_without_any_coupon()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var vertical = new StorefrontFullOwnerVerticalTests.Vertical(db, marketplace: true);
        vertical.Core.Promotions.DiscountsByCoupon[CanonicalCoupon] = new Dictionary<Guid, decimal>
        {
            [OfferA] = 10m,
        };
        vertical.Core.Promotions.CouponsSeen.Clear();

        var read = await vertical.Planner.BuildAsync(
            vertical.Carts.Page,
            new CartAccess(CapturedFinalSubmitIntegrationTests.Buyer, null),
            await DestinationAsync(vertical),
            "IRR",
            Guid.Empty,
            CancellationToken.None,
            "   ");

        Assert.True(read.Enabled);
        Assert.Null(read.NoticeReason);
        Assert.NotEmpty(vertical.Core.Promotions.CouponsSeen);
        Assert.All(vertical.Core.Promotions.CouponsSeen, seen => Assert.Null(seen));
        // بدون قصد کوپن همان تخفیفِ پایهٔ غیرکوپنی حاکم است: 100−10+10٪=99 و 200−20+10٪=198 و 300+10٪=330.
        // تفاوت گروه دوم با خواندن کوپنی (۲۲۰) نشان می‌دهد «نبود قصد» یک مسیر واقعاً جداست، نه کد خالی.
        Assert.Equal(
            ["99.00", "198.00", "330.00"],
            read.Groups.Select(group => group.FinalGoodsAmountAfterDiscountAndTax).ToArray());
    }

    private static string? FindSqlState(Exception? error)
    {
        for (var current = error; current is not null; current = current.InnerException)
        {
            if (current.GetType().GetProperty("SqlState")?.GetValue(current) is string state)
            {
                return state;
            }
        }

        return null;
    }

    private static string? FindConstraintName(Exception? error)
    {
        for (var current = error; current is not null; current = current.InnerException)
        {
            if (current.GetType().GetProperty("ConstraintName")?.GetValue(current) is string name
                && name.Length > 0)
            {
                return name;
            }
        }

        return null;
    }

    private static async Task<DestinationIdentifiers> DestinationAsync(
        StorefrontFullOwnerVerticalTests.Vertical vertical)
    {
        var resolved = await vertical.Core.Geography.ResolveLegacyAsync(
            "IR",
            "تهران",
            "تهران",
            serviceAreaName: null,
            CancellationToken.None);
        return resolved.Identifiers
            ?? throw new InvalidOperationException("the fixture destination must resolve");
    }
}
