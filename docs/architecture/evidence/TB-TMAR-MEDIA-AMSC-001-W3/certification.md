# TB-TMAR-MEDIA-AMSC-001-W3 — Certification (tooba-architecture-certify)

- Task: `TB-TMAR-MEDIA-AMSC-001-W3`
- Parent: `TB-TMAR-MEDIA-AMSC-001-W2` (commit `991551e9`)
- Skill: `tooba-architecture-certify`
- Target: `src/backend/Modules/Media/Tooba.Media.*`
- Lock: `ARCH-COMPLETE-002`
- Starting HEAD: `991551e9` (`HEAD == origin/main` verified before the wave)
- Structure gate consumed: `TB-TMAR-MEDIA-AMSC-001-W2` → `Structure-State = READY_FOR_CERTIFY`,
  `Folder-Granularity-State = PROFESSIONAL_SHALLOW`, `Solution-Explorer-State = CANONICAL`,
  `Path-Namespace-State = EXACT`, `Physical-Copy-State = CLEAN`, `Root-Allowlist-State = ENFORCED`,
  zero single-file request leaves, zero technical-axis-first request tree, Host final closure preserved.
  The gate is current for this exact surface and is not contradicted by disk.

**Final verdict: `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` `STRUCTURE_CERTIFIED`.**

---

## 1. Final physical tree

```text
Tooba.Media.Contracts/
|-- Assets/   IMediaAssetDemoPort.cs  IMediaAssetUploadPort.cs
|-- Errors/   MediaErrorCatalogContributor.cs  MediaErrorCodes.cs  MediaErrorResourceSet.cs
|-- Ports/    IMediaAssetReadinessPort.cs  MediaAssetContractCodes.cs
|-- Resources/ MediaErrors.fa.resx  MediaErrors.resx
`-- Tooba.Media.Contracts.csproj

Tooba.Media.Domain/
|-- Aggregates/ MediaAsset.cs
|-- Enums/      MediaAssetStatus.cs
`-- Tooba.Media.Domain.csproj

Tooba.Media.Application/
|-- Assets/Commands/   UploadMediaAssetCommand.cs
|-- Assets/Models/     MediaUploadBatchResponse.cs
|-- Assets/Queries/    GetMediaAssetQuery.cs  GetMediaStorageKeyQuery.cs  QueryMediaAssetsQuery.cs
|-- Assets/Validators/ GetMediaAssetQueryValidator.cs  MediaValidationCodes.cs
|                      QueryMediaAssetsQueryValidator.cs  UploadMediaAssetCommandValidator.cs
|-- Composition/       MediaOperation.cs
|-- Models/            MediaAssetInfo.cs
|-- Ports/             IMediaDirectory.cs  IMediaObjectStore.cs
`-- Tooba.Media.Application.csproj

Tooba.Media.Infrastructure/
|-- Adapters/    MediaAssetReadinessBridge.cs
|-- Assets/      MediaAssetDemoBridge.cs  MediaAssetUploadBridge.cs
|                MediaDirectory.cs  MediaDirectory.Queries.cs  MediaDirectory.Upload.cs
|-- Persistence/ MediaDbContext.cs
|   `-- Migrations/ 20260830060000_InitialMedia.cs  20260916053000_AddMediaFocalPoint.cs
|                   MediaDbContextModelSnapshot.cs
|-- Storage/     LocalFileMediaStore.cs
|-- MediaModule.cs
`-- Tooba.Media.Infrastructure.csproj

Tooba.Media.Endpoints/
|-- Admin/       MediaAdminEndpoints.cs  MediaAssetServing.cs
|-- Storefront/  MediaStorefrontEndpoints.cs
|-- MediaEndpointModule.cs
`-- Tooba.Media.Endpoints.csproj
```

