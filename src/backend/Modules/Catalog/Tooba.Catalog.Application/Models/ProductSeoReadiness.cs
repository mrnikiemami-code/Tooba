using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>آمادگی SEO محصول — جدا از Media و بدون وابستگی تجاری.</summary>
public sealed record ProductSeoReadiness(
    bool HasValidSlug,
    bool HasSeoTitleOrFallback,
    bool HasSeoDescription,
    bool HasLocalizedIdentity,
    bool IsReady,
    string? MessageFa);
