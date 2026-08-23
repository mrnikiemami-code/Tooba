# Tooba — SpiceDB Authorization Foundation

Status:

```text
IN_PROGRESS — TB-P02-T002 REPAIR awaiting Architect ACCEPT
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

`Authzed.Net` 1.6.0 and generated gRPC types stay in Host infrastructure (`SpiceDbAuthorizationAdapter`). They are not referenced from Domain, Application, or ModuleContracts.

Host adapters:

- `InMemoryAuthorizationAdapter` — semantic double of the foundation schema for tests/dev (`Mode=InMemory`, forbidden in Production). Not a production fallback when SpiceDB is down.
- `FailClosedAuthorizationAdapter` — `Mode=Disabled`; every check is `Unavailable`, never ALLOW.
- `SpiceDbAuthorizationAdapter` — `Mode=SpiceDb`; real gRPC client to SpiceDB via Authzed.Net 1.6.0 (`PermissionsService` + `SchemaService`).

## Client, transport, configuration

Locked client: `Authzed.Net` 1.6.0.

Configuration under `Tooba:Authorization` / `Tooba:Authorization:SpiceDb`:

- `Mode`: `Disabled` | `InMemory` | `SpiceDb`
- `Endpoint` (required when Mode=SpiceDb)
- `Token` (preshared / bearer; never committed as a real secret; never logged)
- `UseTls` (default true; local/test may disable; production must not silently skip TLS verification)
- `TimeoutSeconds`
- `ApplySchemaOnStartup` (opt-in; production default false so every startup does not overwrite schema)

Bearer metadata is `Authorization: Bearer {token}`. TLS uses channel SSL credentials. Cleartext HTTP/2 is only for non-TLS local/test with insecure call credentials.

## Check mapping

Real `CheckPermission`:

- SpiceDB `HAS_PERMISSION` → `AuthorizationDecision.Allow`
- SpiceDB `NO_PERMISSION` → `AuthorizationDecision.Deny`
- transport/server/gRPC failure → `AuthorizationDecision.Unavailable`

No fail-open. Transport failure is not rewritten to DENY in a way that hides unavailability. InMemory is not used when Mode=SpiceDb.

## Relationship writes

`IAuthorizationTupleWriter` sends typed `AuthorizationRelationshipWrite` after `AuthorizationContractValidator`. The adapter maps that to SpiceDB `WriteRelationships` (`TOUCH`). Raw SpiceDB tuple strings are not a module API.

## Schema bootstrap

`IAuthorizationSchemaProvider.SchemaVersion` is explicit (`1`). Minimal foundation schema is user + tenant `member`/`view` only.

`AuthorizationSchemaHostedService` calls `IAuthorizationSchemaBootstrapper` on Host start. Live `WriteSchema` runs only when `Mode=SpiceDb` and `ApplySchemaOnStartup=true`. Development/test may apply explicitly; production must not blindly overwrite schema every start.

## Integration test

Isolated Testcontainers run uses image `authzed/spicedb:v1.56.0` (not `latest`). A test-only preshared key lives only in test configuration. The test starts SpiceDB, applies schema, writes a relationship, asserts ALLOW, DENY, Tenant A ≠ Tenant B, then stops the container and asserts Unavailable. Skip if Docker is unavailable; in-memory tests are not reported as SpiceDB integration PASS.

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

## Consistency

In-memory checks are immediately consistent for that process only. Live checks use SpiceDB fully-consistent reads for this foundation. ZedTokens remain available on `AuthorizationCallContext.ConsistencyToken` for later work; this foundation does not claim cluster-wide product linearizability beyond that check.

## Failure

Protected operations: `Unavailable` on config/network/SpiceDB failure. Normal DENY is not an exception. No silent allow-all.

## Observability / audit

Metrics: check count, outcome allow/deny/error, latency, resource type, permission, edition. Not UserId/TenantId/ResourceId as metric labels.

Logs record resource type, permission, outcome, edition — not token, UserId, TenantId, or ResourceId.

`IAuthorizationSecurityEventSink` records permission_denied and relationship_changed. Not a full audit store. Not decision caching.

## Deferred

Party/Organization/B2B/seller/catalog/order permissions; Redis authz cache; full permission matrix.
