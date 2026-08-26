# 12 — Integration event contract audit (TB-P06-T003-R1)

## Transport envelope (`ToobaIntegrationTransportMessage`)

- Stable string `EventType` (not assembly-qualified)
- `Version`, `EventId`, `OccurredAt`
- `TenantId`, `Edition`, `DeploymentId` (business context)
- `PayloadJson` — business DTO JSON without `$type`
- No connection strings or credentials

## Module integration events

- Defined in module `Events/*Events.cs`
- Immutable DTO shapes with `.v1` suffix convention
- No EF entities serialized

## Versioning

Contract version in event type string + `Version` integer field.
