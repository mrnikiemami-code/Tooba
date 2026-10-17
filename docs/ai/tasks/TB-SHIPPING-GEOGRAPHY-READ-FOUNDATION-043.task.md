PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043
Parent-Task: TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_GEOGRAPHY_READ_FOUNDATION_NO_HOST_WIRING
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: dd3d334b2322b30437b953cb45f5707033e58718
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT AUTHORITY

Architect ACCEPT for Task 042: both INSERT/UPDATE ancestry triggers and isolated PostgreSQL Up/Down/Up and concurrency verification accepted. READ COMMITTED-only ancestry writes are the approved rule; higher isolation must fail closed. This does NOT authorize migration on any Tooba database.
Deliver the bounded Geography read-side implementation over the existing model and previously approved Contracts. No changes in production shipping or Checkout behaviour.
One task only; never self-authorize next work.

PREFLIGHT — STOP ON CONFLICT

Execute only when claimed from Bridge after BRIDGE-WAKE on tooba-main; claim exactly one Pending task. No idle polling.
Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md, ADR 036, report 042, Geography Contracts and DbContext, and actual module patterns for Application/Infrastructure dependency direction. Do not scan the deferred findings register.
Fetch origin and verify main, HEAD == origin/main == Expected-Base-SHA. Preserve unrelated dirty/untracked files without touching/staging them. On conflict STOP RECOVERY_CONFLICT; no reset, stash, rebase or force-push.
Verify target paths do not already exist or contain user work; stop rather than overwrite. Inspect current Domain/Contracts signatures before implementation.

MISSION — BOUNDED READ CAPABILITY
A. Implement the existing IGeographyLookup and IGeographyLabelLookup read-only Contracts in Geography Infrastructure, using its own GeographyDbContext. Implement list-by-country/area/city and GetByIds with validated Country/Area/City/ServiceArea ancestry and active-state semantics. No cross-module DbContext reads.
B. Implement ResolveLegacyAsync as a strictly read-only, deterministic resolver; no fuzzy auto-selection or implicit insert. Distinguish Resolved, Ambiguous and Unresolved; identical labels in different areas must not be collapsed. If language ambiguity makes a legacy value unresolvable, return Unresolved/Ambiguous rather than guess. Keep source facts versus proposed semantics explicit.
C. Geography labels: resolve requested LanguageId and defined fallback safely. If the Localization-default lookup cannot be wired within this module without unauthorized cross-module reference, keep fallback unimplemented with an explicit PARTIAL and report; do not invent a global translation service. No fabricated records or country-wide seeds.
D. Add only minimal internal Application project if actually necessary under .cursor backend architecture skill; otherwise implement read adapters directly in Infrastructure behind existing Contracts (avoid empty project scaffolding). Add new Geography project(s) to Tooba.slnx only if created. Do not wire Host/ModuleSchemaMigrator and do not add endpoints or consume it from Order/AddressBook.
E. Verify focused build of changed Geography project(s), and deterministic focused read/resolve cases in a disposable task-exclusive database if safely available, OR report exactly which runtime cases remain unverified. Do not apply migrations to any Tooba database. A disposable database may use the already-validated 042 migration source only if task-exclusive and independently confirmed. No broad solution tests.
F. Create exactly one concise report at docs/planning/shipping/SHIPPING-GEOGRAPHY-READ-FOUNDATION-043.md, with actual file/line evidence, bounded checks, unknowns, time spent and exactly one next-step recommendation.

ALLOWED PATHS

src/backend/Modules/Geography/Tooba.Geography.Infrastructure/** (read-only adapters and module-local support only; NEVER modify existing migration SQL or schema model in this task)
src/backend/Modules/Geography/Tooba.Geography.Application/** (ONLY if required by established architectural pattern and actually used)
src/backend/Tooba.slnx (ONLY additive reference for a new project)
docs/planning/shipping/SHIPPING-GEOGRAPHY-READ-FOUNDATION-043.md (new report)
docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043.task.md (received receipt only, do not stage)
docs/architecture/evidence/TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043/** (local evidence only)

FORBIDDEN

NO Migration edit/generation/application to Tooba database, no schema changes, no seeds/imports, no shared DB; no Host/DI registration, no ModuleSchemaMigrationOrder, no Endpoint, no frontend or Order/AddressBook/Fulfillment/Payment/Settlement/Catalog/Offer edits. No shipping rates, coverage policies, fail-closed Checkout switch, carrier settlement, manifest certification or architecture guard changes.
No unrelated edits or service restarts; never POST dummy/probe payload to Bridge /api/results. No automatic next task.

ACCEPTANCE / STOP

PASS only if contracts are implemented correctly, no unauthorized wiring/data changes, focused build succeeds and report distinguishes verified outcomes from unverified runtime aspects. If work cannot be safely completed in 30min, report PARTIAL and STOP; no scope expansion.
Commit and push only task-owned changes, fetch and verify HEAD == origin/main. Report real elapsed time and exact SHA. Worker PASS != Architect ACCEPT.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043
Parent-Task: TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_GEOGRAPHY_READ_FOUNDATION_NO_HOST_WIRING
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: dd3d334b2322b30437b953cb45f5707033e58718
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE

<preflight; exact changed paths; contract implementation; resolution cases; targeted validation; disposable DB safety if used; runtime unknowns; actual elapsed time; commit/push; remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
