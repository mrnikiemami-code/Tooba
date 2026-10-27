using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.Storefront;
using Tooba.Payment.Contracts.Storefront;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-234: the ONE public storefront edge — real <see cref="StorefrontShippingService"/>, real
/// <see cref="StorefrontCheckoutService"/>, real <see cref="StorefrontStartPaymentComposer"/> and the
/// accepted <c>CheckoutDirectory</c>/<c>CheckoutProcessManager</c> on the task-owned disposable PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// Controlled Contracts (Inventory reservation/conversion, Pricing/Promotion/Tax, 216 quote, 219 pointer,
/// 228 assignment, Fulfillment catalog, Geography, AddressBook, hold preparation and Payment) are the SAME
/// labelled fakes the accepted 233-R3 class uses. The Order schema, migrations, quote/promotion/tax
/// composition, captured acceptance and Order persistence are real.
/// </para>
/// <para>
/// Nothing here claims an HTTP listener, a PSP call, a live shopper or activation: the gate stays default
/// OFF in checked-in configuration and the negative control proves it stays closed.
/// </para>
/// </remarks>
[Collection(OrderEvidenceRelationalCollection.Name)]
public sealed class StorefrontPublicCommitEdgeTests
{
    private static readonly Guid CartId = CapturedFinalSubmitIntegrationTests.CartId;
    private static readonly Guid DraftGroupA = CapturedFinalSubmitIntegrationTests.DraftGroupA;
    private static readonly Guid DraftGroupB = CapturedFinalSubmitIntegrationTests.DraftGroupB;
    private const int CartVersion = 7;
    private const decimal PayableA = 125m;
    private const decimal PayableB = 75m;

    /// <summary>همان تاریخ/تیک‌های کسری ۱۰۰نانوثانیه‌ای که دانهٔ مشترک ۲۳۳ می‌کارد.</summary>
    private static class Window
    {
        internal static readonly DateOnly DateA = new(2026, 10, 27);
        internal static readonly DateOnly DateB = new(2026, 10, 28);
        internal static readonly long OpenTicksA = new TimeOnly(9, 0, 0).Ticks + 1_234_567L;
        internal static readonly long CloseTicksA = new TimeOnly(17, 0, 0).Ticks + 7_654_321L;
        internal static readonly long OpenTicksB = new TimeOnly(10, 0, 0).Ticks + 5_000_000L;
        internal static readonly long CloseTicksB = new TimeOnly(18, 0, 0).Ticks + 2_500_000L;
    }

    /// <summary>
    /// <b>۱)</b> ثبت عمومی دو گروه capture‌شده از همان دروازهٔ واقعی: سفارش پایدار، پول/تاریخ/تیک هر گروه
    /// خودش، یک رزرو/یک تبدیل، و <b>صفر</b> بازنویسی انتخاب قدیمی و صفر نقل‌قول دوبارهٔ گروه‌ها.
    /// </summary>
    [Fact]
    public async Task Public_commit_of_two_captured_groups_persists_the_order_without_legacy_rewrite()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var edge = new EdgeHarness(db);
        await CapturedFinalSubmitIntegrationTests.SeedTwoGroupDraftAsync(db);

        var page = await edge.Shipping.CommitAsync(
            CartId,
            null,
            CartVersion,
            "tb234-public-commit",
            couponCode: null,
            expectedSelectionRevision: "1",
            CancellationToken.None);

        Assert.NotNull(page.CheckoutId);
        Assert.Equal(2, page.ShippingGroups!.Count);
        // یک رزرو، یک تبدیل، و دو نقل‌قول ۲۱۶ برای دو گروه (بدون گذر ۲۱۷ دوباره).
        Assert.Equal(1, edge.Core.Reservation.Calls);
        Assert.Equal(1, edge.Core.Conversion.Calls);
        Assert.Equal(2, edge.Core.Quotes.Requests.Count);

