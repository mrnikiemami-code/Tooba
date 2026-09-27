namespace Tooba.Catalog.Application.ProductIdentity.Models;

/// <summary>Body for POST /v1/admin/products (workspace create).</summary>
public sealed record WorkspaceProductCreateWriteModel(
    string Title,
    string? Slug,
    Guid? CategoryId,
    string? Locale);

/// <summary>Body for PATCH .../catalog-title.</summary>
public sealed record WorkspaceProductCatalogTitleWriteModel(
    string Locale,
    string Title,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Body for PATCH .../core.</summary>
public sealed record WorkspaceProductCoreUpdateWriteModel(
    string Locale,
    string Title,
    string? Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Body for PATCH .../quantity-policy.</summary>
public sealed record WorkspaceProductQuantityPolicyWriteModel(
    Guid UnitOfMeasureId,
    int DecimalPlaces,
    decimal? Step,
    DateTimeOffset ExpectedUpdatedAt);
