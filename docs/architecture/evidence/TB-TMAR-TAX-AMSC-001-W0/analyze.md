# TB-TMAR-TAX-AMSC-001-W0 — Analyze (`tooba-architecture-analyze`)

```text
TASK:          TB-TMAR-TAX-AMSC-001-W0
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-analyze (V2)
TARGET:        src/backend/Modules/Tax/Tooba.Tax.*
STARTING HEAD: 4a114a51
LOCK VERSION:  ARCH-COMPLETE-002
STATE:         ANALYZE_COMPLETE
VERDICT:       READY_TO_MIGRATE
```

Analysis-only wave. **No production code changed in W0.** Evidence + SoT only.

---

## 0. Applicability Gate (MANDATORY BEFORE TARGET SHAPE)

**Classification: `INTERNAL_ONLY`.**

Evidence:

| Probe | Result |
| --- | --- |
| Module HTTP routes | **0** |
| `MapTaxModule` body | `_ = app.MapGroup("/v1/tax");` — empty group, **no mapped operation** |
| Host `MapGet/MapPost/MapPut/MapPatch/MapDelete` with tax path | **0** (guarded by `TaxArchitectureGuardTests.Host_has_no_tax_owned_http_route_maps`) |
| Host `/Tax` folder | does not exist |
| `Tooba.Tax.Endpoints` project contents | one static class, 21 LOC, empty group only |
| User-facing boundary owner | **Order** (`Tooba.Order.Infrastructure.Checkout.Persistence.CheckoutDirectory*` → `ITaxCalculator`); **Catalog** storefront composer; **ProductWorkspace** admin read handler |
| Consumers | `Order.Application`, `Order.Infrastructure`, `Catalog.Infrastructure`, `ProductWorkspace.Application` — all reference `Tooba.Tax.Contracts` only |

`Tooba.Tax.Endpoints` is **ceremony**, not HTTP ownership. Per the Analyze applicability gate, an empty route group is not evidence of HTTP ownership and existing ceremonial `Endpoints` structure in an `INTERNAL_ONLY` module is a finding that **must be retired** by Migrate/Structure.

Consequences for the target plan:

```text
Endpoints          = NOT_APPLICABLE
CQRS               = NOT_APPLICABLE
Validator Matrix   = NOT_APPLICABLE
```

The certified `Inventory` and `Pricing` (W3-R2/R3) precedents are the canonical `INTERNAL_ONLY` shape:
five projects (Contracts, Domain, Application, Infrastructure, Tests), zero Endpoints project,
error-catalog/resource registration owned by the **Infrastructure composition root**.

---

## 1. Target analyzed

Full production + test inventory (40 files, EF migrations excluded from cohesion counting):

```text
Tooba.Tax.Contracts/            (2 csproj + 6 .cs)
  Dtos/TaxOutcome.cs
  Ports/ITaxDevelopmentSeedGateway.cs
  Ports/ITaxSchemaMigrator.cs
  Ports/TaxCalculatorContracts.cs
  Ports/TaxQueryContracts.cs
Tooba.Tax.Domain/               (7 .cs)
  Aggregates/TaxRule.cs, TaxCategory.cs, TaxOfferClassification.cs
  Enums/TaxRuleEnums.cs
  Events/TaxDomainEvents.cs
  Policies/TaxRounding.cs
Tooba.Tax.Application/          (1 .cs)
  Ports/ITaxDirectory.cs
Tooba.Tax.Infrastructure/       (10 .cs + 3 migration artifacts)
  Adapters/TaxDirectory.cs, TaxDevelopmentSeedGateway.cs, TaxModuleMigration.cs, TaxSchemaMigrator.cs
  DependencyInjection/TaxModule.cs
  Events/TaxEvents.cs
  Outbox/TaxOutboxRegistration.cs
  Persistence/TaxDbContext.cs
  Persistence/Configurations/{TaxCategory,TaxOfferClassification,TaxRule}Configuration.cs
  Persistence/Migrations/20260823190000_InitialTax(.Designer).cs, TaxDbContextModelSnapshot.cs
Tooba.Tax.Endpoints/            (1 .cs — CEREMONIAL)
  TaxEndpointModule.cs
Tooba.Tax.Tests/                (5 .cs)
  Architecture/TaxArchitectureGuardTests.cs
  Contracts/TaxCalculationShapeTests.cs
  Domain/TaxRuleInvariantTests.cs
  Endpoints/TaxEndpointModuleTests.cs
  Infrastructure/TaxDbContextOwnershipTests.cs
```

