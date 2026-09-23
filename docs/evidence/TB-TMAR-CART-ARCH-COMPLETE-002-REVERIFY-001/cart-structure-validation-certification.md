# TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001 — Cart Structure + Validation Certification

Parent task: `TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001`
Lock: `ARCH-COMPLETE-002`
Standard: `docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md`
Manifest: `docs/architecture/tmar-module-structure-manifests.json`

Scope: Cart physical-structure reverification + complete FluentValidation coverage for
endpoint-reachable Cart MediatR requests. No business-logic redesign, no route change,
no DB/migration change, no Host authority change, no frontend change, no Order production change.

## 1. Cart Application root files + classification

Project: `src/backend/Modules/Cart/Tooba.Cart.Application`

| Root file | Classification | Justification |
| --- | --- | --- |
| `GlobalUsings.Domain.cs` | ALLOWED (explicit allowlist) | Project-wide shared import of Cart Domain `Aggregates`, `Entities`, `Events`. Not capability-specific; contains no foreign-module Application/Infrastructure/Domain dependency. |
| `GlobalUsings.Layout.cs` | ALLOWED (explicit allowlist) | Project-wide shared import of `Tooba.Cart.Contracts`, `Cart.Application.Ports`, `Cart.Application.Conversion`, `Cart.Application.Lifetime`. Not capability-specific; hides no foreign module dependency. |

No other `.cs` file exists at Application root. Capability folders present:
`Commands/`, `Queries/`, `Models/`, `Ports/`, `Lifetime/`, `Conversion/`, `Errors/`,
`Presentation/`, `Validation/`.

`Ports/CartPersistenceHours.cs` is Cart persistence policy (produces Cart behavior, not a pure
contract) and therefore lives under `Ports` as part of the Cart persistence-hours capability,
consistent with `ICartPersistenceHoursResolver`/`ICartPersistenceHoursSource` in the same folder.
Its namespace (`Tooba.Cart.Application.Ports`) already matches its path, so no move was needed.

## 2. Cart Endpoints root allowlist

Project: `src/backend/Modules/Cart/Tooba.Cart.Endpoints`

| Root file | Classification |
| --- | --- |
| `CartEndpointModule.cs` | ALLOWED (composition entry only) |

Shared `Errors/` (`CartErrorCatalogContributor.cs`) and `Resources/` (`CartErrorResources.cs`) stay in
their own folders. The capability route surface is `Storefront/CartStorefrontEndpoints.cs` and stays
path↔namespace aligned (`Tooba.Cart.Endpoints.Storefront`). No route was added, removed, or changed.

## 3. Cart Infrastructure root files + classification

Project: `src/backend/Modules/Cart/Tooba.Cart.Infrastructure`

| Root file | Classification | Justification |
| --- | --- | --- |
| `GlobalUsings.Domain.cs` | ALLOWED (explicit allowlist) | Project-wide shared Cart Domain imports (`Aggregates`, `Entities`, `Events`, `ValueObjects`). Not capability-specific. |
| `GlobalUsings.Layout.cs` | ALLOWED (explicit allowlist) | Project-wide shared layout imports (`Cart.Application.Ports`, `Conversion`, `Lifetime` + Cart Infrastructure `Directories`, `Security`, `Messaging`, `DependencyInjection`). Not capability-specific. |

No capability implementation exists at Infrastructure root. `DependencyInjection/CartModule.cs` stays in
its coherent DI folder (not moved to root merely for uniformity). Capability/integration folders:
`Directories/`, `Events/`, `Lifetime/`, `Messaging/`, `Persistence/` (+ `Persistence/Migrations/`),
`Security/`, `DependencyInjection/`.

## 4. Path ↔ namespace result

All namespace-bearing sources in `Tooba.Cart.Application`, `Tooba.Cart.Endpoints` and
`Tooba.Cart.Infrastructure` resolve to `project name + relative folder path`.
Global using files declare no namespace by design and are exempt (same convention as Order).
Migrations remain under `Persistence/Migrations`.
Result: `PATH_NAMESPACE_ALIGNMENT = PASS` — no violation found, no file required a move.

## 5. Foreign boundary result

`Tooba.Cart.Application` project references: `Tooba.BuildingBlocks`, `Tooba.Cart.Contracts`,
`Tooba.Cart.Domain`, `Tooba.Catalog.Contracts`, `Tooba.Party.Contracts`, `Tooba.Offer.Contracts`,
`Tooba.Pricing.Contracts`, `Tooba.Inventory.Contracts`.

- No foreign `*.Application` / `*.Infrastructure` / `*.Domain` reference.
- No foreign DbContext (`CatalogDbContext`, `InventoryDbContext`, `OfferDbContext`, `PricingDbContext`).
- No Host dependency.
- Allowed boundary style only: module `Contracts` + `BuildingBlocks`. Result: `PASS`.

## 6. Endpoint-reachable request validation table

Endpoints source: `Tooba.Cart.Endpoints/Storefront/CartStorefrontEndpoints.cs` (7 routes).

