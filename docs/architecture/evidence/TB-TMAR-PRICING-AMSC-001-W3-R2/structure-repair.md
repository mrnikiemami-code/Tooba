# TB-TMAR-PRICING-AMSC-001-W3-R2 — structure repair (tooba-architecture-structure)

- Module: `Pricing`
- Mode: `STRUCTURE_REPAIR_INTERNAL_ONLY_APPLICABILITY`
- Skill: `tooba-architecture-structure`
- Starting HEAD: `2e664bb336f45b8304818b7e5754f9b0fc364f20` (`HEAD == origin/main`, branch `main`)
- Structure-State: `READY_FOR_CERTIFY`
- Prior certification: `SUPERSEDED_PENDING_FRESH_CERTIFY`
- Superseded task/commit: `TB-TMAR-PRICING-AMSC-001-W3` / `3c2cc61e7c61813ac72773ccdb8bb16317cafe70`
- HTTP applicability: `NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER` (unchanged, now structurally honest)
- Behavior: `PRESERVED` (zero business/domain change; only composition-site + ceremony removal)
- Host final closure: preserved (`HOST_ROOT_FINAL_CERTIFIED` untouched; zero Host route/composition change beyond the removal)

## 1. Why this repair exists

The prior W3 certification classified Pricing as `NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER`
(owns zero HTTP routes and zero endpoint-reachable requests) while the repository simultaneously kept:

- a `Tooba.Pricing.Endpoints` project,
- `PricingEndpointModule` with `MapPricingModule()`,
- a deliberately **empty** `/v1/pricing` route group,
- Host `using Tooba.Pricing.Endpoints;`, `AddPricingEndpointPresentation()`, `MapPricingModule()`,
- Host `ProjectReference` to `Tooba.Pricing.Endpoints.csproj`,
- `/Modules/Pricing/` solution grouping with **6** projects.

That is a direct contradiction of the current migrate/structure skill rule *“internal-only modules must
not create or retain Endpoints/CQRS ceremony where no HTTP/application surface exists”* and of the
certified `Inventory` `INTERNAL_ONLY` precedent (Inventory has no Endpoints project and registers
`IErrorCatalogContributor` + `IErrorResourceSet` in `Infrastructure/DependencyInjection/InventoryModule.cs`).

W3-R2 is a **structure repair only**. It does not re-certify.

## 2. Classification states

