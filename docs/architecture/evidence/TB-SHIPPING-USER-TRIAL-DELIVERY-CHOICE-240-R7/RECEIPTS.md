# TB-SHIPPING-USER-TRIAL-DELIVERY-CHOICE-240-R7 — RECEIPTS

All facts below are worker-executed on the isolated task target, not root execution. Raw runner scripts and logs
live outside git in `D:\temp\tb240r7-runner\` and `D:\temp\tb240r7-*.log`; the excerpts here are the exact values
those runs produced.

## A — Isolation / provenance

| Item | Value |
| --- | --- |
| Worktree (fix + FE) | `D:\temp\tb240r7` (detached at `581c5f31aa949e92e410977dab8bab1bbefa01a8`) |
| Main repo (commit target) | `D:\temp\tb238`, branch `task238` |
| FE (isolated) | `http://127.0.0.1:3104`, PID 46448 (deps reused from the shared install; no install) |
| Host (same task Host, untouched) | `127.0.0.1:5190`, PID 43548, `tooba_marketplace_tb239`, Marketplace edition, tenant NULL |
| Browser (task-owned) | installed Chrome, profile `D:\temp\tb240r7-chrome`, CDP **9344**, PID 44116, visible |
| Buyer (public registration) | `userId 01a183e3-59cd-7000-829d-a0587da45e17`, password random/`%TEMP%` only |
| Cart | `01a183e5-b620-7000-bf41-97187918d843` v8, «فروشگاه آرمان» + «دیجی‌استایل نمونه» |
| Frozen / never enumerated | R6 user window `D:\temp\tb240r5-chrome` CDP 9343 + FE 3103; original 3000/5088; R4 9342/3102; handoff 3100/5188 |

Login used the existing BFF routes only: `GET /api/auth/csrf` → `POST /api/auth/login` (password) → `GET
/api/auth/me` 200. No OTP bypass, no dev actor, no injected session, no secret in this file.

## B — BEFORE (base build, real page, 2 groups) — `D:\temp\tb240r7-before.json`

```
group[0].deliveryText = null            // no "زمان تحویل" element at all
group[0].intervalText = "بازهٔ همین روز: 09:00–17:00"
group[0].chips[0]     = { value: "2026-10-29", ariaPressed: null, dataSelected: null, styledPrimary: true }
group[0].methodPicks[0].ariaPressed = "false"
continueDisabled = true ; continueAriaDescribedBy = null ; blockerText = null ; blockerId = null
```

## C — AFTER, two-seller cart — `D:\temp\tb240r7-flow.log`

```
--- S0-empty-address
   both groups: chosenDates [], methodPressed "false",
   deliveryText "زمان تحویل این گروه: هنوز انتخاب نشده",
   intervalText "زمان تحویل (بازهٔ روز پیشنهادی — هنوز انتخاب نشده): 09:00–17:00", chipCount 7
   continueDisabled true, continueDescribedBy "shipping-continue-blocker",
   blockerText "نام و نام خانوادگی گیرنده را کامل کنید."
--- S1-address-complete-no-dates
   continueDisabled true, blockerText "برای «فروشگاه آرمان» یکی از تاریخ‌های تحویل را انتخاب کنید."
--- S2-groupA-date-picked  (chip …|-post-standard-2026-10-29)
   A: chosenDates ["2026-10-29"], methodPressed "true",
      deliveryText "زمان تحویل این گروه: 1405/08/07 — ارسال فروشگاه آرمان ۲۴۰ 09:00–17:00",
      intervalText "زمان تحویل (بازهٔ روز انتخابی): 09:00–17:00"
   B: unchanged, chosenDates []
   continueDisabled true, blockerText "برای «دیجی‌استایل نمونه» یکی از تاریخ‌های تحویل را انتخاب کنید."
--- S3-both-groups-picked  (B chip …|-post-standard-2026-10-31)
   A delivery "…1405/08/07 — ارسال فروشگاه آرمان ۲۴۰ 09:00–17:00", B delivery "…1405/08/09 — ارسال دیجی‌استایل ۲۴۰ 09:00–17:00"
   continueDisabled false, continueDescribedBy null, blockerText null
--- continue-click {"disabled":false}
--- selection-put
   PUT /v1/storefront/shipping/selection  status 200
   body.capturedGroups[0] = { groupKey "cap|seller|01a181b05c877000976eae5ce19864f6|…", serviceCode "post",
     optionCode "standard", selectedDeliveryDate "2026-10-29", expectedSnapshotId "01a18231-8fbe-7000-85fd-23705cc49084",
     expectedSourceRevision "7", expectedSelectionVersion "2", expectedAreaGroupId "01a181dd-6bcc-7000-823b-ee56a67b5736",
     expectedRateRowId "01a181de-0c26-7000-891b-a803cf897db6", expectedGoodsAmount "1580500" }
   body.capturedGroups[1] = { … "2026-10-31", snapshot 01a181de-0e59-7000-9127-f5142617340e, sourceRevision "6",
     selectionVersion "1", areaGroup 01a181de-0d04-7000-9d2b-f3ffede2fde9, rateRow 01a181de-0e2d-7000-83ef-06d8623b713a,
     goods "1951100" }
   body.expectedSelectionRevision = "0"   // TEXT
--- net-after-continue ["PUT /v1/storefront/shipping/selection 200"]
--- payment  /fa/payment?cartId=01a183e5-b620-7000-bf41-97187918d843  title "پرداخت | توبا"
```

