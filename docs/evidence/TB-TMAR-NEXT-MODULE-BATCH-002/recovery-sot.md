# recovery-sot — TB-TMAR-NEXT-MODULE-BATCH-002

## Scope
Wallet + Payment golden Modular-Monolith recovery only.
Tax/Pricing untouched. Checkout PM / Cart / Returns / Order beyond compile-only consumer updates untouched.

## Physical / namespace
- Wallet: Domain Aggregates/ValueObjects; Application Ports/Models; Contracts Payments/Refunds/Dtos; Infrastructure Persistence/Directories/Adapters/DependencyInjection (+Migrations)
- Payment: Domain Aggregates/ValueObjects/Events; Application Ports/Models; Infrastructure Persistence/Directories/Adapters/Providers/Events/Messaging/DependencyInjection
- Root production `.cs` dump = 0; TypeForwardedTo = 0; no ceremonial Payment.Contracts/Endpoints projects
- Evidence: `wallet-physical-tree.md`, `payment-physical-tree.md`

## Foundation
- IClock / IIdGenerator required on WalletDirectory, PaymentDirectory, gateways; WalletPaymentGateway uses IModuleCallTracer.Begin
- No DateTimeOffset.UtcNow / Guid.NewGuid / UuidV7.New / ?? SystemUtcClock / ?? UuidV7 in module production sources
- Domain factories take explicit ids/now from callers
- Localized exception prose scrubbed to stable `wallet.*` / `payment.*` codes
- Silent catch / catch(Exception) ignore = 0 in module production sources

## Cross-module
- Payment → Wallet only via `Tooba.Wallet.Contracts` (project refs + source usings)
- Host production authority for PaymentDbContext absorbed via `IPaymentQueryDirectory` / `IPaymentHoldSettingsDirectory`
- Host retains migrate/seed bootstrap allowlist only (`MarketplaceDevelopmentBootstrap`, `ProductWorkspaceDevelopmentBootstrap`, `WalletDevelopmentSeedHost`)

## CQRS / Endpoints
- Wallet-Endpoint-State: NOT_APPLICABLE (Host owns Wallet HTTP; Directory-style)
- Payment-Endpoint-State: NOT_APPLICABLE (Host owns Payment HTTP; Directory-style)
- MediatR 12.5.0: not introduced (no owned HTTP use-case conversion warranted)

## Guards / validation
- `Tooba.Wallet.Tests` architecture + behavior
- `Tooba.Payment.Tests` architecture + behavior
- Focused Host financial seam tests
- `dotnet build src/backend/Tooba.slnx` 0 errors

## States
- Wallet-State: COMPLETE_REFERENCE_PATTERN
- Wallet-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
- Payment-State: COMPLETE_REFERENCE_PATTERN
- Payment-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
- Batch-State: COMPLETE
- Module-Recovery-State: NEXT_REFERENCE_BATCH_002_COMPLETE
- Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-003
