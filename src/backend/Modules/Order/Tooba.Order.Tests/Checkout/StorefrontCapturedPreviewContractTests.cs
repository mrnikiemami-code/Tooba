using Tooba.AddressBook.Contracts.Dtos;
using Tooba.AddressBook.Contracts.Ports;
using Tooba.Cart.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Application.Storefront.Services;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-235: قرارداد <b>پاسخ</b> ویترین برای یک انتخاب capture‌شده، مستقل از دیتابیس.
/// </summary>
/// <remarks>
/// <para>
/// این آزمون <b>هیچ</b> ادعای پذیرش/ثبت نمی‌کند؛ فقط قرارداد پاسخ را می‌سنجد: صفحهٔ پیش‌نویس پرداخت
/// سبد/عنوان/نسخهٔ واقعی و مبالغ <b>مشتق از گروه‌های پذیرفته‌شده</b> را نگه می‌دارد، نسخهٔ واقعی ریشه را
/// به‌شکل متن برمی‌گرداند، و ثبت نهایی هرگز <c>ShippingGroups</c> غایبِ snapshot پایدارشده را از
/// پلن‌های <b>فعلی</b> پیش‌نویس پر نمی‌کند.
/// </para>
/// <para>
/// درز <see cref="ICheckoutDirectory"/> یک fake صریح است؛ خود منطق نگاشت در
/// <see cref="StorefrontCheckoutService"/> واقعی اجرا می‌شود.
/// </para>
/// </remarks>
public sealed class StorefrontCapturedPreviewContractTests
{
    private static readonly Guid CartId = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b");
    private static readonly Guid LineA = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a6c");
    private static readonly Guid LineB = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a6d");
    private static readonly Guid OfferA = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a6e");
    private static readonly Guid OfferB = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a6f");
    private static readonly Guid SellerA = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a70");
    private static readonly Guid SellerB = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a71");
    private static readonly Guid GroupA = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a72");
    private static readonly Guid GroupB = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a73");
    private static readonly Guid OrderLineOfA = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a74");
    private static readonly Guid OrderLineOfB = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a75");
    private static readonly Guid CheckoutId = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a80");
    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-10-27T06:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public async Task Payment_draft_keeps_the_real_cart_and_derives_its_receipt_from_the_accepted_groups()
    {
        var directory = new DirectoryFake
        {
            QuoteSnapshot = Quote(shippingAmount: 200m),
            Groups =
            [
                Plan(GroupA, SellerA, 125m, [LineA]),
                Plan(GroupB, SellerB, 75m, [LineB]),
            ],
            SelectionRevision = "4294967296",
        };
        var service = Service(directory);

        // TB-235-R1: صفحهٔ سبد مجاز همان‌بار در مسیر عمومی خوانده می‌شود و همان شیء به نمای داخلی می‌رسد.
        var cart = await new CartFake().GetAsync(CartId, guestSecret: null, CancellationToken.None);
        var page = await service.ProjectCapturedPaymentDraftAsync(
            cart!,
            guestSecret: null,
            Draft(),
            couponCode: null,
            expectedSelectionRevision: null,
            CancellationToken.None);

        Assert.Null(page.CheckoutId);
        // سبد/عنوان/نسخهٔ واقعی، نه صفحهٔ ساختگی.
        Assert.Equal(7, page.CartVersion);
        Assert.Equal("کالای الف", page.SellerOrders[0].Lines[0].Title);
        Assert.Equal("فروشندهٔ ب", page.SellerOrders[1].Lines[0].SellerDisplayName);
        // نسخهٔ ریشه به‌شکل متن دقیق (بزرگ‌تر از uint) برای CAS بعدی.
        Assert.Equal("4294967296", page.SelectionRevision);
        // گروه‌های پیش‌نویس عضویت خط سبد دارند و شناسهٔ خط سفارش ندارند.
        Assert.All(page.ShippingGroups!, group => Assert.Null(group.OrderLineIds));
        Assert.Equal([LineA, LineB], page.ShippingGroups!.SelectMany(group => group.CartLineIds!).ToArray());
        // مبلغ مالی نمای پیش‌نویس همان مقدار مشتق‌شدهٔ پذیرش است (فیلد کهنهٔ فرمان ۹۹۹ نیست).
        Assert.Equal(200m, page.ShippingAmount);
        Assert.Equal(page.SellerOrders.Sum(order => order.PayableAmount) + 200m, page.PayableAmount);
    }

