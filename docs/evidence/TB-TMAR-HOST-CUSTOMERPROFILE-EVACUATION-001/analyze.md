# Analyze — TB-TMAR-HOST-CUSTOMERPROFILE-EVACUATION-001

## Before tree (`src/backend/Host/Tooba.Host/CustomerProfile/`)
| File | Notes |
| --- | --- |
| `CustomerProfileDevelopmentSeed.cs` | Only production file; Development-only seed |

Production file count before: **1**

## Classification — CustomerProfileDevelopmentSeed
| Aspect | Finding |
| --- | --- |
| Responsibility | Idempotent Development demo profile for storefront guest actor |
| Dependencies | `CustomerProfileDbContext`, Domain `CustomerProfile.Create`, Order.Application `StorefrontGuestActorId` (illegal) |
| Call sites | `ProductWorkspaceDevelopmentBootstrap` ×2; `CustomerProfileFoundationTests.Development_seed_is_idempotent` |
| Environment gating | Invoked only from Development workspace bootstrap (not Production path) |
| Persistence authority | CustomerProfile-owned DbContext/Domain |
| Destination | `Tooba.CustomerProfile.Infrastructure.Development` |
| Risk | Low if guest actor Guid + seed values + idempotency preserved |

## Disposition
REHOME live seed → Infrastructure/Development; reconnect call sites; DELETE Host file.
