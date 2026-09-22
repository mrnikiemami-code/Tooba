# recovery-start

- Task-ID: TB-TMAR-CART-GOLDEN-001-R1
- Parent-Task: TB-TMAR-CART-GOLDEN-001
- ClaimId: ceb98138-be8f-419e-a811-76a66eadfcb9
- Channel: tooba-main
- Branch: main
- Start-HEAD: 56692b7c2ac8e8145b5030fe733f40bfcac7ddee
- Defect: CartExceptionMapper used Persian/English Contains heuristics and swallowed unknown InvalidOperationException into cart.rejected
- Target: STABLE_CODES_ONLY exact-message mapping; default rethrow; handlers convert only known expected failures
- Scope: Cart module only; Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT; Tax/Pricing/frontend untouched
