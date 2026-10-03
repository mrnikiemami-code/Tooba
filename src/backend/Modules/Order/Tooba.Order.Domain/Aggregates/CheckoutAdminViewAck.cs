using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain.Aggregates;

/// <summary>
/// ثبت مشاهدهٔ Admin برای قفل حذف یادداشت پس از مشاهدهٔ کاربر دیگر.
/// </summary>
public sealed class CheckoutAdminViewAck
{
    /// <summary>سازندهٔ EF.</summary>
    private CheckoutAdminViewAck()
    {
    }

    /// <summary>شناسهٔ ack.</summary>
    public Guid AckId { get; init; }

    /// <summary>checkout مشاهده‌شده.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>مشاهده‌کننده.</summary>
    public Guid ViewerUserId { get; init; }

    /// <summary>زمان مشاهده.</summary>
    public DateTimeOffset ViewedAt { get; init; }

    /// <summary>ack جدید.</summary>
    public static CheckoutAdminViewAck Create(Guid checkoutId, Guid viewerUserId, DateTimeOffset now)
    {
        if (checkoutId == Guid.Empty || viewerUserId == Guid.Empty)
        {
            throw new InvalidOperationException("شناسهٔ مشاهده نامعتبر است.");
        }

        return new CheckoutAdminViewAck
        {
            AckId = UuidV7.New(),
            CheckoutId = checkoutId,
            ViewerUserId = viewerUserId,
            ViewedAt = now,
        };
    }
}
