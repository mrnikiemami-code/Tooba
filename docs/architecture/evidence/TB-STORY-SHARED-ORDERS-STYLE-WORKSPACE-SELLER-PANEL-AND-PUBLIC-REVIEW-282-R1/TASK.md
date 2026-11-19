PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R1
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Finish merchant Story editor, exact recovery and real mounted workflow proof
Mode: CONSOLIDATED_F1_F5_CLOSURE_OF_THE_ALREADY_ISSUED_PRODUCT_SCOPE
Expected-Base-SHA: ebb921235456c6c75c88a06abfa35fcefa0b00db
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Goal / authority
Finish the issued 282 merchant workflow; retain its good grid/shell/toast/player work. Mandatory missing features are not acceptable PASS limitations. This is ONE implementation task, not planning, partial prose or an automatic R2 chain.
One worker; no delegation/subagent/screenshots/polling loop. Normal existing merge history preserved; no rebase/amend/force/reset/stash. Verify main/origin/base, permit docs-only issuance delta. Persist exact Bridge content and hash honestly.
Read AGENTS, frontend/backend feature skills for touched files, architecture standard, OPERATOR-UX-ACCEPTANCE, seller-panel integration requirement and 282/ARCHITECT-REVIEW. Original 282 requirements remain binding with the precise implementation clarification F4 below.
Existing VendorShell /vendor-panel/stories/story.view only; shared Story feature, no parallel panel/menu/login/admin credentials in seller calls. No unrelated module redesign.

## 0 — One residual batch-containment correction
DetachAttemptOwned must not treat Guid.Empty as ALL modified Story/items in the DbContext. Capture exact command-owned ids/entries, preserve unrelated caller/tenant instance/state/values. Prove losing batch + unrelated modified caller + later lawful SaveChanges: loser never persists. Keep existing winner identity/single-story Gate0 fixes.
If claiming a PG reached-SaveChanges branch, actually assert the probe; otherwise describe its narrower observed scope. No broad reopening of 281; ZERO schema.

## F1 — Real executor and unresolved-act UI, all mutations
Current readStoryIntent is unused; store overwrites pending; only create/submit store partial payloads. Fix the real screen, not only helper tests.
- Small Story-owned API/state modules composed by UI. Freeze actor+role+shop scope identifiers, route/method/target, reviewed versions, occurrence and EXACT serialized payload before send; verified storage read-back. Never persist bearer tokens, cookies or keys; current transport auth must still bind to the frozen actor/scope.
- Every create/update/item/reorder/submit/review/schedule/stop/return/upload uses it. pending valid/unreadable evidence blocks replacement, malformed storage is explicit (not null/no pending). Actor binding required.
- Render unresolved-act recovery independently of fresh detail GET; remount shows exact retry/reconcile. Unknown/5xx/later auth/state refusal cannot erase possibly-sent evidence. No new key/body for retry; no background retry/polling.
- All same-tick double clicks -> one command, including approve/reject/schedule. Missing storage/getter throw/silent discard -> zero send + plain notice. Malformed 200 is not success.
- Epoch invalidates on target/actor/shop/mount, fences GET/dispatch/answer/toast/finally; old finally never releases new lock. Same-tab seller context change must be observed without polling. Old data/intent never paints another shop.
- Child journal for upload->add and create->submit: commit each successful child, retry unresolved child only. Never upload again because add reply was lost.
- Upload frozen key/digest/name/type/size/scope; no binary in sessionStorage. Reload asks for SAME file then verifies digest before retry. No blind discard of pending act.
- Requests must actually send frozen bytes/fields and scope, not rebuild from current UI/default shop. Do not mint on every retry.

## F2 — Complete merchant editor, preview and lawful actions
- Replace literal Active-shop-id input with searchable Party NAME picker; seller owner fixed by existing context. No merchant Guid input.
- Stable detail/new routes under both existing panels, same shared editor. No appended form under grid. Deep-link/reload/back and unsaved work protection.
- Edit title, cover from lawful assets, actual image/video preview, caption, image duration, slide reorder/edit/remove, upload progress/failure. A video img tag is not preview. Open/cancel => no POST.
- Enabled languages from real DB registry/permitted read and configured default, not fa/en hardcode. Minimal owning read-only seam if needed, no admin grants/settings/seed.
- Real destination picker, at least shop-eligible product + correct public route. Category/article options only if backed by actual lookup/navigation; preserve legacy values. Product with a valid offer from A must not be refused just because a winning/cheapest card belongs to B: use real owning Catalog/Offer authority, not first-seller shortcut; no pricing redesign.
- Public CTA resolves canonical destination through authorized read projection/adapter; a displayed button that returns silently for Guid is not delivered.
- Action rendering: server actionCodes + actual role permissions, not reviewStatus-only heuristics. Wire stop/return/edit/resubmit, lawful platform publication, approve-and-publish AND approve-and-schedule.
- Admin sees exact image/video/order/destination of reviewed version, then deliberately confirms publication; stale review needs a fresh deliberate review, no silent refreshed-version resend.
- Standard DS Dialog short actions/focus/Escape, existing react-toastify container, inline errors. Standard date/time UI + accepted timezone, not native datetime-local/browser offset. Plain Persian merchant labels.

