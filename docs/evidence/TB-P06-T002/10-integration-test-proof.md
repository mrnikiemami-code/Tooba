# 10 — Integration test proof (TB-P06-T002)

Project: `Tooba.MigrationRunner.Tests` (Testcontainers PostgreSQL `postgres:16-alpine`)

| Test | Proves |
|---|---|
| `Apply_on_empty_database_succeeds_and_reapply_is_idempotent` | Empty DB apply succeeds; second apply has no pending |
| `Plan_does_not_write_migration_history` | Plan is no-write; pending detected |
| `Single_tenant_apply_does_not_touch_other_tenant_database` | Tenant isolation |
| `Output_does_not_contain_password_literal` | No secrets in orchestrator output |

## Validation run

```
Tooba.MigrationRunner.Tests: Passed 4, Failed 0, Skipped 0
Tooba.Host.Tests: Passed 208, Failed 0, Skipped 0
```

Script: `scripts/run-backend-validation-with-nuget-proxy.ps1`

Build: 0 warnings, 0 errors

## Docker dependency

Tests skip gracefully when Docker/Testcontainers unavailable (`SkippableFact`).

## Not covered in automated tests (documented limits)

- Concurrent dual-runner advisory lock race (mechanism implemented; manual ops verification recommended)
- Full 19-module apply duration on production-sized DB (integration test validates Catalog path + full orchestrator loop)
