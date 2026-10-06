# TB-TMAR-LOCALIZATION-AMSC-001-W0 — Analyze (tooba-architecture-analyze)

- Task: `TB-TMAR-LOCALIZATION-AMSC-001-W0`
- Mode: `ARCHITECT_DIRECT_AMSC`
- Skill: `tooba-architecture-analyze`
- Target: `src/backend/Modules/Localization/Tooba.Localization.*`
- Lock: `ARCH-COMPLETE-002`
- Starting HEAD: `dbdd08e7`
- Production code changed: **NO** (analysis-only wave)

---

## 1. Target analyzed

| Project | Production `.cs` | Notes |
| --- | --- | --- |
| `Tooba.Localization.Contracts` | 6 | `Errors/` (codes, catalog contributor, resource set), `Ports/` (3), `Resources/` (resx pair) |
| `Tooba.Localization.Domain` | 3 | `Aggregates/Language.cs` (120 LOC), `Enums/` (2) |
| `Tooba.Localization.Application` | 12 | `Languages/{Commands,Queries,Validators}`, `Composition/`, `Models/`, `Ports/` |
| `Tooba.Localization.Infrastructure` | 8 + 3 migrations | `Languages/`, `Adapters/`, `Bootstrap/`, `Persistence/`, `Persistence/Migrations/` |
| `Tooba.Localization.Endpoints` | 3 | `LocalizationEndpointModule.cs`, `Admin/` (2) |

Solution grouping: `src/backend/Tooba.slnx` already contains `<Folder Name="/Modules/Localization/">` with all 5 projects (lines 42–48).

Prior history: the module was already taken through `TB-TMAR-LOCALIZATION-AMC-001` W1–W4
(Host evacuation, CQRS/error catalog, structure, certification). It therefore starts this AMSC
pass **materially healthier** than Inventory did: canonical `Result`/`ApiResponseFactory`,
a registered error-catalog contributor, a resource set with a bilingual `.resx` pair, and a
Host ZERO HTTP surface. W0 therefore confirms rather than re-discovers most cross-cutting state,
and reduces the migration wave to the remaining real gaps.

---

## 2. Responsibility map

| Responsibility | Where it lives | Classification | Owner |
| --- | --- | --- | --- |
| Language aggregate + invariants (default-must-be-active, identity validation, normalization) | `Domain/Aggregates/Language.cs` | `DOMAIN_RULE` | Localization |
| Direction / calendar enums | `Domain/Enums/*` | `DOMAIN_RULE` | Localization |
| Create / Update / Patch language use cases | `Application/Languages/Commands/*` | `APPLICATION_USE_CASE` | Localization |
| List languages (admin) | `Application/Languages/Queries/*` | `APPLICATION_USE_CASE` | Localization |
| Transport-shape validators (4) | `Application/Languages/Validators/*` | `APPLICATION_USE_CASE` | Localization |
| Aggregate↔snapshot mapping, direction/calendar parsing | `Application/Composition/LanguageMappings.cs` | `APPLICATION_USE_CASE` | Localization |
| Typed-fault→`Result` seam | `Application/Composition/LocalizationOperation.cs` | `APPLICATION_USE_CASE` | Localization |
| Stable language error codes | `Contracts/Errors/LanguageErrorCodes.cs` | `CONTRACT` | Localization |
| Error catalog descriptors | `Contracts/Errors/LocalizationErrorCatalogContributor.cs` | `CONTRACT` | Localization |
| Error resource set + `LocalizationErrors(.fa).resx` | `Contracts/Errors`, `Contracts/Resources` | `CONTRACT` | Localization |
| Cross-module language lookup / activation / reference ports | `Contracts/Ports/*` | `CONTRACT` | Localization |
| Admin HTTP surface (`/v1/admin/languages`) | `Endpoints/Admin/LocaleAdminEndpoints.cs` | `HTTP_ENDPOINT` | Localization |
| Admin authorizer port | `Endpoints/Admin/ILocalizationAdminAuthorizer.cs` | `AUTHORIZATION_ADAPTER` (port) | Localization |
| Admin authorizer implementation | `Host/Tooba.Host/Admin/Access/Authorizers/HostLocalizationAdminAuthorizer.cs` | `AUTHORIZATION_ADAPTER` | Host (accepted platform seam) |
| DB directory (CRUD, duplicate checks, invariant re-checks) | `Infrastructure/Languages/LanguageDirectory.cs` | `PERSISTENCE` | Localization |
| `localization` schema DbContext + migrations | `Infrastructure/Persistence/*` | `PERSISTENCE` | Localization |
| Outbox registration (no external events) | `Infrastructure/Persistence/LocalizationOutboxRegistration.cs` | `INTEGRATION_ADAPTER` | Localization |
| Contracts→Application bridges | `Infrastructure/Adapters/*` | `INTEGRATION_ADAPTER` | Localization |
| fa/en idempotent bootstrap | `Infrastructure/Bootstrap/LanguageBootstrapHostedService.cs` | `BACKGROUND_WORKER` + `DEVELOPMENT_SEED` | Localization |
| Module DI/schema composition | `Infrastructure/LocalizationModule.cs` | `HOST_COMPOSITION_ROOT` (module-owned) | Localization |
| Endpoint mapper + presentation seam | `Endpoints/LocalizationEndpointModule.cs` | `PRESENTATION_COMPOSITION` | Localization |

