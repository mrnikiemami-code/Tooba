# Offer cohesion balance — W0

File-Cohesion-State: `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (narrow: 2 findings)

## Size audit (production sources, LOC)

| File | LOC | Classification |
| --- | --- | --- |
| `Tooba.Offer.Infrastructure/Adapters/Tracing/OfferModuleCallGateways.cs` | 303 | `OVERSIZED_ONLY` — one cohesive traced-gateway decoration set (12 decorators, identical shape). WATCH only; no split required. |
| `Tooba.Offer.Infrastructure/Adapters/OfferStore.cs` | 231 | `COHESIVE` — one persistence adapter implementing `IOfferStore` + `IOfferQueryGateway`. |
| `Tooba.Offer.Domain/Aggregates/SellerOffer.cs` | 189 | `COHESIVE` — one aggregate, one reason to change. |
| `Tooba.Offer.Contracts/Ports/ReturnPolicyContracts.cs` | 182 | **`MULTI_RESPONSIBILITY_COHESION_VIOLATION`** — see below. |
| `Tooba.Offer.Endpoints/Seller/OfferSellerEndpoints.cs` | 120 | `COHESIVE` — one endpoint class. |
| all other production files | ≤ 112 | `COHESIVE` |

Maximum production LOC = 303. `ARCH-SIZE-001` ceiling is 800. No oversized production file.

## Finding 1 — `Contracts/Ports/ReturnPolicyContracts.cs` (182 LOC, 4 responsibilities)

One file declares:

| # | Type | Reason to change | Correct home |
| --- | --- | --- | --- |
| 1 | `ReturnPolicyOptions` (config POCO) | configuration shape | Contracts (`ReturnPolicy`) |
| 2 | `OfferReturnPolicyChoices` (stable choice constants + `Normalize`) | boundary vocabulary | Contracts (`ReturnPolicy`) |
| 3 | `ResolvedReturnPolicy` (checkout snapshot DTO) | boundary DTO | Contracts (`ReturnPolicy`) |
| 4 | `IReturnPolicyResolver` (port) | boundary contract | Contracts (`ReturnPolicy`) |
| 5 | `ReturnPolicyResolver` (concrete governance implementation, `Result` decisions, Persian labels) | **business policy** | **Application** |

Additional defect: `Contracts` must be boundary contracts only. A concrete resolver implementation with decision logic in `*.Contracts` is a `Contracts-Boundary-State` smell. It is also the only reason `Tooba.Offer.Contracts` needs `Tooba.BuildingBlocks.Results` semantics beyond stable codes.

`ResolvedReturnPolicy.LabelFa` is Persian source text (`"غیرقابل مرجوعی"`, `" روز پس از تحویل"`). Moving it to Application removes Persian literals from the Contracts project while keeping them inside Offer (behavior preserved; see §13 of analyze.md).

## Finding 2 — `Application/Validators/` (7 files) misplaced

`Application/Validators/` mixes two different concerns:

| Concern | Files | Correct home |
| --- | --- | --- |
| Shared cross-capability FluentValidation fragments | `OfferFluentRules.cs` (102), `OfferValidationCodes.cs` (56) | `Application/Validation/` |
| Per-request transport validators | `CreateOfferCommandValidator.cs`, `UpdateOfferCommandValidator.cs`, `SetOfferPriceCommandValidator.cs`, `SetOfferInventoryCommandValidator.cs`, `GetOfferQueryValidator.cs` | next to their request (`Offers/Commands|Queries/<UseCase>/`) |

Repository precedent: `Modules/Content/Tooba.Content.Application/Validators/` holds exactly **one** shared file, while each Content capability (`Articles`, `Authors`, `Categories`, `Comments`, `Media`, `Tags`) owns its own `Validators/` folder.

Consequence today: a reader must open a distant `Validators/` folder to discover that `CreateOfferCommand` has transport validation; the Solution Explorer hides request↔validator ownership.

## No over-split risk

- No `OVER_SPLIT`: no cosmetic split exists; no file was split to game a size guard.
- No god-file flattening required: the planned moves **increase** cohesion and do not merge unrelated files.
- `OfferModuleCallGateways.cs` stays a single file (12 identical-shape decorators are more readable together than split into 12 tiny files).

## W1/W2 action

Split finding 1 into 5 files by responsibility; split finding 2 by moving request validators to their capability folders and shared rules to `Application/Validation/`. Both are behavior-preserving moves (namespace/using updates only).
