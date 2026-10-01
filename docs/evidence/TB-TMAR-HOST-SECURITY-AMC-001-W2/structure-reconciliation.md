# structure-reconciliation — TB-TMAR-HOST-SECURITY-AMC-001-W2

## Live tree

| Scope | Exact count |
| --- | --- |
| Root | 2 |
| Checkout | 2 |
| Payment | 1 |
| Seller | 14 (includes HostReviewsSellerAuthorizer.cs) |
| Whole Host/Security | **19** |

## Guard

`HostSecurityAmcGuardTests` now asserts exact folder membership (not Contains-only), includes Reviews, locks count=19, and preserves W1 SemanticException hygiene assertions.

Production `.cs` under Host/Security: **unchanged**.
