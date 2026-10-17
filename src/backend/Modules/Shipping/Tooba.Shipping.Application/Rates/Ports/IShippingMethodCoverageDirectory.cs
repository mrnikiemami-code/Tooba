using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Contracts.Admin;

namespace Tooba.Shipping.Application.Rates.Ports;

/// <summary>
/// درز فقط-اختصاصیِ Shipping برای تعیین <em>حالت تحدید جغرافیایی</em> یک روش canonical
/// (خدمت + گزینهٔ اختیاری).
/// </summary>
/// <remarks>
/// <para>
/// این seam عمداً از <see cref="IShippingRateCoverageDirectory"/> جدا است: مسئولیت آن یک تصمیم واحد
/// است («آیا ادمین این روش را به مقاصد محدودی محدود کرده؟»)، نه نویسندگی نرخ/مجوز. جدا بودن آن همچنین
/// به معنی آن است که ردیف‌های مجوز سطح‌خدمت هرگز به‌طور خودکار به گزینه‌ها نسبت داده نمی‌شوند.
/// </para>
/// <para>
/// هیچ ردیف مقصد فراگیری ساخته نمی‌شود: «مجموعهٔ مجاز خالی» با
/// <c>Restricted</c> + صفر مجوز بیان می‌شود، نه با یک مقصد ساختگی.
/// </para>
/// </remarks>
public interface IShippingMethodCoverageDirectory
{
    /// <summary>
    /// حالت تحدید یک روش را تعیین می‌کند (ایجاد در اولین بار، ویرایش پس از آن). کد خدمت از مسیر
    /// می‌آید و مرجع قطعی است.
    /// </summary>
    /// <param name="serviceCode">کد خدمت مسیر؛ مرجع قطعی، نه مقدار بدنه.</param>
    /// <param name="request">بدنهٔ درخواست شامل گزینهٔ اختیاری و حالت هدف.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingMethodCoverageAdminItem>> SetMethodCoverageAsync(
        string serviceCode,
        ShippingMethodCoverageWriteRequest request,
        CancellationToken cancellationToken);
}
