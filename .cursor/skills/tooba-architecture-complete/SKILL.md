---
name: tooba-architecture-complete
description: Actively complete and harden a Tooba destination module after Host evacuation so ownership relocation is not mistaken for architectural completion. Reads Analyze → Migrate → Certify first, then repairs CQRS/MediatR, validators, Result/ApiResponseFactory, stable errors/localization, logging/telemetry/correlation, foldering, Contracts-only boundaries, guards, and COMPLETE_REFERENCE_PATTERN readiness.
---

# Tooba Architecture Complete

Use this skill when a Host folder has been evacuated or a module has received migrated code, but the destination is still structurally non-canonical.

This skill exists because:

```text
Host evacuation != module completion
moved code != recovered architecture
green build != certification
```

It is an **active repair skill**. Unlike certify, it does not stop merely because certification finds defects that are locally repairable inside the bounded destination-module surface. It must repair those defects, then run certification again.

## 0. Mandatory skill chain

Before doing any work, read the current repository versions of:

1. `.cursor/skills/tooba-architecture-analyze/SKILL.md`
2. `.cursor/skills/tooba-architecture-migrate/SKILL.md`
3. `.cursor/skills/tooba-architecture-certify/SKILL.md`

Then use this skill as the completion layer above them.

Repository reality is authoritative over chat memory.

## 1. Mission

Given one bounded destination module/capability:

1. recover current architecture state;
2. analyze the entire touched destination surface;
3. actively repair all locally resolvable canonical-architecture defects;
4. certify the resulting state;
5. return PASS only when the module surface satisfies all applicable COMPLETE_REFERENCE_PATTERN requirements.

The goal is not to make Host text disappear. The goal is to ensure responsibility lands in the **right architecture**.

## 2. Scope discipline

The active unit is one destination module/capability created or changed by the current Host evacuation.

Allowed outside scope only when required for:
- direct Host composition/security seams;
- direct caller/call-site repair;
- the smallest Contracts boundary with another module;
- BuildingBlocks canonical mechanisms;
- focused tests/guards/evidence/SoT;
- solution/project metadata.

Do not:
- scan every Host folder;
- start the next Host folder;
- independently recover another module;
- redesign product behavior;
- change schema/migrations unless explicitly authorized;
- touch frontend under BACKEND_ONLY mode.

If a required repair would need a product/data/schema/public-contract decision outside the bounded task, STOP with `NEEDS_ARCHITECT_DECISION`.

## 3. Completion is stricter than evacuation

A destination module is NOT complete if any of these remain:

- Endpoints directly reference Infrastructure;
- Endpoints directly use DbContext/DbSet/repository/directory implementation;
- endpoint business behavior bypasses MediatR/ISender;
- endpoint-reachable requests are not exhaustively validator-classified;
- raw/ad-hoc `Results.Json` error mapping replaces the canonical Result pipeline;
- expected failures are inferred from `Exception.Message`;
- stable error codes lack exactly one canonical descriptor owner;
- user-facing error text is hard-coded instead of localized through resources;
- module directly references another module's Application/Infrastructure/Domain;
- cross-module DbContext/DbSet/raw SQL/join exists;
- logging bypasses canonical structured logging or leaks sensitive data;
- tracing/correlation creates a parallel mechanism or loses canonical context;
- folders are root dumps / namespaces do not match paths;
- touched files are god-files or cosmetic splits;
- Host business/persistence authority remains without explicit lock;
- SoT says debt is deferred for an item required by certification.

A PASS with any applicable item above is forbidden.

## 4. Canonical dependency shape

For an HTTP-owning module, target:

```text
Endpoints
  -> Application
  -> Contracts / Domain

Infrastructure
  -> Application / Contracts / Domain
  -> foreign module Contracts only

Host
  -> module Endpoints/Infrastructure only for composition/security adapters
```

Forbidden:

```text
Endpoints -> Infrastructure
Application -> foreign Application/Infrastructure/Domain
Infrastructure -> foreign Application/Infrastructure/Domain
Module -> Host
foreign DbContext/DbSet/raw SQL
cross-module EF navigation/join
```

A narrow cross-module contract is preferred over a shared god abstraction.

## 5. HTTP ownership and CQRS

Inventory every endpoint-reachable behavior.

Every business use case must be:

```text
Endpoint
-> ISender
-> IRequest<Result<T>> / IRequest<Result>
-> IRequestHandler<,>
-> module Application port / domain logic
-> Infrastructure implementation
```

Requirements:
- MediatR repository-standard version (currently 12.5 where locked);
- real handlers, no generic custom dispatcher;
- no business workflow in endpoint;
- no direct persistence/directory implementation from endpoint;
- no composer acting as a CQRS bypass.

A thin transport mapper/assembler may remain only when it is persistence-free and owns no business policy.

