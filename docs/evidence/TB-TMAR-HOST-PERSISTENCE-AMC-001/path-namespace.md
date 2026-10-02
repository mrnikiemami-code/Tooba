# path-namespace — TB-TMAR-HOST-PERSISTENCE-AMC-001

## Verification

| Check | Result |
| --- | --- |
| Path | `Host/Tooba.Host/Persistence/` |
| Namespace | `Tooba.Host.Persistence` |
| Match | **EXACT** |
| Duplicate resolver under `namespace Tooba.Host` | **ZERO** |
| Foreign module usings in file | **ZERO** |

## Npgsql boundary

Direct `NpgsqlConnectionStringBuilder` usage = legitimate infrastructure syntax parser.

| Forbidden | State |
| --- | --- |
| Direct SQL | ZERO |
| DB connection open | ZERO |
| DbContext | ZERO |
| Module persistence ownership | ZERO |

## Microservice readiness

`ConnectionReference` shields callers from raw Host config lookup. Host implementation is deployment composition. Future service can supply its own `IDatabaseConnectionResolver`. Boundary is **suitable**.
