using Tooba.BuildingBlocks;
using Tooba.Notification.Contracts.Errors;
using ContractsKind = Tooba.Notification.Contracts.Dtos.NotificationRecipientKind;
using DomainKind = Tooba.Notification.Domain.ValueObjects.NotificationRecipientKind;

namespace Tooba.Notification.Application.Models;

/// <summary>
/// نگاشت صریح Domain ↔ Contracts برای نوع گیرنده (مقادیر عددی پایدار).
/// مقدار ناشناخته یک خطای تایپ‌شده با کد پایدار ماژول است، نه استثنای خام.
/// </summary>
public static class NotificationRecipientKindMapping
{
    /// <summary>Contracts → Domain.</summary>
    public static DomainKind ToDomain(ContractsKind kind) => kind switch
    {
        ContractsKind.Customer => DomainKind.Customer,
        ContractsKind.Seller => DomainKind.Seller,
        _ => throw new ContractOperationException(NotificationErrorCodes.RecipientKindInvalid),
    };

    /// <summary>Domain → Contracts.</summary>
    public static ContractsKind ToContracts(DomainKind kind) => kind switch
    {
        DomainKind.Customer => ContractsKind.Customer,
        DomainKind.Seller => ContractsKind.Seller,
        _ => throw new ContractOperationException(NotificationErrorCodes.RecipientKindInvalid),
    };
}
