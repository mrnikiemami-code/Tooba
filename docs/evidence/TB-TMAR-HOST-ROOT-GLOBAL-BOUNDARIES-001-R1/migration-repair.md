# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1 — Migration / Repair

## Preserved migration shape (from `chatgpt/host-root-boundaries-001`)

### Catalog
- `Tooba.Catalog.Contracts/Reservation/StoreHoldPolicySettingsContracts.cs`:
  `IStoreHoldPolicyHoursReader` (narrow read seam) introduced; existing
  `IStoreHoldPolicySettingsPort : IStoreHoldPolicyHoursReader`.
- `CatalogModule` registers `IStoreHoldPolicyHoursReader` from the existing
  persistence implementation.

### Payment
- `Tooba.Payment.Infrastructure/Adapters/CommerceHoldPolicySource.cs` owns platform
  and per-method hold semantics; consumes Catalog only via
  `Tooba.Catalog.Contracts.Reservation.IStoreHoldPolicyHoursReader`; implements
  `Tooba.Payment.Contracts.Hold.ICommerceHoldPolicySource`.
- `PaymentModule` registers `ICommerceHoldPolicySource`.
- `Payment.Infrastructure.csproj` adds `Catalog.Contracts` (Contracts-only edge).

### Order
- `Integrations/Payment/CheckoutReservationHoldPolicyAdapter.cs` consumes
  `Payment.Contracts.Hold` only and implements the Order checkout port.
- `ReservationCycle/UnpaidOrderExpiryWorker.cs` + `UnpaidOrderExpiryWorkerOptions.cs`
  are Order-owned and Host-independent.

### Host
- Six root files removed; business registrations removed from `Program.cs`;
  Settlement global-using shims removed.

## R1 repair applied

1. **Order registration isolated** into `ReservationCycle/ReservationCycleRegistration.cs`
   and consumed from Host composition, so `OrderModule.cs` and
   `Tooba.Order.Infrastructure.csproj` stay identical to `main`.
2. **Settlement global-using repair** in `Composition/ToobaModuleComposition.cs`:
   explicit `using Tooba.Settlement.Infrastructure.DependencyInjection;`.

## Worker behavior parity (verified by source diff, not assumption)

Diffing the removed `Host/UnpaidOrderExpiryHostedService.cs` against
`Order/.../UnpaidOrderExpiryWorker.cs` shows changes confined to the file header:

- namespace `Tooba.Host` → `Tooba.Order.Infrastructure.ReservationCycle`
- type name `UnpaidOrderExpiryHostedService` → `UnpaidOrderExpiryWorker`
- `UnpaidOrderExpiryHostOptions` → `UnpaidOrderExpiryWorkerOptions`
- Host-concrete seams replaced by their neutral platform interfaces:
  `WorkerCommerceContextFactory` → `IWorkerCommerceContextFactory`,
  `BackgroundWorkerRegistry` → `IBackgroundWorkerRegistry`
- removed redundant Host XML-inherit comments
- added required module `using` directives

`ExecuteAsync` body is otherwise **character-identical**: same `Enabled` gate,
`TimeSpan.FromSeconds(Math.Max(5, PollIntervalSeconds))`, tenant loop,
per-tenant fault isolation (`catch` count 4 → 4), `BatchSize`, logging, telemetry,
`IUnpaidOrderExpiryReconciler` usage, worker name `unpaid-order-expiry`, and the
`tooba.unpaid_expiry.expired` metric.

Options parity: section `Tooba:UnpaidOrderExpiry`, `Enabled = true`,
`PollIntervalSeconds = 15`, `BatchSize = 20` — all identical.

## Behavior parity checklist

| Responsibility | Status |
|----------------|--------|
| Precedence method > store > `Payment:Gateway` default | preserved (`CommerceHoldPolicySource`) |
| Clamp `1..24*30` | preserved |
| online hold / manual initial hold / manual review hold | preserved |
| `ResolveInitialExpiresAt` = `now + max(online, manual)` | preserved |
| `ResolveUnpaidTimeoutAt` manual-vs-online branches | preserved |
| `ResolveManualReviewExpiresAt` | preserved |
| unpaid expiry config defaults, tenant loop, fault isolation | preserved |
| metric `tooba.unpaid_expiry.expired`, worker name `unpaid-order-expiry` | preserved |
| schema / migration / frontend | unchanged |

## Test-surface repair (deterministic, in-scope)

- `CartLifetimeSeparationTests` — removed the instantiation of the deleted Host
  type; the behavior is now covered by a dedicated Payment-owned characterization
  suite rather than a Host-shell construction.
- `UnpaidOrderExpiryTests`, `ReservationCycleFoundationTests` — path assertions
  repointed from the deleted Host worker to the Order-owned worker.
- `HostRootGlobalBoundariesGuardTests` (new in 001) — unchanged.
- `HostFolderStructureTests` — the six evacuated names removed from the root allowlist.
- `HostCartResidualGuardTests` — the six evacuated names removed from the Cart-naming allowlist.
- `HostOrderReverseAuditGuardTests` — `UnpaidOrderExpiryHostedService` absence is now
  asserted from the new `hostRootGlobalBoundariesR1Update.removedFiles` record instead
  of the stale `files` list.
- `PaymentPrecertHygieneTests` — the DI smoke test now supplies the narrow Catalog
  Contracts read stub and Payment hold-override directory stub required by the new
  Payment-owned registration.

## New durable coverage

`Payment.Tests/Behavior/CommerceHoldPolicySourceTests.cs` — 8 characterization facts
for precedence, gateway overrides, clamping, and all three expiry resolutions,
replacing the deleted Host instantiation test with behavioral proof at the new owner.
