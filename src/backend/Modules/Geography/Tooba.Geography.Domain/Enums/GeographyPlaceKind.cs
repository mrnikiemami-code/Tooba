namespace Tooba.Geography.Domain.Enums;

/// <summary>
/// نوع مکان مرجع که یک سطر ترجمه به آن اشاره می‌کند.
/// این تفکیک‌کننده فقط برای مالکیت ترجمه است و سطح اداری (D10) را تعیین نمی‌کند.
/// </summary>
public enum GeographyPlaceKind
{
    /// <summary>کشور.</summary>
    Country = 0,

    /// <summary>ناحیهٔ اداری (استان/ایالت/منطقه) در سلسله‌مراتب داده‌محور.</summary>
    AdministrativeArea = 1,

    /// <summary>شهر.</summary>
    City = 2,

    /// <summary>ناحیهٔ خدمت زیر یک شهر.</summary>
    ServiceArea = 3,
}
