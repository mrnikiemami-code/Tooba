# TB-TMAR-CART-AMSC-001-W2 — physical tree (after)

## `Tooba.Cart.Application`

```text
GlobalUsings.Domain.cs                                  Tooba.Cart.Application (global usings)
GlobalUsings.Layout.cs                                  Tooba.Cart.Application (global usings)
Carts/Commands/AddCartLineCommand.cs                    Tooba.Cart.Application.Carts.Commands
Carts/Commands/ChangeCartLineQuantityCommand.cs         Tooba.Cart.Application.Carts.Commands
Carts/Commands/CreateGuestCartCommand.cs                Tooba.Cart.Application.Carts.Commands
Carts/Commands/MergeCartAfterLoginCommand.cs            Tooba.Cart.Application.Carts.Commands
Carts/Commands/RemoveCartLineCommand.cs                 Tooba.Cart.Application.Carts.Commands
Carts/Queries/GetCartQuery.cs                           Tooba.Cart.Application.Carts.Queries
Carts/Queries/GetCurrentAuthenticatedCartQuery.cs       Tooba.Cart.Application.Carts.Queries
Carts/Validators/AddCartLineCommandValidator.cs         Tooba.Cart.Application.Carts.Validators
Carts/Validators/ChangeCartLineQuantityCommandValidator.cs Tooba.Cart.Application.Carts.Validators
Carts/Validators/GetCartQueryValidator.cs               Tooba.Cart.Application.Carts.Validators
Carts/Validators/RemoveCartLineCommandValidator.cs      Tooba.Cart.Application.Carts.Validators
Composition/CartOperation.cs                            Tooba.Cart.Application.Composition
Conversion/CartConversionAdapter.cs                     Tooba.Cart.Application.Conversion
Lifetime/CartExpiryOptions.cs                           Tooba.Cart.Application.Lifetime
Lifetime/CartLifetimeOptions.cs                         Tooba.Cart.Application.Lifetime
Lifetime/ICartExpiryReconciler.cs                       Tooba.Cart.Application.Lifetime
Ports/CartPersistenceHours.cs                           Tooba.Cart.Application.Ports
Ports/ICartCommerceContextResolver.cs                   Tooba.Cart.Application.Ports
Ports/ICartDirectory.cs                                 Tooba.Cart.Application.Ports
Ports/ICartPersistenceHoursResolver.cs                  Tooba.Cart.Application.Ports
Ports/ICartPersistenceHoursSource.cs                    Tooba.Cart.Application.Ports
Presentation/CartCurrencyTotals.cs                      Tooba.Cart.Application.Presentation
Presentation/CartPresentationComposer.cs                Tooba.Cart.Application.Presentation
Validation/CartFluentRules.cs                           Tooba.Cart.Application.Validation
Validation/CartValidationCodes.cs                       Tooba.Cart.Application.Validation
```

## Other projects (unchanged in W2)

```text
Tooba.Cart.Contracts/     Checkout/ Errors/ Lifetime/ Presentation/
Tooba.Cart.Domain/        Aggregates/ Entities/ Events/ ValueObjects/
Tooba.Cart.Infrastructure/ DependencyInjection/ Directories/ Events/ Lifetime/
                           Messaging/ Persistence/ Security/
Tooba.Cart.Endpoints/     Errors/ Resources/ Storefront/ + CartEndpointModule.cs (root allowlist)
Tooba.Cart.Tests/         Architecture/ Behavior/ Endpoints/ Validation/ (not a production surface)
```

## Removed

```text
Tooba.Cart.Application/Commands/                     (5 leaf folders)
Tooba.Cart.Application/Queries/                      (2 leaf folders)
Tooba.Cart.Application/Models/CartPage.cs            (comment-only tombstone)
```

## Solution Explorer

```text
/Modules/Cart/   (unchanged — was already canonical)
  Tooba.Cart.Domain
  Tooba.Cart.Contracts
  Tooba.Cart.Application
  Tooba.Cart.Infrastructure
  Tooba.Cart.Endpoints
  Tooba.Cart.Tests
```
