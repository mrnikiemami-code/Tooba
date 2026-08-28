namespace Tooba.BuildingBlocks.Grid;

/// <summary>
/// سیاست اعتبارسنجی و نرمال‌سازی GridQuery برای یک گرید مشخص.
/// هر ماژول whitelist فیلد/عملگر خود را تعریف می‌کند؛ Host/engine اجرای پرس‌وجو را مالک می‌ماند.
/// </summary>
public interface IGridQueryPolicy
{
    /// <summary>Query ورودی را sanitize می‌کند یا خطای 400 می‌دهد.</summary>
    GridQueryRequest Normalize(GridQueryRequest request);
}
