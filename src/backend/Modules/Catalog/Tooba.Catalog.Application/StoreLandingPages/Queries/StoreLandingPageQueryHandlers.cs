using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.StoreLandingPages.Models;
using Tooba.Catalog.Application.StoreLandingPages.Ports;
using Tooba.Catalog.Contracts.Errors;

namespace Tooba.Catalog.Application.StoreLandingPages.Queries;

/// <summary>Handlers خواندن Landing — workspace + Result.</summary>
public sealed class ListStoreLandingPagesHandler
    : IRequestHandler<ListStoreLandingPagesQuery, Result<IReadOnlyList<StoreLandingPageAdminView>>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public ListStoreLandingPagesHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoreLandingPageAdminView>>> Handle(
        ListStoreLandingPagesQuery request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() => _workspace.ListAsync(cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class GetStoreLandingPageHandler
    : IRequestHandler<GetStoreLandingPageQuery, Result<StoreLandingPageAdminView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public GetStoreLandingPageHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreLandingPageAdminView>> Handle(
        GetStoreLandingPageQuery request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() => _workspace.GetAsync(request.PageId, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class PreviewStoreLandingPageHandler
    : IRequestHandler<PreviewStoreLandingPageQuery, Result<StoreLandingPagePublicView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public PreviewStoreLandingPageHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreLandingPagePublicView>> Handle(
        PreviewStoreLandingPageQuery request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() => _workspace.ResolvePreviewAsync(request.PageId, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class ListStoreLandingPageSectionsHandler
    : IRequestHandler<ListStoreLandingPageSectionsQuery, Result<IReadOnlyList<StoreLandingPageSectionAdminView>>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public ListStoreLandingPageSectionsHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoreLandingPageSectionAdminView>>> Handle(
        ListStoreLandingPageSectionsQuery request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() => _workspace.ListSectionsAsync(request.PageId, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class GetStoreHomeSelectionHandler
    : IRequestHandler<GetStoreHomeSelectionQuery, Result<StoreHomeSelectionView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public GetStoreHomeSelectionHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreHomeSelectionView>> Handle(
        GetStoreHomeSelectionQuery request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() => _workspace.GetHomeSelectionAsync(cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class ResolvePublicStoreLandingPageHandler
    : IRequestHandler<ResolvePublicStoreLandingPageQuery, Result<StoreLandingPagePublicView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public ResolvePublicStoreLandingPageHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public async Task<Result<StoreLandingPagePublicView>> Handle(
        ResolvePublicStoreLandingPageQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = await _workspace.ResolvePublicAsync(request.Locale, request.Slug, cancellationToken);
            return page is null
                ? Result.Failure<StoreLandingPagePublicView>(new SemanticError(CatalogErrorCodes.LandingPageMissing))
                : Result.Success(page);
        }
        catch (PlatformHttpException ex) when (!string.IsNullOrWhiteSpace(ex.ErrorCode))
        {
            return Result.Failure<StoreLandingPagePublicView>(new SemanticError(ex.ErrorCode!));
        }
    }
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class ListStoreLandingSitemapHandler
    : IRequestHandler<ListStoreLandingSitemapQuery, Result<IReadOnlyList<StoreLandingSitemapEntry>>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public ListStoreLandingSitemapHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoreLandingSitemapEntry>>> Handle(
        ListStoreLandingSitemapQuery request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() => _workspace.ListIndexableLandingsAsync(cancellationToken));
}
