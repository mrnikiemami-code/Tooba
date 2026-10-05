# TB-TMAR-INVENTORY-AMSC-001 — Wave 2 (Structure)

- **Skill:** `tooba-architecture-structure`
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/Inventory/Tooba.Inventory.*`
- **Parent waves:** `TB-TMAR-INVENTORY-AMSC-001-W0` (Analyze), `W1` (Migrate `133d413d`)
- **Starting HEAD:** `133d413d` (`main`, `HEAD == origin/main`)
- **Wave outcome:** `STRUCTURE_READY_FOR_CERTIFY`
- **Host touched:** NO (one Host test file added: the durable W2 structure guard)

---

## 1. Structured state

| Field | Value |
| --- | --- |
| **Structure-State** | `READY_FOR_CERTIFY` |
| **Folder-Granularity-State** | `PROFESSIONAL_SHALLOW` (W0 `PROFESSIONAL_SHALLOW_WITH_ONE_DEVIATION` closed) |
| **Path-Namespace-State** | `EXACT` (0 mismatches across 4 production projects) |
| **Root-Allowlist-State** | `EMPTY_ROOT_ON_ALL_PROJECTS` (no root `.cs` anywhere) |
| **Over-Foldering-State** | `NONE` (no single-file request leaf folder; no technical-axis-first request tree) |
| **Stale/Duplicate-Copy-State** | `NONE` (`InventoryDirectoryPorts.cs` deleted; no duplicate copy) |
| **Solution-Grouping-State** | `CANONICAL` (`/Modules/Inventory/` in `src/backend/Tooba.slnx`) |
| **Endpoints-Project-State** | `ABSENT_BY_DESIGN` (`INTERNAL_ONLY`) |
| **Manifest-State** | `PRE_CERT_RECORD_ADDED` (`preCertModules`, `structureCertified: false`) |
| **Structure-Handoff-State** | `READY_FOR_W3_CERTIFY` |

---

## 2. Physical layout (capability-first, shallow)

```text
Tooba.Inventory.Contracts/
    Availability/   IInventoryDevelopmentSeedGateway, IInventorySchemaMigrator,
                    InventoryAvailabilityContracts, InventoryQueryContracts
    Cart/           CartInventoryHoldContracts
    Checkout/       CheckoutInventoryReservationContracts
    Errors/         InventoryErrorCodes, InventoryErrorCatalogContributor, InventoryErrorResourceSet
    Fulfillment/    FulfillmentInventoryLifecyclePort
    Orders/         OrderInventoryLifecycleContracts
    Resources/      InventoryErrors.resx, InventoryErrors.fa.resx
    Returns/        InventoryReturnContracts
    Seller/         SellerOfferInventoryContracts

Tooba.Inventory.Domain/
    Aggregates/     InventoryLocation, StockPosition, StockReservation
    Events/         5 domain events
    ValueObjects/   status/kind enums + value objects

Tooba.Inventory.Application/
    Checkout/       CheckoutInventoryReservationAdapter
    Composition/    InventoryOperation                (typed-fault -> Result seam)
    Orders/         OrderInventoryLifecycleAdapter, OrderSupplyContracts
    Ports/          IInventoryDirectory, IInventoryUseCaseGuard, ReservationReceipt

Tooba.Inventory.Infrastructure/
    Adapters/            InventoryDevelopmentSeedGateway, InventoryModuleMigration,
                         InventoryQueryGateway, InventoryReturnGateway, InventorySchemaMigrator
    DependencyInjection/ InventoryModule
    Directories/         InventoryDirectory{,.Availability,.Lookups,.OrderSupply,.Reclaimer,.SellerWrite},
                         OpenInventoryUseCaseGuard
    Events/              InventoryIntegrationEvents
    Messaging/           InventoryOutboxRegistration
    Persistence/         InventoryDbContext, ReturnRestockInboxRecord
    Persistence/Migrations/  4 migrations + model snapshot

Tooba.Inventory.Tests/
    Architecture/   InventoryArchitectureGuardTests, InventoryAmcW1MigrateGuardTests
    Behavior/       InventoryReleaseIdempotencyTests
