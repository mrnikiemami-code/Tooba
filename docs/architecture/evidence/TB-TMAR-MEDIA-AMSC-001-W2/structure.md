# TB-TMAR-MEDIA-AMSC-001-W2 — Structure (tooba-architecture-structure)

- Task: `TB-TMAR-MEDIA-AMSC-001-W2`
- Parent: `TB-TMAR-MEDIA-AMSC-001-W1` (commit `0b0fde0a`)
- Skill: `tooba-architecture-structure`
- Target: `src/backend/Modules/Media/Tooba.Media.*`
- Lock: `ARCH-COMPLETE-002`
- Starting HEAD: `0b0fde0a` (`HEAD == origin/main` verified before the wave)

---

## 1. Physical tree before / after

**Unchanged.** No production file was moved, renamed, created or deleted in this wave. W1 already
performed the only physical work this module needed (the `MediaDirectory` cohesion split and the
`MediaAssetContracts.cs` → `IMediaAssetUploadPort.cs` rename), and the resulting tree satisfies every
structural gate below.

### physical-tree-after (current, authoritative)

```text
Tooba.Media.Contracts/
|-- Assets/
|   |-- IMediaAssetDemoPort.cs
|   `-- IMediaAssetUploadPort.cs
|-- Errors/
|   |-- MediaErrorCatalogContributor.cs
|   |-- MediaErrorCodes.cs
|   `-- MediaErrorResourceSet.cs
|-- Ports/
|   |-- IMediaAssetReadinessPort.cs
|   `-- MediaAssetContractCodes.cs
|-- Resources/
|   |-- MediaErrors.fa.resx
|   `-- MediaErrors.resx
`-- Tooba.Media.Contracts.csproj

Tooba.Media.Domain/
|-- Aggregates/
|   `-- MediaAsset.cs
|-- Enums/
|   `-- MediaAssetStatus.cs
`-- Tooba.Media.Domain.csproj

Tooba.Media.Application/
|-- Assets/
|   |-- Commands/   UploadMediaAssetCommand.cs
|   |-- Models/     MediaUploadBatchResponse.cs
|   |-- Queries/    GetMediaAssetQuery.cs  GetMediaStorageKeyQuery.cs  QueryMediaAssetsQuery.cs
|   `-- Validators/ GetMediaAssetQueryValidator.cs  MediaValidationCodes.cs
|                   QueryMediaAssetsQueryValidator.cs  UploadMediaAssetCommandValidator.cs
|-- Composition/    MediaOperation.cs
|-- Models/         MediaAssetInfo.cs
|-- Ports/          IMediaDirectory.cs  IMediaObjectStore.cs
`-- Tooba.Media.Application.csproj

Tooba.Media.Infrastructure/
|-- Adapters/     MediaAssetReadinessBridge.cs
|-- Assets/       MediaAssetDemoBridge.cs  MediaAssetUploadBridge.cs
|                 MediaDirectory.cs  MediaDirectory.Queries.cs  MediaDirectory.Upload.cs
|-- Persistence/  MediaDbContext.cs
|   `-- Migrations/ 20260830060000_InitialMedia.cs
|                    20260916053000_AddMediaFocalPoint.cs
|                    MediaDbContextModelSnapshot.cs
|-- Storage/      LocalFileMediaStore.cs
|-- MediaModule.cs
`-- Tooba.Media.Infrastructure.csproj

Tooba.Media.Endpoints/
|-- Admin/       MediaAdminEndpoints.cs  MediaAssetServing.cs
|-- Storefront/  MediaStorefrontEndpoints.cs
|-- MediaEndpointModule.cs
`-- Tooba.Media.Endpoints.csproj
```

Untracked, git-ignored `artifacts/` scratch folders exist under `Application/`, `Domain/` and
`Infrastructure/` exactly as in ~70 other projects (including certified Localization/Inventory/Cart/
Content/Offer/Order/Payment/AccessControl). `git ls-files | grep '/artifacts/'` → **0 tracked files**.
They cannot affect `rootAllowlist` (which enumerates `*.cs`) or the Solution Explorer (which
enumerates projects). **Not structure debt; no change made.**

## 2. Classification states

| State | Value |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` |
| Structure-State | `READY_FOR_CERTIFY` |

## 3. Capability map

One real business capability: **Assets** (the media asset library: binary upload, library
query/paging, binary serving).

| Layer | Capability placement |
| --- | --- |
| Application | `Assets/{Commands,Queries,Validators,Models}` — capability first, technical axes secondary |
| Endpoints | `Admin/` (upload/query/get + binary serving) and `Storefront/` (public binary route) audience folders |
| Infrastructure | `Assets/` directory + bridges, `Storage/` object store, `Adapters/` readiness bridge, `Persistence/` |
| Contracts | `Assets/` cross-module ports, `Ports/` readiness port + contract codes, `Errors/` + `Resources/` error boundary |
| Domain | `Aggregates/MediaAsset`, `Enums/MediaAssetStatus` |

