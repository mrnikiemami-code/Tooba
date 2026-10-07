# TB-TMAR-PAYMENT-AMSC-001-W2 — structure (tooba-architecture-structure)

- Module: `Payment`
- Skill: `tooba-architecture-structure`
- Starting HEAD: `2d69d828` (W1 Migrate, == origin/main)
- Structure-Handoff-State: `READY_FOR_CERTIFY`
- Host final closure: preserved (zero Host production folder/file added, moved or widened)
- Behavior: `PRESERVED` (pure physical reorganization; zero route/DTO/validator/DI/schema change)

## 1. Classification states

| Axis | Before | After |
| --- | --- | --- |
| Folder-Granularity-State | `TECHNICAL_AXIS_FIRST` + 17 `OVER_FOLDERED` | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` (`/Modules/Payment/`, 6 projects) | `CANONICAL` (unchanged) |
| Path-Namespace-State | `EXACT` | `EXACT` |
| Physical-Copy-State | `CLEAN` | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` | `ENFORCED` (manifest `forbiddenTopLevelFolders` tightened) |
| File-Cohesion-State | `COHESIVE` | `COHESIVE` |
| Structure-State | `REPAIR_REQUIRED` | `READY_FOR_CERTIFY` |

## 2. The defect that was repaired

`Tooba.Payment.Application` was organized on the **technical axis** with one use-case folder per
request, each holding exactly one production source file:

- `Commands/<UseCase>/<UseCase>Command.cs` × 11 → `OVER_FOLDERED` (source-file count = 1)
- `Queries/<UseCase>/<UseCase>Query.cs` × 6 → `OVER_FOLDERED` (source-file count = 1)
- `Validators/{Admin,Storefront,Webhooks}/` → **audience** axis, not capability axis
- `Orchestration/`, `Models/`, `Ports/` at the Application root → technical axis at the top level

Payment has **four** real capabilities (Admin payment operations, Storefront payment journey, PSP
webhooks, stale-payment reconciliation), so the technical axis was the primary organization →
`TECHNICAL_AXIS_FIRST`.

## 3. Repaired tree (capability-first, shallow)

```text
Tooba.Payment.Application/
  Admin/         Commands/ (3)  Queries/ (2)  Validators/ (5)
  Storefront/    Commands/ (6)  Queries/ (4)  Validators/ (9)  Models/ (1)  Orchestration/ (2)
  Webhooks/      Commands/ (1)  Validators/ (1)
  Reconciliation/ Commands/ (1)
  Composition/   PaymentOperation.cs
  Models/        PaymentGatewayActorContext.cs, PaymentGatewayOutcomes.cs
  Ports/         5 files
  Validators/    PaymentFluentRules.cs, PaymentValidationCodes.cs
```

Namespaces are **full path-derived** (Content/Notification canonical style), e.g.
`Tooba.Payment.Application.Storefront.Commands`, not a flattened `...Application.Storefront`.

Moves performed with `git mv` (35 renames, history preserved): 17 request sources + 15 validators +
`StorefrontPaymentOrchestrator.cs` + `Orchestration/GlobalUsings.cs` + `StorefrontPaymentDtos.cs`.
The six emptied legacy directories (`Commands/`, `Queries/`, `Orchestration/`,
`Validators/{Admin,Storefront,Webhooks}/`) were deleted from disk — not merely emptied.

## 4. Mixed DTO bundle split by responsibility

`Application/Models/StorefrontPaymentDtos.cs` mixed two capabilities' read models. It was split **by
capability only**, member-for-member (no member renamed, no signature or body changed, no serialized
shape changed):

| Before | After |
| --- | --- |
| `Application/Models/StorefrontPaymentDtos.cs` = storefront provider codes + `PaymentStorefrontActors` + storefront DTOs **+** 4 admin grid read models | `Application/Storefront/Models/StorefrontPaymentDtos.cs` (storefront half) + `Application/Admin/Models/AdminPaymentGridDtos.cs` (admin grid half) |

The four admin grid records (`AdminPaymentGridItemDto`, `AdminPaymentGridPageDto`,
`AdminPaymentGridFilterInput`, `AdminPaymentGridQueryInput`) moved to the admin capability's own
`Models/` axis; their only consumers are `QueryAdminPaymentsGridQuery` and `PaymentAdminEndpoints`
(plus Payment behavior/validation tests). The storefront half keeps the storefront namespace.

## 5. Behavior preservation

Routes, HTTP methods, success DTO shapes, authorization semantics, business rules, state transitions,
ordering, cancellation, idempotency, transactions, persistence semantics, schema, migration IDs/Up-Down,
telemetry metric names, correlation/trace behavior and public Contracts: **unchanged**. W2 is pure
physical reorganization plus namespace/using repointing.