Repository context read: `AGENTS.md`, `TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md`,
`TMAR-architecture-locks.md`, `tmar-current-state.json`, `tmar-module-structure-manifests.json`,
plus the four AMSC skills and the certified `Pricing` / `Inventory` surfaces.

---

## 2. Responsibility map

| Responsibility | Owner | Current home | Verdict |
| --- | --- | --- | --- |
| Effective-dated tax rule invariant (`TaxRule`) | Tax | `Tax.Domain/Aggregates/TaxRule.cs` | correct |
| Opaque tax category (`TaxCategory`) | Tax | `Tax.Domain/Aggregates/TaxCategory.cs` | correct |
| Offer→category assignment (`TaxOfferClassification`) | Tax | `Tax.Domain/Aggregates/TaxOfferClassification.cs` | correct |
| Deterministic currency-scale rounding (`TaxRounding`) | Tax | `Tax.Domain/Policies/TaxRounding.cs` | correct |
| Rule/category domain events | Tax | `Tax.Domain/Events/TaxDomainEvents.cs` | correct |
| Calculation + configuration directory | Tax | `Tax.Infrastructure/Adapters/TaxDirectory.cs` | correct |
| Development seed seam | Tax | `Tax.Infrastructure/Adapters/TaxDevelopmentSeedGateway.cs` | correct |
| Schema migration entrypoint | Tax | `Tax.Infrastructure/Adapters/TaxSchemaMigrator.cs` + `TaxModuleMigration.cs` | correct |
| Persistence (own `tax` schema) | Tax | `Tax.Infrastructure/Persistence/*` | correct |
| Outbox translation | Tax | `Tax.Infrastructure/Outbox/TaxOutboxRegistration.cs` | correct |
| Module DI composition | Tax | `Tax.Infrastructure/DependencyInjection/TaxModule.cs` | correct |
| Cross-module contract surface | Tax | `Tax.Contracts/*` | correct (namespace drift only) |
| **Empty HTTP route group** | *nobody* | `Tax.Endpoints/TaxEndpointModule.cs` | **CEREMONY — retire** |
| Stable fault codes | Tax | *missing* | **MUST ADD (W1)** |
| Typed fault seam | Tax | *missing* | **MUST ADD (W1)** |

`MUST_SPLIT` files: **none.** `TaxDirectory.cs` is 295 LOC single-responsibility (four Tax-owned ports over one schema).

---

## 3. Ownership map

```text
Tax OWNS:      effective-dated tax rule truth, tax category vocabulary, offer→category classification,
               tax amount/outcome computation, tax outbox, tax schema.
Tax DOES NOT:  own price amounts (Pricing), order lifecycle (Order), invoices (Settlement),
               payment gateways (Payment), offer selection (Offer), customer identity (Identity),
               cart lines (Cart), promotion definition (Promotion).
```

No file contains responsibilities owned by two modules. Ownership is `correct`.

---

## 4. Current illegal dependencies

**ZERO.**

