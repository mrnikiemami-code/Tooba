# TB-TMAR-OPERATORPROFILE-AMSC-001-W0 — Analyze (tooba-architecture-analyze)

## Scope

`src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.*` — full AMSC re-standardization under `ARCH-COMPLETE-002` (W0 Analyze → W1 Migrate → W2 Structure → W3 Certify), starting head `fa5bc0e9` on `main` (`HEAD == origin/main`, known/safe tree).

## Baseline correction applied this wave (durable-guard prerequisite)

The AMSC W0 audit reproduced a **repository-global manifest regression** first introduced by `TB-TMAR-NOTIFICATION-AMSC-001-W3` (commit `763dab13`): `docs/architecture/tmar-module-structure-manifests.json` carried **two top-level `"modules"` keys**. JSON last-key-wins parsing shadowed the canonical 24-module certified array with a Notification-only 1-module array, hiding every other certified module (including OperatorProfile) from all manifest readers. This broke the durable guard `OperatorProfileModuleAmcW4CertGuardTests.OperatorProfile_is_manifest_structure_certified_under_modules_operatorprofile` (`Sequence contains no matching element`) and `TmarCompleteReferenceStructureGateTests.Manifest_is_well_formed_and_only_declared_modules_are_certified` on clean HEAD.

W0 repaired the manifest to a **single merged `"modules"` array with 25 certified modules** (the original 24 + Notification with its reconciled W3-R3 certification note preserved verbatim), keeping `uncertifiedHttpOwningModules` (Returns, Support, Wallet, Promotion) and `preCertModules` (ProductWorkspace) intact. The stale frozen expected-module lists in `TmarCompleteReferenceStructureGateTests` were reconciled to include `Notification` (certified by the accepted W3-R3 lineage). No structural field, allowlist, forbidden list or certification verdict of any other module changed.

## Structured State Fields

