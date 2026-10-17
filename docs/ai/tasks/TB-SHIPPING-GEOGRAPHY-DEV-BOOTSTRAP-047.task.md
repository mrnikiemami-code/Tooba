Received Bridge task artifact (row f3d2386e-f293-4881-bbb3-ed844a99c276) — RECEIPT ONLY, NEVER STAGED.

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047
Parent-Task: TB-SHIPPING-GEOGRAPHY-RUNNER-VERIFY-046
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_GEOGRAPHY_DEVELOPMENT_BOOTSTRAP_ONLY
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 85dd0fda2960674c7209ffcf5b9e722424a413d0
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT AUTHORITY

Architect ACCEPT for 046 evidence. This is task 2 of 6 for Shipping MVP. No separate planning task.
Authorized ONLY on an independently verified local Development database of Tooba: apply the Geography-only initial EF migration and bootstrap minimal explicit geographic reference data from the existing 12-province legacy list. Production, staging, shared/remote, marketplace-wide and unrelated tenant databases remain FORBIDDEN.
No authorization to restart Host or register Geography's startup migrator. Never use Tooba.MigrationRunner apply for this step: it targets all 32 modules.
Keep D3/D8/D9 and ARCH-COMPLETE-002 locks. This is a bounded setup slice, NOT a Checkout cutover or a claim that Shipping is finished.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one received Bridge task following BRIDGE-WAKE. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md, ADR-036, 042/046 evidence and actual DbContext/migration/legacy constant. Do not inspect unrelated audit register.
git fetch origin; main and HEAD == origin/main == Expected-Base-SHA mandatory. Preserve unrelated dirty work; never reset/stash/rebase/force push. Unsafe state -> RECOVERY_CONFLICT.
Positively identify intended LOCAL DEVELOPMENT database using existing project configuration and process/container identity, verify database name, endpoint, host, role and tenant and prove it is NOT production, staging, shared or another tenant. No assumptions from a default environment variable. If ambiguous, STOP BLOCKED with no DB access.
Before any DDL: obtain a restorable database backup / verified safe recovery point, or STOP BLOCKED. Record exact provenance and rollback method. Never echo credentials in logs/results.

MISSION — ONE BOUNDED ACTIVATION SLICE
A. Using a dedicated scoped EF Geography migration operation ONLY (not runner apply), apply exactly 20261014113949_InitialGeography to the proven local DEV database. Inspect pending/applied state first; if unexpected schema/migration drift or an existing nonempty geography schema, STOP without overwrite. Do not touch another module's migrations/history. No Host restart.
B. Bootstrap ONLY the currently supported geography values from StorefrontIranGeography.Provinces: country IR and its existing province/city values, with deterministic stable IDs/codes and Persian names; use an idempotent, explicit, reviewable SQL/script in task-owned evidence (not ad hoc invisible writes). Do not invent missing provinces, cities, shipping service coverage, foreign countries, districts or arbitrary nationwide enablement. Resolve duplicate display names by (parent identity, place identity); fail on ambiguous or untraceable mapping rather than guessing.
C. Verify country/area/city counts vs source, parent integrity, translations, idempotent rerun, composite FKs and both ancestry triggers. Verify old GET /v1/storefront/geography/provinces and existing Checkout behaviour are untouched by source changes; no runtime smoke against payment.
D. If a safely reproducible bootstrap cannot fit timebox, stop PARTIAL and preserve evidence; do not rush a bad mapping. Document precise follow-up blocker, not broad redesign.
E. Produce a concise report: destination proof, backup proof, exact DDL/seed operations, outcomes, actual elapsed, schema-only rollback plan, risk and ONE recommended next bounded implementation step. Never expose credentials.

ALLOWED PATHS

docs/planning/shipping/SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047.md (new)
docs/architecture/evidence/TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047/** (scripts/logs; scrub credentials)
docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047.task.md (received receipt only; DO NOT STAGE)
One verified LOCAL DEV geography schema/database migration + geographically scoped seed rows as above, only after all preflight gates pass.

FORBIDDEN

No production C#/TS edits, new endpoint, migration source/model changes, Host startup migrator, DI wiring, module manifest/guards changes, appsettings changes or Host restart.
No Tooba.MigrationRunner apply (all descriptors), no DDL or DML against other schemas; no staging/production/shared/remote databases, PSP/payment/paid checkout/fulfillment actions.
No broad testing, nonessential docs, secret exposure, probing Bridge /api/results or automatic next task.

ACCEPTANCE / STOP

PASS only if DEV identity and backup are proven, scoped migration and minimum legacy-derived reference data are repeatably applied and checked, existing consumers untouched, authorized evidence committed/pushed and HEAD == origin/main.
PARTIAL if safe migration succeeds but bootstrap cannot be completed and verified; report exact state and stop. BLOCKED before mutation if provenance/backup uncertain; don't force progress.
Worker PASS != Architect ACCEPT. Return complete result, return IDLE, no polling.

RESULT CONTRACT — BRIDGE-WAKE-V1

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047
Parent-Task: TB-SHIPPING-GEOGRAPHY-RUNNER-VERIFY-046
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_GEOGRAPHY_DEVELOPMENT_BOOTSTRAP_ONLY
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 85dd0fda2960674c7209ffcf5b9e722424a413d0
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE

<preflight; proven target and backup; exact migration/seed state; tests; changed paths; elapsed; push; outstanding risk>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
