# TB-TMAR-PRICING-AMSC-001-W3-R3 — boundary verification

Machine-checked by `certify-audit.cjs` → `audit-after.json` (`boundaries`, `typedFault`, `persistence`,
`hostAuthority` sections), plus the durable assertions in `PricingModuleAmsc001W3R3CertGuardTests`.

## 1. Outbound project edges (Contracts-only or own-module layering)

| Project | ProjectReferences |
| --- | --- |
| `Tooba.Pricing.Contracts` | `Tooba.BuildingBlocks`, `Tooba.Offer.Contracts` |
| `Tooba.Pricing.Domain` | `Tooba.BuildingBlocks`, `Tooba.Pricing.Contracts` |
| `Tooba.Pricing.Application` | `Tooba.Pricing.Domain`, `Tooba.Pricing.Contracts`, `Tooba.Offer.Contracts` |
| `Tooba.Pricing.Infrastructure` | `Tooba.Pricing.Application`, `Tooba.Pricing.Contracts`, `Tooba.Offer.Contracts`, `Tooba.ModuleContracts`, `Tooba.Persistence` |

`Tooba.Offer.Contracts` is the legal, narrow lookup seam (Pricing resolves offer existence/ownership
through `IOfferLookupGateway`). No Host reference. No foreign module
Application/Infrastructure/Domain/Endpoints project reference anywhere.

```text
foreignAppInfraDomainCoupling = ZERO
Cross-Module-Boundary-State = LEGAL_CONTRACTS_ONLY_BOTH_DIRECTIONS
```

## 2. Foreign source references

A regex sweep of every production `.cs` in the four production projects for
`Tooba.<foreignModule>.(Application|Infrastructure|Domain|Endpoints)` returned **zero** hits
(`foreignAppInfraDomainSources = []`).

Also asserted: no `OfferDbContext`, no `TypeForwardedTo`, no namespace alias workaround, no
`Offer.Application`/`Offer.Domain` reference.

## 3. Inbound edge (Promotion → Pricing)

| Probe | Result |
| --- | --- |
| `Promotion` `.csproj` referencing `Tooba.Pricing.Contracts` | **1** (`Tooba.Promotion.Infrastructure.csproj`) |
| `Promotion` `.csproj` referencing `Tooba.Pricing.Application` | **0** |
| `Promotion` sources referencing `Tooba.Pricing.Application` | **0** |

The illegal `Promotion.Infrastructure → Pricing.Application` edge removed by W1 stays removed. Promotion
reaches Pricing only through `Tooba.Pricing.Contracts.Ports.IPriceDirectory`.

## 4. Cross-module joins

```text
crossModuleJoinState = NONE
```

No cross-module SQL/EF join, no shared mutable aggregate, no direct table/schema reach-through. The
cross-module Offer lookup is a Contracts-port call, not persistence leakage.

## 5. Typed-fault seam (canonical API result / error mapping)

`Tooba.Pricing.Application.Composition.PricingOperation`:

- `catch (ContractOperationException ex) when (PricingErrorCodes.IsKnown(ex.Code))` × **2** overloads
  → `Result.Failure(new SemanticError(ex.Code))`;
- `catch (SemanticException ex)` × **2** overloads → `Result.Failure(ex.Error)`;
- unknown exceptions and contract faults with a non-Pricing code propagate untouched to the canonical
  global exception boundary.

```text
messageClassification = ZERO          (0 `ex.Message` occurrences in the seam)
productionExMessageOccurrences = 0     (0 across the whole production surface)
```

Zero `Results.Json` / `Results.BadRequest` / `Results.Problem`, zero local ProblemDetails builder and zero
catch-and-map in Pricing production. The seller price write returns `Result` from the Contracts port and
is mapped by the Offer-owned consumer via `api.From(result)`.

## 6. Localization / stable-code ownership

| Axis | Value |
| --- | --- |
| Stable-code home | `Tooba.Pricing.Contracts.Errors.PricingErrorCodes` (single) |
| Declared codes | **11** |
| `KnownCodes` HashSet / `public static bool IsKnown(string?)` | present / present |
| `PricingErrorCatalogContributor` classes | **1** |
| Descriptor factories in the contributor | **11** |
| `PricingErrorResourceSet` classes | **1** |
| `PricingErrors.resx` keys / `PricingErrors.fa.resx` keys | **11 / 11** (identical key sets) |
| Composed catalog `pricing.` descriptors | exactly the 11 owned codes (no foreign re-registration, no duplicate) |
| Composed bilingual resolution | every code resolves with real EN + FA text (no fallback) |

Duplicate **usage** of a code is allowed; duplicate **descriptor ownership** is not — and there is none.
No suppression mechanism (`first wins`, `DistinctBy`, catch-and-ignore) exists.

## 7. Persistence ownership

| Axis | Value |
| --- | --- |
| Schema | `pricing` (own) |
| DbContext | `PricingDbContext` (single class), `DbSet<AuthoredPrice>` |
| Schema migrator | `AddModuleSchemaMigrator("Pricing", ModuleSchemaMigrationOrder.Pricing, …)` × 1 |
| Outbox | `PricingOutboxRegistration` × 1 |
| Migrations | `20260823085546_InitialPricing.cs` + `.Designer.cs` + `PricingDbContextModelSnapshot.cs` (unchanged) |
| Application/Endpoints DbContext access | none |
| Cross-module FK | none |

`schemaMigrationState = UNCHANGED`; `migrationFilesChanged = 0`.

## 8. Host authority classification

| Host probe | Result | Classification |
| --- | --- | --- |
| `Host/Pricing` folder | absent | — |
| Host sources referencing `PricingDbContext` / `AuthoredPrice` / `IPriceDirectory` | **0** | `ILLEGAL_PERSISTENCE_AUTHORITY = 0`, `ILLEGAL_BUSINESS_AUTHORITY = 0` |
| Host `Program.cs` `MapPricingModule` / `AddPricingEndpointPresentation` / `Tooba.Pricing.Endpoints` / `"/v1/pricing"` | **0 / 0 / 0 / 0** | `ILLEGAL_ENDPOINT_OWNERSHIP = 0` |
| Host `.csproj` `Tooba.Pricing.Endpoints` / `Tooba.Pricing.Infrastructure.csproj` | **0 / 1** | `ALLOWED_COMPOSITION_ROOT` |

```text
hostAuthorityState = ZERO_BUSINESS_ZERO_HTTP
hostFinalClosure = PRESERVED
currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED
lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001
```

No `HOST_FINAL_CLOSURE_REGRESSION`: no new Host production folder, no Host-owned Pricing business logic,
policy, worker, repository, DbContext/DbSet access, composer/projection or adapter.

## 9. Closed-folder regression audit

The only new source file in this wave is the W3-R3 guard test under the Host **test** project. No
production file was added to any previously closed destination, no closed Host/module folder was
resurrected, and no retained-file set grew. `SINK_FOLDER_REGRESSION = NONE`.

## 10. Microservice extractability

Zero foreign Application/Infrastructure/Domain edge; only legal Contracts seams; own schema, own
DbContext, own migrator, own outbox, own migration, own bilingual resources, own stable codes. Pricing can
be extracted as an isolated microservice with no caller change.

```text
microserviceExtractable = true
```
