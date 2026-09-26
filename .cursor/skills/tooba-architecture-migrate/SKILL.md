---
name: tooba-architecture-migrate
description: Migrate Tooba code from Host or the wrong module into the correct module(s), creating the target module foundation first when missing, safely decomposing mixed/god files, removing cross-module coupling and joins, enforcing Contracts-only communication, CQRS/MediatR/FluentValidation/Endpoints structure, canonical localization/API-result/logging/telemetry patterns, and preserving behavior.
---

# Tooba Architecture Migrate (V2)

Use this skill when the user wants actual architecture migration, Host evacuation, ownership correction, capability extraction, decoupling, safe file decomposition, or movement of one or more files into their true owning module(s).

This skill MAY modify production code.

The migration is not complete merely because files were moved.
The final state must be structurally correct, behavior-preserving, Contracts-bounded, CQRS-aligned where applicable, cohesive, canonical in its cross-cutting concerns (localization, API result/error, logging, tracing/correlation), and ready for later microservice extraction.

## 1. Mission

Move every responsibility to its true owning module while preserving behavior and eliminating architectural coupling.

A successful migration means:

- true module ownership is established;
- mixed-responsibility files are split when required;
- missing destination module structure is created BEFORE moving business code;
- Host owns composition/platform seams only;
- HTTP ownership lives in module Endpoints;
- application use cases use MediatR CQRS;
- transport validation is explicit and exhaustively classified;
- cross-module communication uses Contracts only;
- no cross-module DB join or foreign persistence access remains;
- path and namespace are exact;
- user-facing text uses the canonical localization mechanism;
- endpoints use the canonical API result/error mapping;
- logging and telemetry use the canonical foundation (no secrets logged);
- trace/correlation continuity is preserved;
- schema and behavior remain unchanged unless explicitly authorized;
- focused tests and architecture guards pass.

## 2. Mandatory Repository Recovery

Before editing:

1. Read `AGENTS.md`.
2. Read current TMAR architecture sources of truth if present:
   - `docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md`
   - `docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md`
   - `docs/architecture/TMAR-architecture-locks.md`
   - `docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md`
   - `docs/architecture/tmar-current-state.json`
   - `docs/architecture/tmar-module-structure-manifests.json`
3. Inspect the full target file/folder/capability.
4. Inspect all callers, consumers, registrations, tests and dependencies.
5. Preserve user work.
6. Establish behavior baseline before modification.

Repository reality is authoritative.

## 3. Canonical Mechanism Discovery (MANDATORY)

Do not invent abstractions and do not blindly copy the reference module. Before writing new code for any cross-cutting concern, discover the repository's canonical mechanism and reuse it. If `src/backend/Modules/Offer` demonstrates the established pattern for that concern, use Offer as the reference; if another module or BuildingBlocks mechanism is authoritative, use that.

Canonical mechanisms in this repository (verify current state before relying on them):

- **Result / expected failures**: `Tooba.BuildingBlocks.Results.Result` / `Result<T>` carrying `SemanticError`.
- **API response mapping**: `Tooba.BuildingBlocks.Presentation.ApiResponseFactory` (`From`, `From<T>`, `Created`, `FromFailure`, `FromException`, `FromSemanticException`, `FromPlatformException`).
- **Error catalog / mapping**: `IErrorCatalogContributor` + `ErrorDescriptor` + `IErrorDefinitionCatalog` + `ISafeErrorMapper` (`SafeErrorMapper`).
- **ProblemDetails / trace**: `IProblemDetailsContextProvider` supplies `errorCode`, `traceId`, `correlationId`, `requestId`.
- **Global exception boundary**: `Tooba.BuildingBlocks.Presentation.IExceptionPresentationService` registered by Host.
- **Stable error codes**: `Tooba.<Module>.Contracts.Errors.<Module>ErrorCodes`; validation codes in `Tooba.<Module>.Application.Validators.<Module>ValidationCodes`.
- **Localization**: `IErrorResourceSet` (module resource set) + `.resx` (`<Module>Errors.resx` + `<Module>Errors.fa.resx`) + `IErrorMessageLocalizer` / `ResourceErrorMessageLocalizer` + `IRequestLocaleResolver`.
- **CQRS foundation**: `Tooba.BuildingBlocks.ToobaCqrsRegistration.AddToobaCqrsFoundation` (MediatR 12.5.0, `ValidationBehavior`, `LoggingBehavior`, `TracingBehavior`).
- **Logging**: `ILogger<T>` + `ObservabilityLogScope` + `ObservabilityLogScopeKeys`.
- **Tracing/correlation**: `ToobaTelemetry` (`ActivitySource`/`Meter` named `Tooba`), `IModuleCallTracer` / `ModuleCallTracer`, `CorrelationIdConstants.HeaderName` (`X-Correlation-Id`), `ICorrelationIdProvider`.
- **Source-size guard**: `TmarSourceSizeGuard` + `Baselines/tmar-source-size-baseline.json`.

