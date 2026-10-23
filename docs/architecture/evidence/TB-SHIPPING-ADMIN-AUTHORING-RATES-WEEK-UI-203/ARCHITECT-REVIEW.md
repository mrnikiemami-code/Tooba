# TB203 Architect review — 2026-10-23

Disposition: REPAIR_REQUIRED. Independently confirmed main ceeaa609b0c1875a992cb65d115f00cba2a243ff and compared to44a220dea77c87e3d8c1a80e07b22af4e2846e45. Read rates/week models, composed intent, actual screen and rate/week UI, focused tests and RESULT. Backend was not changed. Preserve the new existing-group row editor, root week editor and single merged intent.

Worker reports7 new +48 existing facts, typecheck,7 guards green, and four pre-existing source-size failures outside the diff. Architect did not rerun those suites. Do not widen/fix their baselines. Visual and mounted React remain NOTRUN; no acceptance implied.

## Observed defects

1. editor-week uses ONE hoursEdited flag for BOTH bounds. Architect executed unchanged module using Node24 type stripping with stored09:30:00.2500000 ->18:00:00.5000000 and edited only start to10:00. weekIssues returns dayTimeInvalid; buildWeeklyScheduleEdit emits start10:00:00 and end=null. Untouched end is parsed as if newly entered HH:mm. Ordinary one-field editing is broken and precision retention is false for the other bound on the same day. Track edits/normalize per bound and preserve the exact untouched token.

2. Actual screen enterDestination(view), confirmed Cancel Edit, resets only General setForm and controller.edit({}); it never calls setRates/setWeek. No formSeed change happens on this path. Modified rows/week remain displayed in view despite the clean session; another General edit can resurrect them into the next PUT. Reset ALL local capabilities from the same confirmed baseline as part of explicit discard. Existing cancel-reject/busy/unknown semantics must remain.

3. Architect executed withPendingTimeZone(createWeekForm(null),'Typed/Zone'): weekDirty returns false and issues=[] before Configure. This is typed local work but has no discard/browser warning. Track the pending raw text as dirty while still omitting weeklySchedule and blocking an empty-only Apply; never fabricate configured state.

4. New AuthoringRatesGroup read-only rendering shows only weight bounds, otherwise noCondition. It drops item-count and merchandise-amount conditions that the previous read-only202 panel displayed. This misrepresents stored rates in view mode. Render all actual axes, retaining exact decimal text and current labels/RTL behavior.

## Continuation

Only TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1, a short existing-model/screen repair. Regression tests must exercise per-bound editing, whole-form discard/reset, pending timezone dirty and count/amount-only read presentation; existing current tests missed these paths. Prefer small pure helpers actually called by UI rather than installing a React framework; honestly report source versus mounted coverage. No new group/destination UI before review. Backend199/201, adapter200 and General202 remain closed, no proof replay. Production functional acceptance remains PENDING.
