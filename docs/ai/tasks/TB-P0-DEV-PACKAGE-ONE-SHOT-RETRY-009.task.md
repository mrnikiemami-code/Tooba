PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-P0-DEV-PACKAGE-ONE-SHOT-RETRY-009
Parent-Task: TB-P0-DEV-HOST-DEPLOY-AUTHORIZED-008
Program: TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: ONE_SHOT_DEV_PACKAGE_RECOVERY_WITH_HARD_STOP
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 207dae32fd899584d8057405901593aecf222e45

OBJECTIVE
Validate the actual fixed consolidated-package dispatch against the single known FAKE DEVELOPMENT package MP-01A12F796EF1. No code changes. Bounded one-shot execution; STOP for Architect review. User has stated the database is fake/development and wants one practical targeted proof, not repetitive testing. This authorization does NOT extend to production or any other package.

PREFLIGHT (READ ONLY; FAIL CLOSED)

Read AGENTS.md, .cursor/skills/tooba-backend-feature/SKILL.md and relevant 003/004/004R/005/008 receipts. Do not search unrelated files or deferred findings.
Confirm origin/main == HEAD == expected SHA; no substantive tracked code changes. Confirm the running process on 127.0.0.1:5088 is Development, tenant-alpha/tooba_alpha, and LIVE loaded Host, Inventory, Fulfillment assembly ProductVersion == expected SHA. If not, STOP without any write.
Confirm inventory migration 20261012110133_AddReservationConsumptions is recorded, table and PK exist, using SELECT only. If not, STOP; do not run migrations.
Confirm target package id 01a12f79-6ef1-7000-8578-dd7cc841195d, number MP-01A12F796EF1, tracking 125444, status Created; EXACT active members: shipment 01a12f79-1ad4-7000-9174-3237c0d645bf already Dispatched with tracking 125444-01A12F79, and 01a12f79-33ed-7000-b7ad-e562dcf67be8 still Created. Confirm first member's five lines ReservationConsumed=true and other member's line ReservationConsumed=false, Held reservation 01a12e97-ef78-7000-9854-7b1a4affaad3 with ordered qty 5. Confirm no journal row for that reservation. If any state differs, STOP, report current state; no retry.
Ensure proper admin authorization/actor from existing approved dev workflow; never bypass authentication or hardcode credentials. Confirm action projection includes dispatch_consolidated_package with matching package ID; if not, STOP.

ONE ACTION ONLY
Issue EXACTLY ONE normal authorized admin dispatch_consolidated_package operation for the target package, using the standard Host API or UI. Do not fabricate direct SQL changes or call internal methods; do not issue a second attempt on timeout, ambiguous response, exception or non-2xx. Record request code/target identity and sanitized response; exclude tokens/secrets. No parallel calls, no loops, no cancellation, no manual tracking edit.

POSTCHECK (READ ONLY)
Read target package, both shipments, six lines/reservations, Inventory consumption journal and relevant stock_position. Expected: package Dispatched; already-Dispatched shipment remains unchanged incl. original tracking, no new journal rows falsely created for its five historical consumptions; previously Created shipment Dispatched, collision-free full GUID tracking suffix, its reservation Consumed with exactly one journal record and correct stable consumer key, fulfillment ReservationConsumed=true; stock decremented exactly once. If response ambiguous, collect evidence only; NEVER retry or repair automatically.

TIME / TEST BUDGET
Single preflight, one mutation maximum, one postcheck. No dotnet build, full test suites, AMSC, migration, schema edits, code edits, Host restart, backups unless needed by a newly revealed hard safety blocker. Target <= 20 minutes, STOP with partial report if blocked instead of exploring. This task must not run against Production.

REPORT
Return GO/NO-GO and precise before/after evidence, Host loaded SHA, API status, package/member/reservation/journal/stock deltas, any discrepancy, and whether user-visible bug is actually fixed. Do not claim success from HTTP status alone. No commit needed for read-only evidence; do not push unrelated files. STOP; no automatic next task.
END_TOOBA_TASK
