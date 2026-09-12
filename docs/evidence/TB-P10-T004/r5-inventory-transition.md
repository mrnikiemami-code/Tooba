# TB-P10-T004-R5 — Inventory Transition

- Domain: `StockReservation.PromoteForManualPaymentReview`
- Directory: `PromoteReservationForManualPaymentReviewAsync`
- Order bridge: `PromoteReservationsForManualPaymentReviewAsync` (Held promote; Released → authoritative Reserve with review expiry + ReplaceReservation)
- Trigger: `StorefrontPaymentComposer.SubmitManualEvidenceAsync` after successful evidence validation

No foreign-module SQL. Released/Consumed never resurrected.
