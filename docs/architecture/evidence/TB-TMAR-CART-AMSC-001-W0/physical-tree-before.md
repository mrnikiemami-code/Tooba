# TB-TMAR-CART-AMSC-001-W0 — Physical Tree (Before)

Root: `src/backend/Modules/Cart/`
`bin/` and `obj/` omitted. `[D]` = directory, otherwise file.

```text
[D] Tooba.Cart.Application
    Tooba.Cart.Application/GlobalUsings.Domain.cs
    Tooba.Cart.Application/GlobalUsings.Layout.cs
    Tooba.Cart.Application/Tooba.Cart.Application.csproj
[D] Tooba.Cart.Application/Commands
[D] Tooba.Cart.Application/Commands/AddCartLine
    Tooba.Cart.Application/Commands/AddCartLine/AddCartLineCommand.cs
    Tooba.Cart.Application/Commands/AddCartLine/AddCartLineCommandValidator.cs
[D] Tooba.Cart.Application/Commands/ChangeCartLineQuantity
    Tooba.Cart.Application/Commands/ChangeCartLineQuantity/ChangeCartLineQuantityCommand.cs
    Tooba.Cart.Application/Commands/ChangeCartLineQuantity/ChangeCartLineQuantityCommandValidator.cs
[D] Tooba.Cart.Application/Commands/CreateGuestCart
    Tooba.Cart.Application/Commands/CreateGuestCart/CreateGuestCartCommand.cs
[D] Tooba.Cart.Application/Commands/MergeCartAfterLogin
    Tooba.Cart.Application/Commands/MergeCartAfterLogin/MergeCartAfterLoginCommand.cs
[D] Tooba.Cart.Application/Commands/RemoveCartLine
    Tooba.Cart.Application/Commands/RemoveCartLine/RemoveCartLineCommand.cs
    Tooba.Cart.Application/Commands/RemoveCartLine/RemoveCartLineCommandValidator.cs
[D] Tooba.Cart.Application/Conversion
    Tooba.Cart.Application/Conversion/CartConversionAdapter.cs
[D] Tooba.Cart.Application/Errors
    Tooba.Cart.Application/Errors/CartErrorCodes.cs
    Tooba.Cart.Application/Errors/CartExceptionMapper.cs
[D] Tooba.Cart.Application/Lifetime
    Tooba.Cart.Application/Lifetime/CartExpiryOptions.cs
    Tooba.Cart.Application/Lifetime/CartLifetimeOptions.cs
    Tooba.Cart.Application/Lifetime/ICartExpiryReconciler.cs
[D] Tooba.Cart.Application/Models
    Tooba.Cart.Application/Models/CartPage.cs            <-- comment-only tombstone (F9)
[D] Tooba.Cart.Application/Ports
    Tooba.Cart.Application/Ports/CartPersistenceHours.cs
    Tooba.Cart.Application/Ports/ICartCommerceContextResolver.cs
    Tooba.Cart.Application/Ports/ICartDirectory.cs
    Tooba.Cart.Application/Ports/ICartPersistenceHoursResolver.cs
    Tooba.Cart.Application/Ports/ICartPersistenceHoursSource.cs
[D] Tooba.Cart.Application/Presentation
    Tooba.Cart.Application/Presentation/CartCurrencyTotals.cs
    Tooba.Cart.Application/Presentation/CartPresentationComposer.cs
[D] Tooba.Cart.Application/Queries
[D] Tooba.Cart.Application/Queries/GetCart
    Tooba.Cart.Application/Queries/GetCart/GetCartQuery.cs
    Tooba.Cart.Application/Queries/GetCart/GetCartQueryValidator.cs
[D] Tooba.Cart.Application/Queries/GetCurrentCart
    Tooba.Cart.Application/Queries/GetCurrentCart/GetCurrentAuthenticatedCartQuery.cs
[D] Tooba.Cart.Application/Validation
    Tooba.Cart.Application/Validation/CartFluentRules.cs
    Tooba.Cart.Application/Validation/CartValidationCodes.cs

[D] Tooba.Cart.Contracts
    Tooba.Cart.Contracts/Tooba.Cart.Contracts.csproj
[D] Tooba.Cart.Contracts/Checkout
    Tooba.Cart.Contracts/Checkout/CartContracts.cs           <-- multi-responsibility bundle (F10)
[D] Tooba.Cart.Contracts/Lifetime
    Tooba.Cart.Contracts/Lifetime/ICartPersistenceHoursSource.cs
[D] Tooba.Cart.Contracts/Presentation
    Tooba.Cart.Contracts/Presentation/CartPresentationContracts.cs

[D] Tooba.Cart.Domain
    Tooba.Cart.Domain/Tooba.Cart.Domain.csproj
[D] Tooba.Cart.Domain/Aggregates
    Tooba.Cart.Domain/Aggregates/ShoppingCart.cs
[D] Tooba.Cart.Domain/Entities
    Tooba.Cart.Domain/Entities/CartLine.cs
[D] Tooba.Cart.Domain/Events
    Tooba.Cart.Domain/Events/CartConvertedDomainEvent.cs
    Tooba.Cart.Domain/Events/CartCreatedDomainEvent.cs
    Tooba.Cart.Domain/Events/CartExpiredDomainEvent.cs
    Tooba.Cart.Domain/Events/CartLineAddedDomainEvent.cs
    Tooba.Cart.Domain/Events/CartLineChangedDomainEvent.cs
    Tooba.Cart.Domain/Events/CartLineRemovedDomainEvent.cs
[D] Tooba.Cart.Domain/ValueObjects
    Tooba.Cart.Domain/ValueObjects/CartAccessKind.cs
    Tooba.Cart.Domain/ValueObjects/CartConversionIntent.cs
    Tooba.Cart.Domain/ValueObjects/CartStatus.cs

[D] Tooba.Cart.Endpoints
    Tooba.Cart.Endpoints/CartEndpointModule.cs
    Tooba.Cart.Endpoints/Tooba.Cart.Endpoints.csproj
[D] Tooba.Cart.Endpoints/Errors
    Tooba.Cart.Endpoints/Errors/CartErrorCatalogContributor.cs
[D] Tooba.Cart.Endpoints/Resources
    Tooba.Cart.Endpoints/Resources/CartErrorResources.cs
    Tooba.Cart.Endpoints/Resources/CartErrors.fa.resx
    Tooba.Cart.Endpoints/Resources/CartErrors.resx
[D] Tooba.Cart.Endpoints/Storefront
    Tooba.Cart.Endpoints/Storefront/CartStorefrontEndpoints.cs

[D] Tooba.Cart.Infrastructure
    Tooba.Cart.Infrastructure/GlobalUsings.Domain.cs
    Tooba.Cart.Infrastructure/GlobalUsings.Layout.cs
    Tooba.Cart.Infrastructure/Tooba.Cart.Infrastructure.csproj
[D] Tooba.Cart.Infrastructure/DependencyInjection
    Tooba.Cart.Infrastructure/DependencyInjection/CartModule.cs
[D] Tooba.Cart.Infrastructure/Directories
    Tooba.Cart.Infrastructure/Directories/CartDirectory.cs      <-- 831 LOC (F5)
    Tooba.Cart.Infrastructure/Directories/CartLineCurrency.cs
[D] Tooba.Cart.Infrastructure/Events
    Tooba.Cart.Infrastructure/Events/CartEvents.cs
[D] Tooba.Cart.Infrastructure/Lifetime
    Tooba.Cart.Infrastructure/Lifetime/CartCommerceContextResolver.cs
    Tooba.Cart.Infrastructure/Lifetime/CartExpiryReconciler.cs
    Tooba.Cart.Infrastructure/Lifetime/CartExpiryWorker.cs
    Tooba.Cart.Infrastructure/Lifetime/CartPersistenceHoursSource.cs
    Tooba.Cart.Infrastructure/Lifetime/CatalogCartPersistenceHoursResolver.cs
[D] Tooba.Cart.Infrastructure/Messaging
    Tooba.Cart.Infrastructure/Messaging/CartOutboxRegistration.cs
[D] Tooba.Cart.Infrastructure/Persistence
    Tooba.Cart.Infrastructure/Persistence/CartDbContext.cs
[D] Tooba.Cart.Infrastructure/Persistence/Migrations
    Tooba.Cart.Infrastructure/Persistence/Migrations/20260823100150_InitialCart.cs
    Tooba.Cart.Infrastructure/Persistence/Migrations/20260823100150_InitialCart.Designer.cs
    Tooba.Cart.Infrastructure/Persistence/Migrations/20260909130200_DecimalCartQuantity.cs
    Tooba.Cart.Infrastructure/Persistence/Migrations/20260919183000_CartLineMerchandisingCampaignId.cs
    Tooba.Cart.Infrastructure/Persistence/Migrations/CartDbContextModelSnapshot.cs
[D] Tooba.Cart.Infrastructure/Security
    Tooba.Cart.Infrastructure/Security/CartCredentialHasher.cs

[D] Tooba.Cart.Tests
    Tooba.Cart.Tests/Tooba.Cart.Tests.csproj
[D] Tooba.Cart.Tests/Architecture
    Tooba.Cart.Tests/Architecture/CartArchitectureGuardTests.cs
[D] Tooba.Cart.Tests/Behavior
    Tooba.Cart.Tests/Behavior/CartMulticurrencyTests.cs
    Tooba.Cart.Tests/Behavior/CartPresentationAndErrorTests.cs
[D] Tooba.Cart.Tests/Endpoints
    Tooba.Cart.Tests/Endpoints/CartEndpointOwnershipTests.cs
[D] Tooba.Cart.Tests/Validation
    Tooba.Cart.Tests/Validation/CartEndpointValidatorCoverageGuardTests.cs
```

