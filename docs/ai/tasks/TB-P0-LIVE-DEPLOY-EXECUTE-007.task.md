# TB-P0-LIVE-DEPLOY-EXECUTE-007 — Task Receipt

Persisted claim artifact — full Architect body received via Bridge
`GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
at Bridge task UUID `b90a6cd0-4a12-46a7-a841-b5754d45cb5b` (createdAtUtc 2026-10-12T22:11:09.7612717Z).

* Task-ID: `TB-P0-LIVE-DEPLOY-EXECUTE-007`
* Parent-Task: `TB-P0-LIVE-DEPLOY-READINESS-006`
* Program: `TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY`
* Channel: `tooba-main`, WorkerId: `tooba-worker-01`, AgentType: `cursor`
* Mode: `BOUNDED_DEV_HOST_DEPLOY_WITH_HARD_STOP_GATES`
* Expected-Starting-HEAD: `207dae32fd899584d8057405901593aecf222e45`
* Skill: `.cursor/skills/tooba-backend-feature/SKILL.md` (read before coding)

## Objective as received

Deploy the already-reviewed backend fixes to the DEVELOPMENT Host on 127.0.0.1:5088 ONLY, and prove
exact running-binary provenance. Do NOT retry or mutate the real consolidated package. Small, bounded
operation; no exploratory loops.

## Pre-flight (read-only, one pass)

1. Read AGENTS.md, `.cursor/skills/tooba-backend-feature/SKILL.md`, report 006 and evidence for
   003/004/004R/005. Verify HEAD == origin/main == expected; tracked production diff must be empty.
   If not, STOP with exact discrepancy. Never reset/rebase/stash/force-push.
2. Confirm target process/launcher, executable path, listener 127.0.0.1:5088, Environment=Development,
   target database=tooba_alpha. If target is production/ambiguous or another service shares path or
   port: STOP. Do not stop unrelated processes.
3. Verify target migration `20261012110133_AddReservationConsumptions` is recorded on `tooba_alpha`
   AND `inventory.reservation_consumptions` has expected PK/columns, SELECT-only. Do NOT execute
   migrations, backfill, UPDATE/DELETE, order actions, or run any live dispatch.
4. Record running binary ProductVersion and backup path, checksum, and a nonsecret process/service
   restart plan. Back up the exact CURRENT working host binaries/config required for rollback,
   without revealing credentials. If unable to take/verify a safe rollback copy: STOP BEFORE restart.

## Execution safety

Service interruption requires explicit operator permission. If no unambiguous authorization to
restart the DEVELOPMENT Host has been conveyed in the active Cursor session/task context, report
`READY_AWAITING_OPERATOR_RESTART_APPROVAL` and STOP. Do NOT infer approval for production or any
DB/order mutation. When authorized, only the specified Development Host may be stopped, rebuilt and
restarted.

## Deploy (after gates)

5. Build only the backend Host from the verified commit (not `--no-build`), without changing source;
   perform at most ONE build attempt. If locked outputs prevent it, use staged isolated output; do not
   fight file locks, kill unknown processes, or retry loops. If failure: STOP and report.
6. Perform ONE controlled restart of the authorized Development Host, preserving its established
   launch arguments/environment and the verified rollback copy. If restart/health fails, perform at
   most ONE rollback to the saved binaries and STOP; never improvise changes to live database.
7. Verify ACTUAL running PID/listener plus ProductVersion/SourceRevisionId of loaded Host,
   Inventory.Infrastructure and Fulfillment.Infrastructure assemblies match the intended commit and
   have coherent timestamps. A successful build alone is NOT deployment proof. Read-only GET/health
   only; no order POST/PATCH.
8. Re-read package MP-01A12F796EF1 + both member shipment states and Inventory reservations/journal
   in SELECT-only mode. Compare with report 006; confirm no unapproved data mutation. Never send
   dispatch/cancel/retry/assign tracking.

## Budget

Aim 10-20 minutes. No full test sweep, AMSC, broad guards, browser automation, multi-step diagnosis,
build retries, or speculative refactoring. 0 code edits. At most one scoped build, one restart and one
rollback if necessary. STOP if work expands; return partial evidence and blockers.

## Report required

Exact before/after HEAD, command/pid and service target, migration SELECT witness, backup and checksum,
build result, listener + loaded assembly ProductVersions, post-deploy SELECT snapshot of package,
GO/NO-GO for a SEPARATE authorized package retry; state clearly if deployment NOT executed. Do not
expose secrets. Preserve docs-only evidence locally if needed, no automatic unrelated commit. STOP for
independent Architect acceptance; DO NOT start 008.