Never create a parallel version of any of the above.

## 4. FIRST GATE — TARGET MODULE FOUNDATION CHECK

Before moving any business file into a module, inspect whether the target module has a valid architectural foundation.

Do NOT assume the destination is ready.

Examples:

- Offer may already have a valid structure and can receive migrated code directly.
- Catalog may still have legacy/root-dump structure and may require foundation creation before migration.

For EACH target module, classify:

- **FOUNDATION_READY** — The module already has the required projects/folders and can receive the capability safely. Use the existing structure. Do not create parallel folder schemes. If `structureCertified: true` exists for the module in `tmar-module-structure-manifests.json` (or equivalent canonical certification), treat it as `FOUNDATION_READY` and extend the existing capability-oriented structure only.
- **FOUNDATION_PARTIAL** — Some required projects or capability folders exist, but the destination needed for this migration is missing or structurally invalid. Create only the minimum missing standard structure required for the migration. Do not redesign unrelated parts of the module.
- **FOUNDATION_MISSING** — The module does not yet have a safe destination architecture. Before moving business code, create the minimum module foundation required by the current Tooba architecture standard. The migration must NOT dump new files into a legacy root simply because the correct structure is absent.

## 5. FOUNDATION CREATION RULES

When foundation creation is required, create it BEFORE moving the migrated responsibility.

The minimum foundation depends on module applicability.

**HTTP-owning module**

Create or verify as applicable:

- `Tooba.<Module>.Contracts`
- `Tooba.<Module>.Domain`
- `Tooba.<Module>.Application`
- `Tooba.<Module>.Infrastructure`
- `Tooba.<Module>.Endpoints`

Do not create ceremonial projects that are not applicable to the module.

**Internal-only module**

Do not create Endpoints or CQRS ceremony when the module has no HTTP/application use cases.

Follow the established INTERNAL_ONLY precedent (e.g. Inventory).

**Required folder patterns**

Create only the folders needed by the real capability.

Application:

```text
Application/
  <Capability>/
    <UseCase>/
    Models/
    Ports/
  Validators/
    <Capability>/
```

Endpoints:

```text
Endpoints/
  Admin/
  Seller/
  Customer/
  Storefront/
  Errors/       (IErrorCatalogContributor implementation)
  Resources/    (resx + IErrorResourceSet)
```

Infrastructure:

```text
Infrastructure/
  Persistence/
    Migrations/
  Directories/
  Adapters/
  Workers/
  Development/
```

Contracts:

```text
Contracts/
  <Capability>/
  Errors/       (stable <Module>ErrorCodes)
```

Domain structure should follow the module's actual aggregate/value-object organization and current certified precedents.

Do not create empty decorative folders.

## 6. FOUNDATION SAFETY RULES

Foundation creation MUST NOT:

