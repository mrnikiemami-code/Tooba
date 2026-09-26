---
name: tooba-architecture-certify
description: Certify a Tooba module against ARCH-COMPLETE-002 after migration, verifying structure, file cohesion, CQRS/MediatR, exhaustive validation coverage, module endpoint ownership, Contracts-only boundaries, no cross-module persistence/joins, canonical localization/API-result/logging/telemetry, trace continuity, schema preservation, durable guards, manifest state, and recovery SoT without changing business behavior.
---

# Tooba Architecture Certify (V2)

Use this skill only after a module/capability migration is believed complete.

This skill verifies and locks architecture. It is not a redesign task.

## Certification Standard

Use the current repository sources of truth, especially:

- `AGENTS.md`
- `docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md`
- `docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md`
- `docs/architecture/TMAR-architecture-locks.md`
- `docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md`
- `docs/architecture/tmar-current-state.json`
- `docs/architecture/tmar-module-structure-manifests.json`

Repository reality wins over assumptions.

## Canonical Mechanism Verification (MANDATORY)

Certification must confirm the migrated surface uses the repository's **existing** canonical mechanisms, not a parallel invention. Verify against current repository state (do not trust memorized names):

- API result/error mapping: `ApiResponseFactory` + `SafeErrorMapper` + `IErrorDefinitionCatalog`.
- Localization: module `IErrorResourceSet` + `.resx` resources + `IErrorMessageLocalizer`.
- Stable error codes: `Contracts.Errors.<Module>ErrorCodes` catalogue-backed.
- Logging: `ILogger<T>` + `ObservabilityLogScope`.
- Tracing/correlation: `ToobaTelemetry`, `IModuleCallTracer`, `ICorrelationIdProvider` (`X-Correlation-Id`).
- CQRS foundation: `AddToobaCqrsFoundation`.
- Size/cohesion guard: `TmarSourceSizeGuard` + baseline.

If the canonical mechanism for a concern is demonstrated by `src/backend/Modules/Offer`, Offer is the reference; if another module/BuildingBlocks is authoritative, use that. Do not force Offer-specific business concepts into other modules.

## Certification Preconditions

Do not certify unless all applicable conditions hold:

- correct module ownership;
- correct foundation usage (no parallel architecture beside a certified module);
- correct capability-oriented structure/foldering;
- exact path↔namespace equality;
- files physically exist with no stale/duplicate copy and required solution grouping preserved;
- file cohesion with no new oversized/god file;
- Host business ownership removed;
- Host persistence ownership removed;
- module endpoint ownership established;
- CQRS uses MediatR;
- endpoints dispatch with `ISender`;
- validator coverage classified and guarded;
- root allowlists defined;
- no namespace alias workaround;
- cross-module dependencies use Contracts only;
- no cross-module persistence access;
- no cross-module SQL/EF joins;
- canonical localization compliance;
- canonical API result/error mapping;
- stable error codes catalogue-backed;
- canonical structured logging;
- no sensitive-data logging;
- OpenTelemetry/correlation continuity;
- schema/migration preservation;
- required focused builds/tests pass;
- durable architecture guards pass;
- SoT and manifest can be made honest.

If any condition fails, return blockers and do not claim certification.

### 0. Ownership ≠ Quality

Ownership and quality are independent. Host-owned ≠ quality-exempt.

A canonical ownership exception applies **only** to ownership/location/runtime responsibility. It never legalizes direct foreign Application/Infrastructure/Domain coupling, foreign DbContext/DbSet access, cross-module persistence joins, non-canonical API/error mapping, unregistered error codes, localization violations, observability/correlation violations, or cohesion/foldering violations.

Certification must never claim `LEGAL_CONTRACTS_ONLY` while any direct foreign Application/Infrastructure/Domain dependency remains. An ownership exception is not a quality exception.

### 0a. Touched-Surface Certification

Any production file changed by the current task is part of the active certification surface. Before PASS, re-read every touched production file and verify: cohesive responsibility; correct capability folder; exact path↔namespace alignment; no root dump unless explicitly allowed; no obsolete/duplicate type; no hard-coded user-facing localized text; no foreign Application/Infrastructure/Domain leakage; no parallel canonical mechanism; no unintended behavior/schema change.

Do NOT expand this into a full-module audit: cover only touched files, directly affected dependencies, and the minimum destination-module surface required by the active Host-folder task. A task is NOT complete merely because the original dependency was fixed, focused tests passed, or code compiles.

## Certification Procedure

### 1. Physical Tree Audit

Enumerate all non-generated production files in:
- Contracts
- Domain
- Application
- Infrastructure
- Endpoints

Verify capability-oriented structure.

Identify:
- root files;
- root allowlists;
- forbidden root files;
- forbidden top-level folders.

