# TB-TMAR-PRICING-AMSC-001-W3-R3 — certification

- Module: `Pricing`
- Skill: `tooba-architecture-certify`
- Mode: `FRESH_CERTIFY_AFTER_INTERNAL_ONLY_STRUCTURE_REPAIR`
- Parent task: `TB-TMAR-PRICING-AMSC-001-W3-R2`
- Starting HEAD: `7159c8f773c1faa9b4b6d425b19067f50ca27572` (branch `main`, `HEAD == origin/main`)
- Structure authority consumed: `TB-TMAR-PRICING-AMSC-001-W3-R2` @ `7159c8f7` (`READY_FOR_CERTIFY`)
- Superseded certification: `TB-TMAR-PRICING-AMSC-001-W3` @ `3c2cc61e7c61813ac72773ccdb8bb16317cafe70`

## 1. Verdict

```text
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
Http-Applicability = NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER (INTERNAL_ONLY)
```

Pricing is a genuine INTERNAL_ONLY capability provider reached only through `Tooba.Pricing.Contracts`
ports. The prior W3 certification was structurally stale (it certified `NOT_HTTP_OWNING_*` while the
module still carried a ceremonial `Tooba.Pricing.Endpoints` project, an empty `/v1/pricing` route group
and Host mapping/registration ceremony). W3-R2 removed that ceremony; this wave independently
re-verified the repaired surface and promoted it honestly.

## 2. Structure gate consumption (mandatory)

The Structure gate for this exact surface is `TB-TMAR-PRICING-AMSC-001-W3-R2` at `7159c8f7`, which
returned `Structure-State = READY_FOR_CERTIFY`, `Folder-Granularity-State = PROFESSIONAL_SHALLOW`,
`Solution-Explorer-State = CANONICAL`, `Path-Namespace-State = EXACT`, `Physical-Copy-State = CLEAN`,
`Root-Allowlist-State = ENFORCED`, with Host final closure preserved.

Certify did **not** infer this PASS from compilation, namespaces or manifest membership: every
structural invariant was independently re-enumerated from disk (section 3) and is re-asserted durably by
`PricingModuleAmsc001W3R3CertGuardTests.Structure_invariants_hold_for_the_five_project_solution_group`.
The structure gate is current, scoped to this exact surface, and not contradicted by disk state.

## 3. Independent re-enumeration (machine-checked)

`node docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R3/certify-audit.cjs`
(full output: `audit-after.json`)

| Axis | Evidence | Result |
| --- | --- | --- |
| Project directories under `Modules/Pricing` | `Contracts, Domain, Application, Infrastructure, Tests` | exactly **5** |
| `Tooba.Pricing.Endpoints` directory / test `Endpoints` folder | `existsSync` | **ABSENT / ABSENT** |
| `/Modules/Pricing/` `.slnx` project entries | regex over the solution folder | **5**, no Endpoints entry |
| Production `MapGet/MapPost/MapPut/MapPatch/MapDelete/MapGroup` | source scan of all production `.cs` | **0 / 0 / 0 / 0 / 0 / 0** |
| Production `IEndpointRouteBuilder` / `ISender` / `MediatR` | source scan | **0 / 0 / 0** |
| Production `Tooba.Pricing.Endpoints` reference | source scan | **0** |
| Host `MapPricingModule` / `AddPricingEndpointPresentation` / Endpoints reference | `Program.cs` scan | **0 / 0 / 0** |
| Host `.csproj` Endpoints reference / Pricing Infrastructure reference | `.csproj` scan | **0 / 1** (composition root preserved) |
| Declared stable codes | `PricingErrorCodes` | **11** |
| `KnownCodes` HashSet + `IsKnown(string?)` | source scan | **present / present** |
| `PricingErrorCatalogContributor` classes / descriptor factories | Contracts/Errors | **1 / 11** |
| `PricingErrorResourceSet` classes | Contracts/Errors | **1** |
| EN / FA resource keys, key-set equality | `PricingErrors.resx`, `PricingErrors.fa.resx` | **11 / 11, identical** |
| `IErrorCatalogContributor` / `IErrorResourceSet` registrations | `PricingModule.AddServices` | **1 / 1** |
| Typed-fault `IsKnown` filter / `SemanticException` catch | `PricingOperation.cs` | **2 / 2** |
| `ex.Message` occurrences (seam / all production) | source scan | **0 / 0** |
| Path↔namespace mismatches (4 production projects) | derived-namespace comparison | **0** |
| Foreign Application/Infrastructure/Domain/Endpoints references | regex over production sources | **0** |
| `Promotion -> Pricing.Application` edges | `.csproj` scan | **0** |
| `Promotion -> Pricing.Contracts` edges | `.csproj` scan | **1** (legal inbound seam) |
| Pricing schema literal / `DbSet<AuthoredPrice>` / `PricingDbContext` classes | `PricingDbContext.cs` | **1 / 1 / 1** |
| `AddModuleSchemaMigrator("Pricing", ModuleSchemaMigrationOrder.Pricing, …)` | `PricingModule.cs` | **1** |
| Outbox registration | `PricingModule.cs` | **1** |
| Migration files | `Persistence/Migrations` | **`20260823085546_InitialPricing` + designer + snapshot (unchanged)** |
| Host Pricing business/persistence hits (`PricingDbContext`/`AuthoredPrice`/`IPriceDirectory`) | Host source scan | **0** |
| Host `Pricing` folder | `existsSync` | **ABSENT** |
| Largest production file | `PriceDirectory.cs` | **485 LOC** (< 800 threshold, guard `ARCH-SIZE-001` clean) |

