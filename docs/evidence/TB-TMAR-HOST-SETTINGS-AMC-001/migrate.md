# Migrate — Host/Settings AMC-001

## Waves

1. **Party seed**: `PartyOrganizationProfileDevelopmentSeed` via `IPartyDevelopmentSeedGateway` (no Host PartyDbContext)
2. **UserPreference seed**: `UserPreferenceDevelopmentSeed` (guest + optional admin ActorIds from Host binder)
3. **OperatorProfile seed**: `OperatorProfileDevelopmentSeed` (admin ActorId from Host binder)
4. **Host thin seam**: `Host/Composition/SettingsFoundationDevelopmentSeedHost` binds display name / guest / admin, delegates to modules
5. **Guest actor**: `Order.Contracts.Fulfillment.StorefrontGuestActor` (Order.Application leakage ZERO)
6. **Caller**: `DevelopmentSchemaMigrator` retargeted to Composition binder (2 call sites)
7. **Host ZERO**: deleted `Host/Settings/`
8. **Guards**: `HostSettingsAmcGuardTests` + retargeted `SettingsFoundationTests` + durable SoT

## Behavior preserved

- Development-only idempotent seeds still run from schema migrator after workspace demo / admin bootstrap
- Same demo seller display name (`فروشگاه آرمان`), support phone/email/address/description constants
- Same guest locale `fa` and operator demo profile copy
- HTTP routes / CQRS / schema unchanged; frontend unchanged