| Check | Result |
| --- | --- |
| Foreign `.Application` / `.Infrastructure` / `.Domain` project reference in any Tax project | **0** |
| Foreign `DbContext` / `DbSet` / foreign schema read | **0** |
| Cross-module EF/SQL join | **0** |
| Foreign FK (Offer/Order/Pricing) in `tax` schema | **0** — `offer_classifications` keeps a plain `OfferId` PK, `rules.CategoryId` is a plain `Guid`; no navigation property crosses a module boundary |
| `Tooba.Tax.*` project referencing `Tooba.Host` | **0** |
| `DateTimeOffset.UtcNow` / `Guid.NewGuid()` / `StartActivity` / `PlatformHttpException` in Tax production | **0** (guarded today) |
| `TypeForwardedTo` / namespace alias workaround | **0** |

Tax outbound references: `Tooba.BuildingBlocks`, `Tooba.ModuleContracts`, `Tooba.Persistence` foundations only.

**Inbound**: `Order`, `Catalog`, `ProductWorkspace` consume `Tooba.Tax.Contracts` only. `Host` touches Tax only at the composition root (`Program.cs` map call, `ToobaModuleComposition.cs` `new TaxModule()`, `Tooba.Host.csproj` references, `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` migration descriptor).

`Cross-Module-Coupling-State = NONE` (zero foreign edges in either direction beyond the legal Contracts seam).

---

## 5. Cross-module join inventory

**NONE.** Verified by reading `TaxDbContext`, all three `IEntityTypeConfiguration` implementations,
and the single migration `20260823190000_InitialTax`. No `HasOne/WithMany`, no `HasForeignKey`
pointing outside `tax`, no raw SQL, no `FromSql`.

---

## 6. Contracts-only replacement map

Already satisfied. The Contracts surface is the module boundary:

| Port | Consumer | Purpose |
| --- | --- | --- |
| `ITaxCalculator` + `TaxCalculationRequest` / `TaxCalculationResult` / `TaxOutcome` | Order (checkout), Catalog (storefront) | compute tax for a line |
| `ITaxQueryGateway` + `TaxCategorySnapshot` / `TaxClassificationSnapshot` | ProductWorkspace (admin read), Catalog | label/classification reads |
| `ITaxDevelopmentSeedGateway` + `EnsureDevelopment*` | Catalog development seeds | demo classification/rule |
| `ITaxSchemaMigrator` | Host migration seam | apply Tax migrations |

W1 adds the module-owned **stable error-code** boundary (`Tooba.Tax.Contracts.Errors`) — a legal
module-boundary semantic per the Analyze "semantic contract ownership gate".

---

## 7. CQRS / MediatR gaps

`NOT_APPLICABLE`. Tax owns zero endpoint-reachable requests. No `IRequest`, `IRequestHandler`,
`ISender` call site, Command/Query/Validator folder exists or is required.

The W1 `TaxOperation` seam stays a **`Result`-returning boundary**, never an `IResult`/endpoint mapper
(the certified Pricing precedent: `PricingOperation` is a dormant typed-fault seam for an
`INTERNAL_ONLY` module).

---

## 8. Validation classification matrix

**Empty by construction.** `endpointReachableRequests = 0`, `validatorRequiredCount = 0`,
`validatorsPresentCount = 0`, `noValidatorRequiredCount = 0`.

`Validator-Coverage-State = EXHAUSTIVE_0_OF_0_NO_VALIDATOR_REQUIRED` (set equality trivially holds:
the shipped endpoint-reachable request set is empty, and no FluentValidation pipeline is registered
for Tax). No validator may be invented for an `INTERNAL_ONLY` module.

---

## 9. Localization findings

**Current state: `MISSING_INFRASTRUCTURE_USE`.**

Every Tax fault is a raw `InvalidOperationException` carrying a bare string literal:

