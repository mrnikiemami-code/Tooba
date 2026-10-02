# disclosure-sanitization — TB-TMAR-HOST-HEALTH-AMC-001-W1

| Public field | Before | After |
|---|---|---|
| `checks.postgresql` missing | `missing-reference:{actual-ref}` | `missing-reference` |
| `checks.messaging-schema` | emitted Schema name | REMOVED |
| `checks.messaging` | preserved | preserved |
| `checks.messaging-transport` | `postgresql-sql` / `n/a` | preserved |

HTTP 503 on not-ready preserved. No connection strings, credentials, tokens, stack traces, or exception.Message in public readiness JSON.