1. **Foundation-State**: `FOUNDATION_READY` — 5 production projects (Contracts/Domain/Application/Infrastructure/Endpoints) exist, grouped under `/Modules/OperatorProfile/` in `Tooba.slnx`; module has a prior AMC-001 W4 certification (`operatorProfileAmc001` SoT block, `COMPLETE_REFERENCE_PATTERN`, `manifestCertified: true`).
2. **Ownership-State**: `correct` — operator self descriptive profile (display name / first / last / bio) is OperatorProfile-owned; login identifiers stay in Identity; global platform settings stay platform-owned. `OperatorProfile.cs` aggregate documents this separation.
3. **File-Cohesion-State**: `COHESIVE` — largest production file `Domain/Aggregates/OperatorProfile.cs` = 136 LOC (single aggregate with field invariants); 25 production files total, largest others: `OperatorProfileDirectory.cs` 101 LOC (single persistence seam), `OperatorProfileDbContext.cs` 57 LOC, endpoints 53 LOC. No ARCH-SIZE-001 baseline entry required (all far below the 800 LOC ceiling).
4. **Oversized/God-File-State**: `NONE` — no file above ceiling, no multi-responsibility file found.
5. **Localization-State**: `CANONICAL` — `Contracts/Errors/OperatorProfileErrorResourceSet.cs` (`IErrorResourceSet` owning the `operator.profile.` keyspace) + bilingual `Contracts/Resources/OperatorProfileErrors.resx` / `.fa.resx` (6 keys × 2 cultures); no hard-coded user-facing API text in production C# (Persian appears only in doc-comments and Development seed demo content — no API message contract).
6. **API-Result-Pattern-State**: `CANONICAL` — endpoints inject `ApiResponseFactory api` and return `api.From(Result<T>)`; zero `Results.Json/BadRequest/Problem` in module code.
7. **Stable-Error-Code-State**: `CATALOGUED_6_OF_6` + **one gap**: `OperatorProfileErrorCodes.cs` declares 6 codes consumed by the catalog contributor, but lacks the canonical `IsKnown(string?)` declared-code guard that certified Media/Inventory/Notification seams use; see blockers.
8. **Logging-State**: `CANONICAL` — `ILogger<T>` with structured event names (`operator.profile.upsert.succeeded` / `.failed`, `operator.profile.get.*`); no Console/Debug writers; no duplicate telemetry. Minor inconsistency: failure logs emit the event literal while Media's precedent emits the stable code through a template placeholder (`{MediaUploadEvent}`); no sensitive data logged.
9. **Sensitive-Logging-State**: `NONE` — no credentials/tokens/OTP/secrets in any log call.
10. **OpenTelemetry-State**: `CANONICAL` — no second ActivitySource/Meter; module adds no custom tracing. (No cross-module call sites exist yet, so `IModuleCallTracer` decoration is not currently applicable; the single foreign consumer seam is inbound via `IActorDisplayLookup`.)
11. **Correlation-Trace-State**: `CANONICAL` — no parallel correlation mechanism, no manual traceparent parsing; ProblemDetails trace/correlation flows through the BuildingBlocks `ProblemDetailsContextProvider`.
12. **CQRS-State**: `COMPLIANT` — 2 endpoint-reachable MediatR requests (`GetOperatorProfileQuery`, `UpsertOperatorProfileCommand`), real `IRequestHandler<,>` implementations, thin endpoints dispatch through `ISender`; MediatR registered via `AddToobaCqrsFoundation` (12.5.0) with the module assembly passed in `Program.cs` (line 194).
13. **Validator-Coverage-State**: `EXHAUSTIVE_2_OF_2_REQUIRED_PRESENT` — `GetOperatorProfileQueryValidator` (ActorUserId transport shape) + `UpsertOperatorProfileCommandValidator` (ActorUserId, DisplayName min/max, optional FirstName/LastName/Bio max lengths); 0 `NO_VALIDATOR_REQUIRED`; discovery via `AddValidatorsFromAssembly` in the CQRS foundation; validators emit stable `operator.profile.validation.*` codes only.
14. **Contracts-Boundary-State**: `CLEAN` — `Contracts` carries only `Ports/ActorDisplayContracts.cs` (true module-boundary port + projection consumed by Order.Application, AccessControl.Application, Catalog.Endpoints, ProductWorkspace.Endpoints), `Errors/` (codes, catalog contributor, resource set) and `Resources/` (resx pair). No Application-internal type leaked into Contracts.
15. **Cross-Module-Coupling-State**: `LEGAL_CONTRACTS_ONLY` — project references are exclusively self-module + BuildingBlocks/ModuleContracts/Persistence foundation; grep proves **zero** foreign `*.Application|Infrastructure|Domain|Endpoints` references in any OperatorProfile project or source file. Foreign consumers reach OperatorProfile only through `Tooba.OperatorProfile.Contracts` (`IActorDisplayLookup`), registered by `OperatorProfileModule` as the `ActorDisplayLookupAdapter`.
16. **Cross-Module-Join-State**: `NONE` — single `OperatorProfileDbContext` touching only the `operator_profile` schema (+ own Outbox table); no LINQ/SQL join to any foreign table, no foreign DbSet, no navigation crossing modules.
17. **Persistence-Ownership-State**: `CORRECT` — own schema `operator_profile`, own `OperatorProfileDbContext`, own design-time factory, own migration `20260827215300_InitialOperatorProfile`, own `IOutboxModuleRegistration`, registered in `Tooba.MigrationRunner` ModuleMigrationRegistry and `ModuleSchemaMigrationOrder.OperatorProfile`.
18. **Endpoint-Ownership-State**: `MODULE_OWNED` — routes `GET /v1/admin/operator/profile` and `PUT /v1/admin/operator/profile` mapped by `OperatorProfileEndpointModule.MapOperatorProfileModuleEndpoints`; Host `OperatorProfile` folder = ZERO (HOST_ZERO closed by `TB-TMAR-HOST-OPERATORPROFILE-AMC-001-R1`); Host retains only the thin security adapter `HostOperatorProfileAdminAuthorizer` (implements module-owned `IOperatorProfileAdminAuthorizer`), composition-root registration, dev-seed call in `SettingsFoundationDevelopmentSeedHost` and the migration descriptor.
19. **Host-Residue-State**: `ALLOWED_COMPOSITION_ROOT_ONLY` (Program.cs usings/registration/map ×4) + allowed thin authorizer + allowed development-seed invocation; pinned by `HostOperatorProfileAmcGuardTests`.
20. **Schema-Migration-State**: `UNCHANGED` — no migration planned in any wave; 0 migration files touched.
21. **Behavior-Preservation-Risk**: `LOW` — all waves are structure/mechanism-only; routes, shapes, status codes, stable codes, validation semantics, authorization seam, persistence schema and seed behavior remain identical.
22. **Canonical-Reference-Used**: Inventory/Media `*Operation` typed-fault seam with declared-code `IsKnown` guard (InventoryErrorCodes/MediaErrorCodes precedent); Media structured log-code precedent; Notification/Inventory durable cert-guard shape; Offer/BuildingBlocks canonical result/localization/observability mechanisms.
23. **Final-Disposition**: `READY_TO_MIGRATE` (bounded, mechanism-completion only; no ownership moves required).

## Responsibility Map

