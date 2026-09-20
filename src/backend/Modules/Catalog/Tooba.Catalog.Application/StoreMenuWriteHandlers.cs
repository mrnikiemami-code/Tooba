using MediatR;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application;

/// <summary>Handlerهای CQRS نوشتن منو — Command → Directory.</summary>
public sealed class CreateStoreMenuHandler : IRequestHandler<CreateStoreMenuCommand, StoreMenu>
{
    private readonly IStoreMenuDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public CreateStoreMenuHandler(IStoreMenuDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreMenu> Handle(CreateStoreMenuCommand request, CancellationToken cancellationToken)
        => _directory.CreateAsync(request.Model, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class UpdateStoreMenuHandler : IRequestHandler<UpdateStoreMenuCommand, StoreMenu>
{
    private readonly IStoreMenuDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public UpdateStoreMenuHandler(IStoreMenuDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreMenu> Handle(UpdateStoreMenuCommand request, CancellationToken cancellationToken)
        => _directory.UpdateAsync(request.MenuId, request.Model, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreMenuEnabledHandler : IRequestHandler<SetStoreMenuEnabledCommand, StoreMenu>
{
    private readonly IStoreMenuDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public SetStoreMenuEnabledHandler(IStoreMenuDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreMenu> Handle(SetStoreMenuEnabledCommand request, CancellationToken cancellationToken)
        => _directory.SetEnabledAsync(request.MenuId, request.Enabled, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class DeleteStoreMenuHandler : IRequestHandler<DeleteStoreMenuCommand, Unit>
{
    private readonly IStoreMenuDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public DeleteStoreMenuHandler(IStoreMenuDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Unit> Handle(DeleteStoreMenuCommand request, CancellationToken cancellationToken)
    {
        await _directory.DeleteAsync(request.MenuId, cancellationToken);
        return Unit.Value;
    }
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class AddStoreMenuItemHandler : IRequestHandler<AddStoreMenuItemCommand, StoreMenuItem>
{
    private readonly IStoreMenuDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public AddStoreMenuItemHandler(IStoreMenuDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreMenuItem> Handle(AddStoreMenuItemCommand request, CancellationToken cancellationToken)
        => _directory.AddItemAsync(request.MenuId, request.Model, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class UpdateStoreMenuItemHandler : IRequestHandler<UpdateStoreMenuItemCommand, StoreMenuItem>
{
    private readonly IStoreMenuDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public UpdateStoreMenuItemHandler(IStoreMenuDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreMenuItem> Handle(UpdateStoreMenuItemCommand request, CancellationToken cancellationToken)
        => _directory.UpdateItemAsync(request.MenuId, request.MenuItemId, request.Model, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreMenuItemEnabledHandler : IRequestHandler<SetStoreMenuItemEnabledCommand, StoreMenuItem>
{
    private readonly IStoreMenuDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public SetStoreMenuItemEnabledHandler(IStoreMenuDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreMenuItem> Handle(SetStoreMenuItemEnabledCommand request, CancellationToken cancellationToken)
        => _directory.SetItemEnabledAsync(request.MenuId, request.MenuItemId, request.Enabled, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class DeleteStoreMenuItemHandler : IRequestHandler<DeleteStoreMenuItemCommand, Unit>
{
    private readonly IStoreMenuDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public DeleteStoreMenuItemHandler(IStoreMenuDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Unit> Handle(DeleteStoreMenuItemCommand request, CancellationToken cancellationToken)
    {
        await _directory.DeleteItemAsync(request.MenuId, request.MenuItemId, cancellationToken);
        return Unit.Value;
    }
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class ReorderStoreMenuItemsHandler : IRequestHandler<ReorderStoreMenuItemsCommand, StoreMenu>
{
    private readonly IStoreMenuDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public ReorderStoreMenuItemsHandler(IStoreMenuDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreMenu> Handle(ReorderStoreMenuItemsCommand request, CancellationToken cancellationToken)
        => _directory.ReorderItemsAsync(request.MenuId, request.OrderedIds, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreHeaderMenuHandler : IRequestHandler<SetStoreHeaderMenuCommand, Unit>
{
    private readonly IStoreMenuDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public SetStoreHeaderMenuHandler(IStoreMenuDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Unit> Handle(SetStoreHeaderMenuCommand request, CancellationToken cancellationToken)
    {
        await _directory.SetHeaderAsync(request.HeaderMenuId, cancellationToken);
        return Unit.Value;
    }
}
