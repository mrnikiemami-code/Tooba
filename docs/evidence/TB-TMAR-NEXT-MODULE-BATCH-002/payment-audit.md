# Payment audit — TB-TMAR-NEXT-MODULE-BATCH-002

## Project references
- Domain → BuildingBlocks
- Application → Domain (+ ports/contracts)
- Infrastructure → Application, **Wallet.Contracts**, ModuleContracts, Persistence
- No Wallet.Application/Domain/Infrastructure refs — OK
- No Contracts / Endpoints / Tests projects

## Foreign coupling
- WalletPaymentGateway uses IWalletOrderPaymentPort (Contracts) — OK
- Host composers/endpoints inject PaymentDbContext for production reads/writes — **LEAK**
- ICommerceHoldPolicy implemented in Host with PaymentDbContext.MethodHoldOverrides

## Host DbContext production authority (must absorb)
| Site | Use |
|------|-----|
| CommerceHoldPolicy | MethodHoldOverrides read |
| HoldPolicySettingsEndpoints | MethodHoldOverrides read/write |
| AdminPaymentsGridQueryEngine | Payments EF grid query |
| StorefrontPendingPaymentComposer | Payments/Attempts batch by checkout |
| AdminPanelComposer | PaymentDbContext inject |
| Program.cs | composition wiring |
| *Bootstrap migrate | allowed composition |

## TypeForwardedTo
- 0

## Physical / namespace
- Root dumps: PaymentDomain.cs (1250), PaymentMethodHoldOverride.cs, PaymentContracts.cs (494), other Application root files, many Infrastructure gateways at root
- Folders present: Persistence/, Events/
- God-file Domain + PaymentDirectory (963)

## Foundation
- Domain: Guid.NewGuid() in PaymentAllocation / PaymentAttempt / PaymentProofAsset / CustomerPayment factories
- Infra: DateTimeOffset.UtcNow throughout Directory/gateways/webhook; PaymentWebhookInboxRecord Guid.NewGuid
- PaymentDirectory: catch(Exception) ignore-style on refund path; catch InvalidOperationException
- Localized Persian prose in Domain throws
- PaymentGatewayInstrumentation may wrap Activity — audit StartActivity
- No IClock/IIdGenerator injection on Directory

## HTTP / CQRS
- Webhook HTTP in Host PaymentWebhookEndpoints → IPaymentWebhookHandler
- No MediatR; Directory/gateway style
- Endpoint-State target: NOT_APPLICABLE for module Endpoints project (Host thin webhook transport)

## Cross-module SQL/FK
- None observed to Wallet schema; Order enrichment stays in Host composers (no JOIN across schemas in Payment module)
