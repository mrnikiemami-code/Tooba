PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-FULL-OPERATIONS-073
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-072
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_WEIGHT_QUANTITY_PRICING_DOMAIN_AND_PERSISTENCE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: b6ec9c5926ff85a45920e8dd5d0883cb82877ad5
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
Task 072 ACCEPT: the real Persian schedule editor and authorized API roundtrip are proven. This is task 3 of at most 10 Architect-reviewed tasks (071–080), NOT authorization to begin 074. User requests full Shipping configurable operations including weight ranges, item-count ranges, packaging, prepaid/collect freight, day/time windows and capacity; task 073 owns the weight/count pricing foundation only. Do not mistake working rate configuration for customer Checkout cutover or approved commercial tariffs. Use existing disposable Development tooba_alpha only when a focused proof needs it; NEVER create scratch/parallel DB. Make actual deliverable progress within 45 minutes. No loops, broad suites, unnecessary Host restarts or DB clones. Existing 071/072 schedule capability and 069/070 option-level rate resolution are locked against regression. Capacity remains CONFIGURATION ONLY until later atomic enforcement.

PREFLIGHT — STOP ON CONFLICT

On BRIDGE-WAKE claim exactly ONE Pending task from tooba-main; preserve exact received .task.md under docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-073.task.md unstaged. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md if present, 069–072 planning/evidence, ShippingRatePolicy/ShippingRatePolicyEngine/ShippingRateQuoteLookup, Shipping Contracts/EF mappings/migrations, Order shadow input, and current carrier service-option identity. Do not use unrelated findings register.
git fetch origin; verify HEAD == origin/main and ancestry from Expected-Base-SHA. Accept only path-by-path proved nonoverlapping evidence/bookkeeping fast-forward; overlapping source drift => RECOVERY_CONFLICT/STOP. Preserve all unrelated dirty/untracked paths. Never reset/stash/rebase/force-push. If direct DB access is needed, positively prove Development host and resolved tenant-alpha/tooba_alpha target before writes; no Production/staging connections.
Trace existing policy precedence and currency/threshold/free-shipping behavior before changing ANY production path. Identify any quote request missing measured parcel inputs. Do not invent weights, quantities or billable data from cart subtotal.

MISSION — AN IMPLEMENTED WEIGHT / QUANTITY PRICING FOUNDATION
A. In Shipping-owned Domain + Contracts, implement explicit optional bounded weight and quantity conditions for an option-/service-/geography-scoped rate rule. Use one canonical weight unit (prefer grams as nonnegative integer to avoid floating errors); quantity is a nonnegative or positive integer with a documented shipment-item-count definition. State exact inclusive/exclusive bounds and whether open ends are supported. Unspecified inputs must NOT accidentally match restricted tiers. Preserve unchanged pricing semantics for existing unconditioned rates and old clients.
B. Define deterministic precedence across option identity, destination specificity, and weight/count tiers; do not resolve overlapping equally-specific active rules by row order. Reject ambiguous tiers or return an explicit stable conflict. Preserve original 069 priority (option-specific beats service-level; most-specific destination inside the option tier) unless documented and tested extension demands otherwise. Avoid creating a second pricing engine. Handle currency equality, free-shipping threshold, and missing/unknown measurements fail-closed for conditioned rules. Document numeric limits, zero-weight/zero-quantity behavior, input validation, boundaries and non-overlap rules.
C. Persist tier/range definitions in Shipping-owned EF persistence with a narrowly scoped additive migration. Do not rewrite/delete existing 9 rate policies or 7 grants; old rows MUST continue to mean unconditioned policies with the same results. A new table or optional columns are acceptable if uniqueness and exclusion/conflict semantics are proven and backward compatible. Keep admin write/read DTOs and validations complete for future 074 editor; update the existing module-owned admin CQRS paths rather than providing an unusable persistence-only abstraction. Reuse Fulfillment.Contracts for service and option validation, Geography.Contracts for place identity; no foreign DbContext or cross-module FK.
D. Extend the Shipping quote request Contract with optional measured parcel weight and item count, preserving all old call sites. If live Order currently lacks trustworthy physical package weight or item-count semantics, do NOT derive an invented measurement and do NOT change Checkout behavior. Expose a focused Shipping-level evaluation proof with explicit synthetic measured values. Do not activate quote authority or mutate customer paid snapshots. Existing schedule/coverage behavior unchanged.
E. Focused acceptance examples, synthetic DEVELOPMENT ONLY: one price tier for weight 0–2000 grams and another 2000–5000 grams (choose and DOCUMENT non-overlapping boundary, e.g. [0,2000) and [2000,5000)); count tier 1–5 and 6–10; demonstrate boundary at 1999/2000 and count 5/6, missing measurement refusal, invalid reversed/overlapping ranges refusal, combined weight+count intersection and stable no-match outcome. Demonstrate option-specific standard/express do not leak, destination/coverage and legacy unconditional rows remain correct. Use targeted Shipping tests and, if safe and necessary, one existing-Development admin HTTP write/read against synthetic fixtures with rollback ledger. Never run broad Host.Tests against locked live DLLs.
F. Evidence must distinguish CONDITIONED rate pricing from future packaging fees, payer mode, schedule availability and atomic capacity. Explicitly reserve those capabilities for later reviewed tasks, without pretending they are already implemented. Keep result and test logs bounded; stop when focused proof is sufficient.