- move unrelated legacy code;
- certify the entire module prematurely;
- redesign domain behavior;
- change DB schema;
- introduce fake placeholder implementations;
- create duplicate module abstractions;
- invent a second architectural pattern;
- add Endpoints when module applicability is INTERNAL_ONLY;
- add MediatR merely for ceremony where no application use case exists.

Foundation creation is only enough to provide the correct destination for the migration.

After foundation creation, continue the actual migration in the same workflow if safe.

If creating the required foundation would become a broad module-wide redesign, stop and report:

`FOUNDATION_REQUIRES_SEPARATE_BOUNDED_TASK`

Do not dump migrated code into the wrong place as a shortcut.

Never create a parallel architecture beside an already certified structure.

## 7. Responsibility Analysis Before Move

For every significant responsibility classify it as:

- DOMAIN_RULE
- APPLICATION_USE_CASE
- CONTRACT
- HTTP_ENDPOINT
- AUTHORIZATION_ADAPTER
- PERSISTENCE
- INTEGRATION_ADAPTER
- BACKGROUND_WORKER
- DEVELOPMENT_SEED
- PRESENTATION_COMPOSITION
- HOST_COMPOSITION_ROOT
- CROSS_MODULE_ORCHESTRATION

If one source file contains responsibilities owned by multiple modules:

`MUST_SPLIT`

Never move the whole file into one module merely to reduce the diff.

## 8. True Ownership Rule

Assign every responsibility to the module that owns the business capability.

Examples:

- payment lifecycle/state -> Payment
- order lifecycle/state -> Order
- cart behavior -> Cart
- offer selection/pricing-offer concern -> Offer
- address lifecycle -> AddressBook
- catalog product/category ownership -> Catalog
- identity-owned authentication business capability/state, credential lifecycle, login/password/OTP business rules -> Identity
- global authentication/session HTTP boundary, middleware, principal/session/runtime plumbing -> MAY remain Host-owned when explicitly allowed by canonical architecture locks

Host is not a business owner.

Host may keep only legitimate composition/platform/security adaptation.

### 8a. Authentication Ownership During Migration

Never infer authentication ownership from the word "authentication" alone. Canonical architecture documents, locks and current SoT decide the boundary.

If Analyze classifies a responsibility as `GLOBAL_HOST_AUTH_PLATFORM_BOUNDARY` or `HOST_AUTH_RUNTIME_PLUMBING`:

- do NOT migrate it into Identity;
- preserve its Host ownership;
- allow safe internal file splitting/cohesion cleanup inside Host;
- preserve routes, middleware order, session/principal semantics, error codes, telemetry, correlation, tenant isolation and security behavior;
- do not create `Identity.Endpoints` merely to evacuate this legitimate Host platform seam.

If Analyze classifies a responsibility as `IDENTITY_BUSINESS_CAPABILITY`:

- migrate it to the correct Identity project/layer using the existing valid module structure;
- use Contracts-only boundaries where cross-module access is required;
- do not leave business authority in Host.

If a file contains both (`MIXED_AUTH_RESPONSIBILITY`):

- split it by true responsibility;
- move only the Identity-owned business portion;
- retain only legitimate Host platform code.

Boundary example only (not a naming requirement): `src/backend/Host/Tooba.Host/Authentication` currently holds the global authentication/session HTTP/runtime boundary and legitimately consumes Identity services without inheriting Identity business ownership. Do not hard-code its current file names as permanent architecture requirements.

## 9. Multi-Module Migration

A single source file may legitimately be decomposed into multiple target modules.

Example:

```text
Host/Admin/SomeLargeComposer.cs
    ├── Order responsibility   -> Order
    ├── Payment responsibility -> Payment
    ├── Catalog responsibility -> Catalog
    └── composition            -> Host
```

For every target module:

1. check foundation readiness;
2. create minimum missing foundation if required;
3. migrate only that module's responsibility;
4. create/reuse Contracts boundaries;
5. remove illegal coupling;
6. verify behavior.

Never solve a mixed file by assigning arbitrary ownership to one module.

