# 08 — SpiceDB integration

Task: TB-P06-T024  
Status: LIVE (extended foundation; no parallel auth engine)

## Schema

`src/backend/Host/Tooba.Host/authorization-foundation.zed` (v3)

```
user, tenant, party          — prior panel gates
role { relation member: user }
permission { relation granted: user | role#member; permission check = granted }
category { relation handler: user | role#member; permission handle_orders = handler }
```

Object types / relations: `AuthorizationObjectTypes` + `AuthorizationRelations` in  
`src/backend/BuildingBlocks/Tooba.BuildingBlocks/Authorization.cs`  
(`Permission`, `Role`, `Category`, `Granted`, `Handler`, `HandleOrders`, `Check`)

## Source of truth

| Layer | Role |
|-------|------|
| PostgreSQL `access_control` | Config SoT: roles, grants, assignments, ceiling, audit |
| SpiceDB / InMemory adapter | Enforcement SoT for capability + category handler checks |
| Sync | `AccessControlDirectory.SyncUserCapabilityTuplesAsync` rewrite granted + category handlers |

## Endpoint gate

`AccessControlEndpoints.EnsureCapabilityAsync` checks `permission/{permissionId}#check` before ACC mutations/views.

## Adapters

Host: `SpiceDbAuthorizationAdapter`, fail-closed, InMemory for tests — unchanged architecture; ACC writes tuples via `IAuthorizationTupleWriter`.
