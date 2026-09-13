# TB-P10-T004-R21-R1 — Cancel Order

`POST /v1/storefront/checkout/{id}/cancel` → `StorefrontPendingPaymentComposer.CancelAsync`.

- Ownership: authenticated `PlacedByUserId` or guest cart proof + secret.
- Unpaid-only; Paid / succeeded-or-refund denied (`order.cancel.unpaid_only`).
- Dispatch/deliver blocked (`LOCK-OPS-002` / `order.cancel.forbidden`).
- Idempotent: already-cancelled returns `{ alreadyCancelled: true }`.
- Releases via `AbortForCheckoutCancelAsync` + `CancelSellerOrderAsync` + cycle `ReleasedByCancel`.
- Pending projector hides cancelled / `ReleasedByCancel`.
- Customer panel `paymentState=Cancelled` when all seller orders cancelled (still listed under لغو شده).
- Manual AwaitingAdmin follows unpaid cancel policy; no new semantics invented.
