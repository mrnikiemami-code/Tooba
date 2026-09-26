---
name: tooba-architecture-analyze
description: Analyze one or more Tooba files/capabilities, determine true module ownership, detect cross-module coupling, non-canonical API/localization/logging/tracing/cohesion patterns, and microservice blockers, and produce a behavior-preserving migration plan before any code is moved.
---

# Tooba Architecture Analyze (V2)

Use this skill when the user asks to analyze a Host file, legacy service, composer, endpoint, directory, capability, or mixed-responsibility code and determine where it belongs in the Tooba modular architecture.

This skill is ANALYSIS-ONLY unless the user explicitly asks to migrate.

## Primary Goal

Determine the real architectural ownership of the target code without guessing and without moving code prematurely.

The output must identify:
- which Tooba module owns each responsibility;
- whether one source file must be split across multiple modules or into cohesive files;
- which cross-module dependencies are legal through Contracts;
- which dependencies violate module boundaries;
- whether any cross-module database join, DbContext reach-through, navigation, direct table access, or foreign persistence coupling exists;
- whether the target follows the repository's **canonical** localization, API result/error mapping, structured logging, and OpenTelemetry/correlation mechanisms;
- whether the file is oversized or a god-file with unrelated responsibilities;
- the exact target structure for later migration;
- behavior that must remain unchanged.

## Required Repository Context

Before analyzing, read the relevant repository architecture sources of truth if present:

- `AGENTS.md`
- `docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md`
- `docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md`
- `docs/architecture/TMAR-architecture-locks.md`
- `docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md`
- `docs/architecture/tmar-current-state.json`
- `docs/architecture/tmar-module-structure-manifests.json`

Treat current repository state as authoritative over chat memory.

## Canonical Mechanism Discovery (MANDATORY, BEFORE JUDGING)

Do **not** assume type names or invent abstractions. Before classifying anything as non-canonical, search the repository for the **existing** canonical mechanism for each concern. If a mechanism already exists, the target must use it; if the target uses a parallel mechanism, that is a finding.

Discover, at minimum:

1. **API result/error mapping** — look for a central response factory / result-to-HTTP mapper / ProblemDetails mapper and its registration. In this repository the canonical mechanism is:
   - `Tooba.BuildingBlocks.Results.Result` / `Result<T>` (`IsSuccess` / `IsFailure` / `Errors` carrying `SemanticError`).
   - `Tooba.BuildingBlocks.Presentation.ApiResponseFactory` (`From(Result)`, `From<T>(Result<T>)`, `Created`, `FromFailure`, `FromException`, `FromSemanticException`, `FromPlatformException`).
   - `Tooba.BuildingBlocks.Presentation.Errors.SafeErrorMapper` + `IErrorDefinitionCatalog` + `ErrorDescriptor` + `IErrorCatalogContributor`.
   - `Tooba.BuildingBlocks.Presentation.ProblemDetails.IProblemDetailsContextProvider`.
   - `Tooba.BuildingBlocks.Presentation.IExceptionPresentationService` (global Host exception boundary).
2. **Localization / user-facing text** — look for resource-based localization, error catalog with `LocalizationKey`, and `IErrorMessageLocalizer`. In this repository the canonical mechanism is:
   - machine-stable codes in `*.Contracts.Errors.<Module>ErrorCodes` and validation codes in `*.Application.Validators.<Module>ValidationCodes`;
   - `IErrorCatalogContributor` + `IErrorResourceSet` (e.g. `OfferErrorCatalogContributor`, `OfferErrorResourceSet`);
   - `.resx` + `.resx` localized resources (e.g. `OfferErrors.resx`, `OfferErrors.fa.resx`);
   - `IErrorMessageLocalizer` / `ResourceErrorMessageLocalizer` / `IRequestLocaleResolver`.
3. **Observability / logging** — look for the logging foundation and log scope. In this repository:
   - `Tooba.BuildingBlocks.ToobaTelemetry` (`ActivitySource` / `Meter` named `Tooba`);
   - `Tooba.BuildingBlocks.Observability.Logging.ObservabilityLogScope` + `ObservabilityLogScopeKeys`;
   - `LoggingBehavior<,>` pipeline in `TmarFoundation`.
