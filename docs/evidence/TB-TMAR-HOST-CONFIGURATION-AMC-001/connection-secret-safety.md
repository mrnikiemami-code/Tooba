# connection-secret-safety — TB-TMAR-HOST-CONFIGURATION-AMC-001

| Concern | State |
| --- | --- |
| ConnectionReferences dictionary | OrdinalIgnoreCase; values are secrets |
| Startup validation error text | May include **reference name**; never connection string value |
| BuildRegistry throws | Operator metadata (tenant id/host/edition) only |
| Logging of connection strings in Configuration folder | ZERO (`ILogger` absent) |
| Legacy `PostgreSqlOptions.ConnectionString` | Present; **zero production consumers** outside options type — `DEAD_COMPATIBILITY_RESIDUE` / ignored by Persistence resolver |
| Syntax validation at startup | ABSENT (Persistence CERT defensive) |

**Sensitive-Data-State for Configuration Analyze surface: ZERO connection-string leakage in validation/errors.**
