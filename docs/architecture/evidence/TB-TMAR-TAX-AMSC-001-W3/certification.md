# TB-TMAR-TAX-AMSC-001-W3 — Certification (tooba-architecture-certify)

- **Task**: `TB-TMAR-TAX-AMSC-001-W3` (Certify — the fourth and final wave of the AMSC pipeline)
- **Skill**: `.cursor/skills/tooba-architecture-certify/SKILL.md`
- **Standard**: `ARCH-COMPLETE-002`
- **Target**: `src/backend/Modules/Tax/Tooba.Tax.*`
- **Parent task**: `TB-TMAR-TAX-AMSC-001-W2` (Structure)
- **Starting head**: `eff3cf5b846d33f90652ee691fa23048ffa45f48` (`HEAD == origin/main`)
- **Wave lineage**: W0 `36d243cc` Analyze → W1 `fa87201a` Migrate → W2 `eff3cf5b` Structure → W3 (this wave)
- **Verdict**: `COMPLETE_REFERENCE_PATTERN` — `ARCH-COMPLETE-002` `STRUCTURE_CERTIFIED`

Tax had **no** prior ARCH-COMPLETE-002 certification, **no** manifest entry and was **not** a member of
`structureLock.certifiedModules`. There was no prior accepted baseline to preserve; this wave creates the
first one. No production file was changed by W3: the wave verifies, locks and promotes.

**Final objective**: Tax must be extractable as an independent microservice. The two hard gates are
**zero invalid coupling** and an **exact path↔namespace** boundary. Both are verified below from disk.

---

## 1. Structure gate (mandatory precondition, consumed from W2)

`.cursor/skills/tooba-architecture-structure/SKILL.md` is the structure authority. Its W2 PASS for the same
touched surface is consumed as-is and **not** re-inferred from compilation, namespace checks or manifest
membership.

| Structure gate field | Required | Observed |
| --- | --- | --- |
| `Structure-State` | `READY_FOR_CERTIFY` | `READY_FOR_CERTIFY` (W2) |
| `Folder-Granularity-State` | `PROFESSIONAL_SHALLOW` | `PROFESSIONAL_SHALLOW` |
| `Solution-Explorer-State` | `CANONICAL` / `NOT_APPLICABLE` | `CANONICAL` (`/Modules/Tax/`, 5 projects) |
| `Path-Namespace-State` | `EXACT` | `EXACT` (0 mismatches) |
| `Physical-Copy-State` | `CLEAN` | `CLEAN` |
| `Root-Allowlist-State` | `ENFORCED` | `ENFORCED` |
| single-file request/use-case leaf folders | none unjustified | none (zero endpoint-reachable requests) |
| technical-axis-first request tree | none | none |
| root dump / folder explosion / structure god-file | none | none (`File-Cohesion-State = COHESIVE`) |
| Host final closure preserved | yes | yes |

Structural invariants are re-asserted as defense in depth in
`TaxModuleAmsc001W3CertGuardTests.Tax_structure_invariants_hold_for_the_five_project_solution_group`.

## 2. Applicability certification gate (§0b)

Classification: **`INTERNAL_ONLY`**.

Evidence (W0 applicability gate, preserved by W2, re-verified now):

- `Tooba.Tax.Endpoints` project **absent**; `Tooba.Tax.Tests/Endpoints` **absent**;
  `src/backend/Host/Tooba.Host/Tax` **absent**.
- Zero `MapGroup(` / `MapGet(` / `MapPost(` / `MapPut(` / `MapPatch(` / `MapDelete(` /
  `IEndpointRouteBuilder` / `"/v1/tax"` / `MapTaxModule` / `TaxEndpointModule` /
  `AddTaxEndpointPresentation` in any Tax production file.
- Zero `ISender` / `MediatR` in any Tax production file; `Application/` contains exactly
  `Composition/` + `Ports/` and no `Commands` / `Queries` / `Validators` / `Models` / `Handlers` /
  `Requests` folder.