Do not approve root dumping merely because it compiles.

### 2. Path / Namespace Exactness

Verify every production file namespace matches the physical path-derived namespace.

Namespace/manifest is not proof of physical organization. For every touched module/project also verify: files physically exist under the intended folders on disk; project includes resolve to those real paths; no stale root copy and no duplicate physical copy remains; Solution Explorer organization and filesystem organization both hold where applicable (preserve canonical Solution Folder grouping; do not change assembly names or project paths merely for visual grouping).

Reject:
- namespace mismatch;
- alias workaround;
- TypeForwardedTo workaround;
- duplicate compatibility types;
- foreign-module global aliases hiding coupling;
- stale root copy / duplicate physical copy;
- broken project include or missing required solution grouping.

### 3. File Cohesion / No God-File (V2)

A file must not pass merely because it is under a hard LOC ceiling.

Verify:
- no new oversized file beyond the architecture size baseline (`tmar-source-size-baseline.json`) or the module LOC guard (`ARCH-SIZE-001`);
- no new god-file / multi-responsibility file (`ARCH-MODULE-FILE-001`);
- no artificial parallel decomposition created to game the size guard;
- resulting files have cohesive, single-purpose responsibilities;
- endpoint transport, request/response models, infrastructure and business logic are not collapsed into one file;
- legitimate Host platform files were split within Host rather than migrated into a business module.

Reject cosmetic splitting that produced meaningless tiny files without real responsibility separation.

### 4. Endpoint Ownership

For HTTP-owning modules verify:
- all routes are module-owned;
- Host-owned route count is zero;
- module endpoint composition entry exists;
- no duplicate mapping;
- Host only performs legitimate composition/security adaptation.

Record exact route count.

### 5. CQRS / MediatR

Inventory every endpoint-reachable request.

For each verify:
- `IRequest` / `IRequest<T>`;
- real `IRequestHandler<,>`;
- `ISender` dispatch;
- no endpoint direct persistence/directory call;
- no Host bypass;
- no legacy/custom dispatcher replacing MediatR.

Record MediatR version when the repository standard requires it (12.5.0).

### 6. Validator Coverage

Build an exhaustive request matrix.

Each request must be exactly one of:
- `VALIDATOR_REQUIRED`
- `NO_VALIDATOR_REQUIRED`

For required:
- concrete validator exists;
- validator is discoverable through the normal DI/MediatR validation pipeline.

For not required:
- durable explicit reason exists.

Add or verify a durable guard so new endpoint requests cannot bypass classification.

Do not certify from an informal count only.

Validators must emit stable machine codes, not localized text.

### 7. Localization Compliance (V2)

Verify:
- every user-facing message resolves through the canonical localizer (`IErrorMessageLocalizer` / resource sets / `.resx`);
- each module stable error code has a catalogue descriptor (`IErrorCatalogContributor`) and an English safe fallback resource;
- Persian resources exist for the module's user-facing error set where the module owns such errors;
- no hard-coded Persian/English user-facing strings in Domain/Application/Endpoints/Infrastructure;
- no `exception.Message` / `ex.Message` used as a localized or user-facing contract;
- no endpoint-level `Accept-Language` parsing instead of the canonical locale resolver;
- existing localization keys and semantics were preserved (no silent rename/repurpose).

Fail if migrated user-facing messages bypass the canonical localization mechanism.

### 8. Canonical API Result / Error Mapping (V2)

Verify:
- endpoints map expected outcomes with the canonical factory (`ApiResponseFactory.From` / `Created`);
- no ad-hoc `Results.Json` / `Results.BadRequest` / `Results.Problem`, local `ProblemDetails` builder, or local error mapper where the canonical abstraction applies;
- no `catch`-and-map blocks in endpoints for expected failures;
- no failure classification by parsing `ex.Message` (e.g. `when (ex.Message...)`, `switch (result.FirstError...heuristic)`);
- unknown/unexpected exceptions are not silently converted to business failures;
- stable error codes remain machine-stable and catalogue-backed;
- success response shape preserved (raw DTO where that is the shipped contract).

Fail if migrated endpoints invent a parallel response/error system.

### 9. Logging / Sensitive Data (V2)

Verify:
- canonical `ILogger<T>` structured logging only;
- no `Console.WriteLine` / `Debug.WriteLine` / new logging framework;
- no second telemetry pipeline;
- no secrets or sensitive authentication material logged: passwords, refresh tokens, access tokens, OTP secrets, reset secrets, `Authorization` headers, cookies, session secrets, security stamps, private credentials, full query strings, payment payloads;
- log scope keys reuse `ObservabilityLogScopeKeys`.

Fail if migrated code introduces ad-hoc logging/telemetry or sensitive-data logging.

### 10. OpenTelemetry / Correlation Continuity (V2)

