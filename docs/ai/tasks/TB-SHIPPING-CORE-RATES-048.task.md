Received Bridge task artifact (row fe91ca9e-95bf-4b46-afc0-cffa4bdd567b) — RECEIPT ONLY, NEVER STAGED.

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-CORE-RATES-048
Parent-Task: TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-SIX-SLICES
Mode: BOUNDED_SHIPPING_RATE_AUTHORITY_FOUNDATION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: fcf91e30e21675c94d40bd7eab6df55127e52f6f
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD
Milestone: 3 OF 6

ARCHITECT AUTHORITY

047 ACCEPT for the DEV Geography bootstrap only: geography schema, 12 legacy provinces and 47 cities loaded in tooba_alpha. No checkout migration/switch authorized.
User requires a professional, minimal sellable Shipping MVP in six consolidated slices, without compromising TMAR architecture. Milestone 3 of 6 is the shipping-rate authority foundation; avoid creating a second Admin shipping-services page.
Existing /admin/shipping-services is the canonical Admin surface. Existing Fulfillment shipping service/option CRUD remains authoritative for service identity/display during transition. Shipping owns commercial rate and coverage policy, not Fulfillment; Order holds the customer-facing promise. Do not silently move or duplicate existing CRUD.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one Bridge Task after BRIDGE-WAKE. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md, docs/architecture/SHIPPING-ARCHITECTURE.md, 034 audit, 036 ADR, and source relevant to Fulfillment shipping catalog, Order calculator and Geography.Contracts.
git fetch origin and require main / HEAD == origin/main == Expected-Base-SHA; preserve unrelated dirty/untracked files. No reset/stash/rebase/force push.
Identify the actual canonical feature structure, CQRS/MediatR 12.5, Contracts-only boundaries, namespace-path convention, and existing shipping service codes. Stop rather than guess if required ownership conflicts with certified locks.

MISSION — ONE BOUNDED VERTICAL FOUNDATION
A. Implement one module-owned, non-runtime Shipping rate-policy foundation in new src/backend/Modules/Shipping projects only, with capability-first folders and existing project conventions. Choose the smallest coherent Domain + Contracts + Infrastructure set that can build within the hard timebox. Shipping owns structured quote/rate rules; never call existing Fulfillment internals or read its DbContext.
B. Define stable rate-policy identity and parameters scoped to existing shipping service/option CODES (not duplicate service catalog rows): country/city/service-area IDs where applicable, charge amount/currency, preparation/lead days as distinct concepts, optional free-above-threshold, effective/active status. Model an explicit, bounded eligibility result: a missing/unknown coverage grant must NOT silently yield unconditional serviceability. Preserve the historic empty AllowedProvinces semantics in the CURRENT live route, which this task must not touch.
C. Include an actual minimal owned EF mapping for proposed Shipping rate/coverage data ONLY if achievable safely after A/B; otherwise deliver a complete Contracts + Domain vertical core with a clear NOT-WIRED/NO-MIGRATION report and mark PARTIAL for persistence. Do not claim runtime pricing is now DB-backed until it truly is. Avoid designing carrier payables, international polygon coverage, full scheduling and subsidy ledger in this slice.
D. Add only focused tests/validation of amount non-negativity, identity/scope and fail-closed eligibility policy. Build changed projects; do not run a broad suite.
E. Record exactly what is implemented vs deferred. One concise report with a strict scope ledger and ONE next recommendation supporting milestone 4/6 (Checkout/Order binding). STOP if 30-minute limit is reached.

ALLOWED PATHS

src/backend/Modules/Shipping/** (NEW module-owned foundation ONLY; no preexisting path overwrite)
src/backend/Tooba.slnx (additive new project entries ONLY, if needed)
docs/planning/shipping/SHIPPING-CORE-RATES-048.md (NEW concise report)
docs/architecture/evidence/TB-SHIPPING-CORE-RATES-048/** (focused evidence only)
docs/ai/tasks/TB-SHIPPING-CORE-RATES-048.task.md (received receipt only, NEVER STAGE)

FORBIDDEN

NO changes to Host, Tooba.MigrationRunner, ModuleSchemaMigrationOrder, any existing Order, Fulfillment, Payment, Settlement, AddressBook, Geography, Catalog, Offer or frontend file.
NO edits to /admin/shipping-services or creation of a parallel Admin page in this slice. Existing Fulfillment service/option codes must remain usable.
NO migration generation or application, database writes/reads, Host restart/deploy, service switch, endpoint or runtime DI integration, live checkout behavior or paid-order actions.
NO blind national coverage grants, fabricated provider slots, amount reallocation, cross-module DbContext reference or weakening of TMAR/visual locks.
NO dummy/probe Bridge /api/results calls; no automatic follow-on task.

ACCEPTANCE / STOP

PASS only if new Shipping module foundation compiles; commercial ownership is unambiguously Shipping; explicit reference to existing service codes is compatibility-only; basic rate + coverage rules have focused evidence; NO runtime behavior changed; only authorized files are committed/pushed; full SHA and elapsed reported.
PARTIAL if a coherent persistence or tests cannot fit; never mask gaps. BLOCK/RECOVERY_CONFLICT on genuine scope/architecture/repo conflict. Worker PASS != Architect ACCEPT.
Commit only task-owned files, push origin main, git fetch, verify HEAD == origin/main. STOP_FOR_ARCHITECT.

RESULT CONTRACT — BRIDGE-WAKE-V1

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-CORE-RATES-048
Parent-Task: TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-SIX-SLICES
Mode: BOUNDED_SHIPPING_RATE_AUTHORITY_FOUNDATION
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: fcf91e30e21675c94d40bd7eab6df55127e52f6f
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<changed paths; observed tests/build; ownership and compatibility; runtime non-actions; actual elapsed; push evidence; exact remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