Genuinely cross-capability Application concerns live in shared `Composition/`, `Models/`, `Ports/` —
the same shape Localization, CustomerProfile, BulkInquiry, Cart and Inventory use. No capability was
invented mechanically.

## 4. Folder-granularity audit

| Check | Result |
| --- | --- |
| Technical-axis-first request tree (`Application/Commands/<UseCase>`, …) | **NONE** |
| Single-file request/use-case leaf folder | **NONE** |
| Root `.cs` dump | **NONE** |
| Empty ceremonial folder | **NONE** |
| Over-nesting beyond capability → technical axis → files | **NONE** |

`Application/Assets/Commands/` holds one file (`UploadMediaAssetCommand.cs`), but the folder is named
after the **technical axis** and scoped by the `Assets` capability, not after a use case. It is the
canonical `<Capability>/Commands` shape used by every certified module (`Content/Articles/Commands`,
`Cart/Carts/Commands`, …), so it is **not** an over-foldered single-file request leaf. The guard's
leaf rule is name-scoped (`*Command`/`*Query` folder names) precisely so it does not misfire here.

`MediaDirectory.cs` / `MediaDirectory.Upload.cs` / `MediaDirectory.Queries.cs` are **partial-class
cohesion files** in one flat capability folder — not folders. No per-partial foldering was introduced.

## 5. Root allowlist verification (manifest ↔ disk)

| Project | Manifest `rootAllowlist` | Disk root `*.cs` | Match |
| --- | --- | --- | --- |
| `Tooba.Media.Contracts` | `[]` | none | ✓ |
| `Tooba.Media.Domain` | `[]` | none | ✓ |
| `Tooba.Media.Application` | `[]` | none | ✓ |
| `Tooba.Media.Endpoints` | `["MediaEndpointModule.cs"]` | `MediaEndpointModule.cs` | ✓ |
| `Tooba.Media.Infrastructure` | `["MediaModule.cs"]` | `MediaModule.cs` | ✓ |

Forbidden roots from the existing manifest entry are all absent:

- `Application`: `MediaContracts.cs` absent; no top-level `Commands` / `Queries`.
- `Domain`: `MediaAsset.cs` absent at root.
- `Endpoints`: `MediaAdminEndpoints.cs`, `MediaAssetServing.cs`, `MediaStorefrontEndpoints.cs` all
  absent at root.
- `Infrastructure`: `MediaDirectory.cs`, `LocalFileMediaStore.cs`, `MediaDbContext.cs` all absent at
  root; no top-level `Migrations` (migrations live under `Persistence/Migrations/`).
- `Contracts`: `MediaErrorCodes.cs` absent at root; the W1-retired `MediaAssetContracts.cs` remains
  absent (its replacement `Assets/IMediaAssetUploadPort.cs` is correctly foldered).

## 6. Path ↔ namespace exactness

Verified for all **38** production `.cs` files across the five projects by deriving the expected
namespace from the physical path and comparing against the declared namespace.

**Result: 0 mismatches.** No namespace alias workaround, no `TypeForwardedTo`, no `global using
Tooba.Media.*` shim, no duplicate physical copy, no stale root copy.

Verifier: `docs/architecture/evidence/TB-TMAR-MEDIA-AMSC-001-W2/check-ns.js` (handles the BOM-prefixed
EF migration/designer/snapshot files and the block-scoped `namespace … {` form).

## 7. Solution Explorer

`src/backend/Tooba.slnx` lines 35–41 contain:

```xml
<Folder Name="/Modules/Media/">
  <Project Path="Modules/Media/Tooba.Media.Domain/Tooba.Media.Domain.csproj" />
  <Project Path="Modules/Media/Tooba.Media.Contracts/Tooba.Media.Contracts.csproj" />
  <Project Path="Modules/Media/Tooba.Media.Application/Tooba.Media.Application.csproj" />
  <Project Path="Modules/Media/Tooba.Media.Infrastructure/Tooba.Media.Infrastructure.csproj" />
  <Project Path="Modules/Media/Tooba.Media.Endpoints/Tooba.Media.Endpoints.csproj" />
</Folder>
```

All five projects grouped under `/Modules/Media/`; Endpoints present; every entry resolves to a real
on-disk `.csproj` (asserted in the durable guard); no decorative or stale entry.
`Solution-Explorer-State = CANONICAL`.

## 8. Stale / duplicate physical copy

| Check | Result |
| --- | --- |
| Leftover path after the W1 `MediaAssetContracts.cs` rename | **NONE** — only `Assets/IMediaAssetUploadPort.cs` exists |
| Dual live home for the `MediaDirectory` responsibility | **NONE** — one seam + exactly two partials, 3 declarations total |
| `MediaDbContext` implemented outside `Persistence/` | **NONE** |
| `LocalFileMediaStore` implemented outside `Storage/` | **NONE** |
| Solution entry pointing at a deleted path | **NONE** |

