# Settlement physical tree

| path | namespace | responsibility |
| --- | --- | --- |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Commands/ProcessAdminPayout/ProcessAdminPayoutCommand.cs` | `Tooba.Settlement.Application.Commands.ProcessAdminPayout` | پردازش payout (admin). |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Commands/RequestSellerPayout/RequestSellerPayoutCommand.cs` | `Tooba.Settlement.Application.Commands.RequestSellerPayout` | درخواست payout فروشنده. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Commands/RetryAdminPayout/RetryAdminPayoutCommand.cs` | `Tooba.Settlement.Application.Commands.RetryAdminPayout` | retry payout (admin). |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Errors/SettlementErrorCodes.cs` | `Tooba.Settlement.Application.Errors` | Stable semantic error codes owned by Settlement HTTP/use-case boundary. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Errors/SettlementExceptionMapper.cs` | `Tooba.Settlement.Application.Errors` | /// Maps known Settlement domain/directory stable machine codes to SemanticError. /// Exact messa... |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/GlobalUsings.Domain.cs` | `(global usings)` | project global usings |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/GlobalUsings.Layout.cs` | `(global usings)` | project global usings |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Models/SettlementAdminModels.cs` | `Tooba.Settlement.Application.Models` | ماندهٔ تسویه با نام نمایشی فروشنده برای گرید Admin. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Ports/IAdminPayoutGridQuery.cs` | `Tooba.Settlement.Application.Ports` | پرس‌وجوی DB-native صف payout Admin. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Ports/SettlementContracts.cs` | `Tooba.Settlement.Application.Ports` | /// snapshot سفارش برای تسویه. FK Order نیست. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Queries/GetSellerSettlementBalance/GetSellerSettlementBalanceQuery.cs` | `Tooba.Settlement.Application.Queries.GetSellerSettlementBalance` | مانده فروشنده. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Queries/ListAdminPayoutQueue/ListAdminPayoutQueueQuery.cs` | `Tooba.Settlement.Application.Queries.ListAdminPayoutQueue` | صف payout (admin). |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Queries/ListAdminSettlementBalances/ListAdminSettlementBalancesQuery.cs` | `Tooba.Settlement.Application.Queries.ListAdminSettlementBalances` | مانده همه فروشندگان (admin) با نام نمایشی. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Queries/ListSellerPayoutRequests/ListSellerPayoutRequestsQuery.cs` | `Tooba.Settlement.Application.Queries.ListSellerPayoutRequests` | فهرست payoutهای فروشنده. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Queries/ListSellerSettlementEntries/ListSellerSettlementEntriesQuery.cs` | `Tooba.Settlement.Application.Queries.ListSellerSettlementEntries` | سطرهای posted فروشنده. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Queries/ListSellerSettlementStatements/ListSellerSettlementStatementsQuery.cs` | `Tooba.Settlement.Application.Queries.ListSellerSettlementStatements` | صورت‌حساب‌های فروشنده. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Queries/QueryAdminPayoutGrid/AdminPayoutGridQueryPolicy.cs` | `Tooba.Settlement.Application.Queries.QueryAdminPayoutGrid` | /// Module-owned payout grid Normalize — whitelist seller/amount/status/created, /// default sort... |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Queries/QueryAdminPayoutGrid/QueryAdminPayoutGridQuery.cs` | `Tooba.Settlement.Application.Queries.QueryAdminPayoutGrid` | گرید DB-native صف payout (admin). |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/PayoutAttempt.cs` | `Tooba.Settlement.Domain.Aggregates` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/PayoutRequest.cs` | `Tooba.Settlement.Domain.Aggregates` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/SellerPayoutProfile.cs` | `Tooba.Settlement.Domain.Aggregates` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/SettlementAccount.cs` | `Tooba.Settlement.Domain.Aggregates` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/SettlementEntry.cs` | `Tooba.Settlement.Domain.Aggregates` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/SettlementStatement.cs` | `Tooba.Settlement.Domain.Aggregates` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Entities/CommissionPolicy.cs` | `Tooba.Settlement.Domain.Entities` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Events/PayoutFailedDomainEvent.cs` | `Tooba.Settlement.Domain.Events` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Events/PayoutSucceededDomainEvent.cs` | `Tooba.Settlement.Domain.Events` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Events/SettlementEntryPostedDomainEvent.cs` | `Tooba.Settlement.Domain.Events` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/ValueObjects/CommissionPolicySnapshot.cs` | `Tooba.Settlement.Domain.ValueObjects` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/ValueObjects/EntryType.cs` | `Tooba.Settlement.Domain.ValueObjects` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/ValueObjects/PayoutStatus.cs` | `Tooba.Settlement.Domain.ValueObjects` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/ValueObjects/StatementStatus.cs` | `Tooba.Settlement.Domain.ValueObjects` | /// Domain type. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Endpoints/Admin/ISettlementAdminAuthorizer.cs` | `Tooba.Settlement.Endpoints.Admin` | /// احراز Actor admin برای مسیرهای Settlement؛ پیاده‌سازی در Host. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Endpoints/Admin/SettlementAdminEndpoints.cs` | `Tooba.Settlement.Endpoints.Admin` | Thin admin Settlement HTTP routes — ApiResponseFactory only. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Endpoints/Seller/ISettlementSellerAuthorizer.cs` | `Tooba.Settlement.Endpoints.Seller` | /// احراز Actor/Seller برای مسیرهای Settlement؛ پیاده‌سازی در Host. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Endpoints/Seller/SettlementSellerEndpoints.cs` | `Tooba.Settlement.Endpoints.Seller` | Thin seller Settlement HTTP routes — ApiResponseFactory only. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Endpoints/SettlementEndpointModule.cs` | `Tooba.Settlement.Endpoints` | Thin composition for Settlement HTTP ownership. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Bridges/SettlementOrderBridge.cs` | `Tooba.Settlement.Infrastructure.Bridges` | /// پل snapshot سفارش برای Settlement از درز Order.Contracts.Returns. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Bridges/SettlementPaymentBridge.cs` | `Tooba.Settlement.Infrastructure.Bridges` | /// پل snapshot پرداخت برای Settlement از درز Payment.Contracts. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Bridges/SettlementReturnsBridge.cs` | `Tooba.Settlement.Infrastructure.Bridges` | /// پل snapshot refund برای Settlement از درز Returns.Contracts.Settlement. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/DependencyInjection/SettlementModule.cs` | `Tooba.Settlement.Infrastructure.DependencyInjection` | /// ماژول Settlement: accrual پس از Paid و payout marketplace. سفارش و پرداخت مستقیم اینجا نیستند... |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Directories/SettlementDirectory.cs` | `Tooba.Settlement.Infrastructure.Directories` | /// نگهبان باز موردکاربرد Settlement. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Errors/SettlementErrorCatalogContributor.cs` | `Tooba.Settlement.Infrastructure.Errors` | کاتالوگ صریح کدهای خطای Settlement. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Gateways/FailClosedPayoutGateway.cs` | `Tooba.Settlement.Infrastructure.Gateways` | /// Production fail-closed وقتی درگاه payout واقعی پیکربندی نشده است. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Gateways/FakePayoutGateway.cs` | `Tooba.Settlement.Infrastructure.Gateways` | /// درگاه payout آزمایشی. dev همیشه موفق می‌شود. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/GlobalUsings.Domain.cs` | `(global usings)` | project global usings |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/GlobalUsings.Layout.cs` | `(global usings)` | project global usings |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Handlers/SettlementEventHandlers.cs` | `Tooba.Settlement.Infrastructure.Handlers` | /// مصرف‌کننده payment.succeeded.v1 برای accrual idempotent. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Messaging/SettlementOutboxRegistration.cs` | `Tooba.Settlement.Infrastructure.Messaging` | /// ثبت Outbox ماژول Settlement. سفارش/پرداخت را مستقیم به‌روز نمی‌کند؛ ترجمه فقط Integration پای... |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Observability/SettlementInstrumentation.cs` | `Tooba.Settlement.Infrastructure.Observability` | /// تله‌متری سبک Settlement. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Persistence/SettlementDbContext.cs` | `Tooba.Settlement.Infrastructure.Persistence` | /// DbContext مالک schema <c>settlement</c>. سفارش و پرداخت را نگه نمی‌دارد. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Persistence/SettlementInboxRecords.cs` | `Tooba.Settlement.Infrastructure.Persistence` | /// dedup inbox برای payment.succeeded در schema settlement. /// |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Queries/AdminPayoutGridQueryEngine.cs` | `Tooba.Settlement.Infrastructure.Queries` | پرس‌وجوی DB-native صف payout Admin (Pending|Failed) با batch نام فروشنده از Party.Contracts. |
| `src/backend/Modules/Settlement/Tooba.Settlement.Tests/Architecture/SettlementArchitectureGuardTests.cs` | `Tooba.Settlement.Tests.Architecture` | handwritten production type |
| `src/backend/Modules/Settlement/Tooba.Settlement.Tests/Behavior/SettlementErrorAndGridTests.cs` | `Tooba.Settlement.Tests.Behavior` | handwritten production type |
| `src/backend/Modules/Settlement/Tooba.Settlement.Tests/Endpoints/SettlementEndpointOwnershipTests.cs` | `Tooba.Settlement.Tests.Endpoints` | handwritten production type |

## Host auth adapters (global plumbing only)

| path | namespace | responsibility |
| --- | --- | --- |
| `src/backend/Host/Tooba.Host/Seller/HostSettlementSellerAuthorizer.cs` | `Tooba.Host.Seller` | اتصال Host به درز احراز Settlement seller Endpoints. |
| `src/backend/Host/Tooba.Host/Admin/HostSettlementAdminAuthorizer.cs` | `Tooba.Host.Admin` | /// اتصال Host به درز احراز Settlement admin Endpoints. /// Single-Store از tenant موجود؛ Marketplace در Development از tenant پلتفرم synthetic. /// |
