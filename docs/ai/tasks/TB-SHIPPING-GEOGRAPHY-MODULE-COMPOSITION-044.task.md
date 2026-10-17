PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044
Parent-Task: TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_GEOGRAPHY_MODULE_COMPOSITION_WITHOUT_RUNTIME_MIGRATION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 85509d7aa00535d18968099d4d961d5a81c9af61
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT AUTHORITY

Architect ACCEPT for 043 read foundation. GetByIds rejecting inactive places is accepted for NEW shipping requests only; history continues from frozen Order snapshots, never revalidated. Label fallback is requested language -> Localization default -> stable code.
Authorized: minimal Geography module-level DI composition and migration-order design/registration only if safe and verified, without applying migration or changing shipping runtime behavior.
Exactly one task. No self-authorized follow-up.

PREFLIGHT — STOP ON CONFLICT

Execute ONLY after Bridge claim following BRIDGE-WAKE on tooba-main. No idle polling.
Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md, ADR-036, report-043, actual schema-migrator order and Host composition conventions. Do not read deferred findings register.
git fetch origin, require main and HEAD == origin/main == Expected-Base-SHA. Preserve all unrelated dirty/untracked work; never reset, stash, rebase, force-push. STOP RECOVERY_CONFLICT on unsafe divergence.
Inspect real migration-order numbers and existing registration before editing. Do not guess a slot or renumber/reorder existing modules.

MISSION — ONE BOUNDED MODULE COMPOSITION SLICE
A. Create minimal module-owned Geography DI registration following the repository's canonical pattern; register GeographyDbContext and existing IGeographyLookup / IGeographyLabelLookup implementations via Contracts-only seams. Respect Geography's existing Localization.Contracts dependency, host composition only.
B. Wire Geography module in Host COMPOSITION ONLY if there is a clear non-behaviour-changing seam. Add migration-order registration ONLY if a unique, verified append-safe slot exists and the resulting startup will NOT implicitly execute an unapproved migration on a Tooba database. If runtime auto-migrates at startup, DO NOT register it in the running Host path; instead produce an exact blocked/activation plan in the report and mark PARTIAL. Never silently trigger DDL.
C. Keep Geography INACTIVE as a shipping authority: no existing Order/AddressBook/Checkout consumer changes, no switch from legacy StorefrontIranGeography, no serviceability policy switch, no new endpoint/admin UI, no seed or coverage rows. No revalidation of historical orders.
D. Do not claim ARCH-COMPLETE-002 certification. Inspect manifest/structure rules and report remaining missing projects/certification steps rather than inventing certification or weakening a guard.
E. Run only targeted builds and narrow architecture checks for touched components; if practical verify DI construction without service startup/DB contact. No full suite unless required by an existing touched guard. Record exact proof, uncertainty and actual elapsed time.
F. Produce exactly one concise report docs/planning/shipping/SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044.md. Clearly distinguish ACTIVE REGISTRATION from PREPARED-ONLY, note implicit migration hazards, and recommend ONE safe next slice.

ALLOWED PATHS

src/backend/Modules/Geography/Tooba.Geography.Infrastructure/DependencyInjection/** (new module composition)
src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Tooba.Geography.Infrastructure.csproj (only if necessary)
src/backend/Host/Tooba.Host/** (composition only, ONLY if preflight proves no implicit migration or runtime behaviour)
src/backend/Tooba.slnx (only an actually needed additive project ref)
docs/planning/shipping/SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044.md (new report)
docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044.task.md (received receipt only; never stage)
docs/architecture/evidence/TB-SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044/** (evidence only)

FORBIDDEN

No EF migration generation/edit/apply, no PostgreSQL Tooba DB connection, no schema update or seed, no Host restart, no change to ModuleSchemaMigrationOrder or migrator unless separately approved after actual auto-apply risk analysis, no endpoints, no AddressBook/Order/Fulfillment/Checkout/frontend change, no Shipping policy/coverage/rate change, no manifest certification, no guard weakening or unrelated edits.
No fake Bridge result/probe POST; no automatic next task.

ACCEPTANCE / STOP

PASS only if module-local registration compiles, any Host composition is demonstrably migration-safe, no forbidden paths changed, and report establishes activation state accurately. Otherwise PARTIAL with evidence and STOP; never force activation.
Commit/push task-owned changes only, fetch and verify HEAD == origin/main. Worker PASS != Architect ACCEPT.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044
Parent-Task: TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_GEOGRAPHY_MODULE_COMPOSITION_WITHOUT_RUNTIME_MIGRATION
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 85509d7aa00535d18968099d4d961d5a81c9af61
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE

<preflight; changed paths; DI/Host activation state; migration safety; focused checks; elapsed time; push evidence; remaining risks>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
