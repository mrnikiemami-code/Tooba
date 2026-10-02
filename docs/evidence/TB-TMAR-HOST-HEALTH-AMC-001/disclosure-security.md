# disclosure-security — TB-TMAR-HOST-HEALTH-AMC-001

Unauthenticated readiness JSON may currently emit:

| Field | Classification |
|---|---|
| `status` = ready / not-ready / ok | Safe machine protocol |
| `checks.edition` = Marketplace/SingleStore | Operational label — acceptable |
| `checks.postgresql` = configured | Safe |
| `checks.postgresql` = `missing-reference:{reference}` | **CONFIG_INTERNAL_DISCLOSURE** — connection reference names / topology hints |
| `checks.authorization` = disabled/inmemory/spicedb-* | Safe non-secret labels (Contracts) |
| `checks.messaging` = disabled/unhealthy/Healthy/... | Safe protocol |
| `checks.messaging-schema` = Schema name | **CONFIG_INTERNAL_DISCLOSURE** |
| `checks.messaging-transport` | Safe operational |

No connection strings, tokens, or credentials observed.

### Raw Results.Json

**INTENTIONAL_OPERATIONAL_EXCEPTION** to ApiResponseFactory — health/readiness machine contracts, not business API envelopes. Keep unless Architect requires otherwise.

### Machine vs user-facing text

Literals like `ok`, `ready`, `not-ready`, `configured`, `unhealthy` are **operational protocol/status values** — not localized user-facing presentation. Hard-coded USER-FACING runtime text: **ZERO**.