| File | Line | Literal |
| --- | --- | --- |
| `Tax.Domain/Aggregates/TaxRule.cs` | 105 | `tax.rule.id_required` |
| `Tax.Domain/Aggregates/TaxRule.cs` | 110 | `tax.jurisdiction.required` |
| `Tax.Domain/Aggregates/TaxRule.cs` | 115 | `tax.market.required` |
| `Tax.Domain/Aggregates/TaxRule.cs` | 120 | `tax.validity.inverted` |
| `Tax.Domain/Aggregates/TaxRule.cs` | 126 | `tax.rate.out_of_range` |
| `Tax.Domain/Aggregates/TaxRule.cs` | 131 | `tax.rate.not_applicable` |
| `Tax.Domain/Aggregates/TaxRule.cs` | 174 | `tax.rate.kind_mismatch` |
| `Tax.Domain/Aggregates/TaxRule.cs` | 177 | `tax.rate.out_of_range` |
| `Tax.Domain/Aggregates/TaxCategory.cs` | 46 | `tax.category.id_required` |
| `Tax.Domain/Aggregates/TaxCategory.cs` | 51 | `tax.category.code_required` |
| `Tax.Infrastructure/Adapters/TaxDirectory.cs` | 50 | `tax.category.missing` |
| `Tax.Infrastructure/Adapters/TaxDirectory.cs` | 79 | `tax.category.missing` |
| `Tax.Infrastructure/Outbox/TaxOutboxRegistration.cs` | 76 | `Unmapped Tax integration event type.` (English prose) |

There is **no** `TaxErrorCodes`, **no** `IErrorCatalogContributor`, **no** `IErrorResourceSet`,
**no** `.resx` pair, **no** `Application/Composition/TaxOperation.cs`, and **no** registration in
`TaxModule.AddServices`.

Findings:

- **13 raw faults**, 12 of which already carry a machine-shaped `tax.*` code as prose.
- 1 fault (`TaxOutboxRegistration.cs:76`) carries **English user-facing prose** — must become a stable
  platform-classified code (`tax.outbox.unmapped_event_type`), mirroring `InventoryErrorCodes.OutboxUnmappedEventType`.
- The `tax.*` codes are currently **unregistered** in the composed catalog: the canonical
  `SafeErrorMapper` would fall back to a generic classification instead of the module's real status.
- Zero hard-coded Persian strings; zero `ex.Message` classification; zero `Accept-Language` parsing.
- `Order.Application/Storefront/StorefrontOrderErrors.cs` owns `checkout.tax.unavailable`
  (`TAX_NO_APPLICABLE_RULE`). That code is **Order-owned** (it is the Order checkout use case's own
  outcome) and Tax must **not** claim its descriptor — duplicate usage is allowed, duplicate descriptor
  ownership is not.

---

## 10. API result / error mapping findings

**CANONICAL with one gap.** Tax production contains zero `Results.Json` / `Results.BadRequest` /
`Results.Problem` / local `ProblemDetails` builder / catch-and-map block. Tax owns no HTTP route, so it
owns no `ApiResponseFactory` call site.

The gap is the **missing typed-fault → `Result` seam** for the Contract boundary. The port
`ITaxDevelopmentSeedGateway` already returns `Result`, but the module has no single place that maps its
own typed faults into `Result` while letting foreign/unknown exceptions propagate untouched to the
canonical global exception boundary.

---

## 11. Logging / sensitive-data findings

**CANONICAL.** Zero `ILogger<T>`, zero `Console.WriteLine`, zero `Debug.WriteLine`, zero hand-rolled
writer, zero `ObservabilityLogScope` misuse in Tax production. Nothing to repair and nothing to add.
`Sensitive-Logging-State = NONE`.

---

## 12. OpenTelemetry / correlation findings

**CANONICAL.** Zero `StartActivity`, zero manual `traceparent` parsing, zero competing correlation ID,
zero custom `AsyncLocal`/header/middleware. Tax issues no cross-module call (it is the *callee*), so no
`IModuleCallTracer` decoration is required. `OpenTelemetry-State = CANONICAL`,
`Correlation-Trace-State = CANONICAL`.

---

## 13. File cohesion / splitting audit

