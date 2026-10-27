using Tooba.AddressBook.Contracts.Ports;
using Tooba.AddressBook.Contracts.Dtos;
using Tooba.Cart.Contracts;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Domain;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Storefront.Services;

/// <summary>
/// Order-owned storefront checkout composition (preview/submit/get).
/// </summary>
public sealed class StorefrontCheckoutService
{
    public static readonly Guid StorefrontGuestActorId = Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId;

    private const string DefaultShippingCode = "storefront-default";
    private const string DefaultShippingLabel = "ارسال پیش‌فرض فروشگاه";
    /// <summary>
    /// TB-230: the ONE authoritative storefront tax jurisdiction of this installation. The captured
    /// read preview must quote with the SAME jurisdiction the real submit charges; the cart's market
    /// code is not a jurisdiction key and no rule is keyed by it.
    /// </summary>
    internal const string StorefrontTaxJurisdiction = "IR-NAT";

    private readonly ICartPresentationGateway _carts;
    private readonly ICheckoutDirectory _checkouts;
    private readonly IAddressBookCheckoutLookup _addresses;
    private readonly IOrderStorefrontActor _actor;

    public StorefrontCheckoutService(
        ICartPresentationGateway carts,
        ICheckoutDirectory checkouts,
        IAddressBookCheckoutLookup addresses,
        IOrderStorefrontActor actor)
    {
        _carts = carts;
        _checkouts = checkouts;
        _addresses = addresses;
        _actor = actor;
    }

    /// <summary>
    /// ارزیابی تجاری سبد فعال بدون ماندگاری CheckoutGroup. وقتی انتخاب ارسال ذخیره‌شده داده شود،
    /// همان مقادیر backend-authoritative روی quote اعمال می‌شوند تا «پیش‌نویس پرداخت» قبل از سفارش ساخته شود.
    /// فراخوانی بدون پارامترهای اختیاری، رفتار قبلی را بدون تغییر نگه می‌دارد.
    /// </summary>
    public async Task<StorefrontCheckoutPage> PreviewAsync(
        Guid cartId,
        string? guestSecret,
        string? couponCode,
        CancellationToken cancellationToken,
        StorefrontCheckoutShippingInput? shipping = null,
        string? shippingMethodCode = null,
        string? shippingMethodLabel = null,
        decimal? shippingAmount = null,
        DateOnly? minimumDeliveryDate = null,
        DateOnly? requestedDeliveryDate = null,
        string? requestedDeliveryTimeWindow = null,
        string? customerNote = null,
        IReadOnlyList<OrderShippingGroupPlan>? shippingGroups = null)
    {
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        var quoted = await _checkouts.PreviewAsync(
            BuildCommand(
                cart,
                guestSecret,
                "preview",
                _actor.GuestActorId,
                shipping: shipping,
                couponCode: couponCode,
                shippingMethodCode: shippingMethodCode,
                shippingMethodLabel: shippingMethodLabel,
                shippingAmount: shippingAmount,
                minimumDeliveryDate: minimumDeliveryDate,
                requestedDeliveryDate: requestedDeliveryDate,
                requestedDeliveryTimeWindow: requestedDeliveryTimeWindow,
                customerNote: customerNote),
            cancellationToken);
        return MapPage(quoted, cart, persisted: false, shippingGroups);
    }

