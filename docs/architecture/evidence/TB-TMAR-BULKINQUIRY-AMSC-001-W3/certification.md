# TB-TMAR-BULKINQUIRY-AMSC-001-W3 — Certification

## Verdict

`COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`

## 1. Final physical tree

```text
Modules/BulkInquiry/
  Tooba.BulkInquiry.Contracts/       Errors/{BulkInquiryErrorCodes, BulkInquiryErrorCatalogContributor, BulkInquiryErrorResourceSet}.cs
                                     Resources/{BulkInquiryErrors.resx, BulkInquiryErrors.fa.resx}
  Tooba.BulkInquiry.Domain/          Aggregates/BulkPurchaseInquiry.cs, Enums/BulkInquiryStatus.cs
  Tooba.BulkInquiry.Application/     Composition/BulkInquiryOperation.cs
                                     Models/BulkInquiryModels.cs, Ports/IBulkInquiryDirectory.cs
                                     Storefront/Commands/SubmitBulkInquiryCommand.cs
                                     Validation/{BulkInquiryValidationCodes, SubmitBulkInquiryCommandValidator}.cs
  Tooba.BulkInquiry.Infrastructure/  BulkInquiryModule.cs
                                     Directories/BulkInquiryDirectory.cs
                                     Persistence/{BulkInquiryDbContext, BulkInquiryOutboxRegistration}.cs
                                     Persistence/Migrations/… (3 migrations + snapshot)
  Tooba.BulkInquiry.Endpoints/       BulkInquiryEndpointModule.cs, Storefront/BulkInquiryStorefrontEndpoints.cs
```

## 2. Root allowlists

| Project | allowlist | forbidden root files | forbidden top-level folders |
|---|---|---|---|
| Contracts | [] | `BulkInquiryErrorCodes.cs` | — |
| Domain | [] | `BulkPurchaseInquiry.cs` | — |
| Application | [] | `BulkInquiryContracts.cs` | `Commands`, `Queries`, `Validators` |
| Infrastructure | [`BulkInquiryModule.cs`] | `BulkInquiryDirectory.cs`, `BulkInquiryDbContext.cs`, `BulkInquiryOutboxRegistration.cs` | `Migrations` |
| Endpoints | [`BulkInquiryEndpointModule.cs`] | `BulkInquiryStorefrontEndpoints.cs` | `Errors`, `Resources` |

## 3. Path ↔ namespace proof

All 17 production `.cs` files: declared namespace == path-derived namespace (`EXACT`). Guard:
`BulkInquiryModuleAmsc001W2StructureGuardTests.BulkInquiry_path_namespace_alignment_is_exact`.

## 4. Alias / shim proof

`aliasWorkaround = NONE`, `typeForwardedToWorkaround = ZERO`, `namespaceAliasWorkaround = ZERO`.

## 5. Endpoint ownership

`MODULE_OWNED` via `MapBulkInquiryModuleEndpoints`; Host HTTP ownership ZERO; duplicate mapping ZERO.

## 6. Route count

**1** module-owned route: `POST /v1/storefront/products/{slug}/bulk-inquiries`.

## 7. Request → handler → validator matrix

| Request | Handler | Classification | Validator |
|---|---|---|---|
| `SubmitBulkInquiryCommand` | `SubmitBulkInquiryCommandHandler` | `VALIDATOR_REQUIRED` | `SubmitBulkInquiryCommandValidator` |

## 8. Validator coverage

`EXHAUSTIVE` — 1 endpoint-reachable request, 1 `VALIDATOR_REQUIRED` (present, discoverable through
`AddToobaCqrsFoundation`/`AddValidatorsFromAssembly`), 0 `NO_VALIDATOR_REQUIRED`. Validator emits stable
codes from `BulkInquiryValidationCodes`; no localized text.

## 9. Localization coverage

| Code | Catalog | en resx | fa resx |
|---|---|---|---|
| `bulk_inquiry.rejected` | ✓ | ✓ | ✓ |
| `bulk_inquiry.validation.request_required` | foundation `validation.failed` | ✓ | ✓ |
| `bulk_inquiry.validation.slug_required` | foundation `validation.failed` | ✓ | ✓ |
| `bulk_inquiry.validation.full_name_required` | foundation `validation.failed` | ✓ | ✓ |
| `bulk_inquiry.validation.phone_required` | foundation `validation.failed` | ✓ | ✓ |
| `bulk_inquiry.validation.address_required` | foundation `validation.failed` | ✓ | ✓ |

Composed-catalog uniqueness: BulkInquiry registers exactly one descriptor; no duplicate machine code.
Descriptor ownership is unambiguous (BulkInquiry owns `bulk_inquiry.rejected`; validation codes use the
foundation-owned `validation.failed` descriptor, matching certified sibling modules).

## 10. API result / error mapping proof

`ApiResponseFactory.Created(location, result)` only; zero `Results.Json` / `Results.BadRequest` /
`Results.Problem` / local mapper / endpoint catch-map / `ex.Message` classification.

## 11. Logging / sensitive-data proof

No `Console.WriteLine`, no second telemetry pipeline, no logging of `Phone` / `Email` / `Address`.

## 12. Correlation / trace continuity

No custom correlation, no direct `StartActivity`, no manual `traceparent`. Presentation uses the canonical
`IProblemDetailsContextProvider` for `traceId` / `correlationId`.

## 13. File cohesion / size proof

Largest hand-written production file `Domain/Aggregates/BulkPurchaseInquiry.cs` = 128 lines (ceiling 800);
no size-baseline entry; no god-file, no over-split, no duplicate CQRS request shape.

## 14. Host authority classification

| Host reference | Class |
|---|---|
| `Program.cs` using + `AddBulkInquiryEndpointPresentation` + handler assembly + `MapBulkInquiryModuleEndpoints` | `ALLOWED_COMPOSITION_ROOT` |
| `Composition/ToobaModuleComposition.cs` `new BulkInquiryModule()` | `ALLOWED_COMPOSITION_ROOT` |
| `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` schema descriptor | `ALLOWED_COMPOSITION_ROOT` |

ILLEGAL categories = ZERO. Host BulkInquiry folder absent; Host final closure preserved; no sink-folder regression.

## 15. Cross-module dependency inventory

`Tooba.Catalog.Contracts.Ports.ICatalogReviewProductLookup` (+ `CatalogReviewableProductDto`) only.
Foreign Application / Infrastructure / Domain = ZERO.

## 16. No cross-module join proof

`BulkInquiryDirectory` reads its own `BulkInquiryDbContext` plus the Catalog Contracts port; no EF
navigation, no foreign DbSet, no cross-schema SQL.

## 17. Persistence / schema safety

Schema `bulk_inquiry`; table `bulk_purchase_inquiries`; migrations unchanged (3 files + snapshot, IDs and
order untouched); no new migration; no cross-module FK.

## 18. Durable guards

- `BulkInquiryModuleAmsc001W3CertGuardTests` (new, this wave)
- `BulkInquiryModuleAmsc001W2StructureGuardTests` (new, W2)
- `BulkInquiryModuleAmcW1SolutionGuardTests`, `…W2StructureGuardTests`, `…W3CqrsGuardTests`, `…W4CertGuardTests`
- `HostProductQnAAmcGuardTests`, `TmarCompleteReferenceStructureGateTests`, `TmarSourceSizeGuard`

## 19. Manifest state

`tmar-module-structure-manifests.json` — exactly one BulkInquiry entry, `structureCertified: true`,
`lockVersion: ARCH-COMPLETE-002`, certification note now records the AMSC-001 wave lineage and SHAs.
Already absent from `uncertifiedHttpOwningModules`.

## 20. SoT state

`tmar-current-state.json` — `bulkInquiryModuleAmsc001W0..W3` records added; `bulkInquiryModuleAmsc001W3`
holds the authoritative `COMPLETE_REFERENCE_PATTERN` / `CERTIFIED` state; historical `bulkInquiryAmc001`
annotated with `supersededBy`; `structureLock.certifiedModules` still contains BulkInquiry exactly once.

## 21. Focused builds

`dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj` → 0 errors.

## 22. Focused tests

`dotnet test --filter FullyQualifiedName~BulkInquiry` → **23 passed / 0 failed / 2 skipped** (skips are
Postgres Testcontainers). Reconciled by `TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1` from one deterministic run.

## 23. Residual non-blocking debt

R1 validation codes uncatalogued by design; R2 legitimate self-module Domain → Contracts constant
reference; R3 pre-existing unrelated `TmarCompleteReferenceStructureGateTests` Catalog drift; R4 untracked
foreign evidence artifacts outside scope.

## 24. Exact certification verdict

`COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
Stop gate: `USER_REVIEW_BULKINQUIRY_AMSC_001_W3`.
