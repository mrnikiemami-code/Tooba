# TB-P10-T004-R24-R1-R3 — Logout cart persistence

Cart `01a09b32-787c-7000-91b9-0be7d96bf8c4` owned by `01a0996c-b8d9-7000-9854-80b3f59e8d7c`.

| Moment | Badge | DB |
| --- | --- | --- |
| Before logout | ۲ | Active, 2 lines, same CustomerId |
| After logout | ۰ + ورود | still Active, 2 lines, same owner |
| Anonymous GET cart | 400 | cannot read contents |
| Re-login | ۲ | same Active cart restored |

`clearCartSession` drops only the session pointer. Host logout revokes session only. Header listens to `AUTH_CHANGED` and drops stale in-flight badge loads so the account count cannot leak to anonymous UI.

Screenshots: `r24-r1-r3-cart-before-logout.png`, `r24-r1-r3-anonymous-after-logout.png`, `r24-r1-r3-cart-after-relogin.png`.