- `Program.cs` carries no Tax mapping; `Tooba.Host.csproj` carries no `Tooba.Tax.Endpoints` reference.

| Certify concern | Applicability |
| --- | --- |
| endpoint ownership (§4) | `NOT_APPLICABLE_INTERNAL_ONLY` |
| CQRS / MediatR (§5) | `NOT_APPLICABLE_INTERNAL_ONLY` |
| validator coverage (§6, §6a) | `NOT_APPLICABLE_INTERNAL_ONLY` (`EXHAUSTIVE_0_OF_0_NO_VALIDATOR_REQUIRED`) |
| API result mapping (§8) | `CANONICAL` (no HTTP route → no `ApiResponseFactory` call site to own) |

No ceremonial surface remains merely for framework symmetry, and no real HTTP surface was removed.
The classification and the physical project set do not contradict each other.

## 3. Final physical tree

```
src/backend/Modules/Tax/
  Tooba.Tax.Contracts/            (root allowlist: [])
    Dtos/TaxOutcome.cs
    Errors/TaxErrorCodes.cs
    Errors/TaxErrorCatalogContributor.cs
    Errors/TaxErrorResourceSet.cs
    Ports/ITaxDevelopmentSeedGateway.cs
    Ports/ITaxSchemaMigrator.cs
    Ports/TaxCalculatorContracts.cs
    Ports/TaxQueryContracts.cs
    Resources/TaxErrors.resx
    Resources/TaxErrors.fa.resx
  Tooba.Tax.Domain/               (root allowlist: [GlobalUsings.cs])
    GlobalUsings.cs               (namespace bridge; declares no namespace, holds no type)
    Aggregates/TaxCategory.cs
    Aggregates/TaxOfferClassification.cs
    Aggregates/TaxRule.cs
    Enums/TaxRuleEnums.cs
    Events/TaxDomainEvents.cs
    Policies/TaxRounding.cs
  Tooba.Tax.Application/          (root allowlist: [])
    Composition/TaxOperation.cs
    Ports/ITaxDirectory.cs
    Ports/ITaxUseCaseGuard.cs
  Tooba.Tax.Infrastructure/       (root allowlist: [])
    Adapters/TaxDevelopmentSeedGateway.cs
    Adapters/TaxDirectory.cs
    Adapters/TaxModuleMigration.cs
    Adapters/TaxSchemaMigrator.cs
    DependencyInjection/TaxModule.cs
    Events/TaxEvents.cs
    Outbox/TaxOutboxRegistration.cs
    Persistence/TaxDbContext.cs
    Persistence/Configurations/{TaxCategory,TaxOfferClassification,TaxRule}Configuration.cs
    Persistence/Migrations/20260823190000_InitialTax.cs (+ .Designer.cs)
    Persistence/Migrations/TaxDbContextModelSnapshot.cs
  Tooba.Tax.Tests/                (root allowlist: [])
    Architecture/TaxArchitectureGuardTests.cs
    Contracts/TaxCalculationShapeTests.cs
    Domain/TaxRuleInvariantTests.cs
    Infrastructure/TaxDbContextOwnershipTests.cs
```

## 4. Root allowlists and path↔namespace proof

Root allowlists are the certified manifest entry's values, reconciled against disk by the guard.

