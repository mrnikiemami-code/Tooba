PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-094
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-093
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_INDEPENDENT_METHOD_NAMES_AND_ADMIN_EDIT
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: fbc02e06baefb43cceb3a7ad7d6b4e3efc24aaf5
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
Task 093 is accepted for the shared Development five-tab UI: no need to rebuild that shell. User explicitly wants ONE independent merchant-visible delivery method per row and arbitrary meaningful full labels (e.g., «پست معمولی», «پست پیشتاز»), not a parent provider row plus a second-level option. At present post:standard/post:express labels are only «معمولی»/«پیشتاز». Make independent method display-name editing genuinely usable, persisted and authorized on the shared Development admin surface, without changing immutable method_code, historical binding or Checkout. Scope is method name management ONLY. Do not over-claim full independent creation or commercial cutover.

PREFLIGHT / SAFE BASE
Claim exactly one pending task after BRIDGE-WAKE. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (093), 081-093 planning/evidence, Fulfillment one-level method/translation entities, gate/admin endpoints/read contracts, frontend five-tab shell and existing auth conventions. Fetch origin/main. Verify Expected-Base-SHA is ancestor; tolerate ONLY demonstrably nonoverlapping 093 result-bookkeeping drift; overlapping source changes => RECOVERY_CONFLICT/STOP. Preserve dirty/untracked paths; no reset/stash/rebase/force push. Record real base and changed files. Keep existing 5088/3000 processes intact unless a safe, expressly task-owned reversible action is essential; do not invoke blind Stop-Process.

MISSION A — METHOD LABEL AUTHORITY

Trace actual method-label precedence through Fulfillment DB translations, read DTO, server-rendered tab 1 and other tabs. Identify why the current user-visible names are only «معمولی» and «پیشتاز». Do not silently change existing persisted names until the edit path is proven.
Add the SMALLEST authorized Fulfillment-owned admin write seam to update per-language display name for an existing canonical one-level method. Follow existing architecture, CQRS/MediatR ISender, module-owned authorizer and Result/error conventions; no Host business logic and no EF type across Contracts. Validate language and bounded nonblank human label; return explicit fail-closed result for gate OFF, not-ready table, unknown method, invalid input and 401/403; never trust a client-provided legacy service/option binding. Write only the method's translation (upsert or update by canonical (methodId,language) uniqueness) in a transaction as needed.
Identity invariants: method_code immutable <=32, legacy_service_code/legacy_option_code immutable, provider_kind hidden, active flags unchanged, no option UUID used as identity. Previously paid order/draft/shipment codes remain byte-for-byte identical. No child options, provider grouping or second-level edit control.

MISSION B — REAL ONE-LEVEL TAB 1 EDIT UX
4. On «سرویس‌ها» add a clear per-method «ویرایش نام» action or inline editor with explicit method context, language choice supported by existing locale convention, prefilled current translation, Save/Cancel, loading, validation, success/denied/error and stale-selection protection. Show the FULL independently editable label as the method's name. The user should be able to save «پست معمولی» for post:standard and «پست پیشتاز» for post:express WITHOUT renaming either method_code or activating an inactive method. Do NOT automatically mutate Development records just to demonstrate example naming without an explicitly safe task-owned fixture/rollback; user can edit the real labels.
5. Preserve the current five tabs and their method selection, RTL, accessible keyboard navigation, row selection, gate-off legacy fallback and tabs 2–5 API behavior. After save, refresh the source of truth so the name is consistent in the list and selected-method context. Never fabricate successful save. Handle server-side Next.js data refresh appropriately.
6. Check whether 093's shared :5088 current-code Host must reload for new routes. Do not leave a misleading UI: if a controlled no-data-loss restart is possible using scripts/dev/host-stack.ps1 (owned PID/provenance/rollback validated), exercise it safely; otherwise give a precise BLOCKED_LIVE_DEPLOY report and do source/test proof only. Do not touch frontend login identity, production or staging.

MISSION C — FOCUSED PROOF
7. Targeted tests: module authorization and gate OFF refusing writes; valid per-language update and readback, repeated save idempotency, invalid/missing method, collisions prevented; frontend edit/cancel/error/403/stale selection; frontend typecheck. Use only focused tests relevant to changed code and avoid broad build retries against locked shared Host DLLs. For any live Development write, require explicit rollback of task-owned sample labels and verify old values restored; do not edit actual merchant-entered labels as test fixtures.
8. If safe shared-stack deployment and an authorized admin session exist, demonstrate real browser edit/read-back and rollback on a task-owned reversible sample, record minimal screenshots/logs. Otherwise truthfully distinguish tested code from live proof. The shared Development stack must remain healthy, with /health and /__platform-commerce provenance matching dev-singlestore/store-alpha/tenant-alpha.
9. Record exact elapsed time, SHAs, tests actually run, changed paths, explicit limitations and rollback in docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-094.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-094/. Do not stage task receipt. Commit and push only task-owned changes. Stop at 30 minutes even if PARTIAL.

ALLOWED PATHS

src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/**
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Shipping/** only if strictly required by the existing contract conventions
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/**
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs only if DI required
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/** corresponding tests
src/backend/Host/Tooba.Host.Tests/Architecture/FulfillmentModuleAmsc001W3CertGuardTests.cs only if the additive endpoint changes its manifest
src/frontend/app/admin/shipping-services/**
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-094.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-094/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-094.task.md receipt ONLY, UNSTAGED
If implementing requires other files, STOP and request Architect decision, do not expand silently.

FORBIDDEN
No method_code renaming; no edits to historical checkouts/cart_shipping_drafts/shipments; no delete/reseed/backfill; no new nested provider-option merchant UX; no Checkout authority/rates cutover; do not enable Tooba:ShippingRateAuthority or Tooba:ShippingOperationalShadow; no capacity reservation, COD settlement, delivery promise or synthetic packed-weight claims; no auth bypass, no arbitrary admin seed/grant; no unmanaged restart, no production/staging writes.

ACCEPTANCE
PASS only if names can be edited independently through a real authorized Fulfillment-owned write seam, the five-tab UI remains intact, focused tests pass and (if live proof is impossible) that limit is stated without claiming live usability. PARTIAL on any incomplete binding/deployment/verification; BLOCKED on unsafe access or unprovable data scope; RECOVERY_CONFLICT on overlapping drift. No automatic next task. Return actual elapsed and real Code-SHA/Evidence-SHA. STOP_FOR_ARCHITECT.

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-094
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-093
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_INDEPENDENT_METHOD_NAMES_AND_ADMIN_EDIT
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: fbc02e06baefb43cceb3a7ad7d6b4e3efc24aaf5
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<source proof, exact UI and backend behavior, guard/test/live evidence, changed paths, elapsed, limitations, rollback>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
