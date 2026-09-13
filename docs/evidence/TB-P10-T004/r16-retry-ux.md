# R16 retry UX

- Active cycle + failed: CTA پرداخت, same cycle, same countdown
- Expired + retries remaining: «بررسی موجودی و پرداخت مجدد» → `retryStorefrontUnpaidPayment(paymentId, checkoutId)` using committed proof, then `/payment?checkoutId=`
- Backend retry path unchanged: `EnsureRetryAfterExpiryAsync` (new cycle only after reacquire)
- Unavailable: «این سفارش در حال حاضر قابل تأمین نیست.» no payment start
- Max cycles: no retry CTA + retry-limit copy
- Manual AwaitingAdmin: «در انتظار بررسی پرداخت», no pay-again
