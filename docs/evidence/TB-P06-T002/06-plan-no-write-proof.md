# 06 — Plan no-write proof (TB-P06-T002)

## Test

`MigrationRunnerIntegrationTests.Plan_does_not_write_migration_history`

## Procedure

1. Start empty PostgreSQL (Testcontainers `postgres:16-alpine`)
2. Count rows in `catalog.__ef_migrations_history` (0 if table absent)
3. Run `MigrationRunnerCommand.Plan` against empty DB
4. Re-count history rows

## Result

| Step | catalog history count |
|---|---|
| Before plan | 0 |
| After plan | 0 |
| Pending detected | Yes (`Catalog` module reports pending migrations) |

**Conclusion:** `plan` inspects pending migrations via EF `GetPendingMigrationsAsync` without calling `MigrateAsync`.
