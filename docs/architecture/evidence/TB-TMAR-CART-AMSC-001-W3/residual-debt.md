# TB-TMAR-CART-AMSC-001-W3 — Residual debt

`blockingResidualDebt = ZERO`. The items below are recorded watch points only; none is a certification
prerequisite violation.

| # | Item | Classification | Note |
|---|---|---|---|
| R1 | `CartErrorCodes.LineCurrencyMissing` (`cart.line.currency_missing`) has no descriptor and no throwing site | pre-existing dead/unregistered code | Introduced by `f0cf9afb` (multi-currency lines). Both resx files localize it. Composed-catalog uniqueness is unaffected because no descriptor is registered. Left untouched during certification (removing or wiring it would be a behavior change). |
| R2 | `CartDirectory.cs` is 602 non-blank LOC (658 total lines) | cohesive, under ceiling | Single-responsibility adapter; W1 decomposed it from 831 LOC. Below the 800 LOC `ARCH-SIZE-001` ceiling and has no size-baseline entry. Durable guard `Cart_directory_stays_under_the_arch_size_ceiling`. |
| R3 | 7 CQRS requests are request+handler colocated per file | accepted capability-first shape | `Application/Carts/{Commands,Queries}` each hold >1 cohesive file; no duplicate request shape exists beside the authoritative MediatR request, and `Application/Models` is gone. |
| R4 | `Tooba.Cart.Tests` has 27 tests and no Testcontainers-gated integration project | pre-existing scope | Recorded in W0; not required for structure certification. |
| R5 | Untracked foreign artifacts in the working tree (`TB-TMAR-ORDER-AMC-001-W5-R1/worker-result.txt`, `TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/*`) | out of Cart scope | Deliberately left untracked and not committed by this wave. |
| R6 | `TmarSourceSizeAndInfraAppTests` 3 failing tests | repo-wide pre-existing drift | Dominant cause is the **untracked** `.tmp-baseline/` working copy (`git ls-files .tmp-baseline` → 0 files) that the scanner walks, plus stale baseline keys from other modules' migrations. The only Cart mention is the old `831` LOC reading of `CartDirectory.cs` taken from that untracked copy; the real file is 602 non-blank LOC with no baseline entry. Not repaired here. |

## Bounded repair performed in W3

`HostCartResidualGuardTests.StoreContext_owns_effective_store_commerce_and_BuildingBlocks_does_not`
read `Host/Tooba.Host/Outbox/OutboxWorkerSeams.cs`, a file that commit `382ef10a` (Host/Outbox namespace
migration) renamed to `WorkerStoreCommerceContextFactory.cs` without updating the guard. The guard
therefore failed with `FileNotFoundException`.

- Reproduced identically at W2 head `7364d793` and W1 head `a5ddc052` → the failure predates AMSC-001.
- Repair: one-line path update to the real current file.
- No assertion, baseline or exemption was weakened; the guard's substantive assertions (adapter must
  implement `IWorkerStoreCommerceContextFactory`, must not carry `"IR"` / `"IRR"` /
  `SalesChannel.Direct` / `SalesChannel.Marketplace` literals) are unchanged and now execute.
- Result: `HostCartResidualGuard` 14 passed / 0 failed.
