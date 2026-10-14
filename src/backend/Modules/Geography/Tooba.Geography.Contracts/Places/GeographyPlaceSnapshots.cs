namespace Tooba.Geography.Contracts.Places;

/// <summary>کشور؛ برگهٔ ریشهٔ هویت مکانی با کد ISO پایدار.</summary>
/// <param name="CountryId">شناسهٔ پایدار کشور.</param>
/// <param name="CountryCode">کد کشور مستقل از زبان (ISO 3166-1).</param>
/// <param name="IsActive">آیا کشور در فهرست فعال است.</param>
public sealed record CountrySnapshot(
    Guid CountryId,
    string CountryCode,
    bool IsActive);

/// <summary>
/// ناحیهٔ اداری (استان/ایالت/منطقه). عمق سلسله‌مراتب داده‌محور است و سطح آن تصمیم‌گیری نشده است.
/// </summary>
/// <param name="AdministrativeAreaId">شناسهٔ پایدار ناحیه.</param>
/// <param name="CountryId">کشور مالک.</param>
/// <param name="ParentAdministrativeAreaId">ناحیهٔ والد در سلسله‌مراتب؛ برای ریشه تهی است.</param>
/// <param name="Code">کد پایدار مستقل از زبان.</param>
/// <param name="IsActive">آیا ناحیه فعال است.</param>
public sealed record AdministrativeAreaSnapshot(
    Guid AdministrativeAreaId,
    Guid CountryId,
    Guid? ParentAdministrativeAreaId,
    string Code,
    bool IsActive);

/// <summary>شهر؛ هویت پایدار جدا از نام محلی‌شده. دو شهر هم‌نام در ناحیه‌های متفاوت یکسان نیستند.</summary>
/// <param name="CityId">شناسهٔ پایدار شهر.</param>
/// <param name="CountryId">کشور مالک.</param>
/// <param name="AdministrativeAreaId">ناحیهٔ اداری اختیاری مالک.</param>
/// <param name="Code">کد پایدار مستقل از زبان.</param>
/// <param name="IsActive">آیا شهر فعال است.</param>
public sealed record CitySnapshot(
    Guid CityId,
    Guid CountryId,
    Guid? AdministrativeAreaId,
    string Code,
    bool IsActive);

/// <summary>ناحیهٔ خدمت (زیرشهر/منطقهٔ عملیاتی) زیر یک شهر.</summary>
/// <param name="ServiceAreaId">شناسهٔ پایدار ناحیهٔ خدمت.</param>
/// <param name="CityId">شهر مالک.</param>
/// <param name="Code">کد پایدار مستقل از زبان.</param>
/// <param name="IsActive">آیا ناحیهٔ خدمت فعال است.</param>
public sealed record ServiceAreaSnapshot(
    Guid ServiceAreaId,
    Guid CityId,
    string Code,
    bool IsActive);
