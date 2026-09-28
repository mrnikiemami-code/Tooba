# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1 — Certification Blocker Analysis

## The blocker as originally declared

The 001 migration touched `Tooba.Order.Infrastructure/OrderModule.cs` and
`Tooba.Order.Infrastructure.csproj`. That destination surface pre-exists with
direct foreign `*.Application` dependencies. The certify skill forbids PASS on a
touched surface while such a dependency remains.

## Required question

> Can the Order portion of this migration be made certifiable without making the
> already-dirty `OrderModule.cs` / `Tooba.Order.Infrastructure.csproj` part of this
> touched recovery surface?

## Audit of the pre-existing coupling (evidence)

The foreign `*.Application` references on the Order.Infrastructure surface are:

| File | Foreign Application namespace | Pre-existing on `main`? |
|------|-------------------------------|-------------------------|
| `OrderModule.cs` | `Tooba.Payment.Application.Models`, `Tooba.Payment.Application.Ports` | yes |
| `Integrations/Payment/OrderPaymentBridge.cs` | `Tooba.Payment.Application.Models`, `Tooba.Payment.Application.Ports` | yes |
| `Events/Payment/OrderPaymentSucceededHandler.cs` | `Tooba.Payment.Application.Models`, `Tooba.Payment.Application.Ports` | yes |

`.csproj` additionally references `Payment.Application`, `Fulfillment.Application`,
`AccessControl.Application` and others — all pre-existing.

Impact of the original 001 migration on that surface was exactly:

- `OrderModule.cs`: `+3` service registrations (`ICheckoutReservationHoldPolicy`,
  `UnpaidOrderExpiryWorkerOptions`, `UnpaidOrderExpiryWorker`).
- `Tooba.Order.Infrastructure.csproj`: `+1` reference to `Tooba.Payment.Contracts`.

## Resolution applied — `ISOLATED_FROM_TOUCHED_SURFACE_AND_CERTIFIABLE`

The migration's registration was moved **out of** the dirty surface into a new,
focused, capability-owned DI registration file:

```text
src/backend/Modules/Order/Tooba.Order.Infrastructure/ReservationCycle/ReservationCycleRegistration.cs
```

`AddOrderReservationCycleBoundaries(configuration)` registers the checkout hold
adapter, the worker options, and the worker. Host composition consumes it once:

```text
builder.Services.AddToobaModules(...);
builder.Services.AddOrderReservationCycleBoundaries(builder.Configuration);
```

Result:

- `OrderModule.cs` — **byte-identical to `main`** (zero diff).
- `Tooba.Order.Infrastructure.csproj` — **byte-identical to `main`** (zero diff).
- The required `Tooba.Payment.Contracts` type was already transitively reachable
  through the existing `Tooba.Payment.Application` reference, so **no new project
  reference** and **no new foreign edge** were introduced.
- `Program.cs` — the only production Host composition edit; it is a legitimate
  composition root and adds exactly one module-owned DI call.
- The three foreign `Payment.Application` references that remain on
  Order.Infrastructure are pre-existing `main` debt and are **not** part of this
  recovery's touched surface.

### New files introduced by this recovery (all clean)

- `ReservationCycleRegistration.cs` — DI composition only, no foreign Application/Infrastructure/Domain.
- `ReservationCycle/UnpaidOrderExpiryWorker.cs` — Order-owned worker, Host-independent.
- `ReservationCycle/UnpaidOrderExpiryWorkerOptions.cs` — Order-owned options.
- `Integrations/Payment/CheckoutReservationHoldPolicyAdapter.cs` — consumes `Tooba.Payment.Contracts.Hold` only.
- `Payment.Infrastructure/Adapters/CommerceHoldPolicySource.cs` — consumes `Tooba.Catalog.Contracts.Reservation` only.

### A note on the pre-existing `Payment.Application` edge

A searched alternative (relocating `PayableCheckoutSnapshot` / `OrderPaymentMode` /
`IPayableCheckoutReader` into `Payment.Contracts`) was evaluated and rejected: it
would relocate Payment module-owned projection DTOs and ports into Contracts purely
to satisfy a cross-module consumer, which the certify skill's semantic-contracts
rules forbid. It would also be an unbounded redesign far outside this bounded
recovery unit, and would touch three additional pre-existing dirty files.
