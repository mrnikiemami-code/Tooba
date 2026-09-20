using MediatR;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application;

/// <summary>درز موقت اعتبارسنجی LanguageId برای ترجمهٔ واحد (بدون وابستگی Catalog به Localization).</summary>
public interface IUnitOfMeasureLanguageGate
{
    /// <summary>همه LanguageIdها باید در رجیستری زبان شناخته‌شده باشند.</summary>
    Task EnsureKnownAsync(IReadOnlyList<Guid> languageIds, CancellationToken cancellationToken);
}

/// <summary>نوشتن واحد اندازه‌گیری روی مالک فعلی Catalog.</summary>
public interface IUnitOfMeasureDirectory
{
    /// <summary>واحد جدید.</summary>
    Task<Guid> CreateAsync(UnitOfMeasureWriteModel model, CancellationToken cancellationToken);

    /// <summary>ویرایش واحد و ترجمه‌ها.</summary>
    Task<Guid> UpdateAsync(Guid unitId, UnitOfMeasureWriteModel model, CancellationToken cancellationToken);

    /// <summary>غیرفعال‌سازی نرم.</summary>
    Task<(Guid UnitId, bool IsActive)> DeactivateAsync(Guid unitId, CancellationToken cancellationToken);
}

/// <summary>مدل ترجمهٔ واحد.</summary>
/// <param name="LanguageId">زبان.</param>
/// <param name="Name">نام.</param>
/// <param name="ShortName">نام کوتاه.</param>
public sealed record UnitOfMeasureTranslationWriteModel(Guid LanguageId, string Name, string ShortName);

/// <summary>مدل نوشتن واحد.</summary>
/// <param name="Code">کد.</param>
/// <param name="Dimension">بعد.</param>
/// <param name="IsActive">فعال؟</param>
/// <param name="SortOrder">ترتیب.</param>
/// <param name="Translations">ترجمه‌ها.</param>
public sealed record UnitOfMeasureWriteModel(
    string Code,
    string Dimension,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<UnitOfMeasureTranslationWriteModel> Translations);

/// <summary>فرمان ایجاد واحد.</summary>
/// <param name="Model">مدل.</param>
public sealed record CreateUnitOfMeasureCommand(UnitOfMeasureWriteModel Model) : IRequest<Guid>;

/// <summary>فرمان ویرایش واحد.</summary>
/// <param name="UnitId">شناسه.</param>
/// <param name="Model">مدل.</param>
public sealed record UpdateUnitOfMeasureCommand(Guid UnitId, UnitOfMeasureWriteModel Model) : IRequest<Guid>;

/// <summary>فرمان غیرفعال‌سازی واحد.</summary>
/// <param name="UnitId">شناسه.</param>
public sealed record DeactivateUnitOfMeasureCommand(Guid UnitId) : IRequest<(Guid UnitId, bool IsActive)>;