| # | Request | Route | Classification | Reason |
| --- | --- | --- | --- | --- |
| 1 | `CreateGuestCartCommand` | `POST /v1/storefront/cart` | NO_VALIDATOR_REQUIRED | Parameterless request; no transport input to validate. |
| 2 | `GetCurrentAuthenticatedCartQuery` | `GET /v1/storefront/cart/current` | NO_VALIDATOR_REQUIRED | Parameterless request; authentication requirement is business/security behavior, not transport shape. |
| 3 | `GetCartQuery` | `GET /v1/storefront/cart/{cartId:guid}` | VALIDATOR_REQUIRED | `CartId` non-empty; `GuestSecret` optional (no existing max-length contract). |
| 4 | `MergeCartAfterLoginCommand` | `POST /v1/storefront/cart/merge` | NO_VALIDATOR_REQUIRED | Inputs intentionally optional (`CartId`, `GuestSecret`); authenticated-user requirement is business/security validation. |
| 5 | `AddCartLineCommand` | `POST /v1/storefront/cart/{cartId:guid}/lines` | VALIDATOR_REQUIRED | `CartId`, `OfferId` non-empty; `ExpectedVersion >= 0`; quantity remains domain/business. |
| 6 | `ChangeCartLineQuantityCommand` | `PATCH /v1/storefront/cart/{cartId:guid}/lines/{lineId:guid}` | VALIDATOR_REQUIRED | `CartId`, `LineId` non-empty; `ExpectedVersion >= 0`; zero quantity keeps existing remove-line semantics. |
| 7 | `RemoveCartLineCommand` | `DELETE /v1/storefront/cart/{cartId:guid}/lines/{lineId:guid}` | VALIDATOR_REQUIRED | `CartId`, `LineId` non-empty; `ExpectedVersion >= 0`. |

Totals: endpoint-reachable requests = 7; VALIDATOR_REQUIRED = 4; NO_VALIDATOR_REQUIRED = 3.
No request is unclassified.

## 7. Validators added

| Validator | Location |
| --- | --- |
| `GetCartQueryValidator` | `Queries/GetCart/GetCartQueryValidator.cs` |
| `AddCartLineCommandValidator` | `Commands/AddCartLine/AddCartLineCommandValidator.cs` |
| `ChangeCartLineQuantityCommandValidator` | `Commands/ChangeCartLineQuantity/ChangeCartLineQuantityCommandValidator.cs` |
| `RemoveCartLineCommandValidator` | `Commands/RemoveCartLine/RemoveCartLineCommandValidator.cs` |

Shared syntactic rules: `Validation/CartFluentRules.cs`.
Stable machine codes: `Validation/CartValidationCodes.cs`.

No empty validators exist. Validator count is asserted by the coverage guard (exactly 4).

## 8. Stable error codes

```
cart.validation.cart_id_required
cart.validation.offer_id_required
cart.validation.line_id_required
cart.validation.expected_version_min
```

No raw/localized message identity.

## 9. Validation error semantics

Canonical shared pipeline reused unchanged:
`MediatR → ValidationBehavior → FluentValidation → ValidationException → SafeErrorMapper → validation.failed`
with per-field stable codes surfaced through `ValidationErrors`. No Cart-specific pipeline was created.
`TryReadExpectedVersion` in the endpoint was not redesigned; the MediatR validator still enforces
non-negative version on the resulting integer.

## 10. Business validation separation

FluentValidation does NOT perform: authentication, cart existence, ownership/access checks,
guest-secret correctness, persisted version conflict detection, offer existence, stock/inventory,
merge eligibility, active-cart state, campaign eligibility, or state-dependent quantity rules.
These remain in handlers/`ICartDirectory`/Domain. Change-line zero-quantity remove semantics preserved.

## 11. Validator coverage guard

`src/backend/Modules/Cart/Tooba.Cart.Tests/Validation/CartEndpointValidatorCoverageGuardTests.cs`

- scans `Tooba.Cart.Endpoints` recursively and extracts every constructed `*Command`/`*Query`,
- compares them to an explicit 7-row classification manifest (exactly once each),
- resolves `IValidator<TRequest>` through real `AddToobaCqrsFoundation(...)` DI for every
  `VALIDATOR_REQUIRED` request (no representative substitution),
- asserts `NO_VALIDATOR_REQUIRED` requests have no registered validator,
- asserts validator count and primitive-shape-only behavior (no `ICartDirectory`/`DbContext`/auth logic),
- asserts `ValidationException → validation.failed` stable semantics.

## 12. Manifest certification details

`docs/architecture/tmar-module-structure-manifests.json`:

- `Cart` added with `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`.
- `Tooba.Cart.Application` root allowlist: `GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs`
  (+ justification), forbidden root dump files listed.
- `Tooba.Cart.Endpoints` root allowlist: `CartEndpointModule.cs`.
- `Tooba.Cart.Infrastructure` root allowlist: `GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs`
  (+ justification), forbidden root dump files listed.
- `Cart` removed from `uncertifiedHttpOwningModules`.

`docs/architecture/tmar-current-state.json`:
`structureLock.certifiedModules = ["Order", "Cart"]` — exactly two; no other module certified.
`locksVersion` remains `ARCH-COMPLETE-002`.

## 13. Validation performed

- Cart architecture guard + Cart validator coverage guard (`Tooba.Cart.Tests`): PASS.
- Reusable `TmarCompleteReferenceStructureGateTests`: PASS.
- `TmarDurableGuardTests`: PASS.
- Focused Host guard subset (structure gate, durable guard, Cart lifetime separation,
  Host Cart residual guard, Host folder structure, Host module endpoint ownership): PASS.
- `dotnet build src/backend/Tooba.slnx`: 0 errors.

## 14. Preserved state

- Cart: `COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
- Host: `HOST_CART_ILLEGAL_AUTHORITY = 0` (unchanged).
- Order: existing `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED` preserved (no Order production change).
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`; frontend unchanged.
- No DB schema / migration change; no route change; no production business-behavior change.