Second real selection on the same cart (`D:\temp\tb240r7-echo.log`) kept the SAME shape with
`expectedSelectionRevision: "3"` and produced the payment rows:

```
payment-shipping-groups = "روش‌های ارسال Arman store delivery 240 ۷۱٬۰۰۰ ریال DigiStyle delivery 240 ۴۹٬۵۰۰ ریال"
payment-cart-draft-summary = "… هزینه ارسال: ۱۲۰٬۵۰۰ ریال · قابل پرداخت: ۳٬۶۵۲٬۱۰۰ ریال
  · وضعیت پرداخت: در انتظار پرداخت · این سفارش هنوز ثبت نشده است …"
```

## D — AFTER, real single-group cart — `D:\temp\tb240r7-single.log`

```
remove-second-seller { status 200, cartId 01a183e5-b620-7000-bf41-97187918d843, version 6, lines ["فروشگاه آرمان"] }
single-S0  cardCount 1, pressed [], delivery "زمان تحویل این گروه: هنوز انتخاب نشده",
           interval "…بازهٔ روز پیشنهادی — هنوز انتخاب نشده…", continueDisabled true, blocker "نام و نام خانوادگی…"
single-S1  continueDisabled true, blocker "برای «فروشگاه آرمان» یکی از تاریخ‌های تحویل را انتخاب کنید."
single-S2  picked …-post-standard-2026-10-29, pressed ["2026-10-29"],
           delivery "زمان تحویل این گروه: 1405/08/07 — ارسال فروشگاه آرمان ۲۴۰ 09:00–17:00",
           interval "زمان تحویل (بازهٔ روز انتخابی): 09:00–17:00", continueDisabled false, blocker null
single-continue {"disabled": false}
single-result path "/fa/payment?cartId=01a183e5-…"
  net [{ method "PUT", url "/v1/storefront/shipping/selection", status 200, groups 1, revision "2" }]
```

The second seller was then restored through the public cart API (`POST …/lines` 200, version 8, sellers
`["فروشگاه آرمان","دیجی‌استایل نمونه"]`) and the window was left on `/fa/shipping` (`D:\temp\tb240r7-final.log`):

```
handoff-state  cards: 2 (both «هنوز انتخاب نشده», pressed 0)
               continueDisabled true
               blocker "برای «فروشگاه آرمان» یکی از تاریخ‌های تحویل را انتخاب کنید."
window         { title "اطلاعات ارسال | توبا", url "http://127.0.0.1:3104/fa/shipping" }
cart           { status 200, cartId 01a183e5-…, version 8, lines ["فروشگاه آرمان","دیجی‌استایل نمونه"] }
auth-me        { status 200, id "01a183e3-59cd-7000-829d-a0587da45e17" }
route          { status 200, url "http://127.0.0.1:3104/fa/shipping" }
```

## E — Code checks

```
node --experimental-strip-types --test app/storefront/storefront-shipping-continue-state.test.ts
  tests 13  pass 13  fail 0
node --experimental-strip-types --test lib/architecture/frontend-test-discovery.guard.test.ts
  tests 3   pass 3   fail 0
tsc --noEmit
  exit 0
lib/architecture/frontend-source-size.guard.test.ts
  OVERSIZED_GROWTH src/frontend/app/storefront/storefront-shipping.tsx: 1210 > baseline 945   (base line count 1105 → PRE-EXISTING RED)
```

## F — Files touched

```
src/frontend/app/storefront/storefront-shipping.tsx                    (modified)
src/frontend/app/storefront/storefront-shipping-group-cards.tsx       (modified)
src/frontend/app/storefront/storefront-shipping-continue-state.ts     (new)
src/frontend/app/storefront/storefront-shipping-continue-state.test.ts(new)
docs/architecture/evidence/TB-SHIPPING-USER-TRIAL-DELIVERY-CHOICE-240-R7/{RESULT.md,RECEIPTS.md}
AGENTS.md · docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md · docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md · docs/ai/TOOBA-RECOVERY-CONTEXT.md
```

No backend/Contracts/DI/schema/migration/seed/package.json change. No screenshot was produced or committed (the task
forbids screenshots/visual review); every claim above is a DOM, HTTP or persisted-state fact.
