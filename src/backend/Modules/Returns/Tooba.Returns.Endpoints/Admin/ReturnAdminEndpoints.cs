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
        group.MapPost("/returns/{returnRequestId:guid}/start-refund", AdminStartRefundAsync);
        group.MapPost("/returns/{returnRequestId:guid}/receipt", AdminRecordReceiptAsync);
        group.MapPost("/returns/{returnRequestId:guid}/reopen", AdminReopenForReviewAsync);
        group.MapPost("/returns/{returnRequestId:guid}/resolution", AdminPublishResolutionAsync);
        group.MapPost("/returns/{returnRequestId:guid}/custody", AdminRecordCustodyAsync);
    }

    /// <summary>
    /// TB-279: the audited correction of an already recorded decision. The body names the reviewed occurrence, the
    /// operator's statement and the effective version it was reviewed against; no amount, status or money fact is
    /// accepted from the client.
    /// </summary>
    private static async Task<IResult> AdminReopenForReviewAsync(
        Guid returnRequestId, AdminReturnReopenRequest? body, ISender sender, IReturnAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new ReopenReturnForReviewCommand(
                returnRequestId,
                actorUserId,
                body?.OccurrenceKey ?? string.Empty,
                body?.Reason ?? string.Empty,
                body?.ExpectedDecisionVersion),
            cancellationToken));
    }

    /// <summary>
    /// TB-279: publishes the reviewed per-item resolution. The approved money is derived by the owning policy from the
    /// accepted quantities and the stored unit prices; the client never supplies an amount.
    /// </summary>
    private static async Task<IResult> AdminPublishResolutionAsync(
        Guid returnRequestId, AdminReturnResolutionRequest? body, ISender sender, IReturnAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        var lines = (body?.Lines ?? [])
            .Select(x => new ReturnResolutionLineCommand(
                x.ReturnItemId,
                x.AcceptedUsable,
                x.AcceptedDamaged,
                x.RejectedReceivedUsable,
                x.RejectedReceivedDamaged,
                x.RejectedUnreceived,
                x.Reason))
            .ToList();

        return api.From(await sender.Send(
            new PublishReturnResolutionCommand(
                returnRequestId,
                actorUserId,
                body?.OccurrenceKey ?? string.Empty,
                body?.ExpectedResolutionVersion ?? 0,
                lines),
            cancellationToken));
    }

    /// <summary>
    /// TB-279: records the ACTUAL handback of rejected received goods. The recipient is the case's owning customer
    /// (server-derived); the body carries the exact quantity, inspection, proof reference and occurrence only.
    /// </summary>
    private static async Task<IResult> AdminRecordCustodyAsync(
        Guid returnRequestId, AdminReturnCustodyRequest? body, ISender sender, IReturnAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        var condition = ReturnItemConditionParser.Parse(body?.Condition);
        if (condition.IsFailure)
        {
            return api.From(condition);
        }

        // TB-279-R1 (F3): the proof KIND is parsed before any read, so a bare tracking number (or silence) is a typed
        // refusal instead of an operator attestation by default.
        var proofKind = ReturnCustodyProofKindParser.Parse(body?.ProofKind);
        if (proofKind.IsFailure)
        {
            return api.From(proofKind);
        }

        return api.From(await sender.Send(
            new RecordReturnCustodyCommand(
                returnRequestId,
                actorUserId,
                body?.OccurrenceKey ?? string.Empty,
                body?.ReturnItemId ?? Guid.Empty,
                body?.Quantity ?? 0m,
                condition.Value,
                body?.ProofReference ?? string.Empty,
                proofKind.Value),
            cancellationToken));
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
        Guid returnRequestId, Guid? expectedFailedAttemptId, int? expectedDecisionVersion, ISender sender,
        IReturnAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext context,
        CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(context, cancellationToken);

        // TB-275 (C2): the reviewed failed attempt travels as the occurrence identity of the retry intent (a route/query
        // value, never a client-supplied payload); the server still derives the child attempt identity itself.
        // TB-279-R1 (F4): the reviewed DECISION revision travels with it, so a correction published after the operator
        // looked at the case refuses this retry instead of sending money against the earlier plan.
        return api.From(await sender.Send(
            new RetryReturnRefundCommand(returnRequestId, actorUserId)
            {
                ExpectedFailedAttemptId = expectedFailedAttemptId,
                ExpectedDecisionVersion = expectedDecisionVersion,
            },
            cancellationToken));
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

    private static async Task<IResult> AdminStartRefundAsync(
        Guid returnRequestId, int? expectedDecisionVersion, ISender sender, IReturnAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(context, cancellationToken);

        // TB-277: the FIRST refund is a deliberate financial action of its own. Only the case identity travels; the
        // owning directory reads the approved decision, fixed payment, amount, currency and destination from the
        // stored case and derives the one durable attempt identity from it.
        // TB-279-R1 (F4): the reviewed decision revision is REQUIRED by the directory, so the money can never be sent
        // against a plan the operator did not see.
        return api.From(await sender.Send(
            new StartReturnRefundCommand(returnRequestId, actorUserId, expectedDecisionVersion),
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
/// <para>
/// TB-274-R1: the token has NO default. An omitted inspection token is refused by the parser before any case read, so
/// a browser that forgets to state the condition can never record a usable unit by omission.
/// </para>
/// </summary>
public sealed record AdminReturnReceiptLine(
    Guid ReturnItemId,
    decimal Quantity,
    string? Condition);

/// <summary>
/// TB-273: wire-only explicit recovery body. <c>Kind</c> is the reviewed intent token and
/// <c>ExpectedRefundAttemptId</c> is the exact stored attempt the operator reviewed; no payment, amount, currency,
/// destination or customer value is accepted from the client.
/// </summary>
public sealed record AdminReturnRecoveryRequest(string? Kind, Guid? ExpectedRefundAttemptId = null);

/// <summary>
/// TB-279: wire-only audited correction body. The occurrence identity, the reviewed statement and the effective
/// version the operator looked at travel; no money, status, amount or decision value is accepted.
/// </summary>
public sealed record AdminReturnReopenRequest(
    string? OccurrenceKey,
    string? Reason,
    int? ExpectedDecisionVersion = null);

/// <summary>
/// TB-279-R1 (F2): one reviewed PER-CONDITION split of a requested line on the wire. Every portion is a distinct,
/// deliberate number; a missing portion is a typed refusal (`return.resolution.portion_required`), never zero.
/// </summary>
public sealed record AdminReturnResolutionLine(
    Guid ReturnItemId,
    decimal? AcceptedUsable,
    decimal? AcceptedDamaged,
    decimal? RejectedReceivedUsable,
    decimal? RejectedReceivedDamaged,
    decimal? RejectedUnreceived,
    string? Reason);

/// <summary>
/// TB-279: the reviewed resolution plan submitted for publication. The accepted quantities are the only money basis and
/// the server derives the amount from the STORED unit prices.
/// </summary>
public sealed record AdminReturnResolutionRequest(
    string? OccurrenceKey,
    IReadOnlyList<AdminReturnResolutionLine>? Lines = null,
    int? ExpectedResolutionVersion = null);

/// <summary>
/// TB-279: one ACTUAL physical handback of rejected received goods. <c>Condition</c> uses the same stable tokens as the
/// receipt, and the recipient is never a browser-supplied identity.
/// </summary>
public sealed record AdminReturnCustodyRequest(
    string? OccurrenceKey,
    Guid? ReturnItemId,
    decimal? Quantity,
    string? Condition,
    string? ProofReference,
    string? ProofKind = null);
