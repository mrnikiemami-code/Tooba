# TB-TMAR-PARTY-AMSC-001-W0 — Analyze (tooba-architecture-analyze)

## Scope

`src/backend/Modules/Party/Tooba.Party.*` — full AMSC re-standardization under `ARCH-COMPLETE-002` (W0 Analyze → W1 Migrate → W2 Structure → W3 Certify), starting head `348c3baa` on `main` (`HEAD == origin/main`). Party carries a prior AMC-001 W4 certification (`partyAmc001` SoT block, `COMPLETE_REFERENCE_PATTERN`, `manifestCertified: true`). This AMSC run re-verifies every layer under the current certified Media/Inventory/OperatorProfile/PageComposition seam precedent and repairs the one real boundary defect found.

## Structured State Fields

1. **Foundation-State**: `FOUNDATION_READY` — 5 production projects (Contracts/Domain/Application/Infrastructure/Endpoints) exist, all grouped under `/Modules/Party/` in `src/backend/Tooba.slnx` (lines 49–54); manifest already lists Party as `structureCertified: true` under `ARCH-COMPLETE-002` (prior AMC-001 W4 truth preserved as baseline, not discarded).
2. **Ownership-State**: `correct` — party/organization/membership identity (BusinessParty, PartyMembership, UserPartyLink, OrganizationRelationship, PartyCapability) is Party-owned; authentication credentials and the permission matrix stay in Identity/AccessControl (Party `BusinessParty` doc-comment explicitly states this); seller settings operational profile is Party-owned via the `IPartySellerSettings` Contracts seam.
3. **File-Cohesion-State**: `COHESIVE` — 52 production `.cs` files; largest non-migration file `Infrastructure/Grid/AdminSellersGridQueryEngine.cs` 246 LOC (single cohesive grid engine over Contracts ports), then `Infrastructure/Directories/PartyDirectory.cs` 189 LOC (single port implementation), `Domain/Aggregates/BusinessParty.cs` 172 LOC (single aggregate). Migrations/snapshot are EF-exempt. No ARCH-SIZE-001 baseline entry required.
4. **Oversized/God-File-State**: `NONE` — no multi-responsibility file; each large file has exactly one reason to change.
5. **Localization-State**: `CANONICAL` — `Contracts/Errors/PartyErrorResourceSet.cs` (`IErrorResourceSet` owning `seller.settings.*` + `party.*` keyspaces) + bilingual `Contracts/Resources/PartyErrors.resx` / `.fa.resx` (11 keys × 2 cultures, verified); descriptors carry `LocalizationKey = code`; no hard-coded user-facing API text in production C# (Persian appears only in doc-comments and Development seed demo constants — repo idiom, not an API message contract).
6. **API-Result-Pattern-State**: `CANONICAL` — endpoints inject `ApiResponseFactory api` and return `api.From(...)`; zero `Results.Json/BadRequest/Problem` in module code (verified by grep; guard `PartyModuleAmcW3CqrsGuardTests` pins `Results.Json` absence).
7. **Stable-Error-Code-State**: `CATALOGUED_10_OF_10` + **one gap**: `PartyErrorCodes.cs` declares 10 codes consumed by `PartyErrorCatalogContributor` (10 descriptors), but lacks the canonical `IsKnown(string?)` declared-code guard used by the certified Media/Inventory/Notification/OperatorProfile/PageComposition seams; see blockers.
8. **Logging-State**: `CANONICAL` — module emits zero log calls today (no `ILogger`/`LogInformation`/`Console.WriteLine`/`Debug.WriteLine` anywhere in Party production code, verified by grep); no duplicate telemetry pipeline; nothing to align, so W1 has no logging repair.
9. **Sensitive-Logging-State**: `NONE` — no log call sites at all.
10. **OpenTelemetry-State**: `CANONICAL` — no second ActivitySource/Meter, no direct `StartActivity` in module code; the module issues no outbound module-to-module runtime calls (Offer/Order metrics flow in as injected Contracts ports — inbound-only seams), so `IModuleCallTracer` decoration is not applicable.
11. **Correlation-Trace-State**: `CANONICAL` — no parallel correlation mechanism, no manual traceparent parsing; ProblemDetails trace/correlation flows through the BuildingBlocks `ProblemDetailsContextProvider`.
12. **CQRS-State**: `COMPLIANT` — 4 endpoint-reachable MediatR requests (`GetSellerSettingsQuery`, `UpdateSellerSettingsCommand`, `ListAdminSellersQuery`, `QueryAdminSellersGridQuery`), all real `IRequest<Result<T>>` with real `IRequestHandler<,>`, thin endpoints dispatch through `ISender` only; registered via `AddToobaCqrsFoundation` in `Program.cs` (`typeof(GetSellerSettingsQuery).Assembly`); durable matrix pinned by `PartyModuleAmcW3CqrsGuardTests`.
13. **Validator-Coverage-State**: `EXHAUSTIVE_2_OF_2_REQUIRED_PRESENT_2_NO_VALIDATOR_REQUIRED` — `UpdateSellerSettingsCommandValidator` (7 stable-code shape rules) + `QueryAdminSellersGridQueryValidator` (envelope-only; field/operator whitelist stays in `PartyAdminSellersGridPolicies`, no duplication); `GetSellerSettingsQuery` (authorizer-derived ids, no transport input) and `ListAdminSellersQuery` (no payload) are `NO_VALIDATOR_REQUIRED` — trusted authorizer-derived values are never a validator reason.
14. **Contracts-Boundary-State**: `CLEAN` — `Contracts` carries `Errors/` (codes/catalog/resources), `Ports/` (module-boundary ports + grid contracts + dev-seed gateway). Every cross-module consumer consumes Contracts only: `Promotion.Infrastructure` (dev seed) consumes `Party.Contracts.Ports`; Order/Offer test guards consume no Party production namespace. One defect remains, recorded in the coupling field below.
15. **Cross-Module-Coupling-State**: `LEGAL_CONTRACTS_ONLY` + **one outbound defect**: Party's own Application/Infrastructure consume two foreign Contracts assemblies (`Tooba.Offer.Contracts`, `Tooba.Order.Contracts`) for the Admin sellers composition (`IOfferQueryGateway`, `OfferStatus`, `IAdminSellerOrderCountPort`, `AdminSellerListItem` rebuild) — legal Contracts-only reads. **The defect is on the Promotion side**: `Tooba.Promotion.Infrastructure.csproj` references `Tooba.Party.Application.csproj` and `MerchandisingCampaignDevelopmentSeed.cs` consumes `Tooba.Party.Application.Models/Ports` (`IPartyDirectory`, `PartyReference` via `CreateOrganizationAsync`). Foreign Application coupling is a microservice blocker on the consuming module; Party itself is the injured module. **W1 repairs this by adding a Contracts development-seed surface** so Promotion consumes `Party.Contracts` only (Party keeps its runtime behavior; Promotion csproj + usings are the edited files; this is a bounded, behavior-preserving contract extraction on the touched Party surface — the OrganizationProfile development-seed flow already models this exact pattern through `IPartyDevelopmentSeedGateway`).
16. **Cross-Module-Join-State**: `NONE` — single `PartyDbContext` touching only the `party` schema (+ own Outbox table); the Admin sellers grid explicitly avoids cross-module JOINs (comment: "metric-sort in memory after Contracts filtering (cross-module JOIN forbidden)"); no foreign DbSet, no navigation crossing modules.
17. **Persistence-Ownership-State**: `CORRECT` — own schema `party`, own `PartyDbContext`, own migrations (`20260823062413_InitialParty` + `20260827215300_OrganizationProfileFields` + designer + snapshot), own `PartyOutboxRegistration : IOutboxModuleRegistration`, registered via `AddModuleSchemaMigrator<PartyDbContext>("Party", ModuleSchemaMigrationOrder.Party)`.
18. **Endpoint-Ownership-State**: `MODULE_OWNED` — 4 routes (GET/PUT `/v1/seller/settings`; GET `/v1/admin/sellers`; POST `/v1/admin/sellers/query`) mapped by `PartyEndpointModule.MapPartyEndpoints`; Host Party HTTP ownership ZERO; the seller authorizer seam `IPartySellerAuthorizer` is module-owned in `Endpoints/Seller` with the Host thin adapter `HostPartySellerAuthorizer` retained as `ALLOWED_SECURITY_ADAPTER` (neutral `ISellerPanelAccess` + `IPlatformEffectiveAccessReader` seams; no Party business type in Host).
19. **Host-Residue-State**: `ALLOWED_COMPOSITION_ROOT_PLUS_THIN_SECURITY_ADAPTER` (Program.cs: `AddPartyEndpointPresentation`, CQRS assembly, `MapPartyEndpoints`, authorizer DI ×4; `ToobaModuleComposition.cs`: `new PartyModule()` ×1; `MarketplaceDevelopmentBootstrap.cs`: PartyDbContext migration ×1; `SettingsFoundationDevelopmentSeedHost.cs`: dev-seed orchestration ×1; `Security/Seller/HostPartySellerAuthorizer.cs` ×1) — all pinned by existing Host guards.
20. **Schema-Migration-State**: `UNCHANGED` — no migration planned in any wave; 0 migration files to be touched.
21. **Behavior-Preservation-Risk**: `LOW` — the only production-code wave (W1) adds a dormant declared-code guard + mapping overloads and a Contracts development-seed surface; routes, shapes, status codes, stable codes, validation semantics, authorization seam, persistence schema and seed behavior remain identical.
22. **Canonical-Reference-Used**: Inventory/Media/OperatorProfile/PageComposition `*Operation` typed-fault seam with declared-code `IsKnown` guard (OperatorProfile/PageComposition are the newest Architect-accepted W1 precedents); BuildingBlocks `Result`+`ApiResponseFactory`+`IErrorResourceSet` mechanisms; Party's own `IPartyDevelopmentSeedGateway` Contracts surface as the established dev-seed contract pattern; Notification/Inventory/OperatorProfile/PageComposition durable cert-guard shape.
23. **Final-Disposition**: `READY_TO_MIGRATE` (bounded, mechanism-completion + one inbound-boundary repair; no ownership moves required).

