using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>ورودی به‌روزرسانی SEO محصول (locale + قفل خوش‌بینانه).</summary>
public sealed record ProductSeoUpdateInput(
    string Locale,
    string? Slug,
    string? SeoTitle,
    string? SeoDescription,
    DateTimeOffset ExpectedUpdatedAt);
