using Tooba.BuildingBlocks;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Contracts.Storefront;
using Tooba.Payment.Contracts.Hold;

namespace Tooba.Order.Application.Checkout.Process;

/// <summary>
/// درز آماده‌سازی پرداخت به‌مالکیت Order (بلوک سازنده؛ هیچ endpoint، PSP، برداشت کیف پول،
/// payment attempt، تبدیل سبد یا نوشتن Order ندارد).
///
/// ترتیب گام‌ها و اینکه هر شکست کجا بسته می‌شود:
/// <list type="number">
/// <item>مالکیت: همان قاعدهٔ موجود <see cref="ICheckoutPaymentAccessReader"/> (actor احراز‌شده،
/// تطبیق cartId و گروهِ مهمان با گواه سبد). شکست = <c>AccessDenied</c> و هیچ رزروی لمس نمی‌شود.</item>
/// <item>استخراج مجموعهٔ رزرو از <b>خطوط پایدار سفارش</b> (هرگز از ورودی فراخوان). خط بدون رزرو،
/// سفارش بدون خط، یا شناسهٔ رزرو مشترک بین دو خط = <c>ReservationSetIncomplete</c>.</item>
/// <item>قابل‌پرداخت‌بودن چرخهٔ رزرو Order با ساعت دیوار (همان
/// <see cref="ReservationCyclePayabilityPolicy.IsActive"/>). شکست = <c>CycleNotPayable</c> و تمدید
/// انجام نمی‌شود.</item>
/// <item>مهلت مطلق از سیاست مؤثر همان روش پرداخت
/// (<see cref="ICommerceHoldPolicySource.ResolveUnpaidTimeoutAt"/>). نبودن منبع سیاست یا روش =
/// <c>PolicyUnavailable</c>؛ هیچ TTL و هیچ کف تازه‌ای ساخته نمی‌شود.</item>
/// <item>تمدید اتمی همه‌یا‌هیچ Inventory برای <b>همهٔ</b> رزروها؛ نتیجهٔ نگATIVE = <c>HoldUnavailable</c>
/// یا <c>DeadlineRejected</c> بدون هیچ تمدید جزئی.</item>
/// <item>بازخوانی چرخهٔ Order: اگر پس از تمدید دیگر قابل پرداخت نباشد، <c>CycleClosedAfterExtension</c>
/// گزارش می‌شود و <b>هرگز</b> <c>Prepared</c>.</item>
/// </list>
///
/// آنچه این درز تضمین می‌کند: در لحظهٔ بازگشت <c>Prepared</c>، مجموعهٔ رزرو سفارش با ساعت دیتابیس
/// اثبات‌شده و تا مهلت درخواستی <b>تمدید</b> شده بود و چرخهٔ Order هم آن لحظه قابل پرداخت بود.
/// آنچه تضمین نمی‌کند: هیچ ACID بین‌ماژولی، هیچ «بعد از این لحظه پول مجاز نیست»، هیچ مالکیت‌سنجی
/// خارج از checkout، و هیچ همراستایی خودکار دو مهلت. پایان‌یافتن مهلت‌ها، بسته‌شدن هم‌زمان چرخه،
/// callback دیرهنگام درگاه و جبران/ساگا مسئولیت گام‌های بعدی (Payment/Order) است.
/// </summary>
public sealed class OrderPaymentHoldPreparationService : IOrderPaymentHoldPreparationPort
{
    private readonly ICheckoutPaymentAccessReader _access;
    private readonly IOrderStorefrontActor _actor;
    private readonly ICheckoutDirectory _checkouts;
    private readonly IReservationCycleDirectory _cycles;
    private readonly IInventoryPaymentHoldExtensionPort _holdExtension;
    private readonly IClock _clock;
    private readonly ICommerceHoldPolicySource? _holdPolicy;