| File | LOC | Types | Classification |
| --- | --- | --- | --- |
| `Infrastructure/Adapters/TaxDirectory.cs` | 295 | 2 (`OpenTaxUseCaseGuard`, `TaxDirectory`) | **OVERSIZED_ONLY (cohesive)** — four Tax-owned ports over one schema; guard ceiling 800 |
| `Domain/Aggregates/TaxRule.cs` | 192 | 1 | COHESIVE |
| `Infrastructure/Outbox/TaxOutboxRegistration.cs` | 86 | 1 | COHESIVE |
| `Domain/Events/TaxDomainEvents.cs` | 92 | 4 domain events | COHESIVE (event family) |
| `Application/Ports/ITaxDirectory.cs` | 74 | 3 (`TaxCategoryReference`, `TaxRuleReference`, `ITaxDirectory`, `ITaxUseCaseGuard`) | COHESIVE (one capability port file) |
| `Endpoints/TaxEndpointModule.cs` | 21 | 1 | CEREMONIAL — retire |

No god-file. No file over the 800 LOC `ARCH-SIZE-001` ceiling. No cosmetic split required.
`File-Cohesion-State = COHESIVE`.

Note: `ITaxDirectory.cs` bundles two records + one port + one guard. This mirrors the certified
`Inventory/Application/Ports/*` pattern (one capability port per file). W2 may optionally split
`ITaxUseCaseGuard` into its own file for parity with `Inventory/Ports/IInventoryUseCaseGuard.cs`;
this is **structure polish**, not a cohesion blocker.

---

## 14. Target foldering (exact paths / namespaces)

`Structure-Handoff-State = REQUIRED` (the module loses a project and gains `Errors/` + `Resources/`).

```text
src/backend/Modules/Tax/
  Tooba.Tax.Contracts/                                   namespace Tooba.Tax.Contracts
    Dtos/TaxOutcome.cs                                   -> Tooba.Tax.Contracts.Dtos
    Ports/TaxCalculatorContracts.cs                      -> Tooba.Tax.Contracts.Ports
    Ports/TaxQueryContracts.cs                           -> Tooba.Tax.Contracts.Ports
    Ports/ITaxDevelopmentSeedGateway.cs                  -> Tooba.Tax.Contracts.Ports
    Ports/ITaxSchemaMigrator.cs                          -> Tooba.Tax.Contracts.Ports
    Errors/TaxErrorCodes.cs                    (NEW W1)  -> Tooba.Tax.Contracts.Errors
    Errors/TaxErrorCatalogContributor.cs       (NEW W1)  -> Tooba.Tax.Contracts.Errors
    Errors/TaxErrorResourceSet.cs              (NEW W1)  -> Tooba.Tax.Contracts.Errors
    Resources/TaxErrors.resx                   (NEW W1)
    Resources/TaxErrors.fa.resx                (NEW W1)
  Tooba.Tax.Domain/                                      namespace Tooba.Tax.Domain
    Aggregates/TaxRule.cs, TaxCategory.cs, TaxOfferClassification.cs
    Enums/TaxRuleEnums.cs
    Events/TaxDomainEvents.cs
    Policies/TaxRounding.cs
  Tooba.Tax.Application/                                 namespace Tooba.Tax.Application
    Composition/TaxOperation.cs                (NEW W1)  -> Tooba.Tax.Application.Composition
    Ports/ITaxDirectory.cs                               -> Tooba.Tax.Application.Ports
    Ports/ITaxUseCaseGuard.cs                  (OPT W2)  -> Tooba.Tax.Application.Ports
  Tooba.Tax.Infrastructure/                              namespace Tooba.Tax.Infrastructure
    Adapters/{TaxDirectory,TaxDevelopmentSeedGateway,TaxModuleMigration,TaxSchemaMigrator}.cs
    DependencyInjection/TaxModule.cs
    Events/TaxEvents.cs
    Outbox/TaxOutboxRegistration.cs
    Persistence/TaxDbContext.cs
    Persistence/Configurations/*.cs
    Persistence/Migrations/*
  Tooba.Tax.Tests/                                       namespace Tooba.Tax.Tests
    Architecture/, Contracts/, Domain/, Infrastructure/  (Endpoints/ folder RETIRED)
```

