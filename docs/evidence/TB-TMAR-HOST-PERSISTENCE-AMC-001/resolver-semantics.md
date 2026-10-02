# resolver-semantics — TB-TMAR-HOST-PERSISTENCE-AMC-001

## Lookup algorithm (`DatabaseConnectionResolver.Resolve`)

1. Reject if `reference.Value` is null/blank (`IsNullOrWhiteSpace`).
2. `TryGetValue` on `PostgreSQL.ConnectionReferences` (dictionary comparer = `OrdinalIgnoreCase`).
3. Reject if missing key or configured value blank.
4. Parse with `new NpgsqlConnectionStringBuilder(connectionString)` — syntax only.
5. On `ArgumentException`, fail-closed same as missing.
6. Return **raw** configured connection string (builder used for validation only; no normalization returned).

## Reference lookup state

| Case | Behavior |
| --- | --- |
| null/blank reference | 503 `platform.connection.unconfigured` |
| missing dictionary key | same |
| blank configured value | same |
| case of keys | case-insensitive dictionary |
| duplicate config keys | last-wins via options binding / dictionary semantics (Configuration concern) |
| env var lookup inside resolver | **ZERO** |

## Connection string parse state

| Concern | State |
| --- | --- |
| Classification | **CONFIG_SYNTAX_VALIDATION** (not CONNECTIVITY) |
| Network / DB open | NONE |
| Mutation of options | NONE |
| Unknown Npgsql keywords | accepted by builder (Npgsql behavior); not interpreted by Host |
| Normalization returned | NO — raw string returned |

## Raw return / secret boundary

- Interface contract returns `string` for trusted internal consumers.
- Consumers (`ToobaNpgsql`, MassTransit mapper, Outbox, MigrationRunner) treat as infrastructure secret.
- No public API response surfaces the string.
- No caching outside options snapshot.
- No logging of connection string in resolver (no `ILogger`).

## Unknown exception policy

- Catch only `ArgumentException` from builder.
- Other exceptions propagate (no broad catch).
- Parser exception details discarded — intentional non-leak.

## Thread safety / immutability

- Singleton holds frozen `_options` snapshot.
- Dictionary is mutable in theory but treated startup-frozen; no reload path.
