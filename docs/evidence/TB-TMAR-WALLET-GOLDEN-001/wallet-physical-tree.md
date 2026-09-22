# Wallet Physical Tree

path | namespace | responsibility

src/backend/Modules/Wallet/Tooba.Wallet.Domain/Aggregates/GiftCard.cs | Tooba.Wallet.Domain.Aggregates | GiftCard
src/backend/Modules/Wallet/Tooba.Wallet.Domain/Aggregates/GiftCardRedemption.cs | Tooba.Wallet.Domain.Aggregates | GiftCardRedemption
src/backend/Modules/Wallet/Tooba.Wallet.Domain/Aggregates/WalletAccount.cs | Tooba.Wallet.Domain.Aggregates | WalletAccount
src/backend/Modules/Wallet/Tooba.Wallet.Domain/Aggregates/WalletLedgerEntry.cs | Tooba.Wallet.Domain.Aggregates | WalletLedgerEntry
src/backend/Modules/Wallet/Tooba.Wallet.Domain/ValueObjects/GiftCardStatus.cs | Tooba.Wallet.Domain.ValueObjects | GiftCardStatus
src/backend/Modules/Wallet/Tooba.Wallet.Domain/ValueObjects/LedgerDirection.cs | Tooba.Wallet.Domain.ValueObjects | LedgerDirection
src/backend/Modules/Wallet/Tooba.Wallet.Domain/ValueObjects/LedgerEntryType.cs | Tooba.Wallet.Domain.ValueObjects | LedgerEntryType
src/backend/Modules/Wallet/Tooba.Wallet.Domain/ValueObjects/WalletAccountStatus.cs | Tooba.Wallet.Domain.ValueObjects | WalletAccountStatus
src/backend/Modules/Wallet/Tooba.Wallet.Application/Commands/AdjustAdminWallet/AdjustAdminWalletCommand.cs | Tooba.Wallet.Application.Commands.AdjustAdminWallet | AdjustAdminWalletCommand
src/backend/Modules/Wallet/Tooba.Wallet.Application/Commands/IssueAdminGiftCard/IssueAdminGiftCardCommand.cs | Tooba.Wallet.Application.Commands.IssueAdminGiftCard | IssueAdminGiftCardCommand
src/backend/Modules/Wallet/Tooba.Wallet.Application/Commands/RedeemCustomerGiftCard/RedeemCustomerGiftCardCommand.cs | Tooba.Wallet.Application.Commands.RedeemCustomerGiftCard | RedeemCustomerGiftCardCommand
src/backend/Modules/Wallet/Tooba.Wallet.Application/Commands/RevokeAdminGiftCard/RevokeAdminGiftCardCommand.cs | Tooba.Wallet.Application.Commands.RevokeAdminGiftCard | RevokeAdminGiftCardCommand
src/backend/Modules/Wallet/Tooba.Wallet.Application/Errors/WalletErrorCodes.cs | Tooba.Wallet.Application.Errors | WalletErrorCodes
src/backend/Modules/Wallet/Tooba.Wallet.Application/Errors/WalletExceptionMapper.cs | Tooba.Wallet.Application.Errors | WalletExceptionMapper
src/backend/Modules/Wallet/Tooba.Wallet.Application/Models/WalletDtos.cs | Tooba.Wallet.Application.Models | WalletDtos
src/backend/Modules/Wallet/Tooba.Wallet.Application/Models/WalletEnumParsing.cs | Tooba.Wallet.Application.Models | WalletEnumParsing
src/backend/Modules/Wallet/Tooba.Wallet.Application/Ports/IWalletDemoPreviewPort.cs | Tooba.Wallet.Application.Ports | IWalletDemoPreviewPort
src/backend/Modules/Wallet/Tooba.Wallet.Application/Ports/IWalletDirectory.cs | Tooba.Wallet.Application.Ports | IWalletDirectory
src/backend/Modules/Wallet/Tooba.Wallet.Application/Queries/GetAdminGiftCard/GetAdminGiftCardQuery.cs | Tooba.Wallet.Application.Queries.GetAdminGiftCard | GetAdminGiftCardQuery
src/backend/Modules/Wallet/Tooba.Wallet.Application/Queries/GetAdminWallet/GetAdminWalletQuery.cs | Tooba.Wallet.Application.Queries.GetAdminWallet | GetAdminWalletQuery
src/backend/Modules/Wallet/Tooba.Wallet.Application/Queries/GetCustomerWalletSummary/GetCustomerWalletSummaryQuery.cs | Tooba.Wallet.Application.Queries.GetCustomerWalletSummary | GetCustomerWalletSummaryQuery
src/backend/Modules/Wallet/Tooba.Wallet.Application/Queries/GetWalletDemoPreview/GetWalletDemoPreviewQuery.cs | Tooba.Wallet.Application.Queries.GetWalletDemoPreview | GetWalletDemoPreviewQuery
src/backend/Modules/Wallet/Tooba.Wallet.Application/Queries/ListAdminGiftCards/ListAdminGiftCardsQuery.cs | Tooba.Wallet.Application.Queries.ListAdminGiftCards | ListAdminGiftCardsQuery
src/backend/Modules/Wallet/Tooba.Wallet.Application/Queries/ListAdminWalletLedger/ListAdminWalletLedgerQuery.cs | Tooba.Wallet.Application.Queries.ListAdminWalletLedger | ListAdminWalletLedgerQuery
src/backend/Modules/Wallet/Tooba.Wallet.Application/Queries/ListCustomerWalletLedger/ListCustomerWalletLedgerQuery.cs | Tooba.Wallet.Application.Queries.ListCustomerWalletLedger | ListCustomerWalletLedgerQuery
src/backend/Modules/Wallet/Tooba.Wallet.Contracts/Dtos/WalletCurrency.cs |  | WalletCurrency
src/backend/Modules/Wallet/Tooba.Wallet.Contracts/Payments/WalletOrderPaymentPort.cs | Tooba.Wallet.Contracts.Payments | WalletOrderPaymentPort
src/backend/Modules/Wallet/Tooba.Wallet.Contracts/Refunds/WalletRefundCreditPort.cs | Tooba.Wallet.Contracts.Refunds | WalletRefundCreditPort
src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Adapters/WalletDemoSnapshot.cs | Tooba.Wallet.Infrastructure.Adapters | WalletDemoSnapshot
src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Adapters/WalletDevelopmentSeed.cs | Tooba.Wallet.Infrastructure.Adapters | WalletDevelopmentSeed
src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/DependencyInjection/WalletModule.cs | Tooba.Wallet.Infrastructure.DependencyInjection | WalletModule
src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Directories/WalletDirectory.cs | Tooba.Wallet.Infrastructure.Directories | WalletDirectory
src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Persistence/WalletDbContext.cs | Tooba.Wallet.Infrastructure.Persistence | WalletDbContext
src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Admin/IWalletAdminAuthorizer.cs | Tooba.Wallet.Endpoints.Admin | IWalletAdminAuthorizer
src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Admin/WalletAdminEndpoints.cs | Tooba.Wallet.Endpoints.Admin | WalletAdminEndpoints
src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Customer/IWalletCustomerAuthorizer.cs | Tooba.Wallet.Endpoints.Customer | IWalletCustomerAuthorizer
src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Customer/WalletCustomerEndpoints.cs | Tooba.Wallet.Endpoints.Customer | WalletCustomerEndpoints
src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Errors/WalletErrorCatalogContributor.cs | Tooba.Wallet.Endpoints.Errors | WalletErrorCatalogContributor
src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/WalletEndpointModule.cs | Tooba.Wallet.Endpoints | WalletEndpointModule
