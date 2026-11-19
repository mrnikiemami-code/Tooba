PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R10
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R9
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Close owner-reported deletion, preview, timing, status and submit failures
Mode: OWNER_TRIAL_FAILURES_ONE_CONSOLIDATED_DELIVERY
Expected-Base-SHA: 06e3bd558f0b9a2f323f9dbab596188580edb798
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Owner feedback / authority
Owner rejected current Story functionality: no whole-Story deletion; selected file lacks preview; video incorrectly shows photo duration=5; publication start/end unavailable in seller authoring; status column repeats Draft/awaiting/review with no clear choices; submit-for-approval errors. Screenshot seller header says “زمینهٔ غیرمجاز”; grid includes pending/legacy states and “اقدام ناتمام”.
This is ONE connected implementation task, not analysis-only or another architecture/proof wave. Read AGENTS, applicable FE/BE feature rules and OPERATOR-UX-ACCEPTANCE. Retain existing VendorShell, ownership, review authority, R5–R9 exact replay/protection and local skip-bootstrap recovery. No screenshots/delegation/polling.

Confirmed source at Expected-Base:
StoryManagementScreen has unconditional photo-duration input; no local selected-file preview; status cell combines displayState + raw status badge + review badge; displayState may return English status. Only admin has schedule action. Seller capabilities canSchedule=false. Existing start/end persisted; create/update HTTP commands lack schedule fields. Slide removal exists; whole-Story delete endpoint does not. Fix actual owning paths, not text hiding.

## A — media preview and duration semantics
Show the chosen image as actual local preview BEFORE upload, and chosen video as controlled playable preview with duration when metadata available. Existing-item edit shows original media. Replace/revoke object URLs on change/close/unmount; no upload required just to preview, no background network mutation.
Photo only: “مدت نمایش این عکس (ثانیه)” default 5, editable/validated, seconds->milliseconds existing contract. This is dwell time INSIDE the opened Story player, not lifetime in the storefront rail.
Video: hide photo-duration control, use actual media playback/ended policy, never truncate a 90s video at inherited 5s. Inspect and repair existing public player only where necessary; preserve pause/manual next/previous behavior. Do not pretend browser metadata is trusted backend authority; follow existing Media/Story contracts. Preserve unchanged media identity on edits.
Behavioral proof: photo preview/change/cancel and 7-second payload; video preview + no photo-duration input and player not advancing at 5s while video continues. Correct cleanup and retained file on uncertain upload.

## B — publication start/end visible in the form and list
Add a clear “بازهٔ انتشار” section to shared create/edit with START DATE+TIME and END DATE+TIME using existing JalaliDatePicker, Tehran zone and canonical UTC conversion. Reuse existing StartAt/EndAt storage; small additive command/DTO/validator wiring authorized, no parallel schedule store.
User can select e.g. 14 Aban at16:00 through18 Azar in the explicitly selected year; UI and list show Jalali day AND clock, not raw ISO. End-after-start validation, optional explicit “بدون پایان”, no invented arbitrary default year or silent timezone conversion.
Seller may save/propose this window on OWN editable draft and submit it for approval; this NEVER activates/approves content. Admin sees requested window and can approve with it or explicitly adjust. Admin-created Story can save draft, publish now or schedule using existing lawful lifecycle. Separate “save draft window” from SetSchedule which currently activates/sets Scheduled; do not call activation command from seller authoring.
Draft/rejected editing persists window across save/reload; submitted/approved content follows existing reviewed edit/return rules. Effective public reader uses same approved interval: not before start, stops at end. Reuse existing policy; no timer daemon/schema framework.
Controlled tests: exact Jalali/Tehran roundtrip, end validation, seller draft stays nonpublic after saving dates, submit->admin approval retains window, effective visibility before/inside/at-end, list clocks.

## C — delete actual Story and slides
Provide named/accessible whole-Story “حذف” action in list/detail, distinct from slide removal and stop display, with DS confirmation showing Story title.
Implement the missing scoped backend delete use-case/endpoints/Contracts as needed through module CQRS, validators and permissions; no client-only hiding/disable passed off as deletion. Seller deletes only own editable drafts/rejected content; submitted/approved content offers existing lawful return/stop path and honest reason if deletion unavailable. Admin deletion follows authorized lifecycle, stopping visibility where needed. Do not expose direct lifecycle/status assignment to bypass review.
Delete Story-owned items transactionally and preserve durable occurrence evidence for exact uncertain-response replay AFTER target is gone, through existing schema/ledger. No Media asset/storage cleanup or other Story deletion. Confirmed deletion disappears from list/public query and handles currently open route cleanly. Cancel sends zero requests; unknown failure stays pending and does not disappear optimistically.
No new schema or migration expected: use existing tables/occurrence ledger. If an unavoidable schema blocker is proven, report it precisely; do not execute DDL or substitute fake delete. Tests on controlled data only: owner isolation, confirmation cancel, correct version/key, removed target replay, stale version, public disappearance.

