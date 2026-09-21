# Wallet audit — TB-TMAR-NEXT-MODULE-BATCH-002

## Project references
- Domain → BuildingBlocks, **Contracts** (remove; Domain must not depend on Contracts)
- Application → Domain, Contracts
- Infrastructure → Application, Notification.Application, ModuleContracts, Persistence
- No Endpoints / Tests projects yet

## Foreign coupling
- Payment must not reference Wallet App/Domain/Infra (Payment.Infrastructure already → Wallet.Contracts only — OK)
- Host WalletEndpoints → Wallet.Application + Infrastructure (transport OK; business mapping uses Results.Json / PlatformHttpException)
- Host WalletDevelopmentSeedHost → WalletDbContext (dev migrate/seed bootstrap — allowed)

## Host DbContext authority
- Production: WalletDbContext only in WalletDevelopmentSeedHost (bootstrap) — no grid/query authority leak
- Host.Tests construct WalletDbContext in helpers — justified

## TypeForwardedTo
- 0

## Physical / namespace
- Root dumps: WalletDomain.cs (626), WalletContracts.cs (250), WalletDirectory.cs (666), WalletModule.cs, WalletDevelopmentSeed.cs, WalletDemoSnapshot.cs
- Only Persistence/ folder under Infrastructure
- God-file multi-responsibility Domain + Directory

## Foundation
- Domain: UuidV7.New() in WalletAccount / WalletLedgerEntry / GiftCard / GiftCardRedemption factories
- Directory: DateTimeOffset.UtcNow, Guid.NewGuid; no IClock/IIdGenerator; catch(DbUpdateException) concurrency path present
- Localized Persian exception prose throughout Domain + WalletEnumParsing
- No ActivitySource/StartActivity
- No ?? new SystemUtcClock / UuidV7IdGenerator fallbacks (ctors omit clock entirely)

## HTTP / CQRS
- Owned customer/admin HTTP lives in Host WalletEndpoints calling IWalletDirectory
- No MediatR; Directory-port style (Inventory/Promotion pattern)
- Endpoint recommendation: keep Host thin transport; no ceremonial Wallet.Endpoints / MediatR unless Result+ISender conversion done fully

## Contracts ownership
- IWalletOrderPaymentPort / IWalletRefundCreditPort / WalletCurrency already in Wallet.Contracts — preserve
- Application holds IWalletDirectory + DTOs (module-internal HTTP/admin surface)
