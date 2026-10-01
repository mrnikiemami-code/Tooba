# Migrate — Host/Wallet AMC-001

## Waves

1. **Module bootstrap**: `Wallet.Infrastructure/Development/WalletDevelopmentSeedBootstrap` owns migrate + `WalletDevelopmentSeed`
2. **Host thin seam**: `Host/Composition/WalletDevelopmentSeedHost` binds store-alpha CommerceContext + AdminDevActor only
3. **Host ZERO**: deleted `Host/Wallet/`
4. **Guards**: WalletArchitectureGuardTests + HostWalletAmcGuardTests retargeted

## Behavior preserved

- Development-only seed still runs from Program when store-alpha Active
- Same customer Actor id (`aaaaaaaa-aaaa-4aaa-8aaa-000000000009`)
- Demo snapshot publish unchanged
- HTTP routes / CQRS / schema unchanged; frontend unchanged
