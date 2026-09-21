using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Models;

/// <summary>
/// فیلتر فهرست اعلان‌های یک گیرنده.
/// </summary>
public sealed record NotificationRecipientQuery(
    NotificationRecipientKind RecipientKind,
    Guid RecipientPartyId,
    Guid? RecipientActorUserId,
    int Skip,
    int Take,
    string Locale);

/// <summary>
/// آیتم فهرست اعلان با عنوان/متن محلی‌سازی‌شده در زمان خواندن.
/// </summary>
public sealed record NotificationListItemDto(
    Guid NotificationId,
    string Type,
    string Category,
    string Title,
    string Body,
    string PayloadJson,
    string TargetRoute,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt,
    string SourceType);

/// <summary>
/// صفحهٔ فهرست.
/// </summary>
public sealed record NotificationListPage(
    IReadOnlyList<NotificationListItemDto> Items,
    int Skip,
    int Take,
    long TotalCount,
    long UnreadCount);
