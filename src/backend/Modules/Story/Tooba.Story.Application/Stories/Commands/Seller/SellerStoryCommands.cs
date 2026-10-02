using MediatR;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Presentation;

namespace Tooba.Story.Application.Stories.Commands.Seller;

/// <summary>ایجاد پیش‌نویس فروشنده.</summary>
public sealed record CreateSellerStoryDraftCommand(
    Guid TenantId, Guid SellerPartyId, Guid ActorUserId, CreateStoryCommand Input) : IRequest<AdminStorySnapshot>;

public sealed class CreateSellerStoryDraftCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<CreateSellerStoryDraftCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(CreateSellerStoryDraftCommand request, CancellationToken cancellationToken)
        => composer.SellerCreateDraftAsync(
            request.TenantId, request.SellerPartyId, request.ActorUserId, request.Input, cancellationToken);
}

/// <summary>به‌روزرسانی استوری فروشنده.</summary>
public sealed record UpdateSellerStoryCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, UpdateStoryCommand Input) : IRequest<AdminStorySnapshot>;

public sealed class UpdateSellerStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<UpdateSellerStoryCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(UpdateSellerStoryCommand request, CancellationToken cancellationToken)
        => composer.SellerUpdateAsync(
            request.TenantId, request.SellerPartyId, request.StoryId, request.Input, cancellationToken);
}

/// <summary>ارسال برای بازبینی.</summary>
public sealed record SubmitSellerStoryCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, Guid ActorUserId) : IRequest<AdminStorySnapshot>;

public sealed class SubmitSellerStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<SubmitSellerStoryCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(SubmitSellerStoryCommand request, CancellationToken cancellationToken)
        => composer.SellerSubmitAsync(
            request.TenantId, request.SellerPartyId, request.StoryId, request.ActorUserId, cancellationToken);
}

/// <summary>افزودن آیتم فروشنده.</summary>
public sealed record AddSellerStoryItemCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, AddStoryItemCommand Input) : IRequest<AdminStorySnapshot>;

public sealed class AddSellerStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<AddSellerStoryItemCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(AddSellerStoryItemCommand request, CancellationToken cancellationToken)
        => composer.SellerAddItemAsync(
            request.TenantId, request.SellerPartyId, request.StoryId, request.Input, cancellationToken);
}

/// <summary>به‌روزرسانی آیتم فروشنده.</summary>
public sealed record UpdateSellerStoryItemCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, Guid ItemId, UpdateStoryItemCommand Input)
    : IRequest<AdminStorySnapshot>;

public sealed class UpdateSellerStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<UpdateSellerStoryItemCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(UpdateSellerStoryItemCommand request, CancellationToken cancellationToken)
        => composer.SellerUpdateItemAsync(
            request.TenantId, request.SellerPartyId, request.StoryId, request.ItemId, request.Input, cancellationToken);
}

/// <summary>حذف آیتم فروشنده.</summary>
public sealed record RemoveSellerStoryItemCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, Guid ItemId) : IRequest<AdminStorySnapshot>;

public sealed class RemoveSellerStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<RemoveSellerStoryItemCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(RemoveSellerStoryItemCommand request, CancellationToken cancellationToken)
        => composer.SellerRemoveItemAsync(
            request.TenantId, request.SellerPartyId, request.StoryId, request.ItemId, cancellationToken);
}

/// <summary>مرتب‌سازی آیتم‌های فروشنده.</summary>
public sealed record ReorderSellerStoryItemsCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, IReadOnlyList<Guid> ItemIds) : IRequest<AdminStorySnapshot>;

public sealed class ReorderSellerStoryItemsCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ReorderSellerStoryItemsCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(ReorderSellerStoryItemsCommand request, CancellationToken cancellationToken)
        => composer.SellerReorderItemsAsync(
            request.TenantId, request.SellerPartyId, request.StoryId, request.ItemIds, cancellationToken);
}
