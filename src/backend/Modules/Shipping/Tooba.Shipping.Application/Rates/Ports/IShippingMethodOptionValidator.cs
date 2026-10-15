using Tooba.BuildingBlocks.Results;

namespace Tooba.Shipping.Application.Rates.Ports;

/// <summary>
/// درز اعتبارسنجی «هویت روش ارسال» برای نویسندگی ادمین: کد خدمت به‌همراه گزینهٔ اختیاری آن.
/// </summary>
/// <remarks>
/// <para>
/// <b>چرا یک درز جدا.</b> Shipping مالک «چقدر» است، نه هویت خدمت/گزینه. کاتالوگ خدمت و گزینه
/// دارایی Fulfillment است، پس Shipping فقط با یک درز فقط-خواندنی می‌پرسد که این ترکیب
/// (خدمت، گزینه) واقعاً وجود دارد؛ هیچ‌جا نام گزینهٔ service به‌صورت ثابت هاردکد نمی‌شود.
/// </para>
/// <para>
/// <b>Fail-closed.</b> نبود خدمت یا گزینه یعنی ترکیب نامعتبر است و ثبت رد می‌شود. «کاتالوگ در
/// دسترس نیست» هم به رد ترجمه می‌شود، نه به پذیرش خوش‌بینانه.
/// </para>
/// </remarks>
public interface IShippingMethodOptionValidator
{
    /// <summary>ترکیب خدمت/گزینهٔ درخواستی را در کاتالوگ واقعی بررسی می‌کند.</summary>
    /// <param name="serviceCode">کد خدمت؛ همان کلید Contracts.</param>
    /// <param name="optionCode">کد گزینه؛ تهی یعنی سیاست در سطح خدمت است.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<bool>> ValidateAsync(
        string serviceCode,
        string? optionCode,
        CancellationToken cancellationToken);
}
