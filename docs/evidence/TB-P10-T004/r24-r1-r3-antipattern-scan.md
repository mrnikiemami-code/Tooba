# TB-P10-T004-R24-R1-R3 — Anti-pattern scan

| Anti-pattern | Result |
| --- | --- |
| delete/convert account Cart on logout | CLEAN — Host logout revokes session only |
| Cart lines in Session | CLEAN — sessionStorage is pointer only |
| show account Cart to anonymous | CLEAN — badge ۰; AUTH_CHANGED drops stale loads |
| generic header despite mobile | CLEAN — label `09111111111` |
| shipping recipient as account identity | CLEAN — Me projection uses profile + Identity mobile |
| optimistic badge zero on continue | CLEAN — detach after commit OK |
| route-based Cart clearing | CLEAN |
| duplicate header state | CLEAN — one `StorefrontAccountMenu` |
| localStorage Cart SoT | CLEAN |
| auth/cart polling | CLEAN — no setInterval on me/cart |
| timing hack as product SoT | CLEAN — runtime waits only |

Scan: CLEAN.
