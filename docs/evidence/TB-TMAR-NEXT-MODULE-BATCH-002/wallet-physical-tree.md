# Wallet physical tree (TB-TMAR-NEXT-MODULE-BATCH-002)

Visual Studio Solution Explorer equivalent. Every handwritten production `.cs`:

| Relative path | Namespace | Responsibility |
|---|---|---|
| `src/backend/Modules/Wallet/Tooba.Wallet.Application/Models/WalletDtos.cs` | `Tooba.Wallet.Application.Models` | application model/DTO |
| `src/backend/Modules/Wallet/Tooba.Wallet.Application/Models/WalletEnumParsing.cs` | `Tooba.Wallet.Application.Models` | application model/DTO |
| `src/backend/Modules/Wallet/Tooba.Wallet.Application/Ports/IWalletDirectory.cs` | `Tooba.Wallet.Application.Ports` | application port |
| `src/backend/Modules/Wallet/Tooba.Wallet.Contracts/Dtos/WalletCurrency.cs` | `?` | contracts DTO |
| `src/backend/Modules/Wallet/Tooba.Wallet.Contracts/Payments/WalletOrderPaymentPort.cs` | `Tooba.Wallet.Contracts.Payments` | wallet order-payment contract |
| `src/backend/Modules/Wallet/Tooba.Wallet.Contracts/Refunds/WalletRefundCreditPort.cs` | `Tooba.Wallet.Contracts.Refunds` | wallet refund-credit contract |
| `src/backend/Modules/Wallet/Tooba.Wallet.Domain/Aggregates/GiftCard.cs` | `Tooba.Wallet.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Wallet/Tooba.Wallet.Domain/Aggregates/GiftCardRedemption.cs` | `Tooba.Wallet.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Wallet/Tooba.Wallet.Domain/Aggregates/WalletAccount.cs` | `Tooba.Wallet.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Wallet/Tooba.Wallet.Domain/Aggregates/WalletLedgerEntry.cs` | `Tooba.Wallet.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Wallet/Tooba.Wallet.Domain/ValueObjects/GiftCardStatus.cs` | `Tooba.Wallet.Domain.ValueObjects` | domain value object |
| `src/backend/Modules/Wallet/Tooba.Wallet.Domain/ValueObjects/LedgerDirection.cs` | `Tooba.Wallet.Domain.ValueObjects` | domain value object |
| `src/backend/Modules/Wallet/Tooba.Wallet.Domain/ValueObjects/LedgerEntryType.cs` | `Tooba.Wallet.Domain.ValueObjects` | domain value object |
| `src/backend/Modules/Wallet/Tooba.Wallet.Domain/ValueObjects/WalletAccountStatus.cs` | `Tooba.Wallet.Domain.ValueObjects` | domain value object |
| `src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Adapters/WalletDemoSnapshot.cs` | `Tooba.Wallet.Infrastructure.Adapters` | infrastructure adapter |
| `src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Adapters/WalletDevelopmentSeed.cs` | `Tooba.Wallet.Infrastructure.Adapters` | infrastructure adapter |
| `src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/DependencyInjection/WalletModule.cs` | `Tooba.Wallet.Infrastructure.DependencyInjection` | module DI composition |
| `src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Directories/WalletDirectory.cs` | `Tooba.Wallet.Infrastructure.Directories` | infrastructure directory/use-case orchestration |
| `src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Persistence/WalletDbContext.cs` | `Tooba.Wallet.Infrastructure.Persistence` | EF persistence |

Root production `.cs` dump: **0**. Empty ceremonial Endpoints/Contracts projects: **none** (Payment Contracts/Endpoints N/A).
