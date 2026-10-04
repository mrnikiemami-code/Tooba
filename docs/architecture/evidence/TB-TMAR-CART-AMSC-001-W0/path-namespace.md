# TB-TMAR-CART-AMSC-001-W0 — Path ↔ Namespace

## `Path-Namespace-State = EXACT`

Every hand-written production `.cs` file's declared namespace equals its path-derived namespace for
its project. Verified by reading the first `namespace` declaration of every non-generated file.

## `Tooba.Cart.Contracts`

| Path (relative) | Namespace | Path-derived | Match |
| --- | --- | --- | --- |
| `Checkout/CartContracts.cs` | `Tooba.Cart.Contracts` | `Tooba.Cart.Contracts.Checkout` | **ROOT_NAMESPACE** (intentional, see note) |
| `Lifetime/ICartPersistenceHoursSource.cs` | `Tooba.Cart.Contracts.Lifetime` | same | YES |
| `Presentation/CartPresentationContracts.cs` | `Tooba.Cart.Contracts` | `Tooba.Cart.Contracts.Presentation` | **ROOT_NAMESPACE** (intentional, see note) |

### Note — the two root-namespace Contracts files are deliberate and consistent with the repository

`Cart` places its cross-module contract types in the **root** namespace `Tooba.Cart.Contracts` while
physically foldering them by capability. This is the repository's dominant Contracts convention and
is what every external consumer imports (`using Tooba.Cart.Contracts;`):

| Module | Physical file | Namespace |
| --- | --- | --- |
| Cart | `Checkout/CartContracts.cs` | `Tooba.Cart.Contracts` |
| Cart | `Presentation/CartPresentationContracts.cs` | `Tooba.Cart.Contracts` |
| Catalog | `Checkout/CatalogCheckoutLookupContracts.cs` | `Tooba.Catalog.Contracts` |
| Inventory | `Cart/CartInventoryHoldContracts.cs` | `Tooba.Inventory.Contracts` |
| Order | `Storefront/OrderStorefrontActorContracts.cs` | `Tooba.Order.Contracts` |
| Payment | `Checkout/OrderPaymentProjectionContracts.cs` | `Tooba.Payment.Contracts` |

`Lifetime/ICartPersistenceHoursSource.cs` uses the folder-qualified namespace
`Tooba.Cart.Contracts.Lifetime` because `Cart.Application` already declares a same-named type in
`Tooba.Cart.Application.Ports` and the folder namespace disambiguates the two. This is a legitimate,
already-shipped convention.

**W2 constraint:** any Contracts split must preserve the root namespace for the types external
consumers import. `CartArchitectureGuardTests.AssertNamespacesAlign` enforces
`ns == nsPrefix || ns.StartsWith(nsPrefix + "." + folder)` — i.e. it accepts **both** the root form and
the folder-qualified form. So W2 is free to split `Checkout/CartContracts.cs` into multiple
capability files **as long as each keeps either the root namespace or its own folder-qualified
namespace**.

## `Tooba.Cart.Domain`

| Path (relative) | Namespace | Match |
| --- | --- | --- |
| `Aggregates/ShoppingCart.cs` | `Tooba.Cart.Domain.Aggregates` | YES |
| `Entities/CartLine.cs` | `Tooba.Cart.Domain.Entities` | YES |
| `Events/CartConvertedDomainEvent.cs` | `Tooba.Cart.Domain.Events` | YES |
| `Events/CartCreatedDomainEvent.cs` | `Tooba.Cart.Domain.Events` | YES |
| `Events/CartExpiredDomainEvent.cs` | `Tooba.Cart.Domain.Events` | YES |
| `Events/CartLineAddedDomainEvent.cs` | `Tooba.Cart.Domain.Events` | YES |
| `Events/CartLineChangedDomainEvent.cs` | `Tooba.Cart.Domain.Events` | YES |
| `Events/CartLineRemovedDomainEvent.cs` | `Tooba.Cart.Domain.Events` | YES |
| `ValueObjects/CartAccessKind.cs` | `Tooba.Cart.Domain.ValueObjects` | YES |
| `ValueObjects/CartConversionIntent.cs` | `Tooba.Cart.Domain.ValueObjects` | YES |
| `ValueObjects/CartStatus.cs` | `Tooba.Cart.Domain.ValueObjects` | YES |

## `Tooba.Cart.Application`

