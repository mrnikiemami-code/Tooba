namespace Tooba.Catalog.Application.ProductTaxonomy.Models;

/// <summary>Body for PUT .../category (primary category assign).</summary>
public sealed record WorkspaceProductCategoryAssignWriteModel(
    Guid CategoryId,
    bool ConfirmSchemaImpact,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Body for POST .../categories/additional.</summary>
public sealed record WorkspaceProductAdditionalCategoryWriteModel(
    Guid CategoryId,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Body for PUT .../brand; null BrandId clears the brand.</summary>
public sealed record WorkspaceProductBrandAssignWriteModel(
    Guid? BrandId,
    DateTimeOffset ExpectedUpdatedAt);
