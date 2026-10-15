namespace Tooba.Fulfillment.Contracts.Shipping;

/// <summary>
/// درز فقط-خواندنی برچسب نمایشی فروشگاه برای روش‌های یک‌سطحی.
/// </summary>
/// <remarks>
/// <para>
/// مالک این درز ماژول Fulfillment است، چون نام نمایشی هر روش در
/// <c>fulfillment.shipping_method_translations</c> نگهداری می‌شود. مصرف‌کنندهٔ فروشگاه
/// (Order) فقط از طریق همین قرارداد برچسب را می‌خواند؛ هیچ جدول Fulfillment از Order
/// پرس‌وجو نمی‌شود و هیچ کدی در مرورگر یا Order تجزیه نمی‌شود.
/// </para>
/// <para>
/// این درز فقط «برچسب» است: نه قیمت، نه واجد شرایط بودن، نه فعال‌سازی. پیاده‌سازی
/// fail-closed است؛ دروازهٔ خاموش، جدول مهاجرت‌نشده، کد ناشناخته یا نبود ترجمهٔ زبان درخواستی
/// نتیجهٔ خالی می‌دهد تا مصرف‌کننده برچسب قدیمی خود را دست‌نخورده نگه دارد.
/// </para>
/// </remarks>
public interface IShippingOneLevelStorefrontLabelReader
{
    /// <summary>
    /// نام نمایشی پیکربندی‌شدهٔ هر روش یک‌سطحی برای همان زبان درخواستی.
    /// </summary>
    /// <param name="methodCodes">کدهای پایدار و کانونیک روش (همان <c>method_code</c>).</param>
    /// <param name="languageId">زبان حل‌شدهٔ درخواست؛ <see cref="Guid.Empty"/> یعنی نامعلوم.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <returns>
    /// نگاشت کد روش به نام نمایشی. فقط کدهایی برمی‌گردند که ردیف یک‌سطحی متناظر و ترجمهٔ
    /// دقیق همان زبان با نام غیرخالی دارند. در هر حالت دیگری (دروازهٔ خاموش، نبود جدول،
    /// کد ناشناخته، نبود ترجمه) نگاشت خالی است.
    /// </returns>
    Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        IReadOnlyCollection<string> methodCodes,
        Guid languageId,
        CancellationToken cancellationToken);
}