| File | Responsibilities | Verdict |
|---|---|---|
| `Domain/Aggregates/OperatorProfile.cs` (136) | DOMAIN_RULE | COHESIVE |
| `Infrastructure/Profiles/OperatorProfileDirectory.cs` (101) | PERSISTENCE | COHESIVE |
| `Infrastructure/Persistence/OperatorProfileDbContext.cs` (57) | PERSISTENCE | COHESIVE |
| `Infrastructure/Persistence/OperatorProfileOutboxRegistration.cs` (27) | PERSISTENCE / integration seam | COHESIVE |
| `Infrastructure/Adapters/ActorDisplayLookupAdapter.cs` (32) | INTEGRATION_ADAPTER (Contracts port exposure) | COHESIVE |
| `Infrastructure/Development/OperatorProfileDevelopmentSeed.cs` (44) | DEVELOPMENT_SEED | COHESIVE |
| `Infrastructure/OperatorProfileModule.cs` (46) | PRESENTATION_COMPOSITION / module DI | COHESIVE |
| `Application/Admin/Commands/UpsertOperatorProfileCommand.cs` (47) | APPLICATION_USE_CASE (request+handler) | COHESIVE |
| `Application/Admin/Queries/GetOperatorProfileQuery.cs` (41) | APPLICATION_USE_CASE (request+handler) | COHESIVE |
| `Application/Admin/Validators/*.cs` (15+33) | transport validation | COHESIVE |
| `Application/Composition/OperatorProfileOperation.cs` (24) | typed-fault seam | COHESIVE (mechanism gap below) |
| `Application/Models/*.cs` (17+23) | application read/write models | COHESIVE |
| `Application/Ports/IOperatorProfileDirectory.cs` (26) | persistence port | COHESIVE |
| `Contracts/Ports/ActorDisplayContracts.cs` (29) | CONTRACT (module boundary) | COHESIVE |
| `Contracts/Errors/*.cs` (23+28+25) | CONTRACT stable codes + catalog + resources | COHESIVE |
| `Endpoints/OperatorProfileEndpointModule.cs` (28) | HTTP composition | COHESIVE |
| `Endpoints/Admin/OperatorProfileAdminEndpoints.cs` (53) | HTTP_ENDPOINT (request+2 handlers) | COHESIVE |
| `Endpoints/Admin/IOperatorProfileAdminAuthorizer.cs` (10) | AUTHORIZATION_ADAPTER contract | COHESIVE |

No `MUST_SPLIT` decisions. No generic/mixed `*Contracts.cs` dump inside Application.

## Findings requiring W1 repair (migration plan)

1. **Fault-typing gap (MIXED_FAULT_MECHANISM)**: the module raises `SemanticException` (Domain aggregate + directory guard) but never `ContractOperationException`; the composition seam maps only `SemanticException`. The canonical certified seam (Inventory/Media W1 precedent) maps **both** typed mechanisms through a declared-code `IsKnown` filter so that a foreign module's codes surfacing through the seam propagate untouched to the global boundary. W1: add `IsKnown` to `OperatorProfileErrorCodes`, add the `ContractOperationException` catch with `IsKnown` filter and the value-less `ExecuteAsync` overload to `OperatorProfileOperation` (mirroring the certified InventoryOperation shape), preserving the existing `SemanticException` mapping exactly.
2. **Structured-log consistency (minor)**: align handler failure logs with the Media precedent — `logger.LogInformation("{OperatorProfileEvent}", OperatorProfileErrorCodes.X)` instead of a bare literal — so the stable code is machine-greppable in structured log fields. Success events unchanged.

## Verification plan (this wave)

- `dotnet test Host.Tests --filter OperatorProfileModuleAmc|HostOperatorProfileAmc|TmarCompleteReferenceStructureGate` → all OperatorProfile + manifest-well-formedness guards PASS (Catalog Contracts namespace debt remains the single disclosed pre-existing red, module-local scope, Catalog-owned).
- Manifest JSON parse: single `modules` array, 25 certified modules, sibling keys intact.

## Structure-Handoff-State

`REQUIRED` — W2 will re-verify capability-first shallow structure, root allowlists, `.slnx` grouping and path↔namespace equality under the current structure skill; current evidence shows the tree already `PROFESSIONAL_SHALLOW` (Application `Admin/{Commands,Queries,Validators}` + `Composition/Models/Ports`; Domain `Aggregates`; Infrastructure `Profiles/Adapters/Development/Persistence` with migrations under `Persistence/Migrations`; Endpoints root composition + `Admin/`; Contracts `Ports/Errors/Resources`).

## Certification blockers (to close by W3)

- `IsKnown` + dual-mechanism `OperatorProfileOperation` seam (W1).
- Durable AMSC W3 cert guard + manifest/SoT promotion confirming the AMC-001 W4 state under the AMSC pipeline id (W3).
- SoT `operatorProfileAmsc001` lineage block with per-wave commit SHAs (W3/R1).
