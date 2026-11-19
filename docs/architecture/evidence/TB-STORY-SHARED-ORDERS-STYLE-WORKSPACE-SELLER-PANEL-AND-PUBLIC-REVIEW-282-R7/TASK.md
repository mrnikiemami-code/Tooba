PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R7
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R6
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Finish actual slide creation and contain R6 auth/startup deviations
Mode: R6_CONNECTED_FORM_AND_SAFETY_REPAIR_ONLY
Expected-Base-SHA: 9a28b8eae6c608f60045848383e7b5f62919be73
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Authority / concrete review
R6 is PARTIAL, not Architect ACCEPT. Retain list-only filter, real shop/platform context, localized copy, existing VendorShell and R5 media/product/exact-retry work. Complete these existing obligations in ONE implementation delivery; no redesign, new phase, delegation/screenshots/polling.

Read applicable feature instructions, OPERATOR-UX-ACCEPTANCE and R6 ARCHITECT-REVIEW.md. Sources reviewed: code 49b66122739a83eef5f69a69227990e49a17eea5, evidence 9a28b8eae6c608f60045848383e7b5f62919be73.
Confirmed defects:
- /new preview is an unconditional empty paragraph. Add-slide calls createDraft -> openDetail/router.push (twice) -> opens a dialog on the departing route. It does not keep a populated full form. New tests only inspect markers, never click/save/cancel.
- Back-to-list router.push ignores dirty state; beforeunload does not protect SPA navigation.
- internal/shop/seller types are blindly labeled shop and raw ctaTarget is printed; action origin Admin was relabeled main storefront although Admin can create for a shop.
- Shared cookie helper now disables Secure on arbitrary HTTP/forwarded http/override=false; loopback login fix expanded production behavior.
- Worker ran Development Host without required skip-bootstrap. Program.cs 371–440 enters migration and seed/backfill orchestration. Actual write effects UNKNOWN; “zero seed/migration” not proven by unchanged binary.

## A — finish shared form behavior, not markers
Make BOTH admin and seller create flows actually prepare/add/edit slides and render their real preview on the shared form. Internal early draft creation is allowed if required by existing media APIs, but it must not destroy form/dialog state or require manual technical step coordination.
Keep one owning draft id and reviewed version; after early creation, reuse it on later Save/Add, never create another draft for the same form. Keep title/language, placement/shop, pending file/product/caption/duration and existing slides through legal transitions. Once owner is persisted show immutable truthful owner; no misleading editable switch.
Use one deliberate route transition when appropriate; do not push twice or open a dialog on an unmounting page. Wait for bound loaded detail when needed; no silently inert slide-save.
Cancel/Escape of an untouched dialog sends zero requests; leaving a dirty form prompts via existing DS protection, with keep-editing/discard behavior. Do not claim cancelling an already committed draft undoes that write.
Real preview reflects selected/saved slide media and product/destination, not permanent placeholder.

Focused real-component behavioral tests for BOTH roles must CLICK Add, choose a file/product, enter caption/duration, save, edit, preview and cancel. Model actual route unmount/remount, rather than a no-op navigate hiding the transition. Capture one create then reuse, correct upload/add/update/version identity and retained media. Include one transport-unknown create or add, exact pending/retry without duplicate create/upload, and dirty back navigation. Reuse existing executor; no new mutation framework or proof-loop.

## B — truthful labels
Resolve existing internal destination against actual backend/public-player target contract. Only actual shop targets get shop NAME; arbitrary paths/unknown/invalid get truthful localized destination/unavailable. Never print GUID/raw enum as merchant label; preserve stored target.
Origin is creator role, not placement: restore truthful localized Admin/Seller creator label or omit this redundant badge. Placement derives from actual owner, including admin-created shop stories.
Keep list-only filter and owner name fixes.

## C — contain cookie change and startup deviation
Use the existing secure production default. Any necessary HTTP development exception must be narrowly constrained to explicitly configured LOCAL development on canonical loopback; not arbitrary public HTTP or a forwarded header alone. HTTPS must retain Secure for session/refresh/CSRF. Remove unrestricted false override/downgrade; use existing trusted proxy/deployment policy, never assume all x-forwarded-proto is trusted.
Focused tests: authorized canonical local HTTP works; public HTTP cannot downgrade production policy; HTTPS and spoofed forwarded-http cannot downgrade; all auth cookie creation/clearing callers consistent. No broad auth redesign or new permission bypass.

Do not run another bootstrap-enabled Host to regain dev identity. First inspect already available startup logs/process/config and read-only migration/seed evidence for the R6 restart; record observed outcomes versus UNKNOWN, no invented “NONE”. Do not clean up/restore/reseed to hide effects.
Keep Host5088 safe with TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1 for any authorized refresh. Diagnose identity separately using existing persisted auth path; skip-bootstrap does not authorize admin credential borrowing, new grants/accounts, seed execution or auth bypass. If the existing auth path genuinely cannot support live review safely, report that exact runtime blocker and complete controlled form implementation/tests; don't claim live acceptance.
This task authorizes narrowly fixing the R6 cookie regression and safe process refresh, not schema/seed/identity redesign.

## Validation / delivery
Run affected FE mounts/auth tests, tsc, Next build; no whole-repo/Host/AMSC sweep. Backend production unchanged unless a directly necessary bounded lawful read repair is proven; follow feature rules and skip-bootstrap. Preserve unrelated critical-storefront bg-white failure disclosure.
Refresh affected primary artifacts; record code/build/chunk identity and read-only authenticated actual ADMIN and SELLER forms at 390/1440 where safely available. Login-shell markers never prove interactive editor. Controlled browser states explicit; no live Story/Media POST or screenshots.
Canonical tooba_alpha@127.0.0.1:5432 SingleStore/store-alpha, Host5088 FE3000. ZERO migration/DDL/seed/backfill/reset/cleanup/restore/live business writes/provider/money/stock/key/config/grant/edition changes. No demo rows; controlled tests committedRows=0. Preserve all user work, Returns and payout backlog.
Persist exact envelope/receipt in existing task/evidence convention. Verify base ancestry; docs-only issuance descendants allowed. Normal commit/push/fetch parity, no force/rebase/amend/reset/stash.
Short ledger A–C: actual interaction evidence, source/provider/runtime limits and R6 startup correction. Update CURRENT pointers: worker result awaiting Architect; no readiness claim based on marker-only tests. Return final SHAs, parity and Persian user handoff only after actual behavior works; no automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
