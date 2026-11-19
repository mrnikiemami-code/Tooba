PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R11
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R10
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Fix delete replay and effective status; verify the delivered owner flow
Mode: R10_CONCRETE_DEFECTS_AND_MISSING_INTERACTION_PROOF_ONLY
Expected-Base-SHA: 400ccbf1cade071be3ebd72ab95171360475f6fb
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Review / authority
R10 PARTIAL. Retain actual preview, photo/video separation, draft publication fields, localized single status, scoped delete CQRS and stricter shop media readiness. Read current feature instructions and R10 ARCHITECT-REVIEW.
One consolidated repair of existing obligations; no new feature/architecture phase, screenshots/delegation/polling.

## A — real deletion with correct exact replay
Confirmed bug: AdminDeleteAsync calls TryReplayDeleteAsync with sellerPartyId=null but records actual story.SellerPartyId. After admin deletes a shop Story, exact retry mismatches scope and throws OccurrenceConflict. Fix replay identity without loading the already-deleted Story or weakening seller/tenant/action/story/hash checks. Persist/derive the actual owning identity consistently from existing occurrence ledger. Preserve exact command/version arbitration.
Do not turn an existing, not-deleted target into successful deletion merely because an occurrence row exists; inspect concurrent delete winner/fold behavior. No Media assets or unrelated tracked entries removed.
Complete requested list action as well as detail action; confirmation uses chosen row's title/id/version/scope, cancel zero requests. On unknown DELETE preserve pending; genuine same-command replay after deletion clears matching evidence and leaves list/closed detail, rather than refreshing a deleted detail into error.
Focused owning EF/controlled tests MUST invoke real AdminDeleteAsync and SellerDeleteAsync: admin platform and admin-for-shop; seller own vs other shop/tenant; stale version; items+Story removed in one save; Media untouched; exact retry after target gone, foreign occurrence conflict and list/public absence. No source-string or domain EnsureCanDelete-only substitute.
Mounted admin/seller list/detail deletion: actual DELETE identity/body, cancel, lost-response then replay with same bytes/key and correct route transition. Reuse existing storage/executor.

## B — truthful time/status and draft-window behavior
Confirmed displayState fallback: Active with effectivelyPublic=false returns “در ویترین”; expired persisted Active/Scheduled rows can lie because public eligibility does not rewrite stored Status. Use existing backend effective policy/current clock consistently: “در ویترین” ONLY when effectively public; expired approved publication windows render expired; draft/rejected/submitted/stopped stay their own lawful states. No raw enum or stacked badges.
Verify existing draft window across actual HTTP mapping/directory/save/reload and seller submit->admin approval: dates preserved, no implicit activation, not public before start or at/after end. Explicit “بدون پایان” controls null; unchecking it requires complete end date+clock, not silent null. Align end>start validation/error code (R10 validator >= versus domain <= mismatch).
Keep admin review authority and lawful transitions; don't allow content update on a frozen state merely to change dates. Admin explicit scheduling retains existing policies.
Behavioral fixtures cover approved Active past-end, Scheduled past-end, future window, effective current publication, draft requested window and stopped/rejected. Exact Jalali/Tehran day+clock checks, not merely slash/clock regex.

## C — actual media/submit path and honest proof
R10 FE “mount.test” is only two date/copy units; it does not mount or exercise A/C/E. Completion ledger cannot claim cancel/preview interaction from those tests.
Use supported real workspace/dialog mounts to choose image/video, inspect actual preview src/control, change/close cleanup, photo7s->7000 and video duration=null, saved-original media retention. Exercise existing public player video ongoing at5s vs ended, no source scans.
Keep URL-only unowned seller media refused. For existing invalid slide, show a merchant-readable specific reason and a lawful visible recovery path (select/re-upload valid media, or explicitly remove that slide and add a valid file). Do not silently attach assets, migrate fixtures or merely hide submit and leave owner stuck.
Actual owning controlled upload->add asset->seller submit request with current role/shop/version/key -> bound pending-review response. Invalid-media refusal retains work and shows actionable Persian copy; changed actor/shop stays denied. Use actual observed response code; no fake live grant/account switch.
Read-only active served admin AND the owner's existing seller/shop list/new/detail/dialog at390/1440 must show these repairs; no live mutation. Capture route/context/error code if genuinely blocked. Retain all R5–R9 safeguards.

## Validation / serving
Only affected backend integration/policy tests + FE mounted flows/player/date/status facts, tsc and Next build. No full task-series/Host/AMSC rerun. Diagnose the exact Next Html/404 build failure from stack/command/environment; do not call it pre-existing without evidence. Fix narrowly if tied to changed delivery/runtime setup, otherwise report precise unrelated blocker and truthful next-dev identity. No unrelated landing restyle/guard weakening. Development serving is not a production-build PASS.
Publish affected backend with TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1; preserve R9 memory projection. Record Host DLL/health/ready/store-alpha and FE mode/PID/build or dev chunk identity from final source.
Canonical tooba_alpha@127.0.0.1:5432 SingleStore/store-alpha Host5088 FE3000. ZERO migration/DDL/seeds/backfill/reset/restore/live Story-Media mutation/persistent account-grant-config changes/key/provider/money/stock operations. Controlled EF/rollback tests committedRows=0; no actual owner-data deletion/upload/submit. Media pipeline unchanged.
Preserve R10 progress, VendorShell, language registry, Returns/payout/user work; R6 effects UNKNOWN remains.
Persist exact task+receipt; verify base ancestry, docs-only issuance descendant allowed. Normal commit/push/fetch parity, no force/rebase/amend/reset/stash.
Correct R10 ledger claims by actual evidence. CURRENT pointers: R10 PARTIAL; only R11 issued; result awaits Architect then owner review. Return short visible handoff, precise test/runtime limits and final SHAs; no automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
