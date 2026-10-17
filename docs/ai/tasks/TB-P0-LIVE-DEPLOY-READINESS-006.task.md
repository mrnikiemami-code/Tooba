# TB-P0-LIVE-DEPLOY-READINESS-006 — Task Receipt

Persisted claim artifact — full Architect body received via Bridge
`GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
at Bridge task UUID `6102245c-d997-4f19-b06b-52958a463a8b` (createdAtUtc 2026-10-12T21:33:08.5348891Z).

* Task-ID: `TB-P0-LIVE-DEPLOY-READINESS-006`
* Parent-Task: `TB-P0-PACKAGE-DISPATCH-W6-RECONCILIATION-005`
* Program: `TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY`
* Channel: `tooba-main`, WorkerId: `tooba-worker-01`, AgentType: `cursor`
* Mode: `READ_ONLY_DEPLOY_READINESS_STOP_FOR_ARCHITECT`
* Expected-Starting-HEAD: `207dae32fd899584d8057405901593aecf222e45`
* Skill: `.cursor/skills/tooba-backend-feature/SKILL.md` (read before coding)

## Objective as received

Within a single small bounded pass, establish whether the live Host and tenant DB are READY for a
separately approved deployment of Inventory journal migration and backend fixes (003/004/004R/005).
This task must not deploy, migrate, restart, dispatch, cancel or mutate anything.

## Preflight

Confirm current HEAD, origin/main, clean tracked status, and AGENTS.md plus
`.cursor/skills/tooba-backend-feature/SKILL.md` relevant rules. Verify exact migration
`20261012110133_AddReservationConsumptions` and the wiring version; do not assume GitHub HEAD equals
deployed Host. Read only targeted source and prior evidence, not the full project.

## Read-only checks (no more than five)

1. Determine running Host executable/build provenance and compare against commits `04e3aa79`
   (package), `e7d0bf97` (journal), `8f70763b` (wiring), `67776052` (fail-closed), `207dae32` (W6).
   If build provenance cannot be established, mark UNKNOWN; never infer from repo HEAD.
2. Inspect tenant `tooba_alpha` Inventory migration history (SELECT ONLY), check if
   `inventory.reservation_consumptions` exists and its expected PK/columns. No schema changes.
3. Check read-only status of package `01a12f79-6ef1-7000-8578-dd7cc841195d` and its member
   shipments/reservations (including status, ReservationConsumed, journal presence). Do not copy
   personal/order data beyond identifiers needed for proof.
4. Trace deployment order and compatibility: journal migration before any binary that executes keyed
   consume. Distinguish deploy-ready from operational recovery-ready. Identify required backup/rollback
   and how to verify without production writes.
5. Report GO / NO-GO / UNKNOWN for deployment and separately for the live package retry, with hard
   blockers and the smallest next action.

## Stop / budget

Small read-only inspection; ~10–15 minutes maximum, no repeated diagnostic loops or broad tests. If
blocked, STOP with partial evidence. 0 production files and 0 test files changed. No build or test run
unless an existing executable's version can be examined without side effects; do not run full suite /
AMSC / migration dry-run against live database. No SQL write (INSERT/UPDATE/DELETE), no migration
apply, no Host restart, no API mutation, no actual package dispatch, no cancellation, no post-deploy
action. Do not send or implement a next task. Do not autonomously conduct deployment or data repair.

## Deliverable

Return a concise `BEGIN_TOOBA_WORKER_RESULT` containing checked HEAD, deployed provenance (with
evidence or UNKNOWN), migration status, package/reservation read-only snapshot, ordered deployment
prerequisites, risks, GO/NO-GO decisions, and next recommended bounded task. Evidence can be returned
in the response; do not commit/push docs merely for reporting unless needed by project policy. STOP
for independent Architect acceptance.
