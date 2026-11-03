namespace Tooba.Catalog.Application.ProductIdentity.Models;

/// <summary>Body for POST /v1/admin/products (workspace create).</summary>
public sealed record WorkspaceProductCreateWriteModel(
    string Title,
    string? Slug,
    Guid? CategoryId,
    string? Locale,
    decimal? WeightGrams = null,
    decimal? LengthCentimeters = null,
    decimal? WidthCentimeters = null,
    decimal? HeightCentimeters = null);

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

/// <summary>Body for PATCH .../physical-specification. Null = unspecified (clears prior).</summary>
public sealed record WorkspaceProductPhysicalSpecificationWriteModel(
    decimal? WeightGrams,
    decimal? LengthCentimeters,
    decimal? WidthCentimeters,
    decimal? HeightCentimeters,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Body for variant physical-specification override. Null field = inherit product.</summary>
public sealed record WorkspaceVariantPhysicalSpecificationWriteModel(
    decimal? WeightGrams,
    decimal? LengthCentimeters,
    decimal? WidthCentimeters,
    decimal? HeightCentimeters);
