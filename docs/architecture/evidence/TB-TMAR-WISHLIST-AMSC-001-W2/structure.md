# TB-TMAR-WISHLIST-AMSC-001-W2 — structure (`tooba-architecture-structure`)

```text
TASK:           TB-TMAR-WISHLIST-AMSC-001-W2
MODE:           ARCHITECT_DIRECT_AMSC
SKILL:          tooba-architecture-structure
TARGET:         src/backend/Modules/Wishlist/Tooba.Wishlist.*
PARENT:         TB-TMAR-WISHLIST-AMSC-001-W1  (commit d7fa18f7)
STARTING HEAD:  d7fa18f7
STATE:          READY_FOR_CERTIFY
LOCK VERSION:   ARCH-COMPLETE-002
```

W2 is a **behavior-preserving physical reorganization** of `src/backend/Modules/Wishlist`. Routes,
success payloads, wire-visible outcome codes, the declared stable-code set, the descriptor set, the
resource keys/text, domain invariants, DI lifetimes, the `wishlist` schema and the migration set are
unchanged. Every edit is a path move, a namespace/using repoint, one cohesive file split, or a durable
guard/SoT/manifest update.

---

## 1. Classification states

| Axis | Before (W1 head `d7fa18f7`) | After |
| --- | --- | --- |
| Module applicability | `HTTP_OWNING` (4 routes / 1 group) | `HTTP_OWNING` (unchanged) |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` (capability-first already) | `PROFESSIONAL_SHALLOW` (unchanged) |
| Solution-Explorer-State | `CANONICAL` (`/Modules/Wishlist/`, 5 projects) | `CANONICAL` (unchanged, re-verified) |
| Path-Namespace-State | `EXACT` | `EXACT` (re-verified after the 3 moves) |
| Physical-Copy-State | `CLEAN` | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` | `ENFORCED` (manifest tightened) |
| File-Cohesion-State | `COHESIVE` | `COHESIVE` (one merged validator bundle split) |
| Structure-State | `READY_FOR_CERTIFY` (W0 handoff: physical tree already canonical) | `READY_FOR_CERTIFY` |

W0 recorded `structureHandoffState: NOT_REQUIRED_PHYSICAL_TREE_ALREADY_CANONICAL` and reserved W2 for
"the manifest structure-authority record and the durable W2 structure guard". W2 honours that scope and
adds the residual technical-axis moves that W0 left as non-blocking observations (one implementation
file in `Directories/`, one file in `Development/`, one merged validator bundle).

---

## 2. Defects repaired

### 2.1 `Infrastructure/Directories/` — one implementation file in a technical-axis folder

```text
BEFORE  Tooba.Wishlist.Infrastructure/Directories/WishlistDirectory.cs   (90 LOC, namespace …Infrastructure.Directories)
AFTER   Tooba.Wishlist.Infrastructure/Persistence/WishlistDirectory.cs   (90 LOC, namespace …Infrastructure.Persistence)
```

`WishlistDirectory` implements `IWishlistDirectory` + `IWishlistCountPort` over `WishlistDbContext`.
It belongs next to its own `DbContext`, exactly as the certified `Wallet` W2 did
(`Infrastructure/Persistence/WalletDirectory.cs`) and as `Support`/`AddressBook` keep their directory
adapter under `Persistence/`. The retired `Directories/` folder is now a forbidden top-level folder in
the manifest.

No member, signature, ctor or DI lifetime changed — `WishlistModule` still registers
`services.AddScoped<IWishlistDirectory, WishlistDirectory>()` and
`services.AddScoped<IWishlistCountPort>(sp => sp.GetRequiredService<WishlistDirectory>())`.

### 2.2 `Infrastructure/Development/` — one file in a single-file ceremony folder

```text
BEFORE  Tooba.Wishlist.Infrastructure/Development/WishlistDevelopmentSeed.cs   (namespace …Infrastructure.Development)
AFTER   Tooba.Wishlist.Infrastructure/Adapters/WishlistDevelopmentSeed.cs      (namespace …Infrastructure.Adapters)
```

The module-owned Development seed adapter is an **adapter**, not a lifecycle phase; `Adapters/` is the
module's adapter home and mirrors the certified `Support`/`Wallet`/`Media` shape. The single Host
consumer (`Host/Development/DevelopmentSchemaMigrator.cs`) was repointed for its **using only** —
`WishlistDevelopmentSeed.ApplyAsync` and its two call sites are untouched. The retired `Development/`
folder is now forbidden.

### 2.3 `Application/Customer/Validators/` — merged three-validator bundle

```text
BEFORE  Tooba.Wishlist.Application/Customer/Validators/WishlistValidators.cs   (38 LOC, 3 validator types)
AFTER   Tooba.Wishlist.Application/Validation/AddWishlistItemCommandValidator.cs
        Tooba.Wishlist.Application/Validation/RemoveWishlistItemCommandValidator.cs
        Tooba.Wishlist.Application/Validation/GetWishlistMembershipQueryValidator.cs
```

