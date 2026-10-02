
namespace Tooba.Host.Configuration;

/// <summary>
/// زمینهٔ تجارت مؤثر فروشگاه برای deploymentهایی که Tenant جدا ندارند (Marketplace).
/// این پیش‌فرض تجاری مالک کنترل‌پلین است، نه ماژول‌های دامنه‌ای.
/// </summary>
internal sealed class StoreCommerceOptions
{
    /// <summary>
    /// مرجع بازار مؤثر؛ تهی یعنی resolve نشده و مصرف‌کننده fail-closed می‌شود.
    /// </summary>
    public string? Market { get; set; }

    /// <summary>
    /// کد ارز پیش‌فرض فروشگاه (ISO)؛ تهی یعنی resolve نشده. این ارز فقط انتخاب پیش‌فرض اولیه است،
    /// نه ارز هر line/order/payment-group.
    /// </summary>
    public string? DefaultCurrency { get; set; }

    /// <summary>
    /// نام پایدار کانال فروش؛ تهی یعنی resolve نشده.
    /// </summary>
    public string? SalesChannel { get; set; }
}
