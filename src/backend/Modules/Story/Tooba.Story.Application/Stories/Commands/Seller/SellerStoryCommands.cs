using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Composition;
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

/// <summary>حذف کل استوری قابل‌ویرایش فروشنده.</summary>
public sealed record DeleteSellerStoryCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, int? ExpectedVersion, string? OccurrenceKey)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class DeleteSellerStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<DeleteSellerStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(DeleteSellerStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerDeleteAsync(
                request.TenantId, request.SellerPartyId, request.StoryId,
                request.ExpectedVersion, request.OccurrenceKey, cancellationToken));
}

/// <summary>ارسال برای بازبینی.</summary>
public sealed record SubmitSellerStoryCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, Guid ActorUserId,
    int? ExpectedVersion = null, string? OccurrenceKey = null)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class SubmitSellerStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<SubmitSellerStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(SubmitSellerStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerSubmitAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.ActorUserId, cancellationToken,
                request.ExpectedVersion, request.OccurrenceKey));
}

/// <summary>بازگشت فروشنده به پیش‌نویس.</summary>
public sealed record ReturnSellerStoryToDraftCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, int? ExpectedVersion, string? OccurrenceKey = null)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class ReturnSellerStoryToDraftCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ReturnSellerStoryToDraftCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(
        ReturnSellerStoryToDraftCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerReturnToDraftAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.ExpectedVersion, cancellationToken,
                request.OccurrenceKey));
}

/// <summary>توقف انتشار فروشنده.</summary>
public sealed record StopSellerStoryCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, int? ExpectedVersion, string? OccurrenceKey = null)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class StopSellerStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<StopSellerStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(StopSellerStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerStopAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.ExpectedVersion, cancellationToken,
                request.OccurrenceKey));
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
    Guid TenantId, Guid SellerPartyId, Guid StoryId, Guid ItemId, int? ExpectedVersion, string? OccurrenceKey)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class RemoveSellerStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<RemoveSellerStoryItemCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(
        RemoveSellerStoryItemCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerRemoveItemAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.ItemId,
                request.ExpectedVersion, request.OccurrenceKey, cancellationToken));
}

/// <summary>مرتب‌سازی آیتم‌های فروشنده.</summary>
public sealed record ReorderSellerStoryItemsCommand(
    Guid TenantId, Guid SellerPartyId, Guid StoryId, IReadOnlyList<Guid> ItemIds, int? ExpectedVersion, string? OccurrenceKey)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class ReorderSellerStoryItemsCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ReorderSellerStoryItemsCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(
        ReorderSellerStoryItemsCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerReorderItemsAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, request.ItemIds,
                request.ExpectedVersion, request.OccurrenceKey, cancellationToken));
}
