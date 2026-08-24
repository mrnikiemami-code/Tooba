# Expiry release (server-side)

Mechanism: `ICartDirectory.ExpireDueCartsAsync` plus `CartExpiryHostedService` (same tenant poll list as Outbox). Not `beforeunload`, not `sessionStorage`.

Test proof: `CartFoundationTests` backdates `ExpiresAt`, calls `ExpireDueCartsAsync`, asserts cart `Expired` and Offer availability restored (4 remaining while another active cart still holds 1 of the same Offer). Double `ReleaseAsync` on the expired hold id is a no-op.
