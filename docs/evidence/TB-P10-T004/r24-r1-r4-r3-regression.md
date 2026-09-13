# TB-P10-T004-R24-R1-R4 — R3 regression

| Check | Result |
| --- | --- |
| Header label | `09111111111` |
| Shared dropdown / logout | present |
| Logout hides badge | ۰ |
| DB cart after logout | Active, owner `01a0996c-b8d9-7000-9854-80b3f59e8d7c`, 2 lines (`01a09b33-819c-7000-a3b6-2479c63c653f`) |
| Re-login restore | badge ۲ |
| Shipping before COMMIT | badge ۲ |
| COMMIT → Payment | checkout `01a09b8b-3e28-7000-94ed-e37517c5cbf5`; cart Converted; badge ۰ |
| Injected commit 409 | stays on Shipping; badge ۲ |

No functional regression to reduce traffic.
