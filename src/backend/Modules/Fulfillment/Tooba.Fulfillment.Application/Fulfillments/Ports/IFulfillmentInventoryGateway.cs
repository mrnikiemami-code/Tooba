namespace Tooba.Fulfillment.Application.Fulfillments.Ports;


/// <summary>
/// درز موجودی برای dispatch؛ Fulfillment مستقیم Inventory DbContext باز نمی‌کند.
/// </summary>
public interface IFulfillmentInventoryGateway
{
    /// <summary>
    /// رزرو را پس از dispatch کامل خط مصرف می‌کند.
    /// </summary>
    Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>
    /// مصرف رزرو با هویت پایدار مصرف‌کننده تا replay پس از شکست commit در Fulfillment ایمن شود.
    /// <para>
    /// <b>fail-closed:</b> پیاده‌سازی پیش‌فرض عمداً استثنا می‌دهد و هرگز به مسیر بدون کلید
    /// برنمی‌گردد. اگر درزی این عضو را override نکند، نبود قابلیت کلیددار نباید به مصرف
    /// بدون هویت تبدیل شود، چون آن مسیر نمی‌تواند replay را اثبات کند و ممکن است موجودی را
    /// دوباره کسر کند. فقط <c>FulfillmentInventoryGateway</c> این عضو را override می‌کند و کلید
    /// را به overload کلیددار Inventory می‌رساند؛ مسیرهای غیربستهٔ تجمیعی (dispatch تک‌مرسوله،
    /// بازگشت، لغو، release) عمداً روی مسیر بدون کلید می‌مانند و هرگز این عضو را صدا نمی‌زنند.
    /// </para>
    /// </summary>
    /// <param name="reservationId">شناسهٔ رزرو Held.</param>
    /// <param name="consumerKey">هویت پایدار و غیرتهی مصرف‌کننده.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task ConsumeReservationAsync(Guid reservationId, string consumerKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "fulfillment.inventory.keyed_consume_not_supported: this IFulfillmentInventoryGateway "
            + "implementation does not support keyed reservation consumption; keyed consume must never "
            + "fall back to the unkeyed path.");

    /// <summary>
    /// رزرو Held سفارش پرداخت‌شده را از TTL سبد خارج می‌کند (ExpiresAt=null). Idempotent.
    /// </summary>
    Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>
    /// TB-262: مصرف رزرو اختصاصی یک مرسولهٔ جایگزینِ کالای آسیب‌دیده با هویت پایدار تصمیم. مسیر بدون کلید
    /// عمداً وجود ندارد تا dispatch یک جایگزین هرگز موجودی را دوباره کسر نکند.
    /// </summary>
    Task ConsumeReplacementReservationAsync(Guid reservationId, string consumerKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "fulfillment.inventory.replacement_consume_not_supported: a damaged-goods replacement reservation "
            + "must be consumed with its durable decision identity; there is no unkeyed fallback.");

    /// <summary>
    /// TB-262 RETENTION RULE: آزادسازی رزرو سالمِ جایگزینی فقط با لغو مالکانهٔ خود سفارش مجاز است.
    /// ابطال یک مرسولهٔ جایگزین پیش از ارسال رزرو را آزاد نمی‌کند (موجودی به سفارش فعال گره خورده و در تلاش
    /// بعدی بازمی‌بندد). فقط رزرو Held و مصرف‌نشده یک‌بار آزاد می‌شود.
    /// </summary>
    Task ReleaseReplacementReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "fulfillment.inventory.replacement_release_not_supported: a cancelled damaged-goods replacement "
            + "must release only its own new reservation through the Inventory contract seam.");

    /// <summary>
    /// TB-262: رزرو موجودی سالم برای یک تصمیم جایگزینی. هویت کالا/فروشنده فقط از رزرو فروش اصلیِ متعهد
    /// استخراج می‌شود، نه از کلاینت. نبود موجودی خطای نوع‌دار می‌دهد و رزروی باقی نمی‌گذارد.
    /// </summary>
    Task<FulfillmentReplacementReservation> ReserveReplacementReservationAsync(
        Guid originalReservationId,
        decimal quantity,
        string reservationKey,
        string externalReference,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "fulfillment.inventory.replacement_reserve_not_supported: a damaged-goods replacement needs the "
            + "Inventory-owned purpose-bound reservation seam.");

    /// <summary>TB-262: خواندن یک رزرو جایگزینی برای بازیابی پاسخ نامطمئن.</summary>
    Task<FulfillmentReplacementReservation?> FindReplacementReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "fulfillment.inventory.replacement_find_not_supported: a damaged-goods replacement needs the "
            + "Inventory-owned reservation read seam.");
}

/// <summary>TB-262: رزرو سالمِ اختصاصی یک جایگزینی که Inventory مالک آن است.</summary>
public sealed record FulfillmentReplacementReservation(
    Guid ReservationId,
    Guid StockItemId,
    Guid OfferId,
    decimal Quantity,
    string Status);

/// <summary>
/// مالک واحد قالب <c>consumer_key</c> برای مصرف رزرو در مسیر بستهٔ تجمیعی.
/// <para>
/// قالب: <c>pkg:{packageId:N}:shp:{shipmentId:N}:line:{orderLineId:N}</c> — ۱۱۱ کاراکتر
/// (۴ + ۳۲ + ۵ + ۳۲ + ۶ + ۳۲)، زیر سقف ۱۲۸ ستون
/// <c>inventory.reservation_consumptions.consumer_key</c>. هر سه جزء شناسهٔ پایدار GUID
/// هستند (نه زمان، نه تصادف، نه هش)، پس کلید روی retry پس از شکست commit دقیقاً بازتولید می‌شود و
/// Inventory می‌تواند همان مصرف منطقی را تشخیص دهد. چون هر رزرو به یک خط سفارش یکتا گره خورده و
/// خطوط سفارش داخل یک shipment تکرار نمی‌شوند، هویت مصرف‌کننده یکتا و بدون تصادم است.
/// کلید فقط در مسیر ارسال بستهٔ تجمیعی ساخته می‌شود؛ مسیرهای دیگر بدون کلید می‌مانند.
/// </para>
/// </summary>
public static class FulfillmentInventoryConsumptionKey
{
    /// <summary>
    /// کلید پایدار و قطعی مصرف برای یک رزرو در ارسال بستهٔ تجمیعی.
    /// </summary>
    /// <param name="consolidatedPackageId">شناسهٔ بستهٔ تجمیعی.</param>
    /// <param name="shipmentId">شناسهٔ مرسولهٔ عضو بسته.</param>
    /// <param name="orderLineId">شناسهٔ خط سفارش مالک رزرو.</param>
    /// <returns>کلید پایدار مصرف‌کننده (بدون وابستگی به زمان/تصادف).</returns>
    public static string For(Guid consolidatedPackageId, Guid shipmentId, Guid orderLineId) =>
        $"pkg:{consolidatedPackageId:N}:shp:{shipmentId:N}:line:{orderLineId:N}";
}
