# R3 reservation promotion — TB-P09-T022-R3

## Domain
`StockReservation.ExpiresAt` is `private set`.
`CommitForPaidOrder(now)`:
- Released/Consumed → `inventory.reservation.not_active` (no resurrect)
- Held + `ExpiresAt == null` → idempotent no-op
- Held + `ExpiresAt` set → `ExpiresAt = null`, `UpdatedAt = now`

## Application
`IInventoryDirectory.CommitReservationForPaidOrderAsync` loads, commits, saves, returns receipt via `FindReservationAsync`. Missing → `inventory.reservation.not_found`.

## Payment wiring
`OrderPaymentBridge` injects `IInventoryDirectory`. After `RecordVerifiedPayment`, each line `ReservationId` is committed before fulfillment is forward-usable. Admin ConfirmDeposit uses the same `ApplyVerifiedSuccessAsync` path.

## Fulfillment safety
`EnsureCreatedForPaidCheckoutAsync` also commits each handoff line reservation (idempotent) before `CreateFromPaidOrder`.