38 production `.cs` files + 2 `.resx`. Untracked, git-ignored `artifacts/` scratch folders exist under
`Application/`, `Domain/` and `Infrastructure/` exactly as in ~70 other projects including certified
Localization/Inventory/Cart/Content/Offer/Order/Payment/AccessControl
(`git ls-files | grep '/artifacts/'` → 0 tracked files). Not structure debt.

## 2. Root allowlists

| Project | Manifest `rootAllowlist` | Disk root `*.cs` | Match |
| --- | --- | --- | --- |
| `Tooba.Media.Contracts` | `[]` | none | ✓ |
| `Tooba.Media.Domain` | `[]` | none | ✓ |
| `Tooba.Media.Application` | `[]` | none | ✓ |
| `Tooba.Media.Endpoints` | `["MediaEndpointModule.cs"]` | `MediaEndpointModule.cs` | ✓ |
| `Tooba.Media.Infrastructure` | `["MediaModule.cs"]` | `MediaModule.cs` | ✓ |

All `forbiddenRootFiles` / `forbiddenTopLevelFolders` from the existing manifest entry are absent,
including the W1-retired `Contracts/Assets/MediaAssetContracts.cs` and the forbidden top-level
`Migrations` folder. `Root-Allowlist-State = ENFORCED`.

## 3. Path ↔ namespace proof

All **38** production `.cs` files verified by deriving the expected namespace from the physical path
and comparing the declared namespace (BOM-stripped, block-scoped `namespace … {` handled).

**Result: 0 mismatches.** `Path-Namespace-State = EXACT`.

## 4. Alias / shim proof

`TypeForwardedTo` → ZERO. `global using Tooba.Media` → ZERO. Namespace alias workaround → NONE.
No duplicate compatibility type, no stale root copy, no duplicate physical copy of `MediaDirectory`,
`MediaDbContext` or `LocalFileMediaStore`. `Alias-Workaround-State = NONE`,
`Physical-Copy-State = CLEAN`.

## 5. Endpoint ownership + route count

Module-owned, mapped only by `MediaEndpointModule.MapMediaModuleEndpoints()`:

| # | Route | Mapped in |
| --- | --- | --- |
| 1 | `POST /v1/admin/media/upload` (`DisableAntiforgery`) | `Admin/MediaAdminEndpoints.Map` |
| 2 | `GET /v1/admin/media/` | `Admin/MediaAdminEndpoints.Map` |
| 3 | `GET /v1/admin/media/{id:guid}` | `Admin/MediaAdminEndpoints.Map` |
| 4 | `GET /v1/media/{id:guid}` | `MediaEndpointModule.MapMediaModuleEndpoints` |
| 5 | `GET /v1/storefront/media/{assetId:guid}` | `Storefront/MediaStorefrontEndpoints` |

`routeCount = 5`, `Endpoint-Ownership-State = MODULE_ENDPOINTS`, Host-owned Media routes = **ZERO**,
duplicate route ownership = NONE.

## 6. Request → handler → validator matrix

| Request | `IRequest<T>` | `IRequestHandler<,>` | `ISender` dispatch | Classification | Validator |
| --- | --- | --- | --- | --- | --- |
| `UploadMediaAssetCommand` | ✓ `Result<MediaAssetInfo>` | ✓ | ✓ `MediaAdminEndpoints.UploadAsync` | `VALIDATOR_REQUIRED_PRESENT` | `UploadMediaAssetCommandValidator` |
| `QueryMediaAssetsQuery` | ✓ `Result<MediaPagedResult<MediaAssetInfo>>` | ✓ | ✓ `MediaAdminEndpoints.QueryAsync` | `VALIDATOR_REQUIRED_PRESENT` | `QueryMediaAssetsQueryValidator` |
| `GetMediaAssetQuery` | ✓ `Result<MediaAssetInfo>` | ✓ | ✓ `Admin.GetAsync` + `MediaAssetServing.ServeAsync` | `VALIDATOR_REQUIRED_PRESENT` | `GetMediaAssetQueryValidator` |
| `GetMediaStorageKeyQuery` | ✓ `Result<string?>` | ✓ | ✓ `MediaAssetServing.ServeAsync` | `NO_VALIDATOR_REQUIRED` | — |

