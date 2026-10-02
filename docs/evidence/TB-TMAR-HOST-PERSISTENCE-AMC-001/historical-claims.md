# historical-claims — TB-TMAR-HOST-PERSISTENCE-AMC-001

## Prior artifacts

| Artifact | Classification |
| --- | --- |
| `docs/evidence/TB-TMAR-HOST-PERSISTENCE-AMC-001/migrate.md` | **HISTORICAL** — namespace hygiene already applied |
| `docs/evidence/TB-TMAR-HOST-PERSISTENCE-AMC-001/certify.md` | **HISTORICAL** — claimed KEEP + CERT |
| Commit `f791c86a` Persistence CERT | **HISTORICAL** — production namespace EXACT remains |
| Prior SoT key `hostPersistenceAmc` | **HISTORICAL / SUPERSEDED** — later Host AMC waves dropped the key from live `tmar-current-state.json` |
| Master recovery / bootstrap “Persistence KEEP_PLATFORM” lines | **FOUNDATION_ONLY / HISTORICAL lineage** |
| `HostPersistenceAmcGuardTests` expecting `hostPersistenceAmc` + `KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE` + `USER_REVIEW_HOST_PERSISTENCE_AMC_001_KEEP_PLATFORM` | **STALE vs live SoT** (guard debt) |
| Architecture docs `30-tenant-edition-database-foundation` / `31-postgresql-persistence-foundation` | **FOUNDATION_ONLY** (behavior authority, not folder CERT) |

## Reconciliation

| Claim | Live state |
| --- | --- |
| Folder CERTIFIED on current SoT pointer | **NOT_FOLDER_CERTIFIED** (no live `hostPersistenceAmc*` CERT block before this Analyze) |
| Production path↔namespace EXACT | **CURRENT** (preserved from historical migrate) |
| Disposition vocabulary | Superseded → **KEEP_AS_GLOBAL_HOST_PERSISTENCE_PLATFORM** (this Analyze) |
| Implementation SHA authority | Remains Outbox W1 `382ef10a...` (unchanged) |

This Analyze does **not** re-assert historical CERT. Folder re-enters formal CERT only via recommended W2-CERT after Architect review.
