# TB-P10-T004-R24-R1-R1 — Runtime A–K

Host `http://127.0.0.1:5088` + FE `http://127.0.0.1:3000`. Demo OTP `09111111111` / `123456`. Script: `_r24-r1-r1-runtime.mjs`.

| Step | Result |
| --- | --- |
| A login | PASS — OTP 200 user `01a0996c-b8d9-7000-9854-80b3f59e8d7c` |
| B cart populated | PASS — guest line + merge `lines=1` cart `01a09aae-35cb-7000-a54d-6c9ee875ebd1` |
| C shipping | PASS — projection `min=2026-09-18` |
| D legacy saved address | PASS — existing `محمد لمامی` id `01a09a72-0fd0-7000-b93f-907016579c4a` (no guessed split). `r24-r1-r1-shipping-fields.png` |
| E explicit First/Last | PASS — selection draft `محمد` / `امامی` / display `محمد امامی` |
| F continue to payment | PASS — commit checkout `01a09aae-37d0-7000-89cd-a22cebf0a90f` |
| G payment summary | PASS — `recipientName=محمد امامی` on commit + GET checkout. Snapshot SQL `محمد امامی\|محمد\|امامی`. Not `محمد لمامی`. |
| H customer order | PASS — GET `/v1/customer/orders/{id}` `محمد امامی`. `r24-r1-r1-customer-order-recipient.png` |
| I admin order | PASS — GET `/v1/admin/orders/{id}` `محمد امامی` (dev actor). Browser admin shell denied while a customer session cookie is present; Host projection is the SoT. |
| J historical fallback | PASS — checkout `01a09a9b-3880-7000-af7a-0a21d7de6ac7` still `محمد لمامی`. `r24-r1-r1-historical-legacy-recipient.png` |
| K cart/header | PASS — current cart 404 after COMMIT; FE `/fa` and `/en` 200; shipping after new ATC shows badge ۱ and `حساب کاربری` |

Raw: `_r24-r1-r1-runtime-raw.json`.