`endpointReachableRequests = 4`, `validatorRequiredCount = 3`, `noValidatorRequiredCount = 1`,
validator gap **ZERO**. MediatR **12.5.0** via `AddToobaCqrsFoundation`; discovery through
`AddValidatorsFromAssembly`; no manual validator invocation in endpoints or handlers; no legacy
dispatcher; no endpoint direct persistence/directory call (`MediaAssetServing` uses `ISender` only —
the dead `TryServeStoredMediaAsync` bypass was removed in W1).

`GetMediaStorageKeyQuery` is `NO_VALIDATOR_REQUIRED` with a durable explicit reason: it carries only a
route-derived `Guid` already validated by the paired `GetMediaAssetQuery` in the same request path.

## 7. Validator coverage (transport shape only)

| Validator | Rules | Codes |
| --- | --- | --- |
| `UploadMediaAssetCommandValidator` | `Content` not null; `OriginalFileName` not empty; `ContentType` not empty | `media.validation.upload_content_required` / `_original_file_name_required` / `_content_type_required` |
| `QueryMediaAssetsQueryValidator` | `Page >= 1`; `PageSize` in 1..100 | `media.validation.page_out_of_range` / `_page_size_out_of_range` |
| `GetMediaAssetQueryValidator` | `MediaAssetId` not empty | `media.validation.media_asset_id_required` |

Validators emit **stable machine codes**, never localized text, and never business codes. The six
`MediaValidationCodes` are deliberately **not** registered as error-catalog descriptors: the canonical
`ValidationBehavior` pipeline maps them through the foundation `validation.failed` descriptor
(certified Localization/Content/Cart/Offer precedent).

## 8. Localization coverage (codes → catalog → resources)

| Code | Declared in | Registered descriptor | `MediaErrors.resx` | `MediaErrors.fa.resx` |
| --- | --- | --- | --- | --- |
| `media.upload.failed` | `MediaErrorCodes` | ✓ Business/400 | ✓ | ✓ |
| `media.type.unsupported` | `MediaErrorCodes` | ✓ Business/400 | ✓ | ✓ |
| `media.too_large` | `MediaErrorCodes` | ✓ Business/400 | ✓ | ✓ |
| `media.storage.unavailable` | `MediaErrorCodes` | ✓ Platform/503 | ✓ | ✓ |
| `media.missing` | `MediaErrorCodes` | ✓ NotFound/404 | ✓ | ✓ |
| `media.validation.failed` | `MediaErrorCodes` | ✓ Validation/400 | ✓ | ✓ |
| `media.asset.missing` | `MediaAssetContractCodes` | ✓ NotFound/404 (same owner) | ✓ | ✓ |

**7 declared = 7 descriptors = 7 × 2 cultures**, single owner `Contracts/Errors/` (contributor + resource
set registered by `MediaModule` as `IErrorCatalogContributor` / `IErrorResourceSet`).
`MediaErrorResourceSet.Owns()` claims the whole `media.` prefix — correct here because every `media.*`
string in the repository is Media-owned (no foreign collision). No duplicate descriptor ownership, no
duplicate-suppression mechanism, no shared-errors project. `Localization-State = CANONICAL`.

Canonical localizer use verified: `MediaAdminEndpoints.ResolveTitle` resolves the per-item batch title
through `IErrorMessageLocalizer` + `IRequestLocaleResolver` (the W0 high-severity hard-coded `fa`
culture is gone). Zero hard-coded user-facing fault text; zero `ex.Message` used as a contract; no
endpoint-level `Accept-Language` parsing outside the canonical locale resolver.

## 9. API result / error mapping proof

