using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.StoreMenus.Models;
using Tooba.Catalog.Application.StoreMenus.Ports;

namespace Tooba.Catalog.Application.StoreMenus.Queries;

/// <summary>Handlers خواندن Store Menu — workspace + Result.</summary>
public sealed class ListStoreMenusHandler
    : IRequestHandler<ListStoreMenusQuery, Result<IReadOnlyList<StoreMenuListView>>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public ListStoreMenusHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoreMenuListView>>> Handle(
        ListStoreMenusQuery request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() => _workspace.ListAsync(cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class GetStoreMenuHandler
    : IRequestHandler<GetStoreMenuQuery, Result<StoreMenuDetailView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public GetStoreMenuHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreMenuDetailView>> Handle(
        GetStoreMenuQuery request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() => _workspace.GetAsync(request.MenuId, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class GetStoreMenuUsageHandler
    : IRequestHandler<GetStoreMenuUsageQuery, Result<IReadOnlyList<StoreMenuUsageView>>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public GetStoreMenuUsageHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoreMenuUsageView>>> Handle(
        GetStoreMenuUsageQuery request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() => _workspace.GetUsageAsync(request.MenuId, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class GetStoreHeaderMenuSelectionHandler
    : IRequestHandler<GetStoreHeaderMenuSelectionQuery, Result<StoreHeaderMenuSelectionView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public GetStoreHeaderMenuSelectionHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreHeaderMenuSelectionView>> Handle(
        GetStoreHeaderMenuSelectionQuery request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() => _workspace.GetHeaderSelectionAsync(cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class GetStoreHeaderMenuPublicHandler
    : IRequestHandler<GetStoreHeaderMenuPublicQuery, Result<StoreHeaderMenuSelectionView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public GetStoreHeaderMenuPublicHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreHeaderMenuSelectionView>> Handle(
        GetStoreHeaderMenuPublicQuery request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() => _workspace.GetHeaderPublicAsync(cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class ProjectPublicStoreMenuHandler
    : IRequestHandler<ProjectPublicStoreMenuQuery, Result<IReadOnlyList<StoreMenuPublicItemView>>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public ProjectPublicStoreMenuHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoreMenuPublicItemView>>> Handle(
        ProjectPublicStoreMenuQuery request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() => _workspace.ProjectPublicAsync(request.MenuId, cancellationToken));
}