    /// <summary>سرویس را به روزنه‌های مالکیت/سفارش/چرخه/سیاست و درز Inventory وصل می‌کند.</summary>
    /// <param name="access">مرجع موجود اثبات مالکیت checkout و سبد.</param>
    /// <param name="actor">برای شناسهٔ actor مهمان (هنگام خواندن خطوط با همان قاعدهٔ مالکیت).</param>
    /// <param name="checkouts">خواندن خطوط پایدار سفارش.</param>
    /// <param name="cycles">چرخهٔ رزرو Order به‌عنوان مرز قابل‌پرداخت‌بودن Order.</param>
    /// <param name="holdExtension">درز اتمی Inventory (017).</param>
    /// <param name="clock">ساعت Order برای محاسبهٔ مهلت از سیاست.</param>
    /// <param name="holdPolicy">
    /// منبع سیاست مؤثر نگه‌داشت پرداخت. اگر ثبت نشده باشد این درز TTL نمی‌سازد و
    /// <c>PolicyUnavailable</c> برمی‌گرداند.
    /// </param>
    public OrderPaymentHoldPreparationService(
        ICheckoutPaymentAccessReader access,
        IOrderStorefrontActor actor,
        ICheckoutDirectory checkouts,
        IReservationCycleDirectory cycles,
        IInventoryPaymentHoldExtensionPort holdExtension,
        IClock clock,
        ICommerceHoldPolicySource? holdPolicy = null)
    {
        _access = access;
        _actor = actor;
        _checkouts = checkouts;
        _cycles = cycles;
        _holdExtension = holdExtension;
        _clock = clock;
        _holdPolicy = holdPolicy;
    }