## D — one truthful status and explicit lifecycle choices
Use one clear Persian effective status in list, not stacked duplicate Draft/Submitted/Approved badges or “Active”. If review status needs detail, use concise secondary text ONLY when it adds distinct information. Use backend state/actionCodes as authority, with expiry/readiness/window semantics. No fabricated DB cleanup to normalize historical rows.
Owner choices must be explicit: seller “ذخیرهٔ پیش‌نویس” and “ارسال برای تأیید”; admin draft/publish/schedule/moderation as legal. Explain pending/rejected/stopped and next available action/reason in plain Persian. State is a result of legal actions, not an unrestricted status dropdown that lets seller approve.
Keep admin review filter only in list. Dates and status accurate on reload. Resolve repeated “در انتظار تأیید / در انتظار بازبینی” to one consistent term.
Focused fixtures: draft, submitted, rejected, scheduled future, currently visible, expired and stopped; each one human status, correct role actions and no raw enum/duplicated badge. Unknown pending-operation warning remains separate and actionable.

## E — fix reported submit error on actual owning scope
Inspect existing failed network/log/response evidence and pending intent READ-ONLY; identify exact code/status/body/role/shop/actor/version. Do not claim a root cause from screenshot alone. The visible invalid VendorShell context is an input to diagnosis, not permission to select an arbitrary working seller.
Use the actual user's existing seller/shop session and existing durable assignments/context. Fix precise context adapter/route/payload/version/readiness defect. Keep fail-closed for legitimately unassigned/other-owner actors; do not grant roles or change account to obtain PASS. R9 assigned-actor200 alone does not prove this user's selected shop.
If pending exact envelope exists, retain and safely reconcile/replay it; no clearing whole storage, minting new key after uncertain submit or editing frozen bytes. Translate server errors to actionable Persian field/message, never false success. “ارسال برای تأیید” enabled only when lawful server actions/content allow; success refreshes one truthful awaiting status and removes duplicate action.
Behavioral test actual seller workspace click -> owning submit request with current version/occurrence and correct scope; bound success -> pending status; representative actual reported failure retains work and shows reason. Existing read-only real API checks of THAT actor/shop should establish readiness. No auth/bootstrap redesign or new grants.

## Validation / handoff / boundaries
Complete A–E in one delivery, no planning-only stop. Focused FE interaction/player/status/date tests and affected backend command/policy tests only; tsc/build; affected backend publish when source changes. No full R1–R9/Host/AMSC sweep or unrelated redesign.
Refresh primary Host5088/Next3000, Host TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1; retain R9 Development/InMemory recovery. Confirm actual admin and user seller/shop authenticated list/new/existing-detail/form/dialog DOM at390/1440, record build/chunk/Host identity and observable A–E outcome. No screenshot artifacts. Shell200 or mounted markers alone are not delivered workflow proof.
ZERO live Story/Media POST/PUT/DELETE by worker; use controlled transport/rollback-only fixture with committedRows=0. Read current failed request logs rather than replaying owner submission live. Owner performs first real delete/upload/send/schedule trial.
Canonical tooba_alpha@127.0.0.1:5432 SingleStore/store-alpha, Host5088 FE3000. ZERO DDL/migration/seed/backfill/reset/cleanup/restore/provider/notification/money/stock/key or persistent account/grant/assignment/edition/connection changes. Do not alter/delete owner business rows as tests. Preserve all prior work, Returns and payout backlog, existing template and DB language registry. Known unrelated bg-white failure and R6 startup effects UNKNOWN remain disclosed.
Verify base ancestry; docs-only issuance descendants allowed. Persist exact task+receipt under existing docs/ai/tasks and evidence convention. Normal commit/push/fetch parity; no force/rebase/amend/reset/stash.
Short A–E ledger: actual method/request/test/observed result; unresolved blocker explicit, no generic “PASS” for unfinished gates. Update CURRENT pointers consistently: owner trial failed; only R10 issued; result awaits Architect then owner test. Return short Persian visible-change handoff and code/evidence/final SHAs; no automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
