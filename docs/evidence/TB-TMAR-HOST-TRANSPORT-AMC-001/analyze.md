# Analyze — Host/Transport AMC-001

Skills: `tooba-architecture-analyze` → migrate hygiene → certify  
Scope: `src/backend/Host/Tooba.Host/Transport/` only

## Target inventory

| File | Responsibility |
| --- | --- |
| `SqlTransportOptionsMapper.cs` | Map deployment connection reference → MassTransit `SqlTransportOptions` (Npgsql host/port/db/user/password/schema); no business schema authority; no secret logging |
| `ToobaIntegrationTransportMessage.cs` | Generic durable integration envelope (EventType/Version/EventId/OccurredAt/TenantId/Edition/DeploymentId/CorrelationId/PayloadJson) |
| `ToobaIntegrationTransportConsumer.cs` | MassTransit consumer adapter: deserialize via platform serializer, reconstruct commerce context, dispatch `IIntegrationEventHandler<>` |

Production file count = **3**. Disposition = **KEEP_AS_GENERIC_HOST_TRANSPORT_INFRASTRUCTURE** (Architect: not HOST_ZERO).

## Why KEEP

1. MassTransit SQL transport wiring is Host/process platform infrastructure, not a business module.
2. Envelope + consumer are generic dispatch seams; handlers live in modules via neutral BuildingBlocks contracts.
3. Moving transport into a commerce module would illegally couple messaging substrate to that module.

## Hygiene required

- Path↔namespace was `Tooba.Host` (root dump) → must be exact `Tooba.Host.Transport`.
- Durable allowlist guard + SoT KEEP block.
- Foreign module Application/Domain/Infrastructure/Persistence = ZERO (audit BuildingBlocks / Tooba.Persistence / Host Outbox seams).