ALLOWED PATHS

src/backend/Modules/Shipping/Tooba.Shipping.Contracts/**
src/backend/Modules/Shipping/Tooba.Shipping.Domain/**
src/backend/Modules/Shipping/Tooba.Shipping.Application/**
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/**
src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/** (ONLY if existing admin rate endpoints require DTO/validation forwarding; no new page)
src/backend/Modules/Shipping/Tooba.Shipping.Tests/**
docs/planning/shipping/SHIPPING-FULL-OPERATIONS-073.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-073/**
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-073.task.md (receipt ONLY, never stage)

FORBIDDEN
No Order/Checkout/Payment/Settlement/Fulfillment/Geography/Host production-file edits, no frontend changes in 073 (the 074 editor follows Architect review); no unapproved rate amounts or nationwide coverage grants; no global startup migrator, no mass MigrationRunner, no production/staging DB writes, no parallel database, no arbitrary policy overwrite, no speculative parcel metrics, no bypass or Results.Json, no weaker architecture guards/baselines, no unbounded retries or prolonged investigations. No automatic next implementation task. Unrelated dirty files must remain untouched.

ACCEPTANCE / STOP
PASS requires working persisted Shipping-owned weight/count rate rules with clear half-open range semantics, deterministic nonambiguous precedence, backward-compatible old-rate behavior, admin rate DTO/command integration, migration, targeted tests proving 1999/2000 and 5/6 boundaries, missing-input and overlapping-rule guards, and unchanged legacy Checkout authority. If a safe vertical slice cannot meet these criteria within 45 minutes, return PARTIAL with exact evidence and unfinished work; do not claim a mere design is PASS. Confirm no existing service option, Geography, schedule or coverage behavior was weakened. Record actual elapsed and all affected paths. Commit/push only allowed paths and fetch origin; Worker PASS != Architect ACCEPT.

RESULT CONTRACT — BRIDGE-WAKE-V1
The Worker must replace every placeholder with a real value or NONE before sending ONE canonical result string through Bridge; append no notes after END_TOOBA_TASK. Body belongs between automaticNextImplementationTask and END_TOOBA_WORKER_RESULT. Use the fixed tail order.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-FULL-OPERATIONS-073
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-072
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_WEIGHT_QUANTITY_PRICING_DOMAIN_AND_PERSISTENCE
Bridge-Task-Row: <actual Bridge row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: b6ec9c5926ff85a45920e8dd5d0883cb82877ad5
Base-SHA: <actual base SHA>
Code-SHA: <actual SHA or NONE>
Evidence-SHA: <actual SHA or NONE>
automaticNextImplementationTask: NONE
<ownership/root cause; weight and count tier semantics; deterministic precedence; DB migration/rollback; HTTP admin integration; 1999/2000 and 5/6 proof; old-quote compatibility; targeted tests; target and process safety; elapsed; changed paths; commit/push; remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