        db.ChangeTracker.Clear();
        var checkout = await db.Checkouts
            .Include(x => x.ShippingGroups).ThenInclude(group => group.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(page.CheckoutId, checkout.CheckoutId);
        var groups = checkout.ShippingGroups.ToArray();
        Assert.Equal(2, groups.Length);

        var groupA = Assert.Single(groups, x => x.ShippingGroupId == DraftGroupA);
        var groupB = Assert.Single(groups, x => x.ShippingGroupId == DraftGroupB);
        Assert.Equal(PayableA, groupA.ShippingChargeSnapshot);
        Assert.Equal(PayableB, groupB.ShippingChargeSnapshot);
        Assert.Equal(Window.DateA, groupA.AcceptedEvidence!.ReceivingDate);
        Assert.Equal(Window.OpenTicksA, groupA.AcceptedEvidence!.ReceivingOpenTicks);
        Assert.Equal(Window.CloseTicksA, groupA.AcceptedEvidence!.ReceivingCloseTicks);
        Assert.Equal(Window.DateB, groupB.AcceptedEvidence!.ReceivingDate);
        Assert.Equal(Window.OpenTicksB, groupB.AcceptedEvidence!.ReceivingOpenTicks);
        Assert.Equal(Window.CloseTicksB, groupB.AcceptedEvidence!.ReceivingCloseTicks);

        // مسیر قدیمی هرگز اجرا نشد: ریشهٔ انتخاب دست‌نخورده و همان دو گروه ذخیره‌شده باقی مانده است.
        var stored = await db.ShippingDrafts.Include(x => x.ShippingGroups).SingleAsync(x => x.CartId == CartId);
        Assert.Equal(1, stored.SelectionRevision);
        Assert.Equal(2, stored.ShippingGroups.Count);
    }

    /// <summary>
    /// <b>۲)</b> بازگشت عمومی با <b>همان کلید</b> پس از تغییر اشاره‌گر، حذف نشانی و تبدیل سبد، همان سفارش
    /// مجاز را برمی‌گرداند: صفر نقل‌قول/رزرو/نوشتار/پذیرش تازه — و actor بیگانه همان سفارش را نمی‌گیرد.
    /// </summary>
    [Fact]
    public async Task Public_replay_after_pointer_change_and_converted_cart_returns_the_original_order()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var edge = new EdgeHarness(db);
        await CapturedFinalSubmitIntegrationTests.SeedTwoGroupDraftAsync(db);

        var first = await edge.Shipping.CommitAsync(
            CartId,
            null,
            CartVersion,
            "tb234-public-replay",
            couponCode: null,
            expectedSelectionRevision: "1",
            CancellationToken.None);

        var quoteCalls = edge.Core.Quotes.Requests.Count;
        var pointerCalls = edge.Core.Pointers.Calls;
        var addressCalls = edge.Core.Addresses.Calls;
        var reserved = edge.Core.Reservation.Calls;
        var converted = edge.Core.Conversion.Calls;
        db.ChangeTracker.Clear();
        var orderRows = await db.Checkouts.CountAsync(x => x.CartId == CartId);
        var groupRows = await db.Set<OrderShippingGroup>().CountAsync();

        // دنیای بیرون عوض شده: اشاره‌گر جلو رفته، نشانی حذف شده و سبد Convert شده است.
        edge.Core.Pointers.Revision = 3;
        edge.Core.Addresses.Drop = true;
        edge.MarkCartConverted();

        var replay = await edge.Shipping.CommitAsync(
            CartId,
            null,
            CartVersion,
            "tb234-public-replay",
            couponCode: null,
            expectedSelectionRevision: "1",
            CancellationToken.None);

