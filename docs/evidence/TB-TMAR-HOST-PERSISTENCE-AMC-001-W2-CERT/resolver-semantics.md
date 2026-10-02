# resolver-semantics — TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT

| Concern | Certified |
| --- | --- |
| Source | `ToobaPlatformOptions.PostgreSQL.ConnectionReferences` only |
| Root `ConnectionString` fallback | ABSENT |
| Env / IConfiguration / secret-file lookup | ZERO |
| Case-insensitive keys | YES (`OrdinalIgnoreCase` dictionary) |
| Blank / missing / blank value | Fail-closed 503 |
| Npgsql builder | Syntax validation only |
| Open / SQL / DbContext | ZERO |
| Return | Raw configured string unchanged |
| Tenant Active/Edition policy | POLICY_NEUTRAL (resolves supplied reference only) |
