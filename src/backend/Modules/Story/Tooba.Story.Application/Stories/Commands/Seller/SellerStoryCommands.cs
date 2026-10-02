using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Stories.Composition;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Presentation;

namespace Tooba.Story.Application.Stories.Commands.Seller;

/// <summary>ایجاد پیش‌نویس فروشنده.</summary>
public sealed record CreateSellerStoryDraftCommand(
    Guid TenantId, Guid SellerPartyId, Guid ActorUserId, CreateStoryCommand Input)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class CreateSellerStoryDraftCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<CreateSellerStoryDraftCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(
        CreateSellerStoryDraftCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerCreateDraftAsync(
                request.TenantId, request.SellerPartyId, request.ActorUserId, request.Input, cancellationToken));
}

/// <summary>به‌روزرسانی استوری فروشنده.</summary>
public sealed record UpdateSellerStoryCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, UpdateStoryCommand Input)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class UpdateSellerStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<UpdateSellerStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(UpdateSellerStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerUpdateAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.Input, cancellationToken));
}

/// <summary>ارسال برای بازبینی.</summary>
public sealed record SubmitSellerStoryCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, Guid ActorUserId)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class SubmitSellerStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<SubmitSellerStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(SubmitSellerStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerSubmitAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.ActorUserId, cancellationToken));
}

/// <summary>افزودن آیتم فروشنده.</summary>
public sealed record AddSellerStoryItemCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, AddStoryItemCommand Input)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class AddSellerStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<AddSellerStoryItemCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(AddSellerStoryItemCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerAddItemAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.Input, cancellationToken));
}

/// <summary>به‌روزرسانی آیتم فروشنده.</summary>
public sealed record UpdateSellerStoryItemCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, Guid ItemId, UpdateStoryItemCommand Input)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class UpdateSellerStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<UpdateSellerStoryItemCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(
        UpdateSellerStoryItemCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerUpdateItemAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.ItemId, request.Input,
                cancellationToken));
}

/// <summary>حذف آیتم فروشنده.</summary>
public sealed record RemoveSellerStoryItemCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, Guid ItemId)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class RemoveSellerStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<RemoveSellerStoryItemCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(
        RemoveSellerStoryItemCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerRemoveItemAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.ItemId, cancellationToken));
}

/// <summary>مرتب‌سازی آیتم‌های فروشنده.</summary>
public sealed record ReorderSellerStoryItemsCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, IReadOnlyList<Guid> ItemIds)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class ReorderSellerStoryItemsCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ReorderSellerStoryItemsCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(
        ReorderSellerStoryItemsCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerReorderItemsAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.ItemIds, cancellationToken));
}