    [Fact]
    public async Task Committed_response_never_fills_missing_snapshot_groups_from_the_current_draft_plans()
    {
        // snapshot پایدارشده هیچ گروهی ندارد؛ پلن‌های فعلی پیش‌نویس وجود دارند اما نباید نمایش داده شوند.
        var directory = new DirectoryFake
        {
            Submitted = Quote(shippingGroups: null),
            Groups = [Plan(GroupA, SellerA, 125m, [LineA])],
        };
        var service = Service(directory);

        var page = await service.SubmitCapturedSelectionAsync(
            CartId,
            guestSecret: null,
            expectedCartVersion: 7,
            idempotencyKey: "tb235-contract",
            Draft(),
            couponCode: null,
            expectedSelectionRevision: "1",
            CancellationToken.None);

        Assert.Empty(page.ShippingGroups ?? []);
    }

    [Fact]
    public async Task Committed_response_exposes_order_line_ids_without_substituting_cart_lines()
    {
        var directory = new DirectoryFake
        {
            Submitted = Quote(shippingGroups:
            [
                CommittedGroup(GroupA, SellerA, 125m, OrderLineOfA),
                CommittedGroup(GroupB, SellerB, 75m, OrderLineOfB),
            ]),
            Groups = [Plan(GroupA, SellerA, 125m, [LineA])],
        };
        var service = Service(directory);

        var page = await service.SubmitCapturedSelectionAsync(
            CartId,
            guestSecret: null,
            expectedCartVersion: 7,
            idempotencyKey: "tb235-contract-lines",
            Draft(),
            couponCode: null,
            expectedSelectionRevision: "1",
            CancellationToken.None);

        Assert.NotNull(page.CheckoutId);
        Assert.Equal(2, page.ShippingGroups!.Count);
        Assert.All(page.ShippingGroups!, group => Assert.Null(group.CartLineIds));
        Assert.Equal([OrderLineOfA], page.ShippingGroups!.Single(x => x.ShippingGroupId == GroupA).OrderLineIds);
        Assert.Equal([OrderLineOfB], page.ShippingGroups!.Single(x => x.ShippingGroupId == GroupB).OrderLineIds);
    }

    private static StorefrontCheckoutService Service(ICheckoutDirectory directory) =>
        new(new CartFake(), directory, new EmptyAddressBook(), new CapturedFinalSubmitIntegrationTests.FixedStorefrontActor(), shippingGroupHistory: null!);

    private static CartShippingDraft Draft() => CartShippingDraft.Create(
        CartId,
        "hash",
        7,
        "گیرنده",
        "09120000000",
        "تهران",
        "تهران",
        "نشانی",
        "1234567890",
        null,
        "post:standard",
        "پست",
        999m,
        new DateOnly(2026, 10, 26),
        new DateOnly(2026, 10, 27),
        "09:00-17:00",
        null,
        Now);

    private static OrderShippingGroupPlan Plan(Guid groupId, Guid seller, decimal payable, Guid[] lines) =>
        new(
            groupId,
            seller,
            "post:standard",
            "post",
            "standard",
            "پست",
            null,
            payable,
            "IRR",
            new DateOnly(2026, 10, 26),
            new DateOnly(2026, 10, 27),
            "09:00-17:00",
            0,
            lines);

    private static CheckoutShippingGroupSnapshot CommittedGroup(
        Guid groupId,
        Guid seller,
        decimal amount,
        Guid orderLineId) =>
        new(
            groupId,
            Guid.NewGuid(),
            seller,
            "post:standard",
            "post",
            "standard",
            "پست",
            amount,
            "IRR",
            new DateOnly(2026, 10, 26),
            new DateOnly(2026, 10, 27),
            "09:00-17:00",
            0,
            "Accepted",
            [orderLineId]);

