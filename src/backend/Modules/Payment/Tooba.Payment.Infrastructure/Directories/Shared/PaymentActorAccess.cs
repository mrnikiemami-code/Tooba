using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Domain.Aggregates;

namespace Tooba.Payment.Infrastructure.Directories.Shared;

/// <summary>
/// بررسی مالکیت/دسترسی پرداخت بر پایه تصویر قابل‌پرداخت سفارش. دایرکتوری‌های Payment همین یک پیاده‌سازی را
/// به‌اشتراک می‌گذارند تا قاعدهٔ «order_identity_required» تک‌نسخه بماند.
/// </summary>
internal sealed class PaymentActorAccess(IPayableCheckoutReader orders)
{
    public async Task EnsureActorCanSeeAsync(
        CustomerPayment payment,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken)
    {
        var payable = await orders.GetPayableAsync(payment.CheckoutId, actorUserId, buyerPartyId, cancellationToken);
        if (payable is null)
        {
            throw new ContractOperationException("payment.access.order_identity_required");
        }
    }
}
