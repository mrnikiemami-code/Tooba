using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Commands;

/// <summary>
/// TB-279: the audited correction of an ALREADY recorded decision. It appends a reviewed revision and returns the case
/// to review; it never deletes the decision it corrects and never reverses money, stock or custody.
/// <para>
/// <see cref="OccurrenceKey"/> is the exact reviewed identity of this correction (a duplicate/lost-response POST
/// replays its own recorded revision), <see cref="Reason"/> is the operator's reviewed statement and
/// <see cref="ExpectedDecisionVersion"/> is the effective version the operator reviewed — a newer revision refuses the
/// stale one instead of overwriting it.
/// </para>
/// </summary>
public sealed record ReopenReturnForReviewCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    string OccurrenceKey,
    string Reason,
    int? ExpectedDecisionVersion = null,
    Guid? SellerPartyId = null) : IRequest<Result<ReturnSnapshot>>;

/// <summary>
/// TB-279-R1 (F2/F3): one reviewed PER-CONDITION split of ONE requested line.
/// <para>
/// Every portion is REQUIRED and there is no inferred default: the earlier shape carried only accepted/rejected totals,
/// so the accepted CONDITION was derived from <c>Min(accepted, usable)</c> and the operator's actual decision about the
/// damaged units was lost. A null portion is `return.resolution.portion_required`, never zero.
/// </para>
/// <para>
/// <see cref="AcceptedUsable"/> + <see cref="AcceptedDamaged"/> is the refundable quantity,
/// <see cref="RejectedReceivedUsable"/> + <see cref="RejectedReceivedDamaged"/> is declined but physically received (and
/// therefore owed back to the customer), and <see cref="RejectedUnreceived"/> is the part of the claim that never
/// arrived. The three together must equal the requested quantity.
/// </para>
/// </summary>
public sealed record ReturnResolutionLineCommand(
    Guid ReturnItemId,
    decimal? AcceptedUsable,
    decimal? AcceptedDamaged,
    decimal? RejectedReceivedUsable,
    decimal? RejectedReceivedDamaged,
    decimal? RejectedUnreceived,
    string? Reason);

/// <summary>
/// TB-279: publishes the reviewed per-item/condition resolution plan of an ordinary multi-item case. The approved money
/// is derived by the owning policy from the accepted quantities and each line's STORED unit price — never from a
/// browser amount, today's price or the original claim when the plan is partial.
/// </summary>
public sealed record PublishReturnResolutionCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    string OccurrenceKey,
    int ExpectedResolutionVersion,
    IReadOnlyList<ReturnResolutionLineCommand> Lines,
    Guid? SellerPartyId = null) : IRequest<Result<ReturnSnapshot>>;

/// <summary>
/// TB-279: the deliberate audited confirmation of an ACTUAL physical handback of rejected received goods to the owning
/// customer. It records a Returns custody fact only — no refund, Wallet, stock or carrier transition.
/// <para>
/// TB-279-R1 (F3): <see cref="ProofKind"/> is REQUIRED and is one of the two reviewed proof semantics — an operator
/// in-person attestation or an owning carrier delivered-back fact. A tracking number alone is not a handback, and the
/// kind is stored so a later reader can tell WHICH evidence the custody fact rests on.
/// </para>
/// </summary>
public sealed record RecordReturnCustodyCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    string OccurrenceKey,
    Guid ReturnItemId,
    decimal Quantity,
    ReturnItemCondition Condition,
    string ProofReference,
    string? ProofKind = null,
    Guid? SellerPartyId = null) : IRequest<Result<ReturnSnapshot>>;

/// <summary>Handler — seller scope then the owning corrections authority.</summary>
public sealed class ReopenReturnForReviewHandler(IReturnDirectory returns)
    : IRequestHandler<ReopenReturnForReviewCommand, Result<ReturnSnapshot>>
{
    public async Task<Result<ReturnSnapshot>> Handle(
        ReopenReturnForReviewCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await returns.GetAsync(request.ReturnRequestId, cancellationToken);
        if (existing is null || (request.SellerPartyId is { } scope && existing.SellerPartyId != scope))
        {
            return Result.Failure<ReturnSnapshot>(new SemanticError(ReturnsErrorCodes.Missing));
        }

        return await ReturnsOperation.ExecuteAsync(() => returns.ReopenForReviewAsync(request, cancellationToken));
    }
}

/// <summary>Handler — seller scope then the owning resolution authority.</summary>
public sealed class PublishReturnResolutionHandler(IReturnDirectory returns)
    : IRequestHandler<PublishReturnResolutionCommand, Result<ReturnSnapshot>>
{
    public async Task<Result<ReturnSnapshot>> Handle(
        PublishReturnResolutionCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await returns.GetAsync(request.ReturnRequestId, cancellationToken);
        if (existing is null || (request.SellerPartyId is { } scope && existing.SellerPartyId != scope))
        {
            return Result.Failure<ReturnSnapshot>(new SemanticError(ReturnsErrorCodes.Missing));
        }

        return await ReturnsOperation.ExecuteAsync(() => returns.PublishResolutionAsync(request, cancellationToken));
    }
}

/// <summary>Handler — seller scope then the owning custody authority.</summary>
public sealed class RecordReturnCustodyHandler(IReturnDirectory returns)
    : IRequestHandler<RecordReturnCustodyCommand, Result<ReturnSnapshot>>
{
    public async Task<Result<ReturnSnapshot>> Handle(
        RecordReturnCustodyCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await returns.GetAsync(request.ReturnRequestId, cancellationToken);
        if (existing is null || (request.SellerPartyId is { } scope && existing.SellerPartyId != scope))
        {
            return Result.Failure<ReturnSnapshot>(new SemanticError(ReturnsErrorCodes.Missing));
        }

        return await ReturnsOperation.ExecuteAsync(() => returns.RecordCustodyAsync(request, cancellationToken));
    }
}