Solution Explorer (`src/backend/Tooba.slnx`) — canonical `/Modules/Tax/` folder, **5 projects**:

```text
/Modules/Tax/
  Tooba.Tax.Application
  Tooba.Tax.Contracts
  Tooba.Tax.Domain
  Tooba.Tax.Infrastructure
  Tooba.Tax.Tests
```

`Tooba.Tax.Endpoints` entry is removed from `Tooba.slnx`, from `Tooba.Host.csproj`, and from
`Tooba.Tax.Tests.csproj`; the empty `/v1/tax` group and `app.MapTaxModule()` call are removed from
`Program.cs` (mirroring the certified Pricing W3-R2 removal).

---

## 15. Behavior-preservation baseline

Must remain byte-identical in observable behavior:

- **Schema**: `tax` schema, tables `categories`, `offer_classifications`, `rules`, `outbox_messages`;
  migration id `20260823190000_InitialTax` and its Up/Down semantics; no new migration.
- **Persistence semantics**: PK/column/index/constraint/precision semantics unchanged.
- **Calculation semantics**: the four distinct outcomes (`Taxable`, `Exempt`, `ZeroRated`,
  `NoApplicableRule`) plus `CalculationError`; highest-`Specificity` winner selection; ambiguous
  (tie) → `CalculationError`; `TaxRounding` IRR/JPY/KRW scale 0 else 2 with `Midpoint AwayFromZero`;
  `AllowTrustedOverride` honoured only when the rule policy is `TrustedInternal`; `Fail(...)` returns
  the original exclusive amount with zero rate/amount and null rule/category.
- **Tenant/store isolation**: unchanged (per-context connection string resolution).
- **Outbox**: integration event names `tax.rule_created.v1`, `tax.rule_activated.v1`,
  `tax.rule_changed.v1`, `tax.calculation_failed.v1` and their semantic dimensions unchanged.
- **Contracts**: `TaxOutcome` member names and numeric values, record shapes, port signatures unchanged.
- **HTTP**: none existed and none is added; the empty `/v1/tax` group carried no behavior, no
  documented route, no client contract and no test asserting a route (only that the *method* exists).
- **Localization**: the 12 existing `tax.*` codes keep their exact string values (they are the
  machine identity); the outbox prose fault becomes a stable code.
- **DI lifetimes**: `TaxDirectory`/`ITaxCalculator`/`ITaxQueryGateway`/`ITaxDevelopmentSeedGateway`/
  `ITaxSchemaMigrator` scoped; outbox registration singleton — unchanged.
- **Host composition**: `new TaxModule()` stays in `ToobaModuleComposition.Modules`;
  `ModuleMigrationRegistry` descriptor stays.

`Behavior-Preservation-Risk = LOW`.

**Test-assertion drift to repair (one bounded, behavior-neutral change):**
`Tooba.Host.Tests/TaxFoundationTests.cs:74` reads the deleted file
`Modules/Tax/Tooba.Tax.Domain/TaxDomain.cs` (split into cohesive aggregate files in commit
`edecccce`) and therefore throws `FileNotFoundException` at the W0 baseline. W1 repoints that
assertion at the real aggregate files while preserving the exact intent (no `0.09` rate and no `1405`
date hard-coded in the Tax Domain).

Two further pre-existing, unrelated baseline failures were observed in the focused run and are **out of
scope** (present before this task): `HostAdminAmcW30PwTaxonomyGuardTests.Host_Admin_count_15…`
(expected 15, actual 17) and `HostDevelopmentMigrationSeamGuardTests.All_twenty_eight_active_modules…`
(module count drift). Neither is a Tax defect and neither will be "fixed" by this task.

