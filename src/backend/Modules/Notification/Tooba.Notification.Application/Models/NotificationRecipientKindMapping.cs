using ContractsKind = Tooba.Notification.Contracts.Dtos.NotificationRecipientKind;
using DomainKind = Tooba.Notification.Domain.ValueObjects.NotificationRecipientKind;

namespace Tooba.Notification.Application.Models;

/// <summary>
/// نگاشت صریح Domain ↔ Contracts برای نوع گیرنده (مقادیر عددی پایدار).
/// </summary>
public static class NotificationRecipientKindMapping
{
    /// <summary>Contracts → Domain.</summary>
    public static DomainKind ToDomain(ContractsKind kind) => kind switch
    {
        ContractsKind.Customer => DomainKind.Customer,
        ContractsKind.Seller => DomainKind.Seller,
        _ => throw new InvalidOperationException("notification.recipient_kind.invalid"),
    };

    /// <summary>Domain → Contracts.</summary>
    public static ContractsKind ToContracts(DomainKind kind) => kind switch
    {
        DomainKind.Customer => ContractsKind.Customer,
        DomainKind.Seller => ContractsKind.Seller,
        _ => throw new InvalidOperationException("notification.recipient_kind.invalid"),
    };
}
