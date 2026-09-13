# TB-P10-T004-R24-R1-R3 — Runtime

Host `:5088` + FE `:3000`. Script `_r24-r1-r3-runtime.mjs`. Raw `_r24-r1-r3-runtime-raw.json`. ALL_RUNTIME_PASS.

| Step | Result |
| --- | --- |
| A login 09111111111 / 123456 | PASS — `/fa/shipping` |
| B header identity | PASS — `09111111111` |
| C–D cart 2 | PASS — badge ۲ |
| E–G logout hide | PASS — ورود; DB Active 2; anon GET 400 |
| H–I re-login restore | PASS — badge ۲ |
| J identity Home/Cart/Shipping | PASS — same mobile |
| K–L address + before continue | PASS — badge ۲ |
| M–P commit then Payment | PASS — T4→T8→T9; Converted; Cycle exists; badge ۰ |
| Q fault 409 | PASS — stay Shipping, badge ۲ |
| R mobile menu logout | PASS |

Checkout `01a09b33-751e-7000-9977-1ad5999c8e35`. Account cart `01a09b32-787c-7000-91b9-0be7d96bf8c4`.
