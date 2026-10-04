# TB-TMAR-OFFER-AMC-001 — W1 MIGRATE

- Task: `TB-TMAR-OFFER-AMC-001` (ARCHITECT_DIRECT_AMSC)
- Module: `Modules/Offer`
- Skill: `Migrate` (second of four)
- Starting base: `af421a82` (`HEAD == origin/main`)
- Behavior-Preservation-Risk: `LOW` (pure relocation + one responsibility split; no route/status/DTO/schema change)

## 1. What this wave changed

| # | Change | Kind |
| --- | --- | --- |
| 1 | `Contracts/Ports/ReturnPolicyContracts.cs` (182 LOC, 4 responsibilities) split into `Contracts/ReturnPolicy/*` | cohesion split |
| 2 | Application requests moved into capability folders under `Application/Offers/` | relocation |
| 3 | Validators co-located with their request type | relocation |
| 4 | Shared validation helpers moved to `Application/Validation/` | relocation |
| 5 | Empty technical-axis Application folders deleted | cleanup |
| 6 | Namespaces updated to exact path-derived equality | relocation consequence |
| 7 | Consumers updated (Endpoints / Infrastructure / Host / tests) | consumer repair |
| 8 | Two stale Offer architecture guards repaired + one new folder-granularity guard | guard correctness |

## 2. Cohesion split — `ReturnPolicyContracts.cs`

The single 182 LOC file carried four different responsibilities. It was split by responsibility:

| New file | Responsibility | Namespace |
| --- | --- | --- |
| `Contracts/ReturnPolicy/ReturnPolicyOptions.cs` | configuration vocabulary (`SectionName`, limits, defaults) | `Tooba.Offer.Contracts.ReturnPolicy` |
| `Contracts/ReturnPolicy/OfferReturnPolicyChoices.cs` | boundary enum (`Default` / `Returnable` / `NonReturnable`) | `Tooba.Offer.Contracts.ReturnPolicy` |
| `Contracts/ReturnPolicy/ResolvedReturnPolicy.cs` | immutable resolved snapshot + Persian display label | `Tooba.Offer.Contracts.ReturnPolicy` |
| `Contracts/ReturnPolicy/IReturnPolicyResolver.cs` | the port only | `Tooba.Offer.Contracts.ReturnPolicy` |
| `Contracts/ReturnPolicy/ReturnPolicyResolver.cs` | the concrete governance implementation | `Tooba.Offer.Contracts.ReturnPolicy` |

### Why the implementation stays in `Tooba.Offer.Contracts` (decision, not accident)

W0 §12.1 proposed moving `ReturnPolicyResolver` to `Tooba.Offer.Application.ReturnPolicy`. That plan was **rejected
on evidence** during implementation:

- `Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs:105` constructs the resolver as a
  **default seam**: `_returnPolicies = returnPolicies ?? new ReturnPolicyResolver(new ReturnPolicyOptions());`
- The same file is also the target of the check
  `ReturnFoundationTests.Return_is_not_order_and_modules_do_not_reference_each_other_infrastructure`
  ("modules do not reference each other's infrastructure").
- `Tooba.Order.Infrastructure` therefore may reference **only** `Tooba.Offer.Contracts`. An
  `Offer.Application` reference is not available to it and must not be created.
- `OfferModule` (Offer.Infrastructure) also needs the same concrete default, and it already references
  `Offer.Contracts`.

Moving the implementation into Application would have required either a new foreign Application edge from
Order, or a third copy of the resolver. Both are worse than keeping one Contracts-only implementation that
every consumer can legally reach. The multi-responsibility file was therefore split **in place** — the
cohesion defect is repaired without introducing coupling.

`Tooba.Offer.Contracts` references only `Tooba.BuildingBlocks`; the resolver uses `Result` and the Offer
boundary error codes, both already legal inside that assembly.

## 3. Application capability-first layout (after W1)

```text
Tooba.Offer.Application/
  Offers/
    Commands/
      ActivateOffer/ActivateOfferCommand.cs
      ArchiveOffer/ArchiveOfferCommand.cs
      CreateOffer/{CreateOfferCommand.cs, CreateOfferCommandValidator.cs}
      SetOfferInventory/{SetOfferInventoryCommand.cs, SetOfferInventoryCommandValidator.cs}
      SetOfferPrice/{SetOfferPriceCommand.cs, SetOfferPriceCommandValidator.cs}
      SetOrderQuantityLimits/SetOrderQuantityLimitsCommand.cs
      SetReturnPolicy/SetReturnPolicyCommand.cs
      SuspendOffer/SuspendOfferCommand.cs
      UpdateOffer/{UpdateOfferCommand.cs, UpdateOfferCommandValidator.cs}
    Queries/
      GetOffer/{GetOfferQuery.cs, GetOfferQueryValidator.cs}
      ListSellerOffers/ListSellerOffersCommand.cs (query)
    Mappings/OfferContractMapping.cs
    Policies/PrimaryOfferSelectionPolicy.cs
    Ports/IOfferStore.cs
    ReadModels/OfferReadModelComposer.cs
  Validation/
    OfferFluentRules.cs
    OfferValidationCodes.cs
```

Every request now owns a real capability folder; validators sit next to the request they validate.

## 4. Move table (executed)

