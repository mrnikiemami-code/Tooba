# Tooba — SpiceDB Authorization Foundation

Status:

```text
IN_PROGRESS — TB-P02-T002 awaiting Architect ACCEPT
```

Task:

```text
TB-P02-T002
```

```text
Authentication != Authorization
Role columns are not Tooba's final authorization model.
UserId is principal identity, not a permission model.
```

## Adapter boundary

Application/use-case code talks only to Tooba types in `Tooba.BuildingBlocks` (`IAuthorizationService`, `IAuthorizationTupleWriter`, `IAuthorizationGuard`, subject/resource/check/decision).

SpiceDB SDK types are not referenced from Domain, Application, or ModuleContracts. Host adapters:

- `InMemoryAuthorizationAdapter` — semantic double of the foundation schema for tests/dev (`Mode=InMemory`, forbidden in Production).
- `FailClosedAuthorizationAdapter` — `Mode=Disabled`; every check is `Unavailable`, never ALLOW.
- `SpiceDbAuthorizationAdapter` — `Mode=SpiceDb`; until a live SpiceDB is wired, checks are `Unavailable` (no fake integration PASS). Endpoint/token/TLS/timeout are configured under `Tooba:Authorization:SpiceDb`. Tokens are not logged.

## Subject / resource / relation

- Subject now: `user:{UserId}` (`AuthorizationSubject.ForUser`).
- Foundation resource: `tenant:{TenantId}` (neutral, not Catalog).
- Relation `member`, permission `view`. Names are snake_case constants, not CLR namespaces.

Future organization/group/seller subjects are not implemented.

## Tenant isolation

Relationships are keyed by resource id. Membership on `tenant-a` does not ALLOW `view` on `tenant-b`. TenantId comes from existing commerce context, not Host.

Marketplace must not invent a SingleStore TenantId for checks.

## Use-case boundary

`IAuthorizationGuard.AuthorizeUseCaseAsync` is the reusable check-before-business-logic seam. No MediatR. No SpiceDB calls from Domain entities or controllers as the primary pattern.

## Tuple writes

`IAuthorizationTupleWriter` accepts typed `AuthorizationRelationshipWrite` after `AuthorizationContractValidator`. Raw SpiceDB tuple strings are not a module API. Party/Organization writes are out of scope.

## Schema versioning

`IAuthorizationSchemaProvider.SchemaVersion` is explicit (`1`). `ApplySchemaOnStartup` must be true to bootstrap; production default is false so startups do not blindly overwrite schema.

## Consistency

In-memory checks are immediately consistent for that process only. SpiceDB consistency tokens/ZedTokens are exposed on `AuthorizationCallContext.ConsistencyToken` for a later adapter; this foundation does not claim cluster-wide linearizability.

## Failure

Protected operations: `Unavailable` on config/network/SpiceDB failure. Normal DENY is not an exception. No silent allow-all.

## Observability / audit

Metrics: check count, outcome allow/deny/error, latency, resource type, permission, edition. Not UserId/TenantId/ResourceId as metric labels.

`IAuthorizationSecurityEventSink` records permission_denied and relationship_changed. Not a full audit store. Not decision caching.

## Deferred

Live SpiceDB client wiring; Party/Organization/B2B/seller/catalog/order permissions; Redis authz cache; full permission matrix.
