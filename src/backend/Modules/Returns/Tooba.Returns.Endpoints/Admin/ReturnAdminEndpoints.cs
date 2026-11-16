using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Returns.Application.Composition;
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
        group.MapPost("/returns/{returnRequestId:guid}/recover", AdminRecoverAsync);
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

    private static async Task<IResult> AdminRecoverAsync(
        Guid returnRequestId, AdminReturnRecoveryRequest? body, ISender sender, IReturnAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        var kind = ReturnRecoveryKindParser.Parse(body?.Kind);
        if (kind.IsFailure)
        {
            return api.From(kind);
        }

        return api.From(await sender.Send(
            new RecoverReturnCommand(returnRequestId, actorUserId, kind.Value, body?.ExpectedRefundAttemptId),
            cancellationToken));
    }
}

/// <summary>
/// TB-273: wire-only explicit recovery body. <c>Kind</c> is the reviewed intent token and
/// <c>ExpectedRefundAttemptId</c> is the exact stored attempt the operator reviewed; no payment, amount, currency,
/// destination or customer value is accepted from the client.
/// </summary>
public sealed record AdminReturnRecoveryRequest(string? Kind, Guid? ExpectedRefundAttemptId = null);
