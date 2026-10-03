using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// نمای برچسب تاکسونومی برای Admin (نام انسانی؛ نه GUID خام به‌عنوان UX اصلی).
/// </summary>
public sealed record TagView(
    Guid TagId,
    string Code,
    string? SlugSeam,
    CatalogPublicationStatus Status,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
