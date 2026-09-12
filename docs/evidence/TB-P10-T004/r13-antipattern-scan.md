# TB-P10-T004-R13 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| Current Cart as committed Order auth | CLEAN — `resolveCommittedCheckoutAccess` / `resolvePaymentResultAccess` do not fall back to `readCartSession` |
| Converted retained as active | CLEAN — detach at commit; ensure/load refuse non-Active |
| Secret copied between carts | CLEAN — new POST cart issues new secret |
| Id-only auth fallback | CLEAN — Host still requires guest secret or auth session |
| cart.rejected retry/polling workaround | CLEAN |
| Duplicate committed-proof implementations | CLEAN — one map + R4 payment result proof |
| Unscoped single global proof overwriting Orders | CLEAN — checkoutId keys |
| Raw auth/cart errors in UX | CLEAN — mapped copy |
| Frontend security decisions | CLEAN — Host enforces ownership |
| writePaymentResultProof from new session cart | CLEAN — proof written from committed access only |
