# TB-TMAR-CUSTOMERPROFILE-AMSC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only. No production code moved in this wave.

Baseline: `branch = main`, `HEAD == origin/main == 147a6f2c`.

## Target analyzed

`src/backend/Modules/CustomerProfile` (5 projects, 24 production files) + its Host composition seams
+ its inherited Host/AMC guards and SoT records.

| Project | Production files |
|---|---|
| `Tooba.CustomerProfile.Application` | 7 |
| `Tooba.CustomerProfile.Contracts` | 1 |
| `Tooba.CustomerProfile.Domain` | 1 |
| `Tooba.CustomerProfile.Endpoints` | 7 |
| `Tooba.CustomerProfile.Infrastructure` | 8 (incl. 3 migrations) |

## Structured state fields

| Field | Value |
|---|---|
| Foundation-State | `FOUNDATION_READY` (5 projects; never ARCH-COMPLETE-002 certified) |
| Ownership-State | `correct` |
| File-Cohesion-State | `COHESIVE` |
| Oversized/God-File-State | none (largest production file `Domain/CustomerProfile.cs` = 119 LOC) |
| Localization-State | `CANONICAL` (presentation resx) / `HARDCODED_TEXT` (domain fault text) |
| API-Result-Pattern-State | `CANONICAL` |
| Stable-Error-Code-State | `PARTIAL` — `customer.session.required` resolves through the Foundation owner, but domain faults throw raw `InvalidOperationException` |
| Logging-State | `CANONICAL` (no ad-hoc logging) |
| Sensitive-Logging-State | `NONE` |
| OpenTelemetry-State | `CANONICAL` |
| Correlation-Trace-State | `CANONICAL` |
| CQRS-State | `COMPLIANT` (3 endpoint-reachable requests, `ISender`, real handlers) |
| Validator-Coverage-State | `GAPS` — 1 of 3 requests has a validator (see matrix) |
| Contracts-Boundary-State | `CLEAN` |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` (own `customer_profile` schema) |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Host-Residue-State | `ALLOWED_COMPOSITION_ROOT` only |
| Schema-Migration-State | `UNCHANGED` |
| Behavior-Preservation-Risk | `LOW` |
| Canonical-Reference-Used | AddressBook / Cart / Content (capability-first `Application/<Capability>/{Commands,Queries,Validators}`, `Contracts/Errors/<Module>ErrorCodes.cs`, `Application/Composition/<Module>Operation.cs`, `Endpoints/Errors` + `Endpoints/Resources`); BuildingBlocks (`Result`, `ApiResponseFactory`, `SemanticError`, `ContractOperationException`, `IErrorCatalogContributor`, `IErrorResourceSet`) |
| Final-Disposition | `READY_TO_MIGRATE` |
| Structure-Handoff-State | `REQUIRED` |

## Responsibility map

| Responsibility | Owner (current) | Correct owner |
|---|---|---|
| Profile read/write HTTP routes | `Endpoints.Customer` | same |
| Dashboard + dev-context HTTP routes | `Endpoints.CustomerDashboard` | same |
| Actor authority seam | `Endpoints.Customer.CustomerAccountActorResolver` | same |
| Upsert command + handler | `Application.Commands.UpsertCustomerProfile` | `Application/Profile/Commands/` |
| Profile page query + handler | `Application.Queries.GetCustomerProfilePage` | `Application/Profile/Queries/` |
| Dashboard query + handler | `Application.Queries.GetCustomerAccountDashboard` | `Application/Account/Queries/` |
| Transport validator + codes | `Application.Validators.UpsertCustomerProfile` | `Application/Profile/Validators/` |
| Presentation DTOs | `Application.Models.CustomerAccountPages` | `Application/Account/Models/` (shared presentation surface) |
| Presentation display-text port | `Application.Ports` | `Application/Ports/` (shared, cross-capability) |
| Boundary contract `ICustomerProfileDirectory` + snapshot/write | `Contracts` (root) | `Contracts/Ports/` + `Contracts/Dtos/` |
| Empty placeholder `Application/CustomerProfileContracts.cs` | `Application` | **DELETE** (stale no-op comment file) |
| Profile aggregate + invariants | `Domain` (root) | `Domain/Aggregates/` |
| Directory / DbContext / Outbox / migrations | `Infrastructure` (root + `Persistence`, `Development`) | `Infrastructure/Directories/`, `Persistence/`, `Messaging/`, `DependencyInjection/` |
| Stable error codes | *(none)* | `Contracts/Errors/CustomerProfileErrorCodes.cs` |
| Error catalog + resources | *(none)* | `Endpoints/Errors/` + `Endpoints/Resources/` |
| Host composition (DI + route map + seed seam) | `Tooba.Host` | allowed |

No `MUST_SPLIT` file. No Host business authority.

## Ownership map

Host references (verified complete set):

- `Program.cs` — `using Tooba.CustomerProfile.Endpoints;` (L37) → `ALLOWED_COMPOSITION_ROOT`.
- `Composition/ToobaModuleComposition.cs` — `using Tooba.CustomerProfile.Infrastructure;`, `new CustomerProfileModule()` → `ALLOWED_COMPOSITION_ROOT`.
- `Development/DevelopmentSchemaMigrator.cs` — `CustomerProfileDevelopmentSeed.ApplyAsync` ×2 → `ALLOWED_COMPOSITION_ROOT`.
- `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` — module schema descriptor → `ALLOWED_COMPOSITION_ROOT`.
- No Host `CustomerProfile` folder, no Host endpoint, no Host persistence, no Host business policy.

## Current illegal dependencies

**ZERO.** No foreign `*.Application` / `*.Infrastructure` / `*.Domain` reference from any CustomerProfile
production file. Cross-module seams are Contracts-only: `Order.Contracts` (Customer + Fulfillment),
`Wishlist.Contracts.Ports`, `AddressBook.Contracts.Ports`, `Identity.Contracts`.

`ICustomerProfileDirectory` is a genuine **public boundary port**: it is consumed by
`Tooba.Identity.Application/Auth/Queries/GetAuthMeQuery.cs`. It must stay in `Contracts` — moving it to
Application would break the Contracts-only rule. Confirmed correct as-is.

## Cross-module join inventory

**NONE.** `CustomerProfileDirectory` reads only its own `CustomerProfileDbContext`. No EF navigation
across modules, no cross-schema SQL.

## CQRS / MediatR gaps

**NONE.** Endpoint → `ISender` → `IRequest<Result<T>>` → `IRequestHandler<,>`; registered through
`AddToobaCqrsFoundation`. No endpoint persistence/directory access.

## Validation classification matrix

| Request | Route | Classification | Validator |
|---|---|---|---|
| `GetCustomerAccountDashboardQuery` | `GET /v1/customer/dashboard` | `VALIDATOR_REQUIRED` | **MISSING** |
| `GetCustomerProfilePageQuery` | `GET /v1/customer/profile` | `VALIDATOR_REQUIRED` | **MISSING** |
| `UpsertCustomerProfileCommand` | `PUT /v1/customer/profile` | `VALIDATOR_REQUIRED` | present |

The current guard (`CustomerProfileValidatorCoverageGuardTests`) classifies both GETs as
`NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT`, on the reasoning that their only member is a
server-trusted `ActorUserId`. The repository's AMSC convention is stricter: every endpoint-reachable
request carries a shape validator, and a server-trusted actor id still warrants a defensive
`ActorUserId != Guid.Empty` transport rule (AddressBook `GetCustomerAddressQueryValidator`,
Cart `GetCartQueryValidator`, Content `QueryAdminArticlesGridQueryValidator`).

**Decision:** both GETs become `VALIDATOR_REQUIRED` and receive a minimal `ActorUserId != Guid.Empty`
validator. No business rule is moved into transport.

## Localization findings

- Canonical presentation mechanism present and used: `CustomerAccountPresentation.resx` +
  `.fa.resx`, `CustomerAccountPresentationResources`, `ICustomerAccountDisplayTexts`
  (Application port → Endpoints implementation). No inline user-facing literals in Application.
- **Violation — hard-coded Persian fault text in Domain and Infrastructure:**
  `Domain/CustomerProfile.cs` (`"Actor معتبر الزامی است."`, `"نام نمایشی معتبر نیست."`,
  `"نام بیش از حد بلند است."`, `"نام خانوادگی بیش از حد بلند است."`, `"تاریخ تولد بیش از حد بلند است."`,
  `"بیوگرافی بیش از حد بلند است."`) and `Infrastructure/CustomerProfileDirectory.cs`
  (`"Actor معتبر الزامی است."`).
- **Violation — raw exception type:** these throw `InvalidOperationException`, not a typed
  code-carrying fault, so no stable machine code reaches the presentation layer.
- **Violation — unregistered code:** the endpoint emits `new SemanticError("customer.session.required")`.
  That code is owned by `FoundationErrorCatalogContributor` (`FoundationErrorCodes.CustomerSessionRequired`,
  `ErrorClassification.Forbidden`), so it resolves — but the string literal is untyped and the module
  declares no stable-code owner of its own.
- No `ex.Message`-based classification anywhere.

## API result / error mapping findings

- Endpoints use `api.From(result)` for both business routes — canonical. No `Results.Ok/Json/BadRequest/Problem`
  for business paths.
- `GET /v1/customer/dev-context` is an explicit `PLATFORM_DEV_ROUTE_EXCEPTION`
  (`Results.NotFound()` + raw anonymous JSON), documented and preserved.
- **Gap:** domain faults are untyped `InvalidOperationException`, so the presentation layer can only
  produce a generic 500 instead of a stable classified business error. There is no module error catalog
  contributor and no module error resource set.

## Logging / sensitive-data findings

- No logging, no `Console.WriteLine`, no second telemetry pipeline, no secrets in the module.
- The module stores only descriptive profile fields (`FirstName`, `LastName`, `DisplayName`, `BirthDate`,
  `Bio`); email/mobile remain Identity-owned and are never persisted here.

## OpenTelemetry / correlation findings

- No custom correlation, no direct `StartActivity`, no manual `traceparent`.
- Presentation supplies `traceId`/`correlationId` through the canonical `IProblemDetailsContextProvider`.
- No cross-module call requiring `IModuleCallTracer` decoration beyond ordinary Contracts reads.

## File cohesion / splitting plan

`COHESIVE`. No file requires splitting. Structural refinements only:

1. Delete the stale no-op `Application/CustomerProfileContracts.cs` (comment-only placeholder; the
   contract lives in Contracts).
2. `Application/{Commands/UpsertCustomerProfile, Queries/GetCustomerProfilePage, Queries/GetCustomerAccountDashboard, Validators/UpsertCustomerProfile, Models}`
   → capability-first shallow `Application/Profile/{Commands,Queries,Validators}` +
   `Application/Account/{Queries,Models}`; keep `Application/Ports/` shared.
3. `Domain/CustomerProfile.cs` → `Domain/Aggregates/CustomerProfile.cs`.
4. `Infrastructure/CustomerProfileDirectory.cs` → `Infrastructure/Directories/`;
   `Infrastructure/CustomerProfileModule.cs` → `Infrastructure/DependencyInjection/`;
   `CustomerProfileOutboxRegistration` → `Infrastructure/Messaging/` (split from the module file).
5. `Contracts/CustomerProfileContracts.cs` → `Contracts/Ports/ICustomerProfileDirectory.cs` +
   `Contracts/Dtos/CustomerProfileSnapshot.cs` + `Contracts/Dtos/CustomerProfileWrite.cs`.
6. Add `Contracts/Errors/CustomerProfileErrorCodes.cs`,
   `Application/Composition/CustomerProfileOperation.cs`,
   `Endpoints/Errors/CustomerProfileErrorCatalogContributor.cs`,
   `Endpoints/Resources/CustomerProfileErrorResources.cs` + `CustomerProfileErrors{,.fa}.resx`.

## Exact target paths / namespaces

```text
Modules/CustomerProfile/
  Tooba.CustomerProfile.Contracts/    Errors/CustomerProfileErrorCodes.cs
                                      Ports/ICustomerProfileDirectory.cs
                                      Dtos/CustomerProfileSnapshot.cs
                                      Dtos/CustomerProfileWrite.cs
  Tooba.CustomerProfile.Domain/       Aggregates/CustomerProfile.cs
  Tooba.CustomerProfile.Application/  Composition/CustomerProfileOperation.cs
                                      Profile/{Commands,Queries,Validators}/
                                      Account/{Queries,Models}/
                                      Ports/ICustomerAccountDisplayTexts.cs
  Tooba.CustomerProfile.Infrastructure/ DependencyInjection/CustomerProfileModule.cs
                                        Messaging/CustomerProfileOutboxRegistration.cs
                                        Directories/CustomerProfileDirectory.cs
                                        Persistence/ Development/ Migrations/
  Tooba.CustomerProfile.Endpoints/    Customer/ CustomerDashboard/
                                      Errors/CustomerProfileErrorCatalogContributor.cs
                                      Resources/CustomerProfileErrorResources.cs
                                      CustomerProfileEndpointModule.cs
