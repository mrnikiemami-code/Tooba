using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>جزئیات SEO محصول برای Admin و پیش‌نمایش مسیر عمومی.</summary>
public sealed record ProductSeoDetail(
    Guid ProductId,
    string Locale,
    string? Slug,
    string? SeoTitle,
    string? SeoDescription,
    string? ProductName,
    string? TitleFallback,
    string PublicPath,
    ProductSeoReadiness Readiness,
    DateTimeOffset UpdatedAt);
