# 04 — Migration concurrency safety (TB-P06-T002)

## Mechanism

PostgreSQL session-level advisory lock via `pg_try_advisory_lock` / `pg_advisory_unlock`.

Implementation: `PostgresMigrationAdvisoryLock.cs`

## Lock scope

- One lock per **database target** (connection reference + logical database name)
- Acquired only for **`apply`** command
- `status` and `plan` do not acquire locks

## Lock key

Deterministic hash from scope key: `{ConnectionReference}:{DatabaseLogicalName}`

## Timeout

Default: **2 minutes** (`Program.cs` → `LockTimeout`)

Poll interval while waiting: 250 ms

## Failure behavior

| Condition | Exit code | Behavior |
|---|---|---|
| Lock not acquired within timeout | **2** | No migrations applied; error to stderr |
| Migration failure mid-run | **1** | Stop processing remaining modules for that target; prior applied modules remain committed |
| Usage/validation error | **3** | No DB writes |

## Limits

- Lock is per PostgreSQL database connection — correct for per-tenant DB isolation
- Does not replace EF migration history concurrency protections (EF also records applied migrations atomically)
- Operators must not bypass runner with raw `dotnet ef database update` concurrently on same DB