```

`Tooba.Inventory.Endpoints` does **not** exist and must not exist: Inventory is `INTERNAL_ONLY`
(`endpointOwnership = NOT_APPLICABLE`, `cqrs = INTERNAL_USE_CASE_BOUNDARIES`). Offer owns the seller
HTTP surface `/offers/{offerId:guid}/inventory`.

---

## 3. Root allowlists and forbidden roots

Every Inventory project has an **empty** root allowlist (no loose root `.cs`), recorded in
`docs/architecture/tmar-module-structure-manifests.json` → `preCertModules[Inventory]`:

| Project | `rootAllowlist` | `forbiddenRootFiles` | `forbiddenTopLevelFolders` |
| --- | --- | --- | --- |
| `Tooba.Inventory.Contracts` | `[]` | `InventoryErrorCodes.cs`, `InventoryContracts.cs` | `[]` |
| `Tooba.Inventory.Domain` | `[]` | `StockPosition.cs`, `StockReservation.cs` | `Entities`, `Policies` |
| `Tooba.Inventory.Application` | `[]` | `InventoryDirectoryPorts.cs`, `InventoryContracts.cs` | `Commands`, `Queries`, `Models`, `Validators` |
| `Tooba.Inventory.Infrastructure` | `[]` | `InventoryModule.cs`, `InventoryDirectory.cs`, `InventoryDbContext.cs` | `Migrations` |
| `Tooba.Inventory.Tests` | `[]` | `[]` | `[]` |

The record is added to `preCertModules` with `structureCertified: false` and `lockVersion:
ARCH-COMPLETE-002`; W3 promotes it to the certified `modules` array. The certified array,
`uncertifiedHttpOwningModules` and every other manifest record are byte-identical (additive patch,
66 inserted lines, zero removed).

---

## 4. Namespace alignment

`docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W2/audit-namespaces.js` walks all four
production projects and asserts `namespace == project + path-derived suffix` for every `.cs` file
(excluding `bin`/`obj`/`GlobalUsings`). Result: **0 mismatches**.

---

## 5. Durable structure guards

`Tooba.Host.Tests/Architecture/InventoryModuleAmsc001W2StructureGuardTests.cs` (10 facts):

1. `Inventory_remains_internal_only_with_no_endpoints_project` — no `Tooba.Inventory.Endpoints`, no
   `Host/Tooba.Host/Inventory`, no `MapInventoryEndpoints` anywhere in production, and the SoT
   `completeReferenceModules[Inventory]` still reads `INTERNAL_ONLY` / `NOT_APPLICABLE` /
   `INTERNAL_USE_CASE_BOUNDARIES`;
2. `Inventory_Application_is_capability_first_not_technical_axis_first` — `Checkout`/`Composition`/
   `Orders`/`Ports` present, no `Commands`/`Queries`/`Models`/`Validators` axis, no root `.cs`;
3. `Inventory_Contracts_holds_boundary_semantics_only` — no `IRequest<`/`IRequestHandler`/`DbSet<`,
   no Domain/Application/Infrastructure project reference, and the single code owner + catalog +
   resource set + both resx files exist;
4. `Inventory_has_single_stable_error_code_owner` — Domain declares no `public const string` code;
   the owner stays in `Contracts/Errors`;
5. `Inventory_directory_stays_cohesive_partials_with_guard_in_own_file` — the six directory partials
   plus the guard file exist, every `Directories/*.cs` is ≤ 500 LOC, and the guard type is not
   declared inside `InventoryDirectory.cs`;
6. `Inventory_application_ports_are_one_capability_per_file` — `InventoryDirectoryPorts.cs` absent and
   each port file declares exactly one type;
7. `Inventory_Infrastructure_uses_canonical_capability_folders` — the exact folder set
   `Adapters/DependencyInjection/Directories/Events/Messaging/Persistence`, migrations folded under
   `Persistence/Migrations`, no root `.cs`;
8. `Inventory_root_allowlists_match_disk_and_forbidden_roots_absent` — disk root `.cs` equals the
   manifest allowlist and no forbidden root file/folder exists;
9. `Inventory_production_path_equals_namespace_exactly` — the exact path↔namespace equality over all
   four production projects and the test project;
10. `Inventory_projects_grouped_under_Modules_Inventory_solution_folder` — the five projects are
    nested under `/Modules/Inventory/` in `Tooba.slnx`.

---

## 6. Solution grouping

`src/backend/Tooba.slnx` lines 165–171 already group the five Inventory projects under
`/Modules/Inventory/` (Domain, Contracts, Application, Infrastructure, Tests) — the canonical
COMPLETE_REFERENCE_PATTERN grouping (Domain, Contracts, Application, [Endpoints], Infrastructure,
Tests). No change was required; the guard locks it.

---

## 7. Verification performed

| Check | Result |
| --- | --- |
| `Tooba.Host.Tests` build | **succeeded** |
| `InventoryModuleAmsc001W2StructureGuardTests` | **10/10 passed** |
| `Tooba.Inventory.Tests` | **13/13 passed** (W1 guards unaffected) |
| Path↔namespace audit script | **0 mismatches** |
| `Tooba.Host.Tests` full run vs the W0 baseline failure set | **identical 82 pre-existing failures by test name, zero new failures** |

The 82 pre-existing failures are the repository-wide baseline at the W0 starting HEAD and include the
unrelated stale `TmarCompleteReferenceStructureGateTests` hardcoded module lists (Catalog and
CustomerProfile were certified without updating them), `HostOrderReverseAuditGuardTests` and the
`TmarSourceSizeAndInfraAppTests` evidence-count guard. None is Inventory-related and none changed.

---

## 8. Handoff to W3 (Certify)

W3 must:

1. promote the Inventory record from `preCertModules` to the certified `modules` array
   (`structureCertified: true`) and add `Inventory` to `structureLock.certifiedModules`;
2. reconcile the W2 structure guard's manifest assertion (preCert → certified), as the
   CustomerProfile/Identity precedent does;
3. update the stale hardcoded module lists in `TmarCompleteReferenceStructureGateTests` to the real
   certified set (Catalog, CustomerProfile, Inventory) — a repair of inherited drift, no assertion
   weakened;
4. add `InventoryModuleAmsc001W3CertGuardTests`;
5. record the W0–W3 SoT lineage (`inventoryModuleAmsc001W0..W3` with their commit SHAs) and the
   module-local Master Recovery checkpoint;
6. run the focused certification suite and confirm the zero-new-failure standard.