```

Solution grouping `/Modules/CustomerProfile/` is already canonical in `Tooba.slnx` (5 projects).

## Behavior-preservation checklist

Routes `GET /v1/customer/profile`, `PUT /v1/customer/profile`, `GET /v1/customer/dashboard`,
`GET /v1/customer/dev-context`; methods; response DTO shape and JSON field parity
(`CustomerProfilePage`, `CustomerDashboardPage`); the dev-context 404 + raw anonymous JSON contract;
authorization/session semantics (`customer.session.required` → 403/401 as today, Foundation-owned);
actor resolution precedence (authenticated session → Dev/Testing header → `StorefrontGuestActor`);
domain invariants (display-name 3..128, name parts ≤64, birth date ≤32, bio ≤200, name derivation);
`customer_profile` schema, table, columns, keys, migration IDs; DI lifetimes; outbox registration;
seed values and idempotency; Host composition seams.

## Certification blockers (ARCH-COMPLETE-002, current state)

1. **Never AMSC-certified.** No `structureCertified` manifest entry, no AMSC SoT record, no
   ARCH-COMPLETE-002 durable guard, no AMSC evidence tree. `hostCustomerFullClosure.residualDebt`
   still records "CustomerProfile/Wishlist not COMPLETE_REFERENCE_PATTERN (minimum foundation only)".
2. **Technical-axis-first Application tree** (`Commands/<UseCase>`, `Queries/<UseCase>`,
   `Validators/<UseCase>`, root `Models`) — single-file request leaf folders.
3. **Root-dumped Domain/Contracts/Infrastructure files** — no `Aggregates/`, `Ports/`, `Dtos/`,
   `Directories/`, `Messaging/`, `DependencyInjection/` capability folders.
4. **Hard-coded fault text + untyped domain faults** — no stable machine codes, no error catalog
   contributor, no module error resources, no typed-fault → `Result` composition seam.
5. **Validator coverage gap** — 1 of 3 endpoint-reachable requests.
6. **Stale no-op file** — `Application/CustomerProfileContracts.cs`.
7. **Stale guards RED at clean HEAD (pre-existing):** `CustomerProfileSolutionGroupingGuardTests`
   and `HostCustomerProfileEvacuationGuardTests.Parent_R1_solution_grouping_remains_exact` both assert
   a flat `/Modules/` solution folder that does not exist anywhere in `Tooba.slnx` (the file has no
   `/Modules/` folder at all). Both fail before and after this wave's work and must be repaired, not
   weakened.

## Wave plan

| Wave | Focus |
|---|---|
| W1 Migrate | `Contracts/Errors/CustomerProfileErrorCodes.cs`; typed `ContractOperationException` faults in Domain/Infrastructure (drop hard-coded text); `Application/Composition/CustomerProfileOperation.cs`; `Endpoints/Errors` contributor + `Endpoints/Resources` resx/resource set; typed session code constant; add the two missing GET validators; Domain → own Contracts reference (AddressBook/Cart precedent) |
| W2 Structure | Capability-first shallow Application (`Profile/`, `Account/`); `Domain/Aggregates/`; Contracts `Ports/`+`Dtos/`; Infrastructure `Directories/`+`Messaging/`+`DependencyInjection/`; delete the stale no-op file; exact path↔namespace; root allowlists; repair the two stale grouping guards; structure guard + structure evidence |
| W3 Certify | ARCH-COMPLETE-002 certification; AMSC SoT record + manifest entry; Master Recovery checkpoint; durable cert guard; certification evidence |

## Microservice extractability

Already strong: own schema (`customer_profile`), own migrations, zero foreign
Application/Infrastructure/Domain coupling, Contracts-only cross-module seams, module-owned endpoints,
Host composition-only residue. Remaining extraction work is zero-coupling-preserving quality closure:
typed fault codes, a single stable-code owner, error catalog/resources, validator coverage, canonical
capability-first structure and AMSC certification state.
