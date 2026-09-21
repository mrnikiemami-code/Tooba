using Tooba.Notification.Application.Models;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Domain.Aggregates;

namespace Tooba.Notification.Application.Ports;

/// <summary>
/// دایرکتوری اعلان‌های تراکنشی پایدار (سطح Application؛ شامل خواندن/علامت).
/// ایجاد میان‌ماژولی از طریق <c>INotificationCreationPort</c> در Contracts.
/// </summary>
public interface INotificationDirectory
{
    /// <summary>اگر SourceEventId تکراری باشد ایجاد نمی‌کند.</summary>
    Task<UserNotification?> CreateIfAbsentAsync(CreateNotificationCommand command, CancellationToken cancellationToken);

    /// <summary>فهرست گیرنده به‌ترتیب جدیدترین.</summary>
    Task<NotificationListPage> ListAsync(NotificationRecipientQuery query, CancellationToken cancellationToken);

    /// <summary>تعداد خوانده‌نشدهٔ گیرنده.</summary>
    Task<long> UnreadCountAsync(
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken);

    /// <summary>علامت خوانده‌شدن یک اعلان متعلق به گیرنده؛ idempotent.</summary>
    Task<bool> MarkReadAsync(
        Guid notificationId,
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken);

    /// <summary>همهٔ خوانده‌نشده‌های گیرنده را می‌خواند؛ idempotent.</summary>
    Task<int> MarkAllReadAsync(
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken);

    /// <summary>حذف نرم اعلان متعلق به گیرنده.</summary>
    Task<bool> SoftDeleteAsync(
        Guid notificationId,
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken);
}