## 10. Safe File Decomposition (V2)

A file is not acceptable merely because it is under a hard LOC ceiling.

Before moving or after moving, identify files that are oversized OR clearly contain multiple independent responsibilities:

- endpoint lambdas + request/response models + infrastructure + business logic mixed together;
- multiple unrelated top-level types with different reasons to change;
- god-file symptoms (many regions, dozens of private methods, mixed audiences).

When splitting:

- split into smaller cohesive files;
- keep each responsibility in the correct existing folder;
- preserve behavior exactly;
- preserve routes;
- preserve contracts and public API shape;
- preserve DI behavior and lifetimes;
- preserve visibility unless architecture requires otherwise;
- preserve stable error codes;
- preserve telemetry event names and semantic dimensions;
- preserve correlation semantics;
- preserve persistence behavior and schema.

Rules:

- Do NOT perform cosmetic splitting that creates meaningless tiny files.
- Do NOT use file splitting as an excuse for ownership migration.
- Do NOT create artificial parallel decompositions.
- If the file is legitimate Host platform code (process startup, composition, global auth/session boundary, middleware, health, generic security adapters, observability hosting, tenant/runtime context):
  - split it safely **within Host** when appropriate;
  - do NOT force it into a business module.

If the module/repo has a source-size baseline (`tmar-source-size-baseline.json`), update it only for files that genuinely changed classification, and never to hide an oversized god-file.

## 11. Contracts-Only Cross-Module Rule

Cross-module communication must use the owning module's Contracts boundary.

Allowed:

```text
Order.Application   -> Payment.Contracts
Payment.Application -> Order.Contracts
Catalog.Application -> Media.Contracts
```

Forbidden:

```text
Order.Application   -> Payment.Application
Order.Infrastructure -> Payment.Infrastructure
Order.Domain        -> Payment.Domain
Catalog.Application -> Offer.Infrastructure
```

Contracts must not expose:

- EF entities;
- DbContext;
- repository implementations;
- internal aggregates;
- infrastructure types;
- mutable internal persistence models.

Prefer narrow DTOs and narrow ports.

## 12. Cross-Module Join Elimination

This is a HARD RULE.

Search for and remove:

- cross-module EF joins;
- access to another module's DbSet;
- foreign DbContext usage;
- raw SQL joining different module-owned schemas;
- navigation properties crossing module ownership;
- direct reads of another module's tables;
- transaction logic depending on multiple module databases.

Replace with the smallest correct mechanism:

- Contracts lookup;
- local projection/read model;
- integration event;
- immutable snapshot;
- identifier-based orchestration;
- explicitly approved platform read model.

Do not preserve a cross-module join for convenience.

If safe replacement requires a broader product/consistency decision, stop with a clear architecture blocker instead of hiding the join.

## 13. CQRS / MediatR Migration

For every HTTP-reachable application use case:

- use a real `IRequest<T>` / `IRequest<Unit>`;
- use a real `IRequestHandler<,>`;
- dispatch from Endpoints through `ISender`;
- keep endpoint code transport-only;
- map expected failures with the canonical API response factory (see section 15).

Forbidden:

- endpoint -> DbContext;
- endpoint -> Directory implementation;
- endpoint -> Infrastructure;
- Host endpoint bypass;
- generic custom dispatcher replacing MediatR;
- business rules implemented in endpoint lambdas.

Use `AddToobaCqrsFoundation` for registration; do not register a second MediatR/FluentValidation pipeline.

Do not create CQRS ceremony for internal-only code with no application use case.

## 14. FluentValidation Rule

Every endpoint-reachable request must be classified:

- `VALIDATOR_REQUIRED`
- `NO_VALIDATOR_REQUIRED`

For `VALIDATOR_REQUIRED`, add a concrete FluentValidation validator discoverable through the normal pipeline.

Validators may enforce transport/input shape only.

Examples:

- empty Guid;
- null request;
- invalid primitive shape;
- malformed supplied collection item;
- required text;
- transport-level length/format.

