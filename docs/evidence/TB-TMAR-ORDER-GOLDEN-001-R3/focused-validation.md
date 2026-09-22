# Focused validation
Order.Tests PASS 42/42. Full Tooba.slnx build PASS.
Host focused filters after lock clear (grid/completeness/foundation/durable/ops).
Ops migration incomplete → Status INCOMPLETE.

## Re-verification pass (post-commit)
- `dotnet build src/backend/Tooba.slnx` → 0 errors (24 warnings, all pre-existing).
- `Tooba.Order.Tests` → 42/42 PASS.
- Host focused filter (AdminOrderOperations | Grid | AdminReservationCycle |
  InvoiceHeaderSemantics | ManualPaymentAndListStatus | OrderSupplyUx |
  AdminPanelComposition | Completeness) → 61/61 PASS.

Two of those 61 were failing before this pass. Both were already failing at the
R2C baseline `a98aca6e` (52 Host failures there), so neither is an R3 regression:

- `AdminReservationCycleAuditTests.Order_detail_includes_one_projection_and_grids_batch`
  resolved the payments grid file under `Tooba.Host/Modules/Payment/...`, a path
  that has never existed. Repaired here: the helper now resolves module files
  under `src/backend/Modules`, and the payments anti-N+1 assertion matches the
  current batching shape (one `EnrichAsync(` call per page via
  `IPaymentAdminOrderEnrichmentReader`) instead of the retired
  `GetProjectionsAsync`/`GetStatusesAsync` names.
- `OrderSupplyUxTests.List_items_carry_supply_status` asserts the same retired
  `GetStatusesAsync`/`GetProjectionsAsync` names against the Payment CQRS query.
  The equivalent repair was prepared and verified green, but is **not** applied
  here: the follow-up R3B ops migration holds concurrent edits in that file, so
  the repair is left to R3B to avoid clobbering in-flight work.
