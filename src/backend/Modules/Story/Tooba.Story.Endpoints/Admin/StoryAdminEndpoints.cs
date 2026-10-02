using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Stories.Commands.Admin;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Queries.Admin;
using Tooba.Story.Endpoints.Models;

namespace Tooba.Story.Endpoints.Admin;

/// <summary>مرز HTTP مدیریتی Story.</summary>
public static class StoryAdminEndpoints
{
    /// <summary>مسیرهای Admin را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var admin = app.MapGroup("/v1/admin/stories");
        admin.MapGet("", AdminListAsync);
        admin.MapPost("/query", AdminQueryGridAsync);
        admin.MapGet("/{id:guid}", AdminGetAsync);
        admin.MapPost("", AdminCreateAsync);
        admin.MapPut("/{id:guid}", AdminUpdateAsync);
        admin.MapPut("/reorder", AdminReorderAsync);
        admin.MapPost("/{id:guid}/enable", AdminEnableAsync);
        admin.MapPost("/{id:guid}/disable", AdminDisableAsync);
        admin.MapPost("/{id:guid}/schedule", AdminScheduleAsync);
        admin.MapPost("/{id:guid}/approve", AdminApproveAsync);
        admin.MapPost("/{id:guid}/reject", AdminRejectAsync);
        admin.MapPost("/{id:guid}/items", AdminAddItemAsync);
        admin.MapPut("/{id:guid}/items/{itemId:guid}", AdminUpdateItemAsync);
        admin.MapDelete("/{id:guid}/items/{itemId:guid}", AdminRemoveItemAsync);
        admin.MapPut("/{id:guid}/items/reorder", AdminReorderItemsAsync);
    }

    private static async Task<IResult> AdminListAsync(
        ISender sender,
        IStoryAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string? reviewStatus = null,
        bool pendingReview = false,
        CancellationToken cancellationToken = default)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        var tenantResult = StoryHttpErrors.ResolveTenantId(tenant);
        if (tenantResult.IsFailure)
            return api.From(tenantResult);
        return api.From(await sender.Send(
            new ListAdminStoriesQuery(tenantResult.Value, reviewStatus, pendingReview), cancellationToken));
    }

    private static async Task<IResult> AdminQueryGridAsync(
        GridQueryRequest body,
        ISender sender,
        IStoryAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string? reviewStatus = null,
        CancellationToken cancellationToken = default)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        var tenantResult = StoryHttpErrors.ResolveTenantId(tenant);
        if (tenantResult.IsFailure)
            return api.From(tenantResult);
        return api.From(await sender.Send(
            new QueryAdminStoryGridQuery(tenantResult.Value, reviewStatus, body), cancellationToken));
    }

    private static async Task<IResult> AdminGetAsync(
        Guid id,
        ISender sender,
        IStoryAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        var tenantResult = StoryHttpErrors.ResolveTenantId(tenant);
        if (tenantResult.IsFailure)
            return api.From(tenantResult);
        return api.From(await sender.Send(new GetAdminStoryQuery(tenantResult.Value, id), cancellationToken));
    }

    private static Task<IResult> AdminCreateAsync(
        CreateStoryBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(new CreateAdminStoryCommand(tenantId, StoryBodyMapping.ToCreate(body)), cancellationToken),
            createdLocation: result => $"/v1/admin/stories/{result.StoryId}");

    private static Task<IResult> AdminUpdateAsync(
        Guid id, UpdateStoryBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(new UpdateAdminStoryCommand(tenantId, id, StoryBodyMapping.ToUpdate(body)), cancellationToken));

    private static Task<IResult> AdminReorderAsync(
        ReorderStoriesBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(new ReorderAdminStoriesCommand(tenantId, body.StoryIds), cancellationToken));

    private static Task<IResult> AdminEnableAsync(
        Guid id, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(new EnableAdminStoryCommand(tenantId, id), cancellationToken));

    private static Task<IResult> AdminDisableAsync(
        Guid id, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(new DisableAdminStoryCommand(tenantId, id), cancellationToken));

    private static Task<IResult> AdminScheduleAsync(
        Guid id, SetStoryScheduleBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(
                new ScheduleAdminStoryCommand(tenantId, id, StoryBodyMapping.ToSchedule(body)), cancellationToken));

    private static Task<IResult> AdminApproveAsync(
        Guid id, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            (tenantId, actor) => sender.Send(new ApproveAdminStoryCommand(tenantId, id, actor), cancellationToken));

    private static Task<IResult> AdminRejectAsync(
        Guid id, RejectStoryBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            (tenantId, actor) => sender.Send(
                new RejectAdminStoryCommand(tenantId, id, actor, body.Reason ?? string.Empty), cancellationToken));

    private static Task<IResult> AdminAddItemAsync(
        Guid id, AddStoryItemBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(
                new AddAdminStoryItemCommand(tenantId, id, StoryBodyMapping.ToAddItem(body)), cancellationToken),
            createdLocation: result => $"/v1/admin/stories/{result.StoryId}");

    private static Task<IResult> AdminUpdateItemAsync(
        Guid id, Guid itemId, UpdateStoryItemBody body, ISender sender, IStoryAdminAuthorizer auth,
        HttpContext http, ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(
                new UpdateAdminStoryItemCommand(tenantId, id, itemId, StoryBodyMapping.ToUpdateItem(body)),
                cancellationToken));

    private static Task<IResult> AdminRemoveItemAsync(
        Guid id, Guid itemId, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(new RemoveAdminStoryItemCommand(tenantId, id, itemId), cancellationToken));

    private static Task<IResult> AdminReorderItemsAsync(
        Guid id, ReorderStoryItemsBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(new ReorderAdminStoryItemsCommand(tenantId, id, body.ItemIds), cancellationToken));

    private static Task<IResult> AdminMutationAsync<T>(
        IStoryAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        Func<Guid, Task<Result<T>>> action,
        Func<T, string>? createdLocation = null)
        => AdminMutationAsync(auth, http, tenant, api, cancellationToken, (tenantId, _) => action(tenantId), createdLocation);

    private static async Task<IResult> AdminMutationAsync<T>(
        IStoryAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        Func<Guid, Guid, Task<Result<T>>> action,
        Func<T, string>? createdLocation = null)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        var tenantResult = StoryHttpErrors.ResolveTenantId(tenant);
        if (tenantResult.IsFailure)
            return api.From(tenantResult);
        var result = await action(tenantResult.Value, actorUserId);
        if (createdLocation is not null && result.IsSuccess)
            return api.Created(createdLocation(result.Value), result);
        return api.From(result);
    }
}