    private static CheckoutSnapshot Quote(
        decimal shippingAmount = 0m,
        IReadOnlyList<CheckoutShippingGroupSnapshot>? shippingGroups = null) =>
        new(
            CheckoutId,
            CartId,
            OrderMode.OnlinePurchase,
            BuyerPartyId: null,
            PlacedByUserId: new CapturedFinalSubmitIntegrationTests.FixedStorefrontActor().AuthenticatedUserId!.Value,
            Market: "IR",
            Currency: "IRR",
            Channel: SalesChannel.Direct,
            SubmittedAt: Now,
            SellerOrders:
            [
                SellerOrder(Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a86"), SellerA, OfferA, LineA, 100m),
                SellerOrder(Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a87"), SellerB, OfferB, LineB, 200m),
            ],
            ShippingAmount: shippingAmount,
            ShippingGroups: shippingGroups);

    private static SellerOrderSnapshot SellerOrder(
        Guid sellerOrderId,
        Guid seller,
        Guid offerId,
        Guid lineId,
        decimal amount) =>
        new(
            sellerOrderId,
            "ORD-1",
            seller,
            SellerOrderStatus.PendingPayment,
            amount,
            0m,
            0m,
            amount,
            "IRR",
            [OrderLine(offerId, lineId, seller, amount)]);

    private static OrderLineSnapshot OrderLine(Guid offerId, Guid lineId, Guid seller, decimal amount) =>
        new(
            lineId,
            offerId,
            Guid.Empty,
            seller,
            1m,
            amount,
            amount,
            "IRR",
            true,
            Guid.Empty,
            null,
            "taxable",
            0m,
            0m,
            amount,
            null,
            0m,
            null,
            null,
            null,
            null,
            amount,
            amount,
            null);

    private sealed class DirectoryFake : ICheckoutDirectory
    {
        internal IReadOnlyList<OrderShippingGroupPlan> Groups { get; init; } = [];

        internal string? SelectionRevision { get; init; }

        internal CheckoutSnapshot? Submitted { get; init; }

        internal CheckoutSnapshot? QuoteSnapshot { get; init; }

        public Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(Submitted ?? QuoteOrEmpty);

        public Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("preview must not be used by the captured response contract");

        public Task<CapturedPaymentDraftSnapshot> ProjectCapturedPaymentDraftAsync(
            SubmitCheckoutCommand command,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CapturedPaymentDraftSnapshot(
                QuoteOrEmpty,
                Groups,
                command.ExpectedCartVersion,
                SelectionRevision));

        public Task<CheckoutSnapshot?> GetCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");

        public Task<SellerOrderSnapshot?> GetSellerOrderByNumberAsync(string orderNumber, OrderAccess access, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");

        public Task CancelSellerOrderAsync(Guid sellerOrderId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");

        public Task RestoreCancelledCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");

        public Task<IReadOnlyList<CheckoutOperationalNoteSnapshot>> ListNotesAsync(
            Guid checkoutId,
            Guid viewerUserId,
            int take,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");

        public Task<CheckoutOperationalNoteSnapshot> AddNoteAsync(
            Guid checkoutId,
            Guid actorUserId,
            string body,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");

        public Task<CheckoutNoteDeleteOutcome> DeleteNoteAsync(
            Guid checkoutId,
            Guid noteId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");

        public Task RecordAdminViewAsync(Guid checkoutId, Guid viewerUserId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");

        private CheckoutSnapshot QuoteOrEmpty => QuoteSnapshot ?? StorefrontCapturedPreviewContractTests.Quote();
    }

    private sealed class CartFake : ICartPresentationGateway
    {
        public Task<CartPage?> GetAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult<CartPage?>(Page(cartId));

        public Task<CartPage?> TryGetForOwnershipAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult<CartPage?>(Page(cartId));

        private static CartPage Page(Guid cartId) =>
            new(
                cartId,
                7,
                "IR",
                "IRR",
                "Direct",
                2m,
                [new CartCurrencyTotal("IRR", 300m)],
                [
                    new CartLineView(LineA, OfferA, Guid.Empty, SellerA, null, null, "کالای الف", "فروشندهٔ الف", null, 1m, 100m, 100m, "IRR", true),
                    new CartLineView(LineB, OfferB, Guid.Empty, SellerB, null, null, "کالای ب", "فروشندهٔ ب", null, 1m, 200m, 200m, "IRR", true),
                ],
                GuestSecret: null);
    }

    private sealed class EmptyAddressBook : IAddressBookCheckoutLookup
    {
        public Task<CustomerAddressRecord?> GetAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken) =>
            Task.FromResult<CustomerAddressRecord?>(null);
    }
}
