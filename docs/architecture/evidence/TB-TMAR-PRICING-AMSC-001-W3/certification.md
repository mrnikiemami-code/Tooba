# TB-TMAR-PRICING-AMSC-001-W3 — certification (tooba-architecture-certify)

- Module: `Pricing`
- Skill: `tooba-architecture-certify`
- Lock version: `ARCH-COMPLETE-002`
- Verdict: **`COMPLETE_REFERENCE_PATTERN`** (`structureCertified: true`)
- HTTP applicability: `NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER`
- Host final closure: preserved (`HOST_ROOT_FINAL_CERTIFIED` untouched)
- Behavior: `PRESERVED` (zero production code change in this wave)
- Microservice extractability: **`TRUE`**

## 1. AMSC lineage

| Wave | Skill | Commit (short) | Commit (full) | Verdict |
| --- | --- | --- | --- | --- |
| W0 | `tooba-architecture-analyze` | `08d47b6a` | `08d47b6a…` | `READY_TO_MIGRATE` |
| W1 | `tooba-architecture-migrate` | `069f77d2` | `069f77d2…` | `READY_TO_STRUCTURE` |
| W2 | `tooba-architecture-structure` | `f7f6abfe` | `f7f6abfec455b771952852e8627df4c57b698caf` | `READY_FOR_CERTIFY` |
| W3 | `tooba-architecture-certify` | *(this commit)* | *(this commit)* | `COMPLETE_REFERENCE_PATTERN` |

`Pricing` had **no** prior `ARCH-COMPLETE-002` certification, no manifest entry and was **not** a member
of `structureLock.certifiedModules` before this wave. `structureGateSource = TB-TMAR-PRICING-AMSC-001-W2
(f7f6abfe, READY_FOR_CERTIFY, current, same surface)`.

## 2. Certification axes

| Axis | Verdict | Evidence |
| --- | --- | --- |
| Structure state | `CERTIFIED` | W2 structure gate green on disk + `PricingModuleAmsc001W2StructureGuardTests` (9/9) |
| Path ↔ namespace | `EXACT` | every production `.cs` declares the path-derived namespace (GlobalUsings excluded by design) |
| Root allowlist | `ENFORCED` | per-project `rootAllowlist` in the manifest equals disk for all five projects |
| Physical copy | `CLEAN` | one `PricingErrorCodes`, one `PricingErrorCatalogContributor`, one `PricingErrorResourceSet` |
| Folder granularity | `PROFESSIONAL_SHALLOW` | capability-first; zero technical-axis-first root; zero use-case leaf |
| Solution explorer | `CANONICAL` | `/Modules/Pricing/` groups all 6 projects matching disk |
| Alias workaround | `NONE` | zero project-level alias; zero `TypeForwardedTo`; path-derived usings only |
| File cohesion | `COHESIVE` | largest `PriceDirectory.cs` 477 LOC single four-port adapter (guard `<800`) |

## 3. Stable-code + localization single ownership

- Canonical home: `Tooba.Pricing.Contracts/Errors/PricingErrorCodes.cs` (`namespace Tooba.Pricing.Contracts.Errors`).
- Declared-code guard: `private static readonly HashSet<string> KnownCodes` + `public static bool IsKnown(string?)`.
- Declared codes **11** = registered descriptors **11** = resource keys **11 × 2 cultures**.
- The W0/W1 duplicate `Tooba.Pricing.Domain/Errors/PricingErrorCodes.cs` stays **retired**; `Domain/Errors` is a forbidden root folder.
- Descriptor ownership is unique: every `pricing.*` descriptor in the composed catalog is Pricing-owned;
  Pricing never re-registers a foreign-owned descriptor and no other module claims a `pricing.` code.
- `PricingErrorResourceSet.Owns` claims only the `pricing.` keyspace; the `.resx` pair is embedded from
  the **Contracts** assembly (`Tooba.Pricing.Contracts.Resources.PricingErrors[.fa].resources`), so the
  module carries its own user-facing text when extracted as a microservice.
- Bilingual resolution is verified through the composed localizer (`FoundationErrorResourceSet` +
  `PricingErrorResourceSet`) for all 11 codes in `en` and `fa`.

## 4. Typed-fault seam