Validators must NOT own:

- authorization;
- ownership;
- DB existence;
- business state transition;
- pricing rules;
- inventory rules;
- tenant policy;
- domain invariant;
- workflow eligibility.

Those stay in Application/Domain.

Validators must emit **stable machine codes** (e.g. `offer.validation.*`), never Persian/English user-facing text. If the module uses a validation-codes class (like `OfferValidationCodes`), follow that precedent.

Where the canonical structure requires a durable validator-coverage guard, add/update it (see section 23).

## 15. Localization Repair (V2)

When migrating, repair any non-canonical user-facing text using the repository's canonical mechanism (do not invent one):

- Replace hard-coded Persian/English user-facing messages with resource-backed localization keys.
- Represent expected business errors with stable machine codes (`Contracts.Errors.<Module>ErrorCodes`) carried by `SemanticError`.
- Register each stable code in a module `IErrorCatalogContributor` with `Code`, `Classification`, explicit `HttpStatus`, `LocalizationKey`, `Severity`, and a safe English `SafeTitleFallback`.
- Provide `IErrorResourceSet` for the module key prefix and `.resx` / `.resx` (e.g. `*.fa.resx`) resources for the user-facing text.
- Never use `exception.Message` / `ex.Message` as a localized or user-facing contract.
- Never parse `Accept-Language` in an endpoint; use the canonical `IRequestLocaleResolver` path.
- Preserve existing localization keys and their semantics; do not rename or repurpose published keys.
- Do not introduce hard-coded Persian/English API messages inside Application, Domain, or Endpoints when the localization mechanism exists.

## 16. Canonical API Result / Error Mapping (V2)

Endpoints must use the established response/error mapping pattern, not ad-hoc results.

- Application handlers return `Result` / `Result<T>` for expected business outcomes (real `IRequest<Result<T>>`).
- Endpoints inject `ApiResponseFactory` and return `api.From(result)` / `api.Created(location, result)`.
- Do NOT introduce `Results.Json(...)`, `Results.BadRequest(...)`, `Results.Problem(...)`, local `ProblemDetails` builders, local error mappers, or `catch`-and-map blocks when the canonical abstraction covers the concern.
- Stable error codes remain machine-stable and catalogue-backed.
- Expected failures are typed (`SemanticError`) or stable-code based.
- Never classify failures by parsing `ex.Message` / `when (ex.Message is ...)`.
- Unknown/unexpected exceptions must NOT be silently converted to business failures; they flow to the canonical global exception boundary (`IExceptionPresentationService`).
- Preserve the existing success contract shape. Offer seller success JSON is intentionally a raw DTO (no envelope) for shipped client compatibility — do not "improve" it during migration.
- Preserve status codes and error codes exactly.

## 17. Canonical Logging / Observability (V2)

Use the repository's existing logging/telemetry foundation.

- Use `ILogger<T>`; never `Console.WriteLine` / `Debug.WriteLine` or a new logger framework.
- Do not create a second telemetry pipeline, second `ActivitySource`, or second `Meter`.
- Use structured logging templates with named placeholders; never string-concatenated messages carrying values.
- Reuse `ObservabilityLogScope` / `ObservabilityLogScopeKeys` rather than re-declaring scope keys.
- Preserve existing event names and semantic dimensions unless a change is explicitly authorized.

Never log:

- passwords;
- refresh tokens;
- access tokens;
- OTP secrets;
- reset secrets;
- `Authorization` headers;
- cookies;
- session secrets;
- security stamps;
- private credentials;
- full query strings or payment payloads.

While migrating, remove any such sensitive logging you encounter inside the migrated surface.

## 18. OpenTelemetry / Trace / Correlation Preservation (V2)

