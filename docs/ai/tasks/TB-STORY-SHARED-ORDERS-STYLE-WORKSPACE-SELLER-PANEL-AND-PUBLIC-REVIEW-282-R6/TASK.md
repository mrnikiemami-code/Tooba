PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R6
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R5
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Deliver the actual merchant Story form in admin and existing seller panel
Mode: OWNER_REJECTED_UX_CONSOLIDATED_IMPLEMENT_AND_VERIFY
Expected-Base-SHA: 5a0b341ab5d3fcd7ab8539d3f5c488562529dd6b
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Goal / authority
OWNER rejected the real served admin/seller experience after R5. R5 acceptance was only for owner trial, NOT visual/product acceptance. This owner feedback authorizes ONE consolidated merchant UX repair, including replacing the title-only creation step with the full shared form. Deliver observable changes in BOTH admin and existing VendorShell, not another proof-only or planning task. No delegation/screenshots/polling loop; one worker.

Read current AGENTS, applicable frontend/backend feature instructions, FRONTEND-ARCHITECTURE-STANDARD, OPERATOR-UX-ACCEPTANCE and R5 ARCHITECT-REVIEW. Preserve R5 product identity/update-item/exact retry improvements, current ownership/lifecycle and shared grid/template.

Owner evidence: admin detail /fa/admin/stories/01a04211-7dd7-7000-83dc-5c79db49abc4 displays mixed Story/internal copy, review filter and new-story button above an editor, scattered saves and ambiguous context. Seller /fa/vendor-panel/stories/new opens the old title-only modal. Owner explicitly says this is not deliverable and development is delayed.

Source confirmed at Expected-Base:
- src/frontend/app/stories/management/StoryManagementScreen.tsx: header renders review filter whenever canReview, including detail/create; creation Dialog remains title-only plus placement/shop.
- story-management-copy.ts adminDescription contains “Story”.
- destinationLabelForItem falls through to raw type for existing internal destinations.
- shopLabel already distinguishes platform vs seller-owned; DO NOT invent a shop name for a platform story.

## A — one merchant form, BOTH entry paths
Replace the title-only create modal and fragmented create-then-find-editor journey with the same complete Story form for admin and seller, using existing shared feature and design-system components. Prefer the existing full /new and /{id} route workspace; do not introduce a new shell or duplicate editor.
Show coherent sections: title/language, clear owning shop or main storefront, slides with image/video previews, caption, image duration and destination/product, preview, then clear save/submit actions.
“استوری جدید” must open this full form in BOTH roles. The seller must be able to prepare slides/products there, not be delivered the same “عنوان + ایجاد پیش‌نویس” modal again.
Use one clear primary save for current form and legitimate role/state actions; do not scatter technical step buttons. Existing necessary API steps may execute internally in their lawful order. Preserve frozen per-command occurrence/version, uncertainty recovery and existing media identity. No duplicate draft/upload/add on retry, no false atomicity claim. Unsaved form survives relevant failures; cancel/Escape causes no unintended writes; dirty navigation uses existing protection.
If existing routes require a draft before upload, implement the necessary lawful staging/orchestration inside this same form; clearly represent partial/unknown outcomes and do not make the merchant manually coordinate API steps.
Published content follows existing return-to-draft/review rules. Seller cannot self-approve. No weakening readiness checks just to enable a button.

## B — truthful shop/context and contextual controls
- Admin creation: clearly named “محل نمایش” with “ویترین اصلی” or an existing shop selected BY NAME via current lawful lookup.
- Seller: clear fixed “فروشگاه: <real current shop name>”, own products only; no editable owner or cross-shop switch inside Story form.
- Admin detail/list: show real shop name for seller-owned records; platform record explicitly “محل نمایش: ویترین اصلی”. The supplied record appears platform-owned; absence of a shop there is not evidence of missing backend ownership. Never fabricate ownership.
- If actual owned shop name is missing, trace read/DTO/adapter path and repair using existing Party Contracts. Unknown/unavailable is explicit and localized; do not substitute GUID or unrelated actor’s shop.
- Review filter stays ONLY on admin LIST, next to list controls, defaults to all unless intentionally restored from valid list state. No review-filter/new-story toolbar in create/detail. Back-to-list preserves lawful list state. Seller sees no moderation filter.
- On detail provide compact title/context/status header and relevant actions. Remove implementation explanations such as immutable ownership from ordinary merchant flow unless a meaningful user choice requires them.
- Keep existing AppDataGrid title first/actions logical-end; do not rewrite shared grid.

