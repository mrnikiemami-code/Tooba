using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Stories.Composition;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Presentation;

namespace Tooba.Story.Application.Stories.Commands.Admin;

/// <summary>ایجاد استوری ادمین.</summary>
public sealed record CreateAdminStoryCommand(Guid TenantId, CreateStoryCommand Input)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class CreateAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<CreateAdminStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(CreateAdminStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminCreateAsync(request.TenantId, request.Input, cancellationToken));
}

/// <summary>به‌روزرسانی استوری ادمین.</summary>
public sealed record UpdateAdminStoryCommand(Guid TenantId, Guid StoryId, UpdateStoryCommand Input)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class UpdateAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<UpdateAdminStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(UpdateAdminStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminUpdateAsync(request.TenantId, request.StoryId, request.Input, cancellationToken));
}

/// <summary>مرتب‌سازی استوری‌های ادمین.</summary>
public sealed record ReorderAdminStoriesCommand(Guid TenantId, IReadOnlyList<Guid> StoryIds)
    : IRequest<Result<IReadOnlyList<AdminStorySnapshot>>>;

public sealed class ReorderAdminStoriesCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ReorderAdminStoriesCommand, Result<IReadOnlyList<AdminStorySnapshot>>>
{
    public Task<Result<IReadOnlyList<AdminStorySnapshot>>> Handle(
        ReorderAdminStoriesCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminReorderStoriesAsync(request.TenantId, request.StoryIds, cancellationToken));
}

/// <summary>فعال‌سازی استوری.</summary>
public sealed record EnableAdminStoryCommand(Guid TenantId, Guid StoryId)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class EnableAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<EnableAdminStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(EnableAdminStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminEnableAsync(request.TenantId, request.StoryId, cancellationToken));
}

/// <summary>غیرفعال‌سازی استوری.</summary>
public sealed record DisableAdminStoryCommand(Guid TenantId, Guid StoryId)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class DisableAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<DisableAdminStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(DisableAdminStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminDisableAsync(request.TenantId, request.StoryId, cancellationToken));
}

/// <summary>زمان‌بندی استوری.</summary>
public sealed record ScheduleAdminStoryCommand(Guid TenantId, Guid StoryId, SetStoryScheduleCommand Input)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class ScheduleAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ScheduleAdminStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(ScheduleAdminStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminSetScheduleAsync(request.TenantId, request.StoryId, request.Input, cancellationToken));
}

/// <summary>تأیید استوری فروشنده.</summary>
public sealed record ApproveAdminStoryCommand(Guid TenantId, Guid StoryId, Guid AdminActorUserId)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class ApproveAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ApproveAdminStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(ApproveAdminStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminApproveAsync(
                request.TenantId, request.StoryId, request.AdminActorUserId, cancellationToken));
}

/// <summary>رد استوری فروشنده.</summary>
public sealed record RejectAdminStoryCommand(Guid TenantId, Guid StoryId, Guid AdminActorUserId, string Reason)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class RejectAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<RejectAdminStoryCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(RejectAdminStoryCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminRejectAsync(
                request.TenantId, request.StoryId, request.AdminActorUserId, request.Reason, cancellationToken));
}

/// <summary>افزودن آیتم ادمین.</summary>
public sealed record AddAdminStoryItemCommand(Guid TenantId, Guid StoryId, AddStoryItemCommand Input)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class AddAdminStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<AddAdminStoryItemCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(AddAdminStoryItemCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminAddItemAsync(request.TenantId, request.StoryId, request.Input, cancellationToken));
}

/// <summary>به‌روزرسانی آیتم ادمین.</summary>
public sealed record UpdateAdminStoryItemCommand(
    Guid TenantId, Guid StoryId, Guid ItemId, UpdateStoryItemCommand Input)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class UpdateAdminStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<UpdateAdminStoryItemCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(UpdateAdminStoryItemCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminUpdateItemAsync(
                request.TenantId, request.StoryId, request.ItemId, request.Input, cancellationToken));
}

/// <summary>حذف آیتم ادمین.</summary>
public sealed record RemoveAdminStoryItemCommand(Guid TenantId, Guid StoryId, Guid ItemId)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class RemoveAdminStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<RemoveAdminStoryItemCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(RemoveAdminStoryItemCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminRemoveItemAsync(request.TenantId, request.StoryId, request.ItemId, cancellationToken));
}

/// <summary>مرتب‌سازی آیتم‌های ادمین.</summary>
public sealed record ReorderAdminStoryItemsCommand(Guid TenantId, Guid StoryId, IReadOnlyList<Guid> ItemIds)
    : IRequest<Result<AdminStorySnapshot>>;

public sealed class ReorderAdminStoryItemsCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ReorderAdminStoryItemsCommand, Result<AdminStorySnapshot>>
{
    public Task<Result<AdminStorySnapshot>> Handle(
        ReorderAdminStoryItemsCommand request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.AdminReorderItemsAsync(request.TenantId, request.StoryId, request.ItemIds, cancellationToken));
}