        Assert.Equal(first.CheckoutId, replay.CheckoutId);
        // هیچ بازاعتبارسنجی/نقل‌قول/رزرو/تبدیل تازه‌ای اجرا نشد.
        Assert.Equal(quoteCalls, edge.Core.Quotes.Requests.Count);
        Assert.Equal(pointerCalls, edge.Core.Pointers.Calls);
        Assert.Equal(addressCalls, edge.Core.Addresses.Calls);
        Assert.Equal(reserved, edge.Core.Reservation.Calls);
        Assert.Equal(converted, edge.Core.Conversion.Calls);
        db.ChangeTracker.Clear();
        Assert.Equal(orderRows, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(groupRows, await db.Set<OrderShippingGroup>().CountAsync());

        // همان کلید با actor بیگانه: دسترسی رد می‌شود و هیچ ردیف تازه‌ای ساخته نمی‌شود.
        using var foreign = new EdgeHarness(db, foreignActor: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => foreign.Shipping.CommitAsync(
            CartId,
            null,
            CartVersion,
            "tb234-public-replay",
            couponCode: null,
            expectedSelectionRevision: "1",
            CancellationToken.None));
        db.ChangeTracker.Clear();
        Assert.Equal(orderRows, await db.Checkouts.CountAsync(x => x.CartId == CartId));
    }

    /// <summary>
    /// <b>۳)</b> «پیش‌نویس پرداخت» capture‌شده فقط-خواندنی: همان گروه‌های اعتبارسنجی‌شدهٔ این لحظه،
    /// بدون حتی یک SaveChanges/رزرو/سفارش/تبدیل؛ و کنترل‌های منفی دروازه/اشاره‌گر/نشانی.
    /// </summary>
    [Fact]
    public async Task Captured_payment_draft_validates_read_only_and_refuses_stale_or_closed_states()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var edge = new EdgeHarness(db);
        // نشانی ذخیره‌شدهٔ واقعی: مسیر فقط-خواندنی همان اقتدار کتاب آدرس را می‌سنجد.
        await CapturedFinalSubmitIntegrationTests.SeedTwoGroupDraftAsync(
            db,
            savedAddressId: CapturedFinalSubmitIntegrationTests.SavedAddressId);

        var page = await edge.Shipping.ProjectPaymentDraftAsync(CartId, null, couponCode: null, CancellationToken.None);

        Assert.Null(page.CheckoutId);
        Assert.Equal(2, page.ShippingGroups!.Count);
        Assert.Equal(PayableA, page.ShippingGroups[0].ShippingAmount);
        Assert.Equal(PayableB, page.ShippingGroups[1].ShippingAmount);
        Assert.Equal(2, edge.Core.Quotes.Requests.Count);
        Assert.True(edge.Core.Addresses.Calls > 0);

        // هیچ نوشتنی: صفر سفارش/گروه سفارش، صفر رزرو/تبدیل، و ریشهٔ انتخاب دست‌نخورده.
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(0, await db.Set<OrderShippingGroup>().CountAsync());
        Assert.Equal(0, edge.Core.Reservation.Calls);
        Assert.Equal(0, edge.Core.Conversion.Calls);
        Assert.Equal(1, (await db.ShippingDrafts.SingleAsync(x => x.CartId == CartId)).SelectionRevision);

        // اشاره‌گر عوض‌شده ⇒ conflict صریح (هیچ قیمت/تاریخ تازه‌ای جعل نمی‌شود).
        edge.Core.Pointers.Revision = 9;
        var stale = await Assert.ThrowsAsync<Tooba.BuildingBlocks.ContractOperationException>(() =>
            edge.Shipping.ProjectPaymentDraftAsync(CartId, null, couponCode: null, CancellationToken.None));
        Assert.Equal(StorefrontCapturedSelectionReasons.Conflict, stale.Code);
        edge.Core.Pointers.Revision = 2;

