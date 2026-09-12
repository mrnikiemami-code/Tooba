namespace Tooba.Cart.Application;

/// <summary>
/// ماندگاری سبد به‌عنوان وضعیت کاربر؛ TTL رزرو موجودی نیست.
/// </summary>
public sealed class CartLifetimeOptions
{
    /// <summary>نام بخش پیکربندی.</summary>
    public const string SectionName = "Cart";

    /// <summary>ساعت نگهداری سبد رهاشده/غیرخالی. پیش‌فرض ۷ روز.</summary>
    public int PersistenceHours { get; set; } = 168;
}
