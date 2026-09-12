# TB-P10-T004-R5 — Manual Review Reservation Discovery

## Observed defect (TB-20260912010851-01-456f7b)

Manual proof left reservation on Cart TTL (~30m). Expiry worker Released the hold while Order stayed PendingPayment. Admin `confirm_deposit` → `CommitForPaidOrder` → `inventory.reservation.not_active`.

## Answers

1. **Why proof did not leave Cart TTL:** SubmitManualEvidence only mutated Payment attempt; Inventory ExpiresAt unchanged.
2. **Owner during Admin review:** Inventory Held reservation bound to OrderLine, with Manual Payment Review ExpiresAt (not Cart).
3. **Existing review deadline:** none before R5; added `Payment:Gateway:ManualPaymentReviewHoldHours` (default 24).
4. **Confirm after review expiry:** never resurrect Released; authoritative reacquire or `inventory.manual_review.unavailable`.
5. **Admin reject:** release Held review reservation; retry reacquirers on next evidence submit.