| Project | rootAllowlist | forbiddenRootFiles | forbiddenTopLevelFolders |
| --- | --- | --- | --- |
| `Tooba.Tax.Contracts` | `[]` | `TaxErrorCodes.cs`, `TaxCalculatorContracts.cs`, `TaxQueryContracts.cs`, `ITaxDevelopmentSeedGateway.cs`, `ITaxSchemaMigrator.cs`, `TaxOutcome.cs` | `[]` |
| `Tooba.Tax.Domain` | `[GlobalUsings.cs]` | `TaxDomain.cs`, `TaxRule.cs`, `TaxCategory.cs`, `TaxOfferClassification.cs`, `TaxRuleEnums.cs`, `TaxDomainEvents.cs`, `TaxRounding.cs` | `Errors`, `ValueObjects` |
| `Tooba.Tax.Application` | `[]` | `TaxOperation.cs`, `ITaxDirectory.cs`, `ITaxUseCaseGuard.cs`, `TaxContracts.cs`, `TaxErrorCodes.cs` | `Commands`, `Queries`, `Validators`, `Models`, `Handlers`, `Requests` |
| `Tooba.Tax.Infrastructure` | `[]` | `TaxModule.cs`, `TaxDirectory.cs`, `TaxDbContext.cs`, `TaxEvents.cs`, `TaxOutboxRegistration.cs`, `TaxModuleMigration.cs`, `TaxSchemaMigrator.cs`, `TaxDevelopmentSeedGateway.cs` | `Migrations`, `Repositories`, `Directories`, `Messaging` |
| `Tooba.Tax.Tests` | `[]` | — | `Endpoints` |

Path↔namespace: every production `.cs` declares exactly its path-derived namespace
(`Tooba.Tax.Contracts.Dtos`, `Tooba.Tax.Contracts.Ports`, `Tooba.Tax.Contracts.Errors`,
`Tooba.Tax.Domain.Aggregates`, `Tooba.Tax.Domain.Enums`, `Tooba.Tax.Domain.Events`,
`Tooba.Tax.Domain.Policies`, `Tooba.Tax.Application.Composition`, `Tooba.Tax.Application.Ports`,
`Tooba.Tax.Infrastructure.Adapters`, `Tooba.Tax.Infrastructure.DependencyInjection`,
`Tooba.Tax.Infrastructure.Events`, `Tooba.Tax.Infrastructure.Outbox`,
`Tooba.Tax.Infrastructure.Persistence`). The only file exempt from the rule is the Domain
`GlobalUsings.cs`, which is pure import aggregation and declares no namespace by design. Machine-verified
**0 mismatches**.

## 5. Alias / shim proof

`TypeForwardedTo` — **absent** in all Tax production. No `global using` alias bridging a namespace
mismatch, no duplicate compatibility type, no stale root copy, no duplicate physical copy. The Domain
`GlobalUsings.cs` is a *namespace bridge for consumers*, not a type/namespace workaround: it declares no
namespace and no type.

## 6. Endpoint ownership / route count

`NOT_APPLICABLE_INTERNAL_ONLY`. Module-owned route count **0**; Host-owned Tax route count **0**.

## 7. Request → handler → validator matrix

Empty by construction: **0** endpoint-reachable requests, **0** `IRequest`, **0** `IRequestHandler`,
**0** `ISender` call sites, **0** validators. `EXHAUSTIVE_0_OF_0_NO_VALIDATOR_REQUIRED`. Set equality
holds trivially and no FluentValidation pipeline is registered for Tax. The §6a hard gate is satisfied by
the absence of any input provenance to classify; the durable guard prevents a future wave from inventing
a request/validator tree without re-opening applicability.

## 8. Validator coverage

`NOT_APPLICABLE_INTERNAL_ONLY`. No validator file exists; no validator may be invented for an
INTERNAL_ONLY module.

## 9. Localization coverage (codes → catalog → resources)

- Declared code home: `Tooba.Tax.Contracts/Errors/TaxErrorCodes.cs` — **11** `public const string` codes
  plus a `KnownCodes` `HashSet<string>(StringComparer.Ordinal)` and `IsKnown(string?)`.
- Descriptors: `TaxErrorCatalogContributor.Contribute()` — **11** descriptors, one per declared code,
  each `LocalizationKey == Code`, all `tax.`-prefixed. No duplicate machine code; no first/last-wins,
  overwrite, `DistinctBy` or catch-and-ignore suppression.
- Composed catalog: `new ErrorDefinitionCatalog([FoundationErrorCatalogContributor, TaxErrorCatalogContributor])`
  is constructible and unique; `RegisteredCodes` filtered by the `tax.` prefix equals exactly the 11
  declared codes; every descriptor resolves to exactly one canonical owner.
