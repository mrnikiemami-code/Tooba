# Content Disposition — TB-TMAR-HOST-CUSTOMERPROFILE-EVACUATION-001

| Host artifact | Disposition | Destination |
| --- | --- | --- |
| `CustomerProfile/CustomerProfileDevelopmentSeed.cs` | REHOMED + DELETED | `Modules/CustomerProfile/Tooba.CustomerProfile.Infrastructure/Development/CustomerProfileDevelopmentSeed.cs` |
| namespace `Tooba.Host.CustomerProfile` | REMOVED | `Tooba.CustomerProfile.Infrastructure.Development` |
| `StorefrontCheckoutService.StorefrontGuestActorId` | REPLACED | `StorefrontGuestActor.ActorId` (Order.Contracts.Fulfillment) |
| Bootstrap `using Tooba.Host.CustomerProfile` | REPOINTED | `using Tooba.CustomerProfile.Infrastructure.Development` |
| Foundation test import | REPOINTED | module Infrastructure.Development + Contracts guest actor |
| Host write baseline entry | REMOVED | n/a |

## Call-site inventory (before → after)
| Site | Before | After |
| --- | --- | --- |
| ProductWorkspaceDevelopmentBootstrap L162 | Host seed ApplyAsync(provider) | module seed ApplyAsync(provider) |
| ProductWorkspaceDevelopmentBootstrap L288 | Host seed ApplyAsync(provider, cancellation) | module seed ApplyAsync(provider, cancellation) |
| CustomerProfileFoundationTests | Host namespace | Infrastructure.Development |

## Preserved semantics
- Idempotency: skip if any profile for guest actor exists
- Seed display/first/last/birth/bio/timestamp `2026-08-25T16:00:00Z`
- SaveChanges after insert
- Call count / ordering / CancellationToken defaults unchanged
