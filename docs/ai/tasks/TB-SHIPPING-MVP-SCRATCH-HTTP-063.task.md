PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MVP-SCRATCH-HTTP-063
Parent-Task: TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_SCRATCH_GEOGRAPHY_SHIPPING_MIGRATION_AND_AUTHORIZED_HTTP_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 49a1fd195e8d734122d99e8352cec6886c3aeff4
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT AUTHORITY
This is ONE bounded task, not authorization to migrate/restart the existing Tooba Development Host, touch existing databases, seed commercial shipping data, change Checkout, or issue a follow-on task. Worker PASS != Architect ACCEPT. Stop at 30 minutes, even if partial. Follow AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md.

PREFLIGHT — STOP ON CONFLICT

On BRIDGE-WAKE claim exactly one Pending task on tooba-main, preserve receipt verbatim, fetch origin; verify clean source ancestry from Expected-Base-SHA. Accept only individually proved evidence/bookkeeping fast-forward; any overlapping source drift -> RECOVERY_CONFLICT and STOP. Preserve ALL unrelated dirty files, no reset/stash/rebase/force-push.
Read recovery Shipping checkpoint in docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md, 059–062 reports/evidence, canonical EF design-time factories and migration scripts for Geography and Shipping, Development auth bootstrap, 060 scratch-target guard and 061/062 isolation launch recipe. Do not consult unrelated findings register.
Before any service/SQL/request, establish dedicated task-created postgres:17 disposable container with UNIQUE database tooba_063_scratch_<8-32 char sentinel>, dedicated role and loopback-only port distinct from 5432; NO Tooba live DB name exists in container. Create a fresh isolated Host launch directory. No inherited Tooba__*/ASPNETCORE__*/DOTNET__*/ConnectionStrings__* or external configuration. Use canonical hyphenated connection reference and one tenant; do NOT use the faulty tenant_alpha override. Run the existing 060 offline guard on actual launch config+override and verify config SHA256/fingerprint; prove effective connection target. In addition, before ANY WRITE verify database-side port/container/backend association and verify all resolved connection strings point solely at disposable target; inability to prove -> BLOCKED, no writes. Credentials never logged/committed. Never query 127.0.0.1:5432 or tooba_alpha, even read-only.

MISSION — ACTUAL USEFUL COMPLETION, NO NEW DESIGN
A. Prepare only the scratch database. Execute existing EF Geography and Shipping migrations explicitly, each scoped to its own DbContext/design-time factory with the exact scratch connection. DO NOT invoke the all-module MigrationRunner apply, do not add startup migrators, and do not modify migration sources, migrations order, Host, appsettings, or real tenant data. Run only indispensable prerequisites for canonical development admin auth (existing 062 fix); DO NOT enable RunLegacyBootstraps. For Geography place selections use the existing 047 verified seed generator/seed script against SCRATCH only, with genuine existing Localization fa-IR language id; if language/schema prerequisites unavailable, stop safely and report blocker rather than inventing ids. Do not fabricate rates/coverage for real businesses.
B. Boot CURRENT code Host in isolated launch directory/port only. Independently prove effective connection reference AND database-side pg_stat_activity point to scratch. Obtain admin capability via the existing canonical Development bootstrap; unauthorized GET must be 401/403 and authorized GET Geography and Shipping must be 200, not 500. If other missing dependencies obstruct, record the exact missing migration and run ONLY appropriately scoped existing migration on scratch if confidently safe; no production source architecture work.
C. HTTP regression on scratch: POST rate and POST coverage grant with absent scope must both return validation HTTP 400, stable shipping.validation.scope_required (not 500), with valid provisioned admin actor. Exercise one valid real-ID rate+grant create/read-after-write, rate edit and active toggle, grant active toggle. Verify invalid Geography id and cross-service mutation rejected with stable errors. Mark all created values explicitly synthetic, scratch-only; they are NOT live tariffs or checkout parity evidence. If any prerequisite cannot be met inside timebox, PARTIAL with precise blocker; never relax authorization or use bypass.
D. Focused proof: verify no customer quote/Checkout behavior touched; capture redacted HTTP statuses, stable error codes, migration history, before/after scratch row counts, source SHA, elapsed, cleanup; stop and destroy ONLY task-owned Host, scratch container/volume and temp secrets. Confirm user's Host and existing databases not accessed/restarted. No broad suites, no idle polling.

ALLOWED PATHS

docs/planning/shipping/SHIPPING-MVP-SCRATCH-HTTP-063.md
docs/architecture/evidence/TB-SHIPPING-MVP-SCRATCH-HTTP-063/**
docs/ai/tasks/TB-SHIPPING-MVP-SCRATCH-HTTP-063.task.md (Bridge receipt only, never stage)
No production source change is authorized. Any genuine source defect -> report exact evidence, stop; Architect will issue repair.

FORBIDDEN
No original Development/Production/Marketplace database connection, writes or cleanup; no original :5088 Host restart; no user frontend process restart; no generated roles/permissions/capability tuples outside canonical Development path; no invented rates or nationwide grants outside scratch; no payment/orders/snapshot/cutover; no mass migrations; no Host/Admin/Security guard changes; no bypass, no Results.Json, no new page; no unapproved modifications; no automatic next task.

ACCEPTANCE / STOP
PASS only with proven target isolation, scratch Geography+Shipping migrated, canonical admin authorized, both missing-scope POSTs return HTTP 400 with declared stable code, one complete authenticated scratch rate/grant CRUD roundtrip plus cross-service guard, and cleanup proof. PARTIAL if any real condition absent. Preserve evidence of failures; never claim production availability or shadow parity. HARD STOP at 30 minutes.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MVP-SCRATCH-HTTP-063
Parent-Task: TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_SCRATCH_GEOGRAPHY_SHIPPING_MIGRATION_AND_AUTHORIZED_HTTP_PROOF
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 49a1fd195e8d734122d99e8352cec6886c3aeff4
Base-SHA: <actual>
Code-SHA: NONE
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<target/config/database-side provenance; migrations; authorized HTTP 400 and CRUD matrix; safety; elapsed; exact paths; commit/push; remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
