# TB-P10-T004-R21-R1 — Hide Pending

`POST /v1/storefront/checkout/{id}/hide-pending-card`.

- Denied while reservation Active with remaining seconds (`pending.hide.active_hold`).
- Inserts `order.pending_payment_card_hides` scoped by `OwnerUserId` or `GuestCartId`.
- Does not mutate Order / Payment / Supply / Reservation / Cycle history.
- Pending list filters hidden IDs only; customer Orders and unpaid dashboard counts still include the Order.
- Customer A hide does not match Customer B owner key.
- FE calls backend then caches locally; reload uses server list. Not localStorage-only SoT.
- Does not remove committed checkout proof.
