# migration-plan — TB-TMAR-HOST-SECURITY-AMC-001

## Automatic next

`automaticNext = NONE`  
`workflowStop = USER_REVIEW_HOST_SECURITY_AMC_001`

Do **not** auto-start Security W1 / migrate / CERT from this Analyze.

## Recommended waves (Architect-authorized only)

### W1 — `TB-TMAR-HOST-SECURITY-AMC-001-W1` Seller error / localization hygiene (production)

- Exact files: `SellerPanelAccess.cs`, `HostPartySellerAuthorizer.cs`, `HostSupportSellerAuthorizer.cs` (optionally helper/codes if needed).
- Replace 8 `PlatformHttpException` Persian title sites with code-only / SemanticException + localization catalog lookup (Order/Foundation owned messages).
- Preserve deny/unavailable/401/400/403/503 semantics and fail-closed behavior.
- Guards: SellerPanelAuthorizationTests + HostSellerAmc* + HostSecurityAmcGuardTests.
- Time estimate: ≤20 minutes.
- Do **not** migrate business ownership out of modules.

### W2 — Guard / SoT reconcile (docs + tests)

- Exact files: `HostSecurityAmcGuardTests.cs`, SoT `hostSecurityAmc*` metadata.
- Update allowlist from 18 → **19** including `HostReviewsSellerAuthorizer.cs`.
- Refresh historical Security AMC SoT paths that claim 18 files.
- Behavior preserved: ZERO production change expected.
- Time estimate: ≤20 minutes.

### W3 — CERT keep-thin-platform

- Run certify skill against post-W1/W2 tree.
- Exact ownership outcome: KEEP_THIN_PLATFORM / CERTIFIED thin Host Security (not HOST_ZERO).
- Guards: HostSecurityAmcGuardTests green at 19.
- Preserve Admin CERT and Access lineage.
- Time estimate: ≤20 minutes.

## Explicit non-goals this Analyze

- ZERO production edits
- No Security W1 start
- No CERT claim in this task
- No Admin / AccessControl production changes
