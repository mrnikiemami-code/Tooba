# Migrate — Host/Support AMC-001

## Waves

1. **Module bootstrap**: `Support.Infrastructure/Development/SupportDevelopmentSeedBootstrap` owns migrate + `SupportDevelopmentSeed`
2. **Host thin seam**: `Host/Composition/SupportDevelopmentSeedHost` binds ControlPlane + AdminDev + SellerDev + AccessControl tuples
3. **Guest actor**: `Order.Contracts.StorefrontGuestActor` (Order.Application leakage ZERO)
4. **Host ZERO**: deleted `Host/Support/`
5. **Guards**: SupportArchitectureGuardTests + HostSupportAmcGuardTests retargeted

## Behavior preserved

- Development-only seed still runs from Program when store-alpha Active
- Same guest Actor id via Contracts
- Seller/Admin AccessControl prelude unchanged
- HTTP routes / CQRS / schema unchanged; frontend unchanged
