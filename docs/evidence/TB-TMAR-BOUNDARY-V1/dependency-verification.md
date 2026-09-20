# TB-TMAR-BOUNDARY-V1 Dependency Verification

Machine graph: `dependency-graph.json` (all module/Host/shared ProjectReferences classified).

## Reported claims

### 1) Cart.Domain → Offer.Domain — **CONFIRMED**
- csproj: `src/backend/Modules/Cart/Tooba.Cart.Domain/Tooba.Cart.Domain.csproj`
- ProjectReference: `..\..\Offer\Tooba.Offer.Domain\Tooba.Offer.Domain.csproj`
- Source: `using Tooba.Offer.Domain;` in `CartDomain.cs`
- Type usage: **no Offer.Domain public type symbols used** (Guid OfferId only; local)
- Impact: structural Domain→foreign Domain edge; currently dead reference; blocks clean Cart microservice cut until removed

### 2) Order.Domain → Offer.Domain — **CONFIRMED**
- csproj: `src/backend/Modules/Order/Tooba.Order.Domain/Tooba.Order.Domain.csproj`
- ProjectReference: `..\..\Offer\Tooba.Offer.Domain\Tooba.Offer.Domain.csproj`
- Source: `using Tooba.Offer.Domain;` in `OrderDomain.cs`
- Type usage: **no Offer.Domain type symbols used**
- Impact: same as Cart — dead structural leak

### 3) Pricing.Domain → Offer.Domain — **CONFIRMED**
- csproj: `src/backend/Modules/Pricing/Tooba.Pricing.Domain/Tooba.Pricing.Domain.csproj`
- ProjectReference: `..\..\Offer\Tooba.Offer.Domain\Tooba.Offer.Domain.csproj`
- Source: `using Tooba.Offer.Domain;` in `PricingDomain.cs` (docs mention Offer conceptually; defines local `Money`/`CurrencyCode`)
- Type usage: **no Offer.Domain type symbols used**
- Impact: dead structural leak

### 4) Payment.Infrastructure → Wallet.Domain — **CONFIRMED**
- csproj: `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Tooba.Payment.Infrastructure.csproj`
- ProjectReference: `..\..\Wallet\Tooba.Wallet.Domain\Tooba.Wallet.Domain.csproj` (also references Wallet.Application)
- Source: `WalletPaymentGateway.cs` uses `WalletAccount.NormalizeCurrency(...)` from Wallet.Domain
- Impact: Infrastructure→foreign Domain; wallet debit gateway coupled to Wallet entity helper — extraction requires Contracts/ACL

## Order.Application → foreign Application — **CONFIRMED** (hub)
Exact ProjectReferences:
- Tooba.Cart.Application
- Tooba.Offer.Application
- Tooba.Pricing.Application
- Tooba.Inventory.Application
- Tooba.Tax.Application
- Tooba.Promotion.Application

(Already baselined in `tmar-app-to-app-edges.json`; freeze exists.)

## Classification counts (Modules + Host + Shared)
See `dependency-graph.json` edge classifications. Critical foreign categories found:
- Domain → foreign Domain: 3 (claims above)
- Infrastructure → foreign Domain: 1 (Payment→Wallet)
- Application → foreign Application: 16 (includes Order hub + Cart/Inventory/Pricing/Promotion chains)
