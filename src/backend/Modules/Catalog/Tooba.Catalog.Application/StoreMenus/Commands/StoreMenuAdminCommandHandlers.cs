using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.StoreMenus.Models;
using Tooba.Catalog.Application.StoreMenus.Ports;

namespace Tooba.Catalog.Application.StoreMenus.Commands;

/// <summary>Handlers نوشتن Store Menu Admin — workspace + Result.</summary>
public sealed class CreateStoreMenuAdminHandler
    : IRequestHandler<CreateStoreMenuAdminCommand, Result<StoreMenuDetailView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public CreateStoreMenuAdminHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreMenuDetailView>> Handle(
        CreateStoreMenuAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() => _workspace.CreateAsync(request.Body, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class UpdateStoreMenuAdminHandler
    : IRequestHandler<UpdateStoreMenuAdminCommand, Result<StoreMenuDetailView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public UpdateStoreMenuAdminHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreMenuDetailView>> Handle(
        UpdateStoreMenuAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() =>
            _workspace.UpdateAsync(request.MenuId, request.Body, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreMenuEnabledAdminHandler
    : IRequestHandler<SetStoreMenuEnabledAdminCommand, Result<StoreMenuDetailView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public SetStoreMenuEnabledAdminHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreMenuDetailView>> Handle(
        SetStoreMenuEnabledAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() =>
            _workspace.SetEnabledAsync(request.MenuId, request.Enabled, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class DeleteStoreMenuAdminHandler
    : IRequestHandler<DeleteStoreMenuAdminCommand, Result<StoreMenuDeletedView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public DeleteStoreMenuAdminHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreMenuDeletedView>> Handle(
        DeleteStoreMenuAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(async () =>
        {
            await _workspace.DeleteAsync(request.MenuId, cancellationToken);
            return new StoreMenuDeletedView();
        });
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class AddStoreMenuItemAdminHandler
    : IRequestHandler<AddStoreMenuItemAdminCommand, Result<StoreMenuItemAdminView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public AddStoreMenuItemAdminHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreMenuItemAdminView>> Handle(
        AddStoreMenuItemAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() =>
            _workspace.AddItemAsync(request.MenuId, request.Body, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class UpdateStoreMenuItemAdminHandler
    : IRequestHandler<UpdateStoreMenuItemAdminCommand, Result<StoreMenuItemAdminView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public UpdateStoreMenuItemAdminHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreMenuItemAdminView>> Handle(
        UpdateStoreMenuItemAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() =>
            _workspace.UpdateItemAsync(request.MenuId, request.ItemId, request.Body, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreMenuItemEnabledAdminHandler
    : IRequestHandler<SetStoreMenuItemEnabledAdminCommand, Result<StoreMenuItemAdminView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public SetStoreMenuItemEnabledAdminHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreMenuItemAdminView>> Handle(
        SetStoreMenuItemEnabledAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() =>
            _workspace.SetItemEnabledAsync(request.MenuId, request.ItemId, request.Enabled, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class DeleteStoreMenuItemAdminHandler
    : IRequestHandler<DeleteStoreMenuItemAdminCommand, Result<StoreMenuDeletedView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public DeleteStoreMenuItemAdminHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreMenuDeletedView>> Handle(
        DeleteStoreMenuItemAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(async () =>
        {
            await _workspace.DeleteItemAsync(request.MenuId, request.ItemId, cancellationToken);
            return new StoreMenuDeletedView();
        });
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class ReorderStoreMenuItemsAdminHandler
    : IRequestHandler<ReorderStoreMenuItemsAdminCommand, Result<StoreMenuDetailView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public ReorderStoreMenuItemsAdminHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreMenuDetailView>> Handle(
        ReorderStoreMenuItemsAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() =>
            _workspace.ReorderItemsAsync(request.MenuId, request.ItemIds, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreHeaderMenuAdminHandler
    : IRequestHandler<SetStoreHeaderMenuAdminCommand, Result<StoreHeaderMenuSelectionView>>
{
    private readonly IStoreMenuWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public SetStoreHeaderMenuAdminHandler(IStoreMenuWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreHeaderMenuSelectionView>> Handle(
        SetStoreHeaderMenuAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreMenuPlatformResult.CaptureAsync(() =>
            _workspace.SetHeaderAsync(request.HeaderMenuId, cancellationToken));
}
