# TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001 — Offer ARCH-COMPLETE-002 bounded audit

Task: `TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001`
Parent: `TB-TMAR-CART-MULTICURRENCY-LINES-001-R1`
Channel: `tooba-main` · Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`
Verdict: **AUDIT ONLY — no production code changed.**

## Parent recovery stamp

- `lastAcceptedTask = TB-TMAR-CART-MULTICURRENCY-LINES-001-R1`
- `lastAcceptedCommit = 1aa6ab8b99a689319f25e8c31f55c332bc3f3ab4`
- Cart multi-currency = accepted with the Order single-currency fail-closed compatibility guard.
- `nextTask` advanced to `USER_REVIEW_OFFER_ARCH_COMPLETE_002_AUDIT_001`.
- Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`.

## Offer current state (verified on disk)

| Aspect | Value |
| --- | --- |
| state | `COMPLETE_REFERENCE_PATTERN` |
| httpApplicability | `HTTP_OWNING` |
| endpointOwnership | `MODULE_ENDPOINTS` |
| cqrs | `MEDIATR_12_5` |
| lastAcceptedTask | `TB-TMAR-OFFER-FINAL-REVERIFY-001` |
| lastAcceptedCommit | `813184b90906489b5654694b60afc96c4803cd3d` |
| structureCertifiedUnderArchComplete002 | **false** (absent from manifest `modules[]`, still listed in `uncertifiedHttpOwningModules`) |
| physical projects | `Domain`, `Application`, `Contracts`, `Infrastructure`, `Endpoints`, `Tests` |

Offer is the **next** module in `uncertifiedHttpOwningModules` after Order/Cart/StoreContext were certified; it is already HTTP-owning, module-owned endpoints, MediatR 12.5, Result-based.

## A. Host residue classification

| # | Host file / reference | Classification | Evidence |
| --- | --- | --- | --- |
| 1 | `src/backend/Host/Tooba.Host/Seller/HostOfferSellerAuthorizer.cs` | `KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER` | 27 LOC. Implements `Tooba.Offer.Endpoints.Seller.IOfferSellerAuthorizer` only. Body resolves `CurrentAuthenticatedSession`, `IAuthorizationGuard`, `IHostEnvironment` from `HttpContext.RequestServices` and delegates once to `SellerPanelAccess.RequireAuthorizedAsync`. No Offer business rule, no ranking/buy-box, no pricing, no inventory, no persistence, no response composition. Registered once in `Program.cs:207`. |
| 2 | `src/backend/Host/Tooba.Host/Storefront/StorefrontPrimaryOfferResolver.cs` | `REMOVE_TO_OFFER_MODULE` | Static business selection policy: `OrderByDescending(AvailableUnits > 0).ThenBy(AmountExclusiveOfTax).ThenBy(OfferId).First()` plus variant fallback rule. This is an Offer buy-box/selection decision, not Host composition. |
| 3 | `src/backend/Host/Tooba.Host/OfferGlobalUsings.cs` | `RENAME_OR_REMOVE_GLOBAL_ALIAS` | Two global aliases (`OfferStatus`, `SalesChannel` → `Tooba.Offer.Contracts.Dtos.*`). Project-wide `global using` in Host injects `Tooba.Offer.Contracts.Dtos` names into every Host file, hiding which Host files actually consume Offer contracts, and is banned by manifest rule `NO_NAMESPACE_ALIAS_WORKAROUND`. **Exact unqualified consumers: 9 files** — `AccessControl/AccessControlDevelopmentSeed.cs`, `Admin/AdminPanelComposer.cs`, `Admin/CatalogAttributeSchemaDevelopmentBootstrap.cs`, `Admin/MerchandisingCampaignAdminEndpoints.cs`, `Admin/ProductWorkspaceComposer.cs`, `Admin/ProductWorkspaceDevelopmentBootstrap.cs`, `Configuration/ToobaPlatformOptions.cs`, `Outbox/OutboxWorkerSeams.cs`, `Storefront/StorefrontDemoCatalogBootstrap.cs`. |
| 4 | `src/backend/Host/Tooba.Host/.tmp-t014-test-out/Tooba.Offer.{Application,Domain,Infrastructure}.{dll,pdb,xml}` | `REMOVE_DEAD_RESIDUE` | Stale build artifacts **inside the Host source tree** (9 files). Not source, not referenced by any csproj; a build-output residue directory under Host that also conflicts with the source-size / no-residue gates. |
| 5 | `StorefrontOfferCandidate` in `Host/Tooba.Host/Storefront/StorefrontModels.cs:6-17` | `REMOVE_TO_OFFER_MODULE` (as a consumed policy input only — see B) | Host presentation record with 9 fields; only `OfferId`, `CatalogVariantId`, `SellerPartyId`, `AmountExclusiveOfTax`, `AvailableUnits` are selection-relevant. The selection policy that consumes it must move, so those fields must become an Offer-owned selection input while `SellerDisplayName`/`SellerSku`/`Currency`/`Market`/`TaxCategoryLabel` stay Host presentation. |

