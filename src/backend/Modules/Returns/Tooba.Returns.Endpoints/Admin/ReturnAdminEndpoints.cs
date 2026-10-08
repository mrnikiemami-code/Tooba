using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Queries;

namespace Tooba.Returns.Endpoints.Admin;

/// <summary>Thin admin Returns HTTP routes.</summary>
public static class ReturnAdminEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/returns", AdminListAsync);
        group.MapPost("/returns/query", AdminQueryGridAsync);
        group.MapGet("/returns/{returnRequestId:guid}", AdminGetAsync);
        group.MapPost("/returns/{returnRequestId:guid}/retry-refund", AdminRetryRefundAsync);
    }

    private static async Task<IResult> AdminListAsync(
        ISender sender, IReturnAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListAdminReturnsQuery(), cancellationToken));
    }

    private static async Task<IResult> AdminQueryGridAsync(
        GridQueryRequest body, ISender sender, IReturnAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new QueryAdminReturnsGridQuery(body), cancellationToken));
    }

    private static async Task<IResult> AdminGetAsync(
        Guid returnRequestId, ISender sender, IReturnAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new GetAdminReturnQuery(returnRequestId), cancellationToken));
    }

    private static async Task<IResult> AdminRetryRefundAsync(
        Guid returnRequestId, ISender sender, IReturnAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new RetryReturnRefundCommand(returnRequestId, actorUserId), cancellationToken));
    }
}
