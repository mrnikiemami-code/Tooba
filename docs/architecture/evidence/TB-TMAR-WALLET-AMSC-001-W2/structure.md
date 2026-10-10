# TB-TMAR-WALLET-AMSC-001-W2 — structure (`tooba-architecture-structure`)

```text
TASK:           TB-TMAR-WALLET-AMSC-001-W2
MODE:           ARCHITECT_DIRECT_MANUAL_AMSC
SKILL:          tooba-architecture-structure
TARGET:         src/backend/Modules/Wallet/Tooba.Wallet.*
PARENT:         TB-TMAR-WALLET-AMSC-001-W1  (commit c802fe81)
STARTING HEAD:  c802fe81
STATE:          READY_FOR_CERTIFY
LOCK VERSION:   ARCH-COMPLETE-002
```

W2 is a **behavior-preserving physical reorganization** of `src/backend/Modules/Wallet`. Routes, success
payloads, wire-visible outcome codes, descriptor set, resource keys/text, domain invariants, DI
lifetimes, the `wallet` schema and the migration set are unchanged. Every edit is a path move, a
namespace/using repoint, a file split by capability, or a durable guard update.

---

## 1. Classification states

| Axis | Before (W1 head `c802fe81`) | After |
| --- | --- | --- |
| Module applicability | `HTTP_OWNING` (11 routes / 3 groups) | `HTTP_OWNING` (unchanged) |
| Folder-Granularity-State | `TECHNICAL_AXIS_FIRST` (Application `Commands/<UseCase>/` + `Queries/<UseCase>/`) | `PROFESSIONAL_SHALLOW` (capability-first) |
| Solution-Explorer-State | `CANONICAL` (`/Modules/Wallet/`, 6 projects) | `CANONICAL` (unchanged, re-verified) |
| Path-Namespace-State | `MISMATCH` (11 Application files + 2 Infrastructure files) | `EXACT` |
| Physical-Copy-State | `CLEAN` | `CLEAN` |
| Root-Allowlist-State | `VIOLATION` (no manifest entry) | `ENFORCED` (manifest `preCertModules[Wallet]`) |
| File-Cohesion-State | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (`WalletDirectory.cs` = 4 capabilities in 682 LOC) | `COHESIVE` (5 capability partials, all < 300 LOC) |
| Structure-State | `REPAIR_REQUIRED` | `READY_FOR_CERTIFY` |

---

## 2. Defects repaired

### 2.1 `TECHNICAL_AXIS_FIRST` Application tree

The multi-capability Application project organized its requests on the **technical** axis with a
use-case-named single-file leaf per request:

```text
BEFORE  Tooba.Wallet.Application/
          Commands/RedeemCustomerGiftCard/RedeemCustomerGiftCardCommand.cs
          Commands/IssueAdminGiftCard/IssueAdminGiftCardCommand.cs
          Commands/RevokeAdminGiftCard/RevokeAdminGiftCardCommand.cs
          Commands/AdjustAdminWallet/AdjustAdminWalletCommand.cs
          Queries/GetCustomerWalletSummary/GetCustomerWalletSummaryQuery.cs
          Queries/ListCustomerWalletLedger/ListCustomerWalletLedgerQuery.cs
          Queries/GetAdminGiftCard/…  Queries/GetAdminWallet/…  Queries/GetWalletDemoPreview/…
          Queries/ListAdminGiftCards/…  Queries/ListAdminWalletLedger/…
          Models/WalletDtos.cs   (157 LOC, 4 capabilities in one bundle)
```

Every `Commands/<UseCase>/` and `Queries/<UseCase>/` folder contained exactly **one** production
source file — the canonical single-file request leaf defect (skill §8). W2 retires the tree:

```text
AFTER   Tooba.Wallet.Application/
          Admin/{Commands,Queries,Models}/
          Customer/{Commands,Queries,Models}/
          Payments/Models/   Refunds/Models/
          Composition/  Ports/  Models/  Validation/
```