**KEEP proof (#1)** — the only allowed exception: it contains **no** Offer business rule, **no** ranking/buy-box logic, **no** pricing decision, **no** inventory decision, **no** persistence and **no** response composition. It is pure platform authentication/authorization composition, and `IOfferSellerAuthorizer` is an Offer-owned port (`Tooba.Offer.Endpoints/Seller/IOfferSellerAuthorizer.cs`).

## B. `StorefrontPrimaryOfferResolver` ownership

### Exact callers (production)

| Caller | File | Use |
| --- | --- | --- |
| `StorefrontComposer.ComposeProductAsync` | `Host/Tooba.Host/Storefront/StorefrontComposer.cs:929` | `Resolve(resolvableCandidates)` → `primary` (nullable) |
| `StorefrontComposer.ComposeProductAsync` | `…/StorefrontComposer.cs:916` | `ResolveVariantId(selectedVariantId, variantIds, candidates)` |
| `StorefrontComposer.BuildVariantsAsync` | `…/StorefrontComposer.cs:1044` | `Resolve(candidates.Where(c => c.CatalogVariantId == variant.VariantId))` |

Test callers: `src/backend/Host/Tooba.Host.Tests/StorefrontCompositionTests.cs` (lines 168, 192, 195).

### `StorefrontOfferCandidate` definition

- Definition: `src/backend/Host/Tooba.Host/Storefront/StorefrontModels.cs:6`.
- Fields: `OfferId`, `CatalogVariantId`, `SellerPartyId`, (amount fields), `AvailableUnits`, `SellerDisplayName`, `ShippingLabel`.
- Classification: **Host-only presentation DTO today**, but its fields are exactly an Offer selection input: `OfferId`, `CatalogVariantId`, `SellerPartyId`, `AmountExclusiveOfTax`, `AvailableUnits`.
- Reusable entering Offer contract? Only the four selection-relevant fields are reusable; the display-only fields (`SellerDisplayName`, `ShippingLabel`) are Host presentation and must stay in Host.

### Destination

The selection policy belongs in `Tooba.Offer.Application` as an Offer-owned selection service/policy (Offer owns seller-offer identity, availability semantics and the offer identifier). The callers already have the Offer-owned inputs from `IOfferQueryGateway`, so the Host only passes data and renders the result.

### Smallest contract extraction required

1. Offer contracts: a minimal selection-candidate contract carrying `OfferId`, `CatalogVariantId`, `SellerPartyId`, `AmountExclusiveOfTax`, `AvailableUnits` (e.g. `Tooba.Offer.Contracts` DTO `OfferSelectionCandidate`).
2. Offer application: `PrimaryOfferSelectionPolicy` exposing `Resolve(IReadOnlyList<OfferSelectionCandidate>)` and `ResolveVariantId(requested, productVariantIds, candidates)` with identical ordering semantics.
3. Host keeps `StorefrontOfferCandidate` as its presentation record and either maps to the Offer contract or drops display-only fields before calling the policy.

Constraints honoured by the destination: no `Offer → Host` reference, no foreign `DbContext`, no direct Pricing/Inventory **implementation** dependency in the policy (inputs arrive pre-composed through Offer contracts).

**Not implemented in this audit.**

## C. Application / Endpoints / Infrastructure structure map

### Application (`Tooba.Offer.Application`) — root `.cs` files: **none** ✔

| Top-level folder | Contents |
| --- | --- |
| `Commands/` | `ActivateOffer`, `ArchiveOffer`, `CreateOffer`, `SetOfferInventory`, `SetOfferPrice`, `SetOrderQuantityLimits`, `SetReturnPolicy`, `SuspendOffer`, `UpdateOffer` (capability folder per use case) |
| `Queries/` | `GetOffer`, `ListSellerOffers` |
| `Mappings/` | `OfferContractMapping.cs` |
| `Ports/` | `IOfferStore.cs` |
| `ReadModels/` | `OfferReadModelComposer.cs` |
| *missing* | `Validators/` (see E) |

### Endpoints (`Tooba.Offer.Endpoints`)

Root allowlist candidate: `OfferEndpointModule.cs` (only root `.cs` file ✔).

| Folder | Contents |
| --- | --- |
| `Seller/` | `OfferSellerEndpoints.cs`, `IOfferSellerAuthorizer.cs` |
| `Errors/` | `OfferErrorCatalogContributor.cs` |
| `Resources/` | `OfferErrorResources.cs`, `OfferErrors.resx`, `OfferErrors.fa.resx` |
| *absent* | `Admin/`, `Storefront/` (currently no empty ceremonial folder on disk ✔) |

### Infrastructure (`Tooba.Offer.Infrastructure`) — root `.cs` files: **none** ✔

| Folder | Contents |
| --- | --- |
| `Persistence/` | `OfferDbContext.cs`, `Configurations/SellerOfferConfiguration.cs`, `Migrations/…` |
| `Adapters/` | `OfferStore.cs`, `OfferModuleMigration.cs`, `OfferSchemaMigrator.cs`, `OfferDevelopmentSeedGateway.cs`, `Tracing/OfferModuleCallGateways.cs`, `Tracing/OfferModuleCallTracingRegistration.cs` |
| `Events/` | `OfferEvents.cs` |
| `Outbox/` | `OfferOutboxRegistration.cs` |
| `DependencyInjection/` | `OfferModule.cs` |
| *absent* | `Repositories/` |

### Path ↔ namespace state

- Root dumps: **none**.
- Approved top-level folders only: **yes**.
- Namespace alignment: `Tooba.Offer.Contracts.Errors.OfferErrorCodes` lives physically at `Tooba.Offer.Contracts/Errors/OfferErrorCodes.cs` but declares `namespace Tooba.Offer.Contracts;` → **namespace ≠ folder**. (The current physical guard does not check `Contracts/Errors`, so this is latent.)
- Latent generalization risk: `OfferPhysicalStructureGuardTests.ExpectNs` does `StartsWith(expectedNamespace)`, so a folder-named-namespace file (e.g. `…Commands.ActivateOffer`) trivially satisfies the check and hides real path↔namespace splits. `ActivateOffer`/`ArchiveOffer`/`SuspendOffer`/`SetOrderQuantityLimits`/`SetReturnPolicy` are the affected root project files.
- 7 files have no single-line namespace (correct: two Contracts DTOs use block-scoped namespaces; five `Migrations`/`ModelSnapshot` files are EF-generated exceptions).

### Missing capability folders vs ARCH-COMPLETE-002

- `Validators/` in `Tooba.Offer.Application` — **missing** (root projected earlier but never created; correctly absent from disk so the empty-folder guard passes, yet validation coverage is still missing — see E).

## D. Endpoint-reachable MediatR request inventory

`OfferSellerEndpoints` is the only reachable surface. `Program.cs:145` registers `Tooba.Offer.Application.Commands.CreateOffer.CreateOfferCommand`'s assembly, so all Application requests below are reachable through `ISender`.

| # | Route | Request | Kind | Handler | MediatR 12.5 | Endpoint uses `ISender` | Direct service call |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `GET /v1/seller/offers` | `ListSellerOffersQuery` | Query | `ListSellerOffersHandler` | ✔ | ✔ | none |
| 2 | `POST /v1/seller/offers` | `CreateOfferCommand` | Command | `CreateOfferHandler` | ✔ | ✔ | none |
| 3 | `GET /v1/seller/offers/{offerId}` | `GetOfferQuery` | Query | `GetOfferHandler` | ✔ | ✔ | none |
| 4 | `PATCH /v1/seller/offers/{offerId}` | `UpdateOfferCommand` | Command | `UpdateOfferHandler` | ✔ | ✔ | none |
| 5 | `POST/PUT /v1/seller/offers/{offerId}/price` | `SetOfferPriceCommand` | Command | `SetOfferPriceCommandHandler` | ✔ | ✔ | none |
| 6 | `POST/PUT /v1/seller/offers/{offerId}/inventory` | `SetOfferInventoryCommand` | Command | `SetOfferInventoryCommandHandler` | ✔ | ✔ | none |

**Endpoint-reachable requests: 6.** All have handlers; zero endpoints invoke an Application/Directory service directly.

Unreachable (defined but not reachable from any endpoint): `ActivateOfferCommand`, `ArchiveOfferCommand`, `SuspendOfferCommand`, `SetOrderQuantityLimitsCommand`, `SetReturnPolicyCommand`. `UpdateOfferHandler` internally applies the same transitions (`Activate`/`Suspend`/`Archive`/`SetReturnPolicy`/`SetOrderQuantityLimits`), so these five requests are **not** dead capability gaps.

## E. FluentValidation coverage matrix

`TmarFoundation.AddTmarFoundation(...)` registers `AddValidatorsFromAssembly(typeof(FoundationPingCommand).Assembly)` plus each `additionalHandlerAssemblies` entry and adds the `ValidationBehavior<,>` pipeline (`TmarFoundation.cs:226-240`). `Program.cs:145` passes the Offer Application assembly.

| Request | Classification | Validator path |
| --- | --- | --- |
| `ListSellerOffersQuery` | `NO_VALIDATOR_REQUIRED` | zero-input query (only a non-empty `SellerPartyId`, owned by the authorizer) |
| `GetOfferQuery` | `VALIDATOR_REQUIRED` | **MISSING** — must reject `OfferId == Guid.Empty` and `SellerPartyId == Guid.Empty` |
| `CreateOfferCommand` | `VALIDATOR_REQUIRED` | **MISSING** — `CatalogVariantId`/`SellerPartyId` non-empty, `Channel` defined, `SellerSku` shape/length, `Status` known value, return-policy choice shape, custom window range |
| `UpdateOfferCommand` | `VALIDATOR_REQUIRED` | **MISSING** — `OfferId`/`SellerPartyId` non-empty, `Status` in known set, optional numeric bounds non-negative, custom window range |
| `SetOfferPriceCommand` | `VALIDATOR_REQUIRED` | **MISSING** — `OfferId`/`SellerPartyId` non-empty, `Amount >= 0`, `Currency` 3-letter shape, optional `Market` shape |
| `SetOfferInventoryCommand` | `VALIDATOR_REQUIRED` | **MISSING** — `OfferId`/`SellerPartyId` non-empty, `OnHand >= 0`, `Reason` length shape |

`Tooba.Offer.Application/Validators/` does not exist; **zero** Offer validators exist anywhere in the module. Offer therefore relies entirely on in-handler checks, which is transport-shape work that belongs in FluentValidation.

## F. Contracts / foreign boundaries

Verified **clean** on disk:

| Check | Result |
| --- | --- |
| `Application` → foreign `*.Application` / `*.Infrastructure` refs | none (only `*.Contracts` + `BuildingBlocks` + own Domain/Contracts) |
| `Infrastructure` → foreign non-contract refs | none (Catalog/Party/Pricing/Inventory `*.Contracts` only) |
| `Domain` → Application/Infrastructure/Endpoints | none (`BuildingBlocks` only) |
| `Endpoints` → Infrastructure | none (`Application` + `Contracts` + `BuildingBlocks`) |
| foreign `DbContext` in Application/Endpoints | none |
| Offer → Host reference | none |
| direct cross-module persistence | none (`IOfferStore` → `OfferDbContext` only) |

Single flagged item (not a foreign-module violation, but a path↔namespace defect): `Tooba.Offer.Contracts/Errors/OfferErrorCodes.cs` declares `namespace Tooba.Offer.Contracts;`. It duplicates the type name of `Tooba.Offer.Domain/Errors/OfferErrorCodes.cs` (`Tooba.Offer.Domain.Errors`), so it is a duplicate-name/placement defect as well as a namespace mismatch.

Host business logic compensating for a missing Offer contract: **yes — item 2 in section A**, the Host buy-box resolver, which is exactly the missing Offer selection policy.

## G. Error / locale / clock / id / Result findings

| Check | Result |
| --- | --- |
| `Result`/`SemanticError` for expected outcomes | ✔ Domain `Activate/SetOrderQuantityLimits/...` return `Result`; handlers return `Result<T>`; no expected-failure `SemanticException` throws |
| message parsing | ✔ none — endpoints use `ApiResponseFactory api` + `api.From(result)` / `api.Created(...)`; no `catch (`, no `ex.Message`, no exception string matching |
| localized text at presentation boundary only | ✔ error/resource files live in `Endpoints/Errors` + `Endpoints/Resources` (`OfferErrors.resx`, `OfferErrors.fa.resx`) |
| no Persian prose in Domain/Application/Infrastructure | ⚠ **Host-side only** — Offer module production projects clean; the Host residue files carry Persian XML docs (`HostOfferSellerAuthorizer.cs`, `StorefrontPrimaryOfferResolver.cs`), which is Host's existing convention and out of this module's lock scope, but the resolver move should not carry them into Offer |
| `IClock` | ✔ handlers/Domain use injected `IClock.UtcNow` |
| `IIdGenerator` | ✔ no direct id minting in Offer production paths |
| no `DateTime.UtcNow` / `DateTimeOffset.UtcNow` / `Guid.NewGuid()` | ✔ Offer production sources clean |
| error catalog contribution | ✔ `OfferErrorCatalogContributor` registered by `AddOfferEndpointPresentation()` |

## H. Proposed manifest entry (not applied)

```json
{
  "module": "Offer",
  "structureCertified": true,
  "lockVersion": "ARCH-COMPLETE-002",
  "projects": [
    {
      "projectName": "Tooba.Offer.Application",
      "rootAllowlist": [],
      "forbiddenRootFiles": ["OfferContractMapping.cs", "IOfferStore.cs", "OfferReadModelComposer.cs", "OfferRequests.cs", "OfferHandlers.cs", "OfferQueries.cs", "OfferQueryHandlers.cs"],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.Offer.Endpoints",
      "rootAllowlist": ["OfferEndpointModule.cs"],
      "forbiddenRootFiles": ["OfferSellerEndpoints.cs", "IOfferSellerAuthorizer.cs", "OfferErrorCatalogContributor.cs", "OfferErrorResources.cs", "OfferEndpointLocalizer.cs"],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.Offer.Infrastructure",
      "rootAllowlist": [],
      "forbiddenRootFiles": ["OfferModule.cs", "OfferDbContext.cs", "OfferStore.cs", "OfferEvents.cs", "OfferModuleMigration.cs", "OfferSchemaMigrator.cs", "OfferDevelopmentSeedGateway.cs", "OfferOutboxRegistration.cs"],
      "forbiddenTopLevelFolders": []
    }
  ]
}
```

Also required for certification, and **not** part of this audit:

- remove `Offer` from `uncertifiedHttpOwningModules` and add it to `structureLock.certifiedModules`;
- extend `OfferPhysicalStructureGuardTests.ExpectNs` from `StartsWith` to exact equality so the folder-named-namespace files cannot mask a split;
- fix `Tooba.Offer.Contracts/Errors/OfferErrorCodes.cs` namespace/path (and remove the Domain/Contracts duplicate-name collision);
- add the `Validators/` capability folder with the six validators from section E.

## I. Next implementation task decision

**Split is necessary — two tasks.** One combined task would mix a Host business-policy extraction (touching `StorefrontComposer`, `StorefrontModels`, Host tests, Offer Contracts/Application) with structure certification + validator work + manifest/SoT updates. That is one large unrelated-concern batch and would create the exact discovery loop the Architect forbids.

1. **`TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001`** — Host residue ownership repair.
   - move the buy-box selection policy into `Tooba.Offer.Application` behind a minimal Offer contract;
   - delete `StorefrontPrimaryOfferResolver.cs` from Host and re-point `StorefrontComposer` (3 call sites);
   - remove `OfferGlobalUsings.cs` aliases and qualify usages;
   - delete the `Host/Tooba.Host/.tmp-t014-test-out/` build residue;
   - keep `HostOfferSellerAuthorizer` as the explicit thin Host security adapter;
   - update `Host/Tooba.Host.Tests/StorefrontCompositionTests.cs` to target the Offer-owned policy.
2. **`TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001`** — structure certification.
   - exact namespace/path repair (including `Contracts/Errors`);
   - add `Application/Validators/` with the six validators;
   - tighten the physical guard from `StartsWith` to exact path↔namespace alignment;
   - apply the manifest entry, remove Offer from `uncertifiedHttpOwningModules`, add Offer to `structureLock.certifiedModules`;
   - update `tmar-current-state.json`, `TOOBA-TMAR-MASTER-RECOVERY.md`, `TOOBA-ARCHITECT-BOOTSTRAP.md` and the durable guard.

### Exact next implementation file list (evidence-backed, no discovery left)

Task 1:

- `src/backend/Modules/Offer/Tooba.Offer.Contracts/Dtos/OfferSelectionCandidate.cs` (new)
- `src/backend/Modules/Offer/Tooba.Offer.Application/Policies/PrimaryOfferSelectionPolicy.cs` (new; folder `Policies` added to the Application approve list)
- `src/backend/Host/Tooba.Host/Storefront/StorefrontPrimaryOfferResolver.cs` (delete)
- `src/backend/Host/Tooba.Host/OfferGlobalUsings.cs` (delete)
- `src/backend/Host/Tooba.Host/Storefront/StorefrontModels.cs` (candidate record + alias removal fallout)
- `src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs` (3 call sites)
- `src/backend/Host/Tooba.Host/.tmp-t014-test-out/` (delete tree)
- `src/backend/Host/Tooba.Host.Tests/StorefrontCompositionTests.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferArchitectureGuardTests.cs` (residue guard)

Task 2:

- `src/backend/Modules/Offer/Tooba.Offer.Application/Validators/ListSellerOffersQueryValidator.cs` (only if the Architect requires a shape check; otherwise excluded)
- `src/backend/Modules/Offer/Tooba.Offer.Application/Validators/GetOfferQueryValidator.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Validators/CreateOfferCommandValidator.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Validators/UpdateOfferCommandValidator.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Validators/SetOfferPriceCommandValidator.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Validators/SetOfferInventoryCommandValidator.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Contracts/Errors/OfferErrorCodes.cs` (namespace/path repair; resolve Domain duplicate name)
- `src/backend/Modules/Offer/Tooba.Offer.Application/Mappings/OfferContractMapping.cs` (namespace alignment if the Architect adopts folder-exact namespaces)
- `src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferPhysicalStructureGuardTests.cs` (exact alignment)
- `docs/architecture/tmar-module-structure-manifests.json`
- `docs/architecture/tmar-current-state.json`
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`
- `docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md`
- `src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs`

## Validation performed

- `Tooba.Offer.Tests` architecture selection (`OfferArchitectureGuardTests` + `OfferPhysicalStructureGuardTests`) — **34 passed, 0 failed**.
- `TmarDurableGuardTests` + `TmarCompleteReferenceStructureGateTests` — **8 passed, 0 failed**.
- `dotnet build src/backend/Tooba.slnx` — Build succeeded, 0 errors (required because the recovery guard assertions changed).
- No Offer production compile change was made; no broad suite was run.

## Recovery acceptance stamp evidence

Recorded in `docs/architecture/tmar-current-state.json`, `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`, `docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md` and `TmarDurableGuardTests`: `lastAcceptedTask = TB-TMAR-CART-MULTICURRENCY-LINES-001-R1`, `lastAcceptedCommit = 1aa6ab8b99a689319f25e8c31f55c332bc3f3ab4`, `nextTask = USER_REVIEW_OFFER_ARCH_COMPLETE_002_AUDIT_001`.

## Non-goals honoured

No Offer production edit, no Host production edit, no manifest entry added, no certification added, no Offer Archive/Order/Payment scope, Checkout still paused at W5, frontend frozen.