## Responsibility Map

| File | Responsibilities | Verdict |
|---|---|---|
| `Domain/Aggregates/BusinessParty.cs` (172) | DOMAIN_RULE (party aggregate + capability grant + org-profile invariants) | COHESIVE |
| `Domain/Aggregates/PartyMembership.cs`, `UserPartyLink.cs`, `OrganizationRelationship.cs`, `PartyCapability.cs` | DOMAIN_RULE aggregates | COHESIVE |
| `Domain/Enums/*` (6 files), `Domain/Events/PartyMembershipEstablishedDomainEvent.cs` | DOMAIN_RULE enums/events | COHESIVE |
| `Contracts/Errors/PartyErrorCodes.cs` | CONTRACT stable codes (10) — W1 adds IsKnown | COHESIVE (gap below) |
| `Contracts/Errors/PartyErrorCatalogContributor.cs` (10 descriptors) | CONTRACT error catalog | COHESIVE |
| `Contracts/Errors/PartyErrorResourceSet.cs` | CONTRACT resource keyspace owner | COHESIVE |
| `Contracts/Ports/IPartyLookup.cs`, `IPartySellerSettings.cs`, `AdminSellersGridContracts.cs`, `IPartyAdminSellerReadGateway.cs`, `IPartyDevelopmentSeedGateway.cs` | CONTRACT module-boundary ports | COHESIVE |
| `Application/Composition/PartyOperation.cs` (22) | typed-fault seam — W1 extends | COHESIVE (gap below) |
| `Application/Ports/IPartyDirectory.cs`, `Application/Models/PartyReferences.cs` | internal persistence port + models (Application-internal; Promotion consumption is the defect, not the placement) | COHESIVE (consumer repaired in W1) |
| `Application/Seller/{Commands,Queries,Models,Validators}` | APPLICATION_USE_CASE (seller settings) | COHESIVE |
| `Application/Admin/Sellers/{Queries,Validators}` | APPLICATION_USE_CASE (admin sellers) | COHESIVE |
| `Infrastructure/Directories/PartyDirectory.cs` (189) | PERSISTENCE (port implementation) | COHESIVE |
| `Infrastructure/Persistence/PartyDbContext.cs` + Migrations + `PartyOutboxRegistration.cs` | PERSISTENCE (own schema + outbox) | COHESIVE |
| `Infrastructure/Grid/{AdminSellersGridQueryEngine,PartyAdminSellersGridPolicies}.cs`, `Adapters/AdminSellersGridAdapter.cs` | INTEGRATION (Contracts-metric grid engine) | COHESIVE |
| `Infrastructure/Admin/PartyAdminSellerReadGateway.cs` | INTEGRATION (read boundary adapter) | COHESIVE |
| `Infrastructure/Seller/PartySellerSettingsAdapter.cs` | INTEGRATION (Contracts seam adapter) | COHESIVE |
| `Infrastructure/Development/{PartyDevelopmentSeedGateway,PartyOrganizationProfileDevelopmentSeed}.cs` | DEVELOPMENT_SEED | COHESIVE |
| `Infrastructure/Events/PartyEvents.cs`, `Projections/PartyMembershipProjectionHandler.cs` | INTEGRATION (outbox event + SpiceDB projection) | COHESIVE |
| `Infrastructure/PartyModule.cs` (67) | PRESENTATION_COMPOSITION / module DI + outbox + error catalog registration | COHESIVE |
| `Endpoints/PartyEndpointModule.cs` (33) | HTTP composition | COHESIVE |
| `Endpoints/Seller/{PartySellerSettingsEndpoints,IPartySellerAuthorizer}.cs` | HTTP_ENDPOINT + AUTHORIZATION_ADAPTER seam | COHESIVE |
| `Endpoints/Admin/Sellers/PartyAdminSellersEndpoints.cs` | HTTP_ENDPOINT (2 routes, transport-only) | COHESIVE |