    /// <inheritdoc />
    public async Task<OrderPaymentHoldPreparationResult> PrepareAsync(
        OrderPaymentHoldPreparationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var checkoutId = request.CheckoutId;
        var providerCode = request.ProviderCode?.Trim() ?? string.Empty;
        if (checkoutId == Guid.Empty)
        {
            return Fail(OrderPaymentHoldPreparationOutcome.AccessDenied, checkoutId, providerCode);
        }

        // 1) Ownership comes only from the existing Order-owned access rule.
        var owned = await _access.GetForMutationAsync(
            checkoutId,
            request.CartId,
            request.GuestSecret,
            request.AuthenticatedUserId,
            cancellationToken).ConfigureAwait(false);
        if (owned is null)
        {
            return Fail(OrderPaymentHoldPreparationOutcome.AccessDenied, checkoutId, providerCode);
        }

        var actorId = request.AuthenticatedUserId is { } userId && userId != Guid.Empty
            ? userId
            : _actor.GuestActorId;
        var snapshot = await _checkouts
            .GetCheckoutAsync(checkoutId, new OrderAccess(null, actorId), cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
        {
            return Fail(OrderPaymentHoldPreparationOutcome.AccessDenied, checkoutId, providerCode);
        }

        // 2) The reservation set is derived from persisted lines only — never from the caller.
        var lines = snapshot.SellerOrders.SelectMany(order => order.Lines).ToArray();
        var reservationIds = lines
            .Select(line => line.ReservationId)
            .ToArray();
        if (reservationIds.Length == 0
            || reservationIds.Any(id => id is null || id == Guid.Empty))
        {
            return Fail(OrderPaymentHoldPreparationOutcome.ReservationSetIncomplete, checkoutId, providerCode);
        }

        var distinctReservationIds = reservationIds
            .Select(id => id!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();
        if (distinctReservationIds.Length != reservationIds.Length)
        {
            // One reservation bound to two lines is ambiguity, not a second hold.
            return Fail(OrderPaymentHoldPreparationOutcome.ReservationSetIncomplete, checkoutId, providerCode);
        }

        var now = _clock.UtcNow;

        // 3) Order-owned payability, including wall-clock expiration of the active cycle.
        var cycle = await ReadCycleAsync(checkoutId, now, cancellationToken).ConfigureAwait(false);
        if (!ReservationCyclePayabilityPolicy.IsActive(cycle))
        {
            return Fail(OrderPaymentHoldPreparationOutcome.CycleNotPayable, checkoutId, providerCode, cycle: cycle);
        }

        // 4) Effective policy of the selected method; no invented TTL when it cannot be resolved.
        if (providerCode.Length == 0 || _holdPolicy is null)
        {
            return Fail(OrderPaymentHoldPreparationOutcome.PolicyUnavailable, checkoutId, providerCode, cycle: cycle);
        }

        var deadlineUtc = _holdPolicy.ResolveUnpaidTimeoutAt(providerCode, now).ToUniversalTime();

        // 5) Atomic all-or-none Inventory extension for the whole set.
        var extension = await _holdExtension
            .ExtendHeldForPaymentAsync(distinctReservationIds, deadlineUtc, cancellationToken)
            .ConfigureAwait(false);
        switch (extension.Outcome)
        {
            case InventoryPaymentHoldExtensionOutcome.NotHeld:
                return Fail(
                    OrderPaymentHoldPreparationOutcome.HoldUnavailable,
                    checkoutId,
                    providerCode,
                    distinctReservationIds.Length,
                    cycle: cycle,
                    requestDeadline: deadlineUtc,
                    unavailable: extension.UnavailableReservationIds);
            case InventoryPaymentHoldExtensionOutcome.DeadlineInvalid:
                return Fail(
                    OrderPaymentHoldPreparationOutcome.DeadlineRejected,
                    checkoutId,
                    providerCode,
                    distinctReservationIds.Length,
                    cycle: cycle,
                    requestDeadline: deadlineUtc);
            case InventoryPaymentHoldExtensionOutcome.AmbientTransactionUnsupported:
                return Fail(
                    OrderPaymentHoldPreparationOutcome.AmbientTransactionUnsupported,
                    checkoutId,
                    providerCode,
                    distinctReservationIds.Length,
                    cycle: cycle,
                    requestDeadline: deadlineUtc);
        }

        // 6) Re-evaluate the Order side after the durable extension: the two deadlines are independent,
        //    so the cycle may have closed in between and Prepared must never be claimed then.
        var afterCycle = await ReadCycleAsync(checkoutId, _clock.UtcNow, cancellationToken).ConfigureAwait(false);
        if (!ReservationCyclePayabilityPolicy.IsActive(afterCycle))
        {
            return new OrderPaymentHoldPreparationResult(
                OrderPaymentHoldPreparationOutcome.CycleClosedAfterExtension,
                checkoutId,
                providerCode,
                distinctReservationIds.Length,
                deadlineUtc,
                extension.GuaranteedUntil,
                afterCycle?.CurrentCycleNumber ?? cycle?.CurrentCycleNumber,
                afterCycle?.ExpiresAt ?? cycle?.ExpiresAt,
                []);
        }

        return new OrderPaymentHoldPreparationResult(
            OrderPaymentHoldPreparationOutcome.Prepared,
            checkoutId,
            providerCode,
            distinctReservationIds.Length,
            deadlineUtc,
            extension.GuaranteedUntil,
            afterCycle?.CurrentCycleNumber,
            afterCycle?.ExpiresAt,
            []);
    }

    private async Task<ReservationCycleProjection?> ReadCycleAsync(
        Guid checkoutId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await _cycles.GetProjectionAsync(checkoutId, now, null, cancellationToken).ConfigureAwait(false);

    private static OrderPaymentHoldPreparationResult Fail(
        OrderPaymentHoldPreparationOutcome outcome,
        Guid checkoutId,
        string providerCode,
        int reservationCount = 0,
        ReservationCycleProjection? cycle = null,
        DateTimeOffset? requestDeadline = null,
        IReadOnlyList<Guid>? unavailable = null) =>
        new(
            outcome,
            checkoutId,
            providerCode,
            reservationCount,
            requestDeadline,
            null,
            cycle?.CurrentCycleNumber,
            cycle?.ExpiresAt,
            unavailable ?? []);
}