        // نشانی ذخیره‌شده حذف‌شده ⇒ مقصد تأییدنشده، بدون نوشتن.
        edge.Core.Addresses.Drop = true;
        var missingAddress = await Assert.ThrowsAsync<Tooba.BuildingBlocks.ContractOperationException>(() =>
            edge.Shipping.ProjectPaymentDraftAsync(CartId, null, couponCode: null, CancellationToken.None));
        Assert.Equal(StorefrontCapturedSelectionReasons.DestinationUnverified, missingAddress.Code);
        edge.Core.Addresses.Drop = false;

        // دروازهٔ ناهمساز (ثبت نهایی خاموش) ⇒ همان رد پایدار، پیش از هر خواندن بیرونی.
        using var closed = new EdgeHarness(db, checkoutEnabled: false);
        var refused = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            closed.Shipping.ProjectPaymentDraftAsync(CartId, null, couponCode: null, CancellationToken.None));
        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, refused.Code);

        // و همان دروازهٔ بسته، ثبت عمومی را هم می‌بندد (پیش‌نویس capture هرگز به مسیر قدیمی نمی‌افتد).
        var commitRefused = await Assert.ThrowsAsync<StorefrontOrderException>(() => closed.Shipping.CommitAsync(
            CartId,
            null,
            CartVersion,
            "tb234-closed",
            couponCode: null,
            expectedSelectionRevision: "1",
            CancellationToken.None));
        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, commitRefused.Code);
        Assert.Equal(0, closed.Core.Reservation.Calls);
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
    }

    /// <summary>
    /// <b>۴)</b> مسیر واقعی «کلیک نهایی پرداخت»: بدنهٔ JSON با توکن <b>متنی</b> عظیم دقیقاً به همان ثبت
    /// پذیرفته‌شده می‌رسد؛ توکن عددی/غایب/بدشکل پیش از رزرو رد می‌شود و شکست پایین‌دستی صادقانه گزارش می‌شود.
    /// </summary>
    [Fact]
    public async Task Start_payment_body_token_reaches_the_accepted_commit_without_starting_payment()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        using var edge = new EdgeHarness(db);
        await CapturedFinalSubmitIntegrationTests.SeedTwoGroupDraftAsync(db);

        // توکن غایب و بدشکل: رد پیش از هر رزرو/نوشتن با کد پایدار خود مالک.
        foreach (var token in new[] { null, " 1", "abc", "-1", "1.5" })
        {
            var refused = await edge.Composer.StartPaymentAsync(
                new StorefrontStartPaymentRequest(
                    CartId,
                    GuestSecret: null,
                    CartVersion,
                    $"tb234-bad-{token ?? "missing"}",
                    UseWallet: false,
                    ProviderCode: null,
                    AuthenticatedUserId: edge.Actor.AuthenticatedUserId,
                    CouponCode: null,
                    ExpectedSelectionRevision: token),
                CancellationToken.None);
            Assert.Equal(StorefrontStartPaymentOutcome.CheckoutCommitFailed, refused.Outcome);
            Assert.Equal(StorefrontCapturedSelectionReasons.IntentInvalid, refused.FailureCode);
        }

        Assert.Equal(0, edge.Core.Reservation.Calls);
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));

        // نسخهٔ انتخاب بزرگ‌تر از uint: توکن باید به‌صورت متن و بی‌افت دقیق منتقل شود.
        const long LargeRevision = 4_294_967_296L;
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"order\".cart_shipping_drafts SET selection_revision = {0} WHERE cart_id = {1}",
            LargeRevision,
            CartId);

        // همان مسیری که endpoint طی می‌کند: JSON → StorefrontStartPaymentBody → StorefrontStartPaymentRequest.
        const string json = """
            {"expectedCartVersion":7,"idempotencyKey":"tb234-start-payment","expectedSelectionRevision":"4294967296"}
            """;
        var body = JsonSerializer.Deserialize<StorefrontStartPaymentBody>(json, WebJson)!;
        Assert.Equal("4294967296", body.ExpectedSelectionRevision);

        var result = await edge.Composer.StartPaymentAsync(
            new StorefrontStartPaymentRequest(
                CartId,
                GuestSecret: null,
                body.ExpectedCartVersion,
                body.IdempotencyKey,
                body.UseWallet,
                body.ProviderCode,
                AuthenticatedUserId: edge.Actor.AuthenticatedUserId,
                body.CouponCode,
                body.ExpectedSelectionRevision),
            CancellationToken.None);

        // سفارش واقعاً پایدار شد؛ فقط آماده‌سازی کنترل‌شده توقف کرد و «پرداخت آغاز شد» ادعا نمی‌شود.
        Assert.Equal(StorefrontStartPaymentOutcome.HoldPreparationFailed, result.Outcome);
        Assert.NotNull(result.CheckoutId);
        Assert.False(result.CheckoutReplayed);
        Assert.Equal(1, edge.Core.Reservation.Calls);
        Assert.Equal(0, edge.Payments.Calls);
        db.ChangeTracker.Clear();
        var checkout = await db.Checkouts
            .Include(x => x.ShippingGroups)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(2, checkout.ShippingGroups.Count);
        Assert.StartsWith(StorefrontStartPaymentComposer.CommitKeyPrefix, checkout.IdempotencyKey, StringComparison.Ordinal);
        // نسخهٔ ریشه در پذیرش تغییر نمی‌کند و توکن بزرگ بی‌افت خوانده می‌شود.
        Assert.Equal(
            LargeRevision,
            (await db.ShippingDrafts.SingleAsync(x => x.CartId == CartId)).SelectionRevision);

        // توکن عددی JSON هرگز به یک قصد معتبر تبدیل نمی‌شود (فیلد قرارداد «متن» است).
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StorefrontStartPaymentBody>(
            """{"expectedCartVersion":7,"idempotencyKey":"k","expectedSelectionRevision":4294967296}""",
            WebJson));
    }

    /// <summary>همان گزینه‌های JSON وب که endpoint برای بدنه استفاده می‌کند.</summary>
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// <b>۵)</b> مسیر <b>واقعاً قدیمی</b> دست‌نخورده است: با همهٔ کلیدهای capture خاموش، پیش‌نویس بدون شاهد
    /// از همان اعتبارسنجی قدیمی می‌گذرد (خطای خودش، نه <c>not_ready</c>) و هیچ نوشتنی رخ نمی‌دهد؛ و همان
    /// پیش‌نویس قدیمی با پیش‌نمایش روشن اما <b>بدون شاهد ذخیره‌شده</b> به مسیر قدیمی نمی‌افتد.
    /// </summary>
    [Fact]
    public async Task Legacy_off_route_is_unchanged_and_captured_gate_never_reopens_it()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await CapturedFinalSubmitIntegrationTests.ReadyAsync(target);
        await CapturedFinalSubmitIntegrationTests.SeedLegacyGroupDraftAsync(db);

        // همهٔ کلیدها خاموش: همان اعتبارسنجی قدیمی اجرا می‌شود و با قاعدهٔ خودش (نام کوچک) می‌ایستد.
        using (var off = new EdgeHarness(db, capturedEnabled: false))
        {
            var legacy = await Assert.ThrowsAsync<StorefrontOrderException>(() => off.Shipping.CommitAsync(
                CartId,
                null,
                CartVersion,
                "tb234-legacy-off",
                couponCode: null,
                expectedSelectionRevision: null,
                CancellationToken.None));
            Assert.Equal(StorefrontOrderErrors.ShippingFirstNameRequired, legacy.Code);
            Assert.Equal(0, off.Core.Reservation.Calls);
            Assert.Equal(0, off.Core.Pointers.Calls);
            Assert.Equal(0, off.Core.Addresses.Calls);
        }

        // پیش‌نمایش روشن + پیش‌نویس بدون شاهد ذخیره‌شده ⇒ همان رد پایدار؛ هرگز مسیر قدیمی.
        using (var on = new EdgeHarness(db))
        {
            var refused = await Assert.ThrowsAsync<StorefrontOrderException>(() => on.Shipping.CommitAsync(
                CartId,
                null,
                CartVersion,
                "tb234-legacy-on",
                couponCode: null,
                expectedSelectionRevision: null,
                CancellationToken.None));
            Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, refused.Code);
            Assert.Equal(0, on.Core.Reservation.Calls);
        }

        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(1, (await db.ShippingDrafts.SingleAsync(x => x.CartId == CartId)).SelectionRevision);
    }

    /// <summary>
    /// دروازهٔ واقعی ویترین: <see cref="StorefrontShippingService"/>/<see cref="StorefrontCheckoutService"/>/
    /// <see cref="StorefrontStartPaymentComposer"/> با همان دایرکتوری پذیرفته‌شده و همان دانهٔ ۲۳۳.
    /// </summary>
    private sealed class EdgeHarness : IDisposable
    {
        internal readonly CapturedFinalSubmitIntegrationTests.CapturedSubmitHarness Core;
        internal readonly StorefrontShippingDraftStore Drafts;
        internal readonly StorefrontShippingService Shipping;
        internal readonly StorefrontStartPaymentComposer Composer;
        internal readonly CapturedFinalSubmitIntegrationTests.FixedStorefrontActor Actor = new();
        internal readonly RecordingPreparation Preparation = new();
        internal readonly RecordingPayments Payments = new();
        private readonly EdgeCartPresentation _carts = new();

        internal EdgeHarness(
            OrderDbContext db,
            bool checkoutEnabled = true,
            bool foreignActor = false,
            bool capturedEnabled = true)
        {
            Core = new CapturedFinalSubmitIntegrationTests.CapturedSubmitHarness(db, checkoutEnabled: checkoutEnabled);
            Drafts = new StorefrontShippingDraftStore(db);
            var options = Options.Create(new StorefrontCapturedPreviewOptions
            {
                Enabled = capturedEnabled,
                SelectionEnabled = capturedEnabled,
                CheckoutEnabled = capturedEnabled && checkoutEnabled,
                AvailableDateCount = capturedEnabled ? 7 : null,
            });
            var planner = new StorefrontCapturedPreviewPlanner(
                Core.Assignments,
                Core.ShippingCatalog,
                Core.Types,
                Core.Pointers,
                new StorefrontCapturedGroupProjectionComposer(Core.Directory, Core.Quotes, new FixedClock()),
                new CapturedFinalSubmitIntegrationTests.FakeEdition(),
                new CapturedFinalSubmitIntegrationTests.FakeTenant(),
                options);
            var writer = new StorefrontCapturedSelectionWriter(planner, Drafts, Core.Geography, new FixedClock());
            var actorSeam = foreignActor ? new ForeignActor() : (IOrderStorefrontActor)Actor;
            var checkouts = new StorefrontCheckoutService(_carts, Core.Directory, Core.Addresses, actorSeam);
            Shipping = new StorefrontShippingService(
                _carts,
                checkouts,
                Core.Addresses,
                Core.ShippingCatalog,
                null!,
                Drafts,
                null!,
                new ShippingMethodsOptions(),
                null!,
                actorSeam,
                new FixedClock(),
                capturedPreview: planner,
                capturedSelection: writer);
            Composer = new StorefrontStartPaymentComposer(
                Shipping,
                checkouts,
                Preparation,
                Payments,
                new FixedProviderSelection());
        }

        /// <summary>سبد همین لحظه دیگر Active نیست (Convert شده) — بازگشت عمومی نباید به آن وابسته باشد.</summary>
        internal void MarkCartConverted() =>
            Core.Carts.Cart = Core.Carts.Cart! with { Status = CartStatus.Converted };

        public void Dispose() => Core.Dispose();
    }

    /// <summary>
    /// همان دو خط واقعی سبد برای خواندن دسترسی/ارز/نسخه؛ دادهٔ مالی همچنان از دایرکتوری authoritative می‌آید.
    /// </summary>
    private sealed class EdgeCartPresentation : ICartPresentationGateway
    {
        public Task<CartPage?> GetAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult<CartPage?>(new CartPage(
                cartId,
                CartVersion,
                "IR",
                "IRR",
                "Direct",
                2m,
                [new CartCurrencyTotal("IRR", 300m)],
                [
                    new CartLineView(
                        CapturedFinalSubmitIntegrationTests.LineA,
                        CapturedFinalSubmitIntegrationTests.OfferA,
                        CapturedFinalSubmitIntegrationTests.VariantOf(CapturedFinalSubmitIntegrationTests.OfferA),
                        CapturedFinalSubmitIntegrationTests.SellerA,
                        null,
                        null,
                        "کالای الف",
                        "فروشندهٔ الف",
                        null,
                        1m,
                        100m,
                        100m,
                        "IRR",
                        true),
                    new CartLineView(
                        CapturedFinalSubmitIntegrationTests.LineB,
                        CapturedFinalSubmitIntegrationTests.OfferB,
                        CapturedFinalSubmitIntegrationTests.VariantOf(CapturedFinalSubmitIntegrationTests.OfferB),
                        CapturedFinalSubmitIntegrationTests.SellerB,
                        null,
                        null,
                        "کالای ب",
                        "فروشندهٔ ب",
                        null,
                        1m,
                        200m,
                        200m,
                        "IRR",
                        true),
                ],
                null));

        public Task<CartPage?> TryGetForOwnershipAsync(
            Guid cartId,
            string? guestSecret,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FixedClock : Tooba.BuildingBlocks.IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class ForeignActor : IOrderStorefrontActor
    {
        private static readonly Guid Other = Guid.Parse("01a17cf8-1111-7000-8000-000000000001");

        public Guid GuestActorId => Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId;

        public bool IsAuthenticated => true;

        public Guid? AuthenticatedUserId => Other;

        public Guid ResolvePlacementActor(bool usingSavedAddress) => Other;

        public Guid? TryResolveListActor() => Other;

        public CartAccess BuildCartAccess(string? guestSecret) => new(Other, guestSecret);
    }

    private sealed class FixedProviderSelection : IPaymentProviderSelectionPort
    {
        public Task<StorefrontProviderSelection?> ResolveAsync(
            string? requestedProviderCode,
            bool useWallet,
            CancellationToken cancellationToken) =>
            Task.FromResult<StorefrontProviderSelection?>(
                new StorefrontProviderSelection(requestedProviderCode ?? "manual", "manual"));
    }

    /// <summary>آماده‌سازی کنترل‌شده: بعد از سفارش واقعی صادقانه متوقف می‌شود (پرداخت آغاز نشده).</summary>
    private sealed class RecordingPreparation : IOrderPaymentHoldPreparationPort
    {
        internal OrderPaymentHoldPreparationRequest? Last { get; private set; }

        public Task<OrderPaymentHoldPreparationResult> PrepareAsync(
            OrderPaymentHoldPreparationRequest request,
            CancellationToken cancellationToken)
        {
            Last = request;
            return Task.FromResult(new OrderPaymentHoldPreparationResult(
                OrderPaymentHoldPreparationOutcome.HoldUnavailable,
                request.CheckoutId,
                request.ProviderCode,
                ReservationCount: 0,
                RequestedDeadlineUtc: null,
                GuaranteedUntilUtc: null,
                CycleNumber: null,
                CycleExpiresAtUtc: null,
                UnavailableReservationIds: []));
        }
    }

    private sealed class RecordingPayments : IPaymentInitiationPort
    {
        internal int Calls { get; private set; }

        public Task<StorefrontPaymentInitiationSnapshot> InitiateForCheckoutAsync(
            StorefrontPaymentInitiationRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("payment must not be initiated in this proof");
        }
    }
}