    public async Task<StorefrontCheckoutPage> SubmitAsync(
        Guid cartId,
        string? guestSecret,
        int expectedVersion,
        string idempotencyKey,
        StorefrontCheckoutShippingInput shipping,
        string? couponCode,
        CancellationToken cancellationToken,
        string? shippingMethodCode = null,
        string? shippingMethodLabel = null,
        decimal? shippingAmount = null,
        DateOnly? minimumDeliveryDate = null,
        DateOnly? requestedDeliveryDate = null,
        string? requestedDeliveryTimeWindow = null,
        string? customerNote = null,
        IReadOnlyList<OrderShippingGroupPlan>? shippingGroups = null,
        string? expectedSelectionRevision = null)
    {
        var prepared = await PrepareShippingAsync(shipping, cancellationToken);
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        if (cart.Version != expectedVersion)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutVersionConflict);
        }

        var submitted = await _checkouts.SubmitAsync(
            BuildCommand(
                cart,
                guestSecret,
                idempotencyKey,
                prepared.PlacedByUserId,
                prepared.Shipping,
                couponCode,
                shippingMethodCode,
                shippingMethodLabel,
                shippingAmount,
                minimumDeliveryDate,
                requestedDeliveryDate,
                requestedDeliveryTimeWindow,
                customerNote,
                shippingGroups,
                expectedSelectionRevision),
            cancellationToken);
        return MapPage(submitted, cart, persisted: true, shippingGroups);
    }

    public async Task<StorefrontCheckoutPage?> GetAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var owned = await GetOwnedForPaymentResultAsync(checkoutId, guestSecret, cancellationToken);
        if (owned is not null)
        {
            return owned;
        }

        _ = cartId;
        var actor = _actor.ResolvePlacementActor(usingSavedAddress: false);
        var snapshot = await _checkouts.GetCheckoutAsync(
            checkoutId,
            new OrderAccess(null, actor),
            cancellationToken);
        if (snapshot is not null)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutAccessDenied);
        }

        return null;
    }

    public async Task<StorefrontCheckoutPage?> GetOwnedForPaymentResultAsync(
        Guid checkoutId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var actor = _actor.ResolvePlacementActor(usingSavedAddress: false);
        var snapshot = await _checkouts.GetCheckoutAsync(
            checkoutId,
            new OrderAccess(null, actor),
            cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        if (_actor.IsAuthenticated && _actor.AuthenticatedUserId is Guid userId && userId != Guid.Empty)
        {
            return MapPage(snapshot, StubCartPage(snapshot), persisted: true);
        }

        if (string.IsNullOrWhiteSpace(guestSecret))
        {
            return null;
        }

        var committedCart = await _carts.TryGetForOwnershipAsync(snapshot.CartId, guestSecret, cancellationToken);
        if (committedCart is null)
        {
            return null;
        }

        return MapPage(snapshot, committedCart, persisted: true);
    }

    private static CartPage StubCartPage(CheckoutSnapshot snapshot) =>
        new(
            snapshot.CartId,
            0,
            snapshot.Market,
            snapshot.Currency,
            snapshot.Channel.ToString(),
            0,
            Array.Empty<CartCurrencyTotal>(),
            Array.Empty<CartLineView>(),
            null,
            "Converted");

    public async Task<StorefrontCheckoutPlacement> PrepareShippingAsync(
        StorefrontCheckoutShippingInput shipping,
        CancellationToken cancellationToken)
    {
        var usingSaved = shipping.SavedAddressId is Guid savedId && savedId != Guid.Empty;
        var actor = _actor.ResolvePlacementActor(usingSaved);
        if (!usingSaved)
        {
            ValidateShipping(shipping);
            return new StorefrontCheckoutPlacement(actor, shipping);
        }

        var saved = await _addresses.GetAsync(actor, shipping.SavedAddressId!.Value, cancellationToken);
        if (saved is null)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutAddressForbidden);
        }

        var names = StorefrontRecipientNames.ResolveExplicitOverLegacy(
            shipping.FirstName,
            shipping.LastName,
            shipping.RecipientName,
            saved.FirstName,
            saved.LastName,
            saved.RecipientName);
        var snapshot = new StorefrontCheckoutShippingInput(
            names.Recipient,
            saved.ContactMobile,
            saved.ProvinceName ?? string.Empty,
            saved.CityName,
            saved.PostalAddress,
            saved.PostalCode,
            null,
            names.First,
            names.Last);
        // منشأ شناسه فقط-سرور؛ تصویر آماده‌شده همان معنای قدیمی (SavedAddressId تهی) را نگه می‌دارد.
        return new StorefrontCheckoutPlacement(actor, snapshot, shipping.SavedAddressId);
    }

    /// <summary>
    /// ثبت نهایی یک انتخاب <b>capture‌شده</b> روی همان دامنهٔ موجود checkout (TB-234).
    /// </summary>
    /// <remarks>
    /// <para>
    /// این عضو عمداً <b>مقدمهٔ قدیمی</b> (<see cref="PrepareShippingAsync"/> + بررسی نسخهٔ سبد جاری) را
    /// اجرا نمی‌کند: آن مقدمه پیش از رسیدن به idempotency، سبد/نشانی را می‌خواند و یک سبد تبدیل‌شده یا
    /// نشانی حذف‌شده می‌توانست بازگشتِ سفارشِ <b>همین قبل پایدارشده</b> را بشکند. فرمان فقط از
    /// <b>انتخاب ذخیره‌شدهٔ سرور</b> + هویت مرز اعتماد ساخته می‌شود و به مرجعِ موجود
    /// <c>ICheckoutDirectory.SubmitAsync</c> سپرده می‌شود که اول idempotency/دسترسی را می‌سنجد و برای
    /// ثبت <b>تازه</b> همان بازاعتبارسنجی کامل ۲۳۳ را داخل تراکنش اجرا می‌کند. هیچ هماهنگ‌کنندهٔ دوم و
    /// هیچ snapshot ساختگی از سبد ساخته نمی‌شود.
    /// </para>
    /// <para>
    /// هیچ مالک/مبلغ/شاهد/تاریخی از مرورگر خوانده نمی‌شود؛ گروه‌ها فقط انتقال snapshot ذخیره‌شده‌اند و
    /// تصمیم نهایی با پذیرش ۲۳۳ است. خطای خارجی پس از ثبوت سفارش ساخته نمی‌شود.
    /// </para>
    /// </remarks>
    /// <param name="cartId">سبد همین درخواست.</param>
    /// <param name="guestSecret">گواه مالکیت سبد مهمان.</param>
    /// <param name="expectedCartVersion">نسخهٔ سبدی که فراخوان دیده است.</param>
    /// <param name="idempotencyKey">کلید پایدار همین ارسال.</param>
    /// <param name="draft">ریشهٔ ذخیره‌شدهٔ انتخاب (نه بدنهٔ فراخوان).</param>
    /// <param name="couponCode">کوپن؛ خالی یعنی نبود (کوپن در این برش پشتیبانی نمی‌شود).</param>
    /// <param name="expectedSelectionRevision">متن دقیق نسخهٔ انتخاب.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public async Task<StorefrontCheckoutPage> SubmitCapturedSelectionAsync(
        Guid cartId,
        string? guestSecret,
        int expectedCartVersion,
        string idempotencyKey,
        CartShippingDraft draft,
        string? couponCode,
        string? expectedSelectionRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (draft.CartId != cartId)
        {
            // پیش‌نویس سبد دیگری هرگز مبنای این فرمان نمی‌شود.
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingSelectionRequired);
        }

        var usingSaved = draft.SavedAddressId is Guid savedId && savedId != Guid.Empty;
        var actor = _actor.ResolvePlacementActor(usingSaved);
        var command = new SubmitCheckoutCommand(
            cartId,
            _actor.BuildCartAccess(guestSecret),
            expectedCartVersion,
            OrderMode.OnlinePurchase,
            BuyerPartyId: null,
            PlacedByUserId: actor,
            IdempotencyKey: idempotencyKey,
            TaxJurisdiction: StorefrontTaxJurisdiction,
            CouponCode: string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim(),
            QuotedDiscountAmount: null,
            RecipientName: draft.RecipientName,
            ContactMobile: draft.ContactMobile,
            ProvinceName: draft.ProvinceName,
            CityName: draft.CityName,
            PostalAddress: draft.PostalAddress,
            PostalCode: draft.PostalCode,
            ShippingMethodCode: string.IsNullOrWhiteSpace(draft.ShippingMethodCode)
                ? DefaultShippingCode
                : draft.ShippingMethodCode.Trim().ToLowerInvariant(),
            ShippingMethodLabel: string.IsNullOrWhiteSpace(draft.ShippingMethodLabel)
                ? DefaultShippingLabel
                : draft.ShippingMethodLabel.Trim(),
            ShippingAmount: draft.ShippingAmount,
            MinimumDeliveryDate: draft.MinimumDeliveryDate,
            RequestedDeliveryDate: draft.SelectedDeliveryDate,
            RequestedDeliveryTimeWindow: draft.SelectedDeliveryTimeWindow ?? string.Empty,
            CustomerNote: draft.CustomerNote ?? string.Empty,
            RecipientFirstName: draft.FirstName,
            RecipientLastName: draft.LastName,
            ShippingGroups: StorefrontShippingService.BuildOrderGroupPlans(draft),
            ExpectedSelectionRevision: expectedSelectionRevision);

        var submitted = await _checkouts.SubmitAsync(command, cancellationToken);

        // پس از تبدیل سبد، سبد فعالی برای نمایش عنوان‌ها نمی‌ماند؛ نمای پایدار از snapshot سفارش می‌آید و
        // هیچ خواندن بیرونی پس از ثبوت انجام نمی‌شود.
        return MapCapturedPage(submitted, StubCartPage(submitted));
    }

    /// <summary>
    /// «پیش‌نویس پرداخت» یک انتخاب capture‌شده: همان یک گذر مالی + همان بازاعتبارسنجی ۲۳۳، بدون هیچ نوشتن.
    /// </summary>
    /// <param name="cartId">سبد همین درخواست.</param>
    /// <param name="guestSecret">گواه مالکیت سبد مهمان.</param>
    /// <param name="draft">ریشهٔ ذخیره‌شدهٔ انتخاب.</param>
    /// <param name="couponCode">کوپن؛ خالی یعنی نبود.</param>
    /// <param name="expectedSelectionRevision">متن دقیق نسخهٔ انتخاب.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public async Task<StorefrontCheckoutPage> ProjectCapturedPaymentDraftAsync(
        Guid cartId,
        string? guestSecret,
        CartShippingDraft draft,
        string? couponCode,
        string? expectedSelectionRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (draft.CartId != cartId)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingSelectionRequired);
        }

        // سبد <b>واقعی</b> همین درخواست: عنوان‌ها/فروشنده‌ها/نسخه از همان سبد authoritative می‌آید تا
        // نمای پیش‌نویس پرداخت هرگز به صفحهٔ ساختگی یا نسخهٔ کهنهٔ پیش‌نویس برنگردد.
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);

        var usingSaved = draft.SavedAddressId is Guid savedId && savedId != Guid.Empty;
        var actor = _actor.ResolvePlacementActor(usingSaved);
        var command = new SubmitCheckoutCommand(
            cartId,
            _actor.BuildCartAccess(guestSecret),
            cart.Version,
            OrderMode.OnlinePurchase,
            BuyerPartyId: null,
            PlacedByUserId: actor,
            IdempotencyKey: "payment-draft",
            TaxJurisdiction: StorefrontTaxJurisdiction,
            CouponCode: string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim(),
            QuotedDiscountAmount: null,
            RecipientName: draft.RecipientName,
            ContactMobile: draft.ContactMobile,
            ProvinceName: draft.ProvinceName,
            CityName: draft.CityName,
            PostalAddress: draft.PostalAddress,
            PostalCode: draft.PostalCode,
            ShippingMethodCode: string.IsNullOrWhiteSpace(draft.ShippingMethodCode)
                ? DefaultShippingCode
                : draft.ShippingMethodCode.Trim().ToLowerInvariant(),
            ShippingMethodLabel: string.IsNullOrWhiteSpace(draft.ShippingMethodLabel)
                ? DefaultShippingLabel
                : draft.ShippingMethodLabel.Trim(),
            ShippingAmount: draft.ShippingAmount,
            MinimumDeliveryDate: draft.MinimumDeliveryDate,
            RequestedDeliveryDate: draft.SelectedDeliveryDate,
            RequestedDeliveryTimeWindow: draft.SelectedDeliveryTimeWindow ?? string.Empty,
            CustomerNote: draft.CustomerNote ?? string.Empty,
            RecipientFirstName: draft.FirstName,
            RecipientLastName: draft.LastName,
            ShippingGroups: StorefrontShippingService.BuildOrderGroupPlans(draft),
            ExpectedSelectionRevision: expectedSelectionRevision);

        var projected = await _checkouts.ProjectCapturedPaymentDraftAsync(command, cancellationToken);
        return MapPage(
            projected.Quote,
            // سبد واقعیِ همین درخواست (عنوان‌ها/فروشنده‌ها/نسخهٔ واقعی) — نه یک صفحهٔ ساختگی.
            cart,
            persisted: false,
            groupViews: projected.Groups.Select(MapGroupPlan).ToArray(),
            selectionRevision: projected.SelectionRevision);
    }

    /// <summary>
    /// نمای پاسخ برای یک ثبت capture‌شده: گروه‌های <b>پایدارشده</b> از snapshot همان سفارش، و در نبودشان
    /// <b>هیچ</b> گروهی (نه پلن‌های فعلی پیش‌نویس). هیچ خواندن بیرونی پس از ثبوت انجام نمی‌شود.
    /// </summary>
    /// <remarks>
    /// فقدان <c>ShippingGroups</c> در یک snapshot ذخیره‌شده یعنی «گروهی ثبت نشده است»، نه مجوز پرکردن آن
    /// از پیش‌نویس جاری: پلن‌های امروز می‌توانند مبلغ/روش/تاریخ دیگری داشته باشند و نمایش آن‌ها به‌جای
    /// snapshot یک سفارشِ پایدار، یک تاریخچهٔ نادرست می‌سازد.
    /// </remarks>
    private static StorefrontCheckoutPage MapCapturedPage(CheckoutSnapshot snapshot, CartPage cart)
    {
        var committed = snapshot.ShippingGroups;
        var groupViews = committed is { Count: > 0 }
            ? committed
                .OrderBy(group => group.ShippingGroupId)
                .Select(MapCommittedGroup)
                .ToArray()
            : (IReadOnlyList<StorefrontShippingGroupView>?)null;
        return MapPage(
            snapshot,
            cart,
            persisted: true,
            groupViews: groupViews);
    }

    /// <summary>گروه ارسال <b>پایدارشده</b> → نمای ویترین (فقط انتقال مقدار snapshot).</summary>
    private static StorefrontShippingGroupView MapCommittedGroup(CheckoutShippingGroupSnapshot group) =>
        new(
            group.ShippingGroupId,
            group.SellerPartyId,
            group.MethodCode,
            group.ServiceCode,
            group.OptionCode,
            group.MethodLabel,
            group.ShippingChargeSnapshot,
            group.Currency,
            group.MinimumDeliveryDate.ToString("yyyy-MM-dd"),
            group.RequestedDeliveryDate?.ToString("yyyy-MM-dd"),
            group.RequestedDeliveryTimeWindow,
            group.LeadDays,
            group.OrderLineIds.Count,
            // گروه پایدارشده عضویت <b>خط سبد</b> ندارد؛ شناسهٔ خطوط سفارش جداگانه و بدون جانشینی می‌آید.
            CartLineIds: null,
            OrderLineIds: group.OrderLineIds.ToArray());

    private async Task<CartPage> RequireCartAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var cart = await _carts.GetAsync(cartId, guestSecret, cancellationToken)
            ?? throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutCartMissing);
        if (cart.Lines.Count == 0)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutCartEmpty);
        }

        // Order boundary: sole line currency is the only transaction currency; mixed/missing fails closed.
        _ = StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart);
        return cart;
    }

    private SubmitCheckoutCommand BuildCommand(
        CartPage cart,
        string? guestSecret,
        string idempotencyKey,
        Guid placedByUserId,
        StorefrontCheckoutShippingInput? shipping = null,
        string? couponCode = null,
        string? shippingMethodCode = null,
        string? shippingMethodLabel = null,
        decimal? shippingAmount = null,
        DateOnly? minimumDeliveryDate = null,
        DateOnly? requestedDeliveryDate = null,
        string? requestedDeliveryTimeWindow = null,
        string? customerNote = null,
        IReadOnlyList<OrderShippingGroupPlan>? shippingGroups = null,
        string? expectedSelectionRevision = null) =>
        new(
            cart.CartId,
            _actor.BuildCartAccess(guestSecret),
            cart.Version,
            OrderMode.OnlinePurchase,
            null,
            placedByUserId,
            idempotencyKey,
            StorefrontTaxJurisdiction,
            string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim(),
            null,
            shipping?.RecipientName ?? string.Empty,
            shipping?.ContactMobile ?? string.Empty,
            shipping?.ProvinceName ?? string.Empty,
            shipping?.CityName ?? string.Empty,
            shipping?.PostalAddress ?? string.Empty,
            shipping?.PostalCode ?? string.Empty,
            string.IsNullOrWhiteSpace(shippingMethodCode) ? DefaultShippingCode : shippingMethodCode.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(shippingMethodLabel) ? DefaultShippingLabel : shippingMethodLabel.Trim(),
            shippingAmount ?? 0m,
            minimumDeliveryDate,
            requestedDeliveryDate,
            requestedDeliveryTimeWindow ?? string.Empty,
            customerNote ?? string.Empty,
            shipping?.FirstName ?? string.Empty,
            shipping?.LastName ?? string.Empty,
            shippingGroups,
            expectedSelectionRevision);

    private static StorefrontCheckoutPage MapPage(
        CheckoutSnapshot snapshot,
        CartPage cart,
        bool persisted,
        IReadOnlyList<OrderShippingGroupPlan>? shippingGroups = null,
        IReadOnlyList<StorefrontShippingGroupView>? groupViews = null,
        string? selectionRevision = null)
    {
        // یک آفر می‌تواند روی دو خط سبد تکرار شود؛ عنوان/فروشندهٔ نمایشی کلید یکتا لازم دارد و تکرار
        // نباید نمای پرداخت را بشکند (اولین جفت غیرتهی همان آفر، وگرنه اولین خط).
        var titleByOffer = cart.Lines
            .GroupBy(line => line.OfferId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var named = group
                        .Select(line => (line.Title, line.SellerDisplayName))
                        .FirstOrDefault(pair => !string.IsNullOrWhiteSpace(pair.Title) && !string.IsNullOrWhiteSpace(pair.SellerDisplayName));
                    return string.IsNullOrWhiteSpace(named.Title)
                        ? (group.First().Title, group.First().SellerDisplayName)
                        : named;
                });
        var sellers = snapshot.SellerOrders.Select(order =>
        {
            var sellerName = order.Lines
                .Select(line => titleByOffer.GetValueOrDefault(line.OfferId).SellerDisplayName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
                ?? "فروشنده";
            var lines = order.Lines.Select(line =>
            {
                var names = titleByOffer.GetValueOrDefault(line.OfferId);
                return new StorefrontCheckoutLineView(
                    line.OfferId,
                    line.SellerPartyId,
                    string.IsNullOrWhiteSpace(names.Title) ? "کالا" : names.Title,
                    string.IsNullOrWhiteSpace(names.SellerDisplayName) ? sellerName : names.SellerDisplayName,
                    line.Quantity,
                    line.LineTotalSnapshot,
                    line.DiscountAmountSnapshot,
                    line.TaxAmountSnapshot,
                    line.TaxInclusiveSnapshot,
                    line.Currency);
            }).ToList();
            return new StorefrontSellerOrderView(
                order.SellerOrderId,
                order.OrderNumber,
                order.SellerPartyId,
                sellerName,
                order.Status.ToString(),
                order.SubtotalSnapshot,
                order.TaxSnapshot,
                order.DiscountSnapshot,
                order.GrandTotalSnapshot,
                order.Currency,
                lines);
        }).ToList();

        var paymentState = sellers.Count > 0 && sellers.All(x => string.Equals(x.Status, "Paid", StringComparison.Ordinal))
            ? "Paid"
            : "PendingPayment";
        // A genuine multi-group checkout has no single applicable method: never present one arbitrary
        // method (or the legacy default) as if it covered every line.
        var multiGroup = shippingGroups is { Count: > 1 } || groupViews is { Count: > 1 };
        var methodCode = multiGroup
            ? string.Empty
            : string.IsNullOrWhiteSpace(snapshot.ShippingMethodCode) ? DefaultShippingCode : snapshot.ShippingMethodCode;
        var methodLabel = multiGroup
            ? string.Empty
            : string.IsNullOrWhiteSpace(snapshot.ShippingMethodLabel) ? DefaultShippingLabel : snapshot.ShippingMethodLabel;
        return new StorefrontCheckoutPage(
            persisted ? snapshot.CheckoutId : null,
            snapshot.CartId,
            cart.Version,
            snapshot.Market,
            snapshot.Currency,
            snapshot.Channel.ToString(),
            paymentState,
            methodCode,
            methodLabel,
            StorefrontRecipientNames.Display(snapshot.RecipientFirstName, snapshot.RecipientLastName, snapshot.RecipientName),
            snapshot.ContactMobile,
            snapshot.ProvinceName,
            snapshot.CityName,
            snapshot.PostalAddress,
            snapshot.PostalCode,
            snapshot.RecipientFirstName,
            snapshot.RecipientLastName,
            sellers.Sum(x => x.SubtotalExclusiveOfTax),
            sellers.Sum(x => x.DiscountAmount),
            sellers.Sum(x => x.TaxAmount),
            snapshot.ShippingAmount,
            sellers.Sum(x => x.PayableAmount) + snapshot.ShippingAmount,
            sellers,
            !string.Equals(paymentState, "Paid", StringComparison.Ordinal),
            shippingGroups is { Count: > 0 }
                ? shippingGroups.Select(MapGroupPlan).ToArray()
                : groupViews,
            selectionRevision);
    }

    private static StorefrontShippingGroupView MapGroupPlan(OrderShippingGroupPlan plan) =>
        new(
            plan.ShippingGroupId,
            plan.SellerPartyId,
            plan.MethodCode,
            plan.ServiceCode,
            plan.OptionCode,
            plan.MethodLabel,
            plan.ShippingCharge,
            plan.Currency,
            plan.MinimumDeliveryDate.ToString("yyyy-MM-dd"),
            plan.RequestedDeliveryDate?.ToString("yyyy-MM-dd"),
            plan.RequestedDeliveryTimeWindow,
            plan.LeadDays,
            plan.CartLineIds.Count,
            plan.CartLineIds.ToArray());

    private static void ValidateShipping(StorefrontCheckoutShippingInput shipping)
    {
        if (string.IsNullOrWhiteSpace(shipping.ContactMobile)
            || string.IsNullOrWhiteSpace(shipping.ProvinceName)
            || string.IsNullOrWhiteSpace(shipping.CityName)
            || string.IsNullOrWhiteSpace(shipping.PostalAddress)
            || string.IsNullOrWhiteSpace(shipping.PostalCode))
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutShippingIncomplete);
        }

        var names = StorefrontRecipientNames.Resolve(shipping.FirstName, shipping.LastName, shipping.RecipientName);
        if (names.Recipient.Length == 0)
        {
            StorefrontRecipientNames.EnsureNewAddressNames(names.First, names.Last);
        }
    }
}

/// <summary>نتیجهٔ Resolve هویت ثبت و تصویر ارسال قبل از ماندگاری سفارش.</summary>
/// <param name="PlacedByUserId">هویت واقعی ثبت‌کننده از مرز اعتماد.</param>
/// <param name="Shipping">تصویر ارسالِ آماده‌شده (همان چیزی که قیمت می‌گیرد).</param>
/// <param name="SourceSavedAddressId">
/// منشأ <b>فقط-سرور</b> نشانی ذخیره‌شده: شناسهٔ واقعی آدرس کتاب آدرس که این تصویر از آن ساخته شده است.
/// این فیلد جای <c>Shipping.SavedAddressId</c> را نمی‌گیرد و آن مقدار برای معنای snapshot قدیمی تهی
/// می‌ماند؛ یعنی هیچ مصرف‌کنندهٔ legacy شاهد تازه‌ای نمی‌بیند و هیچ شناسه‌ای حدس زده نمی‌شود.
/// </param>
public sealed record StorefrontCheckoutPlacement(
    Guid PlacedByUserId,
    StorefrontCheckoutShippingInput Shipping,
    Guid? SourceSavedAddressId = null);
