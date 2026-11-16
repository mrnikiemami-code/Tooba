using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Queries;
using Tooba.Returns.Contracts.Operations;
using Tooba.Returns.Domain.ValueObjects;

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
        group.MapPost("/returns/{returnRequestId:guid}/receipt", AdminRecordReceiptAsync);
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

    private static async Task<IResult> AdminRecordReceiptAsync(
        Guid returnRequestId, AdminReturnReceiptRequest? body, ISender sender, IReturnAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(context, cancellationToken);

        // TB-274: an undefined inspection token is refused BEFORE any read/write and never coerced into "usable".
        var items = new List<CustomerReceiptLine>(body?.Items?.Count ?? 0);
        foreach (var line in body?.Items ?? [])
        {
            var condition = ReturnItemConditionParser.Parse(line.Condition);
            if (condition.IsFailure)
            {
                return api.From(condition);
            }

            items.Add(new CustomerReceiptLine(line.ReturnItemId, line.Quantity, condition.Value));
        }

        return api.From(await sender.Send(
            new RecordReturnReceiptCommand(returnRequestId, actorUserId, body?.OccurrenceKey ?? string.Empty, items),
            cancellationToken));
    }
}

/// <summary>
/// TB-274: wire-only customer receipt body. <c>OccurrenceKey</c> is the reviewed identity of the delivery and
/// <c>Items</c> are the inspected quantities; no money, destination, amount or approval value is accepted.
/// </summary>
public sealed record AdminReturnReceiptRequest(
    string? OccurrenceKey,
    IReadOnlyList<AdminReturnReceiptLine>? Items = null);

/// <summary>
/// TB-274: one inspected line of the submitted customer receipt. <c>Condition</c> is the stable wire token
/// (<c>usable</c>/<c>damaged</c>) parsed by <see cref="ReturnItemConditionParser"/>, so a malformed token is a typed
/// refusal rather than a transport fault.
/// </summary>
public sealed record AdminReturnReceiptLine(
    Guid ReturnItemId,
    decimal Quantity,
    string? Condition = ReturnItemConditionParser.Usable);

/// <summary>
/// TB-273: wire-only explicit recovery body. <c>Kind</c> is the reviewed intent token and
/// <c>ExpectedRefundAttemptId</c> is the exact stored attempt the operator reviewed; no payment, amount, currency,
/// destination or customer value is accepted from the client.
/// </summary>
public sealed record AdminReturnRecoveryRequest(string? Kind, Guid? ExpectedRefundAttemptId = null);