4. **OpenTelemetry / trace / correlation** — look for trace source, correlation provider, and the module-call tracer. In this repository:
   - `Tooba.BuildingBlocks.Observability.Correlation` (`ICorrelationIdProvider`, `CorrelationIdContext`, `CorrelationIdConstants.HeaderName = X-Correlation-Id`, `CorrelationIdMiddleware`);
   - `Tooba.BuildingBlocks.Observability.Tracing` (`IModuleCallTracer` / `ModuleCallTracer`, `TracingBehavior<,>`, `ModuleCallTrace`);
   - `ProblemDetailsContextProvider` which supplies `traceId`, `correlationId`, `requestId` from canonical sources.
5. **Source-size / cohesion guard** — look for the size baseline and guard. In this repository:
   - `src/backend/Host/Tooba.Host.Tests/Baselines/tmar-source-size-baseline.json`, `TmarSourceSizeGuard.cs`, `TmarSourceSizeAndInfraAppTests.cs`, plus module guards (`ARCH-SIZE-001`, `ARCH-MODULE-FILE-001`).

If a concern's canonical mechanism is demonstrated by the reference module `src/backend/Modules/Offer`, use Offer as the reference. If another canonical module or BuildingBlocks mechanism is clearly authoritative for that concern, use that instead. Never invent a parallel mechanism.

## Analysis Procedure

### 1. Read the Target Completely

Inspect the target file/folder and all directly related:
- constructors;
- injected services;
- interfaces;
- implementations;
- routes;
- DTOs;
- commands/queries;
- handlers;
- validators;
- persistence calls;
- EF DbContexts;
- SQL;
- callers;
- consumers;
- registrations;
- tests;
- background workers;
- options;
- adapters;
- contracts;
- user-facing strings and resource keys;
- logging/telemetry calls.

Do not decide ownership from filename alone.

### 2. Build a Responsibility Map

Classify every significant responsibility as one of:

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

If one file contains multiple ownership domains, mark it `MUST_SPLIT`.

### 3. Determine True Module Ownership

Assign each responsibility to the module that owns the business capability.

Examples:
- payment decision/state -> Payment
- order lifecycle/state -> Order
- cart state -> Cart
- offer selection -> Offer
- address ownership/address lifecycle -> AddressBook
- authentication identity/authentication mechanisms -> Authentication
- cross-module UI aggregation may remain a thin composition layer only if it owns no business policy.

Never move code into a module merely because that module happens to call it most often.

### 4. Detect Forbidden Coupling

Find and report all cross-module references of these forms:

- `OtherModule.Application`
- `OtherModule.Infrastructure`
- `OtherModule.Domain`
- foreign DbContext usage
- foreign DbSet access
- direct foreign repository/store/directory implementation usage
- EF navigation crossing module boundaries
- SQL joins across module-owned schemas/tables
- cross-module transaction assumptions
- shared mutable entities
- Host-owned business policy
- Host-owned persistence policy
- endpoint-to-infrastructure direct calls

Treat these as microservice blockers unless explicitly permitted by the current architecture standard.

### 5. Contracts-Only Boundary Design

For each required cross-module interaction, decide the smallest lawful boundary.

Prefer one of:
- existing `OtherModule.Contracts`
- new narrow Contracts DTO
- new narrow Contracts port
- integration event
- local projection/read model
- immutable snapshot
- identifier + contract lookup

Never solve coupling by:
- adding Application-to-Application references;
- adding Infrastructure-to-Infrastructure references;
- adding Domain-to-Domain references;
- exposing DbContext;
- exposing internal entities;
- introducing a shared god-contract;
- preserving a cross-module SQL/EF join.

### 6. No Cross-Module Join Rule

Explicitly search for:
- LINQ joins using two module persistence sources;
- foreign DbSet access;
- FromSql/raw SQL joining module-owned tables;
- navigation properties that require foreign module persistence;
- query handlers reading another module's database directly.

If found, classify each join and propose a replacement:
- contract query;
- local replicated projection;
- integration event;
- snapshot;
- orchestration with IDs;
- dedicated read model at an explicitly approved boundary.

