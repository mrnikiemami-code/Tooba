PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R9
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R8
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Restore existing local authorization projection and deliver live owner review
Mode: LOCAL_INMEMORY_AUTHORIZATION_RECOVERY_ONLY
Expected-Base-SHA: dcd5e4e383f819924b182d421ffcb7dce32c1857
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Scope / source diagnosis
R8 recovered-create fix and read-only dev-context bind are accepted within source/controlled evidence limits. Do not reopen UI/R5–R8. Owner review remains blocked by actual Story API 403.
Read AGENTS/applicable backend feature instructions and R8 ARCHITECT-REVIEW.
Confirmed chain: HostAdminPanelAccess -> AdminPanelAccess -> tenant#view; InMemoryAuthorizationAdapter stores tuples only in process memory. Restart with skip-bootstrap leaves that memory empty. Finding persisted admin identity does NOT establish membership/permissions. AccessControlDirectory.GetEffectiveAccessAsync reads existing role assignments; SyncUserCapabilityTuplesAsync projects capabilities through the owning authority. Do not call EnsureBootstrap/EnsureAsync seed paths to recover these.
One focused implementation, one worker, no screenshots/delegation/polling or new Story design.

## One delivery — reconstruct only already-authorized process state
1. Read actual runtime engine/config and the exact denied check for current admin and seller. Read existing durable membership/assignment/effective permission authorities through owner Contracts; report which evidence supports tenant/party and Story capabilities. Never infer admin membership merely from email/Snapshot or a client header.
2. For Development + actual InMemory engine ONLY, authorize rebuilding the PROCESS-LOCAL derived authorization projection from those existing verified authorities. This narrowly supersedes earlier zero-tuple-write restriction for that memory projection ONLY. No DB/account/role/assignment/membership/ceiling/grant creation or modification; no SpiceDB write or mode switch.
3. Implement/reuse AccessControl-owned recovery via Contracts, with small Host composition seam only as needed. Keep TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1 and all business bootstrap paths disabled. Snapshot lookup remains read-only. Separate reading durable grants from loading ephemeral derived tuples.
4. Project only the real actor/current trusted tenant/shop and existing effective access. Keep archived/revoked/missing assignments denied, seller/admin isolation, unavailable fail-closed. No hardcoded allow, static email-to-admin grant, default actor, authorization bypass, or synthetic membership. If no durable authority proves a required relation, STOP with exact missing relation/read evidence; do not invent it. Do not expand this task into an authorization redesign.
5. Do not invoke broad bootstrap, seeds or existing sync against a persistent authorization engine. Verify runtime engine before any tuple writer call. Do not expose a public unauthenticated write/recovery endpoint. Reuse existing lifecycle/local composition policy; recovery must not be a hidden grant on arbitrary Story GET.

## Verify and hand off
Focused behavioral test of the actual recovery path (not source-string guards): valid durable actor/scope restores required local decisions after a fresh empty engine; revoked/unassigned and other-scope actors stay denied; no persistent write/registration/seed call; non-InMemory or non-Development does not run. Use existing controlled harness; no whole-repo/AMSC or UI test wave.
Build/publish affected backend, refresh Host5088 with skip-bootstrap=1. Keep FE3000 existing R8 build unless affected. Actual existing-session login + read-only admin/seller capability and Story list/detail endpoints must be checked; record status/code/actor/scope without secrets.
On authenticated served admin and existing VendorShell inspect list, /new and available existing detail at 390/1440: complete form, correct shop/main placement, localized destinations, list-only admin review filter, reachable slide dialog and overflow. DOM/geometry only, no screenshots and no live create/upload/submit/moderation requests. State explicitly when existing data is unavailable. HTTP200 page shell alone is insufficient.
Return usable owner URLs and short Persian checklist, code/evidence/final SHA, origin parity, Host DLL/skip identity and Next BUILD_ID. Mark READY_FOR_OWNER_REVIEW only if actual authorization and active screens are available. Controlled tests cannot replace live readiness.

## Boundaries / durable state
Canonical tooba_alpha@127.0.0.1:5432 SingleStore/store-alpha, Host5088 FE3000. ZERO DDL/migrations/business seeds/backfill/reset/cleanup/restore/live business rows/provider/payment/stock/key operations. No persistent permission/account/membership changes, no config/edition/grant/connection changes. Preserve Returns, payout backlog, user data, existing auth policy, VendorShell and language registry.
R6 startup effects remain UNKNOWN; no new investigation/cleanup wave. Existing critical-storefront bg-white failure stays disclosed.
Exact task/receipt under docs/ai/tasks/<Task-ID>/TASK.md and docs/architecture/evidence/<Task-ID>/. Verify base ancestry; docs-only issuance descendants allowed. Normal commit/push/fetch parity, no force/rebase/amend/reset/stash.
Update CURRENT pointers consistently: R8 bounded accepted, R9 environment repair only, result awaits Architect then owner. Worker PASS != Architect ACCEPT != owner acceptance. No automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