## 4. Certification axes

### 4.1 Correct ownership
Authored-price truth (amount, currency, market, channel, validity, qualifier, status) is Pricing-owned.
Tax, FX, offer selection/lifecycle, promotion definition/discount evaluation and cart line state are
owned elsewhere. The seller price write stays Offer-owned HTTP (`POST|PUT /v1/seller/offers/{offerId}/price`
→ `SetOfferPriceCommand` → `ISellerOfferPricingGateway`), reached inside the Offer handler, not by a
direct endpoint→directory call.

### 4.2 INTERNAL_ONLY applicability
`moduleOwnedRouteCount = 0`, `hostOwnedRouteCount = 0`, `endpointReachableRequests = 0`. No `Endpoints`
project, no `PricingEndpointModule`, no `MapPricingModule`, no `AddPricingEndpointPresentation`, no
`"/v1/pricing"` group. There is no ceremonial project, no empty route group and no presentation
extension. This is the same shape as the certified Inventory INTERNAL_ONLY precedent.

### 4.3 CQRS / validators — `NOT_APPLICABLE_INTERNAL_ONLY`
Pricing owns zero endpoint-reachable requests, therefore the request→handler→validator matrix is empty by
construction. No `Commands`/`Queries`/`Validators`/`Models`/`Handlers`/`Requests` folder exists under
`Application` (asserted by both the W2 structure guard and the W3-R3 cert guard), and no MediatR/IRequest
ceremony was invented. Pricing re-validates its own boundary inputs inside the directory via
`MarketCode.TryParse`/`CurrencyCode.TryParse` → `Result.Failure`, which is business/domain validation and
correctly not FluentValidation.

### 4.4 Localization + stable errors
Single canonical home `Tooba.Pricing.Contracts.Errors.PricingErrorCodes` (11 declared codes, `KnownCodes`
+ `IsKnown`), one `PricingErrorCatalogContributor` contributing exactly 11 descriptors, one
`PricingErrorResourceSet` claiming the `pricing.` keyspace with the bilingual `PricingErrors.resx` /
`PricingErrors.fa.resx` pair (11 EN + 11 FA keys). The composed `ErrorDefinitionCatalog` contains no
duplicate Pricing descriptor; the `pricing.` keyspace is exclusively Pricing-owned (no foreign
re-registration). Every declared code resolves through the composed `IErrorMessageLocalizer` with real
Persian text (no fallback). Zero hard-coded user-facing prose, zero `Accept-Language` parsing.