Do not implement the replacement in analyze mode.

### 7. CQRS / MediatR Readiness

For HTTP-reachable behavior, determine whether the target should become:
- Command;
- Query;
- `IRequest<T>`;
- real `IRequestHandler<,>`;
- dispatched from Endpoints through `ISender`.

Flag:
- endpoint business logic;
- direct Directory/DbContext calls from endpoints;
- generic/custom dispatchers replacing MediatR;
- Host bypasses around module CQRS.

The reference module `src/backend/Modules/Offer` demonstrates the canonical shape: real `IRequest<T>`/`IRequestHandler<,>`, `ISender` in Endpoints, `ApiResponseFactory api` injected into endpoints, and `AddToobaCqrsFoundation` registration (MediatR 12.5.0). Use it as reference for shape only — do not copy Offer-specific business concepts.

### 8. Validation Classification

Classify each endpoint-reachable request as:
- `VALIDATOR_REQUIRED`
- `NO_VALIDATOR_REQUIRED`

Use FluentValidation only for transport/input shape. Validators emit **stable machine codes** (e.g. `offer.validation.*`), never localized text.

Do not duplicate:
- DB existence rules;
- ownership rules;
- authorization rules;
- domain invariants;
- workflow state rules;
- business decisions.

These belong in Application/Domain.

### 9. Localization / User-Facing Text Audit

Detect and report:

- hard-coded Persian user-facing strings in production C# (domain/application/endpoints/infrastructure);
- hard-coded English user-facing strings used as client messages;
- duplicated localized messages appearing in more than one place;
- response/error decisions made by parsing `exception.Message` or `ex.Message`;
- missing use of the existing localization infrastructure (`IErrorMessageLocalizer`, `IErrorResourceSet`, `.resx`);
- hard-coded `Accept-Language` parsing in endpoints instead of `IRequestLocaleResolver`.

Rules to enforce:
- Stable machine error codes may exist in code (correct and expected).
- User-facing Persian/English text must come from the canonical resource mechanism.
- Never treat `exception.Message` as a localized/user-facing contract.
- Never introduce Persian/English hard-coded API messages when the localization mechanism exists.
- Never invent a second localization system.
- Existing localization keys and their semantics must be preserved.

### 10. API Result / Error Mapping Audit

Identify whether every endpoint follows the repository's canonical result/error path.

Report:
- endpoints that return raw `Results.Json(...)` / `Results.BadRequest(...)` / `Results.Problem(...)` instead of `ApiResponseFactory`;
- endpoints that build local `ProblemDetails` mappers;
- endpoints that catch exceptions and map them ad hoc;
- failure classification done by parsing `ex.Message` / `ex.Message is` / `when (ex.Message...)`;
- stable error codes that are not registered in an `IErrorCatalogContributor`;
- unknown/unexpected exceptions silently converted into business failures;
- success DTOs whose shape/envelope differs from the shipped contract (e.g. Offer seller success is intentionally raw DTO, not an envelope).

### 11. Observability / Logging Audit

Report:
- non-standard logging (`Console.WriteLine`, `Debug.WriteLine`, custom logger frameworks, hand-rolled file writers);
- sensitive-data logging risks: passwords, refresh/access tokens, OTP secrets, reset secrets, `Authorization` headers, cookies, session secrets, security stamps, private credentials, full query strings, payment payloads;
- duplicated/competing telemetry abstractions (second `ActivitySource`, second `Meter`, second correlation provider);
- missing reuse of the logging foundation (`ObservabilityLogScope`, `ObservabilityLogScopeKeys`, `ILogger<T>`);
- unstructured/string-concatenated log messages instead of structured templates.

### 12. OpenTelemetry / Trace / Correlation Audit

The canonical correlation identity is `X-Correlation-Id` via `ICorrelationIdProvider`, and the canonical trace identity is the W3C `Activity.TraceId`/`SpanId` exposed through `ProblemDetailsContextProvider`.