- Preserve OpenTelemetry integration and distributed trace propagation.
- Preserve `Activity` / `TraceId` / `SpanId` semantics.
- Preserve existing correlation behavior; do not generate a competing correlation ID when the repository already supplies one (`ICorrelationIdProvider`, `X-Correlation-Id`).
- Do not introduce custom correlation middleware, custom headers, or raw `AsyncLocal` correlation for the migrated surface.
- Do not call `ActivitySource.StartActivity(...)` directly in Application/Endpoints; use `IModuleCallTracer` for cross-module calls.
- Do not parse `traceparent` manually.
- API ProblemDetails/error responses must use the canonical trace/correlation source (`IProblemDetailsContextProvider`).
- Do not break trace continuity during module migration; keep the module-call tracer decoration wiring (see Offer `Adapters/Tracing`) when the module issues cross-module calls.

## 19. Endpoint Ownership

HTTP routes must belong to `<Module>.Endpoints`.

Organize by actual audience/capability:

- Admin
- Seller
- Customer
- Storefront

Keep a small composition entry such as:

`<Module>EndpointModule.cs`

Host may call the module endpoint mapper but must not own business endpoint implementation.

No duplicate route ownership.

## 20. Infrastructure Placement

Place infrastructure by responsibility:

- `Persistence/`
- `Persistence/Migrations/`
- `Directories/`
- `Adapters/`
- `Workers/`
- `Development/`

Use other folders only when they represent a coherent integration/capability and align with current Tooba standards.

Do not leave implementation files at project root unless explicitly allowed.

Do not regenerate migrations merely because they moved folders.

## 21. Physical Path ↔ Namespace

Namespace must exactly match physical path.

Hard failures:

- mismatched namespace;
- namespace alias hiding wrong placement;
- `TypeForwardedTo` used to preserve bad architecture;
- duplicate compatibility type;
- global alias hiding foreign module coupling.

Update real consumers instead.

## 22. Host Evacuation Classification

For every remaining Host reference classify:

- ALLOWED_COMPOSITION_ROOT
- ALLOWED_SECURITY_ADAPTER
- ALLOWED_CONTRACT_CONSUMPTION
- STRUCTURAL_DEBT_ONLY
- ILLEGAL_BUSINESS_AUTHORITY
- ILLEGAL_PERSISTENCE_AUTHORITY
- ILLEGAL_ENDPOINT_OWNERSHIP

Remove all ILLEGAL ownership.

Legitimate global/platform Host concerns may remain:

- process startup;
- composition root;
- global authentication/session boundary;
- generic authorization infrastructure;
- middleware;
- health/readiness;
- observability/platform hosting;
- tenant/runtime context;
- other explicitly allowed canonical Host platform seams.

Do not chase textual Host-reference ZERO when a legitimate composition reference is required.

If a legitimate retained Host file is oversized or mixes internal responsibilities, split it safely **within Host** (section 10) — do not migrate it merely because it lives in Host.

## 23. Architecture Guard Requirement

When migration creates a new stable boundary, add or update focused durable guards where appropriate.

Guards should enforce real architecture such as:

- Host residue absence;
- route ownership;
- request inventory;
- validator classification;
- path↔namespace exactness;
- forbidden root files;
- Contracts-only references;
- no alias workaround;
- canonical API result/error mapping (no raw `Results.Json`, no `ex.Message` classification);
- canonical localization (codes catalogued, resources present, no hard-coded FA/EN in Domain/Application);
- canonical logging (no `Console.WriteLine`, no second telemetry pipeline);
- correlation/trace continuity (no raw `StartActivity`, no `traceparent` parsing).

Do not implement behavior in architecture tests.

Do not weaken an existing guard merely to pass.

## 24. Mandatory Post-Migration Audit

Search for all of the following:

- foreign `.Application`
- foreign `.Infrastructure`
- foreign `.Domain`
- module -> Host dependency
- foreign DbContext
- foreign DbSet
- cross-module EF join
- cross-module SQL join
- endpoint direct Directory call
- endpoint direct DbContext call
- missing `IRequest`
- missing `IRequestHandler`
- non-`ISender` dispatch
- missing validator classification
- missing validator
- namespace/path mismatch
- using alias workaround
- `TypeForwardedTo`
- duplicate route ownership
- stale Host implementation
- stale DI registration
- migration/schema drift
- compatibility shims
- duplicated contract types
- hard-coded Persian/English user-facing text in Domain/Application/Endpoints
- `ex.Message` / `exception.Message` used for response/classification
- raw `Results.Json` / `Results.BadRequest` / `Results.Problem` where the factory is canonical
- unregistered stable error codes
- `Console.WriteLine` / custom logger frameworks
- sensitive-data logging
- direct `StartActivity` / manual `traceparent` parsing / competing correlation IDs
- oversized/god files newly created

Any unresolved item must be explicitly reported.

## 25. Focused Build/Test Strategy

Prefer focused validation.

Build changed projects:

- Contracts
- Domain if relevant
- Application
- Infrastructure
- Endpoints
- Host when composition changed
- test project containing architecture guards

Run focused tests for:

- behavior parity;
- migrated capability;
- validator coverage;
- endpoint ownership;
- structure guards;
- localization/error catalog guard;
- tracing/correlation guard;
- durable architecture guards.

Do not claim success while required focused guards fail.

Do not broaden the migration merely to make unrelated repository-wide tests green.

## 26. Migration Completion States

Return exactly one conceptual state:

**READY_FOR_CERTIFICATION**

Use only when:

- ownership is correct;
- required foundation exists;
- migration completed;
- file cohesion is correct and no new god-file exists;
- illegal coupling removed;
- no cross-module persistence/join remains;
- CQRS/validation/endpoint rules satisfied where applicable;
- localization/API-result/logging/telemetry use canonical mechanisms;
- no sensitive logging introduced;
- behavior preserved;
- focused validation passes.

**FOUNDATION_REQUIRES_SEPARATE_BOUNDED_TASK**

Use when the destination module is too structurally incomplete to safely create the foundation inside the requested migration scope.

Do not perform a dirty move.

**INCOMPLETE**

Use when work started but not all required migration criteria were safely completed.

**RECOVERY_CONFLICT**

Use when repository reality conflicts with expected architecture or user work in a way that prevents safe continuation.

## 27. Required Completion Report

Report:

1. Source target
2. Destination module(s)
3. Foundation readiness per destination
4. Foundation created/extended
5. Responsibility split
6. Ownership map
7. Files moved/created/deleted
8. Old -> new path map
9. Namespace changes
10. Contracts reused/created
11. Illegal references removed
12. Cross-module joins removed
13. Replacement communication mechanism
14. CQRS/MediatR state
15. Validation matrix
16. Endpoint ownership
17. Localization state (codes, catalog, resources)
18. API result/error mapping state
19. Logging/telemetry state (including sensitive-data check)
20. Correlation/trace continuity state
21. File cohesion / decomposition performed
22. Host residue/authority
23. Persistence/schema state
24. DI/composition updates
25. Behavior-preservation evidence
26. Focused builds/tests
27. Guards added/updated
28. Residual debt
29. Certification readiness

## 28. Hard Rules

- Never move into an invalid destination structure.
- If destination foundation is missing, build the minimum correct foundation first.
- Never create a parallel architecture beside an existing valid one.
- Never move a mixed-responsibility file wholesale when it must be split.
- Correct ownership is more important than minimal diff.
- Contracts-only cross-module communication.
- No cross-module DB joins.
- No foreign DbContext access.
- No foreign Application/Infrastructure/Domain dependencies.
- No endpoint business logic.
- No Host business/persistence ownership.
- No behavior redesign during architecture migration.
- No schema change unless explicitly authorized.
- No compatibility shim hiding incorrect structure.
- No namespace alias hiding incorrect placement.
- No new parallel localization/response/logging/telemetry mechanism.
- No sensitive-data logging.
- No cosmetic god-file splitting and no god-file creation.
- Preserve user work.
- Verify before claiming readiness.
