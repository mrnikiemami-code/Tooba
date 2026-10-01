# Namespace and allowlist — Host/Transport AMC-001

## Path ↔ namespace

| File | Namespace |
| --- | --- |
| `Transport/SqlTransportOptionsMapper.cs` | `Tooba.Host.Transport` |
| `Transport/ToobaIntegrationTransportConsumer.cs` | `Tooba.Host.Transport` |
| `Transport/ToobaIntegrationTransportMessage.cs` | `Tooba.Host.Transport` |

State = **EXACT**. No aliases, shims, or duplicate compatibility namespaces.

## Allowlist (locked)

1. `SqlTransportOptionsMapper.cs`
2. `ToobaIntegrationTransportConsumer.cs`
3. `ToobaIntegrationTransportMessage.cs`

No fourth production `.cs` without future Architect authorization. Guard: `HostTransportAmcGuardTests`.

## Composition consumers

- `Messaging/MessagingRegistration.cs` → `using Tooba.Host.Transport;`
- `Messaging/MassTransitIntegrationEventPublisher.cs` → `using Tooba.Host.Transport;`