Report:
- custom/parallel correlation mechanisms (own header, own AsyncLocal, own middleware);
- manually generated IDs (`Guid.NewGuid()` used as correlation) that conflict with canonical trace context;
- lost trace propagation across module calls (missing `IModuleCallTracer` decoration where the module issues cross-module calls);
- telemetry paths that bypass the standard mechanism (`StartActivity` directly, parsing `traceparent` manually);
- ProblemDetails responses that do not use the canonical trace/correlation source.

Do not implement fixes in analyze mode.

### 13. File Size / Cohesion / Safe-Splitting Audit

A file is NOT acceptable merely because it is under a hard LOC ceiling.

Audit for:
- physical size and position against `tmar-source-size-baseline.json`;
- number of unrelated responsibilities;
- multiple unrelated top-level types with different reasons to change;
- endpoint + request/response models + infrastructure + business logic mixed in one file;
- god-file symptoms (deep regions, dozens of private methods, mixed audiences Admin/Seller/Customer/Storefront).

Classify each oversized/mixed file as:
- `OVERSIZED_ONLY` (single cohesive responsibility, above ceiling);
- `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (must be split by responsibility);
- `LEGITIMATE_HOST_PLATFORM_FILE` (split within Host, do not migrate into a business module).

Do not plan cosmetic splitting that creates meaningless tiny files. Do not use file splitting as an excuse for ownership migration, and vice versa.

### 14. Target Foldering

Propose exact physical target paths and namespaces.

Preferred patterns:

`Application/<Capability>/<UseCase>/...`
`Application/<Capability>/Models/...`
`Application/<Capability>/Ports/...`
`Application/Validators/<Capability>/<UseCase>/...`

`Endpoints/Admin/...`
`Endpoints/Seller/...`
`Endpoints/Customer/...`
`Endpoints/Storefront/...`
`Endpoints/Errors/...` (error catalog contributor)
`Endpoints/Resources/...` (resx + resource set)

`Infrastructure/Persistence/...`
`Infrastructure/Directories/...`
`Infrastructure/Adapters/...`
`Infrastructure/Workers/...`
`Infrastructure/Development/...`

`Contracts/<Capability>/...`
`Contracts/Errors/...` (stable error codes)

Namespace must exactly match physical path.

### 15. Behavior-Preservation Baseline

Before any future migration, enumerate behavior that must remain unchanged:

- routes;
- HTTP methods;
- response shapes;
- status codes;
- stable error codes;
- request/response DTO semantics;
- authorization semantics;
- authentication/session semantics;
- business rules;
- state transitions;
- ordering;
- cancellation;
- idempotency;
- transaction behavior;
- persistence semantics;
- schema;
- migration IDs and Up/Down behavior;
- table/column/index/constraint semantics;
- background worker cadence;
- seed values;
- feature flags;
- telemetry event names and semantic dimensions;
- correlation/trace behavior;
- localization keys and their semantics;
- tenant/store scoping;
- public Contracts.

### 16. Foundation / Certified-Module State

For every target module classify foundation state:

- `FOUNDATION_READY` — the module already has the required projects/folders and can receive the capability safely. Treat a module with `structureCertified: true` (or equivalent canonical certification in `tmar-module-structure-manifests.json`) as `FOUNDATION_READY`. Do not create parallel structure beside a certified module.
- `FOUNDATION_PARTIAL` — some required projects/capability folders exist, but the destination needed is missing or structurally invalid. Plan only the minimum missing standard structure required.
- `FOUNDATION_MISSING` — the module has no safe destination architecture. Plan the minimum correct foundation; do not dump into a legacy root.

If the correct foundation requires a broad module-wide redesign rather than the bounded migration, plan disposition `FOUNDATION_REQUIRES_SEPARATE_BOUNDED_TASK`.

## Required Output

Return an architecture migration plan containing all of the following state fields and narrative sections:

### Structured State Fields

1. **Foundation-State** — `FOUNDATION_READY` | `FOUNDATION_PARTIAL` | `FOUNDATION_MISSING` (per target module)
2. **Ownership-State** — correct | MUST_SPLIT | UNKNOWN_OWNER
3. **File-Cohesion-State** — `COHESIVE` | `OVERSIZED_ONLY` | `MULTI_RESPONSIBILITY_COHESION_VIOLATION`
4. **Oversized/God-File-State** — list with LOC and classification
5. **Localization-State** — `CANONICAL` | `HARDCODED_TEXT` | `EXCEPTION_MESSAGE_BASED` | `MISSING_INFRASTRUCTURE_USE`
6. **API-Result-Pattern-State** — `CANONICAL` | `AD_HOC` | `RAW_RESULTS` | `PARALLEL_MAPPER`
7. **Stable-Error-Code-State** — `CATALOGUED` | `UNREGISTERED_CODES` | `STRING_HEURISTIC`
8. **Logging-State** — `CANONICAL` | `NON_STANDARD` | `DUPLICATE_TELEMETRY`
9. **Sensitive-Logging-State** — `NONE` | `RISK_FOUND` (list exact locations)
10. **OpenTelemetry-State** — `CANONICAL` | `BYPASSED` | `SECOND_PIPELINE`
11. **Correlation-Trace-State** — `CANONICAL` | `PARALLEL_CORRELATION` | `LOST_PROPAGATION`
12. **CQRS-State** — `COMPLIANT` | `PARTIAL` | `MISSING`
13. **Validator-Coverage-State** — `EXHAUSTIVE` | `GAPS` (with matrix)
14. **Contracts-Boundary-State** — `CLEAN` | `VIOLATION`
15. **Cross-Module-Coupling-State** — `NONE` | `LEGAL_CONTRACTS_ONLY` | `ILLEGAL` (list)
16. **Cross-Module-Join-State** — `NONE` | `FOUND` (list)
17. **Persistence-Ownership-State** — `CORRECT` | `FOREIGN_ACCESS` | `HOST_OWNED`
18. **Endpoint-Ownership-State** — `MODULE_OWNED` | `HOST_OWNED` | `DUPLICATED`
19. **Host-Residue-State** — classification list (see below)
20. **Schema-Migration-State** — `UNCHANGED` | `DRIFT_RISK` (with evidence)
21. **Behavior-Preservation-Risk** — `LOW` | `MEDIUM` | `HIGH` (with justification)
22. **Canonical-Reference-Used** — which canonical mechanism/module was used per concern (Offer or BuildingBlocks or other named module)
23. **Final-Disposition** — one of the bounded dispositions below

### Narrative Sections

24. Target analyzed
25. Responsibility map
26. Ownership map
27. MUST_SPLIT decisions
28. Current illegal dependencies
29. Cross-module join inventory
30. Contracts-only replacement map
31. CQRS/MediatR gaps
32. Validation classification matrix
33. Localization findings
34. API result/error mapping findings
35. Logging/sensitive-data findings
36. OpenTelemetry/correlation findings
37. File cohesion / splitting plan
38. Exact target paths/namespaces
39. Behavior-preservation checklist
40. Migration order
41. Verification plan
42. Certification blockers

### Bounded Final Dispositions

Return exactly one:

- `READY_TO_MIGRATE`
- `KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE`
- `KEEP_AS_GLOBAL_HOST_PLATFORM_BOUNDARY`
- `FOUNDATION_REQUIRES_SEPARATE_BOUNDED_TASK`
- `NEEDS_ARCHITECT_DECISION`
- `RECOVERY_CONFLICT`
- `BLOCKED_BY_UNKNOWN_BEHAVIOR`

## Hard Rules

- Do not move code in this skill unless the user explicitly requests migration.
- Do not invent a new module if an existing owner is correct.
- Do not preserve illegal coupling for convenience.
- Do not redesign business behavior.
- Do not introduce cross-module joins.
- Do not use compatibility shims to hide wrong ownership.
- Do not use namespace aliases to hide wrong folder structure.
- Do not claim microservice readiness while direct cross-module persistence remains.
- Discover the repository's canonical mechanism before declaring anything non-canonical.
- Never propose a parallel localization, response/error, logging, or telemetry system.
- Never treat a file as acceptable solely because it is under a LOC ceiling.
- Never classify a legitimate retained Host platform file for migration merely because it lives in Host.
- Never chase textual Host-reference ZERO by moving legitimate platform seams into a business module.