## 6. Validator completeness

Create an exhaustive endpoint-reachable request matrix.

Every request is exactly one of:

- `VALIDATOR_REQUIRED`
- `NO_VALIDATOR_REQUIRED`

For required:
- concrete FluentValidation validator;
- stable machine validation codes;
- discovered through canonical CQRS foundation;
- no manual validation call in endpoints.

For no-validator:
- explicit durable reason.

Add a durable guard so a newly endpoint-reachable request cannot silently escape classification.

## 7. Canonical Result / HTTP presentation

Expected business/application failures must use:

- `Result`
- `Result<T>`
- `SemanticError`
- canonical `ApiResponseFactory`

Endpoints should normally reduce to:

```csharp
var result = await sender.Send(request, cancellationToken);
return api.From(result);
```

Use canonical Created/NoContent helpers or repository-established equivalent where needed while preserving response shape/status semantics.

Do not maintain:
- anonymous error JSON objects;
- duplicated status/error mapping;
- endpoint-owned ProblemDetails variants;
- broad `InvalidOperationException` catches for expected outcomes.

Unknown exceptions propagate to the global exception boundary.

## 8. Zero message classification

For expected outcomes, search touched surface for:

- `.Message.Contains(`
- `.Message.StartsWith(`
- `.Message ==`
- regex/message heuristics;
- localized-message branching.

Replace with typed/stable failures:
- `Result` / `SemanticError`;
- `ContractOperationException(Code)` only at a lawful Contracts boundary when that is the repository pattern;
- typed fault DTO where already canonical.

Never classify business meaning from human-readable exception text.

## 9. Stable errors and localization

For each module-owned machine code:

- natural module owns the code;
- exactly one canonical descriptor owner;
- `IErrorCatalogContributor`;
- `IErrorResourceSet`;
- default `.resx`;
- localized `.fa.resx` where applicable;
- `IErrorMessageLocalizer`;
- no duplicate descriptor ownership;
- no localized text embedded in Application/Endpoints error creation.

Invariant:

```text
duplicate usage allowed
duplicate descriptor ownership forbidden
```

Do not create Shared Errors simply because multiple modules consume a code.

## 10. Authorization boundary

Separate:
- Host/global platform authentication/session runtime seams;
- module endpoint authorization adapters;
- module business rules.

Endpoint authorization should:
- use neutral security/authorization abstractions;
- expose narrow module-specific authorization interface where useful;
- preserve capability/permission IDs;
- return stable deny/unavailable machine codes;
- avoid localized response strings;
- avoid repeated raw authorization choreography in every endpoint when one cohesive adapter is appropriate.

Authorization ownership exceptions do not permit module business/persistence leakage.

## 11. Cross-module Contracts-only repair

For every foreign dependency:

Allowed:
- foreign `*.Contracts`.

Forbidden:
- foreign `*.Application`;
- foreign `*.Infrastructure`;
- foreign `*.Domain`;
- foreign DbContext/DbSet;
- foreign repository implementation;
- cross-module SQL/EF join.

If the needed contract does not exist:
- create the smallest narrow contract in the natural owning module's Contracts project;
- implement it in the owning module;
- bind via DI/composition;
- do not start independent recovery of that module.

## 12. Foldering and cohesion

Folder by responsibility/capability/use case.

Typical patterns when semantically appropriate:

- `Application/Commands/<UseCase>/`
- `Application/Queries/<UseCase>/`
- `Application/Validators/<AudienceOrCapability>/`
- `Application/Ports/<Capability>/`
- `Contracts/Errors/`
- `Contracts/Dtos/`
- `Contracts/Ports/`
- `Endpoints/Admin/`
- `Endpoints/Storefront/`
- `Endpoints/Errors/`
- `Endpoints/Resources/`
- `Infrastructure/Directories/`
- `Infrastructure/Grid/`
- `Infrastructure/Adapters/`
- `Infrastructure/Development/`

These are examples, not a physical template. Responsibility chooses the folder.

Requirements:
- exact physical path ↔ namespace;
- explicit root allowlists;
- no stale duplicate copy;
- no alias/type-forwarding workaround;
- no god-file;
- no cosmetic split to game LOC guards;
- preserve solution folder grouping.

## 13. Logging

Discover the current canonical logging mechanism first.

Verify touched production code:
- uses `ILogger<T>`;
- structured templates, not concatenated sensitive values;
- no secrets/tokens/passwords/OTP/full private payloads;
- no business audit pretending to be technical log;
- canonical `ObservabilityLogScope` when contextual scope is needed;
- no parallel logger abstraction.

Do not add logs just to satisfy a checklist. Add only operationally useful logs.

## 14. OpenTelemetry / tracing / correlation

