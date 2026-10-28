using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tooba.AddressBook.Contracts.Dtos;
using Tooba.AddressBook.Contracts.Ports;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Catalog.Contracts.Checkout;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Storefront;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Promotion.Contracts.Checkout;
using Tooba.Tax.Contracts.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Checkout.Persistence;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-233-R3: the ONE coherent captured submit through the ACTUAL
/// <see cref="CheckoutDirectory.SubmitAsync"/> path on the task-owned disposable PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// <b>Controlled Contracts vs real Order.</b> Reservation/conversion, Pricing/Promotion/Tax, the 216 quote,
/// the 219 pointer, the 228 assignment, the Fulfillment catalog, Geography and AddressBook are clearly
/// labelled controlled Contracts; <see cref="OrderDbContext"/>, migrations, the real quote/promotion/tax
/// composition, the captured acceptance and Order persistence are real.
/// </para>
/// <para>
/// <b>One binding.</b> Both sellers use the SAME Store service/option, one shared immutable snapshot, one
/// draft/revision and the SAME destination area group; independence is each group's own rate row, fee and
/// chosen date/hour on ONE shared receiving week.
/// </para>
/// </remarks>
[Collection(OrderEvidenceRelationalCollection.Name)]
public sealed class CapturedFinalSubmitIntegrationTests
{
    internal static readonly Guid CartId = Guid.Parse("01a17d0d-e033-7000-af43-49a1daa28821");
    internal static readonly Guid Buyer = Guid.Parse("01a17cf7-e38d-7000-82a5-00c57929bc54");
    internal static readonly Guid SellerA = Guid.Parse("0199aaaa-0000-7000-8000-0000000000aa");
    internal static readonly Guid SellerB = Guid.Parse("0199aaaa-0000-7000-8000-0000000000bb");
    internal static readonly Guid OfferA = Guid.Parse("01a17cfc-0000-7000-8000-00000000000a");
    internal static readonly Guid OfferB = Guid.Parse("01a17cfc-0000-7000-8000-00000000000b");
    internal static readonly Guid LineA = Guid.Parse("01a17cfd-0000-7000-8000-00000000000a");
    internal static readonly Guid LineB = Guid.Parse("01a17cfd-0000-7000-8000-00000000000b");
    internal static readonly Guid DraftGroupA = Guid.Parse("01a17d0f-0000-7000-8000-0000000000a1");
    internal static readonly Guid DraftGroupB = Guid.Parse("01a17d0f-0000-7000-8000-0000000000b2");
    // همان ناحیه برای همان مقصد: تفاوت مستقل گروه، ردیف نرخ/کارمزد/تاریخ خودش است.
    internal static readonly Guid AreaGroup = Guid.Parse("01a17d10-0000-7000-8000-0000000000a1");
    internal static readonly Guid RowA = Guid.Parse("01a17d11-0000-7000-8000-0000000000a1");
    internal static readonly Guid RowB = Guid.Parse("01a17d11-0000-7000-8000-0000000000b2");
    private static readonly Guid SnapshotId = Guid.Parse("01a17d0e-0419-7000-b500-0000000000a1");
    internal static readonly Guid ShippingTypeId = Guid.Parse("01a1785e-01f3-7000-b500-0000000000c1");
    private static readonly Guid CountryId = Guid.Parse("e3451206-e503-51a8-9100-0000000000d1");
    private static readonly Guid OtherCountryId = Guid.Parse("e3451206-e503-51a8-9100-0000000000e1");
    private static readonly Guid ProvinceId = Guid.Parse("e3451206-e503-51a8-9100-0000000000d2");
    private static readonly Guid CityId = Guid.Parse("e3451206-e503-51a8-9100-0000000000d3");
    internal static readonly Guid SavedAddressId = Guid.Parse("01a17d13-0000-7000-8000-0000000000f1");
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 9, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly DateA = new(2026, 10, 27); // سه‌شنبه
    private static readonly DateOnly DateB = new(2026, 10, 28); // چهارشنبه
    internal static readonly long OpenTicksA = new TimeOnly(9, 0, 0).Ticks + 1_234_567L;
    private static readonly long CloseTicksA = new TimeOnly(17, 0, 0).Ticks + 7_654_321L;
    internal static readonly long OpenTicksB = new TimeOnly(10, 0, 0).Ticks + 5_000_000L;
    private static readonly long CloseTicksB = new TimeOnly(18, 0, 0).Ticks + 2_500_000L;

    /// <summary>
    /// دو فروشنده روی <b>همان</b> Binding و همان snapshot/پیش‌نویس/بازبینی و همان ناحیهٔ مقصد:
    /// دو ردیف نرخ مستقل (بر اساس مبلغ کالای هر گروه)، دو تاریخ قانونی متفاوت از <b>یک</b> هفتهٔ
    /// ذخیره‌شده با ساعت‌های کسری ۱۰۰نانوثانیه‌ای، و مبلغ کالای واقعی ۱۰۰−۱۰+۹=۹۹ و ۲۰۰−۲۰+۱۸=۱۹۸.
    /// </summary>
    [Fact]
    public async Task Two_sellers_on_one_binding_submit_real_persisted_groups_with_their_own_row_date_and_ticks()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        using var harness = new CapturedSubmitHarness(db);

        await SeedTwoGroupDraftAsync(db);
        var snapshot = await harness.Directory.SubmitAsync(
            harness.Command(expectedSelectionRevision: "1"),
            CancellationToken.None);

        Assert.Equal(CartId, snapshot.CartId);
        Assert.Equal(2, snapshot.SellerOrders.Count);

