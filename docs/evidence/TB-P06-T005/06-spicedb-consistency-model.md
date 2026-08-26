# 06 — Tuple write / revoke / idempotency (TB-P06-T005)

## Write path

`SpiceDbAuthorizationAdapter.WriteAsync` maps `AuthorizationRelationshipWrite.Operation`:

| Operation | gRPC | Audit event |
|---|---|---|
| Touch (default) | `RelationshipUpdate.Operation.Touch` | `relationship_changed` |
| Delete | `RelationshipUpdate.Operation.Delete` | `relationship_revoked` |

## Revoke proof

After DELETE on `tenant#member@user`, subsequent `view` check returns **Deny**.

**Test:** `SpiceDbIntegrationTests.Revoke_removes_access_and_duplicate_touch_is_idempotent`.

## Duplicate touch

Second identical Touch write is safe; permission remains Allow.

## InMemory parity

`InMemoryAuthorizationAdapter` mirrors Delete via `ConcurrentDictionary.TryRemove`.

## Contract validation

`AuthorizationContractValidator` rejects invalid resource types, relation names, and non-GUID user ids before any network call.

## Fail-closed on outage

Tuple writes throw `authorization.unavailable` when SpiceDB unreachable (no silent success).

## Not in scope

- Bulk relationship import API
- Cross-tenant tuple purge job