| Path (relative) | Namespace | Match |
| --- | --- | --- |
| `GlobalUsings.Domain.cs` | (global usings, no namespace) | N/A — allowlisted |
| `GlobalUsings.Layout.cs` | (global usings, no namespace) | N/A — allowlisted |
| `Commands/AddCartLine/AddCartLineCommand.cs` | `Tooba.Cart.Application.Commands.AddCartLine` | YES (current shape) |
| `Commands/AddCartLine/AddCartLineCommandValidator.cs` | `Tooba.Cart.Application.Commands.AddCartLine` | YES |
| `Commands/ChangeCartLineQuantity/ChangeCartLineQuantityCommand.cs` | `Tooba.Cart.Application.Commands.ChangeCartLineQuantity` | YES |
| `Commands/ChangeCartLineQuantity/ChangeCartLineQuantityCommandValidator.cs` | same | YES |
| `Commands/CreateGuestCart/CreateGuestCartCommand.cs` | `Tooba.Cart.Application.Commands.CreateGuestCart` | YES |
| `Commands/MergeCartAfterLogin/MergeCartAfterLoginCommand.cs` | `Tooba.Cart.Application.Commands.MergeCartAfterLogin` | YES |
| `Commands/RemoveCartLine/RemoveCartLineCommand.cs` | `Tooba.Cart.Application.Commands.RemoveCartLine` | YES |
| `Commands/RemoveCartLine/RemoveCartLineCommandValidator.cs` | same | YES |
| `Conversion/CartConversionAdapter.cs` | `Tooba.Cart.Application.Conversion` | YES |
| `Errors/CartErrorCodes.cs` | `Tooba.Cart.Application.Errors` | YES |
| `Errors/CartExceptionMapper.cs` | `Tooba.Cart.Application.Errors` | YES |
| `Lifetime/CartExpiryOptions.cs` | `Tooba.Cart.Application.Lifetime` | YES |
| `Lifetime/CartLifetimeOptions.cs` | `Tooba.Cart.Application.Lifetime` | YES |
| `Lifetime/ICartExpiryReconciler.cs` | `Tooba.Cart.Application.Lifetime` | YES |
| `Models/CartPage.cs` | `Tooba.Cart.Application.Models` | YES (but file declares no types — G6) |
| `Ports/CartPersistenceHours.cs` | `Tooba.Cart.Application.Ports` | YES |
| `Ports/ICartCommerceContextResolver.cs` | `Tooba.Cart.Application.Ports` | YES |
| `Ports/ICartDirectory.cs` | `Tooba.Cart.Application.Ports` | YES |
| `Ports/ICartPersistenceHoursResolver.cs` | `Tooba.Cart.Application.Ports` | YES |
| `Ports/ICartPersistenceHoursSource.cs` | `Tooba.Cart.Application.Ports` | YES |
| `Presentation/CartCurrencyTotals.cs` | `Tooba.Cart.Application.Presentation` | YES |
| `Presentation/CartPresentationComposer.cs` | `Tooba.Cart.Application.Presentation` | YES |
| `Queries/GetCart/GetCartQuery.cs` | `Tooba.Cart.Application.Queries.GetCart` | YES (current shape) |
| `Queries/GetCart/GetCartQueryValidator.cs` | same | YES |
| `Queries/GetCurrentCart/GetCurrentAuthenticatedCartQuery.cs` | `Tooba.Cart.Application.Queries.GetCurrentCart` | YES |
| `Validation/CartFluentRules.cs` | `Tooba.Cart.Application.Validation` | YES |
| `Validation/CartValidationCodes.cs` | `Tooba.Cart.Application.Validation` | YES |

## `Tooba.Cart.Infrastructure`

| Path (relative) | Namespace | Match |
| --- | --- | --- |
| `GlobalUsings.Domain.cs` | (global usings) | N/A — allowlisted |
| `GlobalUsings.Layout.cs` | (global usings) | N/A — allowlisted |
| `DependencyInjection/CartModule.cs` | `Tooba.Cart.Infrastructure.DependencyInjection` | YES |
| `Directories/CartDirectory.cs` | `Tooba.Cart.Infrastructure.Directories` | YES |
| `Directories/CartLineCurrency.cs` | `Tooba.Cart.Infrastructure.Directories` | YES |
| `Events/CartEvents.cs` | `Tooba.Cart.Infrastructure.Events` | YES |
| `Lifetime/CartCommerceContextResolver.cs` | `Tooba.Cart.Infrastructure.Lifetime` | YES |
| `Lifetime/CartExpiryReconciler.cs` | `Tooba.Cart.Infrastructure.Lifetime` | YES |
| `Lifetime/CartExpiryWorker.cs` | `Tooba.Cart.Infrastructure.Lifetime` | YES |
| `Lifetime/CartPersistenceHoursSource.cs` | `Tooba.Cart.Infrastructure.Lifetime` | YES |
| `Lifetime/CatalogCartPersistenceHoursResolver.cs` | `Tooba.Cart.Infrastructure.Lifetime` | YES |
| `Messaging/CartOutboxRegistration.cs` | `Tooba.Cart.Infrastructure.Messaging` | YES |
| `Persistence/CartDbContext.cs` | `Tooba.Cart.Infrastructure.Persistence` | YES |
| `Persistence/Migrations/*` | EF-generated | exempt |
| `Security/CartCredentialHasher.cs` | `Tooba.Cart.Infrastructure.Security` | YES |

## `Tooba.Cart.Endpoints`

| Path (relative) | Namespace | Match |
| --- | --- | --- |
| `CartEndpointModule.cs` | `Tooba.Cart.Endpoints` | YES — root allowlisted |
| `Errors/CartErrorCatalogContributor.cs` | `Tooba.Cart.Endpoints.Errors` | YES |
| `Resources/CartErrorResources.cs` | `Tooba.Cart.Endpoints.Resources` | YES |
| `Storefront/CartStorefrontEndpoints.cs` | `Tooba.Cart.Endpoints.Storefront` | YES |

## Alias / shim scan

| Forbidden workaround | Hits |
| --- | --- |
| `TypeForwardedTo` | **0** |
| `global using` alias hiding a foreign module (`global using X = Tooba.Other...`) | **0** |
| `using` alias to a foreign Application/Infrastructure/Domain type | **0** |
| duplicate compatibility type | **0** |

The only alias in the module is `using CartContract = Tooba.Cart.Contracts;` in
`CartDirectory.cs:11` — a **same-module** alias used to disambiguate `Tooba.Cart.Contracts.CartStatus`
from `Tooba.Cart.Domain.ValueObjects.CartStatus`. It hides no foreign coupling and is lawful.

## Guard status at HEAD

`CartArchitectureGuardTests.AssertNamespacesAlign` covers `Contracts`, `Domain`, `Application`,
`Infrastructure`, `Endpoints` and **passes** for the namespace dimension. The same test class fails on
the **folder allowlist** dimension (`AllowedContractsFolders` missing `Lifetime`) — see
`root-allowlist.md`.
