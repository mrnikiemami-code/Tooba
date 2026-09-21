# Cart physical tree

| Path | Namespace | Folder responsibility |
|------|-----------|------------------------|
| `src/backend/Modules/Cart/Tooba.Cart.Application/Conversion/CartConversionAdapter.cs` | `Tooba.Cart.Application.Conversion` | Conversion |
| `src/backend/Modules/Cart/Tooba.Cart.Application/GlobalUsings.Domain.cs` | `?` | Tooba.Cart.Application |
| `src/backend/Modules/Cart/Tooba.Cart.Application/GlobalUsings.Layout.cs` | `?` | Tooba.Cart.Application |
| `src/backend/Modules/Cart/Tooba.Cart.Application/Lifetime/CartLifetimeOptions.cs` | `Tooba.Cart.Application.Lifetime` | Lifetime |
| `src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartDirectory.cs` | `Tooba.Cart.Application.Ports` | Ports |
| `src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartPersistenceHoursSource.cs` | `Tooba.Cart.Application.Ports` | Ports |
| `src/backend/Modules/Cart/Tooba.Cart.Contracts/Checkout/CartContracts.cs` | `Tooba.Cart.Contracts` | Checkout |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/Aggregates/ShoppingCart.cs` | `Tooba.Cart.Domain.Aggregates` | Aggregates |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/Entities/CartLine.cs` | `Tooba.Cart.Domain.Entities` | Entities |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartConvertedDomainEvent.cs` | `Tooba.Cart.Domain.Events` | Events |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartCreatedDomainEvent.cs` | `Tooba.Cart.Domain.Events` | Events |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartExpiredDomainEvent.cs` | `Tooba.Cart.Domain.Events` | Events |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartLineAddedDomainEvent.cs` | `Tooba.Cart.Domain.Events` | Events |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartLineChangedDomainEvent.cs` | `Tooba.Cart.Domain.Events` | Events |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/Events/CartLineRemovedDomainEvent.cs` | `Tooba.Cart.Domain.Events` | Events |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/ValueObjects/CartAccessKind.cs` | `Tooba.Cart.Domain.ValueObjects` | ValueObjects |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/ValueObjects/CartConversionIntent.cs` | `Tooba.Cart.Domain.ValueObjects` | ValueObjects |
| `src/backend/Modules/Cart/Tooba.Cart.Domain/ValueObjects/CartStatus.cs` | `Tooba.Cart.Domain.ValueObjects` | ValueObjects |
| `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/DependencyInjection/CartModule.cs` | `Tooba.Cart.Infrastructure.DependencyInjection` | DependencyInjection |
| `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Directories/CartDirectory.cs` | `Tooba.Cart.Infrastructure.Directories` | Directories |
| `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Events/CartEvents.cs` | `Tooba.Cart.Infrastructure.Events` | Events |
| `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/GlobalUsings.Domain.cs` | `?` | Tooba.Cart.Infrastructure |
| `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/GlobalUsings.Layout.cs` | `?` | Tooba.Cart.Infrastructure |
| `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Messaging/CartOutboxRegistration.cs` | `Tooba.Cart.Infrastructure.Messaging` | Messaging |
| `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Persistence/CartDbContext.cs` | `Tooba.Cart.Infrastructure.Persistence` | Persistence |
| `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Security/CartCredentialHasher.cs` | `Tooba.Cart.Infrastructure.Security` | Security |

Handwritten production .cs count: 26
