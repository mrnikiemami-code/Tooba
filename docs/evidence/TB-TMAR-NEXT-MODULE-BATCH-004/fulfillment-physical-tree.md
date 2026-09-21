# Fulfillment physical tree

| path | namespace | responsibility |
|---|---|---|
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Models/ActivePackageMembershipSnapshot.cs` | `Tooba.Fulfillment.Application.Models` | ActivePackageMembershipSnapshot |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Models/ConsolidatedPackageMemberSnapshot.cs` | `Tooba.Fulfillment.Application.Models` | ConsolidatedPackageMemberSnapshot |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Models/ConsolidatedPackageSnapshot.cs` | `Tooba.Fulfillment.Application.Models` | ConsolidatedPackageSnapshot |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Models/FulfillmentItemSnapshot.cs` | `Tooba.Fulfillment.Application.Models` | FulfillmentItemSnapshot |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Models/FulfillmentSelectionCommand.cs` | `Tooba.Fulfillment.Application.Models` | FulfillmentSelectionCommand |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Models/FulfillmentSnapshot.cs` | `Tooba.Fulfillment.Application.Models` | FulfillmentSnapshot |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Models/ShipmentLineCommand.cs` | `Tooba.Fulfillment.Application.Models` | ShipmentLineCommand |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Models/ShipmentLineSnapshot.cs` | `Tooba.Fulfillment.Application.Models` | ShipmentLineSnapshot |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Models/ShipmentSnapshot.cs` | `Tooba.Fulfillment.Application.Models` | ShipmentSnapshot |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Ports/IFulfillmentDirectory.cs` | `Tooba.Fulfillment.Application.Ports` | IFulfillmentDirectory |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Ports/IFulfillmentInventoryGateway.cs` | `Tooba.Fulfillment.Application.Ports` | IFulfillmentInventoryGateway |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Ports/IFulfillmentUseCaseGuard.cs` | `Tooba.Fulfillment.Application.Ports` | IFulfillmentUseCaseGuard |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/ShippingMethodRegistry.cs` | `Tooba.Fulfillment.Application.Shipping` | ShippingMethodDefinition, ShippingMethodRateOptions, ShippingMethodsOptions, ShippingMethodRegistry, PostShipmentMetadata, TipaxShipmentMetadata, CourierShipmentMetadata, StoreCourierShipmentMetadata, InPersonShipmentMetadata, ShippingProviderMetadataValidator |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/ShippingServiceWriteContracts.cs` | `Tooba.Fulfillment.Application.Shipping` | IShippingServiceLanguageGate, ShippingServiceSeedLanguage, ShippingServiceTranslationWriteModel, ShippingServiceOptionTranslationWriteModel, ShippingServiceOptionWriteModel, ShippingServiceWriteModel, IShippingServiceDirectory, CreateShippingServiceCommand, UpdateShippingServiceCommand, DeactivateShippingServiceCommand, EnsureShippingCatalogSeedCommand |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/ShippingServiceWriteHandlers.cs` | `Tooba.Fulfillment.Application.Shipping` | CreateShippingServiceHandler, UpdateShippingServiceHandler, DeactivateShippingServiceHandler, EnsureShippingCatalogSeedHandler |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Events/FulfillmentCreatedIntegrationEvent.cs` | `Tooba.Fulfillment.Contracts.Events` | FulfillmentCreatedIntegrationEvent |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Events/ShipmentDispatchedIntegrationEvent.cs` | `Tooba.Fulfillment.Contracts.Events` | ShipmentDispatchedIntegrationEvent |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Returns/FulfillmentReturnContracts.cs` | `Tooba.Fulfillment.Contracts.Returns` | LineDeliverySlice, FulfillmentReturnEligibilitySnapshot, IFulfillmentReturnReader |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ConsolidatedPackage.cs` | `Tooba.Fulfillment.Domain.Aggregates` | ConsolidatedPackage |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ConsolidatedPackageMember.cs` | `Tooba.Fulfillment.Domain.Aggregates` | ConsolidatedPackageMember |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/FulfillmentItem.cs` | `Tooba.Fulfillment.Domain.Aggregates` | FulfillmentItem |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/FulfillmentUnit.cs` | `Tooba.Fulfillment.Domain.Aggregates` | FulfillmentUnit |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/Shipment.cs` | `Tooba.Fulfillment.Domain.Aggregates` | Shipment |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShipmentItem.cs` | `Tooba.Fulfillment.Domain.Aggregates` | ShipmentItem |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingService.cs` | `Tooba.Fulfillment.Domain.Aggregates` | ShippingService |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingServiceOption.cs` | `Tooba.Fulfillment.Domain.Aggregates` | ShippingServiceOption |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingServiceOptionTranslation.cs` | `Tooba.Fulfillment.Domain.Aggregates` | ShippingServiceOptionTranslation |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingServiceTranslation.cs` | `Tooba.Fulfillment.Domain.Aggregates` | ShippingServiceTranslation |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Events/FulfillmentCreatedDomainEvent.cs` | `Tooba.Fulfillment.Domain.Events` | FulfillmentCreatedDomainEvent |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Events/FulfillmentLinePackedDomainEvent.cs` | `Tooba.Fulfillment.Domain.Events` | FulfillmentLinePackedDomainEvent |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Events/FulfillmentLineUnpackedDomainEvent.cs` | `Tooba.Fulfillment.Domain.Events` | FulfillmentLineUnpackedDomainEvent |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Events/ShipmentCancelledDomainEvent.cs` | `Tooba.Fulfillment.Domain.Events` | ShipmentCancelledDomainEvent |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Events/ShipmentCreatedDomainEvent.cs` | `Tooba.Fulfillment.Domain.Events` | ShipmentCreatedDomainEvent |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Events/ShipmentDeliveredDomainEvent.cs` | `Tooba.Fulfillment.Domain.Events` | ShipmentDeliveredDomainEvent |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Events/ShipmentDispatchedDomainEvent.cs` | `Tooba.Fulfillment.Domain.Events` | ShipmentDispatchedDomainEvent |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Events/ShipmentTrackingCorrectedDomainEvent.cs` | `Tooba.Fulfillment.Domain.Events` | ShipmentTrackingCorrectedDomainEvent |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/ValueObjects/ConsolidatedPackageStatus.cs` | `Tooba.Fulfillment.Domain.ValueObjects` | ConsolidatedPackageStatus |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/ValueObjects/FulfillmentStatus.cs` | `Tooba.Fulfillment.Domain.ValueObjects` | FulfillmentStatus |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/ValueObjects/ShipmentStatus.cs` | `Tooba.Fulfillment.Domain.ValueObjects` | ShipmentStatus |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Bridges/FulfillmentReturnBridge.cs` | `Tooba.Fulfillment.Infrastructure.Bridges` | FulfillmentReturnBridge |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Bridges/FulfillmentSellerOrderCancelGate.cs` | `Tooba.Fulfillment.Infrastructure.Bridges` | FulfillmentSellerOrderCancelGate |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs` | `Tooba.Fulfillment.Infrastructure.DependencyInjection` | FulfillmentModule |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Directories/FulfillmentDirectory.cs` | `Tooba.Fulfillment.Infrastructure.Directories` | OpenFulfillmentUseCaseGuard, FulfillmentDirectory |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Gateways/FulfillmentInventoryGateway.cs` | `Tooba.Fulfillment.Infrastructure.Gateways` | FulfillmentInventoryGateway |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Handlers/FulfillmentPaymentSucceededHandler.cs` | `Tooba.Fulfillment.Infrastructure.Handlers` | FulfillmentPaymentSucceededHandler |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Messaging/FulfillmentOutboxRegistration.cs` | `Tooba.Fulfillment.Infrastructure.Messaging` | FulfillmentOutboxRegistration |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Observability/FulfillmentInstrumentation.cs` | `Tooba.Fulfillment.Infrastructure.Observability` | FulfillmentInstrumentation |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/FulfillmentDbContext.cs` | `Tooba.Fulfillment.Infrastructure.Persistence` | FulfillmentDbContext, FulfillmentDbContextFactory |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/FulfillmentPaymentInboxRecord.cs` | `Tooba.Fulfillment.Infrastructure.Persistence` | FulfillmentPaymentInboxRecord |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/ShippingServiceDirectory.cs` | `Tooba.Fulfillment.Infrastructure.Shipping` | ShippingServiceDirectory |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Architecture/FulfillmentArchitectureGuardTests.cs` | `Tooba.Fulfillment.Tests.Architecture` | FulfillmentArchitectureGuardTests |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/FulfillmentCharacterizationTests.cs` | `Tooba.Fulfillment.Tests.Behavior` | FulfillmentCharacterizationTests |

Handwritten production count: 52
