namespace Tooba.Inventory.Contracts.Fulfillment;

/// <summary>
/// درز پایدار Inventory برای مصرف/commit رزرو پس از پرداخت — بدون افشای Directory داخلی.
/// </summary>
public interface IFulfillmentInventoryLifecyclePort
{
    /// <summary>
    /// رزرو را پس از dispatch مصرف می‌کند.
    /// <para>
    /// این عضو بدون هویت مصرف‌کننده است و سخت‌گیرانه باقی می‌ماند: مصرف دوباره روی رزروی که
    /// دیگر Held نیست خطای نوع‌دار می‌دهد و هرگز به‌صورت کورکورانه موفق اعلام نمی‌شود.
    /// فراخوان‌های جدیدِ replay-آگاه باید overload کلیددار را با هویت پایدار مصرف‌کننده صدا بزنند.
    /// </para>
    /// </summary>
    Task ConsumeAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>
    /// مصرف رزرو با هویت پایدار مصرف‌کننده.
    /// <para>
    /// مصرف با همان <paramref name="consumerKey"/> و ژورنال مصرف منطبق، replay اثبات‌شده است و
    /// بدون کسر دوبارهٔ موجودی موفق برمی‌گردد؛ مصرف‌کنندهٔ متفاوت، نبودن ژورنال برای رزروی که
    /// وضعیت مصرف‌شده دارد، یا عدم تطابق مقدار/موقعیت، خطای نوع‌دار می‌دهد.
    /// </para>
    /// </summary>
    /// <param name="reservationId">شناسهٔ رزرو Held.</param>
    /// <param name="consumerKey">هویت پایدار و غیرتهی مصرف‌کننده؛ مثل package/member/line.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task ConsumeAsync(Guid reservationId, string consumerKey, CancellationToken cancellationToken);

    /// <summary>
    /// رزرو را برای سفارش پرداخت‌شده commit می‌کند.
    /// </summary>
    Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken);
}
