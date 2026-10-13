using Tooba.BuildingBlocks;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Contracts.Payments;

namespace Tooba.Order.Application.Checkout.Process;

/// <summary>
/// ارزیاب فقط‌خواندنی «پول تازه» برای شروع پرداخت. هیچ‌چیز را نمی‌نویسد، هیچ رزروی را تمدید/آزاد/مصرف
/// نمی‌کند، هیچ مهلتی نمی‌سازد و هیچ PSP/کیف پولی را صدا نمی‌زند.
/// <list type="number">
/// <item>مجموعهٔ رزرو فقط از خطوط پایدار سفارش استخراج می‌شود و باید کامل و بدون تکرار باشد.</item>
/// <item>چرخهٔ رزرو با سیاست متعارف <see cref="ReservationCyclePayabilityPolicy"/> سنجیده می‌شود.</item>
/// <item>وضعیت واقعی هر رزرو از درز فقط‌خواندنی مالک Inventory خوانده می‌شود، نه از وضعیت چرخه.</item>
/// <item>غیبت اقتدار چرخه یا نبود چرخه هرگز <c>Payable</c> نیست.</item>
/// </list>
/// تضمین نمی‌کند: هیچ ACID بین‌ماژولی و هیچ «بعد از این لحظه پول مجاز نیست». این یک خواندن لحظه‌ای است؛
/// آزادسازی/انقضا/بسته‌شدن چرخه می‌تواند بلافاصله پس از آن رخ دهد و بستن آن باقی‌مانده وظیفهٔ مسیر
/// متعارف بازتملک/بازگردانی است.
/// </summary>
public sealed class OrderPaymentFreshPayabilityService : IOrderPaymentFreshPayabilityPort
{
    /// <summary>نام وضعیت رزرو اثبات‌شده در projection مالک Inventory (از <c>StockReservationStatus.Held</c>).</summary>
    private const string HeldReservationStatus = "Held";

    private readonly IOrderCheckoutReservationSetReader _reservations;
    private readonly IReservationCycleDirectory? _cycles;
    private readonly IOrderInventoryLifecyclePort _inventory;
    private readonly IClock _clock;

    /// <summary>ارزیاب را به خواندن خطوط سفارش، چرخهٔ Order و وضعیت واقعی Inventory وصل می‌کند.</summary>
    public OrderPaymentFreshPayabilityService(
        IOrderCheckoutReservationSetReader reservations,
        IReservationCycleDirectory? cycles,
        IOrderInventoryLifecyclePort inventory,
        IClock clock)
    {
        _reservations = reservations;
        _cycles = cycles;
        _inventory = inventory;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<OrderFreshPaymentPayabilityResult> GetFreshPaymentPayabilityAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        if (checkoutId == Guid.Empty)
        {
            return Fail(OrderFreshPaymentPayability.CheckoutUnavailable, checkoutId);
        }

        var set = await _reservations.GetReservationSetAsync(checkoutId, cancellationToken).ConfigureAwait(false);
        if (set is null)
        {
            return Fail(OrderFreshPaymentPayability.CheckoutUnavailable, checkoutId);
        }

        var byLine = set.ReservationIdsByLine;
        if (byLine.Count == 0 || byLine.Any(id => id is null || id == Guid.Empty))
        {
            return Fail(OrderFreshPaymentPayability.ReservationSetIncomplete, checkoutId);
        }

        var distinct = byLine
            .Select(id => id!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();
        if (distinct.Length != byLine.Count)
        {
            // One reservation id bound to two lines is ambiguity (or a foreign id), not a second hold.
            return Fail(OrderFreshPaymentPayability.ReservationSetIncomplete, checkoutId);
        }

        if (_cycles is null)
        {
            // No cycle authority composed: payability cannot be proven, so this is never authorization.
            return Fail(OrderFreshPaymentPayability.AuthorityUnavailable, checkoutId);
        }

        var now = _clock.UtcNow;
        var cycle = await _cycles
            .GetProjectionAsync(checkoutId, now, supplyStatus: null, cancellationToken)
            .ConfigureAwait(false);
        if (!ReservationCyclePayabilityPolicy.IsActive(cycle))
        {
            var outcome = ReservationCyclePayabilityPolicy.HasReachedRetryLimit(cycle)
                ? OrderFreshPaymentPayability.RetryExhausted
                : OrderFreshPaymentPayability.CycleNotPayable;
            return Fail(outcome, checkoutId, cycle);
        }

        var unavailable = new List<Guid>();
        foreach (var reservationId in distinct)
        {
            var view = await _inventory.FindReservationAsync(reservationId, cancellationToken).ConfigureAwait(false);
            var held = view is not null
                && string.Equals(view.Status, HeldReservationStatus, StringComparison.Ordinal)
                && (view.ExpiresAt is not { } expiresAt || expiresAt > now);
            if (!held)
            {
                unavailable.Add(reservationId);
            }
        }

        if (unavailable.Count > 0)
        {
            return Fail(
                OrderFreshPaymentPayability.HoldUnavailable,
                checkoutId,
                cycle,
                distinct.Length,
                unavailable);
        }

        return new OrderFreshPaymentPayabilityResult(
            OrderFreshPaymentPayability.Payable,
            checkoutId,
            distinct.Length,
            [],
            cycle?.CurrentCycleNumber,
            cycle?.ExpiresAt,
            cycle?.RetryCountRemaining,
            cycle?.EffectiveMaxCycles);
    }

    private static OrderFreshPaymentPayabilityResult Fail(
        OrderFreshPaymentPayability outcome,
        Guid checkoutId,
        ReservationCycleProjection? cycle = null,
        int reservationCount = 0,
        IReadOnlyList<Guid>? unavailable = null) =>
        new(
            outcome,
            checkoutId,
            reservationCount,
            unavailable ?? [],
            cycle?.CurrentCycleNumber,
            cycle?.ExpiresAt,
            cycle?.RetryCountRemaining,
            cycle?.EffectiveMaxCycles);
}