No file mixes responsibilities owned by **different modules**. No `MUST_SPLIT` by ownership.

---

## 3. Ownership map

- **Localization owns** the language registry capability end to end: domain invariants, use cases,
  persistence (`localization` schema), HTTP admin surface, stable codes, catalog, resources,
  lookup/activation/reference ports.
- **Host retains only** `HostLocalizationAdminAuthorizer` (`AUTHORIZATION_ADAPTER`) plus
  composition-root wiring in `Program.cs` / `ToobaModuleComposition.cs`. This is the accepted
  `GLOBAL_HOST_PLATFORM_BOUNDARY` / `ALLOWED_COMPOSITION_ROOT` seam already recorded in
  `tmar-current-state.json` as `hostLocalizationPlatformRetained = THIN_ADMIN_AUTHORIZER_AND_COMPOSITION`.
- **`ILanguageReferenceGuard` is consumer-side inverted**: declared in `Localization.Contracts`,
  implemented by `Content.Infrastructure/Adapters/ContentLanguageReferenceGuard.cs` against
  **Content's own** `ContentDbContext`. This is a legal Contracts-only boundary, not coupling.
- `Ownership-State = correct`.

---

## 4. Forbidden coupling audit

Searched all five production projects for foreign module references.

- Foreign `*.Application` / `*.Infrastructure` / `*.Domain` / `*.Endpoints` references: **ZERO**.
- Foreign `DbContext` / `DbSet` / foreign repository access: **ZERO**.
- EF navigation across module boundaries: **ZERO** (`Language` is a flat aggregate with no
  navigation properties).
- Raw SQL joining module-owned tables: **ZERO**.
- Namespace alias / `TypeForwardedTo` workaround: **ZERO**.

Project reference graph (all inward, no cycles):

```text
Contracts   -> BuildingBlocks
Domain      -> BuildingBlocks, Contracts (own)
Application -> BuildingBlocks, Domain (own), Contracts (own)
Endpoints   -> Application (own), Contracts (own), BuildingBlocks
Infrastructure -> Contracts (own), Application (own), ModuleContracts, Persistence
```

`Cross-Module-Coupling-State = NONE` (strictly: zero foreign module edges, not even a
Contracts consumption edge).
`Cross-Module-Join-State = NONE`.
`Persistence-Ownership-State = CORRECT` (own `localization` schema, own DbContext, own migrations).

---

## 5. CQRS / MediatR

- 4 endpoint-reachable requests, all real `IRequest<Result<...>>` with real `IRequestHandler<,>`:
  `CreateLanguageCommand`, `UpdateLanguageCommand`, `PatchLanguageCommand`, `ListLanguagesAdminQuery`.
- Endpoints dispatch through `ISender` and map with `ApiResponseFactory.From(...)`.
  No `Results.Json` / `Results.BadRequest` / `Results.Problem`; no `DbContext`, no `ILanguageDirectory`,
  no `SemanticException` in Endpoints.
- `CQRS-State = COMPLIANT`.

## 6. Validation classification matrix

| Request | Transport fields | Classification | Validator present |
| --- | --- | --- | --- |
| `ListLanguagesAdminQuery` | none | `VALIDATOR_REQUIRED_PRESENT` (marker) | yes |
| `CreateLanguageCommand` | 7 required strings | `VALIDATOR_REQUIRED_PRESENT` | yes |
| `UpdateLanguageCommand` | 6 required strings | `VALIDATOR_REQUIRED_PRESENT` | yes |
| `PatchLanguageCommand` | `Code` required | `VALIDATOR_REQUIRED_PRESENT` | yes |

`validatorRequiredCount = 4`, `noValidatorRequiredCount = 0`. Validators emit stable machine
codes (`LanguageErrorCodes.Invalid*`) and carry no user-facing text.
`Validator-Coverage-State = EXHAUSTIVE`.

## 7. Localization audit

