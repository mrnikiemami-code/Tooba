# Physical / namespace audit — TB-TMAR-NEXT-MODULE-BATCH-002

## Wallet (before)
| Path | Namespace | Issue |
|------|-----------|-------|
| Domain/WalletDomain.cs | Tooba.Wallet.Domain | god-file enums+aggregates |
| Application/WalletContracts.cs | Tooba.Wallet.Application | DTOs+ports+parsing root dump |
| Contracts/*.cs | Tooba.Wallet.Contracts | root dump (Payments/Refunds folders missing) |
| Infrastructure/*.cs | Tooba.Wallet.Infrastructure | Directory/Module/Seed/Demo root dump |
| Infrastructure/Persistence/WalletDbContext.cs | …Persistence | OK |

## Payment (before)
| Path | Namespace | Issue |
|------|-----------|-------|
| Domain/PaymentDomain.cs | Tooba.Payment.Domain | god-file |
| Domain/PaymentMethodHoldOverride.cs | Tooba.Payment.Domain | root (→ Aggregates) |
| Application/*.cs | Tooba.Payment.Application | root dump ports/contracts |
| Infrastructure/*Gateway*.cs etc | Tooba.Payment.Infrastructure | root dump |
| Infrastructure/Persistence/* | …Persistence | OK |
| Infrastructure/Events/PaymentEvents.cs | …Events | OK |

## Target (Inventory/Offer style)
- Domain: Aggregates, ValueObjects, Events (+ Policies if needed)
- Application: Ports, Models (+ Queries for Host-absorbed reads)
- Contracts (Wallet): Payments, Refunds, Dtos/Errors as fits existing ports
- Infrastructure: Persistence, Directories, Adapters, Providers, Events, Messaging, DependencyInjection
- namespace = module.layer.folder
- No TypeForwardedTo; no empty ceremonial folders; no root production .cs except justified allowlist none
