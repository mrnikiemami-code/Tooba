# Inventory physical tree — TB-TMAR-NEXT-MODULE-BATCH-001-R1

Handwritten production .cs after repair: **33**

- `Tooba.Inventory.Application/Checkout/CheckoutInventoryReservationAdapter.cs` → `Tooba.Inventory.Application.Checkout`
- `Tooba.Inventory.Application/Orders/OrderInventoryLifecycleAdapter.cs` → `Tooba.Inventory.Application.Orders`
- `Tooba.Inventory.Application/Orders/OrderSupplyContracts.cs` → `Tooba.Inventory.Application.Orders`
- `Tooba.Inventory.Application/Ports/InventoryDirectoryPorts.cs` → `Tooba.Inventory.Application.Ports`
- `Tooba.Inventory.Application/Returns/IInventoryReturnGateway.cs` → `Tooba.Inventory.Application.Returns`
- `Tooba.Inventory.Contracts/Availability/IInventorySchemaMigrator.cs` → `Tooba.Inventory.Contracts.Availability`
- `Tooba.Inventory.Contracts/Availability/InventoryAvailabilityContracts.cs` → `Tooba.Inventory.Contracts.Availability`
- `Tooba.Inventory.Contracts/Availability/InventoryQueryContracts.cs` → `Tooba.Inventory.Contracts.Availability`
- `Tooba.Inventory.Contracts/Checkout/CheckoutInventoryReservationContracts.cs` → `Tooba.Inventory.Contracts.Checkout`
- `Tooba.Inventory.Contracts/Errors/InventoryErrorCodes.cs` → `Tooba.Inventory.Contracts.Errors`
- `Tooba.Inventory.Contracts/Orders/OrderInventoryLifecycleContracts.cs` → `Tooba.Inventory.Contracts.Orders`
- `Tooba.Inventory.Contracts/Seller/SellerOfferInventoryContracts.cs` → `Tooba.Inventory.Contracts.Seller`
- `Tooba.Inventory.Domain/Aggregates/InventoryLocation.cs` → `Tooba.Inventory.Domain.Aggregates`
- `Tooba.Inventory.Domain/Aggregates/StockPosition.cs` → `Tooba.Inventory.Domain.Aggregates`
- `Tooba.Inventory.Domain/Aggregates/StockReservation.cs` → `Tooba.Inventory.Domain.Aggregates`
- `Tooba.Inventory.Domain/Events/StockAdjustedDomainEvent.cs` → `Tooba.Inventory.Domain.Events`
- `Tooba.Inventory.Domain/Events/StockAvailabilityChangedDomainEvent.cs` → `Tooba.Inventory.Domain.Events`
- `Tooba.Inventory.Domain/Events/StockReleasedDomainEvent.cs` → `Tooba.Inventory.Domain.Events`
- `Tooba.Inventory.Domain/Events/StockReservationConsumedDomainEvent.cs` → `Tooba.Inventory.Domain.Events`
- `Tooba.Inventory.Domain/Events/StockReservedDomainEvent.cs` → `Tooba.Inventory.Domain.Events`
- `Tooba.Inventory.Domain/ValueObjects/InventoryLocationStatus.cs` → `Tooba.Inventory.Domain.ValueObjects`
- `Tooba.Inventory.Domain/ValueObjects/StockAdjustmentKind.cs` → `Tooba.Inventory.Domain.ValueObjects`
- `Tooba.Inventory.Domain/ValueObjects/StockReservationStatus.cs` → `Tooba.Inventory.Domain.ValueObjects`
- `Tooba.Inventory.Infrastructure/Adapters/InventoryModuleMigration.cs` → `Tooba.Inventory.Infrastructure.Adapters`
- `Tooba.Inventory.Infrastructure/Adapters/InventoryQueryGateway.cs` → `Tooba.Inventory.Infrastructure.Adapters`
- `Tooba.Inventory.Infrastructure/Adapters/InventoryReturnGateway.cs` → `Tooba.Inventory.Infrastructure.Adapters`
- `Tooba.Inventory.Infrastructure/Adapters/InventorySchemaMigrator.cs` → `Tooba.Inventory.Infrastructure.Adapters`
- `Tooba.Inventory.Infrastructure/DependencyInjection/InventoryModule.cs` → `Tooba.Inventory.Infrastructure.DependencyInjection`
- `Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.cs` → `Tooba.Inventory.Infrastructure.Directories`
- `Tooba.Inventory.Infrastructure/Events/InventoryIntegrationEvents.cs` → `Tooba.Inventory.Infrastructure.Events`
- `Tooba.Inventory.Infrastructure/Messaging/InventoryOutboxRegistration.cs` → `Tooba.Inventory.Infrastructure.Messaging`
- `Tooba.Inventory.Infrastructure/Persistence/InventoryDbContext.cs` → `Tooba.Inventory.Infrastructure.Persistence`
- `Tooba.Inventory.Infrastructure/Persistence/ReturnRestockInboxRecord.cs` → `Tooba.Inventory.Infrastructure.Persistence`
