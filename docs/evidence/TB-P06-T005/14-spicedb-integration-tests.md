# 14 — SpiceDB integration tests (TB-P06-T005)

## Test infrastructure

- **Framework:** xUnit + Testcontainers
- **Image:** `authzed/spicedb:v1.56.0` (preshared key, gRPC `:50051`)
- **Collection:** `[Collection("PostgresSerial")]`
- **Skip:** `SkippableFact` when Docker unavailable

## SpiceDbIntegrationTests

| Test | Covers |
|---|---|
| `Real_spicedb_allows_member_denies_other_tenant_and_fails_closed_when_stopped` | Allow/Deny, tenant isolation, party, Unavailable on stop |
| `Revoke_removes_access_and_duplicate_touch_is_idempotent` | DELETE revoke + duplicate Touch |
| `Readiness_probe_succeeds_when_spicedb_is_up` | Probe pass |
| `Readiness_probe_fails_when_spicedb_is_stopped` | Probe fail |
| `Production_rejects_insecure_tls_and_inmemory_mode` | Production validator |

## AuthorizationFoundationTests (unit)

| Test | Covers |
|---|---|
| `User_subject_allow_deny_and_tenant_isolation` | InMemory semantic parity |
| `Unavailable_adapter_does_not_fail_open` | Fail-closed |
| `Schema_bootstrap_is_versioned_and_opt_in` | Bootstrap gate |
| `Authorization_token_is_not_logged_by_bootstrap` | Secret safety |
| `SpiceDb_adapter_does_not_claim_allow_without_a_running_server` | Unavailable on dead endpoint |
| `SpiceDb_mode_without_endpoint_fails_validation` | Options validator |
| `Domain_and_application_have_no_spicedb_sdk_and_user_has_no_role_column` | Boundary enforcement |

## Run command

```bash
dotnet test src/backend/Tooba.slnx --filter "FullyQualifiedName~SpiceDbIntegrationTests|FullyQualifiedName~AuthorizationFoundationTests"
```

## PASS criteria

Real SpiceDB integration tests require Docker; skipped gracefully when unavailable — not counted as fake PASS.
