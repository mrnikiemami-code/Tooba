using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Contracts.Commands;

/// <summary>
/// فرمان ایجاد idempotent اعلان برای مصرف‌کنندگان میان‌ماژولی.
/// </summary>
public sealed record CreateNotificationCommand(
    NotificationRecipientKind RecipientKind,
    Guid RecipientPartyId,
    Guid? RecipientActorUserId,
    string Type,
    object Payload,
    string TargetRoute,
    string SourceEventId,
    string SourceType);