---

## 16. Foundation / certified-module state

**`FOUNDATION_PARTIAL`.**

- Projects present: Contracts, Domain, Application, Infrastructure, Endpoints (ceremonial), Tests.
- Missing for the canonical `INTERNAL_ONLY` shape: `Contracts/Errors/`, `Contracts/Resources/`,
  `Application/Composition/`.
- Excess: `Tooba.Tax.Endpoints` + its empty route group + its test folder.
- Tax has **no** `structureCertified` manifest entry and is **not** in
  `structureLock.certifiedModules` (30 certified modules; Tax is absent). No prior AMSC lineage.

---

## 17. Migration order (W1 → W2 → W3)

**W1 — Migrate** (`tooba-architecture-migrate`)
1. `Contracts/Errors/TaxErrorCodes.cs` — 13 declared codes + `KnownCodes` + `IsKnown(string?)`.
2. `Contracts/Errors/TaxErrorCatalogContributor.cs` — 13 `ErrorDescriptor`s (`LocalizationKey = code`,
   explicit HTTP status, `Severity`, safe English fallback).
3. `Contracts/Errors/TaxErrorResourceSet.cs` + `Resources/TaxErrors.resx` / `TaxErrors.fa.resx`
   (13 keys each, distinct EN/FA), embedded with explicit logical names.
4. `Application/Composition/TaxOperation.cs` — dual-mechanism typed-fault seam
   (`ContractOperationException` by declared code + `SemanticException`), value + value-less overloads,
   unknown codes/exceptions propagate untouched.
5. Replace the 13 raw `InvalidOperationException` faults with the declared codes
   (`SemanticException` in Domain, `ContractOperationException` in Infrastructure), so the existing
   `Assert.Throws<InvalidOperationException>` invariant tests are repointed to the canonical typed fault
   **without weakening intent**.
6. Register exactly one `IErrorCatalogContributor` + one `IErrorResourceSet` in
   `TaxModule.AddServices` (certified `Inventory`/`Pricing` internal-only precedent).
7. Repair the stale `TaxFoundationTests.cs:74` assertion.
8. Add `TaxModuleAmsc001W1MigrateGuardTests`.

**W2 — Structure** (`tooba-architecture-structure`)
1. Retire `Tooba.Tax.Endpoints`: delete project + `TaxEndpointModule.cs` + `Endpoints/` test folder;
   remove the `Tooba.slnx` entry, the `Tooba.Host.csproj` reference, the `Tooba.Tax.Tests.csproj`
   reference, the `using Tooba.Tax.Endpoints;` and `app.MapTaxModule();` in `Program.cs`.
2. Repoint the stale guards that pin the retired ceremony to the certified `INTERNAL_ONLY` shape
   (`TaxArchitectureGuardTests.Endpoints_csproj_…`, `Program_maps_tax_module`,
   `TaxEndpointModuleTests`, `HostModuleEndpointOwnershipTests.Program_maps_offer_and_tax_modules`).
3. Make every production namespace path-derived (`Contracts.Dtos`, `Contracts.Ports`,
   `Application.Ports`, `Domain.Aggregates`, `Domain.Enums`, `Domain.Events`, `Domain.Policies`,
   `Infrastructure.Adapters`, `Infrastructure.DependencyInjection`, `Infrastructure.Outbox`,
   `Infrastructure.Events`, `Infrastructure.Persistence[.Configurations]`) and update the in-repo
   consumers that use the namespace.
4. Optionally split `Application/Ports/ITaxUseCaseGuard.cs` out of `ITaxDirectory.cs` for parity with
   the certified Inventory port layout.
5. Add `TaxModuleAmsc001W2StructureGuardTests`; update the manifest pre-cert entry.

