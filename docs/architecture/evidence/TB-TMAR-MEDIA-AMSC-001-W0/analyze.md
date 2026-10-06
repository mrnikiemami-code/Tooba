# TB-TMAR-MEDIA-AMSC-001-W0 — Analyze (tooba-architecture-analyze)

- Task: `TB-TMAR-MEDIA-AMSC-001-W0`
- Skill: `tooba-architecture-analyze` (ANALYSIS-ONLY; zero production change)
- Target: `src/backend/Modules/Media/Tooba.Media.*`
- Lock: `ARCH-COMPLETE-002`
- Starting HEAD: `70e55d40`
- Prior certification: `TB-TMAR-MEDIA-AMC-001` W1→W4 (+ `W4-R1` repair) — Media is already
  `structureCertified: true` / `ARCH-COMPLETE-002` in
  `tmar-module-structure-manifests.json` and is present exactly once in
  `structureLock.certifiedModules`. This AMSC pass is a **re-standardization** of that earlier AMC
  lineage, exactly as was done for Content / CustomerProfile / Identity / Inventory / Localization.

---

## 24. Target analyzed

Five production projects, 33 production `.cs` files (plus 2 `.resx`), ~1,290 physical LOC:

```text
Contracts/      Assets/(2) Errors/(3) Ports/(2) Resources/(2 .resx)
Domain/         Aggregates/MediaAsset.cs  Enums/MediaAssetStatus.cs
Application/    Assets/{Commands(1),Queries(3),Validators(3),Models(1)}
                Composition/MediaOperation.cs  Models/MediaAssetInfo.cs  Ports/(2)
Endpoints/      MediaEndpointModule.cs  Admin/(2)  Storefront/(1)
Infrastructure/ MediaModule.cs  Adapters/(1)  Assets/(4)
                Persistence/MediaDbContext.cs  Persistence/Migrations/(3)  Storage/(1)
```

No file is oversized: the largest production file is
`Infrastructure/Assets/MediaDirectory.cs` at **209 LOC**, far below the 800 LOC `ARCH-SIZE-001`
ceiling, and no Media file appears in `Baselines/tmar-source-size-baseline.json`.

## 25. Responsibility map

| Responsibility | Location | Classification |
| --- | --- | --- |
| Media asset aggregate + invariants (key/name/type/size/checksum bounds, focal-point normalization) | `Domain/Aggregates/MediaAsset.cs` | DOMAIN_RULE |
| Asset lifecycle status | `Domain/Enums/MediaAssetStatus.cs` | DOMAIN_RULE |
| Upload use case (MIME allow-list, size ceiling, SHA-256, storage key, orphan cleanup) | `Infrastructure/Assets/MediaDirectory.cs` | PERSISTENCE + INTEGRATION_ADAPTER (mixed) |
| Library paging/search/filter | `Infrastructure/Assets/MediaDirectory.cs` | PERSISTENCE |
| Binary object storage | `Infrastructure/Storage/LocalFileMediaStore.cs` | INTEGRATION_ADAPTER |
| Admin HTTP surface (upload/query/get) | `Endpoints/Admin/MediaAdminEndpoints.cs` | HTTP_ENDPOINT |
| Public binary serving + SVG fallback | `Endpoints/Admin/MediaAssetServing.cs` | HTTP_ENDPOINT (presentation) |
| Storefront binary route | `Endpoints/Storefront/MediaStorefrontEndpoints.cs` | HTTP_ENDPOINT |
| CQRS requests/handlers | `Application/Assets/**` | APPLICATION_USE_CASE |
| Transport-shape validators | `Application/Assets/Validators/*` | APPLICATION_USE_CASE |
| Module-owned ports | `Application/Ports/IMediaDirectory.cs`, `IMediaObjectStore.cs` | APPLICATION_USE_CASE |
| Foreign boundary ports | `Contracts/Ports/IMediaAssetReadinessPort.cs`, `Contracts/Assets/IMediaAssetDemoPort.cs`, `Contracts/Assets/MediaAssetContracts.cs` | CONTRACT |
| Stable error codes / catalog / resources | `Contracts/Errors/*`, `Contracts/Resources/*` | CONTRACT |
| Composition + DI + schema migrator | `Infrastructure/MediaModule.cs` | HOST_COMPOSITION_ROOT (module-owned) |
| EF mapping | `Infrastructure/Persistence/MediaDbContext.cs` | PERSISTENCE |
| Dev demo bridge for Catalog | `Infrastructure/Assets/MediaAssetDemoBridge.cs` | DEVELOPMENT_SEED (foreign-consumed) |

