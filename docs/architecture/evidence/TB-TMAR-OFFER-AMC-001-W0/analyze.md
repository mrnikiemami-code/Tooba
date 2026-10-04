# TB-TMAR-OFFER-AMC-001 — W0 ANALYZE

- Task: `TB-TMAR-OFFER-AMC-001` (ARCHITECT_DIRECT_AMSC)
- Module: `Modules/Offer`
- Skills: `Analyze → Migrate → Structure → Certify`
- Branch: `main`
- Base commit: `922bffa1` (`HEAD == origin/main`)
- Mode: analysis-only (no production code changed in this wave)

## 1. Target analyzed

`src/backend/Modules/Offer` — six on-disk projects:

| Project | Files (prod) | Layer |
| --- | --- | --- |
| `Tooba.Offer.Domain` | 5 | Aggregates / ValueObjects / Events / Errors |
| `Tooba.Offer.Contracts` | 14 | Dtos / Ports / Errors |
| `Tooba.Offer.Application` | 24 | CQRS + Policies + Ports + ReadModels + Validators |
| `Tooba.Offer.Infrastructure` | 17 | Persistence / Adapters / Outbox / Events / DI |
| `Tooba.Offer.Endpoints` | 5 | Seller routes + Errors + Resources |
| `Tooba.Offer.Tests` | 12 | Domain/Application/Contracts/Infrastructure/Endpoints/Architecture/Observability |

HTTP applicability: `HTTP_OWNING`. Endpoint ownership: `MODULE_ENDPOINTS` (`OfferEndpointModule.cs` at Endpoints root only).

## 2. Structured state fields

