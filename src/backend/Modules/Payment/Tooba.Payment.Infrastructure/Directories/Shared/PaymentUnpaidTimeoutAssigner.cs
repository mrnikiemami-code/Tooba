using Tooba.Payment.Application.Ports;
using Tooba.Payment.Domain.Aggregates;

namespace Tooba.Payment.Infrastructure.Directories.Shared;

/// <summary>
/// تخصیص مهلت پرداخت‌نشده از سیاست نگه‌داشت تجاری. initiation و reopen همین یک پیاده‌سازی را استفاده می‌کنند.
/// </summary>
internal sealed class PaymentUnpaidTimeoutAssigner(ICommerceHoldPolicy? holdPolicy)
{
    public void Assign(CustomerPayment payment, DateTimeOffset now)
    {
        if (holdPolicy is null)
        {
            return;
        }

        payment.AssignUnpaidTimeout(holdPolicy.ResolveUnpaidTimeoutAt(payment.ProviderCode, now), now);
    }
}
