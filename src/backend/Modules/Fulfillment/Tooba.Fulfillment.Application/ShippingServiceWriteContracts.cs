using MediatR;

namespace Tooba.Fulfillment.Application;

/// <summary>درز موقت اعتبار LanguageId و فهرست seed بدون وابستگی Fulfillment به Localization.</summary>
public interface IShippingServiceLanguageGate
{
    /// <summary>همه LanguageIdها باید در رجیستری زبان شناخته‌شده باشند.</summary>
    Task EnsureKnownAsync(IReadOnlyList<Guid> languageIds, CancellationToken cancellationToken);

    /// <summary>زبان‌های موجود برای seed اولیهٔ کاتالوگ ارسال.</summary>
    Task<IReadOnlyList<ShippingServiceSeedLanguage>> ListForSeedAsync(CancellationToken cancellationToken);
}

/// <summary>زبان seed.</summary>
/// <param name="LanguageId">شناسه.</param>
/// <param name="Code">کد.</param>
/// <param name="Culture">فرهنگ.</param>
/// <param name="IsDefault">پیش‌فرض؟</param>
public sealed record ShippingServiceSeedLanguage(
    Guid LanguageId,
    string Code,
    string Culture,
    bool IsDefault);

/// <summary>ترجمهٔ سرویس والد.</summary>
/// <param name="LanguageId">زبان.</param>
/// <param name="Name">نام.</param>
/// <param name="Description">توضیح.</param>
public sealed record ShippingServiceTranslationWriteModel(Guid LanguageId, string Name, string? Description);

/// <summary>ترجمهٔ گزینهٔ فرزند.</summary>
/// <param name="LanguageId">زبان.</param>
/// <param name="Name">نام.</param>
public sealed record ShippingServiceOptionTranslationWriteModel(Guid LanguageId, string Name);

/// <summary>گزینهٔ سطح ۲.</summary>
/// <param name="ShippingServiceOptionId">شناسه اختیاری.</param>
/// <param name="Code">کد.</param>
/// <param name="IsActive">فعال؟</param>
/// <param name="SortOrder">ترتیب.</param>
/// <param name="Translations">ترجمه‌ها.</param>
public sealed record ShippingServiceOptionWriteModel(
    Guid? ShippingServiceOptionId,
    string Code,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceOptionTranslationWriteModel> Translations);

/// <summary>مدل نوشتن سرویس ارسال دو‌سطحی.</summary>
public sealed record ShippingServiceWriteModel(
    string Code,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceTranslationWriteModel> Translations,
    IReadOnlyList<ShippingServiceOptionWriteModel> Options);

/// <summary>نوشتن کاتالوگ سرویس ارسال روی مالک Fulfillment.</summary>
public interface IShippingServiceDirectory
{
    /// <summary>سرویس جدید.</summary>
    Task<Guid> CreateAsync(ShippingServiceWriteModel model, CancellationToken cancellationToken);

    /// <summary>ویرایش سرویس، ترجمه‌ها و گزینه‌ها.</summary>
    Task<Guid> UpdateAsync(Guid serviceId, ShippingServiceWriteModel model, CancellationToken cancellationToken);

    /// <summary>غیرفعال‌سازی نرم.</summary>
    Task DeactivateAsync(Guid serviceId, CancellationToken cancellationToken);

    /// <summary>seed اولیهٔ idempotent وقتی جدول خالی است.</summary>
    Task EnsureSeedAsync(CancellationToken cancellationToken);
}

/// <summary>فرمان ایجاد سرویس ارسال.</summary>
/// <param name="Model">مدل.</param>
public sealed record CreateShippingServiceCommand(ShippingServiceWriteModel Model) : IRequest<Guid>;

/// <summary>فرمان ویرایش سرویس ارسال.</summary>
/// <param name="ServiceId">شناسه.</param>
/// <param name="Model">مدل.</param>
public sealed record UpdateShippingServiceCommand(Guid ServiceId, ShippingServiceWriteModel Model) : IRequest<Guid>;

/// <summary>فرمان غیرفعال‌سازی سرویس ارسال.</summary>
/// <param name="ServiceId">شناسه.</param>
public sealed record DeactivateShippingServiceCommand(Guid ServiceId) : IRequest;

/// <summary>فرمان seed کاتالوگ ارسال.</summary>
public sealed record EnsureShippingCatalogSeedCommand : IRequest;