- `Results.Json` / `Results.BadRequest` / `Results.Problem` in Media: **ZERO**.
- Local `ProblemDetails` builder / local error mapper: **ZERO**.
- Failure classification by parsing `ex.Message`: **ZERO** (`ex.Message.Contains`/`StartsWith` absent).
- `catch`-and-map blocks in endpoints for expected failures: **ZERO**.
- Unknown/unexpected exceptions are not converted to business failures: `MediaOperation` maps only
  `ContractOperationException` **guarded by `MediaErrorCodes.IsKnown`** plus `SemanticException`; a
  foreign or codeless fault propagates untouched to the global presentation boundary.
- The only `Results.File` / `Results.Text` are the correct minimal-API primitives for the non-JSON
  binary and SVG payloads owned solely by `MediaAssetServing`.
- Success shape preserved: `api.From(Result.Success(new MediaUploadBatchResponse(results)))` keeps the
  shipped `{ items: [ { ok, asset | fileName, title, errorCode } ] }` envelope at HTTP 200, and the
  query/get endpoints keep `api.From(Result)`.

`API-Result-Pattern-State = CANONICAL`. `Stable-Error-Code-State = CATALOGUED`.

## 10. Logging / sensitive-data proof

- `ILogger<T>` structured logging only: `UploadMediaAssetCommandHandler` logs
  `logger.LogInformation("{MediaUploadEvent}", MediaErrorCodes.UploadFailed)` and
  `logger.LogInformation("media.upload.succeeded")`.
- `Console.WriteLine` / `Debug.WriteLine` / second logging framework / second telemetry pipeline: **ZERO**.
- Sensitive material logged (passwords, tokens, OTP/reset secrets, `Authorization` headers, cookies,
  session secrets, credentials, payment payloads): **NONE**.

`Logging-State = CANONICAL`, `Sensitive-Logging-State = NONE`.

## 11. Correlation / trace continuity proof

`ActivitySource` / `Meter` / `StartActivity(...)` / manual `traceparent` parsing / raw `AsyncLocal` /
custom correlation header or middleware: **ZERO** in Media. Media issues no cross-module call of its
own, so no `IModuleCallTracer` decoration is required; foreign consumers decorate their own calls to
the Media Contracts ports. `traceId`/`correlationId` reach Media responses through the canonical
`ProblemDetailsContextProvider`. `OpenTelemetry-State = CANONICAL`, `Correlation-Trace-State = CANONICAL`.

## 12. File cohesion / size proof

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

Largest hand-written file **135 LOC**; no Media entry in `Baselines/tmar-source-size-baseline.json`;
well below the 800 LOC `ARCH-SIZE-001` ceiling. No god-file, no cosmetic over-split, no generic
Application `*Contracts.cs` bundle, no duplicate CQRS request shape.
`File-Cohesion-State = COHESIVE`.

## 13. Host authority classification

| Host artifact | Reference | Classification |
| --- | --- | --- |
| `Program.cs` | `using Tooba.Media.Endpoints` (L67), module assembly registration (L186), `app.MapMediaModuleEndpoints()` (L446) | `ALLOWED_COMPOSITION_ROOT` |
| `Composition/ToobaModuleComposition.cs` | `using Tooba.Media.Infrastructure` (L28), `new MediaModule()` (L78) | `ALLOWED_COMPOSITION_ROOT` |
| `Development/MarketplaceDevelopmentBootstrap.cs` | `MediaDbContext` dev migrate (L4, L57) | `ALLOWED_DEV_MIGRATE_SEAM` |
| `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` | `Descriptor<MediaDbContext>("Media", MediaDbContext.Schema)` (L78) | `ALLOWED_COMPOSITION_ROOT` |

`ILLEGAL_BUSINESS_AUTHORITY = 0`, `ILLEGAL_PERSISTENCE_AUTHORITY = 0`,
`ILLEGAL_ENDPOINT_OWNERSHIP = 0`. `Host/Tooba.Host/Media` folder **absent**; `namespace
Tooba.Host.Media` **ZERO**; no Host `MapMediaAdminEndpoints`/`MapMediaStorefrontEndpoints`.
`Host-Final-Closure-State = PRESERVED`, `Sink-Folder-Regression-State = ZERO`.