`Tooba.Pricing.Application/Composition/PricingOperation.cs` — `ExecuteAsync<T>` + value-less
`ExecuteAsync`, `catch (ContractOperationException ex) when (PricingErrorCodes.IsKnown(ex.Code))` →
`SemanticError(code)`, `catch (SemanticException ex)` → `ex.Error`; unknown codes and unknown exceptions
propagate untouched; **classification by typed code only**, never by message text. Registered exactly
once by `PricingEndpointModule.AddPricingEndpointPresentation()` (one `IErrorCatalogContributor`, one
`IErrorResourceSet`).

## 5. HTTP applicability — deliberate internal capability provider

Pricing owns **zero** HTTP routes: `MapPricingModule` maps the deliberately empty `/v1/pricing` group and
`PricingEndpointModule` maps **no** request (`MapGet`/`MapPost`/`MapPut`/`MapDelete`/`MapPatch` = 0) and
never dispatches through `ISender`. Endpoint-reachable requests **0**, required validators **0**
(`EXHAUSTIVE_0_OF_0_NO_VALIDATOR_REQUIRED`), `cqrs = NOT_APPLICABLE_TODAY`.

The only Pricing-reachable write is the **Offer-owned** seller price route
`POST|PUT /v1/seller/offers/{offerId}/price` → `ISender` → `SetOfferPriceCommand` →
`ISellerOfferPricingGateway` (`Tooba.Pricing.Contracts.Seller`). Transport validation of that write stays
Offer-owned (`SetOfferPriceCommandValidator`); Pricing re-validates its own boundary inputs inside the
directory through `MarketCode.TryParse` / `CurrencyCode.TryParse` → `Result.Failure`, which is
business/domain validation and correctly **not** FluentValidation. Certifying Pricing as HTTP-owning would
have required inventing `Commands/Queries/Validators` ceremony — the actual structural defect.

## 6. Boundary hygiene / microservice extractability

- **Outbound**: `Tooba.Pricing.Application`/`Infrastructure` → `Tooba.Offer.Contracts` (the Offer lookup
  seam) plus `BuildingBlocks`/`ModuleContracts`/`Persistence` foundations. Every outbound project edge is
  Contracts-only or internal own-module layering.
- **Inbound**: `Tooba.Promotion.Infrastructure` consumes `Tooba.Pricing.Contracts.Ports` only; the illegal
  `Promotion.Infrastructure → Pricing.Application` edge removed by W1 stays removed and guarded
  (`PromotionArchitectureGuardTests.Infrastructure_uses_contracts_not_foreign_application` green).
- `Endpoints` does **not** reference `Infrastructure` or `Domain`.
- `foreignAppInfraDomainCoupling = ZERO`; `crossModuleJoinState = NONE`; zero foreign `DbContext`
  reach-through; zero `TypeForwardedTo`.
- **Persistence**: own `pricing` schema, own `PricingDbContext` + `PricingDbContextFactory`, own single
  migration `20260823085546_InitialPricing` (+ designer + snapshot), own `PricingOutboxRegistration`,
  own `IPricingSchemaMigrator`/`PricingModuleMigration` (`AddModuleSchemaMigrator("Pricing", …)`) so Host
  bootstraps never type `PricingDbContext`.
- **Host residue**: `ALLOWED_COMPOSITION_ROOT` only — `Program.cs` (`AddPricingEndpointPresentation()`,
  `MapPricingModule()`), `Composition/ToobaModuleComposition.cs` (`new PricingModule()`) and the Host
  `.csproj` references. Zero Host production file added/moved/widened; zero Host-owned Pricing folder,
  route, `PricingDbContext`, `AuthoredPrice` or `IPriceDirectory` reference.

## 7. Behavior preservation

Zero production code changed in W3. Routes (the empty `/v1/pricing` group), the 11 descriptors, the 11
stable-code string values, the bilingual resource keys/text, the `IPriceDirectory` member set and
`PriceDirectory` implementation, DI lifetimes, the `pricing` schema, the single migration and the outbox
translation are **byte-identical**. `schemaMigrationState = UNCHANGED`, `migrationFilesChanged = 0`.

## 8. Manifest + SoT mutation

- `docs/architecture/tmar-module-structure-manifests.json`: the `Pricing` entry **moved** from
  `preCertModules` into the certified `modules` array with `structureCertified: true`,
  `lockVersion: ARCH-COMPLETE-002` and the W3 `certificationNote`; the disk-accurate per-project
  `rootAllowlist` / `forbiddenRootFiles` / `forbiddenTopLevelFolders` are unchanged. Certified module
  count **24 → 25**; `preCertModules` now holds only `ProductWorkspace`. Pricing is correctly absent from
  `uncertifiedHttpOwningModules`.
