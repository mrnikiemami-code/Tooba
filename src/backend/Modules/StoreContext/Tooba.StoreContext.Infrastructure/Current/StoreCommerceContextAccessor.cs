using Tooba.StoreContext.Contracts.Current;

namespace Tooba.StoreContext.Infrastructure.Current;

/// <summary>
/// accessor در scope برای زمینهٔ تجارت مؤثر فروشگاه. هم درز خواندن و هم درز تخصیص را پیاده می‌کند،
/// هیچ حالت قابل‌تغییر static نگه نمی‌دارد، به <c>HttpContext</c> وابسته نیست و از <c>AsyncLocal</c>
/// استفاده نمی‌کند: طول عمر آن دقیقاً یک scope از DI است (یک درخواست یا یک چرخهٔ کارگر).
/// </summary>
public sealed class StoreCommerceContextAccessor : ICurrentStoreCommerceContext, IStoreCommerceContextAssigner
{
    private StoreCommerceContext? _current;

    /// <inheritdoc />
    public StoreCommerceContext? Current => _current;

    /// <inheritdoc />
    public void Assign(StoreCommerceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _current = context;
    }
}