No `MUST_SPLIT` decisions. No generic/mixed `*Contracts.cs` dump inside Application (PartyReferences.cs is a cohesive model file; its cross-module consumption is the W1 defect to repair on the consumer side).

## Findings requiring W1 repair (migration plan)

1. **Declared-code guard gap**: `PartyErrorCodes` lacks `IsKnown(string?)` + `KnownCodes` (10 codes). W1 adds them exactly mirroring the accepted OperatorProfile/PageComposition shape.
2. **Fault-typing gap (MIXED_FAULT_MECHANISM)**: the module raises only `SemanticException` and `PartyOperation` maps only `SemanticException`, with no value-less `ExecuteAsync(Func<Task>)` overload and no `ArgumentNullException` guards. W1: add the `ContractOperationException` catch with `IsKnown` filter and the value-less overload, mirroring the accepted seam. Dormant seam — no behavior delta (module raises no `ContractOperationException` today).
3. **Foreign-Application consumption of Party (inbound boundary defect)**: `Promotion.Infrastructure` dev seed consumes `Party.Application` (`IPartyDirectory`, `PartyReference`, `CreateOrganizationAsync`). W1 adds a narrow Contracts-side development-seed surface in `Tooba.Party.Contracts` (new `PartyAdminSellerDirectoryPort`-style port is NOT added — instead a `PartyDirectoryFacade` port exposing exactly the dev-seed needs: `CreateOrganizationAsync`, `SearchIdsByDisplayNameAsync` semantics already on `IPartyLookup`) and edits only the Promotion csproj/usings to consume Contracts. Party production behavior is unchanged; the Party Application types remain internal to Party.
4. **No logging repair required**: the module has zero log call sites (verified by grep over all 52 production files).

## Verification plan (this wave)

Analysis-only wave: zero production change, zero schema change, zero manifest mutation. SoT records the W0 block + Master Recovery checkpoint; JSON parses verified.