### 26. Ownership map

All responsibilities are owned by **Media**. No responsibility belongs to another module or to Host.
Host residue is exactly: `Program.cs` (module assembly registration line 186, DI authorizer not
needed, `app.MapMediaModuleEndpoints()` line 446), `Composition/ToobaModuleComposition.cs`
(`new MediaModule()`), `Development/MarketplaceDevelopmentBootstrap.cs` (dev `MediaDbContext`
migrate), and `Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs` (migration descriptor). All four
are `ALLOWED_COMPOSITION_ROOT` / dev-seam. `Host/Tooba.Host/Media` folder is **absent**;
`namespace Tooba.Host.Media` count is **ZERO**; `Tooba.Media.Endpoints` has **zero** Host dependency.

### 27. MUST_SPLIT decisions

**`MUST_SPLIT` (W1) — `Infrastructure/Assets/MediaDirectory.cs` is a two-responsibility file.**

It owns both (a) the upload pipeline (content-type normalization, allow-list, size ceiling,
SHA-256, storage-key derivation, `IMediaObjectStore.SaveAsync`, `MediaAsset.CreateReady`, metadata
persist, compensating delete) and (b) read-only library queries (`QueryAsync`/`GetAsync`/
`GetManyAsync`/`GetStorageKeyAsync`) plus shared private helpers (`Map`, `EscapeLike`,
`SanitizeOriginalFileName`, `NormalizeContentType`). Both responsibilities are Media-owned, so this
is a *cohesion* split inside Media, not an ownership migration. W1 will decompose it into cohesive
files under `Infrastructure/Assets/` (persistence seam + upload + queries) with no behavior change.

**`MUST_SPLIT` (W1) — `Contracts/Assets/MediaAssetContracts.cs` is a generic mixed contract file.**

It contains a single interface (`IMediaAssetUploadPort`) behind `#pragma warning disable CS1591`,
has a file name that does not match its only type, and carries no doc comments. Per the semantic
contract-ownership gate this must be renamed to match its type and documented.

### 28. Current illegal dependencies

**NONE.** No Media project references any foreign `*.Application` / `*.Infrastructure` / `*.Domain` /
`*.Endpoints` project. The only foreign project references are the generic platform seams:

| Project | Foreign references |
| --- | --- |
| `Contracts` | `Tooba.BuildingBlocks` |
| `Domain` | `Tooba.BuildingBlocks` |
| `Application` | `Tooba.BuildingBlocks` |
| `Endpoints` | `Tooba.BuildingBlocks` |
| `Infrastructure` | `Tooba.ModuleContracts`, `Tooba.Persistence` |

`foreignAppInfraDomainCoupling = ZERO`. The only legal *consumer* edge into Media is
`Payment.Application → Tooba.Media.Contracts` (Contracts-only) and
`Content.Infrastructure → Tooba.Media.Contracts` (Contracts-only),
`Catalog.Infrastructure → Tooba.Media.Contracts` (Contracts-only).

### 29. Cross-module join inventory

**NONE.** `MediaDbContext` owns only the `media` schema (`assets` + shared outbox table). No foreign
`DbSet`, no foreign `DbContext`, no EF navigation crossing ownership, no raw SQL joining another
module's tables, no cross-module transaction assumption. Conversely no foreign module reads
`media.*` by raw SQL (verified by repository-wide search for `media.assets` / `media.` schema SQL).

### 30. Contracts-only replacement map

No replacement is required — the boundary is already Contracts-only and narrow:

