# TB-TMAR-MEDIA-AMSC-001-W1 — Migrate (tooba-architecture-migrate)

- Task: `TB-TMAR-MEDIA-AMSC-001-W1`
- Skill: `tooba-architecture-migrate`
- Target: `src/backend/Modules/Media/Tooba.Media.*`
- Lock: `ARCH-COMPLETE-002`
- Parent wave: `TB-TMAR-MEDIA-AMSC-001-W0` (`06f7de21`)
- Structure-Handoff-State: `REQUIRED` (physical organization is re-verified in W2)

## 1. Foundation check

`FOUNDATION_READY`. Five canonical projects already exist and are `structureCertified: true` under
`ARCH-COMPLETE-002`; no parallel structure was created and no project path/assembly name changed.

## 2. Responsibility split (from W0 `MUST_SPLIT`)

| Item | Before | After |
| --- | --- | --- |
| `Infrastructure/Assets/MediaDirectory.cs` (209 LOC, 2 responsibilities) | upload pipeline + library queries + shared helpers in one file | `MediaDirectory.cs` (shared seam: config bounds, MIME policy, extension map, `Map`, `NormalizeContentType`, `SanitizeOriginalFileName`, `EscapeLike`) + `MediaDirectory.Upload.cs` (`UploadAsync`) + `MediaDirectory.Queries.cs` (`QueryAsync`/`GetAsync`/`GetManyAsync`/`GetStorageKeyAsync`) |
| `Contracts/Assets/MediaAssetContracts.cs` (generic mixed name, `#pragma` + no docs) | one `IMediaAssetUploadPort` in a misleadingly named bundle | `Contracts/Assets/IMediaAssetUploadPort.cs` (same namespace/type; documented) |

Partial-class split, so DI lifetime (`Scoped`), constructor shape, public signatures and behavior are
byte-for-byte preserved. No new type, no facade, no compatibility shim.

## 3. Canonical typed-fault seam

`MediaOperation` (Application/Composition) now mirrors the certified
Cart/Fulfillment/Inventory/Content/Localization `*Operation` seam:

- maps `ContractOperationException` **guarded by `MediaErrorCodes.IsKnown(ex.Code)`**;
- maps `SemanticException` by `ex.Error`;
- unknown codes and unknown exceptions propagate untouched to the canonical global exception
  boundary (`IExceptionPresentationService` / `SafeErrorMapper`).

`MediaErrorCodes` gained a private `KnownCodes` set + public `IsKnown(string?)` — the module's single
declared-code authority.

`Infrastructure` no longer raises the legacy Host-platform `PlatformHttpException` (11 sites):
`MediaDirectory.Upload` (4), `LocalFileMediaStore` (5), and the `catch`-and-remap sites now raise
`ContractOperationException` carrying the **same** stable codes. `MediaAssetReadinessBridge` already
used the typed seam and is unchanged.

### Observable deltas (accepted, documented)

| Behavior | Before | After | Why acceptable |
| --- | --- | --- | --- |
| HTTP status for `media.storage.unavailable` | `503` (`PlatformHttpException.StatusCode`) | `503` (catalog `ErrorDescriptor.HttpStatus`, already registered `Platform` + 503) | identical status and code |
| HTTP status for invalid upload stream | `400` | `400` (catalog `UploadFailed` is `Business` + 400) | identical |
| HTTP status for invalid storage key | `400` (`PlatformHttpException(400, …)`) | `503` (`StorageUnavailable` is registered 503) | the same stable code was already registered as 503 in the pre-existing catalog, so the 400/503 split was itself an internal inconsistency; 503 is the canonical registered meaning of `media.storage.unavailable`. Not reachable from HTTP: storage keys are server-derived (`{yyyy}/{MM}/{assetId:N}{ext}`) and never client-supplied. |
| Fault type surfaced to non-HTTP in-process consumers (Payment proof upload) | `PlatformHttpException` (not mapped by `PaymentExceptionMapper` → global 500) | `ContractOperationException` (still not a Payment code → still propagates → global 500) | identical end state; the typed seam is now the canonical mechanism |
| Validation error code payload (ProblemDetails `errors[property][0]`) | `media.validation.failed` | `media.validation.<field>` via foundation `validation.failed` | HTTP 400, `errorCode=validation.failed` and the message envelope are unchanged; only the per-field transport identity is now distinct, matching Localization/Content/Cart/Offer |
| Per-item batch `title` culture | hard-coded `fa` | request locale via `IRequestLocaleResolver` | removes a hard-coded culture; the Admin UI renders its own localized message and the field remains present with the same shape |
| Log event for upload failure | `LogInformation("media.upload.failed")` (message==code) | `LogInformation("{MediaUploadEvent}", MediaErrorCodes.UploadFailed)` (structured template, same event name) | same event name, now a proper structured template |

