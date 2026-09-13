# TB-P10-T004-R21-R1 — Runtime

Host `:5088` + FE `:3000` (FE already running). Migration `20260913090000_AddPendingPaymentCardHides` applied on `tooba_alpha`.

Guest smoke against `Host: alpha.localhost`:

| Check | Result |
| --- | --- |
| R1 unpaid active → لغو سفارش | PASS `POST /checkout/01a0994e-9f80-7000-a615-710dc04b1f4f/cancel` → `{ok:true,alreadyCancelled:false}`; retry `{alreadyCancelled:true}`; pending list empty for that checkout |
| R2 hide after hold ended/released | PASS hide while Active hold → `409 pending.hide.active_hold`; after cancel/release hide → `{ok:true,hidden:true}` |
| R3 unavailable line | PASS missing offer rejected; existing cart lines remain (`itemCount=2`); no empty-cart checkout |
| R4 happy checkout | PASS checkout `01a0994e-9f80-7000-a615-710dc04b1f4f`; source Cart `Converted` only after HTTP 200; pending card items=1 |
| R5 post-commit payment fail | PASS wallet initiate `400 payment.wallet.mixed_deferred`; checkout remains `PendingPayment` |

USER_VISUAL_ACCEPTED=YES
