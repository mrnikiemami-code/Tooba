# Returns audit — TB-TMAR-NEXT-MODULE-BATCH-004

## Before
- Infrastructure ProjectReferences: Order/Fulfillment/Payment/Inventory Application
- Root dump Domain+Application contracts; IPaymentDirectory + Payment.Domain.PaymentStatus
- UtcNow/NewGuid; Persian throw messages; no IClock on evaluator/directory

## After
- Foreign refs: Order/Fulfillment/Payment/Inventory/Wallet Contracts only (+ own Application/Contracts)
- Physical folders: Domain Aggregates/ValueObjects/Events; Application Ports/Models; Infrastructure DependencyInjection/Directories/Evaluators/Gateways/Bridges/Messaging/Observability/Persistence
- IPaymentReturnReader + string Status=="Succeeded"; IClock/IIdGenerator; returns.* codes
- Settlement consumes Returns.Contracts.Settlement (not Application)
- Module tests: eligibility allow/deny; create idempotent; approve refund PSP; wallet credit path

## Cross-module ports extracted
- Order.Contracts.Returns: IOrderReturnReader
- Payment.Contracts.Returns: IPaymentRefundGateway, IPaymentReturnReader
- Inventory.Contracts.Returns: IInventoryReturnGateway
- Returns.Contracts.Settlement: IReturnSettlementReader
