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
    /// پیاده‌سازی پیش‌فرض برای سازگاریِ درزهای موجود به مسیر بدون کلید برمی‌گردد. فقط
    /// <c>FulfillmentInventoryGateway</c> این عضو را override می‌کند و کلید را به overload
    /// کلیددار Inventory می‌رساند؛ مسیرهای غیربستهٔ تجمیعی (dispatch تک‌مرسوله، بازگشت، لغو،
    /// release) عمداً روی مسیر بدون کلید می‌مانند.
    /// </para>
    /// </summary>
    /// <param name="reservationId">شناسهٔ رزرو Held.</param>
    /// <param name="consumerKey">هویت پایدار و غیرتهی مصرف‌کننده.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task ConsumeReservationAsync(Guid reservationId, string consumerKey, CancellationToken cancellationToken) =>
        ConsumeReservationAsync(reservationId, cancellationToken);

    /// <summary>
    /// رزرو Held سفارش پرداخت‌شده را از TTL سبد خارج می‌کند (ExpiresAt=null). Idempotent.
    /// </summary>
    Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken);
}

/// <summary>
/// مالک واحد قالب <c>consumer_key</c> برای مصرف رزرو در مسیر بستهٔ تجمیعی.
/// <para>
/// قالب: <c>pkg:{packageId:N}:shp:{shipmentId:N}:line:{orderLineId:N}</c> — ۷۸ کاراکتر، زیر سقف
/// ۱۲۸ ستون <c>inventory.reservation_consumptions.consumer_key</c>. هر سه جزء شناسهٔ پایدار GUID
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