- Descriptor ownership: `checkout.tax.unavailable` is **Order-owned** (`StorefrontOrderErrors`) and is
  deliberately absent from both the code home and the contributor. Duplicate *usage* is allowed;
  duplicate *ownership* is not.
- Resources: `Resources/TaxErrors.resx` and `Resources/TaxErrors.fa.resx` each carry exactly the 11
  declared keys; `ResourceErrorMessageLocalizer([FoundationErrorResourceSet, TaxErrorResourceSet], [])`
  resolves every code in `en` and `fa` with no fallback and real Persian text.
- Registration: `TaxModule.AddServices` registers `IErrorCatalogContributor → TaxErrorCatalogContributor`
  and `IErrorResourceSet → TaxErrorResourceSet` **exactly once** (the certified Inventory/Pricing
  internal-only composition-root precedent). Both concrete types are `Tooba.Tax.Contracts.Errors`-owned.
- Zero hard-coded Persian/English user-facing strings in Domain/Application/Infrastructure; zero
  `exception.Message` used as a user-facing contract; zero `Accept-Language` parsing.

## 10. API result / error mapping proof

`CANONICAL` with no owned HTTP route: zero `Results.Json`, zero `Results.BadRequest`, zero
`Results.Problem`, zero local `ProblemDetails` builder, zero catch-and-map blocks, zero failure
classification by message text in the Tax production surface.

The typed-fault seam `TaxOperation` (`Tooba.Tax.Application.Composition`) maps only typed, code-carrying
faults into `Result`:

- `catch (ContractOperationException ex) when (TaxErrorCodes.IsKnown(ex.Code))` → `Result.Failure(new SemanticError(ex.Code))`
- `catch (SemanticException ex)` → `Result.Failure(ex.Error)`

A `ContractOperationException` whose code is not a declared Tax code, and every unknown exception,
propagates untouched to the canonical global exception boundary. Zero `ex.Message`, zero `StartsWith`,
zero `IResult` in the seam — it can never become an endpoint mapper.

## 11. Logging / sensitive-data proof

`CANONICAL` / `ZERO_CALL_SITES`: zero `ILogger<T>` misuse, zero `Console.WriteLine`, zero
`Debug.WriteLine`, zero second telemetry pipeline, zero hand-rolled writer. Nothing to repair and nothing
to add. No sensitive authentication material is logged anywhere in Tax.

## 12. Correlation / trace continuity proof

`CANONICAL`: zero `new ActivitySource(`, zero `traceparent` parsing, zero competing/parallel correlation
id, zero custom header or raw `AsyncLocal` in the Tax production surface. `ToobaTelemetry` /
`IModuleCallTracer` / `ICorrelationIdProvider` continuity is untouched.

## 13. File cohesion / size proof

`COHESIVE`. Zero file over the 800 LOC `ARCH-SIZE-001` ceiling. The largest files are single-purpose:
`Infrastructure/Adapters/TaxDirectory.cs` (one four-port adapter over one schema),
`Domain/Aggregates/TaxRule.cs` (one aggregate). No multi-responsibility god-file, no artificial parallel
decomposition, no single-file-per-request leaf tree.

## 14. Host authority classification

| Host reference | Classification |
| --- | --- |
| `Program.cs` (no Tax mapping) | `ALLOWED_COMPOSITION_ROOT` |
| `Composition/ToobaModuleComposition.cs` (`new TaxModule()`) | `ALLOWED_COMPOSITION_ROOT` |
| `Tooba.Host.csproj` (`Tax.Application` / `Tax.Infrastructure`) | `ALLOWED_COMPOSITION_ROOT` |
| `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` (`TaxModuleMigration` descriptor) | `ALLOWED_COMPOSITION_ROOT` |

`ILLEGAL_BUSINESS_AUTHORITY` = 0, `ILLEGAL_PERSISTENCE_AUTHORITY` = 0,
`ILLEGAL_ENDPOINT_OWNERSHIP` = 0. No Host Tax folder exists. W3 added, moved and widened **zero** Host
production files. `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` / `HOST_ROOT_FINAL_CERTIFIED` preserved.

