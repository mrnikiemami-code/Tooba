using Tooba.Order.Domain;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;

namespace Tooba.Order.Application.Checkout.Process;

/// <summary>ردیابی پایدار فرآیند checkout داخل مالک Order (بدون موتور Saga).</summary>
public interface ICheckoutProcessTracker
{
    /// <summary>فرآیند موفق موجود با کلید ارسال را برمی‌گرداند.</summary>
    Task<CheckoutProcess?> FindSucceededBySubmissionKeyAsync(string submissionIdempotencyKey, CancellationToken cancellationToken);

    /// <summary>فرآیند را در وضعیت Started ایجاد یا برای کلید موجود بازیابی می‌کند.</summary>
    Task<CheckoutProcess> BeginAsync(
        string submissionIdempotencyKey,
        Guid cartId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>مایلستون Validating.</summary>
    void MarkValidating(CheckoutProcess process, DateTimeOffset now);

    /// <summary>مایلستون رزرو.</summary>
    void MarkInventoryReserving(CheckoutProcess process, DateTimeOffset now);

    /// <summary>مایلستون پایدارسازی سفارش.</summary>
    void MarkOrderPersisting(CheckoutProcess process, Guid checkoutId, DateTimeOffset now);

    /// <summary>مایلستون تبدیل سبد.</summary>
    void MarkCartCommitting(CheckoutProcess process, DateTimeOffset now);

    /// <summary>مایلستون موفق submit.</summary>
    void MarkPaymentPending(CheckoutProcess process, DateTimeOffset now);

    /// <summary>شکست؛ فقط اگر ردیف هنوز track می‌شود.</summary>
    void MarkFailed(CheckoutProcess process, string failureCode, DateTimeOffset now);
}
