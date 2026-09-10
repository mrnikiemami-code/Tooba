# R3 restore regression — TB-P09-T022-R3

`CheckoutDirectory.RestoreCancelledCheckoutAsync` already calls `ReserveAsync(..., expiresAt: null)`. Documented in code. Restored holds are durable (not cart TTL) per LOCK-OPS-028.
