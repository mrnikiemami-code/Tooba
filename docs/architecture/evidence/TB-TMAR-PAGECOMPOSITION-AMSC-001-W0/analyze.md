# TB-TMAR-PAGECOMPOSITION-AMSC-001-W0 — Analyze (tooba-architecture-analyze)

## Scope

`src/backend/Modules/PageComposition/Tooba.PageComposition.*` — full AMSC re-standardization under `ARCH-COMPLETE-002` (W0 Analyze → W1 Migrate → W2 Structure → W3 Certify), starting head `0478668b` on `main` (`HEAD == origin/main`, known/safe tree).

## Structured State Fields

1. **Foundation-State**: `FOUNDATION_READY` — 5 production projects (Contracts/Domain/Application/Infrastructure/Endpoints) exist, all grouped under `/Modules/PageComposition/` in `Tooba.slnx`; module carries a prior AMC-001 W4 certification (`pageCompositionAmc001` SoT block, `COMPLETE_REFERENCE_PATTERN`, `manifestCertified: true`, `structureCertified: true` in manifest).
2. **Ownership-State**: `correct` — tenant/storefront home page composition (page definitions, approved section catalog, section visibility/order/variant/config) is PageComposition-owned; product rail payloads stay owned by the consuming surfaces; no foreign business state is touched.
3. **File-Cohesion-State**: `COHESIVE` — 30 production files; largest: `Infrastructure/Directories/PageCompositionDirectory.cs` 233 LOC (single persistence seam implementing the module port), `Domain/Catalog/SectionCatalog.cs` 186 LOC (approved section-type catalog — cohesive domain catalog), `Domain/Aggregates/PageDefinition.cs` 166 LOC (single aggregate), `Endpoints/Admin/PageCompositionAdminEndpoints.cs` 165 LOC (transport-only mapping over 7 routes). No ARCH-SIZE-001 baseline entry and none required (all far below ceiling).
4. **Oversized/God-File-State**: `NONE` — no multi-responsibility file; each large file has exactly one reason to change.
5. **Localization-State**: `CANONICAL` — `Contracts/Errors/PageCompositionErrorResourceSet.cs` (`IErrorResourceSet` owning the `page-composition.` keyspace) + bilingual `Contracts/Resources/PageCompositionErrors.resx` / `.fa.resx` (8 keys × 2 cultures); descriptors carry `LocalizationKey = code`; no hard-coded user-facing API text in production C# (Persian appears only in doc-comments and Development seed demo content — repo idiom, no API message contract).
6. **API-Result-Pattern-State**: `CANONICAL` — endpoints inject `ApiResponseFactory api` and return `api.From(...)` / `api.Created(...)`; zero `Results.Json/BadRequest/Problem` in module code.
7. **Stable-Error-Code-State**: `CATALOGUED_8_OF_8` + **one gap**: `PageCompositionErrorCodes.cs` declares 8 codes consumed by the catalog contributor, but lacks the canonical `IsKnown(string?)` declared-code guard used by the certified Media/Inventory/Notification/OperatorProfile seams; see blockers.
8. **Logging-State**: `CANONICAL` — module emits zero log calls today (no `ILogger` usage anywhere in production code); no Console/Debug writers, no duplicate telemetry, no string-concatenated log messages. Nothing to align; W1 has no logging repair (unlike OperatorProfile whose handlers logged bare literals).
9. **Sensitive-Logging-State**: `NONE` — no log call sites at all; no secrets in any payload logging path.
10. **OpenTelemetry-State**: `CANONICAL` — no second ActivitySource/Meter; no custom tracing; the module issues zero outbound cross-module calls, so `IModuleCallTracer` decoration is not applicable (foreign seam, if any, is inbound-only; currently PageComposition.Contracts has zero foreign consumers — verified by repo-wide grep).
11. **Correlation-Trace-State**: `CANONICAL` — no parallel correlation mechanism, no manual traceparent parsing; ProblemDetails trace/correlation flows through the BuildingBlocks `ProblemDetailsContextProvider`.
12. **CQRS-State**: `COMPLIANT` — 8 endpoint-reachable MediatR requests (5 Admin commands + 1 Admin query + 2 Storefront queries), all real `IRequest<Result<T>>` with real `IRequestHandler<,>`, thin endpoints dispatch through `ISender` only (no endpoint→Directory/DbContext direct call); MediatR registered via `AddToobaCqrsFoundation` (12.5.0) with the module assembly passed in `Program.cs` (`typeof(GetHomeCompositionQuery).Assembly`, line ~191).
13. **Validator-Coverage-State**: `EXHAUSTIVE_8_OF_8_REQUIRED_PRESENT` — validators: `AdminGetHomeCompositionQueryValidator`, `AdminAddHomeSectionCommandValidator`, `AdminReorderHomeSectionsCommandValidator`, `AdminUpdateHomeSectionCommandValidator`, `AdminRemoveHomeSectionCommandValidator`, `AdminRestoreDefaultHomeCompositionCommandValidator` (6 in `Admin/Validators/PageCompositionValidators.cs`), `GetHomeCompositionQueryValidator`, `GetSectionCatalogQueryValidator` (2 in `Storefront/Validators/StorefrontCompositionValidators.cs`); all emit stable `page-composition.*` codes via `WithErrorCode`; 0 `NO_VALIDATOR_REQUIRED`; discovery via `AddValidatorsFromAssembly` in the CQRS foundation.
14. **Contracts-Boundary-State**: `CLEAN` — `Contracts` carries only `Errors/` (codes, catalog contributor, resource set) and `Resources/` (resx pair). Zero module-boundary DTO/port is consumed by any other module (PageComposition is a leaf composition module); no Application-internal type leaked into Contracts; no generic `*Contracts.cs` dump.
15. **Cross-Module-Coupling-State**: `NONE_SELF_CONTAINED` — project references are exclusively self-module + BuildingBlocks/ModuleContracts/Persistence foundation; repo-wide grep proves **zero** foreign `*.Application|Infrastructure|Domain|Endpoints` references in any PageComposition project or source file, and zero foreign modules referencing PageComposition (stronger than the OperatorProfile contracts-only state: PageComposition has no foreign edge in either direction).
16. **Cross-Module-Join-State**: `NONE` — single `PageCompositionDbContext` touching only the `page_composition` schema (+ own Outbox table); no LINQ/SQL join to any foreign table, no foreign DbSet, no navigation crossing modules.
17. **Persistence-Ownership-State**: `CORRECT` — own schema `page_composition`, own `PageCompositionDbContext` (default schema + `OutboxMessageMapping.Map` on own schema), own migrations (`20260827021507_InitialPageComposition` + designer + snapshot), own `PageCompositionOutboxRegistration : IOutboxModuleRegistration`, registered via `AddModuleSchemaMigrator<PageCompositionDbContext>("PageComposition", ModuleSchemaMigrationOrder.PageComposition)`.
18. **Endpoint-Ownership-State**: `MODULE_OWNED` — 8 routes (7 Admin: GET/PUT/POST/DELETE under `/v1/admin/page-composition/home`, GET `/catalog`, POST `/restore-default`; 1 Storefront: GET `/v1/storefront/home/composition`) mapped by `PageCompositionEndpointModule.MapPageCompositionModuleEndpoints`; Host `PageComposition` folder = ZERO (HOST_ZERO closed by prior AMC-001 lineage, pinned by `HostPageCompositionAmcGuardTests`); the admin authorizer (`IPageCompositionAdminAuthorizer` + impl in one cohesive Endpoints file) is module-owned in Endpoints (no Host security adapter residue — cleaner than OperatorProfile); Host retains only composition-root wiring (Program.cs usings/`AddPageCompositionEndpointPresentation`/CQRS assembly/`MapPageCompositionModuleEndpoints` ×4), dev-seed/migration calls in the two Development bootstrappers and module registration in `ToobaModuleComposition.cs`.
19. **Host-Residue-State**: `ALLOWED_COMPOSITION_ROOT_ONLY` (Program.cs ×4, ToobaModuleComposition ×1) + allowed development-seed/migration invocation (`MarketplaceDevelopmentBootstrap` ×2, `DevelopmentSchemaMigrator` ×2 referencing Infrastructure/Development + Persistence); pinned by `HostPageCompositionAmcGuardTests` + `HostModuleEndpointOwnershipTests`.
20. **Schema-Migration-State**: `UNCHANGED` — no migration planned in any wave; 0 migration files touched.
21. **Behavior-Preservation-Risk**: `LOW` — all waves are structure/mechanism-only; routes, shapes, status codes, stable codes, validation semantics, authorization seam, persistence schema and seed behavior remain identical.
22. **Canonical-Reference-Used**: Inventory/Media/OperatorProfile `*Operation` typed-fault seam with declared-code `IsKnown` guard (OperatorProfile is the newest Architect-accepted AMSC W1 precedent); BuildingBlocks `Result`+`ApiResponseFactory`+`IErrorResourceSet` mechanisms; Notification/Inventory/OperatorProfile durable cert-guard shape.
23. **Final-Disposition**: `READY_TO_MIGRATE` (bounded, mechanism-completion only; no ownership moves required).

