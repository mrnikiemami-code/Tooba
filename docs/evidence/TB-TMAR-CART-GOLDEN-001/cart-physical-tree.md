# cart-physical-tree

Handwritten production Cart .cs (path | namespace | responsibility):

- src/backend/Modules/Cart/Tooba.Cart.Application/Commands/AddCartLine/AddCartLineCommand.cs | Tooba.Cart.Application.Commands.AddCartLine
- src/backend/Modules/Cart/Tooba.Cart.Application/Commands/ChangeCartLineQuantity/ChangeCartLineQuantityCommand.cs | Tooba.Cart.Application.Commands.ChangeCartLineQuantity
- src/backend/Modules/Cart/Tooba.Cart.Application/Commands/CreateGuestCart/CreateGuestCartCommand.cs | Tooba.Cart.Application.Commands.CreateGuestCart
- src/backend/Modules/Cart/Tooba.Cart.Application/Commands/MergeCartAfterLogin/MergeCartAfterLoginCommand.cs | Tooba.Cart.Application.Commands.MergeCartAfterLogin
- src/backend/Modules/Cart/Tooba.Cart.Application/Commands/RemoveCartLine/RemoveCartLineCommand.cs | Tooba.Cart.Application.Commands.RemoveCartLine
- src/backend/Modules/Cart/Tooba.Cart.Application/Conversion/CartConversionAdapter.cs | Tooba.Cart.Application.Conversion
- src/backend/Modules/Cart/Tooba.Cart.Application/Errors/CartErrorCodes.cs | Tooba.Cart.Application.Errors
- src/backend/Modules/Cart/Tooba.Cart.Application/Errors/CartExceptionMapper.cs | Tooba.Cart.Application.Errors
- src/backend/Modules/Cart/Tooba.Cart.Application/Lifetime/CartLifetimeOptions.cs | Tooba.Cart.Application.Lifetime
- src/backend/Modules/Cart/Tooba.Cart.Application/Models/CartPage.cs | Tooba.Cart.Application.Models
- src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartDirectory.cs | Tooba.Cart.Application.Ports
- src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartPersistenceHoursSource.cs | Tooba.Cart.Application.Ports
- src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartPresentationGateway.cs | Tooba.Cart.Application.Ports
- src/backend/Modules/Cart/Tooba.Cart.Application/Presentation/CartPresentationComposer.cs | Tooba.Cart.Application.Presentation
- src/backend/Modules/Cart/Tooba.Cart.Application/Queries/GetCart/GetCartQuery.cs | Tooba.Cart.Application.Queries.GetCart
- src/backend/Modules/Cart/Tooba.Cart.Application/Queries/GetCurrentCart/GetCurrentAuthenticatedCartQuery.cs | Tooba.Cart.Application.Queries.GetCurrentCart
- src/backend/Modules/Cart/Tooba.Cart.Contracts/Checkout/CartContracts.cs | Tooba.Cart.Contracts
- src/backend/Modules/Cart/Tooba.Cart.Domain/Aggregates/ShoppingCart.cs | Tooba.Cart.Domain.Aggregates
- src/backend/Modules/Cart/Tooba.Cart.Domain/Entities/CartLine.cs | Tooba.Cart.Domain.Entities
- src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartConvertedDomainEvent.cs | Tooba.Cart.Domain.Events
- src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartCreatedDomainEvent.cs | Tooba.Cart.Domain.Events
- src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartExpiredDomainEvent.cs | Tooba.Cart.Domain.Events
- src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartLineAddedDomainEvent.cs | Tooba.Cart.Domain.Events
- src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartLineChangedDomainEvent.cs | Tooba.Cart.Domain.Events
- src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartLineRemovedDomainEvent.cs | Tooba.Cart.Domain.Events
- src/backend/Modules/Cart/Tooba.Cart.Domain/ValueObjects/CartAccessKind.cs | Tooba.Cart.Domain.ValueObjects
- src/backend/Modules/Cart/Tooba.Cart.Domain/ValueObjects/CartConversionIntent.cs | Tooba.Cart.Domain.ValueObjects
- src/backend/Modules/Cart/Tooba.Cart.Domain/ValueObjects/CartStatus.cs | Tooba.Cart.Domain.ValueObjects
- src/backend/Modules/Cart/Tooba.Cart.Endpoints/CartEndpointModule.cs | Tooba.Cart.Endpoints
- src/backend/Modules/Cart/Tooba.Cart.Endpoints/Errors/CartErrorCatalogContributor.cs | Tooba.Cart.Endpoints.Errors
- src/backend/Modules/Cart/Tooba.Cart.Endpoints/Resources/CartErrorResources.cs | Tooba.Cart.Endpoints.Resources
- src/backend/Modules/Cart/Tooba.Cart.Endpoints/Storefront/CartStorefrontEndpoints.cs | Tooba.Cart.Endpoints.Storefront
- src/backend/Modules/Cart/Tooba.Cart.Infrastructure/DependencyInjection/CartModule.cs | Tooba.Cart.Infrastructure.DependencyInjection
- src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Directories/CartDirectory.cs | Tooba.Cart.Infrastructure.Directories
- src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Events/CartEvents.cs | Tooba.Cart.Infrastructure.Events
- src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Messaging/CartOutboxRegistration.cs | Tooba.Cart.Infrastructure.Messaging
- src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Persistence/CartDbContext.cs | Tooba.Cart.Infrastructure.Persistence
- src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Security/CartCredentialHasher.cs | Tooba.Cart.Infrastructure.Security
- src/backend/Modules/Cart/Tooba.Cart.Tests/Architecture/CartArchitectureGuardTests.cs | Tooba.Cart.Tests.Architecture
- src/backend/Modules/Cart/Tooba.Cart.Tests/Behavior/CartPresentationAndErrorTests.cs | Tooba.Cart.Tests.Behavior
- src/backend/Modules/Cart/Tooba.Cart.Tests/Endpoints/CartEndpointOwnershipTests.cs | Tooba.Cart.Tests.Endpoints

Cart-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
