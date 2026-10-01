# stale-guard-and-historical-sot — TB-TMAR-HOST-SECURITY-AMC-001

## Guard drift

| Artifact | Claim | Live truth |
| --- | --- | --- |
| HostSecurityAmcGuardTests (Host_security_folder) | Expected **18** Security production files | Actual **19** |
| Missing allowlist entry | — | `Seller/HostReviewsSellerAuthorizer.cs` |

Impact: Security AMC guard currently **fails** against live tree. Analyze documents only — no test/production fix in this task.

## Historical SoT / evidence (STALE vs this Analyze)

Prior Security AMC materials that still say 18 files / omit Reviews authorizer are **STALE** relative to disk after Reviews seller edge landed.

Authoritative analyze for current tree: this evidence pack under `docs/evidence/TB-TMAR-HOST-SECURITY-AMC-001/`.

## Reviews authorizer status

- Present on disk
- DI-registered (`IReviewsSellerAuthorizer` → `HostReviewsSellerAuthorizer`)
- Disposition: KEEP_AS_THIN_HOST_SECURITY_ADAPTER
- Must enter W2 guard allowlist before CERT
