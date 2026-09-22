# Host delta — TB-TMAR-ORDER-GOLDEN-001-R8

## Removed from Host

| File | Role |
|------|------|
| `Host/ReservationCycleCoordinator.cs` | Retry-after-expiry orchestration, max-cycle, reacquire, cycle start |
| `Host/ReservationCyclePolicyResolver.cs` | Policy precedence merge via CatalogDbContext |
| `Program.cs` DI for Host coordinator + Host policy resolver | Host registrations |

## Moved / owned by Order

| Surface | Location |
|---------|----------|
| Policy resolver | `Order.Application/ReservationCyclePolicyResolver.cs` |
| Cycle coordinator | `Order.Application/ReservationCycleCoordinator.cs` + `IReservationCycleCoordinator` |
| Checkout line port | `IReservationCycleCheckoutLineSource` → `Order.Infrastructure/ReservationCycleCheckoutLineSource.cs` |
| Unpaid expiry reconciler | `IUnpaidOrderExpiryReconciler` → `Order.Infrastructure/UnpaidOrderExpiryReconciler.cs` |
| DI | `OrderModule` registers resolver, coordinator, line source, reconciler |
| Catalog hold overrides | `Catalog.Contracts.Reservation.IReservationCycleHoldPolicyReader` |
| Catalog reader impl | `Catalog.Infrastructure/Reservation/ReservationCycleHoldPolicyReader.cs` |
| Clock | `IClock` (no `DateTimeOffset.UtcNow` in moved business logic) |
| Retry limit fault | `ContractOperationException(ReservationCycleErrors.RetryLimitReached)` |

## Host shell retained

| Surface | Role |
|---------|------|
| `UnpaidOrderExpiryHostedService` | BackgroundService tenant loop / config / logging / telemetry only; calls reconciler |
| `UnpaidOrderExpiryHostOptions` | Host worker config |
| `ReservationPolicyAdminEndpoints` / Composer | Compile adaptation only (PreviewMany via interface; no Host concrete cast) |
| `CustomerPanelComposer` | Injects `IReservationCycleCoordinator` (panel migration = R9) |

## Still remaining in Host for Order

- CustomerPanel Order authority (R9)
- SellerPanel Order authority (R10)
- Admin dashboard / list / grids (R11)
- ReservationPolicyAdmin Catalog write path (out of R8 ownership move)

## SoT

- Order: `INCOMPLETE_REFERENCE_REPAIR`
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`
- nextTask: `TB-TMAR-ORDER-GOLDEN-001-R9` (not started)
- Slice: `RESERVATION_CYCLE_POLICY_RETRY_EXPIRY_R8`
