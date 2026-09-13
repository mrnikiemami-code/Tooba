# TB-P10-T004-R24-R1-R4 — Merge transition

Frontend: `mergeStorefrontCartAfterLogin` shares one in-flight POST and records `lastMergedAuthUserId`. A second call after guest secret is cleared returns null. `resetStorefrontMergeTransition` on logout allows the next login.

Backend: `MergeAnonymousAfterLoginAsync` is already idempotent — already-authenticated same-owner cart returns without re-adding lines; empty leftover authenticated carts adopt the guest cart.

Runtime:

| Transition | Merge POSTs |
| --- | --- |
| C first login | 1 |
| H same-customer re-login | 1 |
| Q fault-prep (authenticated + new guest pointer from evaluate) | 1 |

Duplicate merge for the same login = 0. Quantity did not multiply (badge stayed ۲).
