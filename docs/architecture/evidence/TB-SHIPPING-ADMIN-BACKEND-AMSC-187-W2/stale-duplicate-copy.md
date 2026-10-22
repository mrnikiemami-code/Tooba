# Stale / duplicate physical copy

State: **`CLEAN`**

* The only move in W2 (`ShippingEndpointModule` from `Admin/ShippingRateCoverageEndpoints.cs` to
  `ShippingEndpointModule.cs`) left **no** leftover path: the source file still exists but no longer
  declares the moved type, and no second file declares `class ShippingEndpointModule`.
* No product of the move exists in two live homes. Verified by scanning every production `.cs` file
  in `Tooba.Shipping.Endpoints` for `class ShippingEndpointModule` (expect exactly one, at the root)
  and for `MapGroup("/v1/admin")` (expect exactly one, in the root composition file).
* No duplicate production file name inside any Shipping project (excluding the locked EF
  `Persistence/Migrations` designer/snapshot pair).
* `Tooba.slnx` has no entry pointing at a deleted/moved path (no path was deleted).
* No `bin/`/`obj/` artifact is treated as source; the pre-existing untracked `bin/Debug` outputs in
  `Tooba.Host.Tests` are unrelated build artifacts and were not committed.

Durable guard: `ShippingModuleStructureGuardTests.Endpoints_composition_entry_lives_at_the_root_and_nowhere_else`
and `...No_stale_or_duplicate_production_copy_exists`.
