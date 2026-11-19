PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282
Parent-Task: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R5
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Story workflow ready for shopkeeper review in the existing seller and admin panels
Mode: ONE_CONNECTED_PRODUCT_DELIVERY_WITH_BOUNDED_FIRST_SAFETY_GATE
Expected-Base-SHA: c9ba2296a3adfbb6f6ed2638df65957f4c3f391c
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## هدف تحویل
کاسب بتواند در پنل فروشندهٔ موجود «استوری جدید» بزند، عکس/ویدیو انتخاب کند، پیش‌نمایش ببیند، پیش‌نویس ذخیره یا برای تأیید بفرستد. ادمین همان فرم را دارد، با انتخاب فروشگاه و تأیید/رد. فهرست مثل Orders، مرتب و قابل‌فهم؛ محتوای تأییدشده با نام فروشگاه درست در ویترین عمومی پخش شود.
این یک تحویل متصل است، نه فقط تغییر متن/جدول و نه تسک برنامه‌ریزی. در انتها لینک واقعی پنل‌ها و چک‌لیست کوتاه فارسی برای تست صاحب محصول بده. تست فنی را user acceptance ننام.

## Claim / mandatory source
One worker tooba-worker-01, no delegation/subagents/screenshots/polling loop/automatic task chain.
Verify main/origin, Expected-Base ancestor; only docs issuance delta permitted without new source review. No reset/stash/rebase/amend/force. Persist exact received Bridge task bytes and receipt, not an alternate reconstructed envelope.
Read AGENTS, frontend + backend feature skills for touched code, FRONTEND-ARCHITECTURE-STANDARD, OPERATOR-UX-ACCEPTANCE.md, Story completion plan, SELLER-PANEL-INTEGRATION-REQUIREMENT.md and R5/ARCHITECT-REVIEW.md.
Source anchors:
- app/stories/management/StoryManagementScreen.tsx and story-api.ts (existing shared feature)
- vendor-panel/layout.tsx -> VendorShell; existing /vendor-panel/stories + story.view navigation
- AdminOrdersScreen and AppGridRowActionsCell (existing Orders table reference)
- storefront/stories/home-stories.tsx + story-modal.tsx, existing Catalog seller-profile public identity
- StoryDirectory.Occurrences.cs and owning command/endpoints for current wire.

## 0 — Small residual containment gate FIRST, in this task
R5 A1/A2/A3 and Media guard progress retained. Do NOT repeat the previous broad repairs.
Before UI, fix/prove exactly:
0a) Collision winner folding uses the SAME full expected identity as pre-read replay: tenant, shop, action/role scope, story/item target and intent hash. Hash-only winner matching must never answer a different shop/action winner. Use existing occurrence columns/complete intent; no schema change.
0b) On unique/concurrency failure, restore/detach only this command's staged Story/items/occurrence; do not leave losing Added/Modified/Deleted rows for a later caller SaveChanges. Preserve unrelated caller instance/state/values. No ChangeTracker.Clear/global detach. Test losing create + item/update, then a subsequent lawful SaveChanges; zero losing rows/effects and winner intact.
0c) Real optimistic concurrency test: TWO contexts preload the same original tracked Story BEFORE writer1 saves; writer2 owning command reaches SaveChanges with that original (assert interceptor/branch), typed loser, no extra occurrence, safe context after failure. Same-transaction rollback-only is sufficient; no committed race/stress.
Use exact occurrence constraint arbitration and correct savepoint/rollback ownership already in scope. Keep no-migration/current schema.
Gate tests must fail on pre-fix code or focused negative control then pass restored. Append truthful C2 proof clarification (R5 current two-context test proves stale command only). Once 0 passes continue A–F without another task. A real external blocker must be evidenced; do not silently skip the gate.

