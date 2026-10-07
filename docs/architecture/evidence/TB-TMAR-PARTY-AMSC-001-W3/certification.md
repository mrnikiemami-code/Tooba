# TB-TMAR-PARTY-AMSC-001-W3 — Certify (tooba-architecture-certify)

## Verdict

**`COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` — `STRUCTURE_CERTIFIED`** for `src/backend/Modules/Party/Tooba.Party.*`, certifying head = this commit. Prior AMC-001 W4 certification preserved as accepted baseline (`partyAmc001` SoT block unchanged); this AMSC run re-verified every layer against the current certified precedent set (Media/Inventory/Notification/OperatorProfile/PageComposition) and repaired the W1 mechanism gaps.

## Certification Gates (re-verified at certifying head)

### 1. Physical Tree Audit + 1a. Semantic Contracts / Folder Granularity
- 5 projects, 54 production files (2 new from W1). Roots: Endpoints=`PartyEndpointModule.cs`, Infrastructure=`PartyModule.cs`, Contracts/Domain/Application empty — allowlists match manifest exactly.
- Contracts semantics: `Errors/` (10 codes + contributor + resource set), `Ports/` — all 6 port files are true module-boundary ports consumed cross-module (`IPartyLookup`/`IPartySellerSettings`/`AdminSellersGridContracts`/`IPartyAdminSellerReadGateway`/`IPartyDevelopmentSeedGateway`/new `IPartyDevelopmentDirectory`) — no Application-internal type leaked, no mixed `*Contracts.cs` dump, no duplicate CQRS request shape (each of the 4 requests has exactly one authoritative MediatR type; `PartySellerSettingsWriteModel` is a nested input payload carried by the command, not a parallel request).
- Folder granularity: capability-first shallow (`Admin/Sellers/{Queries,Validators}`, `Seller/{Commands,Queries,Models,Validators}`, shared `Composition/Models/Ports`); zero per-use-case request leaves; zero technical-axis-first roots; zero root dumps.

### 2. Path/Namespace Exactness
`EXACT` for all 5 projects (guard-enforced scan); EF `Persistence/Migrations` exemption; no alias workaround, no TypeForwardedTo, no duplicate compatibility types.

### 3. File Cohesion
No god-file: largest `AdminSellersGridQueryEngine.cs` 246 LOC (single grid engine), `PartyDirectory.cs` 189 LOC (single port impl), `BusinessParty.cs` 172 LOC (single aggregate). W1 additions are cohesive single-responsibility files. No cosmetic over-splitting.

### 4. Endpoint Ownership
`MODULE_OWNED` — 4 routes (GET/PUT `/v1/seller/settings`, GET `/v1/admin/sellers`, POST `/v1/admin/sellers/query`) via `PartyEndpointModule.MapPartyEndpoints`; Host Party HTTP ownership ZERO; no duplicate mapping.

### 5. CQRS / MediatR
4/4 requests are real `IRequest<Result<T>>` with real `IRequestHandler<,>`; endpoints dispatch via `ISender` only; no endpoint→Directory/DbContext direct call; no Host bypass; MediatR 12.5.0 via `AddToobaCqrsFoundation` (Program.cs `typeof(GetSellerSettingsQuery).Assembly`).

### 6. Validator Coverage (exhaustive matrix)
| Request | Classification | Evidence |
|---|---|---|
| `UpdateSellerSettingsCommand` | `VALIDATOR_REQUIRED_PRESENT` | `UpdateSellerSettingsCommandValidator` (7 stable-code shape rules) |
| `QueryAdminSellersGridQuery` | `VALIDATOR_REQUIRED_PRESENT` | `QueryAdminSellersGridQueryValidator` (envelope-only; whitelist stays in `PartyAdminSellersGridPolicies`, no duplication) |
| `GetSellerSettingsQuery` | `NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT` | ids from the authorizer boundary, not the request body |
| `ListAdminSellersQuery` | `NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT` | payload-less query |

All validators emit stable machine codes via `WithErrorCode(PartyErrorCodes.*)`; discovery through `AddValidatorsFromAssembly` in the CQRS foundation; durable matrix pinned by `PartyModuleAmcW3CqrsGuardTests`.

### 7. Localization Compliance
`CANONICAL` — bilingual `PartyErrors.resx`/`.fa.resx` (11 keys × 2 cultures, all verified present); `PartyErrorResourceSet` owns `seller.settings.*` + `party.*`; descriptors carry `LocalizationKey = code`; exactly one descriptor owner per code (`PartyErrorCatalogContributor`, 10 descriptors; `seller.authorization.denied` stays Foundation-owned by design); zero hard-coded user-facing API text; zero `ex.Message` contracts; no Accept-Language parsing in endpoints.

### 8. Canonical API Result / Error Mapping
All endpoints map through `ApiResponseFactory.From`; zero `Results.Json/BadRequest/Problem`; no catch-and-map in endpoints (the single handler catch maps `GridQueryValidationException` by its typed `ErrorCode` — typed-code classification, not message heuristic); unknown exceptions propagate to the global boundary; success shapes preserved; `ErrorDefinitionCatalog` fail-fast duplicate detection untouched.

