PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269
Parent-Task: TB-SHIPPING-PARCEL-TABS-AND-QUIET-OPERATOR-VIEW-268
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: d4fb3b8e3505a445b123efc8d63badea52df1172
Mode: PRESENTATION_ONLY_QUEUE_PERSIAN_DATE_CONSISTENCY
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

User screenshot2026-11-15 /admin/fulfillments: «تاریخ ها چرا میلادی است؟». Fulfillment queue handoff date is raw2026-11-15 while adjacent operational deadlines are Jalali. Repair this small omission, no new business stage or layout redesign. One worker/task, no delegation.

SOURCE-CONFIRMED ROOT
src/frontend/app/fulfillment/fulfillment-work-queue-dispatch.ts:
formatPlannedHandoff returns row.plannedHandoffDate directly + raw window + raw IANA zone.
formatCustomerReceiving returns row.customerReceivingDate directly.
formatOperationalDeadline already uses fa-IR but returns raw ISO on parse/zone error.
Queue cell AND title call these formatters. TB267 standardized only order operations view, leaving queue inconsistent.

REPAIR
Use the existing shared operational display/date helpers or appropriate app date utility to render Persian queue dates consistently with operations page: explicit Persian/Jalali calendar and Persian digits. Preserve exact date-only commitment as that calendar day independent of browser timezone; don't convert DateOnly to UTC/local midnight and shift it. Preserve instant deadlines rendered in accepted carrier timezone; customer commitment stays separate with its own projected window. Time range isolated LTR, start/end not reversed.
«موعد ارسال», «تحویل مشتری», operational deadlines and their tooltips must all be human-readable and consistent. Human zone label derived from existing accepted zone, not repeated raw Asia/Tehran. No new dictionary/formatting framework if existing helper suffices.
Missing date -> existing «مشخص نشده»/—. Invalid input/invalid zone -> truthful concise unavailable marker, no guessed date/browser-zone fallback or raw ISO spill to operator.
Do NOT localize stored wire/query values: server sorting/filtering/date keys remain ISO facts; raw exports/contract preserve existing behavior. No new date filter, calculation, "today" logic, authority, API/data write or locale registry change. Only display. Don't change unrelated labels or tab counts.
Keep current grid64px/readability/mobile scoped horizontal scroll, existing parcel268 tabs and user order facts. Don't switch edition, mutate shipping draft, reset orders or rerun payment/Returns/scheduler work.

PROOF ONCE
Focused formatter tests: known2026-11-15 ->24آبان1405; date-only stable under opposite client timezones; accepted instant crossing UTC midnight displays correct carrier day/time; time windows retain start/end; missing/invalid/zone refusal no ISO/raw-zone leak. Existing affected queue tests once; no broad backend/module/parity suites. Typecheck/build once, FE restart only, confirm served BUILD_ID.
Real read-only served /fa/admin/fulfillments AND user's /admin/fulfillments route: handoff/customer/deadline cell+title shows Persian calendar, no rawYYYY-MM-DD/raw IANA label on normal accepted row, same underlying sort/query IDs. Check390/1440 without screenshot artifacts, no new overflow/clipping. Existing example group/parcel and user orders GET only, no fixtures or mutations.
No backend/schema/migration/provider call; ONE tooba_alpha5432 SingleStore/store-alpha, FE3000->Host5088, keys/CapturedPreview unchanged.255parked/Tipaxdeferred.

HANDOFF
TASK verbatim docs/ai/tasks/TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269/TASK.md, evidence TASK/RESULT/RECEIPTS/result-contract/date-display-proof.md under docs/architecture/evidence/TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269/. Update AGENTS/START-HERE with root+269display-only,268 awaiting user visual acceptance; no functional reopening.
Commit+push normally with exact code/evidence/finalHEAD/origin parity, servedFEbuild. No force/amend/reset/stash/rebase/credential artifacts. No userVISUAL_ACCEPT/all-shipping claim. One review URL, no next task/loop.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
