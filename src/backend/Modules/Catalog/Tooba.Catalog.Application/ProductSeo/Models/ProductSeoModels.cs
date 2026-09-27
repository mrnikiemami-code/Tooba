namespace Tooba.Catalog.Application.ProductSeo.Models;

/// <summary>Admin HTTP body for product SEO update (locale + optimistic concurrency).</summary>
public sealed record UpdateProductSeoWriteModel(
    string Locale,
    string? Slug,
    string? SeoTitle,
    string? SeoDescription,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>SEO readiness view for Admin HTTP.</summary>
public sealed record ProductSeoReadinessView(
    bool HasValidSlug,
    bool HasSeoTitleOrFallback,
    bool HasSeoDescription,
    bool HasLocalizedIdentity,
    bool IsReady,
    string? MessageFa);

/// <summary>SEO detail view for Admin HTTP — preserves Host JSON property names.</summary>
public sealed record ProductSeoDetailView(
    Guid ProductId,
    string Locale,
    string? Slug,
    string? SeoTitle,
    string? SeoDescription,
    string? ProductName,
    string? TitleFallback,
    string PublicPath,
    ProductSeoReadinessView Readiness,
    DateTimeOffset UpdatedAt);
