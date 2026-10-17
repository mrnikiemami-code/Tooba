TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167
Parent-Task: TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_IMPLEMENT_REAL_CANONICAL_FLAT_METHOD_CREATE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 333a9c9640d5be96ae5d697cf621043ad34e03e9
Timebox: 45 MINUTES HARD; stop and report PARTIAL or BLOCKED when exceeded; no speculative broad refactor.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

Mandatory invariant

ONE customer-selectable canonical method = ONE independent Admin-managed method. Preserve flat list,
3 row actions (View/Edit/Delete), exactly 4 non-nested detail tabs, Persian RTL and existing styles.
Provider serviceCode is metadata/identity, NOT a parent commercial policy source. Do not disguise the
legacy provider-create API as new flat method creation or claim a method was created just because a
translation row exists.

Execution (one bounded implementation slice)

Discover the actual canonical catalog owner, persisted identity, translation/language registry,
method-to-service/option binding, creation seams and whether an independent new method can be safely
represented end to end without changing old recognized codes. Document a concise SOURCE->OWNER map
before edits. Reuse correct owner-CQRS / ISender / Result / Contracts paths. No Host business logic,
cross-module direct DB, raw SQL runtime writes, or client-generated binding.

Implement a REAL authorized method-scoped CREATE contract/handler/endpoint in the canonical owner, only
if the current schema truly supports the operation safely. Accept an explicit canonical code and only
server-validated identity/binding, display names for real registered languages and enabled state if
supported; validate duplicates, invalid codes, invalid provider/option combinations, unavailable
language ids, tenant scope, mismatched binding and concurrent requests. Preserve existing 7 canonical
methods and historical snapshots. Do not invent provider-specific integration readiness, tariffs or
free amounts. Newly created method without validated financial rule must be
NeedsConfiguration/unquotable, not customer-selectable. Explicitly distinguish provider metadata from
method identity.

Connect existing Admin «افزودن سرویس» to the REAL endpoint with a small accessible RTL form. Server
language registry only; no hardcoded fa/en/ru, no fake default translations. On successful create
refresh the flat list and allow View/Edit of exactly that new canonical method. Loading/duplicate/403/
validation/write failure must be visible and fail-closed; never show a success toast on uncertain
response. Keep old 3 row actions and four tabs.

Add focused owner and frontend tests: successful create/read-back, duplicate and race-safe identity,
permissions 401/403, server binding, language validation, new method remains financially unconfigured,
sibling isolation, legacy seven methods unchanged. If no safe dynamic catalog creation is possible
because the list is hardcoded/fixed registry, STOP as BLOCKED, provide precise owner-level missing
authority and the smallest safe predecessor task; never silently add a UI-only placeholder.

If feasible inside timebox, run one scratch-only authorized HTTP create->GET->reopen proof using the
reusable TB-157/158/166 harness patterns. Keep all production flags OFF; do not write shared databases.
If not feasible, mark PARTIAL (implementation not live-accepted) with exact missing proof, no invented
screenshots.

Hard safety and scope exclusions

NO writes/restarts to shared :3000/:5088/:5432, tooba_alpha/store-alpha, RabbitMQ/Redis, no real SMS/PSP
or payout.
No unrelated dirty files, stashes, reset, rebase, force push, repeated full suites, long
logging/perf investigation.
No hand-crafted carrier quotes, fake free/COD/provider modes, sibling tariff or coverage inheritance;
method-specific optional restrictions default unrestricted-by-admin while mandatory finance remains
explicit.
No Case D origin authority, no changes to already accepted customer Shipping group/promise/Payment/
Fulfillment/Settlement paths unless a bounded contract fix is genuinely necessary, reported and tested.
Changes may include a migration only with verified model necessity and safe additive backward
compatibility; if migration cannot be safely verified this timebox, explicitly PARTIAL.

PASS/STOP protocol

PASS only when real persisted canonical method creation works through the Admin entry point, then
list/read/reopen, with real language/binding and authorization checks plus proof that method is not
accidentally customer-quotable without owned policy. If only endpoint/frontend/tests work without
runtime, PARTIAL. If missing canonical authority cannot safely be filled in this task, BLOCKED with
source-specific diagnosis; don't implement a fake create.

Deliver code/evidence under docs/architecture/evidence/TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167/, task
artifact docs/ai/tasks/TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167.task.md, focused test results, exact
SHA and HEAD/origin parity, and BEGIN_TOOBA_WORKER_RESULT...END_TOOBA_WORKER_RESULT. PUSH only committed
scoped changes to main; then STOP_FOR_ARCHITECT. No automatic next implementation.
END_TOOBA_TASK