| Consumer | Contract used | Verdict |
| --- | --- | --- |
| Content (`ContentMediaAssetValidator`) | `IMediaAssetReadinessPort.EnsureReadyAsync` | legal, narrow |
| Payment (`StorefrontPaymentOrchestrator`) | `IMediaAssetUploadPort.UploadAsync` | legal, narrow |
| Catalog (`CatalogDemoMediaFactory`, `CatalogDemoResetService`) | `IMediaAssetDemoPort` | legal, narrow (dev demo) |
| Media stable codes for consumers | `MediaAssetContractCodes.AssetMissing` | legal |

### 31. CQRS / MediatR gaps

`CQRS-State = PARTIAL`.

- Four real `IRequest` types with real `IRequestHandler<,>` implementations exist and are dispatched
  through `ISender` from thin endpoints; MediatR is `12.5.0` via `AddToobaCqrsFoundation`.
- **Gap (W1):** `Endpoints/Admin/MediaAssetServing.cs` does **not** use `ISender` for the
  storage-key lookup — `TryServeStoredMediaAsync` injects `IMediaDirectory` and `IMediaObjectStore`
  directly (an endpoint → Application-port/Infrastructure-adapter call). The sibling `ServeAsync`
  already uses `ISender` for both steps. `TryServeStoredMediaAsync` is also **dead production code**
  (only a test calls it).
- **Gap (W1):** `MediaAdminEndpoints.UploadAsync` calls `ResolveTitle(errorCode)` →
  `MediaErrorResources.Manager.GetString(errorCode, CultureInfo.GetCultureInfo("fa"))`, i.e. a
  **hard-coded `fa` culture** for a per-item title, duplicating the canonical localizer that
  `ApiResponseFactory` already uses for every other failure.
- `MediaAdminEndpoints.QueryAsync` returns `api.From(...)` directly over a `Result`, which is
  canonical; the upload batch endpoint returns `api.From(Result.Success(...))` with the preserved
  `MediaUploadBatchResponse` envelope (HTTP 200 + raw JSON value) — intentional parity.

### 32. Validation classification matrix

| Request | `IRequest` | Handler | `ISender` | Classification | Validator | Codes |
| --- | --- | --- | --- | --- | --- | --- |
| `UploadMediaAssetCommand` | ✓ | ✓ | ✓ | `VALIDATOR_REQUIRED_PRESENT` | `UploadMediaAssetCommandValidator` | `media.validation.failed` |
| `QueryMediaAssetsQuery` | ✓ | ✓ | ✓ | `VALIDATOR_REQUIRED_PRESENT` | `QueryMediaAssetsQueryValidator` | `media.validation.failed` |
| `GetMediaAssetQuery` | ✓ | ✓ | ✓ | `VALIDATOR_REQUIRED_PRESENT` | `GetMediaAssetQueryValidator` | `media.validation.failed` |
| `GetMediaStorageKeyQuery` | ✓ | ✓ | ✓ (via `ServeAsync`) | `NO_VALIDATOR_REQUIRED` | — | — |

`endpointReachableRequests = 4`, `validatorRequiredCount = 3`, `noValidatorRequiredCount = 1`,
gap ZERO. `GetMediaStorageKeyQuery` is `NO_VALIDATOR_REQUIRED` because it carries only a route-derived
`Guid` already validated by the paired `GetMediaAssetQuery` in the same request path — matching the
existing certified classification.

**Finding (W1):** all three validators emit the single coarse code `media.validation.failed`. Per
`ARCH-COMPLETE-002` transport-identity separation the canonical pattern is distinct
`media.validation.*` transport codes mapped through the foundation `validation.failed` descriptor,
as already done for Localization/Inventory. Behaviour-preserving change (HTTP 400 unchanged,
envelope unchanged); recorded as an explicit accepted observable delta.

### 33. Localization findings

`Localization-State = HARDCODED_TEXT`.