## Responsibility Map

| File | Responsibilities | Verdict |
|---|---|---|
| `Domain/Aggregates/PageDefinition.cs` (166) | DOMAIN_RULE (page aggregate + reorder/visibility/config/variant/restore invariants) | COHESIVE |
| `Domain/Aggregates/PageSection.cs` (66) | DOMAIN_RULE (section child entity) | COHESIVE |
| `Domain/Catalog/SectionCatalog.cs` (186) | DOMAIN_RULE (approved section-type catalog + config schema validation) | COHESIVE |
| `Domain/Constants/PageCompositionTenantIds.cs`, `PageKeys.cs` | DOMAIN_RULE constants | COHESIVE |
| `Infrastructure/Directories/PageCompositionDirectory.cs` (233) | PERSISTENCE (single port implementation; load/save + mapping + locale normalization) | COHESIVE |
| `Infrastructure/Persistence/PageCompositionDbContext.cs` | PERSISTENCE (own schema, own Outbox) | COHESIVE |
| `Infrastructure/Persistence/Migrations/*` (3 files) | PERSISTENCE migrations | COHESIVE (EF exemption) |
| `Infrastructure/Development/PageCompositionDevelopmentSeed.cs` | DEVELOPMENT_SEED | COHESIVE |
| `Infrastructure/PageCompositionModule.cs` (70) | PRESENTATION_COMPOSITION / module DI + Outbox registration | COHESIVE |
| `Application/Admin/Commands/AdminHomeCompositionCommands.cs` (5 request+handler pairs) | APPLICATION_USE_CASE | COHESIVE |
| `Application/Admin/Queries/AdminGetHomeCompositionQuery.cs` | APPLICATION_USE_CASE | COHESIVE |
| `Application/Storefront/Queries/HomeCompositionQueries.cs` (2 request+handler pairs) | APPLICATION_USE_CASE | COHESIVE |
| `Application/Admin/Validators/PageCompositionValidators.cs` (6 validators) | transport validation | COHESIVE |
| `Application/Storefront/Validators/StorefrontCompositionValidators.cs` (2 validators) | transport validation | COHESIVE |
| `Application/Composition/PageCompositionOperation.cs` (32) | typed-fault seam | COHESIVE (mechanism gap below) |
| `Application/Composition/PageCompositionPresentationComposer.cs` (85) | APPLICATION_USE_CASE orchestration (thin pass-through + tenant resolution) | COHESIVE |
| `Application/Models/PageCompositionModels.cs` (8 records: snapshots + 2 internal command inputs) | application read/write models | COHESIVE (no duplicate CQRS shape: `AddHomeSectionCommand`/`UpdateHomeSectionCommand` are nested input payloads carried by the authoritative MediatR requests, not parallel request types) |
| `Application/Ports/IPageCompositionDirectory.cs` | persistence port (Application-owned; no foreign consumer) | COHESIVE |
| `Contracts/Errors/*.cs` (3 files) | CONTRACT stable codes + catalog + resources | COHESIVE |
| `Endpoints/PageCompositionEndpointModule.cs` (22) | HTTP composition | COHESIVE |
| `Endpoints/Admin/PageCompositionAdminEndpoints.cs` (165) | HTTP_ENDPOINT (7 routes, transport-only) | COHESIVE |
| `Endpoints/Storefront/PageCompositionStorefrontEndpoints.cs` (39) | HTTP_ENDPOINT (1 route, transport-only) | COHESIVE |
| `Endpoints/Admin/IPageCompositionAdminAuthorizer.cs` (interface + impl) | AUTHORIZATION_ADAPTER (module-owned) | COHESIVE |
| `Endpoints/Models/PageCompositionHttpModels.cs` | HTTP DTO bodies | COHESIVE |

