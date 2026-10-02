# migration-plan — TB-TMAR-HOST-CONFIGURATION-AMC-001

## Recommended: 2 waves (≤20 min each)

### W1 — `TB-TMAR-HOST-CONFIGURATION-AMC-001-W1` (~12–15 min)

MIGRATE structural only:

1. Namespace → `Tooba.Host.Configuration`.
2. Split to 9 one-type-per-file.
3. Fix consumer usings (Host, Host.Tests, MigrationRunner + tests).
4. Add `HostConfigurationAmcW1GuardTests` (tree/namespace/cohesion).
5. No Offer enum relocate; no TrustedProxies Program rewrite; no legacy ConnectionString deletion.

### W2-CERT — `TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT` (~8–10 min)

CERTIFY_ONLY: durable cert guard + evidence; production change ZERO if W1 clean.

## Rejected

| Option | Why |
| --- | --- |
| Direct CERT | Blocked by path↔namespace VIOLATION + MUST_SPLIT |
| Separate Offer-enum move wave | Not required for Configuration KEEP; Architect may schedule later |
| 3-wave mutability rewrite | Dictionary cast-mutate debt non-blocking IMMUTABLE_ENOUGH |

## automaticNext

**NONE** after Analyze — `USER_REVIEW_HOST_CONFIGURATION_AMC_001`.
