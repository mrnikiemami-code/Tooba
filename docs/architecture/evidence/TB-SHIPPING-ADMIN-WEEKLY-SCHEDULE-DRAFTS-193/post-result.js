const fs = require('fs');

const base = '8dec7f8d86646d78588e9186d038352a6a7c6d80';
const code = '87d431cfe78b6c1a2af6ac7c55ccc8e8373587dd';

const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193
Parent-Task: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_OWNER_DRAFT_PER_WEEKDAY_OPERATING_SCHEDULE
Bridge-Task-Row: 7bc484d0-8b19-4211-ac3b-413fc19bc16a
Status: PASS
Expected-Base-SHA: ${base}
Base-SHA: ${base}
Code-SHA: ${code}
Evidence-SHA: ${code}
HEAD-Origin-Parity: HEAD == origin/main == ${code}; only pre-existing unrelated untracked evidence files remain (no modified tracked files).
One-Method-Schedule-And-No-Duplicated-Geography: PASS - the schedule is owned ONCE by the draft/method root (shipping.authoring_weekly_schedules PK draft_id) and is exposed to every region group of that draft; no group/rate/row/packaging/language/geography copy, no JSON blob, no per-language weekday rows, no cross-owner inheritance.
Independent-ISO-Weekdays-And-Remembered-Closed-Hours: PASS - exactly seven unique ISO weekdays (1=Monday..7=Sunday, Saturday=6, Friday=5) with an explicit per-day working/closed flag and one independent same-day [start,end) interval; Saturday 09:00-13:00 and Sunday 15:00-18:00 with Friday closed proven; closed-day remembered hours are retained when both bounds are absent, replaced only for that day when a valid pair is supplied, a partial pair rejects, and GET returns remembered hours truthfully while closed (re-opening needs an explicit valid pair).
Unconfigured-Versus-AllClosed-And-Explicit-Flags: PASS - absent schedule = configured:false with no timezone/days (no fabricated seven-day default); configured all-closed returns configured:true with the actual timezone and seven closed rows (distinct state); a missing isWorking flag is rejected by transport presence validation and again in the command workflow instead of deserializing into an implicit closed day; omitted/duplicate/out-of-range weekday and null collection/elements reject before any tracked write.
Canonical-Timezone-Window-Reuse-And-Legacy-Parity: PASS - the private timezone validator was extracted verbatim into Domain ShippingScheduleRules.NormalizeTimeZone (TimeZoneInfo.FindSystemTimeZoneById, 64-char bound, ScheduleTimeZoneRequired/ScheduleTimeZoneInvalid); ShippingOperatingSchedule now delegates to it with unchanged behavior/errors; legacy aggregate, windows, table and constraints untouched (no collapse/backfill/rekey) and ShippingOperatingScheduleTests still 74/74.
Real-AdminSeller-API-And-PreDispatch-Permissions: PASS - GET/PUT /v1/{admin|seller}/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule; ONE Application query + ONE command for both audiences; fulfillment.view on GET and fulfillment.manage on PUT checked independently through the real IShippingAuthoringOwnerAuthorizer BEFORE ISender dispatch; body carries no owner/mode/tenant/seller/geography/language/currency; foreign or unknown draft is the same safe 404; view-only PUT denied pre-dispatch; SingleStore seller unavailable before seller resolution.
Invalid-Last-Day-Same-Context-Atomicity: PASS - the whole week is preflighted before the first mutation; a bad seventh day returns a stable typed error leaving the same aggregate/tracker, schedule header, timezone, revision, translations, groups/titles, rate rows and geography snapshot unchanged, and a later SaveChanges publishes nothing; header + seven day rows + root revision commit in ONE implicit SaveChanges (no manual BeginTransaction, no per-day write).
Shared-Root-Revision-CrossCapability-Race: PASS - the SAME root draft.Revision token as regions/rates/metadata; stale expected revision -> canonical AuthoringDraftRevisionConflict (409); DbUpdateConcurrencyException clears the tracker and returns the same code; executed deterministic schedule-PUT-versus-rate-writer race on one original revision behind the bounded pre-write barrier (either winner allowed, canonical loser conflict, winner-dependent state with no hybrid rows and a safe loser tracker).
Metadata-Rate-Region-Preservation: PASS - an unrelated capability write preserves the schedule and vice versa; multilingual method text, group titles, rate rows/currency and region selections remain exact across schedule writes (verified in the executed storage roundtrip).
Additive-Migration-Byte-Parity-And-Storage-Negatives: PASS - one additive migration 20261022085811_AuthoringWeeklySchedule (+designer+snapshot) creates only shipping.authoring_weekly_schedules (PK draft_id, ck timezone length, cascade FK to draft) and shipping.authoring_weekly_schedule_days (composite PK draft_id+weekday, cascade FK to header, ck weekday 1..7, ck hours paired, ck end>start, ck working-needs-hours); historical migrations/designer files byte-identical, no legacy table/constraint/data change; raw SQL negatives each rolled back individually asserting exact SQLSTATE (23514/23505/23503) AND the owned constraint identity.
Real-Postgres-Roundtrip-And-Cleanup: PASS - fresh task-owned postgres:17-alpine container tb193-pg on 127.0.0.1:55432 (db shipping_tb190r1, user tb190r1), migrations applied to that disposable DB only; seven distinct day keys + remembered times read back in a NEW context; container removed, port 55432 verified closed, shared :5432/postgres-db, RabbitMQ, Redis and shared apps untouched.
Contracts-DI-Validation-Errors-And-Guard-Parity: PASS - Contracts-only boundaries, canonical Endpoints/ISender/CQRS/Result/ApiResponseFactory; two new stable codes (AuthoringScheduleWeekdaySetInvalid, AuthoringScheduleDayInvalid) with exactly one descriptor owner and neutral+fa resources; no new project/package/foreign reference and no Host business logic; classification manifest updated with exact actual sets (shipped routes 35->39, endpoint-reachable requests 26->28, request validators 23->25), no baseline widening and no certification promotion.
Cutoff-Holiday-Capacity-And-CustomerPromise: NOT_IMPLEMENTED
Focused-Commands-And-Test-Counts: dotnet build Shipping.Tests 0 errors; focused ShippingAuthoringWeeklySchedule 20/20 passed (17 real admin/seller HTTP + 3 executed PostgreSQL); legacy ShippingOperatingScheduleTests 74/74; ShippingRequestClassificationTests 11/11; ShippingPersistenceModelTests 52/52; ShippingErrorCatalogAndResourcesTests 11/11; ONE final whole Tooba.Shipping.Tests 609/609 passed, 0 failed, 0 skipped, 18 s with the disposable DB live.
Changed-Files: Contracts (authoring schedule day/week replace request, two error codes); Domain (new ShippingAuthoringWeeklySchedule entities, ShippingAuthoringDraft week preflight/apply, new ShippingScheduleRules + legacy delegation); Application (Get/Replace weekly schedule query+command+workflows, view models, port, validators); Infrastructure (directory ReplaceWeeklyScheduleAsync + Include, DbContext mapping/CHECK/FK, one additive migration+designer+snapshot); Endpoints (admin/seller GET+PUT, error catalog, neutral/fa resources); Tests (17 endpoint cases, 3 PostgreSQL cases, persistence model/migration/FK counts, classification and error catalog expectations); docs (task artifact, evidence RESULT, SHIPPING-ADMIN-RULES-DECISION section 24, recovery pointer 14, request classification manifest).
Claim-UTC: 2026-10-22T08:50:55Z
Result-UTC: 2026-10-22T09:13:00Z
Elapsed-Minutes: 22
Hard-Deadline-Compliance: PASS - implementation, focused+whole suite, PostgreSQL execution, cleanup, commit, push and report inside the 45-minute hard bound.
Out-Of-Scope-Commands: NONE
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: fresh task-owned postgres:17-alpine container on 127.0.0.1:55432 removed and port verified free; no shared :3000/:3419/:5088/:5432, tooba_alpha, Redis/RabbitMQ or shared bin/obj/.next/cache touched; no reset/rebase/stash/force-push.
Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT`;

(async () => {
  const response = await fetch('http://127.0.0.1:17321/api/results', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ channelId: 'tooba-main', taskId: 'TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193', content }),
  });
  console.log('RESULT', response.status, (await response.text()).slice(0, 200));
  if (response.status >= 200 && response.status < 300) {
    const done = await fetch('http://127.0.0.1:17321/api/tasks/7bc484d0-8b19-4211-ac3b-413fc19bc16a/complete', { method: 'POST' });
    console.log('COMPLETE', done.status, (await done.text()).slice(0, 200));
  }
})();