        db.ChangeTracker.Clear();
        var checkout = await db.Checkouts
            .Include(x => x.SellerOrders).ThenInclude(x => x.Lines)
            .Include(x => x.ShippingGroups).ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);

        var groups = checkout.ShippingGroups.ToArray();
        Assert.Equal(2, groups.Length);
        var groupA = Assert.Single(groups, x => x.ShippingGroupId == DraftGroupA);
        var groupB = Assert.Single(groups, x => x.ShippingGroupId == DraftGroupB);
        // کارمزد پرداختنی هر گروه همان نقل‌قول خودش است.
        Assert.Equal(125m, groupA.ShippingChargeSnapshot);
        Assert.Equal(75m, groupB.ShippingChargeSnapshot);
        Assert.Single(groupA.Lines);
        Assert.Single(groupB.Lines);
        var orderLineIds = checkout.SellerOrders.SelectMany(x => x.Lines).Select(x => x.LineId).ToArray();
        Assert.Contains(groupA.Lines[0].OrderLineId, orderLineIds);
        Assert.Contains(groupB.Lines[0].OrderLineId, orderLineIds);

        var evidenceA = Assert.IsType<ShippingGroupAcceptedEvidence>(groupA.AcceptedEvidence);
        var evidenceB = Assert.IsType<ShippingGroupAcceptedEvidence>(groupB.AcceptedEvidence);
        // یک snapshot/پیش‌نویس/بازبینی مشترک، مالک Store همان tenant فعال.
        Assert.Equal(SnapshotId, evidenceA.SnapshotId);
        Assert.Equal(SnapshotId, evidenceB.SnapshotId);
        Assert.Equal(CartId, evidenceA.DraftId);
        Assert.Equal(2L, evidenceA.SourceRevision);
        Assert.Equal(1L, evidenceA.SelectionVersion);
        Assert.Equal(ShippingGroupOwnerKind.Store, evidenceA.Owner.Kind);
        Assert.Equal("store-alpha", evidenceA.Owner.TenantKey);
        Assert.Equal("store-alpha", evidenceB.Owner.TenantKey);
        Assert.Null(evidenceA.Owner.SellerPartyId);
        // همان ناحیهٔ مقصد، ولی ردیف نرخ هر گروه مستقل است.
        Assert.Equal(AreaGroup, evidenceA.AreaGroupId);
        Assert.Equal(AreaGroup, evidenceB.AreaGroupId);
        Assert.Equal(RowA, evidenceA.RateRowId);
        Assert.Equal(RowB, evidenceB.RateRowId);
        // مبلغ/تاریخ/ساعت مستقل هر گروه، با تیک‌های کسری دقیق.
        Assert.Equal(120m, evidenceA.FreightAmount);
        Assert.Equal(5m, evidenceA.PackagingAmount);
        Assert.Equal(125m, evidenceA.PayableAtCheckoutAmount);
        Assert.Equal(0m, evidenceA.CarrierCollectAmount);
        Assert.Equal(DateA, evidenceA.ReceivingDate);
        Assert.Equal(OpenTicksA, evidenceA.ReceivingOpenTicks);
        Assert.Equal(CloseTicksA, evidenceA.ReceivingCloseTicks);
        Assert.Equal(70m, evidenceB.FreightAmount);
        Assert.Equal(75m, evidenceB.PayableAtCheckoutAmount);
        Assert.Equal(DateB, evidenceB.ReceivingDate);
        Assert.Equal(OpenTicksB, evidenceB.ReceivingOpenTicks);
        Assert.Equal(CloseTicksB, evidenceB.ReceivingCloseTicks);
        Assert.Equal("UTC", evidenceA.ReceivingTimeZone);
        Assert.Equal("UTC", evidenceB.ReceivingTimeZone);

        // دقیقاً یک خواندن ۲۱۹ برای همان Binding مشترک و دو نقل‌قول ۲۱۶ با مبلغ کالای واقعی.
        Assert.Equal(1, harness.PointerCalls);
        Assert.Equal(2, harness.Quotes.Calls);
        Assert.Equal(
            new[] { 99m, 198m },
            harness.Quotes.Requests
                .Select(x => x.FinalGroupGoodsAmountAfterDiscountAndTax!.Value)
                .OrderBy(x => x)
                .ToArray());
        Assert.Equal(1, harness.AssignmentCalls);
        Assert.Equal(1, harness.CatalogCalls);
        // کشور متعارف غیرتهی به Geography داده شده است، نه null.
        Assert.Equal("IR", harness.Geography.LastCountryCode);

        Assert.Equal(1, harness.Reservation.Calls);
        Assert.Equal(1, harness.Conversion.Calls);
        Assert.Equal(new[] { LineA, LineB }, harness.Reservation.LastLines.OrderBy(x => x).ToArray());
    }

    /// <summary>یک OfferId مشروع در دو خط سبد پذیرفته می‌شود (بدون هیچ استنتاج مقدار/مقدار دوم).</summary>
    [Fact]
    public async Task A_shared_offer_id_across_two_lines_is_lawful()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        using var harness = new CapturedSubmitHarness(db, sharedOffer: true);

        await SeedSingleGroupDraftAsync(db, SellerA, [LineA, LineB], confirmedGoods: 198m);
        var snapshot = await harness.Directory.SubmitAsync(
            harness.Command(expectedSelectionRevision: "1"),
            CancellationToken.None);

        var order = Assert.Single(snapshot.SellerOrders);
        Assert.Equal(2, order.Lines.Count);
    }

    /// <summary>آدرس ذخیره‌شدهٔ خودی و بدون تغییر در ثبت نهایی پذیرفته و همان رکورد خوانده می‌شود.</summary>
    [Fact]
    public async Task An_owned_unchanged_saved_address_is_accepted_and_read_once()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        using var harness = new CapturedSubmitHarness(db);

        await SeedTwoGroupDraftAsync(db, savedAddressId: SavedAddressId);
        var snapshot = await harness.Directory.SubmitAsync(
            harness.Command(expectedSelectionRevision: "1"),
            CancellationToken.None);

        Assert.Equal(2, snapshot.SellerOrders.Count);
        Assert.Equal(1, harness.Addresses.Calls);
        Assert.Equal(Buyer, harness.Addresses.LastActor);
        Assert.Equal(SavedAddressId, harness.Addresses.LastAddressId);
    }

    /// <summary>
    /// ناسازگاری دروازه/مالک/اشاره‌گر/مبلغ/تاریخ/نشانی/کشور/فهرست فعال/نسخهٔ قالب/کوپن/نسخهٔ ریشه
    /// با <b>کد دقیق مالک</b> و پیش از هر رزرو/سفارش/تبدیل رد می‌شود.
    /// </summary>
    [Theory]
    [InlineData("gate", "storefront.captured_preview.not_ready")]
    [InlineData("owner", "storefront.captured_selection.conflict")]
    [InlineData("pointer", "storefront.captured_selection.conflict")]
    [InlineData("amount", "storefront.captured_selection.conflict")]
    [InlineData("date", "storefront.captured_selection.conflict")]
    [InlineData("root", "storefront.captured_selection.conflict")]
    [InlineData("format", "storefront.captured_selection.conflict")]
    // TB-236: کوپن دیگر «پشتیبانی‌نشده» نیست؛ قصدی که با قصد تأییدشدهٔ ریشه یکی نیست وضعیت کهنه است.
    [InlineData("coupon", "storefront.captured_selection.conflict")]
    [InlineData("address-missing", "storefront.captured_selection.destination_unverified")]
    [InlineData("address-foreign", "storefront.captured_selection.destination_unverified")]
    [InlineData("address-edited", "storefront.captured_selection.destination_unverified")]
    [InlineData("country", "storefront.captured_selection.destination_unverified")]
    [InlineData("command-address", "storefront.captured_selection.destination_unverified")]
    [InlineData("binding-inactive", "storefront.captured_selection.unavailable")]
    [InlineData("binding-option-inactive", "storefront.captured_selection.unavailable")]
    [InlineData("authority-missing", "storefront.captured_selection.unavailable")]
    public async Task Incoherent_captured_submit_is_refused_with_the_exact_owning_code(string scenario, string code)
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        using var harness = new CapturedSubmitHarness(
            db,
            checkoutEnabled: scenario != "gate",
            freightA: scenario == "amount" ? 130m : null,
            withAddressBook: scenario != "authority-missing");

        await SeedTwoGroupDraftAsync(
            db,
            mutate: scenario,
            savedAddressId: SavedAddressId);

        switch (scenario)
        {
            case "pointer":
                harness.Pointers.Revision = 7;
                break;
            case "format":
                harness.Quotes.FormatVersion = 2;
                break;
            case "address-missing":
                harness.Addresses.Drop = true;
                break;
            case "address-foreign":
                harness.Addresses.ActorMismatch = true;
                break;
            case "address-edited":
                harness.Addresses.ChangePostalAddress = true;
                break;
            case "country":
                harness.Geography.HidePricedCountry = true;
                break;
            case "binding-inactive":
                harness.ShippingCatalog.Active = false;
                break;
            case "binding-option-inactive":
                harness.ShippingCatalog.OptionActive = false;
                break;
        }

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.SubmitAsync(
                harness.Command(
                    expectedSelectionRevision: "1",
                    coupon: scenario == "coupon" ? "OFF10" : null,
                    postalCode: scenario == "command-address" ? "9999999999" : null),
                CancellationToken.None));

        Assert.Equal(code, error.Code);
        Assert.Equal(0, harness.Reservation.Calls);
        Assert.Equal(0, harness.Conversion.Calls);
        db.ChangeTracker.Clear();
        Assert.False(await db.Checkouts.AnyAsync(x => x.CartId == CartId));
        Assert.Equal(0, await db.Set<OrderShippingGroup>().CountAsync());
    }

    /// <summary>
    /// تکرار همان ارسال پس از <b>تغییر واقعی</b> اشاره‌گر ۲۱۹: بدون خواندن دوبارهٔ اشاره‌گر/نقل‌قول،
    /// بدون رزرو/سفارش دوم و با همان شاهد تاریخی پایدار.
    /// </summary>
    [Fact]
    public async Task A_repeat_submit_after_the_pointer_moved_returns_the_original_checkout_and_evidence()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        using var harness = new CapturedSubmitHarness(db);

        await SeedTwoGroupDraftAsync(db);
        var command = harness.Command(expectedSelectionRevision: "1");
        var first = await harness.Directory.SubmitAsync(command, CancellationToken.None);
        var quotesAfterFirst = harness.Quotes.Calls;
        var pointersAfterFirst = harness.PointerCalls;

        // انتشار واقعاً جلو می‌رود: اشاره‌گر ۲۱۹ دیگر همان بازبینی قبلی نیست.
        harness.Pointers.Revision = 9;
        var second = await harness.Directory.SubmitAsync(command, CancellationToken.None);

        Assert.Equal(first.CheckoutId, second.CheckoutId);
        Assert.Equal(
            first.ShippingGroups.Select(x => x.ShippingGroupId).Order().ToArray(),
            second.ShippingGroups.Select(x => x.ShippingGroupId).Order().ToArray());
        Assert.Equal(quotesAfterFirst, harness.Quotes.Calls);
        Assert.Equal(pointersAfterFirst, harness.PointerCalls);
        Assert.Equal(1, harness.Reservation.Calls);
        // تبدیل سبد در مسیر تکرار idempotent بازهم reconcile می‌شود؛ ادعای این تست فقط
        // «بدون خواندن دوبارهٔ اشاره‌گر/نقل‌قول، بدون رزرو/سفارش دوم» است.
        Assert.True(harness.Conversion.Calls >= 1);

        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        var groups = await db.Set<OrderShippingGroup>()
            .Where(x => x.CheckoutId == first.CheckoutId)
            .ToListAsync();
        Assert.Equal(2, groups.Count);
        // شاهد پایدار همان بازبینی تاریخی است؛ اشاره‌گر جدید آن را بازنویسی نمی‌کند.
        Assert.All(groups, group => Assert.Equal(2L, group.AcceptedEvidence!.SourceRevision));
        Assert.Equal(DateA, groups.Single(x => x.ShippingGroupId == DraftGroupA).AcceptedEvidence!.ReceivingDate);
        Assert.Equal(DateB, groups.Single(x => x.ShippingGroupId == DraftGroupB).AcceptedEvidence!.ReceivingDate);
    }

    /// <summary>شکست سد پس از نوشتن سفارش، هیچ گروه/شاهد فشرده‌ای را پایدار نمی‌کند.</summary>
    [Fact]
    public async Task A_barrier_failure_after_the_order_write_leaves_no_captured_evidence()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        var failure = new InvalidOperationException("tb233r3-barrier");
        using var harness = new CapturedSubmitHarness(db, barrier: new FailingBarrier(failure));

        await SeedTwoGroupDraftAsync(db);
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Directory.SubmitAsync(harness.Command(expectedSelectionRevision: "1"), CancellationToken.None));

        Assert.Same(failure, thrown);
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(0, await db.Set<OrderShippingGroup>().CountAsync());
    }

    /// <summary>
    /// TB-233-R3: آماده‌سازی، شناسهٔ واقعی آدرس ذخیره‌شده را فقط در منشأ <b>فقط-سرور</b> نگه می‌دارد و
    /// تصویر snapshot قدیمی (<c>Shipping.SavedAddressId</c>) همان معنای تهی قبلی را حفظ می‌کند.
    /// </summary>
    [Fact]
    public async Task Preparation_keeps_the_saved_address_id_as_server_provenance_only()
    {
        var addresses = new RecordingAddressBook();
        var service = new StorefrontCheckoutService(null!, null!, addresses, new FixedStorefrontActor(), shippingGroupHistory: null!);

        var placement = await service.PrepareShippingAsync(
            new StorefrontCheckoutShippingInput(
                string.Empty,
                "0000000000",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                SavedAddressId,
                string.Empty,
                string.Empty),
            CancellationToken.None);

        Assert.Equal(SavedAddressId, placement.SourceSavedAddressId);
        Assert.Null(placement.Shipping.SavedAddressId);
        Assert.Equal(Buyer, placement.PlacedByUserId);
        // تصویر قیمت‌گذاری همان رکورد کتاب آدرس است، نه فیلدهای بدنه.
        Assert.Equal("تهران", placement.Shipping.ProvinceName);
        Assert.Equal("1234567890", placement.Shipping.PostalCode);
        Assert.Equal("09120000000", placement.Shipping.ContactMobile);
    }

    internal sealed class FixedStorefrontActor : IOrderStorefrontActor
    {
        public Guid GuestActorId => Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId;

        public bool IsAuthenticated => true;

        public Guid? AuthenticatedUserId => Buyer;

        public Guid ResolvePlacementActor(bool usingSavedAddress) => Buyer;

        public Guid? TryResolveListActor() => Buyer;

        public CartAccess BuildCartAccess(string? guestSecret) => new(Buyer, guestSecret);
    }

    /// <summary>
    /// <b>هم‌زمانی ۱:</b> context مستقل B نسخهٔ انتخاب را <b>پیش از</b> گرفتن قفل ریشه توسط A جلو می‌برد؛
    /// A با نسخهٔ قدیمی رد می‌شود (کد دقیق conflict) و هیچ رزرو/سفارشی ساخته نمی‌شود. اثبات با قفل واقعی
    /// PostgreSQL (نه sleep و نه فرض).
    /// </summary>
    [Fact]
    public async Task A_selection_advanced_before_the_root_lock_is_an_exact_conflict_with_zero_reservation()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        using var harness = new CapturedSubmitHarness(db);
        await SeedTwoGroupDraftAsync(db);

        // B: یک به‌روزرسانی باز (uncommitted) روی همان ردیف ریشه.
        await using var dbB = CreateContext(target.ConnectionString);
        await using var txB = await dbB.Database.BeginTransactionAsync();
        await dbB.Database.ExecuteSqlRawAsync(
            "UPDATE \"order\".cart_shipping_drafts SET selection_revision = 2 WHERE cart_id = {0}",
            CartId);

        var submitA = Task.Run(() => harness.Directory.SubmitAsync(
            harness.Command(expectedSelectionRevision: "1"),
            CancellationToken.None));

        // A باید روی همان ردیف منتظر بماند: انتظار قفل واقعی، حداکثر ۱۰ ثانیه.
        Assert.True(
            await WaitForLockWaiterAsync(target.ConnectionString, TimeSpan.FromSeconds(10)),
            "expected the submit to block on the persisted Order root row");
        Assert.False(submitA.IsCompleted);

        await txB.CommitAsync();

        var error = await Assert.ThrowsAsync<ContractOperationException>(() => submitA);
        Assert.Equal("storefront.captured_selection.conflict", error.Code);
        Assert.Equal(0, harness.Reservation.Calls);
        Assert.Equal(0, harness.Conversion.Calls);

        db.ChangeTracker.Clear();
        Assert.False(await db.Checkouts.AnyAsync(x => x.CartId == CartId));
        Assert.Equal(0, await db.Set<OrderShippingGroup>().CountAsync());
    }

    /// <summary>
    /// <b>هم‌زمانی ۲:</b> A قفل ریشه را گرفته و در حال پایدارسازی است؛ B با <b>همان</b> نسخهٔ موردانتظار
    /// از آداپتر واقعی ۲۳۲ روی همان ردیف بلاک می‌شود. پس از یک‌بار آزادسازی، A پایدار می‌شود و نوشتن
    /// انتخاب بعدی، شاهدِ سفارشِ پذیرفته‌شده را تغییر نمی‌دهد.
    /// </summary>
    [Fact]
    public async Task A_held_root_lock_blocks_the_real_selection_writer_and_the_order_evidence_survives()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var barrier = new PausingBarrier(reached, release);
        using var harness = new CapturedSubmitHarness(db, barrier: barrier);
        await SeedTwoGroupDraftAsync(db);

        var submitA = Task.Run(() => harness.Directory.SubmitAsync(
            harness.Command(expectedSelectionRevision: "1"),
            CancellationToken.None));
        // A اکنون قفل ریشه را دارد (پس از رزرو، پیش از نوشتن سفارش) و منتظر آزادسازی است.
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(10));

        await using var dbB = CreateContext(target.ConnectionString);
        var storeB = new Tooba.Order.Infrastructure.Storefront.StorefrontShippingDraftStore(dbB);
        var intentB = await storeB.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        Assert.NotNull(intentB);
        var advanceB = Task.Run(() => storeB.ReplaceCapturedSelectionAsync(
            intentB!,
            intentB!.ShippingGroups,
            isNew: false,
            expectedSelectionRevision: 1,
            CancellationToken.None));

        Assert.True(
            await WaitForLockWaiterAsync(target.ConnectionString, TimeSpan.FromSeconds(10)),
            "expected the real selection writer to block on the accepted Order root lock");
        Assert.False(advanceB.IsCompleted);

        release.TrySetResult();
        var accepted = await submitA;

        // نوشتن انتخاب B پس از آزادسازی کامل می‌شود و نسخه را جلو می‌برد…
        var advanced = await advanceB;
        Assert.Equal(2, advanced.SelectionRevision);

        // …ولی شاهد پایدار سفارش دست‌نخورده می‌ماند.
        db.ChangeTracker.Clear();
        var groups = await db.Set<OrderShippingGroup>()
            .Where(x => x.CheckoutId == accepted.CheckoutId)
            .ToListAsync();
        Assert.Equal(2, groups.Count);
        Assert.All(groups, group => Assert.Equal(2L, group.AcceptedEvidence!.SourceRevision));
        Assert.Equal(DateA, groups.Single(x => x.ShippingGroupId == DraftGroupA).AcceptedEvidence!.ReceivingDate);
        Assert.Equal(DateB, groups.Single(x => x.ShippingGroupId == DraftGroupB).AcceptedEvidence!.ReceivingDate);
        Assert.Equal(1, harness.Reservation.Calls);
    }

    internal sealed class PausingBarrier(
        TaskCompletionSource reached,
        TaskCompletionSource release) : ICheckoutCommitBarrier
    {
        public Task OnAfterReserveAsync(CancellationToken cancellationToken)
        {
            reached.TrySetResult();
            return release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        }

        public Task OnAfterOrderWriteAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task OnAfterCartConvertedWriteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>انتظار قفل واقعی: یک backend در حال انتظار روی قفل ردیف (حداکثر تا مهلت داده‌شده).</summary>
    private static async Task<bool> WaitForLockWaiterAsync(string connectionString, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            await using var probe = new Npgsql.NpgsqlConnection(connectionString);
            await probe.OpenAsync();
            await using var command = probe.CreateCommand();
            command.CommandText =
                "SELECT count(*) FROM pg_stat_activity "
                + "WHERE datname = current_database() AND wait_event_type = 'Lock'";
            var waiting = Convert.ToInt64(await command.ExecuteScalarAsync());
            if (waiting > 0)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return false;
    }

    private static OrderDbContext CreateContext(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<OrderDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            builder, connectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(builder.Options);
    }

    internal sealed class FailingBarrier(Exception failure) : ICheckoutCommitBarrier
    {
        public Task OnAfterReserveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task OnAfterOrderWriteAsync(CancellationToken cancellationToken) =>
            Task.FromException(failure);

        public Task OnAfterCartConvertedWriteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>
    /// DI واقعی scope: دایرکتوری، خوانندهٔ فکت و هستهٔ ترکیب باید بدون چرخه resolve شوند و همان نمونهٔ
    /// scoped دایرکتوری خوانندهٔ فکت باشد.
    /// </summary>
    [Fact]
    public void The_module_scope_resolves_the_directory_reader_and_core_without_a_cycle()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => new OrderDbContext(
            new DbContextOptionsBuilder<OrderDbContext>().Options));
        services.AddScoped<IOrderUseCaseGuard>(_ => new FakeGuard());
        services.AddScoped<ICartQueryGateway>(_ => new FakeCartGateway());
        services.AddScoped<IOfferLookupGateway>(_ => new FakeOfferLookup());
        services.AddScoped<IPriceLookupGateway>(_ => new FakePriceLookup());
        services.AddScoped<IOrderInventoryLifecyclePort>(_ => new NoCallInventoryLifecycle());
        services.AddScoped<ITaxCalculator>(_ => new FakeTaxCalculator());
        services.AddScoped<ICheckoutPromotionPort>(_ => new FakePromotion());
        services.AddScoped<ICatalogCheckoutLookup>(_ => new FakeCatalogCheckout());
        services.AddScoped<ISellerOrderCancelFulfillmentGate>(_ => new NoCallCancelGate());
        services.AddScoped<IClock>(_ => new FixedClock());
        services.AddScoped<IIdGenerator>(_ => new SequentialIds());
        services.AddScoped<ICheckoutInventoryReservationPort>(_ => new NoCallReservationPort());
        services.AddScoped<ICartConversionPort>(_ => new NoCallCartConversion());
        services.AddScoped<IShippingCapturedRevisionQuoteLookup>(_ => new RecordingCapturedQuoteLookup());
        services.AddScoped<ICheckoutDirectory, CheckoutDirectory>();
        services.AddScoped<ICheckoutShippingGroupGoodsFactsReader>(
            sp => (ICheckoutShippingGroupGoodsFactsReader)sp.GetRequiredService<ICheckoutDirectory>());
        services.AddScoped<StorefrontCapturedGroupProjectionComposer>();
        services.AddScoped<StorefrontCapturedGroupCompositionCore>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var scope = provider.CreateScope();
        var directory = scope.ServiceProvider.GetRequiredService<ICheckoutDirectory>();
        var reader = scope.ServiceProvider.GetRequiredService<ICheckoutShippingGroupGoodsFactsReader>();
        var composer = scope.ServiceProvider.GetRequiredService<StorefrontCapturedGroupProjectionComposer>();
        var core = scope.ServiceProvider.GetRequiredService<StorefrontCapturedGroupCompositionCore>();

        Assert.Same(directory, reader);
        Assert.NotNull(composer);
        Assert.NotNull(core);
        Assert.Same(core, scope.ServiceProvider.GetRequiredService<StorefrontCapturedGroupCompositionCore>());
    }

    internal sealed class CapturedSubmitHarness : IDisposable
    {
        internal readonly RecordingCapturedQuoteLookup Quotes = new();
        internal readonly CountingReservationPort Reservation = new();
        internal readonly RecordingConversionPort Conversion = new();
        internal readonly CountingAssignmentLookup Assignments = new();
        internal readonly FakeCartGateway Carts = new();
        internal readonly FakeOfferLookup Offers = new();
        internal readonly FakePriceLookup Prices = new();
        internal readonly FakePromotion Promotions = new();
        internal readonly FakeTaxCalculator Taxes = new();
        internal readonly FakeCatalogCheckout Catalog = new();
        internal readonly FakeGuard Guard = new();
        internal readonly StrictGeography Geography = new();
        internal readonly FixedCapturedPointerLookup Pointers = new();
        internal readonly FixedShippingTypeReader Types = new();
        internal readonly StrictCatalogReader ShippingCatalog = new();
        internal readonly RecordingAddressBook Addresses = new();
        internal readonly CheckoutDirectory Directory;

        internal CapturedSubmitHarness(
            OrderDbContext db,
            bool checkoutEnabled = true,
            bool sharedOffer = false,
            decimal? freightA = null,
            ICheckoutCommitBarrier? barrier = null,
            bool withAddressBook = true,
            ToobaEdition edition = ToobaEdition.SingleStore,
            string? tenantId = "store-alpha")
        {
            CheckoutEnabled = checkoutEnabled;
            SharedOffer = sharedOffer;
            FreightA = freightA;
            Registrations.RegisterAll(Prices);
            Registrations.RegisterAll(Offers);
            // مسیر واقعی نقل‌قول: ۱۰۰ − ۱۰ + ۹ = ۹۹ و ۲۰۰ − ۲۰ + ۱۸ = ۱۹۸.
            Promotions.Discounts[OfferA] = 10m;
            Promotions.Discounts[OfferB] = 20m;
            Taxes.Rate = 0.10m;
            Quotes.Respond = Quoted;
            Directory = new CheckoutDirectory(
                db,
                Guard,
                Carts,
                Offers,
                Prices,
                new NoCallInventoryLifecycle(),
                Taxes,
                Promotions,
                Catalog,
                new NoCallCancelGate(),
                new FixedClock(),
                new SequentialIds(),
                NullLogger<CheckoutProcessManager>.Instance,
                commitBarrier: barrier ?? new NullCheckoutCommitBarrier(),
                abuseGate: new NullCheckoutAbuseGate(),
                inventoryReservation: Reservation,
                cartConversion: Conversion,
                capturedPreview: Options.Create(new StorefrontCapturedPreviewOptions
                {
                    Enabled = true,
                    SelectionEnabled = true,
                    CheckoutEnabled = CheckoutEnabled,
                    AvailableDateCount = 7,
                }),
                offerAssignments: Assignments,
                shippingTypes: Types,
                selectedCaptures: Pointers,
                capturedComposition: new StorefrontCapturedGroupCompositionCore(Quotes, new FixedClock()),
                geography: Geography,
                edition: new FakeEdition(edition),
                tenant: new FakeTenant(tenantId),
                addressBook: withAddressBook ? Addresses : null,
                shippingCatalog: ShippingCatalog);
            ConfigureCart();
        }

        internal bool CheckoutEnabled { get; }

        internal bool SharedOffer { get; }

        internal decimal? FreightA { get; }

        internal int PointerCalls => Pointers.Calls;

        internal int AssignmentCalls => Assignments.Calls;

        internal int CatalogCalls => ShippingCatalog.Calls;

        internal void ConfigureCart()
        {
            Carts.Cart = SharedOffer
                ? CartOf([Line(LineA, OfferA, SellerA), Line(LineB, OfferA, SellerA)])
                : CartOf([Line(LineA, OfferA, SellerA), Line(LineB, OfferB, SellerB)]);
        }

        /// <summary>
        /// دو آفر <b>متفاوت</b> از یک فروشندهٔ واقعی در یک گروه ارسال؛ افراز خطوط معتبر می‌ماند تا
        /// اعتبارسنجی هر خط (آفر/مسئولیت/نوع) جداگانه سنجیده شود، نه افراز مختلط. آفر دوم هم واقعاً
        /// همان فروشنده را بیان می‌کند تا نقل‌قول اولیهٔ submit به خطای فروشنده نرسد.
        /// </summary>
        internal void UseSingleSellerTwoOfferCart()
        {
            Offers.Register(new OfferReference(
                OfferB,
                VariantOf(OfferB),
                SellerA,
                SalesChannel.Direct,
                OfferStatus.Active,
                null));
            Carts.Cart = CartOf([Line(LineA, OfferA, SellerA), Line(LineB, OfferB, SellerA)]);
        }

        internal SubmitCheckoutCommand Command(
            string? expectedSelectionRevision,
            string? coupon = null,
            string? postalCode = null) => new(
            CartId,
            new CartAccess(Buyer, null),
            7,
            OrderMode.OnlinePurchase,
            BuyerPartyId: null,
            PlacedByUserId: Buyer,
            IdempotencyKey: "tb233r3-positive",
            TaxJurisdiction: "IR-NAT",
            RecipientName: "گیرنده",
            ContactMobile: "09120000000",
            ProvinceName: "تهران",
            CityName: "تهران",
            PostalAddress: "نشانی",
            PostalCode: postalCode ?? "1234567890",
            ShippingMethodCode: "post:standard",
            ShippingMethodLabel: "پست",
            ShippingAmount: 200m,
            MinimumDeliveryDate: new DateOnly(2026, 10, 26),
            RequestedDeliveryDate: DateA,
            RequestedDeliveryTimeWindow: "09:00-17:00",
            CouponCode: coupon,
            ShippingGroups: [],
            ExpectedSelectionRevision: expectedSelectionRevision);

        public void Dispose()
        {
        }

        private static CartSnapshot CartOf(CartLineSnapshot[] lines) => new(
            CartId,
            CartStatus.Active,
            CartAccessKind.Authenticated,
            Buyer,
            "IR",
            "IRR",
            SalesChannel.Direct,
            Now.AddHours(1),
            CartConversionIntent.OnlinePurchase,
            7,
            lines);

        private static CartLineSnapshot Line(Guid lineId, Guid offerId, Guid sellerPartyId) => new(
            lineId,
            offerId,
            VariantOf(offerId),
            sellerPartyId,
            1m,
            ReservationId: null,
            QuotedAmount: offerId == OfferA ? 100m : 200m,
            QuotedCurrency: "IRR",
            QuotedTaxExclusive: true,
            PriceId: PriceOf(offerId),
            QuotedAt: Now);

        private ShippingCapturedQuoteResult Quoted(ShippingCapturedQuoteLookupRequest request)
        {
            var isA = SharedOffer || request.FinalGroupGoodsAmountAfterDiscountAndTax == 99m;
            var freight = isA ? FreightA ?? 120m : 70m;
            return new ShippingCapturedQuoteResult(
                ShippingCapturedQuoteVerdict.Quoted,
                ShippingCapturedQuoteVerdictCodes.Quoted,
                "post",
                "standard",
                "IRR",
                freight,
                5m,
                freight + 5m,
                freight + 5m,
                0m,
                Settings: null,
                SnapshotId: SnapshotId,
                DraftId: request.DraftId,
                SourceRevision: request.SourceRevision,
                FormatVersion: Quotes.FormatVersion,
                ShippingTypeId: ShippingTypeId,
                GroupId: AreaGroup,
                RateRowId: isA ? RowA : RowB,
                MinDeliveryDays: 2,
                MaxDeliveryDays: 3,
                ReceivingWeek: Week());
        }

        /// <summary>
        /// یک هفتهٔ <b>مشترک</b> ذخیره‌شده برای همان snapshot/Binding؛ فقط دو روز کاری منتخب ساعت
        /// کسری متفاوت دارند (۱۰۰نانوثانیه‌ای) تا استقلال تاریخ/ساعت قابل اثبات باشد.
        /// </summary>
        private static ShippingCapturedReceivingWeek Week() => new(
            "UTC",
            Enumerable.Range(1, 7)
                .Select(iso => iso switch
                {
                    2 => new ShippingCapturedReceivingWeekDay(iso, true, At(OpenTicksA), At(CloseTicksA)),
                    3 => new ShippingCapturedReceivingWeekDay(iso, true, At(OpenTicksB), At(CloseTicksB)),
                    _ => new ShippingCapturedReceivingWeekDay(iso, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                })
                .ToArray());

        private static TimeOnly At(long ticks) => new(ticks);
    }

    internal static async Task SeedTwoGroupDraftAsync(
        OrderDbContext db,
        string? mutate = null,
        Guid? savedAddressId = null,
        decimal flatShippingAmount = 200m)
    {
        const string service = "post";
        const string option = "standard";
        var draft = NewDraft(savedAddressId, flatShippingAmount);
        var groupA = DraftGroup(DraftGroupA, SellerA, service, option, 125m, DateA);
        var groupB = DraftGroup(DraftGroupB, SellerB, service, option, 75m, DateB);
        groupA.AddWholeLine(LineA, SellerA);
        groupB.AddWholeLine(LineB, SellerB);

        groupA.ReplaceAcceptedEvidence(
            EvidenceFor(
                mutate == "owner" ? "other-tenant" : "store-alpha",
                A: true,
                120m,
                mutate == "date" ? new TimeOnly(16, 0).Ticks : CloseTicksA),
            Now);
        groupB.ReplaceAcceptedEvidence(
            EvidenceFor("store-alpha", A: false, 70m, CloseTicksB),
            Now);

        // TB-236: هر گروه در یک انتخاب واقعی مبلغ کالای همان قصد را هم تأیید می‌کند؛ همان عددی که
        // مسیر واقعی ۲۱۷ می‌سازد (۱۰۰−۱۰+۹٪ = ۹۹ و ۲۰۰−۲۰+۹٪ = ۱۹۸).
        groupA.ConfirmGoodsAmount(99m, Now);
        groupB.ConfirmGoodsAmount(198m, Now);

        draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);
        draft.AddShippingGroups([groupA, groupB], Now);
        if (mutate != "root")
        {
            draft.BumpSelectionRevision(Now);
        }

        db.ShippingDrafts.Add(draft);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    internal static async Task SeedSingleGroupDraftAsync(
        OrderDbContext db,
        Guid sellerId,
        Guid[] lines,
        decimal confirmedGoods = 297m)
    {
        var draft = NewDraft(null);
        var group = DraftGroup(DraftGroupA, sellerId, "post", "standard", 125m, DateA);
        foreach (var line in lines)
        {
            group.AddWholeLine(line, sellerId);
        }

        group.ReplaceAcceptedEvidence(EvidenceFor("store-alpha", A: true, 120m, CloseTicksA), Now);
        // TB-236: یک گروه با هر دو خط (۱۰۰ و ۲۰۰) با همان قصد: ۹۹ + ۱۹۸ = ۲۹۷؛ دو خط از یک آفر
        // ۱۰۰تایی: ۹۰ + ۹۰ + ۹٪ = ۱۹۸ (مقدار از فراخوان می‌آید، نه حدس).
        group.ConfirmGoodsAmount(confirmedGoods, Now);
        draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);
        draft.AddShippingGroups([group], Now);
        draft.BumpSelectionRevision(Now);
        db.ShippingDrafts.Add(draft);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    /// <summary>
    /// یک پیش‌نویس <b>واقعاً قدیمی</b> (بدون هیچ شاهد ۲۳۱/۲۳۲) برای کنترل مسیر legacy: همان ریشه/گروه/خط
    /// موجود، فقط بدون capture — هیچ دادهٔ ساختگی مالی اضافه نمی‌شود.
    /// </summary>
    internal static async Task SeedLegacyGroupDraftAsync(OrderDbContext db)
    {
        var draft = NewDraft(null);
        var group = DraftGroup(DraftGroupA, SellerA, "post", "standard", 125m, DateA);
        group.AddWholeLine(LineA, SellerA);
        draft.AddShippingGroups([group], Now);
        draft.BumpSelectionRevision(Now);
        db.ShippingDrafts.Add(draft);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    internal static CartShippingDraft NewDraft(Guid? savedAddressId, decimal flatShippingAmount = 200m) => CartShippingDraft.Create(        CartId,
        "hash",
        7,
        "گیرنده",
        "09120000000",
        "تهران",
        "تهران",
        "نشانی",
        "1234567890",
        savedAddressId,
        "post:standard",
        "پست",
        flatShippingAmount,
        new DateOnly(2026, 10, 26),
        DateA,
        "09:00-17:00",
        null,
        Now);

    internal static CartShippingDraftGroup DraftGroup(
        Guid groupId,
        Guid sellerId,
        string serviceCode,
        string optionCode,
        decimal payable,
        DateOnly date) => CartShippingDraftGroup.Create(
        groupId,
        CartId,
        sellerId,
        $"{serviceCode}:{optionCode}",
        serviceCode,
        optionCode,
        "روش",
        null,
        payable,
        "IRR",
        new DateOnly(2026, 10, 26),
        date,
        "09:00-17:00",
        0,
        Now);

    internal static ShippingGroupAcceptedEvidence EvidenceFor(
        string tenant,
        bool A,
        decimal freight,
        long closeTicks) =>
        EvidenceForOwner(ShippingGroupOwner.Store(tenant), A, freight, closeTicks);

    /// <summary>همان شاهد با مالک <b>تایپ‌شده</b> (Marketplace/Seller) برای افراز چندمالکی.</summary>
    internal static ShippingGroupAcceptedEvidence EvidenceForOwner(
        ShippingGroupOwner owner,
        bool A,
        decimal freight,
        long closeTicks)
    {
        return ShippingGroupAcceptedEvidence.Create(
            SnapshotId,
            CartId,
            2,
            1,
            1,
            owner,
            AreaGroup,
            A ? RowA : RowB,
            ShippingTypeId,
            freight,
            5m,
            "IRR",
            freight + 5m,
            0m,
            A ? DateA : DateB,
            A ? OpenTicksA : OpenTicksB,
            closeTicks,
            "UTC",
            2,
            3);
    }

    /// <summary>
    /// دانهٔ <b>چندمالکی</b> Marketplace: گروه A با مالک <c>Marketplace</c> و گروه B با مالک فروشندهٔ واقعی
    /// <see cref="SellerB"/> روی <b>همان</b> Store binding (همان سرویس/گزینه)، هر گروه با ردیف/تاریخ/مبلغ خودش.
    /// </summary>
    internal static async Task SeedMarketplaceOwnerDraftAsync(OrderDbContext db, bool storeOwnerForPlatform = false)
    {
        const string service = "post";
        const string option = "standard";
        var draft = NewDraft(null);
        var groupA = DraftGroup(DraftGroupA, SellerA, service, option, 125m, DateA);
        var groupB = DraftGroup(DraftGroupB, SellerB, service, option, 75m, DateB);
        groupA.AddWholeLine(LineA, SellerA);
        groupB.AddWholeLine(LineB, SellerB);

        groupA.ReplaceAcceptedEvidence(
            EvidenceForOwner(
                // کنترل منفی: مالک ذخیره‌شده Store در نصب Marketplace با مالک بازساخته‌شده نمی‌خواند.
                storeOwnerForPlatform ? ShippingGroupOwner.Store("store-alpha") : ShippingGroupOwner.Marketplace(),
                A: true,
                120m,
                CloseTicksA),
            Now);
        groupB.ReplaceAcceptedEvidence(
            EvidenceForOwner(ShippingGroupOwner.Seller(SellerB), A: false, 70m, CloseTicksB),
            Now);

        // TB-236: همان مبلغ کالای تأییدشدهٔ مسیر واقعی ۲۱۷ برای هر گروه (۹۹ و ۱۹۸).
        groupA.ConfirmGoodsAmount(99m, Now);
        groupB.ConfirmGoodsAmount(198m, Now);

        draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);
        draft.AddShippingGroups([groupA, groupB], Now);
        draft.BumpSelectionRevision(Now);
        db.ShippingDrafts.Add(draft);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    internal static Guid VariantOf(Guid offerId)
    {
        var bytes = offerId.ToByteArray();
        bytes[15] ^= 0x5a;
        return new Guid(bytes);
    }

    internal static Guid PriceOf(Guid offerId)
    {
        var bytes = offerId.ToByteArray();
        bytes[14] ^= 0x33;
        return new Guid(bytes);
    }

    internal static async Task<OrderDbContext> ReadyAsync(OrderEvidenceRelationalTestTarget target)
    {
        var builder = new DbContextOptionsBuilder<OrderDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            builder, target.ConnectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        var db = new OrderDbContext(builder.Options);
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"order\".order_shipping_group_lines;"
            + "DELETE FROM \"order\".order_shipping_groups;"
            + "DELETE FROM \"order\".checkout_processes;" + "DELETE FROM \"order\".order_lines;"
            + "DELETE FROM \"order\".seller_orders;"
            + "DELETE FROM \"order\".checkouts;"
            + "DELETE FROM \"order\".cart_shipping_group_lines;"
            + "DELETE FROM \"order\".cart_shipping_groups;"
            + "DELETE FROM \"order\".cart_shipping_drafts;");
        db.ChangeTracker.Clear();
        return db;
    }

    internal sealed class CountingReservationPort : ICheckoutInventoryReservationPort
    {
        internal int Calls { get; private set; }

        internal IReadOnlyList<Guid> LastLines { get; private set; } = [];

        public Task<CheckoutInventoryReservationResult> ReserveForCheckoutAsync(
            CheckoutInventoryReservationRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastLines = request.Lines.Select(x => x.CartLineId).ToArray();
            return Task.FromResult(new CheckoutInventoryReservationResult(
                request.Lines
                    .Select((line, index) => new CheckoutInventoryLineReservation(
                        line.CartLineId,
                        Guid.Parse($"01a17d12-0000-7000-8000-{index:D12}")))
                    .ToArray()));
        }

        public Task ReleaseAsync(IEnumerable<Guid> reservationIds, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    internal sealed class RecordingConversionPort : ICartConversionPort
    {
        internal int Calls { get; private set; }

        public Task<CartConversionResult> ConvertForCheckoutAsync(
            CartConversionRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new CartConversionResult(
                request.CartId, request.ExpectedVersion + 1, CartStatus.Converted, request.Intent));
        }
    }

    internal sealed class CountingAssignmentLookup : IOfferFulfillmentAssignmentLookup
    {
        internal int Calls { get; private set; }

        /// <summary>مسئولیت صریح هر آفر؛ آفر بدون ورودی همان Platform است (پیش‌فرض دانهٔ ۲۳۳).</summary>
        internal readonly Dictionary<Guid, OfferFulfillmentResponsibility?> Responsibility = [];

        /// <summary>فروشندهٔ واقعی هر آفر (پیش‌فرض دانهٔ ۲۳۳: A→SellerA و بقیه→SellerB).</summary>
        internal readonly Dictionary<Guid, Guid> SellerOf = [];

        /// <summary>
        /// آفرِ <b>داخل</b> شاهدِ برگشتی برای کنترل منفی «ردیف زیر کلید درست اما با آفرِ دیگر»؛ پیش‌فرض
        /// همان آفر درخواستی است.
        /// </summary>
        internal readonly Dictionary<Guid, Guid> PayloadOfferId = [];

        public Task<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>> GetAssignmentsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>>(
                offerIds.ToDictionary(
                    id => id,
                    id => new OfferFulfillmentAssignmentSnapshot(
                        PayloadOfferId.TryGetValue(id, out var payload) ? payload : id,
                        SellerOf.TryGetValue(id, out var seller)
                            ? seller
                            : id == OfferA ? SellerA : SellerB,
                        Responsibility.TryGetValue(id, out var responsibility)
                            ? responsibility
                            : OfferFulfillmentResponsibility.Platform,
                        ShippingTypeId)));
        }
    }

    internal sealed class FixedShippingTypeReader : IShippingTypeReader
    {
        public Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>([Active()]);

        public Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>(
                shippingTypeIds.Contains(ShippingTypeId) ? [Active()] : []);

        private static ShippingTypeSnapshot Active() => new(
            ShippingTypeId, "heavy", true, 1, 1, []);
    }

    /// <summary>فهرست فعال فروشگاه: یک سرویس post با گزینهٔ standard (قابل خاموش‌کردن برای کنترل منفی).</summary>
    internal sealed class StrictCatalogReader : IShippingCatalogReader
    {
        internal int Calls { get; private set; }

        internal bool Active { get; set; } = true;

        internal bool OptionActive { get; set; } = true;

        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>(
            [
                new ShippingCatalogServiceSnapshot(
                    Guid.Parse("01a17d14-0000-7000-8000-0000000000a1"),
                    "post",
                    "Manual",
                    "post",
                    "blue",
                    Active,
                    1,
                    [],
                    [
                        new ShippingCatalogOptionSnapshot(
                            Guid.Parse("01a17d14-0000-7000-8000-0000000000a2"),
                            Guid.Parse("01a17d14-0000-7000-8000-0000000000a1"),
                            "standard",
                            OptionActive,
                            1,
                            []),
                    ]),
            ]);
        }

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the captured final acceptance reads the catalog once");
    }

    /// <summary>اشاره‌گر ۲۱۹: همان Binding/همان snapshot و نسخهٔ قابل تغییر برای کنترل تازگی.</summary>
    internal sealed class FixedCapturedPointerLookup : IShippingSelectedCapturedRevisionLookup
    {
        internal int Calls { get; private set; }

        internal long Revision { get; set; } = 2;

        /// <summary>
        /// پاسخ <b>وابسته به مالک/Binding</b> برای اثبات چندمالکی؛ تهی یعنی پاسخ یکسانِ ثابت پایین. هیچ
        /// مسیر تولیدی از این درز استفاده نمی‌کند.
        /// </summary>
        internal Func<ShippingSelectedCapturedRevisionRequest, ShippingSelectedCapturedRevisionResult?>? Respond { get; set; }

        public Task<ShippingSelectedCapturedRevisionResult> ReadAsync(
            ShippingSelectedCapturedRevisionRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (Respond is { } respond)
            {
                var answered = respond(request)
                    ?? throw new InvalidOperationException("the pointer fixture refused an unknown owner/binding");
                return Task.FromResult(answered);
            }

            return Task.FromResult(Default(request));
        }

        internal ShippingSelectedCapturedRevisionResult Default(
            ShippingSelectedCapturedRevisionRequest request) =>
            new(
                ShippingSelectedCaptureVerdict.Selected,
                null,
                ShippingSelectedCaptureReadState.Selected,
                1,
                SnapshotId,
                CartId,
                Revision,
                1,
                request.ServiceCode,
                request.OptionCode,
                ShippingTypeId,
                []);
    }

    /// <summary>
    /// Geography سخت‌گیر: کد کشور تهی/نادرست یا نام نادرست هرگز «حل‌شده» نمی‌شود و کد واقعی داده‌شده
    /// ثبت می‌گردد؛ هیچ کشوری به‌جای کشور قیمت‌گذاری‌شده انتخاب نمی‌شود.
    /// </summary>
    internal sealed class StrictGeography : IGeographyLookup
    {
        internal string? LastCountryCode { get; private set; }

        internal bool HidePricedCountry { get; set; }

        public Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CountrySnapshot>>(
            [
                new CountrySnapshot(HidePricedCountry ? OtherCountryId : CountryId, "IR", true),
            ]);

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
            Guid countryId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the captured final acceptance resolves the destination only");

        public Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
            Guid countryId,
            Guid? administrativeAreaId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the captured final acceptance resolves the destination only");

        public Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
            Guid cityId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the captured final acceptance resolves the destination only");

        public Task<DestinationIdentifiers?> GetByIdsAsync(
            DestinationIdentifiers identifiers,
            CancellationToken cancellationToken) =>
            Task.FromResult<DestinationIdentifiers?>(identifiers);

        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken)
        {
            LastCountryCode = countryCode;
            var code = countryCode?.Trim();
            if (string.IsNullOrEmpty(code)
                || !string.Equals(code, "IR", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(administrativeAreaName?.Trim(), "تهران", StringComparison.Ordinal)
                || !string.Equals(cityName?.Trim(), "تهران", StringComparison.Ordinal)
                || !string.IsNullOrEmpty(serviceAreaName))
            {
                return Task.FromResult(DestinationResolution.Unresolved());
            }

            return Task.FromResult(DestinationResolution.Resolved(
                new DestinationIdentifiers(CountryId, ProvinceId, CityId, null)));
        }
    }

    /// <summary>کتاب آدرس: فقط رکورد خودیِ actor واقعی؛ حذف/بیگانگی/تغییر با پرچم قابل شبیه‌سازی.</summary>
    internal sealed class RecordingAddressBook : IAddressBookCheckoutLookup
    {
        internal int Calls { get; private set; }

        internal Guid LastActor { get; private set; }

        internal Guid LastAddressId { get; private set; }

        internal bool Drop { get; set; }

        internal bool ActorMismatch { get; set; }

        internal bool ChangePostalAddress { get; set; }

        public Task<CustomerAddressRecord?> GetAsync(
            Guid actorUserId,
            Guid addressId,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastActor = actorUserId;
            LastAddressId = addressId;
            if (Drop || ActorMismatch || actorUserId != Buyer || addressId != SavedAddressId)
            {
                return Task.FromResult<CustomerAddressRecord?>(null);
            }

            return Task.FromResult<CustomerAddressRecord?>(new CustomerAddressRecord(
                SavedAddressId,
                "گیرنده",
                "09120000000",
                "IR",
                "تهران",
                "تهران",
                "1234567890",
                ChangePostalAddress ? "نشانی ویرایش‌شده" : "نشانی",
                null,
                "خانه",
                true,
                Now,
                Now,
                "گیرنده",
                "آزمایشی"));
        }
    }

    internal sealed class FakeEdition(ToobaEdition edition = ToobaEdition.SingleStore) : ICurrentEdition
    {
        public EditionContext? Current { get; } = new(edition, "tb233r3");
    }

    internal sealed class FakeTenant(string? tenantId = "store-alpha") : ICurrentTenant
    {
        public TenantContext? Current { get; } = tenantId is null
            ? null
            : new(
                new TenantId(tenantId),
                TenantStatus.Active,
                new ConnectionReference("tb233r3"),
                "Tooba",
                null,
                "IR",
                "127.0.0.1",
                null);
    }

    internal static class Registrations
    {
        internal static void RegisterAll(FakePriceLookup prices)
        {
            prices.Register(new PriceQuote(PriceOf(OfferA), OfferA, "IR", SalesChannel.Direct, 100m, "IRR", true, true));
            prices.Register(new PriceQuote(PriceOf(OfferB), OfferB, "IR", SalesChannel.Direct, 200m, "IRR", true, true));
        }

        internal static void RegisterAll(FakeOfferLookup offers)
        {
            offers.Register(new OfferReference(OfferA, VariantOf(OfferA), SellerA, SalesChannel.Direct, OfferStatus.Active, null));
            offers.Register(new OfferReference(OfferB, VariantOf(OfferB), SellerB, SalesChannel.Direct, OfferStatus.Active, null));
        }
    }
}

