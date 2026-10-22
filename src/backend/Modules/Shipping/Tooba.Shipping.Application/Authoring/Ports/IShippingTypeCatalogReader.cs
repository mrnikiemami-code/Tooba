namespace Tooba.Shipping.Application.Authoring.Ports;

/// <summary>
/// یک متن خام و ذخیره‌شدهٔ تعریف نوع ارسال برای یک زبان محتوا.
/// </summary>
/// <param name="LanguageId">شناسهٔ پایدار زبان محتوا.</param>
/// <param name="Name">نام غیرتهی.</param>
/// <param name="Description">توضیح اختیاری.</param>
public sealed record ShippingTypeFactTranslation(Guid LanguageId, string Name, string? Description);

/// <summary>
/// فکت فقط-خواندنی یک تعریف نوع ارسال که Shipping مصرف می‌کند.
/// </summary>
/// <remarks>
/// این فکت عمداً کوچک است: فقط هویت/کد/ترتیب/وضعیت فعال/بازبینی و متن‌های خام. هیچ نرخ، جغرافیا،
/// بسته‌بندی، برنامه، ارز یا وعده‌ای حمل نمی‌شود و هیچ تعریف کاتالوگی در Shipping کپی/نگه‌داری
/// نمی‌گردد.
/// </remarks>
/// <param name="ShippingTypeId">شناسهٔ پایدار تعریف.</param>
/// <param name="Code">کد canonical.</param>
/// <param name="IsActive">وضعیت فعال در همین لحظهٔ خواندن.</param>
/// <param name="SortOrder">ترتیب اعلام‌شده.</param>
/// <param name="Revision">بازبینی تعریف.</param>
/// <param name="Translations">متن‌های خام ذخیره‌شده.</param>
public sealed record ShippingTypeFact(
    Guid ShippingTypeId,
    string Code,
    bool IsActive,
    int SortOrder,
    long Revision,
    IReadOnlyList<ShippingTypeFactTranslation> Translations);

/// <summary>
/// درز فقط-خواندنی Application برای کاتالوگ نوع ارسال که مالک آن Fulfillment است.
/// </summary>
/// <remarks>
/// Application هرگز قرارداد/مدل ماژول دیگر را مستقیم مصرف نمی‌کند؛ آداپتور زیرساخت آن را به این فکت
/// نگاشت می‌کند. کاتالوگ خالی معتبر است، اما شکست/لغو فراخوانی مقصد هرگز به فهرست خالی تبدیل
/// نمی‌شود و همیشه به‌عنوان خطا منتشر می‌گردد.
/// </remarks>
public interface IShippingTypeCatalogReader
{
    /// <summary>تعریف‌های فعال، به ترتیب اعلام‌شدهٔ مالک و سپس شناسه.</summary>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<ShippingTypeFact>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>تعریف‌های درخواستی با همان شناسه‌ها؛ شناسهٔ ناموجود در نتیجه نمی‌آید.</summary>
    /// <param name="shippingTypeIds">شناسه‌های درخواستی.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<ShippingTypeFact>> GetByIdsAsync(
        IReadOnlyCollection<Guid> shippingTypeIds,
        CancellationToken cancellationToken);
}
