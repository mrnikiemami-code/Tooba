# TB-P10-T004-R12 — Security / ownership

| Check | Result |
| --- | --- |
| Owner guest GET payment after commit | 200 (P) |
| paymentId alone | 400 |
| Wrong guest secret | 401 |
| Empty new Cart authorizing old Payment | 401 |
| Client-chosen EnsureOrderSupply | 401 |
| Admin confirm/reject/recover | `X-Tooba-Dev-Actor-User-Id` admin only |

Authenticated customer panel still uses backend capabilities (R8/R10). Cross-store isolation remains commerce context (unchanged).
