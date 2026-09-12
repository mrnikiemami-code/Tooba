# TB-P10-T004-R13 — Error mapping

| Condition | Host | Customer copy |
| --- | --- | --- |
| Guest secret missing/wrong after commit; checkout exists | 403 `checkout.access.denied` | دسترسی به اطلاعات پرداخت این سفارش تأیید نشد. لطفاً از بخش سفارش‌ها دوباره وارد پرداخت شوید. |
| Same on payment initiate/Get | 403 `payment.access.denied` | same |
| Checkout not found | 404 `checkout.missing` | سفارش پیدا نشد. |
| Pre-commit submit failure | `checkout.rejected` | ثبت سفارش انجام نشد. لطفاً دوباره تلاش کنید. |
| Converted Cart leftover in session | client rotates; no toast | none |
| Converted mutation if forced | `cart.rejected` | عملیات سبد انجام نشد. (normal path does not hit this) |

Never: post-commit ownership → «ثبت سفارش انجام نشد».
Never: raw 401/403, status enums, or JSON in UX (`toCustomerCheckoutMessage` / `toCustomerPaymentMessage`).
