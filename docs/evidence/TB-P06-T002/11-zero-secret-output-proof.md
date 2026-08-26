# 11 — Zero secret output proof (TB-P06-T002)

## Controls

| Control | Implementation |
|---|---|
| No connection string echo | Output uses `connectionReference` key + logical `database` name only |
| No password in logs | Structured logs exclude resolved connection strings |
| No DropDatabase / EnsureDeleted | Runner uses EF `GetPendingMigrationsAsync` + `MigrateAsync` only |
| No destructive reset CLI | No drop/reset commands exposed |

## Test evidence

`Output_does_not_contain_password_literal` — status orchestration output checked for:

- `dev-placeholder` (test container password) — **absent**
- `Password=` — **absent**

## Honest limits

EF does not expose reliable destructive-migration classification without custom parsing. Runner does not invent a SQL parser; ops must review migration files in module Infrastructure assemblies before apply.

## Safety defaults

- Single-Store without tenant flags → exit 3 (usage error)
- `--all-tenants` and explicit tenant flags mutually exclusive
