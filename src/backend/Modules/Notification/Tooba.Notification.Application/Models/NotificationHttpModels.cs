namespace Tooba.Notification.Application.Models;

/// <summary>HTTP list item projection (preserves wire JSON names via camelCase).</summary>
public sealed record NotificationListHttpItem(
    Guid NotificationId,
    string Type,
    string Category,
    string Title,
    string Body,
    string TargetRoute,
    bool IsRead,
    DateTimeOffset CreatedAt);

/// <summary>HTTP list page projection.</summary>
public sealed record NotificationListHttpResponse(
    IReadOnlyList<NotificationListHttpItem> Items,
    int Skip,
    int Take,
    long TotalCount,
    long UnreadCount);

/// <summary>HTTP unread-count projection.</summary>
public sealed record NotificationUnreadCountResponse(long UnreadCount);

/// <summary>HTTP mark-all projection.</summary>
public sealed record NotificationMarkedCountResponse(int MarkedCount);

/// <summary>Maps directory list pages to the preserved HTTP response contract.</summary>
public static class NotificationHttpMapper
{
    /// <summary>Projects a directory page into the wire list response.</summary>
    public static NotificationListHttpResponse ToListResponse(NotificationListPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        var items = page.Items
            .Select(x => new NotificationListHttpItem(
                x.NotificationId,
                x.Type,
                x.Category,
                x.Title,
                x.Body,
                x.TargetRoute,
                x.IsRead,
                x.CreatedAt))
            .ToArray();
        return new NotificationListHttpResponse(items, page.Skip, page.Take, page.TotalCount, page.UnreadCount);
    }
}