Routes, methods, status codes for all reachable paths, response shapes, stable codes, authorization,
MIME allow-list, size ceiling, checksum, storage-key derivation, paging/ordering, schema and both
migration files are unchanged.

## 4. CQRS repair

- Removed the dead, non-canonical `MediaAssetServing.TryServeStoredMediaAsync(Guid, IMediaDirectory,
  IMediaObjectStore, CancellationToken)` helper (endpoint → Application port + Infrastructure adapter,
  no production caller). The only caller was `MediaDamTests`, which was updated to drive the
  canonical `ServeAsync` through a minimal `ISender` adapter — no test-only production code was
  introduced and no public route behavior changed.
- `MediaAssetServing.ServeAsync` remains the single serving path and dispatches both queries through
  `ISender`.
- `MediaAdminEndpoints` continues to be transport-only: `ISender` + `ApiResponseFactory` +
  `IAdminPanelAccess`, zero `Results.Json`, zero direct port/DbContext/Infrastructure use.

## 5. Localization repair

- `MediaAdminEndpoints` no longer reads `MediaErrorResources` with `CultureInfo.GetCultureInfo("fa")`.
  It now injects the canonical `IErrorMessageLocalizer` + `IRequestLocaleResolver` and resolves the
  per-item batch title through the composed resource sets for the resolved request locale.
- All 11 hard-coded Persian fault strings in Infrastructure were deleted together with the legacy
  fault type; the client-facing title is produced only by the canonical
  `IErrorMessageLocalizer` + `.resx` path.
- Retained, explicitly documented non-conformance: the fallback SVG `aria-label="نمایش موقت رسانه"`
  is presentation text inside an image payload (pinned by `HostMediaEvacuationGuardTests`), not an API
  message.
- Keys, values and semantics of `MediaErrors.resx` / `MediaErrors.fa.resx` are untouched.

## 6. Validation

| Request | Classification | Validator | Code emitted |
| --- | --- | --- | --- |
| `UploadMediaAssetCommand` | `VALIDATOR_REQUIRED_PRESENT` | `UploadMediaAssetCommandValidator` | `media.validation.upload_content_required`, `…_original_file_name_required`, `…_content_type_required` |
| `QueryMediaAssetsQuery` | `VALIDATOR_REQUIRED_PRESENT` | `QueryMediaAssetsQueryValidator` | `media.validation.page_out_of_range`, `media.validation.page_size_out_of_range` |
| `GetMediaAssetQuery` | `VALIDATOR_REQUIRED_PRESENT` | `GetMediaAssetQueryValidator` | `media.validation.media_asset_id_required` |
| `GetMediaStorageKeyQuery` | `NO_VALIDATOR_REQUIRED` | — | — |

New `MediaValidationCodes` (Application/Assets/Validators) is deliberately **not** registered as an
error-catalog descriptor: the canonical `ValidationBehavior` maps it through the foundation
`validation.failed` descriptor. `MediaErrorCodes.ValidationFailed` is retained as the Media-owned
validation classification code used when Media raises a validation fault outside the transport
pipeline.

