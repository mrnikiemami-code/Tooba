# TB-280-R1 — RESULT

Task: `TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1`
Parent: `TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: cursor
Status: **PASS within the issued UX1/UX2/UX3 frontend closure scope**

Decision:
`ONE_AUTHORITATIVE_REVIEWED_MONEY_DISPLAY_CONDITION_BOUND_RECEIVED_GOODS_REVIEW_AND_PER_PRODUCT_UNIT_SUMMARY_ON_THE_EXISTING_RETURNS_WORKSPACE_WITH_EXACT_COMMAND_SAFETY_PRESERVED_ZERO_BACKEND_CHANGE_AND_ZERO_LIVE_BUSINESS_WRITE`

Expected-Base-SHA `9075e2fa4325a2de60cbd17ebfe908d9d9ee74ed` is an ancestor of the claimed HEAD. Claim HEAD
`4ce50d00690f3abdb058289f4a82d63275d136fb` is the docs-only issuance of this task, so no incoming
source/config/schema drift arrived with the claim.

NOT Architect ACCEPT, NOT user functional/visual acceptance, NOT sellable/Marketplace readiness.
Parent 280 remains PARTIAL pending independent review of this repair. 279-R3 fixed-scope Architect
ACCEPT is preserved. Backend source was not changed.

| Area | State | What this means |
| --- | --- | --- |
| UX1 money | approved / bound attempt | `firstRefundReviewMoney` shows stored `approvedAmount` (zero stays zero; null without a reviewed-plan frozen amount refuses send); resume/retry use `boundAttemptReviewMoney` for the exact server-published attempt id; mismatched/missing binding disables confirm and posts nothing; requested `refundAmount` is no longer the confirm figure |
| UX2 conditions | exact per-condition init | `beginResolution` fills `acceptedUsable`/`acceptedDamaged` from those fields (never aggregate `accepted`); only positive received ceilings render editable inputs; zero-received siblings show a not-received notice and still require a reason; empty accepted means deliberate zero; over-ceiling refuses locally with «بیشتر از … وارد نکنید» |
| UX3 summary | per product·unit | `describeReceivedGoodsSummary` lists each product's received/requested with its own unit; `hasAnyReceivedGoods` is a boolean presence check — pcs+grams never becomes «501 of 501» |
| proof | focused FE | new `admin-return-workspace-280-r1.mount.test.ts` M1–M7 **7/7**; retained mounted 276+277+279+280+280-R1 **34/34**; FE unit (queue/error-map/quantity/return-api) **49/49**; `tsc --noEmit` **0**; `npm run build` **0** (`BUILD_ID xUhUnq_b5Okl6j0RYf2Jl`); no backend build/publish (zero backend change) |
| served | FE refreshed only | Host `5088` PID `31652` unchanged, dll SHA256 `147D033E79F774EE2F24925C501CF0DCE823571529F3ACE0004EBA37C096605B` (`/health` 200, `/health/ready` 200 SingleStore + messaging Healthy); Next `3000` PID `37500`, chunk `7944-ff7f57f1e093b6b2.js` 200 carries `admin-return-received-summary` / «هنوز کالایی دریافت نشده است» / unavailable-money copy and none of «برنامهٔ تفکیک»/«نسخهٔ بررسی» |
| live | read-only | `01a1eefb-ea96-7000-838d-9a0b37661c8b` still `Requested`, `updatedAt` `2026-11-18T14:07:29.430866Z`, received `0`, attempts `0`, receipts `0`, decision version `0`, `canPublishResolution=false`, `actionCodes=["record_receipt"]`; DOM 1440/390: overflow `0`, summary «هنوز کالایی دریافت نشده است», one primary receipt action (≥44px), open/cancel receipt → 0 alerts, 0 POST, sessionStorage intent empty |
| data/schema | untouched | zero migration/DDL/seed/backfill/reset/cleanup/restore, zero live business write, zero POST, zero money/stock/provider/notification effect, Host untouched |

## Known limits

- mounted proofs are jsdom behaviour with controlled HTTP (not geometry); live checks are read-only GET/DOM/text
  at 1440 and 390 (no screenshots);
- the live case still has no received goods and no approved amount, so money/mixed-condition dialogs were proven on
  the mounted real component and fixtures, not against stored data (no live POST authorized);
- 279-R3 controlled-port limits and key exposure `CONTAINED_PENDING_REVIEWED_RETIREMENT` are unchanged and out of scope.

STOP_FOR_ARCHITECT_AND_USER_REVIEW; `automaticNextImplementationTask: NONE`.
