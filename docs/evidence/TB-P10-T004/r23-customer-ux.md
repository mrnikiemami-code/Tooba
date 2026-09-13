# TB-P10-T004-R23 — Customer UX

Host returns business FA detail + metadata. FE `StorefrontCheckoutLimitNotice`:

- open unpaid: «مشاهده سفارش‌های در انتظار پرداخت» (`#pending-payments`) and «سفارش‌های من» (`/customer-panel/orders`)
- churn: cart remains; copy says stock was not reserved

Cart wrapper `id="pending-payments"` on storefront cart. Shipping maps the same error codes. Raw codes are not shown as the customer sentence.
