using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;


/// <summary>
/// eligibility خط مرجوعی برای یک OrderLine.
/// <para>
/// TB-278: <see cref="ReturnableUntil"/> is the LAST instant this exact line may still be returned, derived from the
/// line's own delivery slice(s) and its own stored policy window — never from the aggregate order status or the
/// browser clock. <see cref="BlockerCode"/> is the ONE stable reason this line cannot fund a new customer selection
/// (<c>null</c> means it can). It is a read fact only: the owning evaluator still computes it, and the same rules
/// remain final for the write path.
/// </para>
/// </summary>
public sealed record ReturnLineEligibility(
    Guid OrderLineId,
    decimal DeliveredQuantity,
    decimal AlreadyReturnedQuantity,
    decimal RemainingReturnableQuantity,
    DateTimeOffset? ReturnableUntil = null,
    string? BlockerCode = null);