**W3 — Certify** (`tooba-architecture-certify`)
1. `TaxModuleAmsc001W3CertGuardTests` — durable cert lock (SoT + manifest + declared-code/resource set
   equality + typed seam + INTERNAL_ONLY + zero foreign coupling + evidence tree).
2. Promote the manifest entry to `modules[]` with `structureCertified: true`; add `Tax` to
   `structureLock.certifiedModules` (31).
3. Record the W0/W1/W2/W3 lineage in `tmar-current-state.json`.
4. Master Recovery checkpoint in `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`.

---

## 18. Verification plan

Per wave: focused build of `Tooba.Tax.Tests` + `Tooba.Host.Tests`; focused run of the Tax behavior
suites, `TaxArchitectureGuardTests`, the new AMSC guard, `HostModuleEndpointOwnershipTests`,
`ContractsW4CharacterizationTests`, `HostDevelopmentEnricherClosureGuardTests`,
`HostDevelopmentMigrationSeamGuardTests`, `ErrorCatalogUniqueCodeGuardTests`, and
`Tooba.Tax.Tests` (Domain/Contracts/Infrastructure). No repository-wide test sweep. No guard weakened.

## 19. Certification blockers

**None blocking.** W0 recorded findings to be closed by W1/W2/W3:

1. 13 raw string faults + 1 English prose fault → unregistered stable codes.
2. Missing typed-fault → `Result` seam.
3. Ceremonial `Endpoints` project + empty route group + Host map call (INTERNAL_ONLY violation).
4. `Contracts`/`Domain`/`Application` namespaces not path-derived (13 files).
5. Stale `TaxFoundationTests.cs:74` assertion on the deleted `TaxDomain.cs`.

---

## 20. Structured state fields

```text
Foundation-State            : FOUNDATION_PARTIAL
Ownership-State             : correct
File-Cohesion-State         : COHESIVE
Oversized/God-File-State    : OVERSIZED_ONLY (TaxDirectory.cs 295 LOC, cohesive, < 800)
Localization-State          : MISSING_INFRASTRUCTURE_USE
API-Result-Pattern-State    : CANONICAL
Stable-Error-Code-State     : UNREGISTERED_CODES (13)
Logging-State               : CANONICAL
Sensitive-Logging-State     : NONE
OpenTelemetry-State         : CANONICAL
Correlation-Trace-State     : CANONICAL
CQRS-State                  : NOT_APPLICABLE (INTERNAL_ONLY)
Validator-Coverage-State    : EXHAUSTIVE (0 of 0)
Contracts-Boundary-State    : VIOLATION (namespace/path drift only; no semantic violation)
Cross-Module-Coupling-State : NONE
Cross-Module-Join-State     : NONE
Persistence-Ownership-State : CORRECT
Endpoint-Ownership-State    : MODULE_OWNED_ZERO_ROUTES_HOST_ZERO (ceremonial project to retire)
Host-Residue-State          : ALLOWED_COMPOSITION_ROOT (3 references)
Schema-Migration-State      : UNCHANGED
Behavior-Preservation-Risk  : LOW
Canonical-Reference-Used    : Inventory + Pricing (INTERNAL_ONLY Contracts/Errors + typed-fault seam +
                              Infrastructure composition-root error registration), BuildingBlocks
                              Result/SemanticError/ContractOperationException/IErrorCatalogContributor/
                              IErrorResourceSet/IErrorDefinitionCatalog
Structure-Handoff-State     : REQUIRED
HTTP-Applicability          : INTERNAL_ONLY
Final-Disposition           : READY_TO_MIGRATE
```

## 21. Destination-integrity check

No Host folder is a destination. `src/backend/Host/Tooba.Host/**` receives **no** new production file;
the only Host edits are the removal of the ceremonial `MapTaxModule()` composition call and the
`Tooba.Tax.Endpoints` project reference (a reduction, not a growth). `HOST_ROOT_FINAL_CERTIFIED` /
`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` are preserved. No closed folder is used as a sink.