## 15. Cross-module dependency inventory

Outbound edges, complete:

| Project | Edges |
| --- | --- |
| `Tooba.Tax.Contracts` | `Tooba.BuildingBlocks` |
| `Tooba.Tax.Domain` | `Tooba.BuildingBlocks`, `Tooba.Tax.Contracts` |
| `Tooba.Tax.Application` | `Tooba.BuildingBlocks`, `Tooba.Tax.Contracts`, `Tooba.Tax.Domain` |
| `Tooba.Tax.Infrastructure` | `Tooba.BuildingBlocks` (via `Tooba.ModuleContracts`, `Tooba.Persistence`), `Tooba.Tax.Application`, `Tooba.Tax.Contracts` |

**Zero** foreign module `Application` / `Infrastructure` / `Domain` / `Endpoints` reference.
Inbound: `Order.Infrastructure`, `Catalog.Infrastructure` and `ProductWorkspace.Application` reference
`Tooba.Tax.Contracts` only (repointed by W2 to the path-derived namespaces). No foreign module gained a Tax
application/infrastructure/domain edge.

## 16. Explicit no-cross-module-join proof

No `HasOne` / `WithMany` / foreign `HasForeignKey` / `FromSql` / raw SQL in `TaxDbContext` or the three
`IEntityTypeConfiguration` implementations. Zero foreign `DbContext` type name in Tax production. Zero
cross-module EF/SQL join. `offer_classifications` keeps a plain `OfferId` primary key and `rules.CategoryId`
is a plain `Guid` with no cross-module navigation property.

## 17. Persistence / schema safety

- Own `tax` schema (`public const string Schema = "tax"`, `HasDefaultSchema(Schema)`), own `TaxDbContext`
  + `TaxDbContextFactory`, own `TaxOutboxRegistration : IOutboxModuleRegistration`, own
  `ITaxSchemaMigrator` + `TaxModuleMigration`.
- Exactly one migration: `20260823190000_InitialTax.cs` (+ `.Designer.cs`) plus
  `TaxDbContextModelSnapshot.cs`. No migration outside `Persistence/Migrations`.
- Byte-identical to the W0 baseline. No new migration, no schema change, no migration id/order/Up/Down
  change. The W2 namespace split has zero EF model surface consequence (the migration only calls
  `modelBuilder.HasDefaultSchema("tax")`).
- Locked outbox integration event names unchanged: `tax.rule_created.v1`, `tax.rule_activated.v1`,
  `tax.rule_changed.v1`, `tax.calculation_failed.v1`.

## 18. Durable guards

| Guard | Facts | Result |
| --- | --- | --- |
| `TaxModuleAmsc001W1MigrateGuardTests` | 8 | pass |
| `TaxModuleAmsc001W2StructureGuardTests` (repointed to the promoted `modules[]` entry) | 8 | pass |
| `TaxModuleAmsc001W3CertGuardTests` (new) | 8 | pass |
| `Tooba.Tax.Tests` (module behavior/ownership/contracts) | 11 | pass |
| `TmarCompleteReferenceStructureGateTests` (certified-module pins extended with `Tax`) | — | see §21 |
| `PricingModuleAmsc001W3R3CertGuardTests` (pinned arrays extended with `Tax`) | — | pass |

New guard coverage: manifest/SoT certification + wave lineage; INTERNAL_ONLY zero-route zero-ceremony
shape; single stable-code owner + 11 descriptors + 11 EN/11 FA resources + composed-catalog uniqueness;
canonical typed-fault seam + exactly-once composition-root registration; Contracts-only microservice
boundary; unchanged schema/migration set; structure invariants (root allowlists, capability folders, exact
path↔namespace, physical copies, solution grouping, manifest reconciliation); evidence tree + preserved
Host closure. No guard was weakened and no baseline was widened.

## 19. Manifest state

`Tax` was **moved** (not copied) from `preCertModules` into the certified `modules[]` array:

