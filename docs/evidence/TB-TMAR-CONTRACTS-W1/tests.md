# Tests — TB-TMAR-CONTRACTS-W1

Command:

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~TmarSourceSizeAndInfraAppTests|FullyQualifiedName~TmarFoundationTests|FullyQualifiedName~ArchitectureBoundaryTests|FullyQualifiedName~WalletCurrencyContractsTests|FullyQualifiedName~WalletFoundationTests|FullyQualifiedName~OfferFoundationTests|FullyQualifiedName~PricingFoundationTests"
```

Result: Passed! Failed: 0, Passed: 49, Total: 49

Also verified compile:

- Cart.Domain / Order.Domain / Pricing.Domain
- Offer.Domain / Offer.Application
- Payment.Infrastructure
- Host

Characterization: `WalletCurrencyContractsTests` covers normalize success/failure and facade parity with `WalletAccount.NormalizeCurrency`.
