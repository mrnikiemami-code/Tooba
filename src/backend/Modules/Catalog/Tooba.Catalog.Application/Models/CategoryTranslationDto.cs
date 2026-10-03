using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>ترجمهٔ رده بدون نشت EF.</summary>
public sealed record CategoryTranslationDto(
    Guid CategoryId,
    string Locale,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    string? MetaKeywords,
    DateTimeOffset UpdatedAt);