## 9. Cohesion balance

| File | LOC | Assessment |
| --- | --- | --- |
| `Persistence/Migrations/MediaDbContextModelSnapshot.cs` | 153 | EF-generated |
| `Endpoints/Admin/MediaAdminEndpoints.cs` | 135 | cohesive admin transport |
| `Domain/Aggregates/MediaAsset.cs` | 115 | cohesive aggregate + invariants |
| `Storage/LocalFileMediaStore.cs` | 90 | cohesive adapter |
| `Assets/MediaDirectory.cs` | 85 | shared seam (ctor, allow-list, `Map`) |
| `Assets/MediaDirectory.Upload.cs` | 73 | upload pipeline only |
| `Assets/MediaDirectory.Queries.cs` | 71 | library read queries only |
| everything else | ≤ 73 | cohesive |

Largest production file 153 LOC (generated) / 135 LOC (hand-written), far below the 800 LOC
`ARCH-SIZE-001` ceiling. No Media entry in `Baselines/tmar-source-size-baseline.json`. No god-file,
no cosmetic over-split. `COHESIVE`.

## 10. Manifest interaction

The existing `modules[]` entry for `Media` already declares `structureCertified: true`,
`lockVersion: ARCH-COMPLETE-002`, and accurate `rootAllowlist` / `forbiddenRootFiles` /
`forbiddenTopLevelFolders` for all five projects. It was **re-verified against disk** in §5 and §6
and matches exactly. **No manifest edit was required or made in this wave** — the wave's job is
structural truth, and the structure is already true. Promotion/annotation remains Certify's mandate.

No `preCertModules` entry was created: Media is already a `modules[]` member, so a duplicate pre-cert
entry would create the exact duplicate-entry defect the standard forbids.

## 11. Durable structure guard

`src/backend/Host/Tooba.Host.Tests/Architecture/MediaModuleAmsc001W2StructureGuardTests.cs` — 10
facts locking:

1. capability-first Application (no top-level `Commands`/`Queries`/`Validators`; `Assets/*` present);
2. no single-file request leaf folder;
3. manifest root allowlists == disk root `*.cs` and forbidden roots absent;
4. Contracts boundary-only semantics (no `IRequest<`, no `IRequestHandler`, no `DbContext`, no
   Domain/Application/Infrastructure reference);
5. Endpoints reference neither Domain nor Infrastructure (project and source level);
6. exact path ↔ namespace across all five projects;
7. `/Modules/Media/` solution grouping, all five projects, every entry resolving on disk;
8. migrations under `Persistence/Migrations/` and `MediaModule.cs` the only Infrastructure root file;
9. no stale/duplicate physical copy of `MediaDirectory`, `MediaDbContext` or `LocalFileMediaStore`;
10. no `TypeForwardedTo` / `global using Tooba.Media` alias workaround.

Shared cross-capability Application buckets (`Composition/`, `Models/`, `Ports/`) are explicitly
allowed, matching the certified Localization/Inventory/CustomerProfile/BulkInquiry/AccessControl
precedent; only request trees are forbidden as a top-level Application axis.

## 12. Focused validation

| Run | Result |
| --- | --- |
| `dotnet test --filter FullyQualifiedName~MediaModuleAmsc001` | **23 passed / 0 failed** (13 W1 migrate facts + 10 W2 structure facts) |
| `dotnet test --filter FullyQualifiedName~Media` | **75 passed / 3 docker-skipped / 0 failed** |
| `dotnet test --filter FullyQualifiedName~Architecture` (whole namespace) | 937 passed / 43 failed with the W2 guard present; **43 failed / 927 passed with the W2 guard temporarily removed** → the 43 failures are pre-existing and unrelated to Media (Catalog Contracts, Host Admin/Grid/StoreMenu AMC lineages, ProductWorkspace shells, `HostAdminCanon001`) |
| `dotnet test --filter FullyQualifiedName~TmarDurableGuardTests` | 2 failed / 4 passed — **identical result with the W2 SoT block stashed**, i.e. pre-existing repo-global drift on `main` (`structureLock.certifiedModules` vs. the guard's frozen 16-module expectation, and the master-recovery checkpoint substring). Unrelated to Media and left untouched per "do not modify unrelated files". |
| `dotnet test --filter FullyQualifiedName~MediaModuleAmsc001|~TmarDurableGuardTests|~MasterRecovery` | 27 passed / 2 failed — the same two pre-existing `TmarDurableGuardTests` failures; **every Media guard passes** |

No broad solution-wide run, no open-ended repair loop.

## 13. Host final closure

Preserved. No Host production file was added, moved or changed in this wave.
`HOST_FINAL_CLOSURE_REGRESSION = NONE`. `Host/Tooba.Host/Media` remains absent; `namespace
Tooba.Host.Media` count remains ZERO.

## 14. Verdict

`Structure-State = READY_FOR_CERTIFY`
`Structure-Handoff-State = READY_FOR_CERTIFY`
