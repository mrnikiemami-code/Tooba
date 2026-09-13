namespace Tooba.Order.Application;

/// <summary>
/// نقاط تزریق خطای کنترل‌شده قبل از COMMIT اتمی checkout. پیاده‌سازی پیش‌فرض هیچ کاری نمی‌کند.
/// </summary>
public interface ICheckoutCommitBarrier
{
    /// <summary>پس از رزرو موجودی و قبل از ماندگاری سفارش.</summary>
    Task OnAfterReserveAsync(CancellationToken cancellationToken);

    /// <summary>پس از نوشتن Order/SellerOrder/Cycle و قبل از تبدیل سبد.</summary>
    Task OnAfterOrderWriteAsync(CancellationToken cancellationToken);

    /// <summary>پس از نوشتن Cart Converted و قبل از COMMIT.</summary>
    Task OnAfterCartConvertedWriteAsync(CancellationToken cancellationToken);
}

/// <summary>سد خنثی برای Host.</summary>
public sealed class NullCheckoutCommitBarrier : ICheckoutCommitBarrier
{
    /// <inheritdoc />
    public Task OnAfterReserveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnAfterOrderWriteAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnAfterCartConvertedWriteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
