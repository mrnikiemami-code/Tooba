# guard-impact — TB-TMAR-HOST-SECURITY-AMC-001

## Primary durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/HostSecurityAmcGuardTests.cs`

| Assert | Current usefulness | Stale? | Future impact |
| --- | --- | --- | --- |
| Folder present + root/Checkout/Payment files | Useful KEEP_THIN_PLATFORM | No | Keep on delete/move |
| `ExpectedSellerFiles` (13 names) | Useful allowlist | **YES** — missing `HostReviewsSellerAuthorizer.cs` | W2 must add Reviews file |
| `Assert.Equal(18, …Count())` | Count lock | **YES** — live **19** | W2 must update to 19 |
| Foreign module layer regex on Seller/Checkout/Payment | Useful | No | Keep |
| Checkout SemanticException + Foundation code; no message.Contains | Useful | No | Keep |
| Payment adapter Contracts port; no Payment.Application | Useful | No | Keep |
| Payment no GetRequiredService | Useful | No | Keep |
| SellerPanelAccess uses ICurrentEdition | Useful | No | Keep |
| Reviews endpoints use IReviewsSellerAuthorizer (not static SellerPanelAccess) | Useful | No | Keep |
| Path↔namespace exact | Useful | No | Keep |
| SoT contains historical `hostSecurityAmc` / R1 KEEP strings | Historical pointer lock | Partial — must not delete historical strings blindly | New analyze block may be added alongside; keep R1 strings until Architect retires guard |

## Related seller guards (do not weaken)

| Guard | Relevance |
| --- | --- |
| HostSellerAmcR1GuardTests | Asserts seller.* codes present in SellerSecurityErrorCodes |
| HostSellerAmcR3GuardTests | seller.authorization.denied ownership notes |
| SellerPanelAuthorizationTests | Behavioral panel deny/unavailable/actor codes |

## Analyze action

Document only. No guard edits in this task.
