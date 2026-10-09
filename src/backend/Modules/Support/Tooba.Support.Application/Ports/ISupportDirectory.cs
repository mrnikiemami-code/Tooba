using Tooba.Support.Application.Models;

namespace Tooba.Support.Application.Ports;

/// <summary>دایرکتوری کاربردی تیکت پشتیبانی.</summary>
public interface ISupportDirectory
{
    /// <summary>فهرست تیکت‌های مشتری مالک.</summary>
    Task<TicketListPageDto> ListForCustomerAsync(Guid actorUserId, AudienceTicketListQuery query, CancellationToken cancellationToken);

    /// <summary>جزئیات تیکت مشتری بدون یادداشت داخلی.</summary>
    Task<TicketSnapshotDto?> GetForCustomerAsync(Guid actorUserId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>ایجاد تیکت مشتری.</summary>
    Task<TicketSnapshotDto> CreateForCustomerAsync(Guid actorUserId, CreateTicketCommand command, CancellationToken cancellationToken);

    /// <summary>پاسخ مشتری.</summary>
    Task<TicketSnapshotDto> ReplyForCustomerAsync(Guid actorUserId, Guid ticketId, ReplyTicketCommand command, CancellationToken cancellationToken);

    /// <summary>بستن تیکت مشتری.</summary>
    Task<TicketSnapshotDto> CloseForCustomerAsync(Guid actorUserId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>بازگشایی تیکت مشتری.</summary>
    Task<TicketSnapshotDto> ReopenForCustomerAsync(Guid actorUserId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>فهرست تیکت‌های SellerParty.</summary>
    Task<TicketListPageDto> ListForSellerAsync(Guid sellerPartyId, AudienceTicketListQuery query, CancellationToken cancellationToken);

    /// <summary>جزئیات تیکت فروشنده بدون یادداشت داخلی.</summary>
    Task<TicketSnapshotDto?> GetForSellerAsync(Guid sellerPartyId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>ایجاد تیکت فروشنده.</summary>
    Task<TicketSnapshotDto> CreateForSellerAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        CreateTicketCommand command,
        CancellationToken cancellationToken);

    /// <summary>پاسخ فروشنده.</summary>
    Task<TicketSnapshotDto> ReplyForSellerAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        Guid ticketId,
        ReplyTicketCommand command,
        CancellationToken cancellationToken);

    /// <summary>بستن تیکت فروشنده.</summary>
    Task<TicketSnapshotDto> CloseForSellerAsync(Guid sellerPartyId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>بازگشایی تیکت فروشنده.</summary>
    Task<TicketSnapshotDto> ReopenForSellerAsync(Guid sellerPartyId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>فهرست Admin با فیلتر.</summary>
    Task<TicketListPageDto> ListForAdminAsync(AdminTicketListQuery query, CancellationToken cancellationToken);

    /// <summary>جزئیات Admin شامل یادداشت داخلی.</summary>
    Task<TicketSnapshotDto?> GetForAdminAsync(Guid ticketId, CancellationToken cancellationToken);

    /// <summary>پاسخ Admin؛ پاسخ عمومی ممکن است اعلان بسازد.</summary>
    Task<TicketSnapshotDto> ReplyForAdminAsync(Guid actorUserId, Guid ticketId, ReplyTicketCommand command, CancellationToken cancellationToken);

    /// <summary>پچ وضعیت/اولویت/ارجاع Admin.</summary>
    Task<TicketSnapshotDto> PatchForAdminAsync(Guid ticketId, AdminTicketPatchCommand command, CancellationToken cancellationToken);
}
