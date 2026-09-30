using MediatR;
using Tooba.Story.Application.Presentation;

namespace Tooba.Story.Application.Commands.Admin;

/// <summary>ایجاد استوری ادمین.</summary>
public sealed record CreateAdminStoryCommand(Guid TenantId, CreateStoryCommand Input) : IRequest<AdminStorySnapshot>;

public sealed class CreateAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<CreateAdminStoryCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(CreateAdminStoryCommand request, CancellationToken cancellationToken)
        => composer.AdminCreateAsync(request.TenantId, request.Input, cancellationToken);
}

/// <summary>به‌روزرسانی استوری ادمین.</summary>
public sealed record UpdateAdminStoryCommand(Guid TenantId, Guid StoryId, UpdateStoryCommand Input)
    : IRequest<AdminStorySnapshot>;

public sealed class UpdateAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<UpdateAdminStoryCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(UpdateAdminStoryCommand request, CancellationToken cancellationToken)
        => composer.AdminUpdateAsync(request.TenantId, request.StoryId, request.Input, cancellationToken);
}

/// <summary>مرتب‌سازی استوری‌های ادمین.</summary>
public sealed record ReorderAdminStoriesCommand(Guid TenantId, IReadOnlyList<Guid> StoryIds)
    : IRequest<IReadOnlyList<AdminStorySnapshot>>;

public sealed class ReorderAdminStoriesCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ReorderAdminStoriesCommand, IReadOnlyList<AdminStorySnapshot>>
{
    public Task<IReadOnlyList<AdminStorySnapshot>> Handle(ReorderAdminStoriesCommand request, CancellationToken cancellationToken)
        => composer.AdminReorderStoriesAsync(request.TenantId, request.StoryIds, cancellationToken);
}

/// <summary>فعال‌سازی استوری.</summary>
public sealed record EnableAdminStoryCommand(Guid TenantId, Guid StoryId) : IRequest<AdminStorySnapshot>;

public sealed class EnableAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<EnableAdminStoryCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(EnableAdminStoryCommand request, CancellationToken cancellationToken)
        => composer.AdminEnableAsync(request.TenantId, request.StoryId, cancellationToken);
}

/// <summary>غیرفعال‌سازی استوری.</summary>
public sealed record DisableAdminStoryCommand(Guid TenantId, Guid StoryId) : IRequest<AdminStorySnapshot>;

public sealed class DisableAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<DisableAdminStoryCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(DisableAdminStoryCommand request, CancellationToken cancellationToken)
        => composer.AdminDisableAsync(request.TenantId, request.StoryId, cancellationToken);
}

/// <summary>زمان‌بندی استوری.</summary>
public sealed record ScheduleAdminStoryCommand(Guid TenantId, Guid StoryId, SetStoryScheduleCommand Input)
    : IRequest<AdminStorySnapshot>;

public sealed class ScheduleAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ScheduleAdminStoryCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(ScheduleAdminStoryCommand request, CancellationToken cancellationToken)
        => composer.AdminSetScheduleAsync(request.TenantId, request.StoryId, request.Input, cancellationToken);
}

/// <summary>تأیید استوری فروشنده.</summary>
public sealed record ApproveAdminStoryCommand(Guid TenantId, Guid StoryId, Guid AdminActorUserId)
    : IRequest<AdminStorySnapshot>;

public sealed class ApproveAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ApproveAdminStoryCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(ApproveAdminStoryCommand request, CancellationToken cancellationToken)
        => composer.AdminApproveAsync(request.TenantId, request.StoryId, request.AdminActorUserId, cancellationToken);
}

/// <summary>رد استوری فروشنده.</summary>
public sealed record RejectAdminStoryCommand(Guid TenantId, Guid StoryId, Guid AdminActorUserId, string Reason)
    : IRequest<AdminStorySnapshot>;

public sealed class RejectAdminStoryCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<RejectAdminStoryCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(RejectAdminStoryCommand request, CancellationToken cancellationToken)
        => composer.AdminRejectAsync(
            request.TenantId, request.StoryId, request.AdminActorUserId, request.Reason, cancellationToken);
}

/// <summary>افزودن آیتم ادمین.</summary>
public sealed record AddAdminStoryItemCommand(Guid TenantId, Guid StoryId, AddStoryItemCommand Input)
    : IRequest<AdminStorySnapshot>;

public sealed class AddAdminStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<AddAdminStoryItemCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(AddAdminStoryItemCommand request, CancellationToken cancellationToken)
        => composer.AdminAddItemAsync(request.TenantId, request.StoryId, request.Input, cancellationToken);
}

/// <summary>به‌روزرسانی آیتم ادمین.</summary>
public sealed record UpdateAdminStoryItemCommand(
    Guid TenantId, Guid StoryId, Guid ItemId, UpdateStoryItemCommand Input) : IRequest<AdminStorySnapshot>;

public sealed class UpdateAdminStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<UpdateAdminStoryItemCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(UpdateAdminStoryItemCommand request, CancellationToken cancellationToken)
        => composer.AdminUpdateItemAsync(
            request.TenantId, request.StoryId, request.ItemId, request.Input, cancellationToken);
}

/// <summary>حذف آیتم ادمین.</summary>
public sealed record RemoveAdminStoryItemCommand(Guid TenantId, Guid StoryId, Guid ItemId)
    : IRequest<AdminStorySnapshot>;

public sealed class RemoveAdminStoryItemCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<RemoveAdminStoryItemCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(RemoveAdminStoryItemCommand request, CancellationToken cancellationToken)
        => composer.AdminRemoveItemAsync(request.TenantId, request.StoryId, request.ItemId, cancellationToken);
}

/// <summary>مرتب‌سازی آیتم‌های ادمین.</summary>
public sealed record ReorderAdminStoryItemsCommand(Guid TenantId, Guid StoryId, IReadOnlyList<Guid> ItemIds)
    : IRequest<AdminStorySnapshot>;

public sealed class ReorderAdminStoryItemsCommandHandler(StoryPresentationComposer composer)
    : IRequestHandler<ReorderAdminStoryItemsCommand, AdminStorySnapshot>
{
    public Task<AdminStorySnapshot> Handle(ReorderAdminStoryItemsCommand request, CancellationToken cancellationToken)
        => composer.AdminReorderItemsAsync(request.TenantId, request.StoryId, request.ItemIds, cancellationToken);
}
