# configuration-boundary — TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT

| Owner | Responsibility |
| --- | --- |
| Configuration | Catalog (`ConnectionReferences`) + edition startup fail-fast via `PlatformOptionsValidator` |
| Persistence | Reference → connection-string resolve adapter only |
| Health | Separate readiness probing (not Persistence authority) |

Startup validator covers Production edition-required reference presence/non-empty. Resolver remains defensive for syntax failures, non-Production, and ad-hoc references.

Legacy `PostgreSQL.ConnectionString` = `LEGACY_CONFIGURATION_COMPATIBILITY_RESIDUE_OUTSIDE_PERSISTENCE_CERT_SCOPE` (ignored by resolver; Configuration cleanup deferred).