Verify:
- OpenTelemetry integration preserved;
- distributed trace propagation preserved; `Activity`/`TraceId`/`SpanId` semantics intact;
- no competing/parallel correlation ID (no custom header, custom middleware, raw `AsyncLocal`);
- no direct `ActivitySource.StartActivity(...)` in Application/Endpoints;
- no manual `traceparent` parsing;
- ProblemDetails responses obtain `traceId`/`correlationId`/`requestId` from the canonical context provider;
- cross-module calls use `IModuleCallTracer` decoration and trace continuity is not broken.

Fail if correlation/trace continuity regressed.

### 11. Cross-Module Boundary Audit

Search production references for every foreign Tooba module.

Allowed:
- approved Contracts references.

Block certification for:
- foreign Application;
- foreign Infrastructure;
- foreign Domain;
- foreign DbContext;
- foreign DbSet;
- foreign repository implementation;
- cross-module SQL/EF join;
- direct table/schema reach-through;
- shared mutable aggregate.

If a synchronous cross-module lookup exists, confirm it is through a narrow Contracts port/DTO and not persistence leakage.

### 12. Persistence Ownership Audit

Verify:
- one module DbContext / schema per module;
- migrations owned by the module;
- no cross-module FK;
- Application/Endpoints have no DbContext access;
- no foreign DbSet or foreign schema read;
- `ARCH-DATA-001` intact.

### 13. Host Authority Audit

Classify all Host references:

- ALLOWED_COMPOSITION_ROOT
- ALLOWED_SECURITY_ADAPTER
- ALLOWED_CONTRACT_CONSUMPTION
- STRUCTURAL_DEBT_ONLY
- ILLEGAL_BUSINESS_AUTHORITY
- ILLEGAL_PERSISTENCE_AUTHORITY
- ILLEGAL_ENDPOINT_OWNERSHIP

Certification requires all ILLEGAL categories = ZERO.

Textual Host references may remain when legitimately compositional.

Do not require migration of a legitimate Host platform seam; require only that any oversized/mixed retained Host file was split safely within Host.

### 13a. Host Authentication Certification Rule

Certification must NOT require Host authentication/session platform ownership to be zero.

Never infer authentication ownership from the word "authentication" alone. Canonical architecture documents, locks and current SoT decide the boundary.

For Authentication-related Host residue, distinguish:

Allowed examples:

- `ALLOWED_GLOBAL_AUTH_PLATFORM_BOUNDARY` — global authentication HTTP boundary; authentication middleware; current authenticated request principal/session projection; explicitly locked global auth/session platform seams.
- `ALLOWED_AUTH_RUNTIME_PLUMBING` — runtime authentication plumbing.

Illegal examples:

- `ILLEGAL_IDENTITY_BUSINESS_AUTHORITY` — Identity business rules; module-specific credential lifecycle ownership; module-specific business policy; business state machine logic that belongs to Identity.
- `ILLEGAL_IDENTITY_PERSISTENCE_AUTHORITY` — Identity persistence; foreign DbContext/persistence access.

Only the ILLEGAL categories must be ZERO.

Do not fail certification merely because legitimate global Host auth/session files remain.

Boundary example only (not a naming requirement): `src/backend/Host/Tooba.Host/Authentication` currently holds the global authentication/session HTTP/runtime boundary and legitimately consumes Identity services without inheriting Identity business ownership. Do not hard-code its current file names as permanent architecture requirements.

### 14. Persistence / Migration Safety

Verify architecture cleanup did not accidentally change:
- schema;
- migration identifiers;
- migration order;
- Up/Down semantics;
- snapshot semantics;
- tables;
- columns;
- indexes;
- constraints;
- transaction behavior.

No new migration should exist unless the migration was explicitly part of the intended feature change.

Do not regenerate migrations for structural cleanup.

### 15. Durable Structure Guard

Ensure automated guards enforce the certified structure, including as applicable:

- root allowlists;
- forbidden root files;
- forbidden top-level folders;
- path↔namespace exactness;
- no alias workaround;
- endpoint-reachable request inventory;
- validator coverage;
- certified manifest membership;
- Host ownership expectations;
- canonical API result/error mapping;
- canonical localization coverage;
- correlation/trace continuity;
- source-size/cohesion.

Do not weaken a guard merely to make certification pass.

### 16. Manifest Promotion

Update `tmar-module-structure-manifests.json` only after verification succeeds.

Final certified entry must include:
- module;
- `structureCertified: true`;
- `lockVersion: ARCH-COMPLETE-002`;
- exact project root allowlists;
- forbidden root files;
- forbidden top-level folders.

Ensure exactly one certified entry exists for the module.

Remove any temporary pre-cert duplicate after successful promotion.

### 17. Recovery SoT