### 4.5 API result / error mapping
Zero `Results.Json`/`Results.BadRequest`/`Results.Problem`, zero local ProblemDetails builder and zero
catch-and-map in Pricing production (Pricing owns no HTTP surface). `Application/Composition/PricingOperation.cs`
is the single dual typed-fault→`Result` seam: `ContractOperationException` filtered by
`PricingErrorCodes.IsKnown(ex.Code)` and `SemanticException`; classification is by typed code only, never
by `ex.Message` heuristics.

### 4.6 Logging / observability / correlation
Zero `Console.WriteLine`/`Debug.WriteLine`, no second telemetry pipeline, no sensitive-data logging, no
competing correlation mechanism, no direct `ActivitySource.StartActivity(...)` in Application. The
Pricing directory uses the canonical `IModuleCallTracer` decoration for the cross-module Offer lookup, so
trace continuity is preserved.

### 4.7 Cross-module boundary
`foreignAppInfraDomainCoupling = ZERO`. Outbound edges are `Tooba.Offer.Contracts` (legal lookup seam)
plus `BuildingBlocks`/`ModuleContracts`/`Persistence` foundations. Inbound, `Tooba.Promotion.Infrastructure`
consumes `Tooba.Pricing.Contracts.Ports` only; the illegal `Promotion.Infrastructure → Pricing.Application`
edge removed by W1 stays removed. No cross-module join, no foreign `DbContext`/`DbSet`, no direct
table/schema reach-through.

### 4.8 Persistence ownership
Own `pricing` schema, own `PricingDbContext`, own outbox registration, own `IPricingSchemaMigrator` /
`PricingModuleMigration` registered via `AddModuleSchemaMigrator("Pricing", ModuleSchemaMigrationOrder.Pricing, …)`.
Application and Endpoints own no `DbContext`. The single migration `20260823085546_InitialPricing`
(+ designer + snapshot) is byte-unchanged; `migrationFilesChanged = 0`.

### 4.9 Host authority
`ILLEGAL_BUSINESS_AUTHORITY = 0`, `ILLEGAL_PERSISTENCE_AUTHORITY = 0`, `ILLEGAL_ENDPOINT_OWNERSHIP = 0`.
No `Host/Pricing` folder; no Host file owns `PricingDbContext`/`AuthoredPrice`/`IPriceDirectory`. Host
keeps `ALLOWED_COMPOSITION_ROOT` only (`Program.cs` composition, `ToobaModuleComposition`, the
composition-root `ProjectReference`). `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED` and
`lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001` are preserved and not displaced.

### 4.10 Closed-folder regression
No file was added to any previously closed Host/module destination; the only new source file is the
W3-R3 guard test in the Host test project. No sink-folder regression, no resurrected closed folder.

### 4.11 Schema / migration safety
`schemaMigrationState = UNCHANGED`, `migrationFilesChanged = 0`, `productionCodeChanged = false`,
`behaviorPreservation = PRESERVED`. No migration was regenerated and no Up/Down/snapshot semantics changed.

### 4.12 Microservice extractability
Zero foreign Application/Infrastructure/Domain project edge in any Pricing project; the only foreign
reference is the legal `Tooba.Offer.Contracts` boundary. Pricing can be extracted into an isolated
microservice with its own schema, outbox and migration without touching callers.

## 5. Manifest + SoT promotion

- `docs/architecture/tmar-module-structure-manifests.json`: `Pricing` moved out of `preCertModules`
  into the certified `modules` array — `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`,
  refreshed W3-R3 `certificationNote`, `structureAuthorityTask = TB-TMAR-PRICING-AMSC-001-W3-R2`,
  `structureAuthorityCommit = 7159c8f7…`, 5 project entries, no Endpoints entry. Certified module count
  **25 → 26**; `preCertModules` now holds only `ProductWorkspace`. The Pricing entry's structural
  allowlists/forbidden lists were byte-preserved (verified by a before/after structural comparison).
