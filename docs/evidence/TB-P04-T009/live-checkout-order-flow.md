# TB-P04-T009 — Live checkout → order flow

Buyer context: guest cart (`CartAccess` with guest secret). `PlacedByUserId` is the storefront guest actor `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`. `BuyerPartyId` is null in this slice; later Customer Address/Party persistence is a documented seam.

Shipping: checkout-owned snapshot (recipient, mobile, province, city, address, postal code). Method code `storefront-default` / label «ارسال پیش‌فرض فروشگاه». Amount `0`. No carrier matrix.

Commercial path: Cart hold already exists → Checkout copies `ReservationId` onto order lines → Pricing re-quote → Promotion evaluate → Tax calculate → persist `CheckoutGroup` + seller orders `PendingPayment` → convert cart. No distributed SQL transaction.

Payment: `PendingPayment` only. Order is not marked Paid.

Duplicate submit: unique `CartId` + `IdempotencyKey`; second submit returns the same `CheckoutId`.

Secrets: guest secret and personal address must be redacted from published screenshots.