`Admin` / `Customer` / `Payments` / `Refunds` are the module's real business capability axes (they
already exist as `Wallet.Contracts.{Payments,Refunds}` ports and `Endpoints.{Admin,Customer}` groups).
No capability name was invented from a command type name. The technical axes survive only as the
**secondary** level under the owning capability, exactly as the standard prescribes.

`Models/WalletDtos.cs` was split **by capability**, not cosmetically:

| Destination | Types |
| --- | --- |
| `Customer/Models/WalletModels.cs` | `WalletSummaryDto`, `WalletLedgerEntryDto`, `WalletLedgerPageDto`, `RedeemGiftCardCommand`, `GiftCardRedeemResultDto` |
| `Admin/Models/WalletModels.cs` | `GiftCardSummaryDto`, `GiftCardDetailDto`, `GiftCardRedemptionDto`, `GiftCardListPageDto`, `GiftCardIssueResultDto`, `IssueGiftCardCommand`, `AdminGiftCardListQuery`, `AdminWalletAdjustmentCommand`, `AdminWalletAdjustmentResultDto`, `WalletDemoPreviewDto` |
| `Payments/Models/WalletModels.cs` | `WalletSpendResultDto` |
| `Refunds/Models/WalletModels.cs` | `WalletCreditResultDto` |

Zero type, member or record-positional-shape change; only the declaring namespace moved.

### 2.2 `MULTI_RESPONSIBILITY_COHESION_VIOLATION` — `WalletDirectory.cs`

`WalletDirectory` implemented **three** interfaces (`IWalletDirectory`, `IWalletOrderPaymentPort`,
`IWalletRefundCreditPort`) and held customer + admin + order-payment + refund + quote use cases plus
mapping helpers in one 682-LOC file (`ARCH-MODULE-FILE-001`). W2 first **moved** it out of the
technical-axis `Infrastructure/Directories/` folder into `Infrastructure/Persistence/` (next to its
own `WalletDbContext`) and then split it into five cohesive partial files:

| File | LOC | Responsibility |
| --- | --- | --- |
| `Persistence/WalletDirectory.cs` | 143 | type declaration, ctor, shared account/ledger core, mapping helpers |
| `Persistence/WalletDirectory.Customer.cs` | 112 | owner summary, owner ledger, gift-card redemption |
| `Persistence/WalletDirectory.Admin.cs` | 174 | gift-card list/detail/issue/revoke, admin wallet inspect/ledger/adjust |
| `Persistence/WalletDirectory.Payments.cs` | 148 | atomic order-payment debit + checkout quote |
| `Persistence/WalletDirectory.Refunds.cs` | 116 | refund credit (once per `ReturnRequestId`) |

The public type, its three interface implementations, the constructor signature, the DI registration
(`services.AddScoped<IWalletDirectory, WalletDirectory>()` and both port casts) and **every member
signature** are unchanged — the split is `partial`-based and type-identity preserving, so all 24
`Tooba.Wallet.Tests` behavior tests and the Host checkout/refund integration tests stay green
unchanged. No file is near the 800-LOC ceiling.

### 2.3 `PATH_NAMESPACE_ALIGNMENT` mismatch

| Project | Files | Before | After |
| --- | --- | --- | --- |
| `Tooba.Wallet.Application` | 11 request files | `Tooba.Wallet.Application.Commands.*` / `.Queries.*` | `Tooba.Wallet.Application.{Admin,Customer}.{Commands,Queries}` |
| `Tooba.Wallet.Application` | 4 model files (from `WalletDtos.cs`) | `Tooba.Wallet.Application.Models` | `…Application.{Admin,Customer,Payments,Refunds}.Models` |
| `Tooba.Wallet.Infrastructure` | `WalletDirectory.cs` | `Tooba.Wallet.Infrastructure.Directories` | `Tooba.Wallet.Infrastructure.Persistence` |
| `Tooba.Wallet.Infrastructure` | `20260827180000_InitialWallet.cs` | `Tooba.Wallet.Infrastructure.Migrations` | `Tooba.Wallet.Infrastructure.Persistence.Migrations` |

