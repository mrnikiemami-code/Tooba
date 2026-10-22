PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193
Parent-Task: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 8dec7f8d86646d78588e9186d038352a6a7c6d80
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_OWNER_DRAFT_PER_WEEKDAY_OPERATING_SCHEDULE
Hard-Timebox: 45 minutes HARD INCLUDING cleanup, commit, push and report; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Persist/read/replace the accepted per-weekday working schedule on ONE owner-scoped Shipping method DRAFT: independently working/closed days with their own hours and an explicit valid timezone. All region groups of that draft share it; no schedule geography or per-language copy.
This is backend schedule authoring with genuine authorized API and relational proof, not runtime availability, a promised delivery date, capacity enforcement or a frontend task.

MANDATORY FIRST CHECK
Fetch main; inspect branch/HEAD/dirty/untracked/stashes and preserve unrelated work. Require HEAD == origin/main == Expected-Base-SHA before edits. A safe strictly-behind branch may fast-forward only to the exact base. Mismatch/divergence/overlapping dirty work => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push.
Read AGENTS.md, docs/ai/AGENT-SKILLS.md, full .cursor/skills/tooba-backend-feature/SKILL.md and relevant Migrate/Structure rules on changed surface, not another full AMSC cycle. Read current Shipping rules/recovery, TB-192-R1 ARCHITECT-REVIEW, TB-186-R2 per-day decision, current draft/directory/owner authorizer, legacy ShippingOperatingSchedule/ShippingOperatingWindow, schedule contracts/validators, EF mapping/migrations, canonical Result/errors/localization and existing endpoint/PostgreSQL harness.
No findings-register scan or unrelated-module audit. Accepted owner/region/rate/metadata drafts remain incomplete, inactive configuration.

BOUNDED D5 AUTHORING DECISION
The schedule is attached ONCE to the draft/method, not group, rate row, packaging, seller actor, interface locale or raw geographic scope. Different owners/methods do not inherit it.
Absent schedule = explicitly UNCONFIGURED, not unrestricted availability, a holiday or seven default working days.
Configured schedule = explicit validated timezone + exactly seven unique ISO weekdays, each with an explicitly supplied working/closed flag and its own local interval. All seven closed is allowed and DISTINCT from unconfigured. Do not invent "at least one working day".
One same-day interval per working weekday in this accepted preview slice. Saturday and Sunday can differ (e.g. 09:00-13:00 vs 15:00-18:00), Friday closed. ISO weekday identity is 1=Monday ... 7=Sunday, so Saturday=6 and Friday=5; never cast .NET DayOfWeek blindly. UI display order/labels are a later locale concern, not persisted identity.
Working interval is [start,end), end strictly after start. Equal/inverted/overnight/missing active times reject; no automatic reversal, overnight extension or normalization into tomorrow.
Closed-day remembered hours: when both hours are absent/null, retain that day's previously stored pair if any; on first configuration they may remain null. A supplied pair must be valid and replaces only that day's remembered pair. One supplied bound without the other rejects. GET returns remembered hours truthfully even while closed; re-opening requires a valid explicit active pair, so UI can reuse returned hours without inventing defaults.
No collapse/backfill/rekey of existing global multi-window ShippingOperatingSchedule records. They remain byte/behavior unchanged. This new owner draft is the future authoring target, not a second active runtime schedule authority.

TIMEZONE / CANONICAL REUSE
Timezone is explicit owner-authored configuration validated in the SERVER, never inferred from Persian locale, country, browser clock or a hardcoded Asia/Tehran default. Reuse the actual existing TimeZoneInfo.FindSystemTimeZoneById/length/typed-error semantics from ShippingOperatingSchedule, and the existing ShippingOperatingWindow weekday/range rules where applicable.
A narrow shared Domain validation extraction is authorized if needed because the timezone validator is currently private; preserve legacy behavior/errors and focused tests, no dummy legacy aggregate construction or duplicated business policy. No foreign module implementation dependency, new package, timezone registry/service or HTTP call.
Use existing TimeOnly transport/storage conventions with documented invariant precision/serialization. Do not round/convert local times into UTC moments or invent a separate culture-dependent parser. A valid timezone id is NOT proof of DST/holiday/calendar/customer-slot integration.

SCOPE EXCLUSIONS FOR TIME
No cutoff calculation/field, holidays, exception dates, per-region override, multi-window legacy conversion, preparation time, delivery-duration reinterpretation, customer date/window generation, capacity field/reservation, warehouse work hours or order promise changes. Keep those explicitly pending. This slice stores independent weekday hours only; it does not claim to complete D5 runtime orchestration.

