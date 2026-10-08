using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductIdentity.Commands;
using Tooba.Catalog.Application.ProductIdentity.Models;
using Tooba.Catalog.Application.ProductPublishing.Commands;
using Tooba.Catalog.Application.ProductTaxonomy.Commands;
using Tooba.Catalog.Application.ProductTaxonomy.Models;
using Tooba.Catalog.Application.Variants.Commands;
using Tooba.Catalog.Application.Variants.Models;
using Tooba.Catalog.Contracts.Ports;

namespace Tooba.Catalog.Infrastructure.Adapters;

/// <summary>
/// Catalog-owned implementation of <see cref="ICatalogAdminProductWorkspaceMutationGateway"/>.
/// The composing ProductWorkspace HTTP surface reaches Catalog product mutations through this
/// adapter only: it maps the narrow Contracts DTOs onto Catalog Application commands, dispatches
/// them with the canonical <see cref="ISender"/> pipeline and returns the handler's
/// <see cref="Result"/> unchanged (stable codes preserved 1:1).
/// <para>
/// Actor attribution is owner-side: the explicit <see cref="CatalogAdminProductWorkspaceActor"/>
/// supplied by the caller is bound onto the scoped <see cref="ICatalogActorContext"/> here, so the
/// consumer never depends on Catalog Application state.
/// </para>
/// </summary>
public sealed class CatalogAdminProductWorkspaceMutationGateway(
    ISender sender,
    ICatalogActorContext actorContext) : ICatalogAdminProductWorkspaceMutationGateway
{
    /// <inheritdoc />
    public Task<Result<Guid>> CreateProductAsync(
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceCreateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BindActor(actor);
        return sender.Send(
            new CreateWorkspaceProductCommand(
                new WorkspaceProductCreateWriteModel(
                    request.Title,
                    request.Slug,
                    request.CategoryId,
                    request.Locale)),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> UpdateCatalogTitleAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceCatalogTitleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BindActor(actor);
        return sender.Send(
            new UpdateProductCatalogTitleCommand(
                productId,
                new WorkspaceProductCatalogTitleWriteModel(
                    request.Locale,
                    request.Title,
                    request.ExpectedUpdatedAt)),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> UpdateCoreAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceCoreRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BindActor(actor);
        return sender.Send(
            new UpdateProductCoreCommand(
                productId,
                new WorkspaceProductCoreUpdateWriteModel(
                    request.Locale,
                    request.Title,
                    request.Slug,
                    request.ShortDescription,
                    request.Description,
                    request.SeoTitle,
                    request.SeoDescription,
                    request.ExpectedUpdatedAt)),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> UpdateQuantityPolicyAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceQuantityPolicyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BindActor(actor);
        return sender.Send(
            new UpdateProductQuantityPolicyCommand(
                productId,
                new WorkspaceProductQuantityPolicyWriteModel(
                    request.UnitOfMeasureId,
                    request.DecimalPlaces,
                    request.Step,
                    request.ExpectedUpdatedAt)),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> AssignPrimaryCategoryAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BindActor(actor);
        return sender.Send(
            new AssignProductCategoryCommand(
                productId,
                new WorkspaceProductCategoryAssignWriteModel(
                    request.CategoryId,
                    request.ConfirmSchemaImpact,
                    request.ExpectedUpdatedAt)),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> AddAdditionalCategoryAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceAdditionalCategoryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BindActor(actor);
        return sender.Send(
            new AddAdditionalCategoryCommand(
                productId,
                new WorkspaceProductAdditionalCategoryWriteModel(
                    request.CategoryId,
                    request.ExpectedUpdatedAt)),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> RemoveAdditionalCategoryAsync(
        Guid productId,
        Guid categoryId,
        CatalogAdminProductWorkspaceActor actor,
        DateTimeOffset expectedUpdatedAt,
        CancellationToken cancellationToken)
    {
        BindActor(actor);
        return sender.Send(
            new RemoveAdditionalCategoryCommand(productId, categoryId, expectedUpdatedAt),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> AssignBrandAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceBrandRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BindActor(actor);
        return sender.Send(
            new AssignProductBrandCommand(
                productId,
                new WorkspaceProductBrandAssignWriteModel(
                    request.BrandId,
                    request.ExpectedUpdatedAt)),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> PublishAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CancellationToken cancellationToken)
    {
        BindActor(actor);
        return sender.Send(new PublishProductCommand(productId), cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> UnpublishAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CancellationToken cancellationToken)
    {
        BindActor(actor);
        return sender.Send(new UnpublishProductCommand(productId), cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> ArchiveAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CancellationToken cancellationToken)
    {
        BindActor(actor);
        return sender.Send(new ArchiveProductCommand(productId), cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> RestoreAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CancellationToken cancellationToken)
    {
        BindActor(actor);
        return sender.Send(new RestoreProductCommand(productId), cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> CreateVariantAsync(
        Guid productId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceVariantCreateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BindActor(actor);
        var axes = request.Axes?
            .Select(a => new WorkspaceVariantAxisWriteModel(a.DefinitionId, a.RawValue, a.EnumOptionId))
            .ToList();
        return sender.Send(
            new CreateProductWorkspaceVariantCommand(
                productId,
                new WorkspaceVariantCreateWriteModel(request.CatalogCodeSeam, axes)),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> PatchVariantAsync(
        Guid productId,
        Guid variantId,
        CatalogAdminProductWorkspaceActor actor,
        CatalogAdminProductWorkspaceVariantPatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BindActor(actor);
        return sender.Send(
            new PatchProductWorkspaceVariantCommand(
                productId,
                variantId,
                new WorkspaceVariantPatchWriteModel(request.Status, request.CatalogCodeSeam)),
            cancellationToken);
    }

    private void BindActor(CatalogAdminProductWorkspaceActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        actorContext.ActorUserId = actor.ActorUserId;
        actorContext.ActorDisplayName = actor.ActorDisplayName;
    }
}