Discover and reuse:
- `ToobaTelemetry`;
- `TracingBehavior<,>`;
- `IModuleCallTracer`;
- `ICorrelationIdProvider`;
- `CorrelationIdMiddleware`;
- canonical `X-Correlation-Id`.

Verify:
- CQRS path participates in canonical tracing;
- direct cross-module contract calls use the established module-call tracing pattern when applicable;
- no module-local parallel Meter/ActivitySource unless explicitly canonical;
- no invented correlation ID;
- response/problem context keeps canonical trace/correlation continuity.

If canonical pipeline already supplies everything, evidence should say no module-specific telemetry code was needed.

## 15. Development seed/composition rule

A Host development/composition file may remain only when it is genuinely orchestration.

Module-specific seed behavior belongs to the module, normally Infrastructure/Development.

Host may:
- choose environment/edition;
- create scope;
- call module-owned seed/migration entry point;
- sequence modules.

Host must not:
- create module domain entities;
- encode module seed business rules;
- inspect/mutate module tables as business logic.

## 16. Source-size and structure guards

Apply current repository:
- `TmarSourceSizeGuard`;
- source-size baseline;
- module structure guards;
- exact root allowlists;
- forbidden top-level folders;
- validator coverage guard;
- error/localization uniqueness guard;
- Host ownership guard;
- cross-module dependency guard.

Never weaken a guard to achieve PASS.

## 17. Active repair loop

This skill may repair defects found during certification **within the bounded module surface**.

Procedure:

1. Analyze completely.
2. Produce blocker matrix.
3. Repair all deterministic in-scope blockers.
4. Run focused validation.
5. Re-read every touched production file.
6. Run certification.
7. If certification finds another deterministic in-scope defect, perform at most ONE bounded repair pass for that defect class and rerun only affected checks.
8. If still failing or scope becomes ambiguous, STOP.

No open-ended test/repair loop.

## 18. Required evidence

Persist evidence for:
- before/after physical tree;
- project dependency graph;
- endpoint inventory;
- request → handler → validator matrix;
- no-validator reasons;
- Result/ApiResponseFactory proof;
- message-classification ZERO proof;
- stable error code → descriptor → resource mapping;
- localization coverage;
- authorization ownership;
- logging/sensitive-data audit;
- OpenTelemetry/correlation audit;
- foreign dependency inventory;
- no cross-module persistence/join proof;
- seed/composition ownership;
- source-size/cohesion;
- focused builds/tests;
- residual debt;
- exact verdict.

## 19. PASS gate

Return PASS only when every applicable item is true:

- ownership correct;
- Host business/persistence residue for active capability resolved;
- endpoint ownership module-owned;
- Endpoints has no Infrastructure dependency;
- endpoint business use cases dispatch via MediatR `ISender`;
- handlers return canonical `Result` / `Result<T>`;
- exhaustive validator classification;
- canonical ApiResponseFactory;
- stable errors/localization complete;
- zero expected-failure message parsing;
- foreign module dependencies are Contracts-only;
- no cross-module persistence/join;
- logging canonical and sensitive-data-safe;
- tracing/correlation canonical;
- path/namespace exact;
- foldering/cohesion/root allowlists valid;
- no shim/alias workaround;
- schema/migration/behavior preserved unless explicitly authorized;
- focused validation passes;
- SoT/evidence honest.

If any required item is deliberately deferred, status is NOT PASS.

## 20. Output

Report at minimum:

- `Analyze-State`
- `Ownership-State`
- `Project-Dependency-State`
- `Endpoint-Ownership-State`
- `CQRS-State`
- `MediatR-State`
- `Endpoint-Reachable-Request-Count`
- `Validator-Coverage-State`
- `Result-Pipeline-State`
- `ApiResponseFactory-State`
- `Message-Classification-State`
- `Authorization-State`
- `Error-Catalog-State`
- `Localization-State`
- `Logging-State`
- `OpenTelemetry-State`
- `Correlation-State`
- `Cross-Module-Boundary-State`
- `Cross-Module-Persistence-State`
- `Path-Namespace-State`
- `Root-Allowlist-State`
- `Source-Cohesion-State`
- `Schema-Migration-State`
- `Focused-Validation`
- `Remaining-Blockers`
- `Residual-Debt`
- `Certification-Verdict`

## Hard rules

- Host evacuation alone is never sufficient evidence of completion.
- Green build alone is never sufficient.
- Moved files are not presumed canonical.
- Never PASS with Endpoints -> Infrastructure.
- Never PASS with foreign Application/Infrastructure/Domain coupling.
- Never PASS with message-based expected-failure classification.
- Never PASS with unclassified endpoint-reachable validation.
- Never PASS with ad-hoc expected-failure HTTP presentation when canonical Result/ApiResponseFactory applies.
- Never create a new generic shared layer to hide ownership.
- Never change business behavior just to fit the pattern.
- Never start the next Host folder.
- Verification before declaration.
