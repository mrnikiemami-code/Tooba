using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Contracts.Admin;

namespace Tooba.Shipping.Application.Rates.Ports;

/// <summary>
/// درز (seam) فقط-اختصاصیِ Shipping برای نویسندگی سیاست نرخ و مجوز پوشش. تنها مالک داده
/// <c>ShippingDbContext</c> است و پیاده‌سازی آن در Infrastructure همین ماژول زندگی می‌کند؛
/// Application هیچ DbContext یا نوع Infrastructure را نمی‌شناسد.
/// </summary>
/// <remarks>
/// ناوردای «نبود مجوز = بدون پوشش» اینجا نیز fail-closed است: نمای یک خدمت بدون ردیف، مجموعهٔ
/// خالی برمی‌گرداند و هرگز یک مجوز پیش‌فرض نمی‌سازد.
/// </remarks>
public interface IShippingRateCoverageDirectory
{
    /// <summary>نرخ‌ها و مجوزهای یک کد خدمت را می‌خواند.</summary>
    /// <param name="serviceCode">کد خدمت Fulfillment.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingRatesAndCoverageView>> ListAsync(string serviceCode, CancellationToken cancellationToken);

    /// <summary>یک سیاست نرخ جدید می‌سازد؛ دامنهٔ تکراری برای همان خدمت رد می‌شود.</summary>
    /// <param name="serviceCode">کد خدمت مسیر؛ مرجع قطعی، نه مقدار بدنه.</param>
    /// <param name="request">بدنهٔ درخواست.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingRatePolicyAdminItem>> CreateRateAsync(
        string serviceCode,
        ShippingRatePolicyWriteRequest request,
        CancellationToken cancellationToken);

    /// <summary>مبلغ/آستانه/زمان‌های یک سیاست نرخ موجود را ویرایش می‌کند.</summary>
    /// <param name="ratePolicyId">شناسهٔ سیاست.</param>
    /// <param name="serviceCode">کد خدمت مسیر؛ مالکیت ردیف با آن سنجیده می‌شود.</param>
    /// <param name="request">بدنهٔ درخواست با دامنهٔ یکسان.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingRatePolicyAdminItem>> UpdateRateAsync(
        Guid ratePolicyId,
        string serviceCode,
        ShippingRatePolicyWriteRequest request,
        CancellationToken cancellationToken);

    /// <summary>سیاست نرخ را فعال/غیرفعال می‌کند.</summary>
    /// <param name="ratePolicyId">شناسهٔ سیاست.</param>
    /// <param name="serviceCode">کد خدمت مسیر؛ مالکیت ردیف با آن سنجیده می‌شود.</param>
    /// <param name="isActive">وضعیت هدف.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingRatePolicyAdminItem>> SetRateActiveAsync(
        Guid ratePolicyId,
        string serviceCode,
        bool isActive,
        CancellationToken cancellationToken);

    /// <summary>یک مجوز پوشش صریح جدید می‌سازد؛ دامنهٔ تکراری برای همان خدمت رد می‌شود.</summary>
    /// <param name="serviceCode">کد خدمت مسیر؛ مرجع قطعی، نه مقدار بدنه.</param>
    /// <param name="request">بدنهٔ درخواست.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingCoverageGrantAdminItem>> CreateGrantAsync(
        string serviceCode,
        ShippingCoverageGrantWriteRequest request,
        CancellationToken cancellationToken);

    /// <summary>مجوز پوشش را فعال/غیرفعال می‌کند.</summary>
    /// <param name="coverageGrantId">شناسهٔ مجوز.</param>
    /// <param name="serviceCode">کد خدمت مسیر؛ مالکیت ردیف با آن سنجیده می‌شود.</param>
    /// <param name="isActive">وضعیت هدف.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingCoverageGrantAdminItem>> SetGrantActiveAsync(
        Guid coverageGrantId,
        string serviceCode,
        bool isActive,
        CancellationToken cancellationToken);
}

/// <summary>
/// نگاشت خطای نوع‌دار Shipping به شکست <see cref="Result"/> بر پایهٔ کد پایدار، آینهٔ
/// <c>SupportOperation</c>/<c>ReturnsOperation</c>: تشخیص فقط با کد نوع‌دار انجام می‌شود، نه با متن پیام.
/// </summary>
public static class ShippingAdminOperation
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