- `structureCertified: true`
- `lockVersion: ARCH-COMPLETE-002`
- `certificationState: TAX_AMSC_001_CERTIFIED`
- `currentCertificationTask: TB-TMAR-TAX-AMSC-001-W3`
- `currentVerdict: COMPLETE_REFERENCE_PATTERN`
- `structureAuthorityTask: TB-TMAR-TAX-AMSC-001-W2`, `structureAuthorityCommit: eff3cf5b846d33f90652ee691fa23048ffa45f48`
- `projects`: 5 entries (4 production + Tests) with disk-accurate allowlists/forbidden lists

`preCertModules` is now **empty**. `Tax` is absent from `uncertifiedHttpOwningModules` (it owns zero HTTP
routes). The certified `modules[]` array grew from 30 to **31** entries with `Tax` present exactly once.

## 20. SoT state

- `structureLock.certifiedModules`: 30 → **31** members, `Tax` present exactly once.
- `taxAmsc001W3`: `state = TAX_AMSC_001_CERTIFIED`, `verdict = COMPLETE_REFERENCE_PATTERN`,
  `lockVersion = ARCH-COMPLETE-002`, `structureCertified = true`, `httpApplicability = INTERNAL_ONLY`,
  `endpointOwnershipState = NOT_APPLICABLE_INTERNAL_ONLY`, `endpointReachableRequests = 0`,
  `cqrsState = NOT_APPLICABLE_INTERNAL_ONLY`,
  `validatorMatrixState = EXHAUSTIVE_0_OF_0_NO_VALIDATOR_REQUIRED`, `pathNamespaceState = EXACT`,
  `rootAllowlistState = ENFORCED`, `aliasWorkaroundState = NONE`,
  `folderGranularityState = PROFESSIONAL_SHALLOW`, `solutionExplorerState = CANONICAL`,
  `physicalCopyState = CLEAN`, `foreignApplicationInfrastructureDomainEdges = ZERO`,
  `crossModuleJoinState = ZERO`, `schemaState = UNCHANGED`, `microserviceExtractable = true`,
  `hostFinalClosureState = HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED_PRESERVED`,
  `guardsWeakened = NONE`, `baselinesWidened = NONE`, `productionCodeChanged = false`,
  `stopGate = USER_REVIEW_TAX_AMSC_001_W3`, `automaticNextImplementationTask = NONE`.
- `taxAmsc001W2.commit` reconciled from `PENDING_W2_COMMIT` to `eff3cf5b`.
- Wave lineage: `taxAmsc001W0.commit = 36d243cc`, `taxAmsc001W1.commit = fa87201a`,
  `taxAmsc001W2.commit = eff3cf5b`, `taxAmsc001W3.startingHead = eff3cf5b…`.
- Master Recovery: `Tax AMSC module recovery checkpoint W3 (authoritative, module-local)` appended.
- Repository-global Host root checkpoint untouched: `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`,
  `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`, `automaticNextImplementationTask = NONE`.

## 21. Focused builds and tests

See `validation.md` in this folder for the exact commands and results.

## 22. Residual non-blocking debt

- `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
  fails for a **pre-existing, unrelated** deviation: `Tooba.Catalog.Contracts/Cart` and
  `Tooba.Cart.Contracts/{Checkout,Presentation}` declare the project-level namespace while sitting in a
  capability folder. This is documented in the gate source itself as out of scope for a module-local
  certification and is identical at the W2 head — **not** a Tax defect and **not** repaired here.
- The remaining Host-suite failures are pre-existing and identical at the W2 head (see `validation.md`).

## 23. Certification verdict

```
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
```

`structureCertified: true`; `microserviceExtractable = true`
(`TRUE_CONTRACTS_ONLY_SELF_CONTAINED_ERROR_SURFACE_EXACT_PATH_NAMESPACE`).

Stop gate `USER_REVIEW_TAX_AMSC_001_W3`; `automaticNextImplementationTask = NONE`.
