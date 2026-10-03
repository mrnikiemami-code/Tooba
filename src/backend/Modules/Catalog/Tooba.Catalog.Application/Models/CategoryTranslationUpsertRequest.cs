using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>درج/به‌روزرسانی ترجمهٔ یک locale.</summary>
public sealed record CategoryTranslationUpsertRequest(
    string Locale,
    string Name,
    string Slug,
    string? ShortDescription = null,
    string? Description = null,
    string? SeoTitle = null,
    string? SeoDescription = null,
    string? MetaKeywords = null);
