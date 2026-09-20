# Behavior preservation
- Cancel: still ReleaseHeld with error propagation
- Restore: durable reacquire via previous reservation; best-effort rollback release; same error keys (order.restore.inventory_failed)
- Manual review promote: Held→Promote; else reacquire under review TTL; same error keys (inventory.reservation.not_found / inventory.manual_review.unavailable)
- Manual reject: ReleaseIfHeld only
- Paid durable: EnsurePaidDurable with AllowReacquire; NewBindings applied; failures still swallowed in ApplyVerifiedSuccess (late money stays Paid)
