# TB-SHIPPING-USER-ENVIRONMENT-FINAL-CONFIGURED-DELIVERY-244-R2 — WORKER RESULT

Status: **PASS** within authorized demo-merchant configuration scope.  
Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.  
Worker result is not Architect / Visual / PSP acceptance.

## Canonical runtime lineage

| Item | Actual |
| --- | --- |
| Source | `D:\Users\User\source\repos\SarvNewVer` |
| FE → Host | `http://127.0.0.1:3000` → `http://127.0.0.1:5088` |
| Host PID / FE PID | 30328 publish-primary / 28396 `src/frontend` |
| Edition / deployment / DB | SingleStore / store-alpha / `tooba_alpha` |
| Base / issue | `3dc16e709f6007b3ee0bdec23c60db451874ba07` |
| Code-SHA | **NONE** (config + evidence only) |

## Corrected actual user / cart identity

| Fact | Value |
| --- | --- |
| Authenticated userId (OTP `09111111111` → `/v1/auth/me`) | `01a0996c-b8d9-7000-9854-80b3f59e8d7c` |
| Cart | `01a17b2d-e219-7000-b3b2-5bcdbe6810c5` |
| Cart.owner_user_id | `01a0996c-b8d9-7000-9854-80b3f59e8d7c` (match) |
| Status / version / qty | Active / **48** / **18** (4 lines) |
| Identity conflict | **none** |

Append-only correction: 244 RESULT correctly recorded `01a0996c-b8d9…`. 244-R1 RECEIPTS’ `01a0996c-1b0e…` was a wrong session artifact and is **not** the cart owner (see IDENTITY-CORRECTION.md under 244 and 244-R1).

## Approved settings applied (exact)

| Setting | Applied |
| --- | --- |
| 4 offers fulfillment | Platform + type `01a16f43-6b4a-7000-8670-4c16e14c6345` (DB readback) |
| Order owner resolution | group keys `cap\|platform\|store\|…`; accepted_owner_kind=0 / tenant `store-alpha` (Store, not Marketplace/Seller) |
| Method / draft | post/NULL «پیشتاز» draft `01a16eeb-70b3-7000-87c2-564c82835496` |
| Weight def | `demo_attr_mobile_weight` unit **g**, active |
| Category bindings | daf5 + dba9 (201) |
| Product packaged weights | Huawei / Samsung / Xiaomi → canonical **500** g each |
| Rate | sole row `01a16f4e-1c45…` min=**0**, max=**NULL** (lawful unbounded), freight **120000**, packaging **5000**, days 1–3 |
| Publication | snapshot `01a186f7-dd7a-7000-95e9-3c8243e01991`, sourceRev **10**, selectionVersion **11** |
| SlotDurationMinutes | **60** preserved on `authoring_weekly_schedules` |

## Per-group weight / rate / selection

| Group (seller) | Line qty sum | Weight (500g×qty) | Freight+pack | Date / hour | Snapshot |
| --- | --- | --- | --- | --- | --- |
| Arman `01a030d1…` (3 lines) | 11+3+1=15 | **7500 g** | 120000+5000 | 2026-11-02 / 14:00–15:00 ticks 504e9–540e9 | `01a186f7-dd7a…` |
| Nilva `01a0ffff…` (1 line) | 3 | **1500 g** | 120000+5000 | same | same |

All four cart lines belong to exactly one accepted group. Selection PUT: one **200** (rev TEXT `"2"`), twin concurrent **400** `storefront.captured_selection.stale`. Stale rejected snapshot `01a185c1-458a…` evidence replaced: both active groups now accept snapshot `01a186f7-dd7a…` selVer 11.

## Final stage / submission counts

- URL: `http://127.0.0.1:3000/fa/payment?cartId=01a17b2d-e219-7000-b3b2-5bcdbe6810c5`
- payment-draft **200**: shipping **250000**, payable **81661464.16**, selectionRevision `"2"`, `canInitiatePayment=true`, paymentState PendingPayment
- Cart **Active** unsubmitted; checkouts **0**; no Order/payment/reservation created by this task
- Stage left for user: review payment form and decide whether to submit (not done here)

## SHAs

Expected-Base-SHA / Base-SHA: `3dc16e709f6007b3ee0bdec23c60db451874ba07`  
Code-SHA: NONE  
Evidence-SHA: *(this evidence commit)*  
local==origin/main after push.