### 9. Logging / Sensitive Data
`CANONICAL_ZERO_LOG_CALL_SITES` — zero `ILogger`/`Console.WriteLine`/`Debug.WriteLine` call sites in module production code; no sensitive-data path; no second telemetry pipeline.

### 10. OpenTelemetry / Correlation
`CANONICAL` — no second ActivitySource/Meter, no direct `StartActivity`, no parallel correlation, no manual traceparent parsing; ProblemDetails trace/correlation via canonical `ProblemDetailsContextProvider`; the module issues no outbound module-to-module runtime calls (Offer/Order metrics arrive as injected Contracts ports), so `IModuleCallTracer` decoration is not applicable.

### 11. Cross-Module Boundary
- Party → foreign: `LEGAL_CONTRACTS_ONLY` — `Offer.Contracts` + `Order.Contracts` only (Admin sellers composition; no foreign Application/Infrastructure/Domain/Endpoints anywhere, guard-enforced regex over all 5 projects).
- Foreign → Party: `CONTRACTS_ONLY_PROVEN` — Promotion consumes `Party.Contracts` only (`IPartyDevelopmentDirectory` dev-seed surface; `IPartyLookup` in the admin composer); the W1 repair removed the `Party.Application` reference and all `Tooba.Party.Application` usings from Promotion.Infrastructure; all other foreign consumers (Order/Offer tests, guards) reference no Party production namespace.
- Zero cross-module SQL/EF JOIN (metric filtering is in-memory over Contracts rows by design, commented in the engine).

### 12. Persistence Ownership
`CORRECT` — one `PartyDbContext`, own `party` schema, own Outbox table mapping, own migrations (`InitialParty`, `OrganizationProfileFields` + designer + snapshot), `AddModuleSchemaMigrator<PartyDbContext>("Party", ModuleSchemaMigrationOrder.Party)`; no cross-module FK; Application/Endpoints have no DbContext access.

### 13. Host Authority Audit
`ALLOWED_COMPOSITION_ROOT` (Program.cs ×4: presentation/CQRS-assembly/endpoint-map/authorizer DI; `ToobaModuleComposition.cs` ×1: module registration) + `ALLOWED_CONTRACT_CONSUMPTION`/dev-seed invocation (`MarketplaceDevelopmentBootstrap` PartyDbContext migration ×1, `SettingsFoundationDevelopmentSeedHost` ×1) + `ALLOWED_SECURITY_ADAPTER` (`HostPartySellerAuthorizer` ×1 — neutral `ISellerPanelAccess`/`IPlatformEffectiveAccessReader` seams, zero Party business types). All ILLEGAL categories ZERO. No `Host/Party` folder. 13b closed-folder regression: W1 touched only Party + Promotion.Infrastructure dev seed — no closed folder reopened, no sink regression.

### 14. Persistence / Migration Safety
0 migration files touched across all waves; schema byte-identical; `Up/Down` semantics unchanged.

### 15. Durable Guards
New `PartyModuleAmsc001W3CertGuardTests` (5 facts: manifest+SoT cert state, W1/W2 SHA lineage pins, canonical seam single-ownership + bilingual keys, exhaustive validator matrix + endpoint dispatch, self-contained boundaries + persistence + Host closure + inbound Promotion boundary). Existing: W2 structure guard, W1 migrate guard, legacy Amc W1/W2/W3/W4 guards, Host guards.

### 16. Manifest Promotion
`tmar-module-structure-manifests.json` Party entry: `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`, `certificationNote` updated to the TB-TMAR-PARTY-AMSC-001-W3 re-certification (root allowlists/forbidden lists unchanged — still exact). Exactly one certified Party entry.

### 17. Recovery SoT
`tmar-current-state.json` gains the `partyAmsc001W3` certification block (this file's commit is recorded at the W3-R1 reconciliation step, following the accepted lineage pattern) and `structureLock.certifiedModules` already contains `Party`.

### 18. Focused Validation
`dotnet build Tooba.Party.Endpoints` chain = 0 errors; `Tooba.Promotion.Infrastructure` = 0 errors; `Tooba.Host.Tests` = 0 errors; Party guard family = 25/25 PASS (W3 cert 5 + W2 structure 5 + W1 migrate 3 + legacy Amc W1/W2/W3/W4 12). No guard weakened; no open-ended repair loop.

## Microservice Extraction Statement

Party is extractable as an isolated service: own schema + migrations + outbox, Contracts-only inbound (`IPartyLookup`, `IPartySellerSettings`, `IPartyAdminSellerReadGateway`, `IPartyDevelopmentSeedGateway`, `IPartyDevelopmentDirectory`, `AdminSellersGridContracts`) and outbound (`Offer.Contracts`, `Order.Contracts`) edges, module-owned HTTP + CQRS, canonical result/localization/telemetry seams, zero foreign Application/Infrastructure/Domain coupling in either direction.
