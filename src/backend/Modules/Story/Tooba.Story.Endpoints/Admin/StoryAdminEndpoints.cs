using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Story.Application.Commands.Admin;
using Tooba.Story.Application.Queries.Admin;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain;
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
        string? reviewStatus = null,
        bool pendingReview = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            var tenantId = StoryHttpErrors.RequireTenantId(tenant);
            StoryReviewStatus? parsed = null;
            if (!pendingReview && !string.IsNullOrWhiteSpace(reviewStatus))
            {
                if (!Enum.TryParse<StoryReviewStatus>(reviewStatus, ignoreCase: true, out var value)
                    || !Enum.IsDefined(value))
                {
                    return Results.Json(
                        new { title = "Bad Request", errorCode = StoryErrorCodes.ReviewStatusInvalid },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                parsed = value;
            }

            return Results.Json(await sender.Send(
                new ListAdminStoriesQuery(tenantId, parsed, pendingReview), cancellationToken));
        }
        catch (PlatformHttpException ex) { return StoryHttpErrors.ToError(ex); }
        catch (InvalidOperationException ex) { return StoryHttpErrors.TenantMissing(ex); }
    }

    private static async Task<IResult> AdminQueryGridAsync(
        GridQueryRequest body,
        ISender sender,
        IStoryAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        string? reviewStatus = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            var tenantId = StoryHttpErrors.RequireTenantId(tenant);
            StoryReviewStatus? parsed = null;
            if (!string.IsNullOrWhiteSpace(reviewStatus))
            {
                if (!Enum.TryParse<StoryReviewStatus>(reviewStatus, ignoreCase: true, out var value)
                    || !Enum.IsDefined(value))
                {
                    return Results.Json(
                        new { title = "Bad Request", errorCode = StoryErrorCodes.ReviewStatusInvalid },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                parsed = value;
            }

            return Results.Json(await sender.Send(
                new QueryAdminStoryGridQuery(tenantId, parsed, body), cancellationToken));
        }
        catch (PlatformHttpException ex) { return StoryHttpErrors.ToError(ex); }
        catch (InvalidOperationException ex) { return StoryHttpErrors.TenantMissing(ex); }
    }

    private static async Task<IResult> AdminGetAsync(
        Guid id,
        ISender sender,
        IStoryAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            var tenantId = StoryHttpErrors.RequireTenantId(tenant);
            var story = await sender.Send(new GetAdminStoryQuery(tenantId, id), cancellationToken);
            return story is null ? Results.NotFound() : Results.Json(story);
        }
        catch (PlatformHttpException ex) { return StoryHttpErrors.ToError(ex); }
    }

    private static Task<IResult> AdminCreateAsync(
        CreateStoryBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            tenantId => sender.Send(new CreateAdminStoryCommand(tenantId, StoryBodyMapping.ToCreate(body)), cancellationToken),
            StatusCodes.Status201Created);

    private static Task<IResult> AdminUpdateAsync(
        Guid id, UpdateStoryBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            tenantId => sender.Send(new UpdateAdminStoryCommand(tenantId, id, StoryBodyMapping.ToUpdate(body)), cancellationToken));

    private static Task<IResult> AdminReorderAsync(
        ReorderStoriesBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            tenantId => sender.Send(new ReorderAdminStoriesCommand(tenantId, body.StoryIds), cancellationToken));

    private static Task<IResult> AdminEnableAsync(
        Guid id, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            tenantId => sender.Send(new EnableAdminStoryCommand(tenantId, id), cancellationToken));

    private static Task<IResult> AdminDisableAsync(
        Guid id, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            tenantId => sender.Send(new DisableAdminStoryCommand(tenantId, id), cancellationToken));

    private static Task<IResult> AdminScheduleAsync(
        Guid id, SetStoryScheduleBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            tenantId => sender.Send(
                new ScheduleAdminStoryCommand(tenantId, id, StoryBodyMapping.ToSchedule(body)), cancellationToken));

    private static Task<IResult> AdminApproveAsync(
        Guid id, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            (tenantId, actor) => sender.Send(new ApproveAdminStoryCommand(tenantId, id, actor), cancellationToken));

    private static Task<IResult> AdminRejectAsync(
        Guid id, RejectStoryBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            (tenantId, actor) => sender.Send(
                new RejectAdminStoryCommand(tenantId, id, actor, body.Reason ?? string.Empty), cancellationToken));

    private static Task<IResult> AdminAddItemAsync(
        Guid id, AddStoryItemBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            tenantId => sender.Send(
                new AddAdminStoryItemCommand(tenantId, id, StoryBodyMapping.ToAddItem(body)), cancellationToken),
            StatusCodes.Status201Created);

    private static Task<IResult> AdminUpdateItemAsync(
        Guid id, Guid itemId, UpdateStoryItemBody body, ISender sender, IStoryAdminAuthorizer auth,
        HttpContext http, ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            tenantId => sender.Send(
                new UpdateAdminStoryItemCommand(tenantId, id, itemId, StoryBodyMapping.ToUpdateItem(body)),
                cancellationToken));

    private static Task<IResult> AdminRemoveItemAsync(
        Guid id, Guid itemId, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            tenantId => sender.Send(new RemoveAdminStoryItemCommand(tenantId, id, itemId), cancellationToken));

    private static Task<IResult> AdminReorderItemsAsync(
        Guid id, ReorderStoryItemsBody body, ISender sender, IStoryAdminAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, CancellationToken cancellationToken) =>
        AdminMutationAsync(auth, http, tenant, cancellationToken,
            tenantId => sender.Send(new ReorderAdminStoryItemsCommand(tenantId, id, body.ItemIds), cancellationToken));

    private static Task<IResult> AdminMutationAsync<T>(
        IStoryAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        CancellationToken cancellationToken,
        Func<Guid, Task<T>> action,
        int successStatusCode = StatusCodes.Status200OK)
        => AdminMutationAsync(auth, http, tenant, cancellationToken, (tenantId, _) => action(tenantId), successStatusCode);

    private static async Task<IResult> AdminMutationAsync<T>(
        IStoryAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        CancellationToken cancellationToken,
        Func<Guid, Guid, Task<T>> action,
        int successStatusCode = StatusCodes.Status200OK)
    {
        try
        {
            var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
            var tenantId = StoryHttpErrors.RequireTenantId(tenant);
            var result = await action(tenantId, actorUserId);
            return Results.Json(result, statusCode: successStatusCode);
        }
        catch (PlatformHttpException ex) { return StoryHttpErrors.ToError(ex); }
        catch (InvalidOperationException ex) { return StoryHttpErrors.ToMutationError(ex); }
    }
}