| Axis | Before (W3 / W3-R1) | After (W3-R2) |
| --- | --- | --- |
| Structure-State | `CERTIFIED` (stale claim) | `READY_FOR_CERTIFY` |
| Http-Applicability-State | `NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER` | `NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER` (unchanged) |
| Internal-Only-Applicability-State | `CEREMONIAL_ENDPOINTS_RETAINED` | `CANONICAL_NO_ENDPOINTS_PROJECT` |
| Pricing-Endpoints-Project-State | `PRESENT` | `ABSENT` |
| Pricing-Http-Route-State | `EMPTY_GROUP_ONLY` | `ZERO` |
| Presentation-Registration-State | `ENDPOINTS_EXTENSION` | `INFRASTRUCTURE_MODULE_COMPOSITION` |
| Pricing-Project-Count-State | 6 (5 production + Tests) | 5 (4 production + Tests) |
| Solution-Grouping-State | 6 projects | `CANONICAL_5` |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` | `PROFESSIONAL_SHALLOW` (unchanged) |
| Path-Namespace-State | `EXACT` | `EXACT` (unchanged) |
| Physical-Copy-State | `CLEAN` | `CLEAN` (unchanged) |
| Root-Allowlist-State | `ENFORCED` | `ENFORCED` (unchanged, Endpoints entry removed) |
| Foreign-App-Infra-Domain-Coupling-State | `ZERO` | `ZERO` (unchanged) |
| Schema-Migration-State | `UNCHANGED` | `UNCHANGED` |
| Behavior-Preservation-State | `PRESERVED` | `PRESERVED` |
| Manifest-Certification-State | `CERTIFIED` (26 in `modules[]`) | `PRECERT_READY_FOR_CERTIFY` (25 in `modules[]`, Pricing in `preCertModules`) |
| StructureLock-Pricing-State | present (25 members) | `TEMPORARILY_REMOVED_PENDING_CERTIFY` (24 members) |

## 3. Changes made

### 3.1 Presentation registration moved into Infrastructure composition (Inventory precedent)

`src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs`

```csharp
// Pricing is INTERNAL_ONLY (zero HTTP routes, no Endpoints project): the module-owned error
// catalog contributor and resource set are registered by the Infrastructure composition root,
// exactly as the certified Inventory precedent. Both concrete types stay Contracts-owned.
services.AddSingleton<IErrorCatalogContributor, PricingErrorCatalogContributor>();
services.AddSingleton<IErrorResourceSet, PricingErrorResourceSet>();
```

The concrete types stay in `Tooba.Pricing.Contracts.Errors` (single self-contained code + text
boundary). `using Tooba.Pricing.Contracts.Errors;` was already present; no duplicate registration was
introduced; no error semantics changed.

### 3.2 Ceremonial Endpoints project removed

Deleted in full:

```text
src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/PricingEndpointModule.cs
src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/Tooba.Pricing.Endpoints.csproj
src/backend/Modules/Pricing/Tooba.Pricing.Tests/Endpoints/PricingEndpointModuleTests.cs
```

`MapPricingModule()` (empty `/v1/pricing` group) and `AddPricingEndpointPresentation()` no longer exist
anywhere. No replacement project, alias or type-forward was introduced.

### 3.3 Host composition cleaned (removal only)

`src/backend/Host/Tooba.Host/Program.cs`

```diff
-using Tooba.Pricing.Endpoints;
...
-builder.Services.AddPricingEndpointPresentation();
...
-app.MapPricingModule();
```

`src/backend/Host/Tooba.Host/Tooba.Host.csproj`

```diff
-<ProjectReference Include="..\..\Modules\Pricing\Tooba.Pricing.Endpoints\Tooba.Pricing.Endpoints.csproj" />
```

No other Host route, registration or reference was touched. The existing Pricing
`Application`/`Infrastructure` references are preserved (Host still constructs `new PricingModule()`
through module composition).

### 3.4 Solution Explorer

`src/backend/Tooba.slnx` — the single `/Modules/Pricing/` `Tooba.Pricing.Endpoints` entry was removed:

```diff
-<Project Path="Modules/Pricing/Tooba.Pricing.Endpoints/Tooba.Pricing.Endpoints.csproj" />
```

`/Modules/Pricing/` now groups exactly 5 projects: `Application`, `Contracts`, `Domain`,
`Infrastructure`, `Tests`.

### 3.5 Tests / guards repointed (never weakened)

| File | Change |
| --- | --- |
| `Tooba.Pricing.Tests/Tooba.Pricing.Tests.csproj` | removed the `Tooba.Pricing.Endpoints` `ProjectReference` |
| `Tooba.Pricing.Tests/Endpoints/PricingEndpointModuleTests.cs` | deleted with the project it tested |
| `Tooba.Pricing.Tests/Architecture/PricingArchitectureGuardTests.cs` | replaced the Endpoints-project test with `Ceremonial_endpoints_project_stays_retired_internal_only`; `Program_no_longer_maps_a_pricing_module_route_group` now asserts the group is gone |
| `Host.Tests/Architecture/PricingModuleAmsc001W1MigrateGuardTests.cs` | removed `Tooba.Pricing.Endpoints` from the pinned production project set |
| `Host.Tests/Architecture/PricingModuleAmsc001W2StructureGuardTests.cs` | retired the Endpoints-root test; added `Ceremonial_endpoints_project_is_retired`; allowlist/manifest assertions now read the `preCertModules` entry and expect Pricing absent from `modules[]`; solution grouping pinned to exactly 5 |
| `Host.Tests/Architecture/PricingModuleAmsc001W3CertGuardTests.cs` → `PricingModuleAmsc001W3R2RepairGuardTests.cs` | renamed and rewritten as the repair lock (superseded W3, absence of Endpoints, Infrastructure registration, 4 production projects, pre-cert manifest/SoT, 24-member `certifiedModules`) |
| `Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs` | removed `"Pricing"` from the expected certified-module lists (manifest **25**, `structureLock` **24**) |
| `Host.Tests/HostModuleEndpointOwnershipTests.cs` | `Program_maps_offer_and_tax_modules` now asserts `MapPricingModule()` is **absent** |

Every unrelated assertion is intact; no guard was weakened and no baseline widened. The W1 semantics
(single canonical code home, no re-inlined literals, no message-text classification, Contracts-only
boundaries) and the W3 semantics (typed-fault seam, one catalog contributor + one resource set,
composed-catalog uniqueness, bilingual composed resolution, own schema/migration) are re-asserted by
`PricingModuleAmsc001W3R2RepairGuardTests`.

The two "ceremony is absent" guards scan the module's **production** `.cs` surface (all Pricing `.cs`
outside the `Tooba.Pricing.Tests` project and outside `bin`/`obj`). The Test project is excluded because
the durable guards necessarily name the retired identifiers (`Tooba.Pricing.Endpoints`,
`MapPricingModule`, `AddPricingEndpointPresentation`, `PricingEndpointModule`, `"/v1/pricing"`) as
negative assertions; including it would make the guard self-contradicting. This narrows the scan to the
surface the claim is actually about and does not relax any assertion.

### 3.6 Manifest honesty

`docs/architecture/tmar-module-structure-manifests.json`

- `Pricing` removed from `modules[]` (certified count **26 → 25**).
- `Pricing` present in `preCertModules` with `structureCertified: false`,
  `lockVersion: ARCH-COMPLETE-002`, `structureState: "READY_FOR_CERTIFY"`,
  `structureRepairTask: "TB-TMAR-PRICING-AMSC-001-W3-R2"` and an explicit
  `certificationNote` recording the supersession.
- The per-project inventory is now exactly the 5 INTERNAL_ONLY projects; the
  `Tooba.Pricing.Endpoints` project entry is gone and `Tooba.Pricing.Tests` carries
  `forbiddenTopLevelFolders: ["Endpoints"]` so the ceremony cannot resurrect.
- `uncertifiedHttpOwningModules` unchanged; Pricing is correctly absent from it (it owns no HTTP routes).
- No other module entry was altered.

### 3.7 SoT honesty

`docs/architecture/tmar-current-state.json`

- `structureLock.certifiedModules`: `"Pricing"` removed (**25 → 24** members).
- Historical `pricingAmsc001W0/W1/W2/W3/W3R1` blocks preserved verbatim as history.
- New additive `pricingAmsc001W3R2` block records the repair: `state =
  PRICING_AMSC_001_STRUCTURE_REPAIRED_READY_FOR_CERTIFY`, `verdict = READY_FOR_CERTIFY`,
  `priorCertificationState = SUPERSEDED_PENDING_FRESH_CERTIFY`, `endpointProjectState = ABSENT`,
  `endpointRouteCount = 0`, `endpointReachableRequests = 0`,
  `presentationRegistrationState = INFRASTRUCTURE_MODULE_COMPOSITION`, `solutionProjectCount = 5`,
  `schemaMigrationState = UNCHANGED`, `productionBehaviorState = PRESERVED`,
  `guardsWeakened = NONE`, `baselinesWidened = NONE`, `automaticNextImplementationTask = NONE`,
  `workflowStop = USER_REVIEW_PRICING_AMSC_001_W3_R2`. No self-referential R2 commit placeholder.
- Global Host checkpoint fields preserved exactly
  (`lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`, `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`).

### 3.8 Master Recovery

`docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` — appended a module-local Pricing W3-R2
structure-repair checkpoint stating the supersession, the 5-project INTERNAL_ONLY shape, the moved
registration, the temporary `certifiedModules` removal, unchanged schema/migrations, the preserved Host
root checkpoint and `automatic next = NONE`. Historical W3 text is not rewritten as if it had been
correct.

## 4. Deliberately preserved (not touched)

- 11 stable codes (`PricingErrorCodes`, `KnownCodes` + `IsKnown(string?)`), 11 descriptors,
  11 EN + 11 FA resource keys/text, byte-identical.
- `PricingOperation` typed-fault seam (`ContractOperationException` when `IsKnown` → `SemanticError`,
  `SemanticException` → `ex.Error`; never message-text classification).
- `pricing` schema, `PricingDbContext`, `PricingDbContextFactory`, the single migration
  `20260823085546_InitialPricing` (+ designer + snapshot), `PricingOutboxRegistration`,
  `IPricingSchemaMigrator`/`PricingModuleMigration`.
- Contracts-only foreign boundaries (outbound `Tooba.Offer.Contracts`; inbound
  `Tooba.Promotion.Infrastructure → Tooba.Pricing.Contracts.Ports`).
- The Offer-owned seller price route (`POST|PUT /v1/seller/offers/{offerId}/price`) and Pricing's own
  internal boundary validation via `MarketCode.TryParse` / `CurrencyCode.TryParse` → `Result.Failure`.
- No CQRS request/handler/validator ceremony was invented, and none was added by this wave.

## 5. Handoff

`Structure-State = READY_FOR_CERTIFY`; `Pricing-Endpoints-Project-State = ABSENT`;
`Pricing-Http-Route-State = ZERO`; `Presentation-Registration-State =
INFRASTRUCTURE_MODULE_EXACTLY_ONCE`; `Pricing-Project-Count-State = EXACT_5`;
`Solution-Grouping-State = CANONICAL_5`; `Guards-Weakened-State = NONE`;
`Baselines-Widened-State = NONE`; `Automatic-Next-Implementation-Task-State = NONE`;
`Workflow-Stop-State = USER_REVIEW_PRICING_AMSC_001_W3_R2`.

A fresh Certify wave is required before Pricing may return to `modules[]` /
`structureLock.certifiedModules`. This task does not self-certify and does not create R3.