Update `tmar-current-state.json` honestly.

Record, as applicable:
- COMPLETE_REFERENCE_PATTERN;
- STRUCTURE_CERTIFIED;
- HTTP applicability;
- endpoint ownership;
- CQRS state;
- request count;
- validator coverage;
- pathNamespace = EXACT;
- rootAllowlist = ENFORCED;
- aliasWorkaround = NONE;
- file cohesion / size state;
- localization state;
- API result/error mapping state;
- logging state;
- correlation/trace state;
- Host residue/authority;
- cross-module boundary state;
- manifest certification;
- certification commit/evidence;
- next workflow gate.

Do not rewrite unrelated history.

### 18. Focused Validation

Run focused builds for:
- Contracts
- Domain if changed/relevant
- Application
- Infrastructure
- Endpoints
- Host if composition/guards depend on it
- test project containing architecture guards

Run focused:
- module behavior tests;
- validator coverage guard;
- structure gate;
- localization/error catalog guard;
- tracing/correlation guard;
- source-size/cohesion guard;
- durable recovery guard;
- module-specific architecture guard.

All required guards must pass.

**TESTS ARE EVIDENCE, NOT NAVIGATION. NO OPEN-ENDED TEST/REPAIR LOOP.** Run only the focused validation required for the bounded scope. If one focused failure has one clear deterministic local cause, perform ONE bounded repair and rerun only the affected validation; if it persists or needs speculation, STOP and report it. Do not repair unrelated pre-existing failures, do not repeatedly run the full repository suite, and never weaken a guard to reach PASS.

No certification with known failing required guard.

## Certification Result

Certify must NOT return a final PASS while any applicable violation remains, including: `RAW_RESULTS`, `AD_HOC`, `PARALLEL_MAPPER`, `UNREGISTERED_CODES`, `HARDCODED_TEXT`, `NON_STANDARD`, `DUPLICATE_TELEMETRY`, `SECOND_PIPELINE`, `PARALLEL_CORRELATION`, `LOST_PROPAGATION`, `VIOLATION`, `ILLEGAL`, `FOREIGN_ACCESS`, any direct foreign Application/Infrastructure/Domain dependency, an unresolved cross-module join, an unresolved path/namespace mismatch, an unresolved stale physical file/copy, an unresolved required solution grouping, an unresolved cohesion/root-dump violation, or an unresolved duplicate/legacy type in the touched surface — unless a canonical architecture lock explicitly exempts that exact quality concern. An ownership exception is not a quality exception.

Only declare:

`COMPLETE_REFERENCE_PATTERN`
`ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`

when every required check passes.

Otherwise return:
- exact blockers;
- exact failing files/tests;
- required repair scope;
- `NOT_CERTIFIED`.

## Required Evidence

Produce evidence containing:

1. final physical tree
2. root allowlists
3. path↔namespace proof
4. alias/shim proof
5. endpoint ownership
6. route count
7. request→handler→validator matrix
8. validator coverage
9. localization coverage (codes→catalog→resources)
10. API result/error mapping proof (no ad-hoc results)
11. logging/sensitive-data proof
12. correlation/trace continuity proof
13. file cohesion / size proof
14. Host authority classification
15. cross-module dependency inventory
16. explicit no-cross-module-join proof
17. persistence/schema safety
18. durable guards
19. manifest state
20. SoT state
21. focused builds
22. focused tests
23. residual non-blocking debt
24. exact certification verdict

## Hard Rules

- Verification before declaration.
- Never certify by self-report alone.
- Certification must NOT PASS by:
  - weakening tests;
  - widening baselines improperly;
  - suppressing guards;
  - creating compatibility shims that hide bad structure;
  - preserving illegal architecture behind aliases.
- Never hide debt using aliases or shims.
- Never allow cross-module persistence or joins.
- Never certify `LEGAL_CONTRACTS_ONLY` while any direct foreign Application/Infrastructure/Domain dependency remains.
- Never treat an ownership exception as a quality exemption; ownership ≠ quality.
- Never PASS while any applicable violation (listed under Certification Result) remains outside an explicit canonical lock exemption.
- Never certify a touched production file without re-reading it against the touched-surface checklist.
- Never weaken tests/guards/baselines/assertions or enter an open-ended test/repair loop to reach PASS.
- Never permit foreign Application/Infrastructure/Domain dependencies in the certified state.
- Never accept a file solely because it is under a LOC ceiling.
- Never accept a parallel localization/response/logging/telemetry mechanism.
- Never accept sensitive-data logging.
- Never redesign business behavior during certification.
- If production refactor is still required, stop certification and return a repair plan.
- Keep this skill deduplicated and bounded: merge/strengthen existing wording instead of appending duplicate rules; this skill should become clearer, not larger.