The three transport validators are a **module-wide transport axis**, not a Customer capability concern,
and the merged bundle hid three distinct request contracts in one file. W2 splits it into one cohesive
file per request under the shared `Application/Validation/` seam — the shape the certified
`Support` (`Application/Validation/SupportRequestValidators.cs`), `AddressBook`
(`Application/Validators/` + `Application/Validation/`) and `Payment` modules use. Each file declares
exactly one `AbstractValidator<T>` with the identical rule and the identical stable error code; the
`WishlistErrorCodes` / no-`WithMessage` / no-raw-literal assertions all still hold.

`ListWishlistPageQuery` stays validator-free — the W0/W1 `3 VALIDATOR_REQUIRED + 1
NO_VALIDATOR_REQUIRED` matrix is preserved, and the W1 non-circular `AddToobaCqrsFoundation`
discoverability proof still passes unchanged.

### 2.4 Deliberately **not** changed

- `WishlistDirectory` is **not** split into capability partials. At 90 LOC it is a single cohesive
  responsibility (one aggregate + two narrow ports). Splitting it would be a cosmetic split to game
  size guards (skill §12) — the opposite defect from Wallet's 682-LOC four-capability god-file.
- `Application/Customer/{Commands,Queries}/` are kept: they are the capability-first primary axis with
  the technical axis secondary, i.e. the prescribed shape, and no folder holds a single-file
  request leaf.