`PATH_NAMESPACE_ALIGNMENT` is now **EXACT** for all 53 production `.cs` files across the five
production projects, machine-verified by the W2 guard.

### 2.4 Retired single-file technical folders

| Retired folder | Reason | Destination |
| --- | --- | --- |
| `Infrastructure/Directories/` | one capability implementation in a technical-axis folder | `Infrastructure/Persistence/` |
| `Infrastructure/Development/` | exactly one file (`WalletDevelopmentSeedBootstrap.cs`) | `Infrastructure/Adapters/` (the module's existing adapter home, mirroring certified `Support`/`Media`) |
| `Infrastructure/Migrations/` | a competing top-level migrations home | `Infrastructure/Persistence/Migrations/` (the standard's only legal home) |
| `Endpoints/Errors/` + `Endpoints/Resources/` | split code/text home inside a composition project | `Endpoints/Contracts/Errors/` + `…/Resources/` |

`WalletModule.cs` deliberately **stays** in `Infrastructure/DependencyInjection/`: `ToobaModuleComposition`
and `ArchitectureBoundaryTests` consume its existing `Tooba.Wallet.Infrastructure.DependencyInjection`
namespace, so flattening it would be a Host-touching cosmetic change with no structural gain. The
`DependencyInjection/` folder is the module's established composition home and already holds the
`WalletOutboxRegistration`; `Infrastructure` therefore keeps an **empty** root allowlist.

### 2.5 Endpoints error/localization home

The Endpoints project is a composition project; its error catalog + resource set + bilingual `.resx`
moved from two top-level technical folders into one self-contained `Contracts/Errors[/Resources]`
surface with path-exact namespaces:

```text
Endpoints/Contracts/Errors/WalletErrorCatalogContributor.cs      Tooba.Wallet.Endpoints.Contracts.Errors
Endpoints/Contracts/Errors/Resources/WalletErrorResources.cs     Tooba.Wallet.Endpoints.Contracts.Errors.Resources
Endpoints/Contracts/Errors/Resources/WalletErrors.resx           (byte-identical)
Endpoints/Contracts/Errors/Resources/WalletErrors.fa.resx        (byte-identical)
```

Both locked `EmbeddedResource LogicalName` values were repointed to
`Tooba.Wallet.Endpoints.Contracts.Errors.Resources.WalletErrors[.fa].resources` so the assembly and
the resolved logical name stay consistent — the localized text is still resolved (proved by the W1
guard's live `WalletErrorResourceSet.GetString` assertions in `en` and `fa`). Endpoints root now holds
exactly the composition entry `WalletEndpointModule.cs`.

### 2.6 Root allowlists

Wallet was absent from `tmar-module-structure-manifests.json`. W2 adds a disk-accurate
`preCertModules[Wallet]` entry (see §6). Every project root is now empty except
`Tooba.Wallet.Endpoints` (`WalletEndpointModule.cs`), and `WalletModule.cs` is not at the
Infrastructure root (it is under `DependencyInjection/`), so the Infrastructure allowlist is empty and
matches disk.

---

## 3. Canonical folders kept deliberately

- `Application` keeps the shared `Composition/`, `Ports/`, `Models/`, `Validation/` seams at root and
  the capability folders for CQRS — the standard's `Application/<Capability>/{Commands,Queries,…}`
  shape, matching certified `Catalog`/`Order`/`Content`.
- `Domain` keeps `Aggregates/` + `ValueObjects/` (already canonical; untouched).
- `Contracts` keeps `Errors/`, `Dtos/`, `Payments/`, `Refunds/` (already canonical; untouched).
- `Endpoints` keeps the `Admin/` + `Customer/` audience folders and the single composition entry.
- `/Modules/Wallet/` keeps all six projects (`Contracts`, `Domain`, `Application`, `Infrastructure`,
  `Endpoints`, `Tests`) matching disk — no decorative or stale solution folder.

No folder named `Errors/` is created inside `Contracts` in this wave: `Wallet.Contracts` already owns
the stable-code identity in `Contracts/Errors/WalletErrorCodes.cs`, and the module's catalog +
localized text stay in the Endpoints composition project exactly as the certified `Support` module
(its closest in-shape sibling) does. Consolidating the text into `Wallet.Contracts` is a *code-home*
concern owned by a future Migrate/Complete wave, not a Structure defect; W2 records it as a residual
(§8) rather than silently re-homing it.

---

## 4. Behavior preservation

| Surface | State |
| --- | --- |
| 11 routes / verbs / group prefixes / templates | unchanged |
| Success response shapes (all 9 DTOs) | unchanged (record positional shape byte-identical) |
| Wire-visible HTTP error codes (10 catalogued) | unchanged |
| Domain invariants + typed-fault seam (`WalletOperation`) | unchanged |
| Authorization + actor resolution order | unchanged |
| DI lifetimes + registration sites | unchanged (1 `IErrorCatalogContributor`, 1 `IErrorResourceSet`) |
| `wallet` schema, migration, designer, snapshot | byte-identical |
| Outbox `Translate`/`ResolveEventClrType` → `null` | unchanged |
| Notification semantics (4 types + source ids + target route) | unchanged |

The only diffs inside `Tooba.Wallet.Infrastructure` are path moves, the `Persistence`/`Persistence.Migrations`
namespace repoints and the `partial` decomposition; no method body, EF mapping or model changed. The
`WalletDirectory` move touched the migration file only for its **namespace** — no table, column, index,
constraint, migration id or `Up`/`Down` semantics changed.

---

## 5. Durable guards added / updated

`src/backend/Host/Tooba.Host.Tests/Architecture/WalletModuleAmsc001W2StructureGuardTests.cs` (**new**, 7 facts)

- `Application_is_capability_first_and_has_no_single_file_request_leaf_folders`
- `Infrastructure_uses_Persistence_for_the_directory_and_Persistence_Migrations_for_the_migration`
  (now also locks the five capability partials and a < 300-LOC ceiling on each)
- `Endpoints_localization_lives_in_Contracts_Errors_Resources_with_locked_logical_names`
- `Path_namespace_alignment_is_exact_for_every_wallet_production_file`
- `Solution_grouping_is_the_canonical_modules_wallet_folder_with_six_projects`
- `Manifest_records_wallet_structure_authority_with_disk_accurate_allowlists`
- `Wallet_w2_sot_record_records_the_structure_handoff`

Guards repointed to the post-W2 physical truth (assertions relocated, **none weakened**):

| Guard | Repoint |
| --- | --- |
| `HostAdminAccessAmcCertGuardTests` | Wallet resx path → `Contracts/Errors/Resources`; error namespaces → `Contracts.Errors[.Resources]` |
| `HostAdminCanon003GuardTests` / `HostAdminCanon009GuardTests` | Wallet catalog path → `Contracts/Errors/WalletErrorCatalogContributor.cs` |
| `HostWalletAmcGuardTests` | seed bootstrap path → `Infrastructure/Adapters/` |
| `WalletModuleAmsc001W1MigrateGuardTests` | resource-set namespace + logical names → `Contracts.Errors.Resources` |
| `WalletArchitectureGuardTests` | allowed folder lists + `WalletDirectory` path → `Persistence/` |
| `ArchitectureBoundaryTests` / `WalletCheckoutRefundTests` | `Directories` using → `Persistence`; `Application.Models` → capability model namespaces |
| `Host/ToobaModuleComposition.cs`, `WalletDevelopmentSeedHost.cs`, `Host/Program.cs` | using/type-literal repoints only (no behavior) |

---

## 6. Manifest interaction

`docs/architecture/tmar-module-structure-manifests.json` → **new** `preCertModules[Wallet]` entry
(`structureCertified: false`, `lockVersion: ARCH-COMPLETE-002`, `structureAuthorityTask:
TB-TMAR-WALLET-AMSC-001-W2`, `structureState: READY_FOR_CERTIFY`), disk-accurate per project:

| Project | `rootAllowlist` | `forbiddenTopLevelFolders` |
| --- | --- | --- |
| `Tooba.Wallet.Application` | `[]` | `["Commands","Queries","Validators"]` |
| `Tooba.Wallet.Domain` | `[]` | `[]` |
| `Tooba.Wallet.Endpoints` | `["WalletEndpointModule.cs"]` | `["Errors","Resources"]` |
| `Tooba.Wallet.Infrastructure` | `[]` | `["Migrations","Directories","Development"]` |
| `Tooba.Wallet.Contracts` | `[]` | `[]` |

`structureCertified` was **not** flipped and `Wallet` was **not** added to
`structureLock.certifiedModules` — the Certify verdict is deliberately left to W3. `Wallet` stays in
`uncertifiedHttpOwningModules` (it is the last outstanding entry). `tmar-current-state.json` gains the
`walletAmsc001W2` structure-handoff record.

---

## 7. Focused validation

| Validation | Result |
| --- | --- |
| `dotnet build Tooba.slnx` | **succeeded, 0 errors** |
| `Tooba.Wallet.Tests` | **24 / 24 passed** |
| `WalletModuleAmsc001W1MigrateGuardTests` + `WalletModuleAmsc001W2StructureGuardTests` | **16 / 16 passed** |
| Wallet-adjacent Host set (`Wallet`, `HostAdminCanon003`, `HostAdminCanon009`, `HostAdminAccessAmcCertGuardTests`, `ErrorCatalogUniqueCodeGuardTests`, `HostCompositionAmcGuardTests`, `ArchitectureBoundaryTests`, `WalletCheckoutRefundTests`, `ReturnFoundationTests`) | **86 passed / 2 skipped / 0 failed** |
| `Tooba.Host.Tests` `~Architecture` full suite | **1364 passed / 42 failed** |
| Same filter at the `c802fe81` baseline (isolated `git worktree`) | **1364 passed / 42 failed** |
| **New failures introduced by W2** | **ZERO** (set-difference of the 42 failing test ids is empty in both directions) |

The 42 repository-global `~Architecture` failures are all present at the `c802fe81` baseline
(Host/Admin count drift from the Catalog `StoreAppearance`/`PW` evacuation waves, the Catalog
`Results.Json` WIP, the pre-existing `Tooba.Catalog.Contracts/Cart` namespace deviation in
`TmarCompleteReferenceStructureGateTests`, the UserPreference/Story/ProductWorkspace recovery pins, and
the repository-global `TmarDurableGuardTests` SoT pins). W2 fixed **three** Wallet-related failures that
W1's own error-home move had left red and that the W1 evidence had mis-attributed to the baseline
(`HostAdminCanon003GuardTests.Module_error_catalogs_own_the_unavailability_descriptors`,
`HostAdminCanon009GuardTests.Unavailable_descriptors_remain_platform_503_and_exactly_once`,
`HostWalletAmcGuardTests.Host_Wallet_folder_is_absent_and_module_owns_seed_bootstrap`) by repointing
their stale path assertions to the post-move truth. Net failing count is unchanged; no assertion was
deleted or relaxed.

---

## 8. Handoff

- `Structure-State = READY_FOR_CERTIFY` — every skill §27 gate met on disk and enforced by the new
  durable guard.
- Residual for W3 Certify: promote the manifest entry into the certified `modules` array
  (`structureCertified: true`), remove `Wallet` from `uncertifiedHttpOwningModules`, add `Wallet` to
  `structureLock.certifiedModules` (**32** total), add `WalletModuleAmsc001W3CertGuardTests`, and
  record the SoT certification block + Master Recovery checkpoint.
- Out-of-scope residual (recorded, not repaired here): the Endpoints-owned error catalog + bilingual
  text are still Endpoints-owned (canonical for the certified `Support` shape). Consolidating the
  module's code **and** text into one `Wallet.Contracts` boundary — as certified `Tax`/`Payment`/
  `Inventory`/`Media`/`Party` do — is a code-home (Migrate/Complete) concern for a later wave.
- Host final closure preserved: zero Host production file added or removed; three Host files were
  touched for **using/type-literal repoints only** (mechanical consequence of the module moves).
- Guards strengthened; zero guards weakened; zero unrelated files touched.
