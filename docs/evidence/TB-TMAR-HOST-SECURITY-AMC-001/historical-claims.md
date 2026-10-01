# historical-claims — TB-TMAR-HOST-SECURITY-AMC-001

Reconciliation of historical Security-related SoT / evidence claims against live disk (19 files).

| Claim source | Claim | Classification | Notes |
| --- | --- | --- | --- |
| `tmar-current-state.json` → `hostSecurityAmc` | CERTIFY / KEEP_THIN_PLATFORM; **18** production files; full analyze+migrate+certify | STALE_METADATA | File count wrong; Reviews authorizer missing from count; CERT predates this mandated re-analyze |
| `tmar-current-state.json` → `hostSecurityAmcR1` | KEEP_THIN_PLATFORM_SECURITY_BOUNDARY_CERTIFIED; **18** files | STALE_METADATA | Same 18-file drift; superseded as pointer historically by PageComposition then later Admin waves |
| Storefront AMC R2 | CheckoutIdentityGate + HostCheckoutActorPolicyAdapter relocated to Host/Security/Checkout KEEP | STILL_CURRENT | Present; Contracts-only; KEEP_AS_THIN_HOST_SECURITY_ADAPTER |
| Payment Host residue closure | HostPaymentStorefrontAuthorizer thin adapter KEEP | STILL_CURRENT | Present; ctor-injected session; no service locator |
| Offer / Settlement / Story seller Host residue | thin seller authorizers KEEP | STILL_CURRENT | Present + DI |
| Seller migrations R1A+ | HostSellerPanelAccess sole ISellerPanelAccess; SellerPanelAccess helper | STILL_CURRENT | Confirmed |
| Prior evidence `migrate.md` / `certify.md` under this folder claiming PASS CERT | CERTIFIED thin platform | STALE_METADATA | Rewritten this task as NOT_EXECUTED / NOT_CERTIFIED for Analyze-only mandate |
| HostSecurityAmcGuardTests ExpectedSellerFiles | 13 seller files; total Assert.Equal(**18**) | STALE_METADATA | Live seller includes `HostReviewsSellerAuthorizer.cs` → total **19**; hygiene test already reads Reviews adapter |
| Admin Access W3-CERT SoT | HOST_ADMIN_FULLY_CERTIFIED; impl SHA `7a0d79b4…` | STILL_CURRENT | Untouched by this Analyze |

Do not treat historical KEEP_CERT strings as substitute for this Analyze. Disposition remains KEEP thin platform shape, but certification is **not** re-asserted until hygiene + guard reconcile + CERT wave.