- `Contracts/{Errors,Ports,Resources}` and `Domain/Aggregates` are already canonical; untouched.
- `Endpoints/` keeps the composition root at root (`WishlistEndpointModule.cs`) plus the `Customer/`
  audience folder; it has no `Errors`/`Resources` (Wishlist's error surface is Contracts-owned), so no
  `Contracts/Errors/Resources` re-home is needed.

---

## 3. Physical tree — after

```text
src/backend/Modules/Wishlist/
  Tooba.Wishlist.Contracts/            root []           Errors/ Ports/ Resources/
  Tooba.Wishlist.Domain/               root []           Aggregates/
  Tooba.Wishlist.Application/          root []           Composition/ Customer/{Commands,Queries}/ Models/ Ports/ Validation/
  Tooba.Wishlist.Infrastructure/       root [WishlistModule.cs]  Adapters/ Persistence/{,Migrations}/
  Tooba.Wishlist.Endpoints/            root [WishlistEndpointModule.cs]  Customer/
```

| Project | Files | Root | Forbidden top-level folders (manifest) |
| --- | --- | --- | --- |
| `Tooba.Wishlist.Contracts` | 7 | — | — |
| `Tooba.Wishlist.Domain` | 1 | — | — |
| `Tooba.Wishlist.Application` | 10 | — | `Commands`, `Queries`, `Validators`, `Presentation` |
| `Tooba.Wishlist.Infrastructure` | 6 | `WishlistModule.cs` | `Migrations`, `Directories`, `Development` |
| `Tooba.Wishlist.Endpoints` | 3 | `WishlistEndpointModule.cs` | `Errors`, `Resources` |

`PATH_NAMESPACE_ALIGNMENT` is **EXACT** for all **27** production `.cs` files, machine-verified by the
W2 guard (0 mismatches).

---

## 4. Behavior preservation

| Surface | State |
| --- | --- |
| 4 routes / verbs / group prefix `/v1/customer/wishlist` / templates | unchanged |
| Success response shapes (`WishlistPage`, `WishlistAddResult`, `WishlistMembershipResult`, `201 Created`) | unchanged |
| Wire-visible outcome codes (3 client-observable + the Foundation session code) | unchanged |
| Declared stable-code set / descriptors / EN+FA resources + locked logical names | unchanged |
| Domain invariants + typed-fault seam (`WishlistOperation`) | unchanged |
| Authorization + actor resolution order | unchanged |
| DI lifetimes + registration sites (1 `IErrorCatalogContributor`, 1 `IErrorResourceSet`) | unchanged |
| `wishlist` schema, migration, designer, snapshot | byte-identical |
| Outbox `Translate`/`ResolveEventClrType` → `null` | unchanged |

The only diffs inside `Tooba.Wishlist.Infrastructure` are the two path moves and their namespace
repoints; no method body, EF mapping or model changed. The single Host edit is a `using` repoint.

---

## 5. Durable guards added / updated

`src/backend/Host/Tooba.Host.Tests/Architecture/WishlistModuleAmsc001W2StructureGuardTests.cs`
(**new**, 8 facts):

```text
1 Application_is_capability_first_and_has_no_single_file_request_leaf_folders
2 Transport_validators_are_one_cohesive_file_per_request_under_the_shared_seam
3 Infrastructure_uses_Persistence_for_the_directory_and_Adapters_for_the_seed
4 Path_namespace_alignment_is_exact_for_every_wishlist_production_file
5 Solution_grouping_is_the_canonical_modules_wishlist_folder_with_five_projects
6 Root_allowlists_are_enforced_and_match_disk
7 Manifest_records_wishlist_structure_authority_with_disk_accurate_allowlists
8 Wishlist_w2_sot_record_records_the_structure_handoff
```

Guards repointed to the post-W2 physical truth (assertions relocated, **none weakened**):

| Guard | Repoint |
| --- | --- |
| `HostWishlistAmcGuardTests` | seed path `Development` → `Adapters`; directory path `Directories` → `Persistence`; seed namespace `…Development` → `…Adapters` |
| `WishlistModuleAmcW2StructureGuardTests` | validator path `Customer/Validators/WishlistValidators.cs` → the three `Validation/*Validator.cs` files; directory path `Directories` → `Persistence` |
| `WishlistModuleAmcW3CqrsGuardTests` | validator bundle read → the three `Validation/*Validator.cs` files (same stable-code / no-prose assertions) |
| `WishlistModuleAmcW4CertGuardTests` | validator folder `Customer/Validators` → `Validation` (and asserted absent); `Adapters` present + `Directories`/`Development` absent; directory path → `Persistence` |
| `WishlistModuleAmsc001W1MigrateGuardTests` | the W1 validator fact now locates the three cohesive files under `Validation/` (count 3, no `WithMessage`, stable codes) — same semantics, stronger cohesion assertion |

No guard was deleted, relaxed or broadened. `WishlistModuleAmcW1SolutionGuardTests`,
`WishlistFoundationTests` and `HostDevelopmentMigrationSeamGuardTests` needed **no** change.

---

## 6. Manifest interaction

`docs/architecture/tmar-module-structure-manifests.json` → the existing `modules[Wishlist]` entry gains
the structure-authority keys and disk-accurate forbidden lists:

```text
structureAuthorityTask   = TB-TMAR-WISHLIST-AMSC-001-W2
structureAuthorityCommit = <this W2 commit>   (recorded by the W3 certify wave)
structureHandoffState    = READY_FOR_CERTIFY_CONSUMED_BY_W3
Tooba.Wishlist.Infrastructure.forbiddenTopLevelFolders = [Migrations, Directories, Development]
```

`structureCertified` was **not** flipped (Wishlist was already certified by the prior
`TB-TMAR-WISHLIST-AMC-001-W4` lineage) and Wishlist was **not** re-added to
`structureLock.certifiedModules` (already a single member of the 32-module set). The patch script
byte-compares every other module entry and every manifest-level key before writing, so the surgical
edit cannot silently rewrite unrelated modules.

---

## 7. Focused validation

| Validation | Result |
| --- | --- |
| `dotnet build Tooba.Host` | **succeeded, 0 errors** |
| `dotnet build Tooba.Host.Tests` | **succeeded, 0 errors** |
| All Host.Tests `Wishlist*` guards + `WishlistFoundationTests` + `HostDevelopmentMigrationSeamGuardTests` | **36 passed / 1 skipped / 0 failed** |
| `WishlistModuleAmsc001W2StructureGuardTests` | **8 / 8 passed** |
| `WishlistModuleAmsc001W1MigrateGuardTests` | **11 / 11 passed** (W1 semantics preserved, not weakened) |
| Host.Tests `~Architecture` full suite | 42 failing test ids |
| Same filter at the `d7fa18f7` baseline | 42 failing test ids (identical set) |
| **New failures introduced by W2** | **ZERO** |

The 42 repository-global `~Architecture` failures pre-exist this task and are unrelated to Wishlist:
the Host/Admin count-drift guards from the Catalog `StoreAppearance`/PW evacuation waves, the
Catalog `Results.Json` WIP and validator-classification pins, the ProductWorkspace W3-R2
commit-kind pin, the Story W3-R1 certified-module-count pin (`29` vs the current `32`), and the
repository-global `TmarCompleteReferenceStructureGateTests` failure whose message names the
pre-existing `Tooba.Catalog.Contracts/Cart` project-level namespace deviation — **Wishlist does not
appear in any failure message** and its own project set is proven exact by the W2 guard.

---

## 8. Handoff

- `Structure-State = READY_FOR_CERTIFY` — every skill §27 gate met on disk and enforced by the new
  durable guard.
- Residual for W3 Certify: record `structureAuthorityCommit` in the manifest entry, re-derive the
  certification truth from disk, add `WishlistModuleAmsc001W3CertGuardTests`, and write the
  certification SoT block + Master Recovery checkpoint. `structureLock.certifiedModules` and
  `completeReferenceModules` stay untouched (Wishlist is already a member of both lineages).
- Host final closure preserved: zero Host production file added, moved or removed; one Host file was
  touched for a `using` repoint only.
- Guards strengthened; zero guards weakened; zero unrelated files touched.
