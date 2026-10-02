# migration-plan — TB-TMAR-HOST-PERSISTENCE-AMC-001

## Recommended: 1 wave (DIRECT CERT) ≤20 min

### W2-CERT — `TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT` (~8–12 min)

CERTIFY_ONLY (or CERT + optional focused tests):

1. Durable cert guard asserting KEEP platform + EXACT 1/1 tree + fail-closed code + secret ZERO.
2. SoT `hostPersistenceAmc001W2Cert` certification stamp.
3. Optionally add blank/malformed resolver unit tests (behavior-preserving).
4. Production change expected **ZERO** (historical migrate already EXACT).

## Rejected

| Option | Why rejected |
| --- | --- |
| W1 exception-type rewrite | `PlatformHttpException` is BuildingBlocks-documented canonical seam |
| MOVE_TO_CONFIGURATION | Resolver is adapter; Configuration already owns catalog |
| MOVE_TO_TOOBAPERSISTENCE | Would pull Host options into BuildingBlocks layer |
| Process-symmetry W1 migrate | Namespace already EXACT; no split debt |

## automaticNext

**NONE** after Analyze — Architect/user review gate (`USER_REVIEW_HOST_PERSISTENCE_AMC_001`).
