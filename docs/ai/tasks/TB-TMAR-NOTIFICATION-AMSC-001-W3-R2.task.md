# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2.task.md — Persisted claim artifact

Mode: STRUCTURE_REPAIR
Skill: tooba-architecture-structure
Title: Repair Notification use-case leaf over-foldering and restore a valid Structure handoff

Persisted claim artifact — full Architect body received via Bridge `GET /api/tasks/next?channelId=tooba-main` at claim row `6ea28cf8-a7eb-47d9-83c8-d9dc11c71130` (createdAtUtc 2026-10-06T16:25:29.7770698Z).

- Starting HEAD (required): `7b8ab79e6f16d4eccd2eeaba5974936c109dba9b`
- Channel: tooba-main, WorkerId: tooba-worker-01, AgentType: cursor
- Parent-Task: TB-TMAR-NOTIFICATION-AMSC-001-W3-R1
- Accepted lineage: W0 `5777ff4a` → W1 `c660b933` → W2 `e2f23975` → W3 `763dab13` → W3-R1 `7b8ab79e`

## Verdict driving this task

CURRENT CERTIFICATION IS NOT ACCEPTED AS FINAL. Certification drift against `.cursor/skills/tooba-architecture-structure/SKILL.md`: one-folder-per-use-case leaves under Application Customer/Seller Commands/Queries (4 single-file leaves + 6 request+validator-only leaves). Flatten ALL ten use-case leaf folders into their existing Commands/Queries axes. Repair recovery-metadata gap (W3-R1 final SHA `7b8ab79e` not recorded) is explicitly OUT OF SCOPE — record as pending post-Certify follow-up.

## Goal tree (after repair)

```text
Application/
Customer/
Commands/
  DismissCustomerNotificationCommand.cs
  DismissCustomerNotificationCommandValidator.cs
  MarkAllCustomerNotificationsReadCommand.cs
  MarkCustomerNotificationReadCommand.cs
  MarkCustomerNotificationReadCommandValidator.cs
Queries/
  GetCustomerUnreadNotificationCountQuery.cs
  ListCustomerNotificationsQuery.cs
  ListCustomerNotificationsQueryValidator.cs
Seller/
Commands/
  DismissSellerNotificationCommand.cs
  DismissSellerNotificationCommandValidator.cs
  MarkAllSellerNotificationsReadCommand.cs
  MarkSellerNotificationReadCommand.cs
  MarkSellerNotificationReadCommandValidator.cs
Queries/
  GetSellerUnreadNotificationCountQuery.cs
  ListSellerNotificationsQuery.cs
  ListSellerNotificationsQueryValidator.cs
Composition/ Models/ Ports/ Rendering/ Validators/
```

Exact namespaces: `Tooba.Notification.Application.Customer.Commands` / `.Customer.Queries` / `.Seller.Commands` / `.Seller.Queries`.

## SoT additive block (required fields)

`notificationModuleAmsc001W3R2`: task, parentTask=W3-R1, mode=STRUCTURE_REPAIR, skill=tooba-architecture-structure, startingHead=7b8ab79e, state=STRUCTURE_REPAIRED_READY_FOR_RECERTIFY, priorW3CertificationState=STRUCTURE_DRIFT_FOUND_OVERFOLDERED_USECASE_LEAVES, folderGranularityBefore=OVER_FOLDERED, folderGranularityAfter=PROFESSIONAL_SHALLOW, singleFileRequestLeafBefore=4, singleFileRequestLeafAfter=0, perUseCaseRequestLeafAfter=0, pathNamespaceState=EXACT, physicalCopyState=CLEAN, rootAllowlistState=ENFORCED, solutionExplorerState=CANONICAL, productionBehaviorChanged=false, schemaMigrationState=UNCHANGED, globalHostCheckpointState=PRESERVED, recoveryFollowupRequired=W3_R1_FINAL_SHA_7B8AB79E_NOT_YET_RECORDED, structureState=READY_FOR_CERTIFY, automaticNextImplementationTask=NONE, workflowStop=USER_REVIEW_NOTIFICATION_AMSC_001_W3_R2.

## Evidence (required under docs/architecture/evidence/TB-TMAR-NOTIFICATION-AMSC-001-W3-R2/)

physical-tree-before.md, physical-tree-after.md, folder-granularity.md, capability-map.md, path-namespace.md, stale-duplicate-copy.md, root-allowlist.md, solution-explorer.md, cohesion-balance.md, validation.md, certification-drift.md.

## Scope / forbidden highlights

Only Notification Application request axes + referencing Endpoints/Tests + Host Program.cs (namespace repoint only if required) + scoped guards + tmar-current-state.json + manifest only if physical truth requires + this-task evidence. No behavior/route/DTO/validator-semantics/DI/schema change; no certification verdict here; no guard weakening; no recovery-SHA repair; no W4/next module; STOP after Result.

## Result contract

Per Bridge Task body — `BEGIN_TOOBA_WORKER_RESULT … END_TOOBA_WORKER_RESULT` posted to `POST /api/results` only after validate + commit + push + `HEAD == origin/main`, then complete the Bridge task lifecycle and stop.