## Counts

| Project | Hand-written production `.cs` | EF-generated | Root `.cs` | `.resx` |
| --- | --- | --- | --- | --- |
| `Tooba.Cart.Contracts` | 3 | 0 | 0 | 0 |
| `Tooba.Cart.Domain` | 11 | 0 | 0 | 0 |
| `Tooba.Cart.Application` | 19 | 0 | 2 (`GlobalUsings.*`, allowlisted) | 0 |
| `Tooba.Cart.Infrastructure` | 11 | 5 | 2 (`GlobalUsings.*`, allowlisted) | 0 |
| `Tooba.Cart.Endpoints` | 4 | 0 | 1 (`CartEndpointModule.cs`, allowlisted) | 2 |
| **Total** | **48** | **5** | **5** | **2** |

Hand-written production `.cs` excluding the 5 root `GlobalUsings.*` / module-entry files and the
comment-only tombstone = **41**.

## Structural classification

| State | Value |
| --- | --- |
| `Folder-Granularity-State` | `TECHNICAL_AXIS_FIRST` (+ 5 unjustified single-file use-case leaves) |
| `Solution-Explorer-State` | `CANONICAL` |
| `Path-Namespace-State` | `EXACT` |
| `Physical-Copy-State` | `STALE_COPY` (`Application/Models/CartPage.cs` tombstone) |
| `Root-Allowlist-State` | `ENFORCED` |
| `File-Cohesion-State` | `OVERSIZED_ONLY` (`CartDirectory.cs` 831 LOC) + 1 multi-responsibility Contracts bundle |