AUTHORIZED READ / WHOLE-WEEK REPLACEMENT
Add GET/PUT:
/v1/admin/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule
/v1/seller/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule
One shared Application query and one command/handler for both audiences. Reuse actual IShippingAuthoringOwnerAuthorizer with fulfillment.view on GET and fulfillment.manage on PUT independently BEFORE dispatch. SingleStore seller remains 404 before seller resolution; no admin arbitrary-seller bypass or client owner credential.
PUT includes expectedRevision, timezoneId and the seven-day collection; no owner/tenant/mode/seller/geography/language/currency fields. A missing working flag must NOT deserialize into an implicit closed day: transport presence must be validated explicitly. Validate route ids, revision, required body/lists, null elements, duplicate/missing/out-of-range weekdays, required flags, time shape/pairs and timezone with canonical stable codes and FluentValidation. Preserve repaired required-collection and null-parent patterns; no silent null-to-empty week.
GET returns actual root revision and configured/unconfigured state. For unconfigured return no fabricated timezone/hours. A configured all-closed week returns its actual rows and timezone. Reads must scope by FULL authorized owner plus draft; foreign/missing draft is the same safe 404.
PUT replaces/preflights the WHOLE week before any tracked mutation, then saves schedule + day rows + root revision atomically via ONE implicit EF SaveChanges. Use the SAME root concurrency token as rates/regions/metadata; stale root revision -> canonical 409. No second explicit BeginTransaction or loosening R2 transaction guard.
Bad last weekday must leave SAME aggregate/context week, timestamp, revision and all unrelated data unchanged; a later SaveChanges cannot leak a partial edit. An identical normalized PUT may be a truthful no-op (document exact behavior), but expectedRevision is always checked. No N independent browser writes/partial success.

PERSISTENCE / ARCHITECTURE
Use cohesive typed Shipping-owned optional schedule header/day storage with own FK(s) to draft and unique (draft,weekday) or equivalent stable key; no JSON blob, foreign FK, duplicate catalog, copied region selections or translated weekday rows.
No schedule exists until explicitly configured; once configured whole-week replacement preserves each day's identity/remembered hours. An omitted day is invalid rather than silently closed. Other capability writes/reorders/group removal preserve this ROOT schedule; schedule writes preserve exact method translations, groups/titles, rate rows/currency and binding/owner.
One additive Shipping migration/designer/snapshot is authorized for NEW own schedule storage only; old migration/designer files byte-identical, no legacy table/constraint/data change or shared DB application.
Own storage checks must match enforceable day/working/time/null-pair invariants with explicit NULL handling. Exactly-seven completeness and resolvable timezone rely on canonical preflight, not a fake SQL cross-row/foreign catalog check. Document which invariants are enforced at each layer.
Endpoints -> ISender/CQRS -> Application ports -> Domain/own Infrastructure; no Host business logic, foreign Application/Infrastructure/Domain/Endpoints/DbContext/schema join/transaction or new project/package.
Canonical Result/ApiResponseFactory/typed error catalog/resources/fallback and DI/tracing conventions. Reuse existing schedule semantic codes only where meanings match; new authoring codes have exactly one owner and actual resource text. No fixed production language inventory; schedule rules shared across all database languages and cultures.
Update route/request/validator classification with exact actual sets, root/path/structure/DI guards for authorized additions, no widened baseline or module certification promotion.

MEANINGFUL TESTS — COMPLETE THE SLICE, NOT A CLAIM
New focused capability tests (cohesive files, avoid growing the giant relational fixture). Reuse or minimally extract existing test-only harness, never substitute successful Shipping authorization/handlers.

actual admin/seller GET/PUT -> real owner authorizer, validation, handlers/directory/factory; authorized configuration then readback; view-only PUT denied before dispatch; foreign store/seller denied safely; SingleStore seller unavailable before resolution;
initial unconfigured vs seven closed, missing/duplicate weekdays, omitted working flag, null collection/elements, missing active times, bad pair/equal/inverted/overnight/timezone reject with stable errors before mutation;
independent Saturday/Sunday hours and Friday closed; switching Friday closed/working and editing one day never overwrite another day; remembered closed hours survive reload;
all-closed allowed, localized interface/content language does not change ISO day identity/timezone/stored hours; no default timezone from locale/country;
invalid seventh day leaves SAME tracker/aggregate/revision/timezone/other capability data unchanged and later SaveChanges publishes nothing;
existing legacy timezone/window tests still pass if the canonical validator was extracted.