No `MUST_SPLIT` decisions. No generic/mixed `*Contracts.cs` dump inside Application.

## Findings requiring W1 repair (migration plan)

1. **Fault-typing gap (MIXED_FAULT_MECHANISM)**: the module raises `SemanticException` (Domain aggregate + `RequireTenantId`) and the composition seam maps only `SemanticException`. The canonical certified seam (Inventory/Media W1 precedent, carried into the Architect-accepted OperatorProfile AMSC W1) maps **both** typed mechanisms through a declared-code `IsKnown` filter so that a foreign code surfacing through the seam propagates untouched to the global boundary. W1: add `IsKnown` to `PageCompositionErrorCodes` (8-code `KnownCodes` set), add the `ContractOperationException` catch with `IsKnown` filter to `PageCompositionOperation.ExecuteAsync<T>` (and add the value-less `ExecuteAsync(Func<Task>)` overload mirroring the accepted OperatorProfile shape), preserving the existing `SemanticException` mapping exactly. The existing sync `Execute<T>` overload stays (used by both endpoints for tenant resolution).
2. **No logging repair required**: the module has zero log call sites (verified by grep over all 30 production files), so the OperatorProfile W1 structured-log-code alignment has no counterpart here.

## Verification plan (this wave)

- JSON parse SoT + manifest; manifest module count and OperatorProfile/PageComposition certified truth intact (no repo-global regression this time — manifest already carries the single 25-module array repaired by OperatorProfile W0).
- No production/manifest/SoT edit in W0 beyond adding the `pageCompositionAmsc001W0` SoT block, Master Recovery checkpoint and this evidence file.
- Focused guard family untouched and green at HEAD.

## Structure-Handoff-State

`REQUIRED` — W2 will re-verify capability-first shallow structure, root allowlists, `.slnx` grouping and path↔namespace equality under the current structure skill; current tree appears `PROFESSIONAL_SHALLOW` (Application `Admin/{Commands,Queries,Validators}` + `Storefront/{Queries,Validators}` + `Composition/Models/Ports`; Domain `Aggregates/Catalog/Constants`; Infrastructure `Directories/Development/Persistence` with migrations under `Persistence/Migrations`; Endpoints root composition + `Admin/` + `Storefront/` + `Models/`; Contracts `Errors/Resources`), but final structural proof belongs to W2.

## Certification blockers (to close by W3)

- `IsKnown` + dual-mechanism `PageCompositionOperation` seam (W1).
- Durable AMSC W2 structure guard under the current structure skill (W2).
- Durable AMSC W3 cert guard + manifest/SoT promotion re-affirming the AMC-001 W4 state under the AMSC pipeline id (W3).
- SoT `pageCompositionAmsc001W0..W3` lineage blocks with per-wave commit SHAs (W3/R1).
