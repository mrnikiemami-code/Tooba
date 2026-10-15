using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Contracts.Scheduling;

namespace Tooba.Shipping.Application.Scheduling.Ports;

/// <summary>
/// درز فقط-اختصاصیِ Shipping برای نویسندگی برنامهٔ عملیاتی هفتگی. تنها مالک داده
/// <c>ShippingDbContext</c> است و پیاده‌سازی آن در Infrastructure همین ماژول زندگی می‌کند؛
/// Application هیچ DbContext یا نوع Infrastructure را نمی‌شناسد.
/// </summary>
/// <remarks>
/// ناوردای «نبود برنامه = بدون بازهٔ عملیاتی ثبت‌شده» اینجا fail-closed است: نمای یک خدمت بدون
/// ردیف، مجموعهٔ خالی برمی‌گرداند و هیچ برنامهٔ پیش‌فرضی ساخته نمی‌شود. این هرگز به معنای
/// «تعطیل کردن Checkout» نیست؛ این ماژول هیچ مصرف‌کنندهٔ runtime فعلی ندارد و مسیر قدیمی
/// دست‌نخورده می‌ماند.
/// </remarks>
public interface IShippingOperatingScheduleDirectory
{
    /// <summary>برنامه‌های عملیاتی یک کد خدمت را می‌خواند.</summary>
    /// <param name="serviceCode">کد خدمت Fulfillment.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingOperatingSchedulesView>> ListAsync(string serviceCode, CancellationToken cancellationToken);

    /// <summary>
    /// برنامهٔ عملیاتی یک هویت روش (خدمت + گزینه) را می‌سازد یا کامل جایگزین می‌کند.
    /// </summary>
    /// <param name="serviceCode">کد خدمت مسیر؛ مرجع قطعی، نه مقدار بدنه.</param>
    /// <param name="request">بدنهٔ درخواست.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingOperatingScheduleAdminItem>> UpsertAsync(
        string serviceCode,
        ShippingOperatingScheduleWriteRequest request,
        CancellationToken cancellationToken);

    /// <summary>برنامهٔ عملیاتی را فعال/غیرفعال می‌کند.</summary>
    /// <param name="scheduleId">شناسهٔ برنامه.</param>
    /// <param name="serviceCode">کد خدمت مسیر؛ مالکیت ردیف با آن سنجیده می‌شود.</param>
    /// <param name="isActive">وضعیت هدف.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingOperatingScheduleAdminItem>> SetActiveAsync(
        Guid scheduleId,
        string serviceCode,
        bool isActive,
        CancellationToken cancellationToken);
}

/// <summary>
/// نگاشت خطای نوع‌دار Scheduling به شکست <see cref="Result"/> بر پایهٔ کد پایدار، آینهٔ
/// <c>ShippingAdminOperation</c>: تشخیص فقط با کد نوع‌دار انجام می‌شود، نه با متن پیام.
/// </summary>
public static class ShippingScheduleOperation
{
    /// <summary>یک عملیات را اجرا و خطای معنایی Shipping را به نتیجهٔ شکست نگاشت می‌کند.</summary>
    /// <typeparam name="T">نوع مقدار موفقیت.</typeparam>
    /// <param name="action">عملیات.</param>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return await action();
        }
        catch (SemanticException ex)
        {
            return Result.Failure<T>(ex.Error);
        }
    }
}
