namespace Tooba.Support.Application.Models;

/// <summary>snapshot شناسه‌های demo برای USER-PREVIEW (قرارداد Application).</summary>
public sealed record SupportDemoSnapshotDto(
    Guid CustomerOpenTicketId,
    Guid CustomerResolvedTicketId,
    Guid SellerWaitingTicketId,
    Guid SellerOpenTicketId,
    Guid? CustomerActorUserId,
    Guid? SellerPartyId,
    Guid? SellerActorUserId,
    Guid? AdminActorUserId,
    Guid? RelatedOrderId,
    string Note);