## F3 — Full server query + actor/shop isolation
Keep Orders-style grid; forward sort/filter/advanced/page/count through typed owning adapters. No ad-hoc omission of sort/advanced or first-value collapse. Unsupported query must fail honestly, not silently no-op.
Seller saved views cannot call admin ui-preferences with adminHeaders. Use existing permitted user preferences or existing scoped client store; scope actor+role+shop, honest persistence errors.
Observe real current VendorShell actor/shop and fence/invalidate cache/views/forms/detail/intent on change. Keep shell/navigation/theme; story.view deny/allow proven. No default seller identity manufactured to make action succeed.
Human effective state + Persian/Jalali shared formatting; no raw Expired/IDs/contradictory badges.

## F4 — Platform upload needs actual keyed replay, current schema
Frontend currently appends occurrenceKey to generic admin upload, which ignores it and always creates a new asset. Fix this integration without changing unrelated generic consumers.
Authorize a SMALL Media-owned Story-platform upload use case through ISender/validator/admin authorizer/canonical tenant. No fake seller binding, key hidden in filename or second Ready-then-ledger save.
Schema-free design decision permitted: derive the MediaAsset custom id from a namespaced canonical tenant+actor+occurrence identity; existing checksum/name/type/size/creator bind full intent. Same key with changed bytes/metadata conflicts; same intent returns SAME durable asset.
Each ATTEMPT must use a distinct object storage key, so concurrent attempts sharing deterministic asset id cannot overwrite/delete winner bytes. Use exact asset-PK arbitration, existing constrained-transaction guard, scoped tracker handling, safe durable read after lost save ack. Read-unavailable must not delete a possibly committed object.
No new object-store/outbox/ledger framework or migration. A proven equivalent existing schema-free method is acceptable with the same guarantees. Generic admin upload and other consumers remain unchanged.
Frontend platform Story calls the keyed owning path. Controlled store tests: exact replay one asset; changed-file conflict; race loser object-only cleanup; after-save throw preserves winner; actor/tenant scope and pre-effect caller transaction refusal.

## F5 — Actual mounted behavior, not regex with a mount filename
Current workspace guard and story-modal.mount.test read fs and match strings. Keep as source guards if useful, label honestly, do NOT count as mounted proof.
Use repository React/jsdom/tsx harness to mount REAL components and REAL VendorShell integration:
- story.view allow/deny, seller no admin controls, actor/shop A->B late GET/mutation/finally fencing, scoped views, real forwarded sorting/filtering/count.
- shop name -> create/edit/cover -> image/video upload -> caption/reorder/remove/destination/DB language -> preview -> submit/reject/edit/resubmit -> reviewed publish/schedule -> stop/return.
- shared executor: unknown/remount exact retry, double click, unreadable/throwing/discarding storage, pending overwrite refusal, unknown then later refusal, upload-success/add-unknown retries only add, platform upload replay.
- public player actual image duration/pause and video timeupdate/ended/error/mute; canonical product destination/shop link, eligible share and hidden/expired share. No fake social controls.
Tests must assert requests/effects/storage bytes/visible controls and actual endpoint/owner wire shapes, not functions existing in source. Controlled HTTP is allowed but does not bypass owner contract. One critical F1 counterexample must fail pre-fix/negative control, then restored final PASS.
Actual served DOM/text/layout 1440/390 in admin and VendorShell + legal editor/preview: document overflow0, local grid scroll allowed, new controls44px, no technical IDs. No screenshots/live mutation; keep mount/served scope honest.
Critical-storefront landing bg-white failure: establish parent/head source+guard provenance narrowly and run the exact case if needed. Don't restyle unrelated landing or weaken assertions. All Story regressions closed; don't claim 19/20 is all-green.
Focused mounted/unit + tsc + Next build + critical-storefront. Backend only relevant 0/F4/Contracts tests/affected guards + build/publish. No whole-project/whole-repo baseline replay; real failure fix/rerun focused allowed.

## Boundaries / evidence / final gate
Canonical tooba_alpha 127.0.0.1:5432 SingleStore/store-alpha, Host5088 FE3000. ZERO migration/manual DDL/backfill/seed/reset/cleanup/restore/live Story-Media writes/upload/provider/notification/key operation. Existing assets/rows READ ONLY. Fixtures controlled or canonical rollback-only with independent committedRows=0; no EnsureCreated/new DB/Docker.
Refresh changed artifacts only, Host existing skip-bootstrap process flag; actual listener/path/hash/build-id/health/ready/platform, no auth/config/edition/port/key change. User performs first live act.
Preserve Returns acceptances/payout backlog/history/seller shell/user work. No staged scratch/keys/binaries, normal push/fetch parity; preserve issuance merge.
Keep parent RESULT unchanged; add CORRECTION-ADDENDUM for missing product behavior/source-only test provenance. Deliver F1-F5 ledger method->actual assertion->provider->observed result, current exact wire handoff, RESULT/RECEIPTS/result-contract, short Persian user-review URLs/checklist with live-write steps marked.
Update AGENTS CURRENT/recovery/START-HERE/Story plan/seller requirement to accurate state. Mandatory omissions cannot be limitations accompanying PASS. A real blocker needs concrete failed operation and evidence, not "wake ended".
Success: merchant types no technical Guid/internal-media URL (ordinary deliberate external website links are allowed), existing panels execute full shared workflow, exact unknown recovery, actual preview/destination, real mounted proof and usable served review URLs. No blanket Marketplace/commercial/user acceptance claim.
Post ONE canonical BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT with Task-ID, Status, Expected/Base/Code/Evidence/Final SHA+parity, F1-F5 states+validation+limits, User-Work-Preserved YES, User-Review-URLs, Recovery-Next-Task NONE, Next-Recommended-Task NONE, automaticNextImplementationTask NONE, STOP_FOR_ARCHITECT_AND_USER_REVIEW. Then STOP.

END_TOOBA_TASK

