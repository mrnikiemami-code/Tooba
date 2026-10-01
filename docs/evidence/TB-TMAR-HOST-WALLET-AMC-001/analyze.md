# Analyze — Host/Wallet AMC-001

## Target

`src/backend/Host/Tooba.Host/Wallet/` (1 file: `WalletDevelopmentSeedHost.cs`)

## True ownership

| Responsibility | Owner |
|---|---|
| Wallet HTTP / CQRS / Domain / schema | Wallet module (already owns Endpoints) |
| Wallet demo seed data + snapshot publish | Wallet.Infrastructure (`WalletDevelopmentSeed`) |
| Wallet migrate + seed orchestration | Wallet.Infrastructure.Development bootstrap |
| ControlPlaneRegistry tenant bind + AdminDevActor | Host composition (platform) |
| Admin authorizer adapter | Host `HostWalletAdminAuthorizer` (Admin/Access; not Wallet folder) |
| Host/Wallet folder | ZERO after evacuation |

## Coupling / blockers

1. Host Wallet folder still owns migrate+seed orchestration and references `WalletDbContext`.
2. Host depends on Host-only `ControlPlaneRegistry` + `AdminDevActorBootstrap` (platform; remain in Host composition).
3. Wallet guard historically required Host/Wallet seed file to remain.

## Final disposition

`READY_TO_MIGRATE` → evacuate Host/Wallet to module Development bootstrap; thin Host Composition caller only; `HOST_ZERO` for `Host/Wallet`.