## A — Existing panels, one feature, Orders-style list
- COMPLETE the purchased-template VendorShell at /vendor-panel/stories; keep existing header/sidebar/drawer/theme/active menu/auth/shop context and story.view permission. NO duplicate panel/menu/login, NO AdminShell substituted for seller.
- /admin/stories and seller use the same Story-owned list/editor logic; admin adds authorized owner selection + review. Role capabilities alone never authorize server data.
- Use existing AppDataGrid/Orders density, toolbar, saved views, pagination, search and compact eye/ellipsis via shared row-action component. Remove colored button walls and raw short-Guid seller labels.
- Default columns: thumbnail+title, human shop name or «ویترین اصلی», clear effective display/review state, slides, display start/end, last change, compact actions. locale/market/technical IDs not default columns. Dates Jalali/Persian digits through shared helpers; configured timezone, no raw ISO/IANA in operator copy.
- Server query for BOTH roles. No seller createClientGridQueryAdapter over fetched list. Search/filter/page/count from the same owning query; human shop filter before paging. Status filters must match server publication/review facts, including elapsed Scheduled visibility; no browser-invented approval/publication policy.
- Saved-view/query/draft/intent scopes separated by actor+role+shop; use existing saved-view authority lawfully, no borrowing admin permissions. Switching shops must not paint old rows/detail/intent into the new surface.
- Detail/editor reads fresh GET by id, not rows.find from an old list. Use stable detail/new routes or an explicit routed workspace under each existing shell. No sudden inline form below the grid. «برگشت به فهرست» preserves lawful list state.

## B — Shared editor a merchant can operate
- Plain labels: «عنوان»، «فروشگاه»، «عکس یا ویدیو»، «اسلایدها»، «پیش‌نمایش»، «ذخیره پیش‌نویس»، «ارسال برای تأیید». No mediaUrl/assetId/CTA/locale/version/Host/header instructions or prefilled demo images/targets.
- Admin chooses «ویترین اصلی» or a real searchable shop NAME; shop ownership remains that shop and creator is admin. Seller owner fixed by existing context, not an editable Guid. Owner change cannot reuse foreign uploaded assets or an old unresolved act.
- Select/upload actual files via correct Media scope: seller /v1/seller/media; admin-for-shop /v1/admin/media/upload-for-shop; platform existing admin upload/library. Ready asset ids stored, no seller raw-URL bypass or admin header inside seller calls.
- Show upload progress/error per file, thumbnail, image/video kind, preview, reorder/edit/remove slides, cover selection from lawful assets. No upload on merely opening/cancelling a dialog. Do not announce a file ready or story saved before actual successful owner response.
- Local draft may be incomplete; submit/publish obey server content gate and actionable per-field messages. Keep input after failures. An unchanged edit must not erase stored unsupported/legacy fields with a guessed default.
- Locale choices from enabled DB language registry, default from configured context. No hardcoded fa/en list or default fa. Seller reads through existing permitted registry seam; if missing, a minimal read-only owning Contracts/CQRS integration is authorized, not admin grants/seed/settings changes.
- Optional destination in business terms «با زدن دکمه چه چیزی باز شود؟». Use actual product/category/article pickers where supported, not typed Guid. Product picker is shop-scoped, platform can pick a valid public product. Resolve navigation through canonical Catalog/Content routes; raw Guid must not become /{Guid}. External links only accepted safe existing schemes, with deliberate label. Do not offer an option with no real read/navigation support.
- For a product sold by multiple shops, verify actual selected-shop eligibility through the owning Catalog/Offer authority, never a first/cheapest-card seller shortcut. Keep foreign/unavailable product refusal. Minimal read/adapter binding correction is in scope; no pricing/offer lifecycle redesign.
- Short actions (reject/stop/delete/schedule/submit confirmation) in ONE existing design-system Dialog, with focus/escape/cancel/return-focus and 44px controls; no window.prompt/alert/confirm or stacked modals.
- All feedback through existing app react-toastify container; do not add another provider/container or custom ToastRegion. Inline field errors remain inline. Plain Persian actionable text; diagnostic codes only in technical detail.

