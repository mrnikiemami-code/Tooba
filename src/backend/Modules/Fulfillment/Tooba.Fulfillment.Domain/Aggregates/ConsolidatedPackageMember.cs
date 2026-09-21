

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>
/// عضو بسته تجمیعی (مرسولهٔ فروشنده).
/// </summary>
public sealed class ConsolidatedPackageMember
{
    private ConsolidatedPackageMember()
    {
    }

    /// <summary>شناسه عضویت.</summary>
    public Guid ConsolidatedPackageMemberId { get; init; }

    /// <summary>بسته مالک.</summary>
    public Guid ConsolidatedPackageId { get; init; }

    /// <summary>مرسوله عضو.</summary>
    public Guid ShipmentId { get; init; }

    /// <summary>فروشندهٔ مرسوله در زمان عضویت.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>fulfillment مالک مرسوله.</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>زمان عضویت.</summary>
    public DateTimeOffset JoinedAt { get; init; }

    /// <summary>
    /// زمان آزادسازی عضویت (پس از Cancel بسته). null یعنی عضویت فعال برای ایندکس یکتایی.
    /// </summary>
    public DateTimeOffset? ReleasedAt { get; private set; }

    internal static ConsolidatedPackageMember Create(
        Guid memberId,
        Guid packageId,
        Guid shipmentId,
        Guid sellerPartyId,
        Guid fulfillmentId,
        DateTimeOffset now) =>
        new()
        {
            ConsolidatedPackageMemberId = memberId,
            ConsolidatedPackageId = packageId,
            ShipmentId = shipmentId,
            SellerPartyId = sellerPartyId,
            FulfillmentId = fulfillmentId,
            JoinedAt = now,
        };

    internal void Release(DateTimeOffset now)
    {
        ReleasedAt ??= now;
    }

    /// <summary>آیا عضویت هنوز برای قفل/ایندکس فعال است.</summary>
    public bool IsActiveMembership => ReleasedAt is null;
}