## 6. Durable guards added / updated

`Tooba.Payment.Tests/Architecture/PaymentModuleAmsc001W2StructureGuardTests.cs` (**new**, 8 tests)
- `Application_root_is_capability_first_with_no_technical_axis_root`
- `Capability_axes_are_flat_with_zero_per_use_case_request_leaves`
- `Request_sources_are_colocated_on_the_capability_axes`
- `Root_allowlists_and_forbidden_lists_match_the_manifest`
- `Path_derived_namespaces_are_exact`
- `Solution_grouping_is_canonical_modules_payment`
- `Endpoints_import_hygiene_stays_application_and_buildingblocks_only`
- `No_stale_or_duplicate_physical_copy_of_the_moved_surface_remains`

`Tooba.Payment.Tests/Architecture/PaymentArchitectureGuardTests.cs`
- `AllowedApplicationFolders` → capability-first allowlist; **new** `AllowedApplicationCapabilityFolders`,
  `AllowedApplicationSharedFolders`, `AllowedCapabilitySubFolders`.
- **New** `Payment_application_is_capability_first_with_no_technical_axis_root`.
- **New** `Payment_has_no_single_file_use_case_leaf_folders`.
- **New** `Payment_application_request_sources_are_colocated_on_capability_axes`.
- Orchestration assertion re-pointed to `Storefront/Orchestration`; GlobalUsings guard re-pointed.

`Tooba.Payment.Tests/Architecture/PaymentValidatorCoverageGuardTests.cs`
- `Validator_folder_layout_is_exactly_storefront_admin_webhooks` →
  `Validator_folder_layout_is_capability_first_shallow` (asserts per-capability validator counts
  5/9/1 on the capability axes and rejects the old audience tree).

`Tooba.Host.Tests/Architecture/PaymentModuleAmsc001W1MigrateGuardTests.cs`
- W1 request-scan re-pointed from `Application/Commands|Queries` to a recursive
  `Application/**/*Command.cs|*Query.cs` scan so the W1 semantics stay enforced without pinning the
  pre-W2 physical shape.

`Tooba.Host.Tests/{OrderSupplyUxTests,AdminReservationCycleAuditTests,ReservationCycleFoundationTests,
AtomicCheckoutCommitTests,StorefrontPayment*}.cs` — file-path literals repointed to the new tree.

## 7. Manifest interaction

`docs/architecture/tmar-module-structure-manifests.json` → `modules[Payment]` →
`Tooba.Payment.Application`:
- `rootAllowlist` stays `[]` (matched exactly).
- `forbiddenRootFiles` extended with `PaymentOperation.cs`, `PaymentFluentRules.cs`,
  `PaymentValidationCodes.cs` (root dumps of the W1/W2 seams must not resurrect).
- `forbiddenTopLevelFolders` = `["Commands", "Queries", "Orchestration"]` (the technical-axis-first
  roots are now explicitly forbidden).
- `structureCertified` was **not** flipped (that remains the W3 Certify verdict).

## 8. Focused validation

| Validation | Result |
| --- | --- |
| `Tooba.Payment.Application` build | succeeded, 0 errors |
| `Tooba.Payment.Tests` build | succeeded, 0 errors (0 `CS0105` after using-dedup) |
| `Tooba.Payment.Tests` | **107/107 passed** (was 96 + 8 new W2 guards + 3 reshaped/renamed) |
| `Tooba.Host.Tests` build | succeeded, 0 errors |
| Host `Payment` filter | **90 passed / 0 failed / 2 skipped** |
| Host Payment/Storefront/Wallet/Unpaid/ReservationCycle/AtomicCheckout/OrderSupplyUx filter | 225 passed / 2 failed / 5 skipped |
| Pre-existing unrelated failures | `HostStorefrontAmcR1GuardTests.Catalog_owns_template_preview_cqrs_infra_and_routes` (Catalog `Results.Json` WIP) + `HostStorefrontAmcR3GuardTests.Downstream_host_adapters_use_catalog_composer_port` (missing `Modules/Wishlist` file) — **not Payment**, unchanged by W2 |
| `.slnx` | `/Modules/Payment/` group unchanged, all 6 projects present and matching disk |

## 9. Handoff

- `Structure-State = READY_FOR_CERTIFY` — all structure §27 gates met.
- Residual for W3 Certify: manifest/SoT certification sync for the W2 physical truth and a durable
  `PaymentModuleAmsc001W3CertGuardTests`.
- Host final closure preserved: `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` and
  `HOST_ROOT_FINAL_CERTIFIED` untouched; zero Host production files added.
