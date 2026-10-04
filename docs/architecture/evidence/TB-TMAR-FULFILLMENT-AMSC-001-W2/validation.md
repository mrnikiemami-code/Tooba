# TB-TMAR-FULFILLMENT-AMSC-001 — W2 validation

## 1. Build

```
dotnet build src/backend/Tooba.slnx
Build succeeded.
    0 Warning(s)   (Fulfillment-scoped; solution-wide pre-existing analyzer warnings unchanged)
    0 Error(s)
```

## 2. Fulfillment module tests

```
dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj
Passed!  - Failed: 0, Passed: 69, Skipped: 0, Total: 69
```

69 = the 65 tests that passed at the end of W1 plus the 4 new W2 structure/cohesion guards.

## 3. Host regression baseline (byte-identical failure set)

Command:

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj ^
  --filter "FullyQualifiedName~Fulfillment|FullyQualifiedName~Shipping|FullyQualifiedName~Tmar|FullyQualifiedName~ErrorCatalog|FullyQualifiedName~AdminDbNativeGrid|FullyQualifiedName~PaidProjection"
```

| Run | Result |
| --- | --- |
| **Before W2** (`git stash push -u`) | Failed: 24 / Passed: 125 |
| **After W2** | Failed: 22 / Passed: 124 |

Both runs fail on the **same** pre-existing conditions, verified by comparing the failing
test-name sets:

- `FulfillmentLineQuantityOpsTests` / `FulfillmentLineQuantityOperationsTests` /
  `FulfillmentShipmentAllocationTests` — the Domain `InvalidOperationException` →
  `ContractOperationException` parity divergence already recorded in W0 §7 / W1 §4 and
  explicitly out of scope for W2.
- `TmarSourceSizeAndInfraAppTests`, `TmarFoundationTests`,
  `TmarCompleteReferenceStructureGateTests`, `TmarDurableGuardTests` — stale repo-wide
  baselines and pre-existing drift (`.tmp-baseline/` cruft, unbuilt `Catalog` WIP,
  `Catalog`/`AccessControl`/`Cart`/`Inventory`/`Order`/`Settlement` moves that never had
  their baselines/SoT updated). These fail **before** W2 on unrelated modules.
- `AdminDbNativeGridQueryTests.Non_trivial_composers_do_not_call_in_memory_Execute` —
  pre-existing.
- `PaidProjectionFinancialTests` — pre-existing.

No test that passed before W2 fails after W2, and no Fulfillment-path assertion regressed.

## 4. Namespace / path alignment

The module guard `AssertNamespacesAlign` enforces **exact** path-derived namespace equality
for all five Fulfillment projects and passes. The repo-wide
`TmarCompleteReferenceStructureGateTests.AssertNamespaceAlignment` still reports only the
**pre-existing** `Tooba.Catalog.Contracts.Cart` drift (unrelated module).

## 5. Structure guards

| Guard | Result |
| --- | --- |
| `Fulfillment_application_is_capability_first_with_no_technical_axis_root` | PASS |
| `Fulfillment_has_no_single_file_use_case_leaf_folders` | PASS |
| `Fulfillment_application_has_no_flat_mixed_contracts_bundles` | PASS |
| `Fulfillment_directory_stays_under_the_arch_size_ceiling` | PASS (794 / 440) |
| `Validator_folder_layout_is_capability_first_shallow` | PASS |
| `Fulfillment_golden_boundaries_and_physical_layout_remain_clean` | PASS |
| `Fulfillment_root_allowlists_and_forbidden_flattened_files_are_enforced` | PASS |

## 6. Stale / duplicate copy check

No leftover path holds a moved type; no dual live home exists for any responsibility.
The four empty technical-axis husk folders left behind by the moves
(`Commands/`, `Queries/`, `Models/`, `Ports/`, `Validators/{Admin,Seller,Customer,Shipping}/`)
were deleted from disk.

## 7. Solution grouping

`src/backend/Tooba.slnx` already groups all six Fulfillment projects under
`/Modules/Fulfillment/`; verified unchanged and consistent with disk.
