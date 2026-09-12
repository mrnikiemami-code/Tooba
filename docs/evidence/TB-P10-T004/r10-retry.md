# TB-P10-T004-R10 — Retry

Same PaymentId / CheckoutId. `EnsureOrderSupply(EnsureUnpaidRetryHold)` then `ReopenExpiredForRetryAsync` (new attempt + fresh `UnpaidTimeoutAt`).

Unavailable → `این سفارش در حال حاضر قابل تأمین نیست.` Endpoints: `POST /v1/storefront/payments/{id}/unpaid-retry` and `POST /v1/customer/orders/{checkoutId}/retry-unpaid`.