- `docs/architecture/tmar-current-state.json`: `"Pricing"` appended to `structureLock.certifiedModules`
  (25 members) and a new `pricingAmsc001W3` block records the certification.
- `TmarCompleteReferenceStructureGateTests` certified-module lists extended with `Pricing` in both the
  manifest and `structureLock` assertions.

## 9. Durable guards

New: `src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W3CertGuardTests.cs` (**9 tests**)

- `Pricing_is_amsc001_certified_in_manifest_and_sot`
- `Amsc_lineage_pins_analyze_migrate_and_structure_shas`
- `Structure_gate_fields_are_ready_for_certify_on_disk`
- `Path_namespace_is_exact_for_every_production_file`
- `Canonical_seams_are_present_and_single_owned`
- `Composed_catalog_has_no_duplicate_pricing_descriptor`
- `Bilingual_resources_resolve_every_pricing_owned_code_through_the_composed_localizer`
- `Boundaries_stay_contracts_only_and_persistence_is_module_owned`
- `Host_final_closure_is_preserved_for_pricing`

Repointed (not weakened): `PricingModuleAmsc001W2StructureGuardTests.Root_allowlists_and_forbidden_lists_match_the_manifest`
now reads the promoted `modules` entry and asserts `Pricing` is absent from `preCertModules`, keeping
every structural allowlist assertion intact.

## 10. Focused validation

| Validation | Result |
| --- | --- |
| `Tooba.Host.Tests` build | succeeded, **0 errors** |
| `Tooba.Pricing.Tests` | **14 / 14 passed** |
| `PricingModuleAmsc001W1MigrateGuardTests` + `PricingModuleAmsc001W2StructureGuardTests` + `PricingModuleAmsc001W3CertGuardTests` + `PricingArchitectureGuardTests` + `PricingErrorCatalogTests` + `ErrorCatalogUniqueCodeGuardTests` | **30 / 30 passed** |
| `TmarCompleteReferenceStructureGateTests` | 3 passed / 1 failed — the failure is the **pre-existing** repository-global `Tooba.Catalog.Contracts/Cart` path↔namespace deviation (`Expected: "Tooba.Catalog.Contracts.Cart"`, `Actual: "Tooba.Catalog.Contracts"`), explicitly out of scope for a module-local certification and unchanged by W3 |
| `Tooba.Host.Tests` full suite (current) | **2035 passed / 130 skipped / 79 failed** |
| `Tooba.Host.Tests` full suite at the `f7f6abfe` baseline (isolated `git worktree`) | **2026 passed / 130 skipped / 79 failed** |
| **New failures introduced by W3** | **ZERO** (failing-test-id set difference: identical 79 ids on both sides) |
| Guards weakened / baselines widened | **NONE** |

The `+9 passed` delta is exactly the new `PricingModuleAmsc001W3CertGuardTests` suite. Every one of the 79
remaining failures is present at the baseline and unrelated to Pricing (Catalog `Results.Json` WIP,
missing `Modules/Wishlist`, Host/Admin count drift, missing `TaxDomain.cs`, missing
`Host/Promotion/PromotionEndpoints.cs`, missing `PromotionContracts.cs`, the pre-existing
`Promotion.Infrastructure/Development/` root-allowlist gap, and the repository-global
`TmarDurableGuardTests` Host-root pins).

## 11. Residual non-blocking debt

- `AuthoredPrice.CreateCore` throws `InvalidOperationException("pricing.price.id_required")` for an empty
  `priceId` — the repo-wide defensive-invariant idiom, never client-facing and not a registered stable
  code. Left untouched to preserve byte-identical behavior.
- `TmarCompleteReferenceStructureGateTests` keeps a stale hardcoded certified-module list (repaired here
  for `Pricing`) and still fails on the unrelated `Tooba.Catalog.Contracts/Cart` deviation, which a
  module-local task must not repair. Recorded as inherited drift.
- `TmarDurableGuardTests` pins the repository-global Host root recovery checkpoint and a stale
  16-module `structureLock.certifiedModules` list; it is red for reasons unrelated to Pricing and a
  module-local wave must not displace the Host root checkpoint.

## 12. Handoff

`Verdict = COMPLETE_REFERENCE_PATTERN`; `structureCertified = true`; `microserviceExtractable = true`;
`guardsWeakened = NONE`; `automaticNextImplementationTask = NONE`;
`workflowStop = USER_REVIEW_PRICING_AMSC_001_W3`.