## 14. Cross-module dependency inventory

| Project | Foreign references | Verdict |
| --- | --- | --- |
| `Contracts` | `Tooba.BuildingBlocks` | legal platform seam |
| `Domain` | `Tooba.BuildingBlocks` | legal platform seam |
| `Application` | `Tooba.BuildingBlocks` | legal platform seam |
| `Endpoints` | `Tooba.BuildingBlocks` | legal platform seam |
| `Infrastructure` | `Tooba.ModuleContracts`, `Tooba.Persistence` | legal platform seams |

No foreign `*.Application` / `*.Infrastructure` / `*.Domain` / `*.Endpoints` project edge anywhere.
Legal *consumer* edges into Media are Contracts-only: `Payment.Application → Tooba.Media.Contracts`,
`Content.Infrastructure → Tooba.Media.Contracts`, `Catalog.Infrastructure → Tooba.Media.Contracts`.
`foreignAppInfraDomainCoupling = ZERO`, `Cross-Module-Boundary-State = CONTRACTS_ONLY`.

## 15. Explicit no-cross-module-join proof

`FromSqlRaw` / `ExecuteSqlRaw` in Media: **ZERO**. No foreign schema literal
(`"catalog.` / `"order.` / `"content.` / `"payment.` / `"identity.` / `"localization.` /
`"inventory.` / `"offer.` / `"party.`) anywhere. `MediaDbContext` owns only the `media` schema:
`DbSet<MediaAsset> Assets` → `media.assets` and the shared `OutboxMessage` table. No foreign `DbSet`,
no foreign `DbContext`, no EF navigation crossing ownership, no cross-module transaction assumption,
no foreign module reading `media.*` by raw SQL. `Cross-Module-Join-State = NONE`.

## 16. Persistence / schema safety

`public const string Schema = "media"` + `modelBuilder.HasDefaultSchema(Schema)` unchanged. Migrations
are exactly `20260830060000_InitialMedia.cs` and `20260916053000_AddMediaFocalPoint.cs` (plus the
generated designer/snapshot); no migration id, order, `Up`/`Down`, table, column, index, constraint or
snapshot semantics changed, and no new migration was generated for this standardization.
`migrationFilesChanged = 0`, `Schema-Migration-State = UNCHANGED`.

## 17. Durable guards

`MediaModuleAmsc001W3CertGuardTests` (7 facts) locks: manifest/SoT certification records with the AMSC
wave lineage and the single certified-module entry; the single stable-code owner with bilingual
coverage (7 = 6 + 1, 7 descriptors, 7 × 2 resources); the typed-fault seam and the two confined
raw-literal idiom files; module-owned HTTP surface with Host residue limited to composition + the
dev-migration seam; microservice extractability with zero foreign coupling and zero cross-module join;
`media` schema and migration preservation; the W0→W3 evidence tree and the preserved repository-global
Host root checkpoint.

Complementary existing guards: `MediaModuleAmsc001W2StructureGuardTests`,
`MediaModuleAmsc001W1MigrateGuardTests`, `MediaModuleAmcW1SolutionGuardTests`,
`MediaModuleAmcW2StructureGuardTests`, `MediaModuleAmcW3CqrsGuardTests`,
`MediaModuleAmcW4CertGuardTests`, `HostMediaEvacuationGuardTests`, `ErrorCatalogUniqueCodeGuardTests`,
`MediaDamTests`.

## 18. Manifest state

`tmar-module-structure-manifests.json` already contains exactly **one** `Media` entry in `modules[]`
with `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002` and disk-accurate `rootAllowlist` /
`forbiddenRootFiles` / `forbiddenTopLevelFolders` for all five projects — re-verified against disk in
§2. No `Media` entry exists in `preCertModules`, so no duplicate-entry defect and no promotion edit was
required. `manifestChanged = false`.

