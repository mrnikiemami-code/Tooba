using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Commands.ExecuteAdminFulfillmentBulk;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Queries.GetAdminFulfillment;
using Tooba.Fulfillment.Application.Queries.ListAdminFulfillments;
using Tooba.Fulfillment.Application.Queries.QueryAdminFulfillmentWorkQueue;

namespace Tooba.Fulfillment.Endpoints.Admin;

/// <summary>Thin admin Fulfillment HTTP routes.</summary>
public static class FulfillmentAdminEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/fulfillments", AdminListAsync);
        group.MapPost("/fulfillments/query", AdminQueryGridAsync);
        group.MapPost("/fulfillments/work-queue/query", AdminWorkQueueQueryAsync);
        group.MapPost("/fulfillments/work-queue/bulk", AdminWorkQueueBulkAsync);
        group.MapGet("/fulfillments/{fulfillmentId:guid}", AdminGetAsync);
    }

    private static async Task<IResult> AdminListAsync(
        ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListAdminFulfillmentsQuery(), cancellationToken));
    }

    private static async Task<IResult> AdminQueryGridAsync(
        GridQueryRequest body, ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new QueryAdminFulfillmentWorkQueueQuery(body), cancellationToken));
    }

    private static async Task<IResult> AdminWorkQueueQueryAsync(
        GridQueryRequest body, ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new QueryAdminFulfillmentWorkQueueQuery(body), cancellationToken));
    }

    private static async Task<IResult> AdminWorkQueueBulkAsync(
        AdminFulfillmentWorkQueueBulkRequest body, ISender sender, IFulfillmentAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        var actor = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        var outcome = await sender.Send(new ExecuteAdminFulfillmentBulkCommand(actor, body), cancellationToken);
        if (outcome.IsFailure) return api.From(outcome);
        var result = outcome.Value;
        if (result.ErrorCode is not null)
            return api.FromFailure(new SemanticError(result.ErrorCode));
        return api.From(Result.Success(result));
    }

    private static async Task<IResult> AdminGetAsync(
        Guid fulfillmentId, ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new GetAdminFulfillmentQuery(fulfillmentId), cancellationToken));
    }
}

