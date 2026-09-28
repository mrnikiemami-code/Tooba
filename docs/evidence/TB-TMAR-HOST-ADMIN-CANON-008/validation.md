# TB-TMAR-HOST-ADMIN-CANON-008 — Validation

## Build

```
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj          -> Build succeeded, 0 Error(s)
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -> Build succeeded, 0 Error(s)
```

## Focused guard run (task-mandated)

```
dotnet test Host/Tooba.Host.Tests --filter "HostAdminCanon008GuardTests|HostAdminCanon007GuardTests|HostAdminCanon006GuardTests"
  Passed!  - Failed: 0, Passed: 23, Skipped: 0, Total: 23
```

CANON-008 guard facts (6/6):

1. `Admin_root_has_zero_flat_cs_files` → PASS
2. `Admin_recursive_cs_file_count_is_15_and_membership_matches_target` → PASS
3. `Each_admin_file_namespace_matches_capability_path` → PASS (all 15, exact match)
4. `No_duplicate_old_path_files_or_root_namespace_shim_remain` → PASS
5. `Canon001_through_007_seams_are_preserved` → PASS
6. `No_module_specific_business_endpoint_file_reintroduced_in_admin` → PASS

## Regression sweep over directly-touched guards/tests (diagnostic, explicit filter only)

```
dotnet test Host/Tooba.Host.Tests --filter "HostAdminCanon*|HostAdminAmcW1GuardTests|
  HostAdminAmcW33MerchandisingGuardTests|HostOrderReverseAuditGuardTests|
  AdminPanelCompositionTests|AdminPanelAuthorizationTests|AdminDbNativeGridQueryTests|
  OrderSupplyUxTests|AdminReservationCycleAuditTests|AdminOrderCancelPrecedenceTests|
  AdminListGridQueryEngineTests"
  Failed: 3, Passed: 128, Total: 131
```

All CANON-001..008 guards, the W1/W33 Admin guards, `AdminPanelCompositionTests`,
`AdminPanelAuthorizationTests`, `AdminDbNativeGridQueryTests`, `AdminReservationCycleAuditTests`,
`AdminOrderCancelPrecedenceTests` and `AdminListGridQueryEngineTests` pass.

### The 3 remaining failures are pre-existing and unrelated to this change

Proof they are stale **at the task's parent commit** (`9a6be0fb9ae91970038d6a576f20e6f826badb13`),
before CANON-008 touched anything — verified with `git cat-file -e HEAD:<path>`:

```
ABSENT_AT_HEAD: src/backend/Host/Tooba.Host/Admin/AdminOrderOperationsComposer.cs
ABSENT_AT_HEAD: src/backend/Host/Tooba.Host/Admin/AdminOrderCompletenessComposer.cs
ABSENT_AT_HEAD: src/backend/Host/Tooba.Host/Admin/HoldPolicySettingsEndpoints.cs
ABSENT_AT_HEAD: src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminComposer.cs
ABSENT_AT_HEAD: src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminEndpoints.cs
ABSENT_AT_HEAD: src/backend/Host/Tooba.Host/Admin/ProductWorkspaceModels.cs
AT_HEAD:        src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs
```

1. `HostOrderReverseAuditGuardTests.Host_Order_reference_inventory_matches_discovered_production_files`
   and `.Host_OrderDbContext_consumers_are_enumerated_in_inventory`
   — stale R7 `host-order-reference-inventory.json` entries pointing at
   `Admin/*` files removed by `TB-TMAR-HOST-ADMIN-AMC-001-W32` and at Host/CustomerProfile files
   removed by `8060ba60` ("evacuate Host/CustomerProfile Development seed"). Both stale at HEAD.
   The drift string lists only entries that were already dead **before** CANON-008.

2. `OrderSupplyUxTests.List_items_carry_supply_status`
   — asserts `AdminPanelModels.cs` contains `SupplyStatus`; the model record no longer carries it
   (Order-owned supply projection). The chosen target path was corrected, the pre-existing
   content assertion was not satisfied at HEAD.

Both classes are outside the CANON-008 concern (physical structure + namespace alignment only).
Repairing them would require editing Order-domain models, an R7 Order audit inventory, or supply
projection semantics — behavior/ownership changes explicitly forbidden by this task. Not repaired.

## Minor cleanup performed (documented, evidence-only)

`docs/evidence/TB-TMAR-BOUNDARY-V1-R1/source-size-inventory.json` and
`docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R7/host-order-reference-inventory.json` had entries naming
`Host/Tooba.Host/Admin/AdminPanelComposer.cs` / `AdminPanelEndpoints.cs` / `AdminPanelModels.cs` /
`HostOrderAdminAuthorizer.cs` / `HostOrderAdminEffectiveAccessReader.cs`. Those entries were updated
to the new canonical paths so that the two inventories remain path-accurate for the files this task
moved. The same path-only refresh was applied to `tmar-source-size-baseline.json`. No LOC or
classification value was changed. The pre-existing missing-file entries in those baselines were left
exactly as they were.

## Task-rule-acknowledged side effects

`HostAdminCanon003GuardTests` and `HostAdminCanon004GuardTests` `ReadAdmin(fileName)` helpers were
switched from a flat-root join to a `SearchOption.AllDirectories` lookup (`.Single()`), because the
single file name is now at a nested canonical path. `Canon006/007` helpers received the identical
recursive lookup. This preserves the assertion suite exactly; no new test was added there.

## Budget

```
MAX_REPAIR_ITERATIONS      = 1   (0 used; no repair of task-introduced defect was needed)
MAX_VALIDATION_COMMAND_RUNS = 6  (focused build + focused guards + diagnostic sweep, within budget)
```
