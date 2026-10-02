# tests-guards — TB-TMAR-HOST-PERSISTENCE-AMC-001

## Existing coverage

| Concern | Location | State |
| --- | --- | --- |
| Missing reference fail-closed 503 + code | `PersistenceFoundationTests.Missing_connection_reference_fails_closed` | PRESENT |
| Tenant HTTP connection miss → ProblemDetails code | `TenantResolutionTests` | PRESENT |
| Path/namespace + 1-file allowlist + no foreign layers | `HostPersistenceAmcGuardTests` | PRESENT (vocabulary stale vs new Analyze — reconcile in this task) |
| DI registration string lock | `HostPersistenceAmcGuardTests` | PRESENT |
| No mega-DbContext | `PersistenceFoundationTests` | PRESENT |

## Gaps (non-blocking for disposition; CERT may close)

| Gap | Severity |
| --- | --- |
| Blank reference value explicit unit test | LOW |
| Blank configured connection string unit test | LOW |
| Malformed connection string → same code unit test | LOW |
| Prove raw string returned unchanged | LOW |
| Explicit singleton lifetime assertion | LOW |

## Guard impact this Analyze

Update `HostPersistenceAmcGuardTests` SoT assertions to:

- key `hostPersistenceAmc001`
- disposition `KEEP_AS_GLOBAL_HOST_PERSISTENCE_PLATFORM`
- stop `USER_REVIEW_HOST_PERSISTENCE_AMC_001`

Update `TmarDurableGuardTests` current pointers for Persistence Analyze checkpoint.

No production code change.
