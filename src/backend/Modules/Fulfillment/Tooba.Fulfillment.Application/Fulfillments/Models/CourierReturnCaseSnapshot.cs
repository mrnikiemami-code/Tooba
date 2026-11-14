using Tooba.Fulfillment.Contracts.Returns;

namespace Tooba.Fulfillment.Application.Fulfillments.Models;

/// <summary>
/// TB-265-R1: a courier-origin Returns case this parcel's received quantity is durably linked to. It is a navigation
/// identity for the operator only — the case itself, its money and its disposition belong to the Returns module.
/// </summary>
/// <param name="ClaimId">The Fulfillment-owned source claim.</param>
/// <param name="ReturnRequestId">The linked Returns case.</param>
/// <param name="Status">Durable claim phase (Requested/Approved/Rejected/Cancelled).</param>
/// <param name="UpdatedAt">Last durable change of the claim.</param>
public sealed record CourierReturnCaseSnapshot(
    Guid ClaimId,
    Guid ReturnRequestId,
    CourierReturnClaimStatus Status,
    DateTimeOffset UpdatedAt);