- `docs/architecture/tmar-current-state.json`: `"Pricing"` re-added to `structureLock.certifiedModules`
  (**24 → 25** members, present exactly once, alphabetical position after `Payment`); new
  `pricingAmsc001W3R3` block records the fresh certification. The historical `pricingAmsc001W3`,
  `pricingAmsc001W3R1` and `pricingAmsc001W3R2` blocks are untouched.
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`: appended the W3-R3 fresh-certification checkpoint
  recording the superseded original W3, the current W3-R2 structure authority, zero Endpoints / zero
  routes, the Infrastructure composition registration, the promotion back into the certified set, the
  preserved Host checkpoint, unchanged schema/migrations and the disclosed recovery debt.

## 6. Durable guards

New: `src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W3R3CertGuardTests.cs` (**8 tests**)

- `Pricing_is_freshly_certified_in_manifest_and_sot`
- `Internal_only_applicability_holds_zero_routes_and_zero_requests`
- `Structure_invariants_hold_for_the_five_project_solution_group`
- `Stable_codes_descriptors_and_bilingual_resources_are_exact`
- `Typed_fault_seam_classifies_by_code_only`
- `Boundaries_stay_contracts_only_and_persistence_stays_module_owned`
- `Host_authority_is_zero_and_global_closure_is_preserved`
- `Structure_authority_and_superseded_lineage_are_recorded`

Repointed without weakening (manifest/SoT certification claim only; every structural assertion kept):

| File | Change |
| --- | --- |
| `PricingModuleAmsc001W2StructureGuardTests.cs` | `Root_allowlists_and_forbidden_lists_match_the_manifest` now reads the certified `modules` entry and asserts Pricing is absent from `preCertModules` (test count unchanged at 8) |
| `PricingModuleAmsc001W3R2RepairGuardTests.cs` | `Prior_w3_certification_is_superseded_and_pricing_is_precert_ready_for_certify` → `…_is_superseded_and_pricing_is_recertified`; every W3-R2 history assertion kept verbatim, certified-set assertions repointed to the promoted truth, plus two new pins for the W3-R3 block and exactly-once `structureLock` membership (9 → 11) |
| `TmarCompleteReferenceStructureGateTests.cs` | `Pricing` re-added to the expected certified-module lists (manifest **26**, `structureLock` **25**) |

`guardsWeakened = NONE`; `baselinesWidened = NONE`; no assertion was relaxed, removed or widened.

## 7. Validation

See `validation.md`. Focused results: `Tooba.Pricing.Tests` **13/13 passed**; all four Pricing AMSC guard
suites + `PricingArchitectureGuardTests` + `PricingErrorCatalogTests` + `PricingDbContextOwnershipTests` +
`ErrorCatalogUniqueCodeGuardTests` + `HostModuleEndpointOwnershipTests` green; full `Tooba.Host.Tests`
suite **2044 passed / 130 skipped / 79 failed / 2253 total** against the measured starting-head baseline
**2036 / 130 / 79 / 2245** — **zero new failures**, with the `+8 passed` delta exactly the new W3-R3 cert
guard suite; solution build **0 errors**.

## 8. Recovery debt disclosure (deliberately NOT repaired in this wave)

`POST_CERT_RECOVERY_RECONCILIATION_REQUIRED`:

- `pricingAmsc001W0` still carries self-referential `PENDING_THIS_COMMIT` placeholders in `commit` /
  `commitFull`;
- the `pricingAmsc001W1` and `pricingAmsc001W2` blocks record only `parentCommit` with no final
  `commit` / `commitFull` field of their own;
- the historical `pricingAmsc001W3R1` block has no final commit SHA of its own.

These are historical lineage-bookkeeping gaps. They are recorded honestly here and were **not** repaired
in this Certify wave, per the task's `FORBIDDEN` list. No self-referential placeholder was introduced for
this wave either: the W3-R3 certification commit SHA is reported in the Bridge result and recorded as
`certificationCommitState = REPORTED_IN_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_SOT_SHA`.

## 9. Stop gate

`workflowStop = USER_REVIEW_PRICING_AMSC_001_W3_R3`; `automaticNextImplementationTask = NONE`.
