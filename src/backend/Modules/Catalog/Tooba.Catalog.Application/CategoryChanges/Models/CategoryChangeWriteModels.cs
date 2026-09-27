namespace Tooba.Catalog.Application.CategoryChanges.Models;

/// <summary>Transport body for primary-category replace.</summary>
public sealed record CategoryChangeWriteModel(Guid NewCategoryId);

/// <summary>Transport body for category-change preview (locale optional; defaulted fa-IR).</summary>
public sealed record CategoryChangePreviewWriteModel(Guid NewCategoryId, string? Locale);