| From | To |
| --- | --- |
| `Application/Commands/*` | `Application/Offers/Commands/*` |
| `Application/Queries/*` | `Application/Offers/Queries/*` |
| `Application/Mappings/OfferContractMapping.cs` | `Application/Offers/Mappings/OfferContractMapping.cs` |
| `Application/Ports/IOfferStore.cs` | `Application/Offers/Ports/IOfferStore.cs` |
| `Application/Policies/PrimaryOfferSelectionPolicy.cs` | `Application/Offers/Policies/PrimaryOfferSelectionPolicy.cs` |
| `Application/ReadModels/OfferReadModelComposer.cs` | `Application/Offers/ReadModels/OfferReadModelComposer.cs` |
| `Application/Validators/CreateOfferCommandValidator.cs` | `Application/Offers/Commands/CreateOffer/` |
| `Application/Validators/UpdateOfferCommandValidator.cs` | `Application/Offers/Commands/UpdateOffer/` |
| `Application/Validators/SetOfferPriceCommandValidator.cs` | `Application/Offers/Commands/SetOfferPrice/` |
| `Application/Validators/SetOfferInventoryCommandValidator.cs` | `Application/Offers/Commands/SetOfferInventory/` |
| `Application/Validators/GetOfferQueryValidator.cs` | `Application/Offers/Queries/GetOffer/` |
| `Application/Validators/OfferFluentRules.cs` | `Application/Validation/` |
| `Application/Validators/OfferValidationCodes.cs` | `Application/Validation/` |
| `Contracts/Ports/ReturnPolicyContracts.cs` | deleted (split into `Contracts/ReturnPolicy/*`) |

Deleted (now empty): `Application/Commands`, `Application/Queries`, `Application/Mappings`,
`Application/Ports`, `Application/Policies`, `Application/ReadModels`, `Application/Validators`.

## 5. Consumers updated

| Consumer | Change |
| --- | --- |
| `Tooba.Offer.Endpoints/Seller/OfferSellerEndpoints.cs` | `Application.Offers.Commands.*` / `Application.Offers.Queries.*` |
| `Tooba.Offer.Infrastructure/Adapters/OfferStore.cs` | `Application.Offers.Mappings` / `Application.Offers.Ports` |
| `Tooba.Offer.Infrastructure/DependencyInjection/OfferModule.cs` | `Application.Offers.ReadModels` / `Application.Offers.Policies` / `Contracts.ReturnPolicy` |
| `Tooba.Host/Program.cs` | CQRS assembly marker → `Tooba.Offer.Application.Offers.Commands.CreateOffer.CreateOfferCommand` |
| `Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs` | `+ using Tooba.Offer.Contracts.ReturnPolicy;` (Contracts-only edge preserved) |
| `Tooba.Offer.Tests/*` | capability namespaces; `ReturnPolicy` moved to `Contracts.ReturnPolicy` |
| `Tooba.Host.Tests/*` | `Application.Offers.Ports`; `Contracts.ReturnPolicy` |
| `Tooba.Inventory.Tests/Architecture/InventoryArchitectureGuardTests.cs` | Offer path reference retargeted |

## 6. Guard correctness repair (part of COMPLETE_REFERENCE_PATTERN)

Both pre-existing RED guards disclosed in W0 §11 were repaired **to their documented intent**, not weakened:

| Guard | Defect | Repair |
| --- | --- | --- |
| `OfferPhysicalStructureGuardTests.ARCH_MODULE_PHYSICAL_001_offer_routes_absent_from_host_seller_endpoints` | asserted the module's own endpoint file must **not** contain `MapGet("/offers"` — contradicting the guard name | now asserts no file under `Tooba.Host` maps Offer routes, and that `Tooba.Offer.Endpoints/Seller/OfferSellerEndpoints.cs` **does** own them |
| `OfferArchitectureGuardTests.Host_owns_no_offer_selection_policy_or_hidden_offer_alias` | required `Host/Tooba.Host/Storefront/StorefrontComposer.cs`, evacuated to Catalog in a prior wave | now asserts the invariant against `Tooba.Catalog.Infrastructure/Storefront/StorefrontComposer.cs` (Contracts-only, no `Offer.Application`) |

New durable guard added:

- `OfferPhysicalStructureGuardTests.ARCH_MODULE_PHYSICAL_001_no_technical_axis_first_application_folders`
  fails if any of `Commands / Queries / Mappings / Ports / Policies / ReadModels / Validators / Dtos / UseCases /
  ReturnPolicy` reappears at the `Tooba.Offer.Application` root.

## 7. Behavior preservation

Unchanged by this wave:

- routes and HTTP verbs; status codes; `Created` location
- 17 stable Offer error codes, classifications, HTTP statuses, localization keys, `.resx` semantics
- 22 validation codes and messages
- request/response DTO shapes; `OfferReference` / `SellerOfferDetailPage`
- seller authorization semantics
- domain invariants and `Result` failure strategy
- `offer` schema, migrations and `OfferDbContext` model
- `IClock` / `IIdGenerator` usage, telemetry span names and dimensions
- `ReturnPolicyOptions` section `Tooba:ReturnPolicy` and its defaults
- `Contracts/ReturnPolicy/ReturnPolicyResolver.cs` logic moved verbatim (only its file boundary changed)

## 8. Coupling after W1

| Edge | State |
| --- | --- |
| Offer → foreign Domain / Application / Infrastructure | `ZERO` |
| Offer → foreign `*.Contracts` | legal (Catalog, Inventory, Party, Pricing) |
| foreign → Offer | `*.Contracts` only (`IOfferLookupGateway`, `IOfferQueryGateway`, `IOfferSellerProductIdLookup`, `IOfferDevelopmentSeedGateway`, `IPrimaryOfferSelectionPolicy`, `IReturnPolicyResolver`) |
| Cross-module joins | `NONE` |
| Host reference inside Offer | `NONE` |

Microservice extractability is preserved: the module can be lifted with only `Tooba.BuildingBlocks` and its
declared foreign `*.Contracts` edges.
