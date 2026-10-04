# TB-TMAR-CART-AMSC-001-W3 — Focused validation

Starting HEAD: `7364d793` (AMSC-001 W2).

## Passed

| Validation | Command | Result |
|---|---|---|
| Cart module tests | `dotnet test Modules/Cart/Tooba.Cart.Tests/Tooba.Cart.Tests.csproj` | **27 passed / 0 failed / 0 skipped** |
| New W3 certification locks | `dotnet test Host/Tooba.Host.Tests --filter FullyQualifiedName~CartModuleAmsc001W3CertGuardTests` | **11 passed / 0 failed** |
| Host Cart residual guard | `--filter FullyQualifiedName~HostCartResidualGuard` | **14 passed / 0 failed** |
| Composed error-catalog uniqueness | `--filter FullyQualifiedName~ErrorCatalogUniqueCodeGuardTests` | **3 passed / 0 failed** |
| Host composition build | `dotnet build Host/Tooba.Host/Tooba.Host.csproj` | **0 errors** |

## Pre-existing drift (not caused by AMSC-001, not repaired by this wave)

`TmarSourceSizeAndInfraAppTests` has 3 failing tests
(`Hand_written_source_size_does_not_expand_beyond_baseline`,
`Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline`,
`Source_size_inventory_evidence_exists_and_matches_scan_count`). They are repo-wide and
pre-existing:

- The dominant cause is an **untracked** `.tmp-baseline/` working copy (proven: `git ls-files .tmp-baseline`
  returns 0) that the scanner walks, producing dozens of `NEW_OVERSIZED_FILE` findings for files that do
  not exist in the repository (`src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs`,
  `.tmp-baseline/...`). This is the same drift recorded in the AddressBook W3 SoT record (watch `R4`/`R5`).
- The remaining findings are stale size-baseline keys from other modules' own migrations
  (Catalog, Fulfillment, AccessControl, Order, Payment, Settlement, Host `Admin/*`, frontend), unrelated to Cart.
- The only Cart reference in the report is `.tmp-baseline/src/backend/Modules/Cart/.../CartDirectory.cs`
  with `currentLoc=831` — the *old* W1 pre-decomposition size read out of the untracked baseline copy.
  The real file is 602 non-blank LOC (658 total lines), below the 800 threshold, and has no baseline entry.
  The module-local durable guard `Cart_directory_stays_under_the_arch_size_ceiling` passes.

Per the certification skill ("do not repair unrelated pre-existing failures, do not repeatedly run the full
repository suite"), this drift is recorded, not repaired, and it does not touch the certified Cart surface.

## Bounded repair (in scope)

`HostCartResidualGuardTests.StoreContext_owns_effective_store_commerce_and_BuildingBlocks_does_not` read a
Host file path that commit `382ef10a` had renamed. Reproduced identically at W1 `a5ddc052` and W2
`7364d793`, so the failure predates AMSC-001. Fixed with a one-line path correction; no assertion weakened.
See `residual-debt.md`.