## C — Reviewed actions and one honest publication lifecycle
- Render executable actions from server actionCodes + actual role/permissions, not only hardcoded reviewStatus. Clearly show next step/rejection reason.
- Seller: draft -> submit -> pending; rejection reason -> edit -> resubmit. Approved story requires lawful stop/return-to-draft before edits; new content awaits approval again. Keep server version fences.
- Admin preview MUST show the actual content/version being reviewed. Actions «تأیید و انتشار»، «تأیید و زمان‌بندی»، «رد با دلیل» explicit. Same fresh reviewed version travels on command; stale response means review again, not silent fresh-version retry.
- Use composite owning commands, not approve+enable in two browser writes. Existing stored decisions/history displayed truthfully. Platform draft publication uses lawful owning paths; don't fake seller submission or role.
- Schedule uses existing date/time UI + accepted timezone conversion; no invented mandatory 24-hour expiry. Show meaning of no end time. Future/not-yet-visible and expired facts clear, same policy in list/detail/public.
- Creation by admin on behalf of A must appear in A, not B, with correct attribution; no blind row approval without content review.

## D — Exact retry and merchant work protection
- Story-owned small API/state modules, UI composition separated; do not grow one multi-thousand-line screen or import Returns feature internals.
- Freeze actor/role/shop/route/target/reviewed versions/occurrence/exact payload BEFORE request; verified read-back in existing browser storage. Every retry sends identical key/body/target. Remove API fallbacks that create a new random key on each retry; new key only for a deliberately NEW reviewed act.
- Same-tick double click -> one request. One command chain (create -> add slides -> submit) records each successful step and continues only unresolved steps, with each next reviewed version obtained from actual prior success. Do not mint another story/file because a response was lost.
- Unknown transport/5xx/conflict/authorization-after-unknown does not erase a possibly-sent intent. Keep recovery visible, exact retry/manual reconcile; no auto-retry/polling or blanket 4xx = no effect.
- Unmount/route/shop switch fences GET, mutation finish, finally and toast so late responses cannot overwrite/release another context. Pending acts cannot overwrite unrelated pending evidence. Protect edits when leaving.
- Upload recovery: retain key + file identity/hash/metadata, not raw bytes in sessionStorage. If reload loses file handle, ask plainly to reselect the SAME file and verify identity before replay; never substitute another file under the key. No new blob-storage framework.
- Storage unavailable => clear merchant notice and no possibly non-recoverable send. Read-only browsing remains usable.

## E — Honest public playback and destinations
- Preserve existing Shopeiva home/rail geometry/theme; change Story feature only, no Home/PDP/global layout redesign. Follow critical-storefront protection.
- Show only server-eligible published stories in approved order, correct shop name and canonical public shop link. Missing identity => honest no link; no fabricated public id. Draft/pending/rejected/stopped/future/expired must not leak via direct link.
- Images use persisted duration and pause/advance correctly; videos advance from real loaded metadata/currentTime/ended, with pause/mute/error and no independent loop racing a timer. Keyboard/touch and close behavior accessible. Error on one asset is actionable and does not trap user.
- Resolve product/category/article destinations through actual route authority; never navigate /{Guid}. Minimal additive public resolved-link/read integration allowed if needed; keep owner/product checks on writes.
- Share links open THAT eligible story at a stable implemented public entry (e.g. existing localized home + story query). After expiry/stopping/reload it must not retrieve protected admin detail or invent public visibility.
- Remove/hide fake local-only likes/comments/saves/report controls unless backed by real durable handlers already present. Do NOT implement a social subsystem/analytics in this task. Preview fakes may stay ONLY under existing explicit preview-fill boundary, never actual seller/admin/public live data.
- Adding a separate story rail to seller-profile is not required here; canonical shop attribution/link IS required. Record that distinction, don't claim an unbuilt profile feature.

