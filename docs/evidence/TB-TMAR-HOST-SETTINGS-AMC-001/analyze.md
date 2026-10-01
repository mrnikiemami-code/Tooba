# Analyze — Host/Settings AMC-001

## Target

`src/backend/Host/Tooba.Host/Settings/` (1 file: `SettingsFoundationDevelopmentSeed.cs`)

## True ownership

| Responsibility | Owner |
|---|---|
| Seller organization profile Development seed | Party.Infrastructure.Development |
| Guest/admin locale preference Development seed | UserPreference.Infrastructure.Development |
| Admin operator profile Development seed | OperatorProfile.Infrastructure.Development |
| Demo seller display-name / guest / admin actor bind | Host Composition (thin binder only) |
| Settings HTTP (seller/customer/operator) | Already module-owned (Party / UserPreference / OperatorProfile Endpoints) |
| Host/Settings folder | ZERO after evacuation |

## Coupling / blockers

1. Host Settings seed owned multi-module Development data (Party + UserPreference + OperatorProfile) — not a platform Settings capability.
2. Host used `PartyDbContext` directly (illegal Host→module Persistence).
3. Guest actor leaked via `Order.Application` `StorefrontCheckoutService.StorefrontGuestActorId`.
4. Host referenced foreign Application directories (`IPartyDirectory`, `IUserPreferenceDirectory`, `IOperatorProfileDirectory`) from a Host folder.

## Final disposition

`READY_TO_MIGRATE` → split seeds to owning modules; thin Host Composition binder; `HOST_ZERO`.
