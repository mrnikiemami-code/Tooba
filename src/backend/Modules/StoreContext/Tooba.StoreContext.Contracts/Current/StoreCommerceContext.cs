using Tooba.BuildingBlocks;

namespace Tooba.StoreContext.Contracts.Current;

/// <summary>
/// زمینهٔ تجارت مؤثر فروشگاه برای چرخهٔ جاری درخواست یا کارگر.
/// این زمینه در اختیار control plane پلتفرم است و ماژول‌های مصرف‌کننده فقط آن را می‌خوانند؛
/// هیچ مصرف‌کننده‌ای مجاز نیست Market/DefaultCurrency/SalesChannel را از خود بسازد.
/// مقدار <c>null</c> یعنی زمینه resolve نشده است و مصرف‌کننده باید fail-closed شود.
/// </summary>
/// <param name="Market">مرجع بازار مؤثر.</param>
/// <param name="DefaultCurrency">
/// فقط ارز پیش‌فرض فروشگاه: ارز پیش‌فرض/مرجحی که یک use-case در صورت نیاز به ارز اولیه به کار می‌برد.
/// این مقدار ارز تراکنش/خط/سفارش/تسویه/گروه پرداخت نیست و هیچ invariant تک‌ارزی روی Cart/Order تحمیل نمی‌کند؛
/// خطوط تراکنش مصرف‌کننده می‌توانند ارز مستقل خود را داشته باشند.
/// </param>
/// <param name="SalesChannel">نام پایدار کانال فروش؛ در این مرز همچنان یک رشته است.</param>
public sealed record StoreCommerceContext(
    string? Market,
    string? DefaultCurrency,
    string? SalesChannel);

/// <summary>
/// دسترسی خواندنی به زمینهٔ تجارت مؤثر فروشگاه برای scope جاری.
/// تا زمانی که مرز پلتفرم آن را برای یک درخواست resolve‌شده یا چرخهٔ کارگر پس‌زمینه تخصیص ندهد، وجود ندارد.
/// </summary>
public interface ICurrentStoreCommerceContext
{
    /// <summary>
    /// زمینهٔ تجارت مؤثر این scope، یا <c>null</c> وقتی resolve نشده/رد شده است.
    /// نبود مقدار یعنی زمینه معتبر نیست؛ مصرف‌کننده باید شکست بدهد و مقدار پیش‌فرض نسازد.
    /// </summary>
    StoreCommerceContext? Current { get; }
}

/// <summary>
/// درز تخصیص زمینهٔ تجارت مؤثر فروشگاه.
/// ترکیب Host/control-plane و کارگرهای پس‌زمینه آن را تخصیص می‌دهند؛
/// ماژول‌های مصرف‌کننده هرگز اختیار تخصیص زمینهٔ خود را ندارند.
/// </summary>
public interface IStoreCommerceContextAssigner
{
    /// <summary>
    /// زمینهٔ تجارت مؤثر را برای scope جاری تخصیص می‌دهد.
    /// </summary>
    /// <param name="context">زمینهٔ مؤثری که مرز پلتفرم resolve کرده است.</param>
    void Assign(StoreCommerceContext context);
}

/// <summary>
/// درز عمومی پلتفرم که زمینهٔ تجارت مؤثر فروشگاه را برای هدف یک کارگر پس‌زمینه
/// (edition و tenant اختیاری) بازسازی می‌کند و هیچ سرآیند HTTP را نمی‌خواند.
/// </summary>
public interface IWorkerStoreCommerceContextFactory
{
    /// <summary>
    /// زمینهٔ تجارت مؤثر را برای هدف edition/tenant داده‌شده انتخاب می‌کند.
    /// </summary>
    /// <param name="edition">edition فرآیند که از control plane resolve شده است.</param>
    /// <param name="tenantId">شناسهٔ tenant در حالت Single-Store؛ در Marketplace مقدار null است.</param>
    StoreCommerceContext FromTarget(ToobaEdition edition, string? tenantId);
}
