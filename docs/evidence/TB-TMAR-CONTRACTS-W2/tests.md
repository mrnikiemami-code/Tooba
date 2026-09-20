# Tests — TB-TMAR-CONTRACTS-W2

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~TmarSourceSizeAndInfraAppTests|FullyQualifiedName~TmarFoundationTests|FullyQualifiedName~ArchitectureBoundaryTests|FullyQualifiedName~WalletOrderPaymentPortContractsTests|FullyQualifiedName~WalletCurrencyContractsTests|FullyQualifiedName~OfferFoundationTests|FullyQualifiedName~PricingFoundationTests|FullyQualifiedName~WalletCheckoutRefundTests"
```

Result: Passed (48) after Pricing assert update for Offer.Contracts.

Characterization: WalletOrderPaymentPortContractsTests + WalletCurrencyContractsTests.