## F — Focused connected proof + owner handoff
Deliver behavior, not source-regex tests or only a served chunk.
1. Gate 0 owning-module and real PG rollback proof as above; exact tenant/shop/action collision negative, later-save containment, actual EF branch.
2. Real mounted shared workspace + REAL VendorShell integration: story.view allow/deny; seller has no admin moderation/owner picker; admin selection; shop switch fences stale response; server paging/count/saved-view isolation; no live role fixture.
3. File/media controlled port: upload Ready -> cover/slide -> draft -> submit; rejected reason -> edit/resubmit; admin preview -> publish/schedule; admin-for-shop creation in correct seller scope. Tests drive registered endpoint/command shapes, not fake success independent of owner contract.
4. Unknown create/add/upload response, exact retry, remount recovery, same-tick double-click, stale reviewed version, storage failures, late mutation reply; assert byte-identical keys/body and zero duplicate logical children.
5. Public image/video/CTA/shop-link and direct share with eligible/ineligible cases; stable player and no fake social persistence. Test real route/destination resolution, not a mocked arbitrary href.
6. Desktop 1440 + mobile 390 served DOM/layout/text checks: admin list/editor + existing VendorShell list/editor + public player where lawful. Local grid scroller allowed, document overflow 0, 44px new controls, no technical IDs/jargon/default demo values. No screenshots per owner rule.
Focused frontend suites + tsc + production Next build; npm run test:critical-storefront mandatory for touched Story storefront components. Gate-0 backend focused tests and affected guards/build/publish only; full repository/Host suite or baseline replay not authorized. Fix actual regressions and rerun affected checks, disclose reruns.
Live is READ-ONLY on existing rows/assets (no create/update/upload/submit/approve/schedule/delete tests on real business data). No seed/demo story/user/role fixture or permission/setting changes. Controlled mounted/endpoint/object-store fixtures and canonical rollback rows are allowed, no durable fixture. User performs the first live new-story act.
If existing seller session cannot be lawfully used, do not fabricate it: document exact handoff/login need; mounted VendorShell proof still required, do not claim served seller mutation tested.
Serve current Host5088/Next3000; Host refresh only if backend changed, with existing skip-development-bootstrap process flag and no seed. Actual process/path/hash/build-id + health/ready/platform; no DB/port/key/config change.
ZERO migration/DDL/backfill/reset/cleanup/restore/provider/notification. One canonical tooba_alpha, SingleStore/store-alpha. Preserve current Story rows and all Returns/payout/key state. No force/amend/rebase/reset/stash, no tracked scratch/keys/binaries.
Update AGENTS CURRENT/recovery/START-HERE/Story plan: exact accepted boundaries, current task, delivered URLs, what user should test, real remaining limitations. R5 original report untouched; review/addendum corrects its A4/C2 limits and closes only when Gate0 passes.
Evidence: RESULT, RECEIPTS, result-contract, Gate0-proof, connected-workflow-proof, media-and-intent-proof, public-player-proof, user-review.md (short Persian steps). Route-command-wire handoff from actual source, not an empty pointer.
User-review checklist: open /admin/stories, open /vendor-panel/stories in existing panel, create draft with own image/video, submit, admin preview/reject-or-publish, public playback/link; clearly mark which steps create/publish data. Do not execute those live writes yourself.
Success: Gate0 closed, existing panels deliver shared merchant workflow and actual public consumption, focused checks pass, served user-review URLs usable. Worker PASS != Architect/user acceptance; no universal Marketplace/provider/commercial readiness claim. Don't mark complete on placeholder editor, raw URL inputs, missing seller panel, empty-only preview or untested unknown recovery.
Post ONE canonical result with Task-ID, Status, base/code/evidence/final SHA + fresh origin parity, Gate0/A–F states, User-Work-Preserved, validation+limits, User-Review-URLs, Recovery-Next-Task NONE, Next-Recommended-Task NONE, automaticNextImplementationTask NONE, STOP_FOR_ARCHITECT_AND_USER_REVIEW. Stop; no automatic continuation.

END_TOOBA_TASK

