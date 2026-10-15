using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Contracts.Packaging;

namespace Tooba.Shipping.Application.Packaging.Ports;

/// <summary>
/// درز (seam) اختصاصی Shipping برای نویسندگی سیاست هزینهٔ بسته‌بندی. تنها مالک داده
/// <c>ShippingDbContext</c> است و پیاده‌سازی آن در Infrastructure همین ماژول زندگی می‌کند؛
/// Application هیچ DbContext یا نوع Infrastructure را نمی‌شناسد.
/// </summary>
/// <remarks>
/// ناوردای «نبود سیاست = هزینهٔ صفر» اینجا نیز fail-closed است: نمای یک خدمت بدون ردیف، مجموعهٔ
/// خالی برمی‌گرداند و هرگز یک هزینهٔ پیش‌فرض نمی‌سازد.
/// </remarks>
public interface IShippingPackagingPolicyDirectory
{
    /// <summary>سیاست‌های بسته‌بندی یک کد خدمت را می‌خواند.</summary>
    /// <param name="serviceCode">کد خدمت Fulfillment.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingPackagingPoliciesView>> ListAsync(string serviceCode, CancellationToken cancellationToken);

    /// <summary>یک سیاست بسته‌بندی جدید می‌سازد؛ دامنهٔ تکراری برای همان خدمت/گزینه رد می‌شود.</summary>
    /// <param name="serviceCode">کد خدمت مسیر؛ مرجع قطعی، نه مقدار بدنه.</param>
    /// <param name="request">بدنهٔ درخواست.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingPackagingPolicyAdminItem>> CreateAsync(
        string serviceCode,
        ShippingPackagingPolicyWriteRequest request,
        CancellationToken cancellationToken);

    /// <summary>هزینه/ارز/طرف پرداخت یک سیاست بسته‌بندی موجود را ویرایش می‌کند.</summary>
    /// <param name="packagingPolicyId">شناسهٔ سیاست.</param>
    /// <param name="serviceCode">کد خدمت مسیر؛ مالکیت ردیف با آن سنجیده می‌شود.</param>
    /// <param name="request">بدنهٔ درخواست با دامنهٔ یکسان.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingPackagingPolicyAdminItem>> UpdateAsync(
        Guid packagingPolicyId,
        string serviceCode,
        ShippingPackagingPolicyWriteRequest request,
        CancellationToken cancellationToken);

    /// <summary>سیاست بسته‌بندی را فعال/غیرفعال می‌کند.</summary>
    /// <param name="packagingPolicyId">شناسهٔ سیاست.</param>
    /// <param name="serviceCode">کد خدمت مسیر؛ مالکیت ردیف با آن سنجیده می‌شود.</param>
    /// <param name="isActive">وضعیت هدف.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingPackagingPolicyAdminItem>> SetActiveAsync(
        Guid packagingPolicyId,
        string serviceCode,
        bool isActive,
        CancellationToken cancellationToken);
}

/// <summary>
/// نگاشت خطای نوع‌دار Shipping به شکست <see cref="Result"/> بر پایهٔ کد پایدار، آینهٔ
/// <c>ShippingAdminOperation</c>: تشخیص فقط با کد نوع‌دار انجام می‌شود، نه با متن پیام.
/// </summary>
public static class ShippingPackagingOperation
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
