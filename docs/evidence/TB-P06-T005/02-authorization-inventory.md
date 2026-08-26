# 02 — Authorization mode inventory (TB-P06-T005)

## Modes (`Tooba:Authorization:Mode`)

| Mode | Adapter | Check outcome | Write outcome | Production allowed? |
|---|---|---|---|---|
| **Disabled** (dev default) | `FailClosedAuthorizationAdapter` | `Unavailable` | throws `authorization.unavailable` | Yes |
| **InMemory** | `InMemoryAuthorizationAdapter` | Allow/Deny from in-process tuple map | Touch/Delete in memory | **No** (validator rejects) |
| **SpiceDb** | `SpiceDbAuthorizationAdapter` | gRPC `CheckPermission` | gRPC `WriteRelationships` | Yes (TLS required) |

## Resolution (`AuthorizationRegistration.ResolveEngine`)

Mode resolved at DI from `IOptions<AuthorizationHostOptions>`; SDK types remain Host-only.

## Contract boundary (`Tooba.BuildingBlocks`)

| Type | Role |
|---|---|
| `IAuthorizationService` | `CanAsync` permission checks |
| `IAuthorizationTupleWriter` | relationship writes |
| `IAuthorizationGuard` | use-case boundary |
| `AuthorizationDecision` | Allow / Deny / Unavailable (never fail-open) |
| `AuthorizationCallContext` | tenant, edition, optional `ConsistencyToken` |

## Schema bootstrap

| Component | Trigger |
|---|---|
| `AuthorizationSchemaHostedService` | one-shot `StartAsync` |
| `ConfiguredAuthorizationSchemaBootstrapper` | only when `ApplySchemaOnStartup=true` and `Mode=SpiceDb` |
| `FoundationAuthorizationSchemaProvider` | schema v2 (user, tenant, party) |

## Dev actor bootstraps (unchanged)

Seller/Admin dev actor bootstraps write tuples via `IAuthorizationTupleWriter` when authorization enabled in dev config.

## Package

| Package | Version | Scope |
|---|---|---|
| `Authzed.Net` | 1.6.0 | Host only |
