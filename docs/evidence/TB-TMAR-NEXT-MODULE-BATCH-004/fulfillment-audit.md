# Fulfillment audit — TB-TMAR-NEXT-MODULE-BATCH-004

## Before
- Infrastructure ProjectReferences: Order.Application, Inventory.Application, Payment.Application
- Root-dumped Infra/Domain/Application files; FulfillmentDirectory god-file
- Guid.NewGuid / DateTimeOffset.UtcNow / UuidV7.New in Domain+Directory
- Empty DbUpdateException catches; localized exception prose; PlatformHttpException in shipping writes

## After
- Infrastructure foreign refs: Order.Contracts, Inventory.Contracts, Payment.Contracts only
- Physical folders: Domain Aggregates/ValueObjects/Events; Application Ports/Models/Shipping; Infrastructure DependencyInjection/Directories/Gateways/Bridges/Handlers/Shipping/Messaging/Observability/Persistence
- IClock + IIdGenerator on FulfillmentDirectory; Domain Create accepts caller Guid/Func<Guid>; EnsureDispatched(now)
- Exception codes fulfillment.* / shipping_service.*; DbUpdateException rethrows with code+inner
- Module tests: architecture guards + characterization (idempotent CreateFromPaid, dispatch consume)

## Cross-module ports extracted (owners)
- Order.Contracts.Fulfillment: IOrderFulfillmentReader, cancel gate
- Inventory.Contracts.Fulfillment: IFulfillmentInventoryLifecyclePort
- Fulfillment.Contracts.Returns: IFulfillmentReturnReader (+ slices)