| Finding | Location | Severity |
| --- | --- | --- |
| Hard-coded Persian fault text passed as the `title` of every thrown platform fault | `Infrastructure/Assets/MediaDirectory.cs` (6 sites), `Infrastructure/Storage/LocalFileMediaStore.cs` (5 sites) | medium — `ApiResponseFactory`/`SafeErrorMapper` resolve the client title from the catalog, so this text is never the user-facing contract, but it is the wrong mechanism |
| Hard-coded `fa` culture for the upload batch per-item title, bypassing `IErrorMessageLocalizer` | `Endpoints/Admin/MediaAdminEndpoints.cs` `ResolveTitle` | **high** |
| Hard-coded Persian `aria-label="نمایش موقت رسانه"` inside the fallback SVG | `Endpoints/Admin/MediaAssetServing.cs` `PlaceholderSvg` | low — `aria-label` inside an image payload is not a localized API message; retained as presentation asset text (documented, not migrated) |
| Hard-coded Persian XML-doc comments | all projects | none — documentation, not user-facing |
| `ex.Message` used as a contract | **ZERO** | — |
| Duplicate localized messages | **NONE** — `MediaErrors.resx` (7 keys) + `MediaErrors.fa.resx` (7 keys) cover all 6 declared codes + `media.asset.missing` | — |
| Second localization system | **NONE** | — |

Declared codes: `MediaErrorCodes` (6) + `MediaAssetContractCodes.AssetMissing` (1) = **7 declared**,
**7 registered descriptors** in `MediaErrorCatalogContributor`, **7 keys in both cultures**.
`MediaErrorResourceSet.Owns()` claims the whole `media.` prefix — correct here, because every
`media.*` string in the repository is Media-owned (no foreign collision, unlike Localization's
`localization.` prefix).

### 34. API result / error mapping findings

`API-Result-Pattern-State = CANONICAL` for JSON endpoints; two local deviations to repair in W1.

- `Results.Json` / `Results.BadRequest` / `Results.Problem` in Media: **ZERO**.
- `Results.File` (binary serving) and `Results.Text` (SVG fallback) are the correct minimal-API
  primitives for non-JSON payloads; `MediaAssetServing` owns the **only** `Results.File`/`Results.Text`
  in the module and must keep them.
- Local `ProblemDetails` builder / local error mapper: **ZERO**.
- Failure classification by parsing `ex.Message`: **ZERO**.
- **Deviation (W1):** `Endpoints/Admin/MediaAssetServing.cs` returns `PlaceholderSvg(id)` (HTTP 200)
  for a missing asset instead of a canonical 404 failure. This is **locked observable behavior**
  ("SVG نمایشی برای Guidهای legacy بدون دارایی واقعی") — `HostMediaEvacuationGuardTests` pins
  `image/svg+xml` + `PlaceholderSvg`. W1 preserves it and documents it as an accepted, explicit
  behavior (legacy-Guid rendering contract), **not** a defect.
- **Deviation (W1):** `ResolveTitle` reads the `.resx` directly with a hard-coded culture. W1
  replaces it with the canonical `IErrorMessageLocalizer` seam.
- Unknown/unexpected exceptions are not converted to business failures: `MediaOperation` maps only
  `PlatformHttpException` **with a non-empty `ErrorCode`**, so a foreign or codeless fault propagates
  to the global presentation boundary. Correct, but the type is the **legacy Host-platform HTTP
  seam** rather than the canonical typed fault used by every other certified module.
- No duplicate-suppression mechanism. No new shared-errors project.
- `media.validation.failed` is a Media-owned **business-classification** descriptor that duplicates
  the foundation `validation.failed` semantic; W1 narrows it to distinct transport-identity codes.
- `media.asset.missing` is registered by `MediaErrorCatalogContributor` (Media owns it) and also
  declared in `MediaAssetContractCodes` — single owner, no duplicate descriptor. Correct.

### 35. Logging / sensitive-data findings

`Logging-State = CANONICAL`. `Sensitive-Logging-State = NONE`.

`ILogger<T>` with structured, value-free event names only (`"media.upload.failed"`,
`"media.upload.succeeded"`). No `Console.WriteLine`, no `Debug.WriteLine`, no custom logger
framework, no second telemetry pipeline. No passwords, tokens, cookies, `Authorization` headers or
credentials are logged. `MediaAdminEndpoints` logs nothing at all (authorization failures surface
through the canonical `IAdminPanelAccess` seam).

### 36. OpenTelemetry / correlation findings

