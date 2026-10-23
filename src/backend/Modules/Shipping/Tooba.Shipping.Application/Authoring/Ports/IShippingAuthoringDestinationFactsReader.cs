using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Authoring;

namespace Tooba.Shipping.Application.Authoring.Ports;

/// <summary>
/// درز فقط-خواندنی که کلیدهای <b>typed</b> مقصد (نوع + شناسه) را به فکت‌های واقعی و برچسب محلی‌شده
/// تبدیل می‌کند تا ویرایشگر پیش‌نویس موجود بتواند والدهای ذخیره‌شده را بدون پیمایش جهان بداند.
/// </summary>
/// <remarks>
/// <para>
/// ورودی فقط کلیدهای انتخاب‌شدهٔ همان پیش‌نویس مجاز است، نه فهرست دلخواه مالک دیگر. خروجی یک فکت
/// برای هر کلید <b>متمایز</b> است و کلید اصلی، جفت (نوع، شناسه) است: یک GUID یکسان در دو نوع، دو
/// هویت مستقل می‌ماند.
/// </para>
/// <para>
/// غیبت/غیرفعال‌بودن هویت یا والد، تناقض کشور/والد و چرخه، یک فکت <c>Unresolved</c> بدون هیچ شکل
/// نوشتنی جعلی است؛ انتخاب خام هرگز حذف نمی‌شود. پاسخ متناقض/با شناسهٔ اشتباه از درزهای پایین‌دست،
/// fail-closed است و استثنا/لغو فراخوان عیناً منتشر می‌شود.
/// </para>
/// </remarks>
public interface IShippingAuthoringDestinationFactsReader
{
    /// <summary>
    /// فکت‌های مقصد را برای کلیدهای typed درخواستی می‌سازد.
    /// </summary>
    /// <param name="keys">کلیدهای انتخاب‌شدهٔ همان پیش‌نویس؛ تکراری‌ها یک‌بار خوانده می‌شوند.</param>
    /// <param name="languageId">شناسهٔ زبان محتوای پیش‌فرض authoritative برای برچسب.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<IReadOnlyList<ShippingAuthoringDestinationFactItem>>> ReadAsync(
        IReadOnlyList<ShippingAuthoringSelectedAreaItem> keys,
        Guid languageId,
        CancellationToken cancellationToken);
}
