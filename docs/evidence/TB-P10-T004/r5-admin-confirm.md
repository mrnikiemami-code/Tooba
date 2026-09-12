# TB-P10-T004-R5 — Admin Confirm

Within active review hold: `CommitReservationForPaidOrder` → durable Paid.

After Released (late confirm): `OrderPaymentBridge` reacquirers with `expiresAt: null`, ReplaceReservation, then Commit. Failure → `inventory.manual_review.unavailable` with actionable FA UX (not raw `not_active` as normal message).