## 19. SoT state

`tmar-current-state.json` records `mediaModuleAmsc001W3` with `state = MEDIA_AMSC_001_CERTIFIED`,
`verdict = COMPLETE_REFERENCE_PATTERN`, `lockVersion = ARCH-COMPLETE-002`, `structureState = CERTIFIED`,
`structureCertified = true`, `pathNamespaceState = EXACT`, `rootAllowlistState = ENFORCED`,
`foreignAppInfraDomainCoupling = ZERO`, `httpApplicability = HTTP_OWNING`,
`endpointOwnershipState = MODULE_ENDPOINTS`, `hostHttpOwnership = ZERO`, `routeCount = 5`,
`endpointReachableRequests = 4`, `blockingResidualDebt = ZERO`, `microserviceExtractable = true`,
`stopGate = USER_REVIEW_MEDIA_AMSC_001_W3`, `automaticNextImplementationTask = NONE`.
`structureLock.certifiedModules` contains exactly one `Media` entry. The historical `mediaAmc001` /
`mediaAmc001W4R1` records are not rewritten. The repository-global Host root checkpoint
(`lastAcceptedTask`, `lastAcceptedCommit`, `latestAcceptedImplementationWave`,
`currentHostCheckpoint`, `nextHostFolder`, `workflowStop`, `automaticNextImplementationTask`) is
untouched.

## 20. Focused builds

`dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj` builds `Tooba.Media.Contracts`,
`Tooba.Media.Domain`, `Tooba.Media.Application`, `Tooba.Media.Infrastructure`,
`Tooba.Media.Endpoints`, `Tooba.Host` and `Tooba.Host.Tests` with **0 errors**.

## 21. Focused tests

| Run | Result |
| --- | --- |
| `dotnet test --filter FullyQualifiedName~MediaModuleAmsc001` | **30 passed / 0 failed** (13 W1 + 10 W2 + 7 W3) |
| `dotnet test --filter FullyQualifiedName~Media` | **82 passed / 3 docker-skipped / 0 failed** |

No broad solution-wide run, no open-ended repair loop, no guard weakened.

## 22. Residual non-blocking debt

Pre-existing, unrelated to Media, disclosed and **not repaired** (module-local scope; repairing them
would displace the repository-global Host root checkpoint):

- `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`
  and `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` are red at the W3
  starting HEAD `991551e9` — reproduced identically with the W3 SoT block stashed. They pin a frozen
  16-module `structureLock.certifiedModules` list and a master-recovery checkpoint substring.
- The `Architecture` namespace carries 43 pre-existing unrelated failures (Catalog Contracts, Host
  Admin/Grid/StoreMenu AMC lineages, ProductWorkspace shells, `HostAdminCanon001`), identical with and
  without the new W3 guard.

Accepted, explicit, documented non-conformances retained (not debt):

- Legacy-Guid SVG fallback returns HTTP 200 `image/svg+xml` for a missing asset — a locked client
  contract pinned by `HostMediaEvacuationGuardTests`.
- SVG `aria-label` Persian presentation text — image payload text, not an API message.
- `MediaOutboxRegistration.GetEventTypeName` throws the framework outbox invariant
  `InvalidOperationException` with literal text — the repo-wide idiom shared by certified
  Localization/Content/CustomerProfile, confined to one file, never user-facing.
- `MediaAsset` Domain invariants raise `InvalidOperationException` with literal text — matching the
  Order/Catalog/Promotion/Notification/Wallet/Support/Returns/Tax domain precedent. They are **not**
  mapped by `MediaOperation` (which catches only `ContractOperationException` / `SemanticException`),
  so they can never surface as a user-facing contract.

`blockingResidualDebt = ZERO`.

## 23. Exact certification verdict

```text
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
Structure-State = CERTIFIED
Stop gate = USER_REVIEW_MEDIA_AMSC_001_W3
automaticNextImplementationTask = NONE
```
