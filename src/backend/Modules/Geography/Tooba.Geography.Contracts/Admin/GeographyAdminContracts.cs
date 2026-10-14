namespace Tooba.Geography.Contracts.Admin;

/// <summary>
/// یک گزینهٔ مکانی برای انتخاب‌های ادمین: شناسهٔ پایدار + کد مستقل از زبان + برچسب نمایشی محلی‌شده.
/// هویت هرگز برچسب نیست؛ برچسب فقط برای نمایش می‌آید و در نبود ترجمه، کد پایدار جانشین می‌شود.
/// </summary>
/// <param name="Id">شناسهٔ پایدار مکان (هویت قطعی برای نوشتن).</param>
/// <param name="Code">کد پایدار مستقل از زبان.</param>
/// <param name="Label">برچسب نمایشی محلی‌شده؛ در نبود ترجمه برابر کد است.</param>
/// <param name="ParentId">شناسهٔ والد در سلسله‌مراتب؛ برای کشور تهی است.</param>
/// <param name="IsActive">آیا مکان در فهرست فعال است.</param>
public sealed record GeographyPlaceOption(
    Guid Id,
    string Code,
    string Label,
    Guid? ParentId,
    bool IsActive);
