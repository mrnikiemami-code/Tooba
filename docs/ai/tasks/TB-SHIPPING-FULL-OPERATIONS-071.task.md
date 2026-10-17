PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-FULL-OPERATIONS-071
Parent-Task: TB-SHIPPING-MVP-FINAL-CLOSURE-070
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_SCHEDULING_VERTICAL_SLICE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 1c64b45731c59b16e36f6f4ed418219cef120800
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
This is task 1 of a maximum 10 Architect-reviewed tasks (071–080) to complete configurable Shipping operations, preferably in fewer. Deliver a working slice, not a planning-only exercise. Requirements: weekly service operating windows (Sunday 13:00–14:00 example), multiple windows, optional capacities, packaging fees, freight prepaid vs collect on delivery, weight and item-count pricing tiers, geographical/option-scoped rates, coherent admin UI, and end-to-end compatibility. Do NOT infer commercial approval or turn on Checkout pricing authority. User permits use of disposable Development tooba_alpha data; do not create scratch databases. Timebox is HARD; no repetitive loops.

BUDGET PLAN — NOT AUTHORIZATION TO AUTO-EXECUTE
071 scheduling Domain/Contracts/persistence/admin API; 072 scheduling admin UI; 073 weight/quantity pricing policy and quote inputs; 074 tier rules + packaging admin UI; 075 freight payer modes with safe Payment/Order interface; 076 schedule/capacity/packing/tier evaluation including atomic capacity handling where needed; 077 storefront presentation and customer promise; 078 existing-Dev live tests and targeted fixes; 079 cutover readiness, production configuration and explicit approval boundary; 080 final integration and recovery certification. Architect may combine tasks as evidence allows. Issue only one task per reviewed result.

PREFLIGHT
On BRIDGE-WAKE claim ONE Pending task on tooba-main and preserve exact received task file under docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-071.task.md (unstaged). Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md where available, 069/070 planning/evidence, existing Shipping Domain/Application/Infrastructure/Endpoints, Fulfillment.Contracts service-option identity, and EF migration registration. Fetch origin/main and prove ancestry from Expected-Base-SHA; allow only path-by-path identified nonoverlapping documentation/result drift. On source overlap: RECOVERY_CONFLICT/STOP. Preserve unrelated dirty/untracked paths. No reset/stash/rebase/force-push. If database access necessary, positively identify Development process, /__platform-commerce, tenant-alpha and tooba_alpha on localhost:5432, with no Production/staging target. Never assume PID from old tasks. Do not build Host while running process locks output DLLs.

MISSION — WORKING SHIPPING SCHEDULE VERTICAL SLICE
A. Ownership: Fulfillment owns service and option identities; Shipping owns schedules/coverage/rates and operating policy; Order owns customer delivery promise/snapshot; Payment/Settlement owns money handling. Implement inside Shipping with capability-first shallow layout, Contracts-only cross-module references, MediatR 12.5 ISender, Result/ApiResponseFactory and stable existing error conventions. No Host business authority.
B. Implement Shipping-owned persistent configurable operating schedule keyed to existing serviceCode and optional optionCode: ISO weekdays 1–7; multiple non-overlapping local time windows per weekday, e.g. Sunday 13:00–14:00; an explicitly validated timezone (prefer IANA), active flag and nullable positive per-window capacity configuration. Define [start,end) semantics, reject invalid/reversed/overlapping windows with stable codes, determine overnight-window behavior explicitly (support carefully or reject). Ensure inactive/absent schedule never silently shuts down legacy Checkout. Scheduling means carrier pickup/dispatch availability, NOT automatically promised customer delivery time.
C. Introduce only necessary Shipping EF tables/indexes/checks and a module-scoped migration; preserve existing 069 rate and coverage rows. Apply ONLY to positively verified tooba_alpha if needed for focused live proof; record bounded rollback. Never use all-module MigrationRunner or parallel database.
D. Implement authorized Shipping admin read/create/update/deactivate for the schedule under existing admin area. Use fulfillment.view and fulfillment.manage authorization; validate codes with canonical Fulfillment.Contracts read seams, forbid invented options. Preserve no Results.Json and no custom privilege bypass. Keep 071 backend-focused; next task creates a usable scheduling editor in existing Shipping UI.
E. Focused verification: one Sunday-only window 13:00–14:00 plus a second distinct window, persisted and readable; overlap/reversed-window/unknown option rejected via stable 400; unauthenticated 401 and unprivileged 403; repeat update idempotent; legacy storefront projection unchanged. Use minimal targeted tests, one Development HTTP roundtrip if safely available. If blocked, PARTIAL with exact cause, no repeated investigation. A configured capacity is NOT enforced capacity unless an atomic reservation/counting path is implemented; explicitly label configuration-only if applicable.

ALLOWED PATHS
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/**
src/backend/Modules/Shipping/Tooba.Shipping.Domain/**
src/backend/Modules/Shipping/Tooba.Shipping.Application/**
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/**
src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/**
src/backend/Modules/Shipping/Tooba.Shipping.Tests/**
src/backend/Host/Tooba.Host/Program.cs (only if ONE composition registration is indispensable)
docs/planning/shipping/SHIPPING-FULL-OPERATIONS-071.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-071/**
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-071.task.md (receipt ONLY, never staged)

FORBIDDEN
No Pricing/Checkout cutover, paid order/snapshot/payment mutation, invented commercial tariff, broad grants, Production/staging writes, separate scratch database, new frontend page, unrelated shared Host/Security edits, global exception handlers, mass migrations, unnecessary restarts, oversized logs, broad suites, polling/repair loops or automatic follow-up. Do not change unrelated dirty files.

ACCEPTANCE / STOP
PASS only with implemented and persisted Shipping-owned weekly schedule, working authorized admin API, proved overlap/invalid-option rejection, focused tests and backwards-compatible default. Disclose whether capacity is merely configured. PARTIAL if a material condition is unproven; BLOCKED for genuine safety/authority obstacle. Stop at 45 minutes. Commit only allowed paths, push, fetch and confirm HEAD==origin/main when safe. Record elapsed, exact files, migration, focused status evidence, rollback and remaining blockers. Worker PASS does not equal Architect ACCEPT.

RESULT CONTRACT
Return one complete canonical Worker Result through Bridge: no unresolved placeholders in ACTUAL transmitted result; no extra text after END_TOOBA_TASK.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-FULL-OPERATIONS-071
Parent-Task: TB-SHIPPING-MVP-FINAL-CLOSURE-070
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_SCHEDULING_VERTICAL_SLICE
Bridge-Task-Row: <actual Bridge row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 1c64b45731c59b16e36f6f4ed418219cef120800
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<ownership and scope; scheduling semantics; API and migration; target provenance; real Sunday 13:00-14:00 proof; tests; compatibility; capacity enforcement status; rollback; elapsed; changed paths; Git commit/push; blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK