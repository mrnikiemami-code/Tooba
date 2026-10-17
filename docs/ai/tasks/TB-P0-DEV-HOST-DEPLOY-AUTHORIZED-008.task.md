# TB-P0-DEV-HOST-DEPLOY-AUTHORIZED-008 — Task Receipt

Persisted claim artifact — full Architect body received via Bridge
`GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
at Bridge task UUID `c78c2cd5-2d82-4844-96d4-e170cb7bd1d8` (createdAtUtc 2026-10-13T01:42:36.6569543Z).

* Task-ID: `TB-P0-DEV-HOST-DEPLOY-AUTHORIZED-008`
* Parent-Task: `TB-P0-LIVE-DEPLOY-EXECUTE-007`
* Program: `TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY`
* Channel: `tooba-main`, WorkerId: `tooba-worker-01`, AgentType: `cursor`
* Mode: `BOUNDED_AUTHORIZED_DEV_HOST_DEPLOY_STOP_FOR_ARCHITECT`
* Expected-Starting-HEAD: `207dae32fd899584d8057405901593aecf222e45`
* Skill: `.cursor/skills/tooba-backend-feature/SKILL.md` (read before coding)

## Operator authorization (explicit)

The operator answered «بله» to the specific request to temporarily stop, rebuild, and restart ONLY the
DEVELOPMENT Tooba.Host listening on 127.0.0.1:5088 and verify health. This does NOT authorize
production deployment, SQL or migration writes, inventory/order/package mutations, package
dispatch/cancel/retry, or any other service interruption.

## Goal

Finish deploy steps 5–8 previously stopped at the authorization gate in
`TB-P0-LIVE-DEPLOY-EXECUTE-007`. Do not redo broad reconnaissance. Hard wall-clock budget 20 minutes;
no loops; STOP and report partial state on timeout/blocker.

## Preflight (read only, narrow)

Read AGENTS.md, `.cursor/skills/tooba-backend-feature/SKILL.md`, and 007 result/rollback manifest.
Confirm HEAD == origin/main == Expected-Starting-HEAD and no substantive tracked diff. If HEAD changed,
STOP (do not deploy a new SHA). Confirm the exact running Development Host PID, executable path,
127.0.0.1:5088 listener, Development / dev-singlestore / tenant-alpha identity. Confirm rollback backup
directory and checksum manifest from 007 exist and the critical host/inventory/fulfillment binaries
compare to the backed-up old files. If any verification fails, STOP without interrupting service.
Confirm target migration `20261012110133_AddReservationConsumptions` is ALREADY APPLIED to `tooba_alpha`
using SELECT-only; if not, STOP. Do not run migrations.

## Authorized deploy (one bounded attempt)

4. Stop ONLY the verified Development Tooba.Host PID 15068 (or its newly verified successor) and its
   verified launcher, release port 5088. Do not stop other processes/services. Build exactly once:
   `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Debug` from the repo root; NO
   `--no-build` for build. Preserve the existing rollback backup.
5. Start Host under explicitly configured `ASPNETCORE_ENVIRONMENT=Development`; never rely on launch
   profile/environment inheritance. Use repository-root working directory and normal startup command.
   Do not trigger startup migrations if the startup path would write schema; STOP if deployment cannot
   meet no-migration constraint.
6. Verify by read-only observations: listener 127.0.0.1:5088, running PID/command line/environment,
   actual loaded Host/Inventory.Infrastructure/Fulfillment.Infrastructure assembly
   ProductVersion/SourceRevisionId = Expected-Starting-HEAD; GET `/health`, `/health/ready` and
   `/__platform-commerce` (must remain Development, dev-singlestore, tenant-alpha). Verify package
   MP-01A12F796EF1 state via SELECT only; it must remain Created with member states unchanged. Report
   all actual results, not merely build output.

## Failure / rollback (at most ONE)

If build fails before stopping, do not stop Host. If stop/start/health/provenance checks fail after
interruption, execute ONE rollback using 007 verified backup, restart with
`ASPNETCORE_ENVIRONMENT=Development` and `--no-build`, verify old ProductVersion 92d2160 and health;
STOP and report. Never retry builds/restarts or branch to unrelated fixes. If rollback fails,
immediately report host DOWN and precise blocker; do not conceal it.

## Strict scope

No production code edits, no test changes, no commits/pushes, no migration apply/backfill, no SQL
writes, no HTTP POST/PATCH/DELETE, no order/package dispatch/cancel/retry or tracking assignment, no
production endpoints, no test suites/AMSC. Only the authorized DEV Host stop/build/start and read-only
verification. Record concise evidence in
`docs/architecture/evidence/TB-P0-DEV-HOST-DEPLOY-AUTHORIZED-008` locally; no need to commit docs.

## Result required

Exact starting/ending HEAD; host PID before/after, command and environment; build exit code once;
actual loaded assembly revision for three critical assemblies; migration status read-only; health and
tenant identity; package snapshot unchanged; whether rollback occurred; GO/NO-GO for a separately
authorized package retry. STOP for Architect independent ACCEPT. NEVER execute the package retry as
part of this task.
