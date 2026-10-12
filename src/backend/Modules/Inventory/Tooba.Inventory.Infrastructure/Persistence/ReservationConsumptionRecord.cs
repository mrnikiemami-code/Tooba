namespace Tooba.Inventory.Infrastructure.Persistence;

/// <summary>
/// ژورنال پایدار مصرف رزرو در schema <c>inventory</c>.
/// <para>
/// کلید اصلی، شناسهٔ رزرو است؛ پس هر رزرو حداکثر یک مصرف پایدار دارد و پایگاه‌داده تکراری‌بودن
/// را در سطح محدودیت یکتا رد می‌کند.
/// </para>
/// <para>
/// provenance تاریخی بازسازی نمی‌شود: نبودن ردیف ژورنال به‌معنای «replay اثبات‌شده» نیست و
/// مصرف‌کنندهٔ کلیددار نباید برای رزروهای قدیمی مصرف‌شده هویت جعلی بگیرد.
/// </para>
/// </summary>
public sealed class ReservationConsumptionRecord
{
    /// <summary>
    /// شناسهٔ رزرو مصرف‌شده؛ هم‌زمان کلید اصلی.
    /// </summary>
    public Guid ReservationId { get; init; }

    /// <summary>
    /// موقعیت موجودی که در همین مصرف کسر شد.
    /// </summary>
    public Guid StockItemId { get; init; }

    /// <summary>
    /// مقدار دقیق کسرشده در لحظهٔ مصرف.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// هویت پایدار مصرف‌کننده برای تشخیص replay؛ برای فراخوان بدون هویت تهی است.
    /// </summary>
    public string? ConsumerKey { get; init; }

    /// <summary>
    /// زمان مصرف UTC.
    /// </summary>
    public DateTimeOffset ConsumedAt { get; init; }
}
