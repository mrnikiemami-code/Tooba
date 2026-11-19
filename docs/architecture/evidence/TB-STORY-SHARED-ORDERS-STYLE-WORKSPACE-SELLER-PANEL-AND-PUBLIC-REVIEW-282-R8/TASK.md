PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R8
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R7
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Bind recovered draft and restore safe local owner review
Mode: TWO_CONFIRMED_BLOCKERS_ONLY
Expected-Base-SHA: 8dc12e7cffed98ee40457639290ed3ae98989a70
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Scope
R7 bounded create/add, dirty-back, labels and cookie hardening progress retained. No redesign or another feature/proof wave. Finish ONLY two concrete blockers below so owner can actually review. Read current AGENTS, applicable feature rules and R7 ARCHITECT-REVIEW. One worker; no delegation/screenshots/polling.

## A — adopt successful recovered create
Confirmed source: normal create success binds ownedDraftIdRef and opens detail. story-recovery-retry success only toasts/refreshes; on /new it never adopts returned storyId. storeStoryIntent prevents a new occurrence while unresolved, but after replay clears that envelope, Save/Add can mint a NEW create for the same form. R7 “unknown … retry” fact stops before clicking retry; it does not prove replay.

Share the lawful bound-create completion between normal success and exact replay. Validate current role/shop/actor/epoch and response identity; adopt the returned owning draft, load bound detail and resume the intended step exactly once. Preserve create->Add continuation through a route remount. No duplicate create/upload, stale actor adoption, or false navigation on malformed response. Do not clear pending evidence before adoption is safely recoverable.
Two focused real-component facts (admin and seller): unknown create -> Save/Add while unresolved cannot dispatch another create -> click recovery retry with SAME occurrence/body -> genuine bound response -> existing draft route/detail -> Add/Save reuse recovered id; only initial and same-command replay create requests, zero fresh create key. Compare frozen bytes/key and current-scope identity. Include malformed retry retention and remount. Reuse existing executor/storage; do not rewrite them broadly.

## B — safe review with bootstrap still disabled
R7 BFF login 200 but /v1/admin/dev-context 404 because AdminDevActorBootstrap.Snapshot is in-memory and only populated by EnsureAsync, which also registers users/writes membership. prepareAdminDevActor relies on this seam.
Repair the LOCAL DEVELOPMENT read/initialization seam using existing Identity Contracts to find the ALREADY EXISTING actor and existing tenant context, and existing authorization checks. A small read-only snapshot initializer/fallback is authorized if necessary; never call EnsureAsync, RegisterAsync, tuple writer, seeds or migrations. Preserve development-only endpoint boundaries and all real admin/seller authorization. Missing actor/permission fails explicitly; no default actor or grant bypass.
Trace the actual failed capability/Story read, not only dev-context. Use persisted login/session/current lawful actor path where it already suffices. No production identity redesign, shared cookie-policy expansion or connection/config changes.

Publish affected backend ONLY if source changes, then refresh Host5088 with TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1. Keep FE3000 primary and refresh affected artifacts. Verify read-only authenticated admin and seller list/new/existing-detail (safe available data), actual form/dialog/owner text and 390/1440 geometry, no live Story/Media writes. Report actual statuses/build identities, not just 200 page shell.
R6 bootstrap effects remain UNKNOWN unless existing read-only evidence resolves them; no cleanup/restore/reseed. Do not reopen that as a new audit.

## Validation / result
Only affected focused tests (new recovered-create facts, bounded read-only dev-context/auth checks if changed), tsc and Next build; backend build/tests only if changed. Retain relevant prior facts, no full R1–R7/AMSC sweep, no new negative-control phase. Known unrelated critical-storefront bg-white remains disclosed.
Canonical tooba_alpha@127.0.0.1:5432 SingleStore/store-alpha Host5088 FE3000. ZERO DDL/migration/seed/backfill/reset/restore/live business writes/key/provider/payment/stock operations, ZERO account/grant/membership changes. Controlled tests committedRows=0. Preserve user work, Returns, payout backlog, VendorShell and existing language registry.
Verify base ancestry; docs-only issuance descendants allowed. Persist exact task+receipt in current convention; normal commit/push/fetch parity, no force/rebase/amend/reset/stash.
Update CURRENT pointers consistently. Return concise A/B before/after evidence, code/evidence/final SHA, Host/Next identities and usable Persian review links. Worker PASS != Architect ACCEPT != owner acceptance. If an actual external blocker remains, name exact refused operation; no unchanged/blocked screen labelled ready. No automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