Real Npgsql/PostgreSQL:

full week + timezone create/update -> NEW context readback; inspect exact seven distinct day keys and remembered times;
owned unique/FK/CHECK negatives for invalid weekday/null-pair/working-without-times/inverted interval; assert actual SQLSTATE plus owned constraint identity, rollback failed SQL attempts individually;
deterministic schedule PUT versus ONE rate/metadata/region writer race on same original root revision using the bounded pre-write barrier, either winner allowed, canonical loser conflict, exact winner-dependent state/no hybrid and actual loser tracker safe;
unrelated group reorder/rate or metadata edit preserves schedule; schedule update preserves the saved multilingual text/rate/currency/geography snapshot.
A directory-only test is not HTTP/authorization proof; static model SQL is not relational execution; label reused/new/source-only evidence precisely.

RUNTIME / DEADLINE
Fresh task-owned PostgreSQL 17 container using existing image/tools and parsed exact fixture target; prior container was removed. Verify free loopback port/container ownership, parsed host/port/db/user before connection and live target before any write. Existing test target 127.0.0.1:55432 / shipping_tb190r1 / tb190r1; never shared :5432/tooba_alpha/store-alpha, no credentials logged.
Apply module migrations only to the disposable test DB through existing module/test authority; no Host startup/shared runner. Setup <=8 minutes, no package/image/browser install/download.
Record actual Bridge receipt claim timestamp and wall-clock result UTC plus monotonic elapsed/deadline; do not estimate/backdate timestamps. If clock/receipt values disagree, disclose it as unresolved, not fabricated compliance. Reserve last 5 minutes; start no new implementation/test after minute 40. Every command <=180 seconds under real timeout, no blocking wait >60 seconds, orphan test or repeated broad loop.
Build affected Shipping.Tests only; run focused tests first, then at most ONE final whole Shipping suite if time remains with required DB live. NO Host.Tests, solution build/test or whole other-module suite. No skip/exclusion/baseline refresh to make green.
If a blocker/required proof cannot fit, commit only coherent in-scope progress and report PARTIAL with the exact missing cases, cleanup and STOP. No artificial deadline extension. Cleanup exact task-owned DB/container/listener/isolated output and verify shared apps untouched.

LOCKS / ACCEPTANCE / EVIDENCE
No frontend, taxonomy, calculation/payment/carrier/free modes, quote/evaluator/publish/activation, allocation/financial/Checkout/payment/deployment or certification change. Runtime merchandise/measurement and merchant-registry/browser integration stay open; default-OFF flags, PendingAllocation and TB-185 pause preserved.
Keep shared :3000/:3419/:5088/:5432, bin/obj/.next/cache, Redis/RabbitMQ and unrelated dirty/untracked/stashes untouched. No reset/rebase/stash/force-push. No dummy/probe POST to Bridge /api/results.
PASS requires real per-day persisted owner schedule, explicit timezone/closed/unconfigured semantics, authentic API/auth/validation and PostgreSQL atomicity/race/constraint proof, legacy preservation and cleanup within deadline. PARTIAL for missing required proof/progress; BLOCKED for authority/architecture/isolation conflict. No full Admin/production claim.
Persist received task verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193.task.md after claim. Evidence in docs/architecture/evidence/TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193/: RESULT.md, week/timezone/null semantics matrix, API/owner/validation proof, migration SQL/parity, relational assertions, commands/counts and timing/cleanup.
Append D5's bounded authoring decision and actual outcome to Shipping rules/recovery without replacing legacy history or claiming Architect ACCEPT. Stop; no automatic next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
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
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 8dec7f8d86646d78588e9186d038352a6a7c6d80
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
One-Method-Schedule-And-No-Duplicated-Geography: ...
Independent-ISO-Weekdays-And-Remembered-Closed-Hours: ...
Unconfigured-Versus-AllClosed-And-Explicit-Flags: ...
Canonical-Timezone-Window-Reuse-And-Legacy-Parity: ...
Real-AdminSeller-API-And-PreDispatch-Permissions: ...
Invalid-Last-Day-Same-Context-Atomicity: ...
Shared-Root-Revision-CrossCapability-Race: ...
Metadata-Rate-Region-Preservation: ...
Additive-Migration-Byte-Parity-And-Storage-Negatives: ...
Real-Postgres-Roundtrip-And-Cleanup: ...
Contracts-DI-Validation-Errors-And-Guard-Parity: ...
Cutoff-Holiday-Capacity-And-CustomerPromise: NOT_IMPLEMENTED
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Out-Of-Scope-Commands: NONE
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: ...
Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK