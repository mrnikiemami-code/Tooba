using Tooba.BuildingBlocks;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Contracts.Operations;
using Tooba.Returns.Domain.ValueObjects;
using AppModels = Tooba.Returns.Application.ReturnRequests.Models;
using ReturnStatusOverlayRow = Tooba.Returns.Contracts.Operations.ReturnStatusOverlayRow;

namespace Tooba.Returns.Infrastructure.Adapters;

/// <summary>
/// Contract-facing adapter over <see cref="IReturnDirectory"/> and <see cref="IReturnEligibilityEvaluator"/>
/// so admin order callers never reference Returns Application/Domain types.
/// The module's internal port consumes the authoritative MediatR request shapes, so this adapter composes
/// the seller scope from the owning snapshot (admin callers never carry it) and never duplicates a
/// command record. Expected failures originate as <see cref="ContractOperationException"/> at the owning
/// Directory; this adapter does not parse Message or promote InvalidOperationException.
/// </summary>
internal sealed class ReturnAdminOperationsAdapter(
    IReturnDirectory directory,
    IReturnEligibilityEvaluator eligibility) : IReturnAdminOperations
{
    public async Task<IReadOnlyList<ReturnSnapshot>> ListBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ReturnSnapshot> list =
            (await directory.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken)).Select(Map).ToList();
        return list;
    }

    public async Task<IReadOnlyList<ReturnStatusOverlayRow>> ListStatusOverlayAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ReturnStatusOverlayRow> list =
            (await directory.ListStatusOverlayAsync(cancellationToken))
            .Select(x => new ReturnStatusOverlayRow(x.SellerOrderId, (ReturnRequestOperationStatus)x.Status))
            .ToList();
        return list;
    }

    public async Task<ReturnEligibilityResult> EvaluateEligibilityAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken) =>
        Map(await eligibility.EvaluateAsync(sellerOrderId, cancellationToken));

    public async Task<ReturnSnapshot> CreateAdminInitiatedAsync(
        CreateAdminReturnCommand command,
        CancellationToken cancellationToken) =>
        Map(await directory.CreateAdminInitiatedAsync(
            new CreateReturnCommand(
                command.SellerOrderId,
                command.ActorUserId,
                command.IdempotencyKey,
                command.Reason,
                command.Items.Select(x => new AppModels.ReturnLineCommand(x.OrderLineId, x.Quantity)).ToList(),
                RefundDestination.OriginalPayment),
            cancellationToken));

    public async Task<ReturnSnapshot> ApproveAsync(
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken,
        int? expectedDecisionVersion = null,
        int? expectedResolutionVersion = null)
    {
        // TB-279-R3 (B1/A4): this reachable Order/admin adapter NEVER invents the revision it is approving. The
        // reviewed decision and plan versions travel exactly as the caller supplied them; a caller that reviewed
        // nothing gets the owning directory's typed stale-review refusal instead of an unreviewed decision taken
        // against whatever the store happened to hold at send time.
        return Map(await directory.ApproveAsync(
            new ApproveReturnCommand(
                returnRequestId,
                actorUserId,
                await RequireSellerPartyAsync(returnRequestId, cancellationToken),
                ExpectedResolutionVersion: expectedResolutionVersion,
                ExpectedDecisionVersion: expectedDecisionVersion),
            cancellationToken));
    }

    public async Task<ReturnSnapshot> StartFirstRefundAsync(
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        // TB-279-R1 (F4): a first refund is version-fenced. This legacy adapter carries no reviewed revision of its own,
        // so it reviews the CURRENT stored revision once and passes THAT to the owning directory: a correction committed
        // between this read and the money step still refuses the stale intent inside the locked reservation.
        var current = await directory.GetAsync(returnRequestId, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.Missing);
        return Map(await directory.StartFirstRefundAsync(
            new StartReturnRefundCommand(returnRequestId, actorUserId, current.DecisionVersion),
            cancellationToken));
    }

    public async Task<ReturnSnapshot> RejectAsync(
        Guid returnRequestId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken,
        int? expectedDecisionVersion = null,
        int? expectedResolutionVersion = null)
    {
        // TB-279-R3 (B1/A4): the same rule as approval — the reviewed revisions are forwarded exactly as supplied and
        // are never synthesized from a fresh read, so an unreviewed rejection is refused by the owning directory.
        return Map(await directory.RejectAsync(
            new RejectReturnCommand(
                returnRequestId,
                actorUserId,
                await RequireSellerPartyAsync(returnRequestId, cancellationToken),
                reason,
                expectedDecisionVersion,
                expectedResolutionVersion),
            cancellationToken));
    }

    public async Task<ReturnSnapshot> RetryRefundAsync(
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        // TB-275 (C2): an older caller does not carry the reviewed failed attempt, so this entry point answers only the
        // case's CURRENT definite failure from the stored work description — the same identity the case workspace
        // reviews. It never mints a count-based attempt and never retries without a definitive failure (fail closed).
        var current = await directory.GetAsync(returnRequestId, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.Missing);
        var failedAttemptId = current.Work?.FailedRefundAttemptId
            ?? throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptUnbound);
        return Map(await directory.RetryRefundAsync(
            new RetryReturnRefundCommand(returnRequestId, actorUserId)
            {
                ExpectedFailedAttemptId = failedAttemptId,
                // TB-279-R1 (F4): the revision this adapter just read is the one it reviews.
                ExpectedDecisionVersion = current.DecisionVersion,
            },
            cancellationToken));
    }

    public async Task<ReturnSnapshot> RecoverRefundAsync(
        Guid returnRequestId,
        Guid actorUserId,
        ReturnRecoveryKindOption kind,
        Guid? expectedRefundAttemptId,
        CancellationToken cancellationToken) =>
        Map(await directory.RecoverAsync(
            new RecoverReturnCommand(
                returnRequestId,
                actorUserId,
                (AppModels.ReturnRecoveryKind)kind,
                expectedRefundAttemptId),
            cancellationToken));

    /// <summary>
    /// Admin callers address a return by identifier only; the authoritative command shape also carries
    /// the owning seller scope (used by the seller use case). The adapter resolves it from the owning
    /// snapshot so the same <c>return.missing</c> fault is raised for an unknown identifier.
    /// </summary>
    public async Task<CourierOriginReturnResult> CreateCourierOriginAsync(
        CreateCourierOriginReturnCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var (snapshot, claimId, alreadyExisted, conflict) = await directory.CreateCourierOriginAsync(
            command.FulfillmentId,
            command.ShipmentId,
            command.ActorUserId,
            command.IdempotencyKey,
            command.OccurrenceKey,
            command.Reason,
            command.Items
                .Select(x => (
                    x.OrderLineId,
                    x.Quantity,
                    x.Condition == ReturnItemConditionOption.Damaged
                        ? ReturnItemCondition.Damaged
                        : ReturnItemCondition.Usable))
                .ToList(),
            cancellationToken);

        if (snapshot is null)
        {
            throw new ContractOperationException(conflict ?? ReturnsErrorCodes.Missing);
        }

        return new CourierOriginReturnResult(Map(snapshot), claimId, alreadyExisted, conflict);
    }

    public async Task<CourierOriginReturnResult?> ReconcileCourierOriginAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        string occurrenceKey,
        CancellationToken cancellationToken)
    {
        var (snapshot, claimId, alreadyExisted, conflict) = await directory.ReconcileCourierOriginAsync(
            fulfillmentId,
            shipmentId,
            occurrenceKey,
            cancellationToken);

        if (snapshot is null)
        {
            return conflict is null ? null : new CourierOriginReturnResult(
                new ReturnSnapshot(
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty,
                    ReturnRequestOperationStatus.Requested,
                    null,
                    string.Empty,
                    0m,
                    null,
                    RefundDestinationOption.OriginalPayment,
                    default,
                    default,
                    Array.Empty<ReturnItemSnapshot>(),
                    Array.Empty<RefundAttemptSnapshot>()),
                claimId,
                alreadyExisted,
                conflict);
        }

        return new CourierOriginReturnResult(Map(snapshot), claimId, alreadyExisted, conflict);
    }

    /// <summary>
    /// TB-274: record one physical customer receipt occurrence. The admin contract carries only the reviewed
    /// occurrence identity and the inspected quantities; the case/state/over-receipt/gating rules stay in the directory.
    /// <para>
    /// TB-274-R1: the inspected condition is mapped EXPLICITLY. There is no "anything not damaged means usable"
    /// fallback — an undefined value is refused with the same typed line fault every other malformed line raises, so a
    /// contract caller cannot silently record a usable unit by omission or by an unmapped enum value. The owner/case
    /// scope is never taken from the caller: the directory resolves the case from its own identifier and only its own
    /// lines may be received.
    /// </para>
    /// </summary>
    public async Task<ReturnSnapshot> RecordCustomerReceiptAsync(
        RecordCustomerReceiptCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Map(await directory.RecordReceiptAsync(
            new RecordReturnReceiptCommand(
                command.ReturnRequestId,
                command.ActorUserId,
                command.OccurrenceKey,
                (command.Items ?? [])
                    .Select(x => new CustomerReceiptLine(
                        x.ReturnItemId,
                        x.Quantity,
                        ToDomainCondition(x.Condition)))
                    .ToList()),
            cancellationToken));
    }

    /// <summary>TB-274-R1: exactly the two defined inspections; anything else is a typed refusal, never a default.</summary>
    private static ReturnItemCondition ToDomainCondition(ReturnItemConditionOption condition) => condition switch
    {
        ReturnItemConditionOption.Usable => ReturnItemCondition.Usable,
        ReturnItemConditionOption.Damaged => ReturnItemCondition.Damaged,
        _ => throw new ContractOperationException(ReturnsErrorCodes.ReceiptLineInvalid),
    };

    private async Task<Guid> RequireSellerPartyAsync(Guid returnRequestId, CancellationToken cancellationToken) =>
        (await directory.GetAsync(returnRequestId, cancellationToken))?.SellerPartyId
        ?? throw new ContractOperationException(ReturnsErrorCodes.Missing);

    private static ReturnSnapshot Map(AppModels.ReturnSnapshot snapshot) =>
        new(
            snapshot.ReturnRequestId,
            snapshot.SellerOrderId,
            snapshot.CheckoutId,
            snapshot.SellerPartyId,
            snapshot.RequestedByUserId,
            (ReturnRequestOperationStatus)snapshot.Status,
            snapshot.Reason,
            snapshot.Currency,
            snapshot.RefundAmount,
            snapshot.PaymentId,
            (RefundDestinationOption)snapshot.RefundDestination,
            snapshot.CreatedAt,
            snapshot.UpdatedAt,
            snapshot.Items
                .Select(x => new ReturnItemSnapshot(
                    x.ReturnItemId,
                    x.OrderLineId,
                    x.Quantity,
                    x.UnitPriceSnapshot,
                    x.Currency,
                    x.ReservationId,
                    (ReturnItemConditionOption)x.Condition,
                    x.Disposition == ReturnItemDisposition.DamagedNonSaleable
                        ? ReturnItemConditionOption.Damaged
                        : null,
                    x.SourceShipmentId,
                    x.SourceReceiptId,
                    x.SaleableRestockedQuantity,
                    x.UnitDecimalPlaces))
                .ToList(),
            snapshot.RefundAttempts
                .Select(x => new RefundAttemptSnapshot(
                    x.RefundAttemptId,
                    x.PaymentId,
                    x.Amount,
                    x.Currency,
                    (RefundAttemptOperationStatus)x.Status,
                    x.IdempotencyKey,
                    x.ProviderReference,
                    x.FailureCode,
                    x.CreatedAt,
                    x.CompletedAt))
                .ToList(),
            (ReturnOriginOption)snapshot.Origin,
            snapshot.SourceFulfillmentId,
            snapshot.SourceShipmentId,
            snapshot.SourceReceiptId,
            snapshot.SourceClaimId,
            snapshot.SourceOccurrenceKey,
            snapshot.ClosurePhase.ToString(),
            snapshot.ObligationClosedAt,
            snapshot.StockSettledAt,
            snapshot.ReleasedHeldQuantity,
            Map(snapshot.Recovery),
            Map(snapshot.Receiving),
            snapshot.DecisionReason,
            snapshot.RecordedDecision,
            snapshot.Receipts
                ?.Select(x => new ReturnReceiptOccurrenceSnapshot(
                    x.ReturnReceiptId,
                    x.OccurrenceKey,
                    x.ReceivedByUserId,
                    x.RecordedAt,
                    x.Lines
                        .Select(line => new ReturnReceiptLineSnapshot(
                            line.ReturnItemId,
                            line.Quantity,
                            (ReturnItemConditionOption)line.Condition))
                        .ToList()))
                .ToList(),
            Map(snapshot.Work),
            snapshot.EffectiveDecision,
            snapshot.DecisionVersion,
            snapshot.DecisionHistory?
                .Select(x => new ReturnDecisionRevisionSnapshot(
                    x.Version,
                    x.Decision,
                    x.Reason,
                    x.ActorUserId,
                    x.RecordedAt))
                .ToList(),
            Map(snapshot.Resolution),
            snapshot.ApprovedAmount,
            snapshot.Custody?
                .Select(x => new ReturnCustodySnapshot(
                    x.ReturnCustodyId,
                    x.ReturnItemId,
                    x.Quantity,
                    (ReturnItemConditionOption)x.Condition,
                    x.RecipientUserId,
                    x.ProofReference,
                    x.RecordedAt))
                .ToList(),
            snapshot.Correction is { } correction
                ? new ReturnCorrectionAssessment(correction.CanReopenForReview, correction.BlockerCode)
                : null);

    /// <summary>
    /// TB-279: the reviewed resolution plan as the wire reports it. The approved amount is the policy's own number, so
    /// no consumer re-multiplies a quantity (or reads today's price) to discover what a partial case may refund.
    /// </summary>
    private static ReturnResolutionSnapshot? Map(AppModels.ReturnResolutionSnapshot? resolution) =>
        resolution is null
            ? null
            : new ReturnResolutionSnapshot(
                resolution.Version,
                resolution.IsComplete,
                resolution.ApprovedAmount,
                resolution.FromReviewedPlan,
                resolution.Lines
                    .Select(x => new ReturnResolutionLineSnapshot(
                        x.ReturnItemId,
                        x.Requested,
                        x.Received,
                        x.Usable,
                        x.Damaged,
                        x.Accepted,
                        x.Rejected,
                        x.Reason,
                        x.Version))
                    .ToList(),
                resolution.BlockerCode);

    /// <summary>
    /// TB-275: the additive operator work stage. Every flag is a positive stored fact computed by the ONE Returns-owned
    /// policy, so this projection never re-derives a permission the actions would refuse.
    /// </summary>
    private static ReturnWorkSnapshot? Map(AppModels.ReturnWorkDescription? work) =>
        work is null
            ? null
            : new ReturnWorkSnapshot(
                work.CategoryCode,
                work.BlockerCode,
                work.PendingRefundAttemptId,
                work.FailedRefundAttemptId,
                work.CanRecordReceipt,
                work.CanDecide,
                work.CanResumeRefund,
                work.CanRetryRefund,
                work.CanSettlePhysical,
                work.IsOpen,
                work.NeedsOperator,
                work.ActionCodes.ToList(),
                work.CanStartRefund);

    /// <summary>
    /// TB-273: additive recovery facts. Money completion and physical completion stay two independent facts so a
    /// consumer can never read a completed refund as fully settled stock.
    /// </summary>
    private static ReturnRecoverySnapshot? Map(AppModels.ReturnRecoveryDescription? recovery) =>
        recovery is null
            ? null
            : new ReturnRecoverySnapshot(
                (ReturnRecoveryPhaseOption)recovery.Phase,
                recovery.RecoveryKind is { } kind ? (ReturnRecoveryKindOption)kind : null,
                recovery.RefundAttemptId,
                recovery.BlockerCode,
                recovery.FinancialComplete,
                recovery.PhysicalComplete);

    /// <summary>
    /// TB-274: the additive receiving facts. <c>IsComplete</c>/<c>IsCoherent</c> are reported separately so a consumer
    /// can never read "some units arrived" as "fully received" and never treats a contradictory row as capacity.
    /// </summary>
    private static ReturnReceivingSnapshot? Map(ReturnReceivingSummary? receiving) =>
        receiving is null
            ? null
            : new ReturnReceivingSnapshot(
                receiving.Lines
                    .Select(x => new ReturnReceivingLineSnapshot(
                        x.ReturnItemId,
                        x.Requested,
                        x.Received,
                        x.Usable,
                        x.Damaged))
                    .ToList(),
                receiving.IsComplete,
                receiving.IsCoherent);

    private static ReturnEligibilityResult Map(AppModels.ReturnEligibilityResult result) =>
        new(
            result.SellerOrderId,
            result.CheckoutId,
            result.Eligible,
            result.ReasonCode,
            result.EligibleUntil,
            result.LastDeliveredAt,
            result.Lines
                .Select(x => new ReturnLineEligibility(
                    x.OrderLineId,
                    x.DeliveredQuantity,
                    x.AlreadyReturnedQuantity,
                    x.RemainingReturnableQuantity))
                .ToList());
}