## 7. Error catalog / resources

7 declared Media codes (`MediaErrorCodes` 6 + `MediaAssetContractCodes.AssetMissing` 1)
= 7 registered descriptors in `MediaErrorCatalogContributor`
= 7 keys in `MediaErrors.resx` and 7 keys in `MediaErrors.fa.resx`.
Single descriptor owner per code, no duplicate registration, no suppression mechanism added.
`MediaErrorResourceSet.Owns("media.")` is correct: every `media.*` string in the repository is
Media-owned.

## 8. Files created / modified / deleted

Created:
- `Contracts/Assets/IMediaAssetUploadPort.cs`
- `Application/Assets/Validators/MediaValidationCodes.cs`
- `Infrastructure/Assets/MediaDirectory.Upload.cs`
- `Infrastructure/Assets/MediaDirectory.Queries.cs`
- `Host/Tooba.Host.Tests/Architecture/MediaModuleAmsc001W1MigrateGuardTests.cs`

Deleted:
- `Contracts/Assets/MediaAssetContracts.cs`

Modified:
- `Contracts/Errors/MediaErrorCodes.cs` (+ `KnownCodes` / `IsKnown`)
- `Application/Composition/MediaOperation.cs`
- `Application/Assets/Commands/UploadMediaAssetCommand.cs`
- `Application/Assets/Validators/{Upload,Query,GetMediaAsset}*Validator.cs`
- `Infrastructure/Assets/MediaDirectory.cs`
- `Infrastructure/Storage/LocalFileMediaStore.cs`
- `Infrastructure/MediaModule.cs` (unused `Application.Models` using removed)
- `Endpoints/Admin/MediaAdminEndpoints.cs`
- `Endpoints/Admin/MediaAssetServing.cs`
- `Host/Tooba.Host.Tests/MediaDamTests.cs`
- `Host/Tooba.Host.Tests/Architecture/HostMediaEvacuationGuardTests.cs` (symbolic code assertions)

Host production code was **not** touched. Frontend was **not** touched. Schema/migrations were
**not** touched (`migrationFilesChanged = 0`).

## 9. Focused validation

- `dotnet build` Media Infrastructure / Media Endpoints / Tooba.Host / Tooba.Host.Tests → succeeded.
- `dotnet test --filter FullyQualifiedName~Media` → **65 passed, 3 skipped (Docker-gated), 0 failed**.
- Related focused guards (`ErrorCatalog`, `AdminPanelComposition`, `CatalogDemoResetSeed`,
  `ContentArticleMedia`, `HostAdminCanonicalCertification`, `AuthenticationV2Canonicalization`) →
  **50 passed, 3 skipped, 0 failed**.
- New durable guard `MediaModuleAmsc001W1MigrateGuardTests` (13 facts): typed-fault seam, declared-code
  catalog, infrastructure fault typing, cohesion split, transport validation codes, no raw code
  literals in production, published code values, Contracts boundary, endpoint canonicalization,
  zero foreign module edges, Domain/Application fault typing, schema/migration immutability.

## 10. Residual debt

- The legacy-Guid SVG fallback (HTTP 200 for a missing asset) is preserved as a locked client
  contract; recorded as an accepted non-conformance rather than a defect.
- The SVG `aria-label` Persian presentation text is retained (image payload text, not an API message).
- W2 must re-verify physical organization (including the new `MediaDirectory.*` partials and
  `MediaValidationCodes`) and issue `READY_FOR_CERTIFY` before W3.

## 11. Completion state

`READY_FOR_CERTIFICATION` at the migrate level; `Structure-Handoff-State = REQUIRED`.
Stop gate: `USER_REVIEW_MEDIA_AMSC_001_W1`. `automaticNextImplementationTask = NONE`.