| Field | Value |
| --- | --- |
| Foundation-State | `FOUNDATION_READY` |
| Ownership-State | `correct` (no foreign-owned responsibility inside Offer) |
| File-Cohesion-State | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (narrow: 2 files) |
| Oversized/God-File-State | none > 800 LOC (max production 303 LOC) |
| Localization-State | `CANONICAL` (persian files = repo-wide pattern; see §13) |
| API-Result-Pattern-State | `CANONICAL` |
| Stable-Error-Code-State | `CATALOGUED` |
| Logging-State | `CANONICAL` |
| Sensitive-Logging-State | `NONE` |
| OpenTelemetry-State | `CANONICAL` |
| Correlation-Trace-State | `CANONICAL` |
| CQRS-State | `COMPLIANT` |
| Validator-Coverage-State | `EXHAUSTIVE` (5 required + 1 NO_VALIDATOR_REQUIRED) |
| Contracts-Boundary-State | `CLEAN` |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` (single `offer` schema) |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Host-Residue-State | `THIN_SECURITY_ADAPTER_ONLY` (legitimate) |
| Schema-Migration-State | `UNCHANGED` |
| Behavior-Preservation-Risk | `LOW` |
| Canonical-Reference-Used | BuildingBlocks (`ApiResponseFactory`, `IErrorCatalogContributor`, `IModuleCallTracer`, `Result`); Content module (capability-first Application layout) |
| Folder-Granularity-State | `TECHNICAL_AXIS_FIRST` + 11 single-file request leaves |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| **Final-Disposition** | `READY_TO_MIGRATE` |
| **Structure-Handoff-State** | `REQUIRED` |

## 3. Responsibility map

All production responsibilities are already Offer-owned. No `MUST_SPLIT` ownership decision is required.
Cohesion splits required (see §12):

1. `Contracts/Ports/ReturnPolicyContracts.cs` — 4 responsibilities in one file.
2. `Application/Validators/` — 6 validators + rules + codes co-located away from their request types.

## 4. Ownership map

| Responsibility | Owner | Location |
| --- | --- | --- |
| `SellerOffer` aggregate + invariants | Offer.Domain | `Aggregates/SellerOffer.cs` |
| Offer lifecycle / return policy / quantity invariants | Offer.Domain | `Aggregates/SellerOffer.cs` |
| Stable Offer error codes (domain) | Offer.Domain | `Errors/OfferErrorCodes.cs` |
| Stable Offer error codes (boundary) | Offer.Contracts | `Errors/OfferErrorCodes.cs` |
| Cross-module ports | Offer.Contracts | `Ports/*` |
| CQRS commands/queries | Offer.Application | `Commands/*`, `Queries/*` |
| Primary offer selection policy | Offer.Application | `Policies/PrimaryOfferSelectionPolicy.cs` |
| Persistence (single `offer` schema) | Offer.Infrastructure | `Persistence/*` |
| Cross-module traced adapters | Offer.Infrastructure | `Adapters/Tracing/*` |
| HTTP routes | Offer.Endpoints | `Seller/OfferSellerEndpoints.cs` |
| Error catalog + localization | Offer.Endpoints | `Errors/*`, `Resources/*` |
| Seller authorization adapter | Host (thin security only) | `Host/Security/Seller/HostOfferSellerAuthorizer.cs` |

## 5. Current illegal dependencies

**NONE.** Verified:

- `Tooba.Offer.Application.csproj` references only `Tooba.BuildingBlocks` + `Catalog/Inventory/Party/Pricing`.**Contracts** + Offer.Domain + Offer.Contracts.
- `Tooba.Offer.Infrastructure.csproj` references only BuildingBlocks (`ModuleContracts`, `Persistence`) + foreign `*.Contracts`.
- `Tooba.Offer.Endpoints.csproj` references Offer.Application + Offer.Contracts + BuildingBlocks only.
- No `Tooba.Host` reference in any Offer production project.
- No `OfferDbContext` / `Tooba.Offer.Infrastructure.Persistence` usage outside `Modules/Offer`.
- No `TypeForwardedTo`, no `global using` alias workaround.

Host consumes `Tooba.Offer.Contracts.Ports.IPrimaryOfferSelectionPolicy` (Contracts-only) — legitimate.

## 6. Cross-module join inventory

**NONE.** No cross-module EF navigation, no foreign `DbSet`, no cross-schema SQL join. Cross-module reads go through Contracts ports (`IOfferQueryGateway`, `IOfferLookupGateway`, `IOfferSellerProductIdLookup`, `IOfferDevelopmentSeedGateway`, `ISellerOfferPricingGateway`, `ISellerOfferInventoryGateway`, `ICatalogVariantLookup`, `IPartyLookup`).

## 7. Contracts-only replacement map

No replacement required. Consumed-by graph (all Contracts-only):

| Consumer | Port consumed |
| --- | --- |
| Cart, Order, Catalog, Party, Pricing, Inventory, Promotion, ProductWorkspace, Reviews, Host | `IOfferLookupGateway` / `IOfferQueryGateway` / `IOfferSellerProductIdLookup` / `IOfferDevelopmentSeedGateway` / `IPrimaryOfferSelectionPolicy` |
| Offer.Application → Pricing / Inventory | `ISellerOfferPricingGateway`, `ISellerOfferInventoryGateway` |
| Offer.Application → Catalog / Party | `ICatalogVariantLookup` / `ICatalogOfferReadGateway`, `IPartyLookup` |

## 8. CQRS / MediatR gaps

**NONE.** All 9 commands + 2 queries are real `IRequest<T>` with real `IRequestHandler<,>`; endpoints dispatch via `ISender`; MediatR `12.5.0`; `AddToobaCqrsFoundation` registers the Offer Application assembly in `Program.cs`.

## 9. Validation classification matrix

| Request | Classification | Validator |
| --- | --- | --- |
| `CreateOfferCommand` | VALIDATOR_REQUIRED | `CreateOfferCommandValidator` |
| `UpdateOfferCommand` | VALIDATOR_REQUIRED | `UpdateOfferCommandValidator` |
| `SetOfferPriceCommand` | VALIDATOR_REQUIRED | `SetOfferPriceCommandValidator` |
| `SetOfferInventoryCommand` | VALIDATOR_REQUIRED | `SetOfferInventoryCommandValidator` |
| `GetOfferQuery` | VALIDATOR_REQUIRED | `GetOfferQueryValidator` |
| `ListSellerOffersQuery` | NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY | — (seller id from trusted authorizer) |

Non-endpoint-reachable application commands (`ActivateOfferCommand`, `SuspendOfferCommand`, `ArchiveOfferCommand`, `SetReturnPolicyCommand`, `SetOrderQuantityLimitsCommand`) carry Guid-only request shapes; no transport validator is applicable and none is fabricated.

## 10. Localization / API result / observability findings

- **Localization:** `CANONICAL`. All 17 machine codes have exactly one `ErrorDescriptor` in `OfferErrorCatalogContributor` with `LocalizationKey = code`; `OfferErrorResourceSet` owns the `offer.*` prefix; `.resx` + `.fa.resx` exist.
- **API result mapping:** `CANONICAL`. `ApiResponseFactory` injected; `api.From(result)` / `api.Created(...)`; no raw `Results.Json`, no local `ProblemDetails` mapper, no `catch (`, no `ex.Message` classification.
- **Logging / telemetry:** `CANONICAL`. No `Console.WriteLine` / `Debug.WriteLine`; `IModuleCallTracer` used for all cross-module calls; no `StartActivity` / `traceparent` / `AsyncLocal` in Application/Endpoints.
- **Sensitive logging:** `NONE`.
- **Time/Id:** all `IClock` / `IIdGenerator`; no `DateTime.UtcNow` / `Guid.NewGuid()` bypass.

## 11. Baseline verification (pre-change, must not regress)

```
dotnet build Tooba.Offer.Tests.csproj      -> Build succeeded, 0 warnings, 0 errors
dotnet test  Tooba.Offer.Tests.csproj      -> Failed: 2, Passed: 77, Total: 79
```

Pre-existing RED (certification drift on the Offer surface, disclosed honestly):

1. `OfferPhysicalStructureGuardTests.ARCH_MODULE_PHYSICAL_001_offer_routes_absent_from_host_seller_endpoints`
   — line 193: `Assert.DoesNotContain("MapGet(\"/offers\"", text)` fails. The guard's intent is *"Offer routes must be owned by the module, not by Host"*; the module's own `OfferSellerEndpoints.cs` legitimately contains the route. The assertion contradicts its own documented intent.
2. `OfferArchitectureGuardTests.Host_owns_no_offer_selection_policy_or_hidden_offer_alias`
   — line 393: `Sequence contains no matching element`. The guard requires `Host/Tooba.Host/Storefront/StorefrontComposer.cs`; that path no longer exists (Storefront composition moved to `Modules/Catalog/Tooba.Catalog.Infrastructure/Storefront/StorefrontComposer.cs`, which the same guard file already validates at line 566).

Both failures are **stale guard paths/intent**, not product defects. They are in scope for this AMSC run (Architecture-guard correctness is part of `COMPLETE_REFERENCE_PATTERN`).

## 12. File cohesion / splitting plan

### 12.1 `Contracts/Ports/ReturnPolicyContracts.cs` (182 LOC) — split by responsibility

| New file | Content | Namespace |
| --- | --- | --- |
| `Contracts/ReturnPolicy/ReturnPolicyOptions.cs` | `ReturnPolicyOptions` | `Tooba.Offer.Contracts.ReturnPolicy` |
| `Contracts/ReturnPolicy/OfferReturnPolicyChoices.cs` | `OfferReturnPolicyChoices` | `Tooba.Offer.Contracts.ReturnPolicy` |
| `Contracts/ReturnPolicy/ResolvedReturnPolicy.cs` | `ResolvedReturnPolicy` | `Tooba.Offer.Contracts.ReturnPolicy` |
| `Contracts/ReturnPolicy/IReturnPolicyResolver.cs` | `IReturnPolicyResolver` (port only) | `Tooba.Offer.Contracts.ReturnPolicy` |
| `Application/ReturnPolicy/ReturnPolicyResolver.cs` | `ReturnPolicyResolver` (implementation) | `Tooba.Offer.Application.ReturnPolicy` |

Rationale: Contracts must carry **boundary contracts only**; a concrete governance resolver with Persian labels and `Result` decision logic is Application behavior. This also removes the last non-port implementation from `Contracts`. (Order already consumes it as an infrastructure default — contracts-only reference preserved.)

### 12.2 Validator co-location — move each validator next to its request

| From | To |
| --- | --- |
| `Application/Validators/CreateOfferCommandValidator.cs` | `Application/Offers/Commands/CreateOffer/CreateOfferCommandValidator.cs` |
| `Application/Validators/UpdateOfferCommandValidator.cs` | `Application/Offers/Commands/UpdateOffer/UpdateOfferCommandValidator.cs` |
| `Application/Validators/SetOfferPriceCommandValidator.cs` | `Application/Offers/Commands/SetOfferPrice/SetOfferPriceCommandValidator.cs` |
| `Application/Validators/SetOfferInventoryCommandValidator.cs` | `Application/Offers/Commands/SetOfferInventory/SetOfferInventoryCommandValidator.cs` |
| `Application/Validators/GetOfferQueryValidator.cs` | `Application/Offers/Queries/GetOffer/GetOfferQueryValidator.cs` |
| `Application/Validators/OfferFluentRules.cs` | `Application/Validation/OfferFluentRules.cs` (shared, cross-capability) |
| `Application/Validators/OfferValidationCodes.cs` | `Application/Validation/OfferValidationCodes.cs` (shared, cross-capability) |

Repository precedent: `Modules/Content/Tooba.Content.Application/Validators/` holds a single shared file while each capability owns its own `Validators/` folder.

### 12.3 Move table (remaining)

| From | To |
| --- | --- |
| `Application/Mappings/OfferContractMapping.cs` | `Application/Offers/Mappings/OfferContractMapping.cs` |
| `Application/Ports/IOfferStore.cs` | `Application/Offers/Ports/IOfferStore.cs` |
| `Application/Policies/PrimaryOfferSelectionPolicy.cs` | `Application/Offers/Policies/PrimaryOfferSelectionPolicy.cs` |
| `Application/ReadModels/OfferReadModelComposer.cs` | `Application/Offers/ReadModels/OfferReadModelComposer.cs` |

## 13. Persian text in Contracts (disclosed, NOT changed)

`Contracts/Ports/ReturnPolicyContracts.cs` carries Persian XML docs and Persian label literals (`"غیرقابل مرجوعی"`, `" روز پس از تحویل"`). Persian text in `*.Contracts` is a repo-wide pattern: 22 of 22 modules with a Contracts project contain Persian source text (e.g. `Catalog` 6 files, `Order` 7 files). `ReturnPolicyOptions` (SectionName) is also bound from `appsettings` by `OfferModule`. These literals are **observable behavior** locked by `Host.Tests/OfferReturnPolicyResolverTests`; moving/removing them would be a behavior change. W0/W1 preserve them verbatim; a resource-based conversion is deferred as a repo-wide localization decision (recorded as a follow-up, not an AMC blocker).

## 14. Behavior-preservation checklist (must remain unchanged)

- routes: `GET /offers`, `POST /offers`, `GET /offers/{offerId:guid}`, `PATCH /offers/{offerId:guid}`, `POST|PUT /offers/{offerId:guid}/price`, `POST|PUT /offers/{offerId:guid}/inventory`
- HTTP methods, status codes, raw-DTO success shape, `Created` location
- 17 stable error codes + classifications + HTTP statuses + localization keys + `.resx` semantics
- 22 validation codes and their messages
- request/response DTO semantics; `OfferReference` / `SellerOfferDetailPage` shapes
- seller authorization semantics (`IOfferSellerAuthorizer.RequireAuthorizedAsync`)
- domain invariants and `Result`-based failure strategy
- `offer` schema, 3 migrations, `OfferDbContext` model
- `IClock` / `IIdGenerator` usage; telemetry span names and dimensions
- `ReturnPolicyOptions` configuration section `Tooba:ReturnPolicy` and its defaults

## 15. Migration order (W1)

1. Split `ReturnPolicyContracts.cs` → `Contracts/ReturnPolicy/*` + `Application/ReturnPolicy/ReturnPolicyResolver.cs`.
2. Move Application request/validator/policy/port/read-model files into capability folders (with exact namespaces).
3. Remove now-empty `Application/Validators`, `Application/Mappings`, `Application/Ports`, `Application/ReadModels`, `Application/Policies`.
4. Update consumers: `OfferSellerEndpoints.cs`, `OfferModule.cs`, `OfferStore.cs`, `OfferSellerProductIdLookup.cs`, Offer.Tests, `Host.Tests/OfferDirectoryTestHelper.cs`, `Inventory.Tests` path reference.
5. Add cohesion/structure guards (W2) and repair the 2 stale guards.

## 16. Verification plan

- `dotnet build Tooba.Offer.Tests.csproj` — 0 warnings / 0 errors
- `dotnet test Tooba.Offer.Tests.csproj` — 0 failed (≥79 passed, +new guards)
- `dotnet build Tooba.Host.csproj` and targeted `Tooba.Host.Tests` runs for the Offer/return-policy/checkout/seller-grid surfaces
- `dotnet build Tooba.Order.Tests.csproj` (Order consumes `IReturnPolicyResolver` + `Offer.Contracts`)
- confirm no foreign project reference added; no schema/migration change

## 17. Certification blockers

None at analyze time. Structural items (technical-axis-first Application + 11 single-file request leaves) are the W2 gate; they are locally repairable and are the reason `Structure-Handoff-State = REQUIRED`.

## 18. Final disposition

`READY_TO_MIGRATE`
