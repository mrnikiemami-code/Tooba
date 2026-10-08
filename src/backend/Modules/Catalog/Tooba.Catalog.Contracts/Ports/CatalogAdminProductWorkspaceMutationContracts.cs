using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Contracts.Ports;

/// <summary>
/// Catalog-owned Admin product-workspace mutation boundary consumed by the ProductWorkspace
/// presentation module. Exposes only narrow transport DTOs plus a stable <see cref="Result"/>
/// outcome — never Catalog Application commands, Catalog Application write models, EF/domain
/// types or the scoped Catalog actor context.
/// <para>
/// The acting admin is an explicit parameter so the boundary stays stateless and can be
/// re-implemented as a remote call without ambient scoped state.
/// </para>
/// </summary>
public interface ICatalogAdminProductWorkspaceMutationGateway
{
    /// <summary>Creates a draft workspace product; returns the new product id.</summary>
    Task<Result<Guid>> CreateProductAsync(
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceCreateRequest request,
        CancellationToken cancellationToken);

    /// <summary>Updates a localized catalog product title.</summary>
    Task<Result> UpdateCatalogTitleAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceCatalogTitleRequest request,
        CancellationToken cancellationToken);

    /// <summary>Updates product core identity and locale translations.</summary>
    Task<Result> UpdateCoreAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceCoreRequest request,
        CancellationToken cancellationToken);

    /// <summary>Updates product quantity policy.</summary>
    Task<Result> UpdateQuantityPolicyAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceQuantityPolicyRequest request,
        CancellationToken cancellationToken);

    /// <summary>Assigns or replaces the product primary category.</summary>
    Task<Result> AssignPrimaryCategoryAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceCategoryRequest request,
        CancellationToken cancellationToken);

    /// <summary>Adds an additional (non-schema) category link.</summary>
    Task<Result> AddAdditionalCategoryAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceAdditionalCategoryRequest request,
        CancellationToken cancellationToken);

    /// <summary>Removes an additional category link.</summary>
    Task<Result> RemoveAdditionalCategoryAsync(
        Guid productId,
        Guid categoryId,
        CatalogAdminProductWorkspaceActor actor,
        DateTimeOffset expectedUpdatedAt,
        CancellationToken cancellationToken);

    /// <summary>Assigns or clears the product brand.</summary>
    Task<Result> AssignBrandAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceBrandRequest request,
        CancellationToken cancellationToken);

    /// <summary>Publishes the product.</summary>
    Task<Result> PublishAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CancellationToken cancellationToken);

    /// <summary>Unpublishes the product.</summary>
    Task<Result> UnpublishAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CancellationToken cancellationToken);

    /// <summary>Archives the product.</summary>
    Task<Result> ArchiveAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CancellationToken cancellationToken);

    /// <summary>Restores an archived product.</summary>
    Task<Result> RestoreAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CancellationToken cancellationToken);

    /// <summary>Creates one product variant from the Admin workspace.</summary>
    Task<Result> CreateVariantAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceVariantCreateRequest request,
        CancellationToken cancellationToken);

    /// <summary>Patches one product variant from the Admin workspace.</summary>
    Task<Result> PatchVariantAsync(
        Guid productId,
        Guid variantId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceVariantPatchRequest request,
        CancellationToken cancellationToken);
}

/// <summary>Acting Admin operator attribution for a Catalog workspace mutation.</summary>
public sealed record CatalogAdminProductWorkspaceActor(Guid ActorUserId, string ActorDisplayName);

/// <summary>Body for POST /v1/admin/products (workspace create).</summary>
public sealed record CatalogAdminProductWorkspaceCreateRequest(
    string Title,
    string? Slug,
    Guid? CategoryId,
    string? Locale);

/// <summary>Body for PATCH .../catalog-title.</summary>
public sealed record CatalogAdminProductWorkspaceCatalogTitleRequest(
    string Locale,
    string Title,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Body for PATCH .../core.</summary>
public sealed record CatalogAdminProductWorkspaceCoreRequest(
    string Locale,
    string Title,
    string? Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Body for PATCH .../quantity-policy.</summary>
public sealed record CatalogAdminProductWorkspaceQuantityPolicyRequest(
    Guid UnitOfMeasureId,
    int DecimalPlaces,
    decimal? Step,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Body for PUT .../category (primary category assign).</summary>
public sealed record CatalogAdminProductWorkspaceCategoryRequest(
    Guid CategoryId,
    bool ConfirmSchemaImpact,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Body for POST .../categories/additional.</summary>
public sealed record CatalogAdminProductWorkspaceAdditionalCategoryRequest(
    Guid CategoryId,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Body for PUT .../brand; null BrandId clears the brand.</summary>
public sealed record CatalogAdminProductWorkspaceBrandRequest(
    Guid? BrandId,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>Axis row for workspace single-variant create.</summary>
public sealed record CatalogAdminProductWorkspaceVariantAxis(
    Guid DefinitionId,
    string? RawValue,
    Guid? EnumOptionId);

/// <summary>Body for POST /v1/admin/products/{id}/variants (workspace).</summary>
public sealed record CatalogAdminProductWorkspaceVariantCreateRequest(
    string? CatalogCodeSeam,
    IReadOnlyList<CatalogAdminProductWorkspaceVariantAxis>? Axes);

/// <summary>Body for PATCH /v1/admin/products/{id}/variants/{variantId} (workspace).</summary>
public sealed record CatalogAdminProductWorkspaceVariantPatchRequest(string? Status, string? CatalogCodeSeam);
