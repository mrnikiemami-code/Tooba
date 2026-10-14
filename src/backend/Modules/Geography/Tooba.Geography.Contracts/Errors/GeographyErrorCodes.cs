namespace Tooba.Geography.Contracts.Errors;

/// <summary>
/// کدهای خطای پایدار مکان‌نگاری. کدها مستقل از زبان‌اند و متن محلی‌سازی‌شده در این لایه نگه‌داری نمی‌شود.
/// </summary>
public static class GeographyErrorCodes
{
    /// <summary>شناسهٔ کشور الزامی است.</summary>
    public const string CountryIdRequired = "geography.country.id.required";

    /// <summary>کد کشور نامعتبر یا خارج از محدودهٔ مجاز است.</summary>
    public const string CountryCodeInvalid = "geography.country.code.invalid";

    /// <summary>شناسهٔ ناحیهٔ اداری الزامی است.</summary>
    public const string AdministrativeAreaIdRequired = "geography.administrative_area.id.required";

    /// <summary>کد ناحیهٔ اداری نامعتبر یا خارج از محدودهٔ مجاز است.</summary>
    public const string AdministrativeAreaCodeInvalid = "geography.administrative_area.code.invalid";

    /// <summary>ناحیهٔ اداری نمی‌تواند والد خودش باشد.</summary>
    public const string AdministrativeAreaParentSelf = "geography.administrative_area.parent_self";

    /// <summary>شناسهٔ شهر الزامی است.</summary>
    public const string CityIdRequired = "geography.city.id.required";

    /// <summary>کد شهر نامعتبر یا خارج از محدودهٔ مجاز است.</summary>
    public const string CityCodeInvalid = "geography.city.code.invalid";

    /// <summary>شناسهٔ ناحیهٔ خدمت الزامی است.</summary>
    public const string ServiceAreaIdRequired = "geography.service_area.id.required";

    /// <summary>کد ناحیهٔ خدمت نامعتبر یا خارج از محدودهٔ مجاز است.</summary>
    public const string ServiceAreaCodeInvalid = "geography.service_area.code.invalid";

    /// <summary>شناسهٔ مکان مرجع برای ترجمه الزامی است.</summary>
    public const string TranslationPlaceIdRequired = "geography.translation.place_id.required";

    /// <summary>شناسهٔ زبان برای ترجمه الزامی است.</summary>
    public const string TranslationLanguageIdRequired = "geography.translation.language_id.required";

    /// <summary>نام محلی‌شدهٔ مکان الزامی است.</summary>
    public const string TranslationNameRequired = "geography.translation.name.required";
}
