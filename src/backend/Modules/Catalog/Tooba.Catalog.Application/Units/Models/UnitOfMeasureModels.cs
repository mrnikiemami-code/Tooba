namespace Tooba.Catalog.Application.Units.Models;

/// <summary>Translation row for unit write.</summary>
/// <param name="LanguageId">Registry language id.</param>
/// <param name="Name">Localized name.</param>
/// <param name="ShortName">Localized short name.</param>
public sealed record UnitOfMeasureTranslationWriteModel(Guid LanguageId, string Name, string ShortName);

/// <summary>Write model for create/update.</summary>
public sealed record UnitOfMeasureWriteModel(
    string Code,
    string Dimension,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<UnitOfMeasureTranslationWriteModel> Translations);

/// <summary>Grid row for Admin unit list.</summary>
public sealed record UnitOfMeasureListItem(
    Guid UnitOfMeasureId,
    string Code,
    string Dimension,
    string Name,
    string ShortName,
    bool IsActive,
    int SortOrder,
    bool IsReferenced);

/// <summary>Detail with all translations.</summary>
public sealed record UnitOfMeasureDetail(
    Guid UnitOfMeasureId,
    string Code,
    string Dimension,
    bool IsActive,
    int SortOrder,
    bool IsReferenced,
    IReadOnlyList<UnitOfMeasureTranslationWriteModel> Translations);

/// <summary>Create/update success payload.</summary>
/// <param name="UnitOfMeasureId">Persisted unit id.</param>
public sealed record UnitOfMeasureIdResult(Guid UnitOfMeasureId);

/// <summary>Deactivate success payload.</summary>
public sealed record UnitOfMeasureDeactivateResult(Guid UnitOfMeasureId, bool IsActive);