## C — complete merchant Persian and existing configured languages
Audit visible copy of these changed shared Story flows, not a whole-repo translation wave: headings/descriptions, create/edit, destinations, lifecycle toolbar/badges, scheduling, upload/picker, errors/empty/pending states, dialogs and confirmations. In Persian no raw “Story”, internal, enum/action tokens or raw IDs as labels.
Reuse existing locale resolution/catalog mechanism; locales come from DB registry, never assume only FA/EN. Preserve supported locale behavior and explicit fallbacks; do not build a new translation authority.
Explain destinations in merchant terms: “بدون لینک”، “محصول”، “فروشگاه” or “پیوند” as actually supported. Map the existing internal type by its actual target contract to a human label; preserve the stored target. Unsupported legacy targets show an honest localized unavailable state, not a made-up product or silent normalization.
Show product NAME selected for that slide, including after reload or beyond current picker page. Keep A/B labels independent and scope-safe. RTL layout, dates/numbers and shared design-system dialog/button conventions apply.
Keep schedule/lifecycle controls legible and aligned; image duration uses seconds in UI with correct existing wire conversion.

## D — verify the delivered screens, stop repeating shell evidence
Focused real mounted tests must cover:
1. BOTH admin and seller /new open complete form with slides/product/preview/actions; title-only modal is gone. Test actual interaction and role differences.
2. Platform vs shop-owned detail renders truthful context/name; shop-name unavailable and seller isolation.
3. Review filter only in admin list, absent from both role create/detail; list behavior retained.
4. Persian visible states have merchant labels, including EXISTING internal destination and lifecycle controls; preserve stored identity.
5. Create/save orchestration and existing-item edit capture actual requests; original media retained; unknown response retains the exact pending command, no false success or duplicate upload/create; cancel/dirty protection.
Reuse existing focused facts rather than replaying R1–R5 or creating another negative-control/certification phase.

Run affected focused tests, tsc, Next build; backend focused checks/build only if production backend changes. Respect critical-storefront guard when applicable; existing unrelated bg-white failure remains disclosed, no guard weakening or landing restyle.
Refresh PRIMARY served frontend and backend only if affected; backend uses TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1. Record BUILD_ID, final code/evidence SHA, Host5088/Next3000 identities, health/ready/store-alpha and origin parity.

Read-only check actual AUTHENTICATED served admin and seller list/new/existing-detail at 390/1440 using DOM/geometry, no screenshots or live business writes. Inspect currently visible UI before concluding. Compare against the reported failures: complete new form, clear owner/location, localized existing destinations, list-only filter, compact actions and no document overflow. Login shell evidence is NOT editor proof. If real auth/data unavailable, identify exact blocker and use labelled controlled browser/component states; do NOT label them live active screens or mark this delivery fully ready on their basis alone.
No new screenshot artifacts. No live create/upload/submit/approve tests; controlled HTTP/component tests may verify these paths without committed business data.
Do not infer stale deployment solely from appearance. Compare actual served build identity, route and shared component to final source, repair serving if necessary and record what BOTH roles actually rendered.

## Delivery / boundaries
Canonical tooba_alpha@127.0.0.1:5432 SingleStore/store-alpha, Host5088 FE3000 only. ZERO migrations/DDL/backfill/seed/reset/cleanup/restore/live Story-Media business write/provider/notification/money/stock/key operations. No demo records, permission/config/edition/connection changes. Controlled tests only, committedRows=0. Media pipeline and unrelated features unchanged; preserve Returns acceptance, payout backlog and all user work.
Verify Expected-Base ancestry/incoming delta; docs-only issuance descendants allowed. Normal commit/push/fetch parity; no force/rebase/amend/reset/stash. No credentials/binaries/scratch staged.

Persist exact envelope at docs/ai/tasks/<Task-ID>/TASK.md and docs/architecture/evidence/<Task-ID>/TASK.md with issuance receipt. Bridge unavailable content -> committed exact envelope, disclose; do not reconstruct.
One short ledger A–D -> changed production component, observable before/after in ADMIN and SELLER, actual test/runtime evidence and limits. Correct prior “ready” pointers: OWNER_REJECTED_UX; only this repair is issued; worker result awaits Architect then owner review. Update CURRENT consistently in AGENTS, TOOBA-RECOVERY-CONTEXT, START-HERE, Story plan and seller requirement; preserve historical evidence.
No planning-only delivery, proof-loop, unrelated discovery, new phase or automatic follow-up task. If a genuine blocker prevents completion, report the concrete blocker and completed work; do not present unchanged UI as delivery.
Return code/evidence/final SHAs, parity and short Persian handoff explaining visible changes in BOTH roles and URLs. Worker PASS != Architect ACCEPT != OWNER acceptance. STOP_FOR_ARCHITECT_AND_USER_REVIEW.

END_TOOBA_TASK
