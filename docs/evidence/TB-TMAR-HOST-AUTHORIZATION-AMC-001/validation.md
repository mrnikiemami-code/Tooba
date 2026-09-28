# TB-TMAR-HOST-AUTHORIZATION-AMC-001 — Validation

Budget respected: bounded focused runs only. No solution-wide build, no broad architecture suite, no
open-ended repair loop.

## Builds

| # | Command | Result |
| --- | --- | --- |
| 1 | `dotnet build Host/Tooba.Host/Tooba.Host.csproj` | PASS, 0 warnings, 0 errors |
| 2 | `dotnet build Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj` | PASS, 0 errors |

## Focused guard / behavior runs

| # | Filter | Result |
| --- | --- | --- |
| 3 | `HostAuthorizationEvacuationGuardTests`, `AuthorizationFoundationTests`, `HostAdminCanon007GuardTests`, `HostFolderStructureTests`, `TmarCompleteReferenceStructureGateTests` | **Passed 32, Failed 0** |
| 4 | `AccessControlFoundationTests`, `AdminPanelAuthorizationTests`, `SellerPanelAuthorizationTests`, `AccessControlRuntimeScopeTests`, `ContentPermissionEnforcementTests`, `SpiceDbIntegrationTests` | **Passed 20, Skipped 8, Failed 0** |

Skips in run 4 are the pre-existing Docker/Testcontainers-gated SpiceDB integration facts.

## Durable guard added

`src/backend/Host/Tooba.Host.Tests/Architecture/HostAuthorizationEvacuationGuardTests.cs` — 7 facts:

1. `Host_authorization_folder_is_evacuated` — no `Host/Authorization`, no `Health/SpiceDbHealthProbe.cs`.
2. `Module_owns_the_spicedb_adapter_slice_with_exact_paths` — exact 7-file slice exists in
   `Tooba.AccessControl.Infrastructure/Authorization`, and the directory contains exactly 7 files.
3. `Host_has_no_spicedb_sdk_or_authorization_ngrpc_code` — no `Authzed.Api.V1` /
   `SpiceDbAuthorizationAdapter` in any Host source; no `Authzed.Net` in `Tooba.Host.csproj`.
4. `Host_composition_binds_the_module_authorization_slice` — `Program.cs` imports the module
   namespace, calls `AddToobaModules`, and no longer calls `AddToobaAuthorization()` directly;
   `AccessControlModule` binds the options section and calls it.
5. `Authorization_configuration_section_and_modes_are_preserved` — `Tooba:Authorization`, `Disabled`,
   `InMemory`, `SpiceDb`, and both production validation messages.
6. `Schema_version_and_fail_closed_contract_are_preserved` — `SchemaVersion => 3`,
   `definition capability`, `definition category`, `AuthorizationDecision.Unavailable(_reason)`.
7. `Adapter_does_not_classify_failures_by_exception_message` — no `ex.Message ==`;
   `SpiceDbUnavailableException` present.

## Path-pinned guard updated

`Architecture/HostAdminCanon007GuardTests.cs` — path assertions repointed from
`Host/Authorization/…` and `Host/Health/SpiceDbHealthProbe.cs` to the module slice. Assertion
strength unchanged (same facts, corrected ownership path); no guard weakened.

## Pre-existing, unrelated failures (NOT caused by this task)

`TmarSourceSizeAndInfraAppTests` is red at clean `HEAD 3ad429dc` for repository-wide debt unrelated
to this slice:

- `Hand_written_source_size_does_not_expand_beyond_baseline` — the violations list contains
  `BASELINE_ENTRY_MISSING_FILE` / `OVERSIZED_GROWTH` entries for `Host/AccessControl/AccessControlEndpoints.cs`,
  `Host/Admin/AdminOrder*Composer.cs`, `Modules/Cart/.../Directories/CartDirectory.cs`,
  `Modules/Catalog/...`, `Modules/Fulfillment/...`, `Modules/Order/...`, `Modules/Settlement/...`,
  `Modules/Payment/...`, `Host/Storefront/StorefrontComposer.cs` (1620 vs 1579 baseline). **No**
  violation entry references `Authorization/`, `SpiceDbAuthorization*`, `SpiceDbHealthProbe`,
  `AccessControlModule.cs` or `AuthorizationRegistration.cs` — the evacuated slice adds none.
- `Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline` — new edges are
  Order→AccessControl.Application, Order→Fulfillment.Application, Promotion→Inventory/Party/Pricing.Application.
  All are pre-existing module edges from earlier waves; this task added no module→module edge.
- `Source_size_inventory_evidence_exists_and_matches_scan_count` — stale inventory evidence.

These are the same repository-wide baseline/inventory debt already recorded by
`TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001` §13/§17 and by
`TB-TMAR-HOST-AUTHENTICATION-SPLIT-001` (`preExistingFailuresNotCausedByThisTask`). They are
explicitly classified as out of scope and were **not** repaired, and no baseline/guard was weakened
to hide them.

## Not touched

- No schema/migration change, no route change, no frontend change.
- No AccessControl business type, handler, validator or endpoint modified.
- No solution-wide test run.
