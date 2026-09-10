# R3 payment–expiry race — TB-P09-T022-R3

Race: cart TTL elapses after checkout but before/around payment success.

Mitigation:
1. Payment success commits reservations in `ApplyVerifiedSuccessAsync` (clears `ExpiresAt`) before fulfillment creation depends on the hold.
2. `EnsureCreatedForPaidCheckoutAsync` re-commits idempotently.
3. Focused test forces `ExpiresAt` into the past, then commits; expiry worker releases 0 rows and hold stays Held.
