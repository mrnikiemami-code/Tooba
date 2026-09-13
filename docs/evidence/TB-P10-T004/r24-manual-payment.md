# TB-P10-T004-R24 — Manual payment

P-manual-order `01a09993-c0d4-7000-9fc5-041f25f03e11` 200. `providerCode: "manual"` 200. SellerOrder remains `PendingPayment` so open-unpaid=1. One Cycle #1 churn event only (payment path does not write `CheckoutReservationCommit`). Cancel still canonical after manual start. Hide remains presentation-only per hold rules. Admin confirm of paid is the existing payment projection path (same as sandbox success → Paid).
