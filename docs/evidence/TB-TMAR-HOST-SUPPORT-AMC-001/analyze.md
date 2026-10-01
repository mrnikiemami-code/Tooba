# Analyze — Host/Support AMC-001

## Target

`src/backend/Host/Tooba.Host/Support/` (1 file: `SupportDevelopmentSeedHost.cs`)

## True ownership

| Responsibility | Owner |
|---|---|
| Support HTTP / CQRS / Domain / schema | Support module (already owns Endpoints) |
| Support demo seed data + snapshot | Support.Infrastructure.Seeds |
| Support migrate + seed orchestration | Support.Infrastructure.Development bootstrap |
| ControlPlane bind + AdminDevActor + SellerDev + AccessControl bootstrap | Host Composition (platform prerequisites) |
| Admin/Seller authorizer adapters | Host Admin/Security (not Support folder) |
| Host/Support folder | ZERO after evacuation |

## Coupling / blockers

1. Host Support folder owns migrate + AccessControl/Seller prelude + Order.Application guest id leak.
2. Guest actor should use Order.Contracts `StorefrontGuestActor`.
3. SupportArchitectureGuard historically required Host/Support seed file.

## Final disposition

`READY_TO_MIGRATE` → evacuate Host/Support to module Development bootstrap; thin Host Composition binder; `HOST_ZERO`.