- Stable machine codes: **19** declared in `Contracts/Errors/LanguageErrorCodes.cs`.
- Descriptors: **19** registered by `LocalizationErrorCatalogContributor` — one canonical owner,
  no duplicates, `LocalizationKey == Code`.
- Resource set: `LocalizationErrorResourceSet` owns the `localization.` key space; both
  `LocalizationErrors.resx` and `LocalizationErrors.fa.resx` carry all 19 keys.
- Raw `localization.*` string literals outside `LanguageErrorCodes.cs`: **ZERO**.
- `ex.Message` / `exception.Message` used for response or classification: **ZERO**.
- `Accept-Language` parsing inside the module: **ZERO**.
- Hard-coded Persian user-facing text in production: **NONE**. The only Persian literal is
  `"فارسی"` in `LanguageDirectory.BootstrapAsync` — **seed data** (the native name of the fa-IR
  language row), not a user-facing message. Bootstrap ordering: `Language.Create` validates
  lengths before assigning, so the literal never surfaces as an error message.
- `Localization-State = CANONICAL`.
- `API-Result-Pattern-State = CANONICAL`.
- `Stable-Error-Code-State = CATALOGUED` (19/19 declared codes have exactly one descriptor owner).

### 7a. Non-blocking resource-set keyspace breadth (watch)

`LocalizationErrorResourceSet.Owns()` matches the **whole `localization.` prefix**. The module only
declares the `localization.language.*` sub-space. Any future code outside this module that emits a
`localization.*` code would silently resolve through this resource set (missing key → `null` →
English fallback) instead of failing fast. Narrowing `Owns()` to the declared sub-space would make
ownership honest, but it is a behavioral refinement of a published resource set and is therefore
recorded as **watch**, not a W1 requirement, unless the Structure/Certify wave can prove zero
behavioral delta.

## 8. Logging / observability / telemetry

- `ILogger<T>` used throughout; structured, dotted, value-free event names
  (`localization.language.create.failed`, `localization.languages.list.succeeded`, …).
- `Console.WriteLine` / `Debug.WriteLine` / custom logger frameworks: **ZERO**.
- Second `ActivitySource` / `Meter` / correlation provider: **ZERO**.
- Sensitive-data logging (passwords, tokens, OTP, cookies, `Authorization`, secrets): **NONE**.
- `LanguageBootstrapHostedService` catches and logs a bootstrap failure and lets the Host continue
  — this is intentional fail-safe behavior and is preserved.
- `Logging-State = CANONICAL`, `Sensitive-Logging-State = NONE`,
  `OpenTelemetry-State = CANONICAL`, `Correlation-Trace-State = CANONICAL`.
- The module issues no cross-module calls of its own, so no `IModuleCallTracer` decoration is
  required (foreign modules decorate their own calls to `ILanguageLookup` / `ILanguageActivationPort`).

## 9. File size / cohesion

| File | LOC | Classification |
| --- | --- | --- |
| `Infrastructure/Languages/LanguageDirectory.cs` | 231 | `COHESIVE` (single responsibility: language persistence directory) |
| `Domain/Aggregates/Language.cs` | 120 | `COHESIVE` |
| `Endpoints/Admin/LocaleAdminEndpoints.cs` | 107 | `COHESIVE` (4 transport handlers + 2 request records) |
| `Application/Composition/LanguageMappings.cs` | 73 | `COHESIVE` |
| `Application/Models/LanguageSnapshot.cs` | 69 | `COHESIVE` — 7 related record shapes in one capability file (snapshots + directory specs + admin response). Acceptable but the widest file in Application; watch for future growth. |
| `Persistence/Migrations/LocalizationDbContextModelSnapshot.cs` | 155 | EF-generated |

No file is above the `ARCH-SIZE-001` 800 LOC ceiling; no entry in
`Baselines/tmar-source-size-baseline.json` references Localization.
`File-Cohesion-State = COHESIVE`.
`Oversized/God-File-State = NONE`.

## 10. Structural symptoms found (handoff to Structure)

1. **`artifacts/` empty folders** physically present under
   `Application/`, `Domain/` and `Infrastructure/`. They are untracked, empty, absent from
   `tmar-module-structure-manifests.json`, and are exactly the "empty decorative folder" that the
   Structure skill forbids. They do not affect the build or the manifests.
2. **`Languages/Validators` is a flat technical folder** inside the single `Languages` capability.
   This is the canonical `Application/<Capability>/Validators` shape and is correct; no change
   required.
3. **`Application/Composition/` holds a 2-file technical bucket** (`LanguageMappings`,
   `LocalizationOperation`). Both are genuinely cross-capability mapping/fault seams, matching the
   `<Module>Operation` precedent used by Content/CustomerProfile/Inventory. Acceptable.
