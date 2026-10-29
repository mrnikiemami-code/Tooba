# RECEIPTS — TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R3

## Identity
- TaskId: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R3
- Bridge row: 96fed0f8-5a85-4a84-844e-ee28cdc4e613
- Base / ISSUE: a3e64896afb51432954b4253abd6453ef0902c7e
- Canonical FE http://127.0.0.1:3000 → Host 5088 SingleStore/store-alpha/tooba_alpha
- Buyer: 01a0996c-b8d9-7000-9854-80b3f59e8d7c
- Cart: 01a17b2d-e219-7000-b3b2-5bcdbe6810c5 Active v48 qty18

## Focused checks
- storefront-shipping-captured-selection.test.ts
- storefront-shipping-destination-refresh.test.ts
- storefront-shipping-continue-state.test.ts
- storefront-shipping-destination-choice.guard.test.ts
- Result: 47/47 pass
- typecheck: clean
- production build: exit 0

## Live browser (FE3000)
- path0/afterProvince/afterCity: `/fa/shipping`
- beforeunload: 0
- shipping-groups mounted before/after province/city: true
- projection count: 1 after province, 2 after city (no duplicate for same identity)
- deliveries retained on compatible group; incompatible group cleared by fingerprint
- Continue disabled while destination unconfirmed

## Host cart re-read
- OTP owner match; cart status Active; version 48; qty 18; lines 4
- No selection PUT / payment submitted in this task