`OpenTelemetry-State = CANONICAL`, `Correlation-Trace-State = CANONICAL`.

No second `ActivitySource`, no `Meter`, no custom correlation provider, no custom header, no raw
`AsyncLocal`, no `StartActivity(...)`, no manual `traceparent` parsing anywhere in Media. Media
issues **no** cross-module call of its own, so no `IModuleCallTracer` decoration is required; foreign
consumers decorate their own calls to the Media Contracts ports. `traceId`/`correlationId` reach
Media responses through the canonical `ProblemDetailsContextProvider`.

### 37. File cohesion / splitting plan

| File | LOC | Verdict |
| --- | --- | --- |
| `Infrastructure/Assets/MediaDirectory.cs` | 209 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` → split (upload vs queries vs shared seam) |
| `Contracts/Assets/MediaAssetContracts.cs` | 6 | generic mixed name → rename to `IMediaAssetUploadPort.cs`, add docs |
| everything else | ≤ 153 | `COHESIVE` |

No god-file, no over-foldering, no single-file request leaf folder, no technical-axis-first tree.

### 38. Exact target paths / namespaces

W1 (behaviour-preserving cohesion + canonical mechanisms):

```text
Infrastructure/Assets/MediaDirectory.cs          (partial: shared seam, ctor, content-type allow-list, Map)
Infrastructure/Assets/MediaDirectory.Upload.cs   (partial: UploadAsync pipeline)
Infrastructure/Assets/MediaDirectory.Queries.cs  (partial: QueryAsync/GetAsync/GetManyAsync/GetStorageKeyAsync)
Contracts/Assets/IMediaAssetUploadPort.cs        (renamed from MediaAssetContracts.cs; same namespace/type)
Application/Assets/Validators/MediaValidationCodes.cs   (new: media.validation.* transport codes)
```

W2 (structure lock; only if W1 leaves drift):

```text
/Modules/Media/  solution folder (already present, 5 projects — verified)
```

### 39. Behaviour-preservation checklist

Routes: `POST /v1/admin/media/upload` (`DisableAntiforgery`), `GET /v1/admin/media/`,
`GET /v1/admin/media/{id:guid}`, `GET /v1/media/{id:guid}`,
`GET /v1/storefront/media/{assetId:guid}` — unchanged. Methods, status codes, response shapes
(including the `MediaUploadBatchResponse` `{ items: [ { ok, asset | fileName, title, errorCode } ] }`
envelope and the HTTP 200 SVG fallback for legacy Guids) — unchanged. Stable codes
`media.upload.failed`, `media.type.unsupported`, `media.too_large`, `media.storage.unavailable`,
`media.missing`, `media.asset.missing` — unchanged. Authorization (`IAdminPanelAccess.RequireAuthorizedAsync`
× 3) — unchanged. MIME allow-list (7 types), extension map, 50 MB default ceiling and
`Tooba:Media:MaxUploadBytes` override, SHA-256 checksum, `{yyyy}/{MM}/{assetId:N}{ext}` storage key
derivation, filename sanitization, compensating delete on persist failure — unchanged. Paging
(`page ≥ 1`, `pageSize` clamped 1..100), `ILike` search escaping, `CreatedAt desc, MediaAssetId asc`
ordering, Ready-only filtering — unchanged. `media` schema, both migrations
(`20260830060000_InitialMedia`, `20260916053000_AddMediaFocalPoint`), snapshot, table/column/index
semantics — unchanged (`migrationFilesChanged = 0`). Frontend `src/frontend/app/admin/media-api.ts`
contract (field names `ok`, `asset`, `fileName`, `title`, `errorCode`) — unchanged. Localization keys
and their semantics — unchanged.

### 40. Migration order

1. Split `MediaDirectory` into cohesive partials (no signature/behaviour change).
2. Rename `MediaAssetContracts.cs` → `IMediaAssetUploadPort.cs`.
3. Introduce `MediaValidationCodes` (`media.validation.*`) and repoint the three validators through
   the foundation `validation.failed` mapping.
4. Introduce the canonical typed-fault seam: replace `PlatformHttpException` in
   `MediaDirectory`/`LocalFileMediaStore` with `ContractOperationException`/`SemanticException`
   carrying the **existing** stable codes, and make `MediaOperation` map typed codes guarded by a
   declared-code catalog (`MediaErrorCodes.IsKnown`), keeping unknown codes propagating.
5. Remove the dead `IMediaDirectory`/`IMediaObjectStore` direct dependency from
   `MediaAssetServing.TryServeStoredMediaAsync` (or delete the dead helper if the guard permits)
   and route the storage-key lookup through `ISender`.
6. Replace `ResolveTitle`'s hard-coded `fa` `.resx` read with the canonical
   `IErrorMessageLocalizer`.
7. Keep the SVG fallback behavior and `Results.File`/`Results.Text` exactly as-is.

### 41. Verification plan

- Build `Tooba.Media.*` (5 projects) + `Tooba.Host.Tests`.
- Focused: `MediaModuleAmc*`, `HostMediaEvacuationGuardTests`, `MediaDamTests`,
  `AdminPanelCompositionTests`, `CatalogDemoResetSeedTests`, `ErrorCatalogUniqueCodeGuardTests`,
  plus the new `MediaModuleAmsc001W*` guards.
- Behaviour parity: `MediaDamTests` (Docker-gated) must still prove MIME reject, size reject, paging
  and binary serving; `Media_module_boundary_static_checks` must stay green.
- `git fetch` + `HEAD == origin/main` after each wave push.

### 42. Certification blockers

- None blocking. W1 must complete items 1–6 of the migration order before W2 can issue
  `READY_FOR_CERTIFY`; the only accepted, explicit, documented non-conformances retained are the
  legacy-Guid SVG fallback (HTTP 200) and the SVG `aria-label` presentation text.

---

## Structured state fields

| Field | Value |
| --- | --- |
| Foundation-State | `FOUNDATION_READY` (5 projects, certified, no parallel structure) |
| Ownership-State | `MUST_SPLIT` (cohesion split inside Media; no ownership migration) |
| File-Cohesion-State | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (`MediaDirectory.cs` 209 LOC, 2 responsibilities) |
| Oversized/God-File-State | none ≥ 800 LOC; largest 209 LOC |
| Localization-State | `HARDCODED_TEXT` |
| API-Result-Pattern-State | `CANONICAL` (2 documented local deviations) |
| Stable-Error-Code-State | `CATALOGUED` (7 declared = 7 registered = 7×2 localized) |
| Logging-State | `CANONICAL` |
| Sensitive-Logging-State | `NONE` |
| OpenTelemetry-State | `CANONICAL` |
| Correlation-Trace-State | `CANONICAL` |
| CQRS-State | `PARTIAL` |
| Validator-Coverage-State | `EXHAUSTIVE` (4 = 3 + 1, gap ZERO) |
| Contracts-Boundary-State | `CLEAN` |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` (zero foreign App/Infra/Domain) |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` (own `media` schema) |
| Endpoint-Ownership-State | `MODULE_OWNED` (5 routes; Host ZERO) |
| Host-Residue-State | `ALLOWED_COMPOSITION_ROOT` ×3 + `ALLOWED_DEV_MIGRATE_SEAM` ×1 |
| Schema-Migration-State | `UNCHANGED` (2 migrations, snapshot, no drift) |
| Behavior-Preservation-Risk | `LOW` |
| Canonical-Reference-Used | Localization/Inventory (typed fault seam + declared-code guard + transport validation codes), Offer/BuildingBlocks (Result/`ApiResponseFactory`/`IErrorCatalogContributor`/`IErrorResourceSet`/`IErrorMessageLocalizer`), CustomerProfile/AddressBook (capability-first shallow layout) |
| Structure-Handoff-State | `REQUIRED` (W2 must re-verify and issue `READY_FOR_CERTIFY`) |
| Final-Disposition | `READY_TO_MIGRATE` |

## W0 stop

`ANALYZE_COMPLETE` / `READY_TO_MIGRATE`. Zero production change in this wave.
Stop gate: `USER_REVIEW_MEDIA_AMSC_001_W0`. `automaticNextImplementationTask = NONE`.