4. No technical-axis-first request tree, no single-file request leaf folder, no root `.cs` dump.
   All five projects already satisfy their manifest `rootAllowlist` / `forbiddenRootFiles` /
   `forbiddenTopLevelFolders` constraints.

`Folder-Granularity-State = PROFESSIONAL_SHALLOW`.
`Structure-Handoff-State = REQUIRED` (only to confirm the above and to reconcile the empty
`artifacts/` folders).

## 11. Schema / migrations

- `localization` schema, single `languages` table, unique indexes on `Code` and `UrlPrefix`,
  composite index `(IsActive, SortOrder)`, plus the shared outbox mapping.
- 1 migration (`20260901220000_InitialLocalization`) + designer + model snapshot.
- `Schema-Migration-State = UNCHANGED`; no drift detected.

## 12. Behavior-preservation baseline

Routes (`GET|POST /v1/admin/languages`, `PUT|PATCH /v1/admin/languages/{code}`), status codes,
response DTO shape (`LanguageAdminResponse`), all 19 stable error codes, DI lifetimes,
bootstrap seed values and idempotency, migration IDs and Up/Down, outbox registration semantics,
and logging event names must remain byte-identical across W1–W3.

## 13. Structured state fields

| Field | Value |
| --- | --- |
| Foundation-State | `FOUNDATION_READY` |
| Ownership-State | `correct` |
| File-Cohesion-State | `COHESIVE` |
| Oversized/God-File-State | `NONE` |
| Localization-State | `CANONICAL` |
| API-Result-Pattern-State | `CANONICAL` |
| Stable-Error-Code-State | `CATALOGUED` |
| Logging-State | `CANONICAL` |
| Sensitive-Logging-State | `NONE` |
| OpenTelemetry-State | `CANONICAL` |
| Correlation-Trace-State | `CANONICAL` |
| CQRS-State | `COMPLIANT` |
| Validator-Coverage-State | `EXHAUSTIVE` (4/4) |
| Contracts-Boundary-State | `CLEAN` |
| Cross-Module-Coupling-State | `NONE` |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Host-Residue-State | `ALLOWED_COMPOSITION_ROOT_AND_AUTHORIZATION_ADAPTER_ONLY` |
| Schema-Migration-State | `UNCHANGED` |
| Behavior-Preservation-Risk | `LOW` |
| Canonical-Reference-Used | `Content/CustomerProfile/Inventory <Module>Operation` seam; `Offer` CQRS/endpoint shape; `BuildingBlocks` result/catalog/resource mechanisms |
| **Final-Disposition** | **`READY_TO_MIGRATE`** |

## 14. W1 scope (smallest repository-consistent set)

1. **Durable AMSC structural guard for Localization** — the existing W1–W4 AMC guards are
   evidence-oriented; W1 adds the AMSC-boundary guard covering zero-foreign-coupling,
   catalog/resource presence, and canonical endpoint dispatch.
2. **Migration-evidence reconciliation** — record the already-canonical state so W2/W3 can
   promote the module honestly rather than re-implementing what is already correct.
3. **Optional, only if provably behavior-neutral** — narrow `LocalizationErrorResourceSet.Owns()`
   to the declared `localization.language.` sub-space to remove the cross-keyspace silent-resolve
   risk identified in §7a. Otherwise leave as watch.

No fault-typing, result-mapping, localization, logging, telemetry, CQRS, validator, cohesion or
schema work is required — those are already canonical.

## 15. Verification plan

- `dotnet build` on the 5 Localization projects.
- `dotnet build` on `Tooba.Host.Tests` (guards live there).
- Focused filter: `LocalizationModuleAmc` + the new AMSC guard.
- Re-enumerate the Localization tree and confirm path↔namespace exactness and physical-copy
  cleanliness.

## 16. Certification blockers carried into W1

| # | Blocker | Resolution wave |
| --- | --- | --- |
| 1 | No AMSC evidence tree / AMSC SoT records for Localization (only the older `localizationAmc001` AMC-001 record exists). | W0–W3 (this pipeline) |
| 2 | Empty untracked `artifacts/` folders under 3 projects. | W2 |
| 3 | Cross-keyspace `Owns()` breadth (§7a) — watch only. | W1 if provably neutral, else documented watch |
| 4 | `AddLocalizationEndpointPresentation()` is an empty composition seam retained for Host registration. | Non-blocking watch (preserved for behavior) |

No architecture decision is required. `noArchitectureDecisionRequired = true`.
`hostTouched = false`.
