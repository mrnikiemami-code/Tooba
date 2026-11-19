PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R5
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R4
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Correct slide product names and finish connected owner-test handoff
Mode: R4_F1_F2_CLOSURE_ONLY_NO_NEW_FEATURE_PHASE
Expected-Base-SHA: 37cceaa9cf33505dd394ed9d4ede32aee465e45d
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Goal / authority
Close ONLY F1/F2 of R4 ARCHITECT-REVIEW in one focused delivery so the owner can test. Read current AGENTS/applicable feature instructions, OPERATOR-UX-ACCEPTANCE and docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R4/ARCHITECT-REVIEW.md.
Retain R4 shared DS slide dialog, compact save/localized upload, per-slide UPDATE route, list title-first defaults, existing logical-end operations pin, frozen executor and catalogue search. No redesign, extra discovery phase, 281/Gate0/Media baseline sweep or unrelated landing restyle.
User explicitly wants a merchant-ready shared form: seller owns current shop/products and submits; admin chooses shop/main storefront at creation and moderates. Existing VendorShell/menu/auth, no duplicate seller panel. One worker, no delegation/screenshots/polling loop; no automatic next task.

## A — F1: truthful per-slide product identity after reload
Current destinationLabel uses productLabelById[id] ?? selectedProductLabel: an unrelated current editor selection can label another slide. Map starts empty and is filled only by selection/save; existing product names are not hydrated on reload.
- Remove all cross-slide/global-selection fallback. Every displayed product name must be bound to THAT slide's own ctaTarget and current owning scope.
- Resolve stored selected product ids through lawful owner Contracts/read/DTO (reuse existing authorities where possible), including targets outside the current search page. Do not scrape current picker page or search all products as a fake id lookup.
- Keep valid target identity intact when an item is absent/unavailable. Show explicit unavailable/unknown state; never invent a label, erase a target, replace another product or silently select the first option.
- Hydrate/cache by proper product+shop/context identity; late reads from another story/shop/actor cannot overwrite current form. Editing/cancelling one slide cannot change labels on others. Selected product remains clear across search/page changes and full remount.
- Existing media identity retained when changing caption/duration/product. No delete/re-add/upload merely to edit product.
- Small additive owner read/DTO if necessary is authorized end-to-end (Contracts/CQRS/validator/catalogue as actually required); no schema, new permission bypass or generic search framework.
Focused real-component counterexample: two stored slides with different products A/B, selected target B outside current picker page; mount/reload both names correct; choose A in one dialog then cancel; the other row continues to show B and neither stored target changes. Also unavailable product is explicit and another shop's labels/results never leak.

## B — F2: prove actual edit command, uncertainty and grid behavior
Extend supported REAL mounted workspace/dialog tests (no regex/source scans, no disableLiveGrid). Capture actual transport.
1. Admin AND seller edit existing slide: change product/caption/image duration, save -> exactly one owning PUT to expected item, original media id/url retained, current reviewed version and one occurrence key frozen; zero upload/delete/add. Genuine bound response closes with success; cancel/Escape sends zero requests.
2. Same real workspace save with transport loss then empty/non-JSON/wrong-story 200: exact stored envelope unchanged, visible unresolved card, no success/navigation; genuine same-command replay clears only matching envelope. Malformed upload starts no add; lost add response replays only frozen add. Retain existing tests where already sufficient, add only missing assertions.
3. A real precise NEGATIVE CONTROL for malformed-success retention: temporarily restore pre-fix production completion behavior, run the SAME new retention fact and record its failure, then restore current source and final green. Simply asserting current verifier rejects {} or commenting "would fail" is NOT the experiment. No weakened assertion or committed broken code.
4. Real AppDataGrid default AND old saved column-order fixture for admin/seller: title first data column; operations last and fixed logical-end when horizontally scrolled. Existing bridge already supplies end pin; do not rewrite shared grid or claim pin was newly implemented. Preserve unrelated filters/sort/widths. If old Story view state overrides constraints, fix Story integration only.
5. Read-only DOM/geometry at 390/1440 verifies compact save, owner name, product edit/picker, grid scroll/end pin and document overflow 0. Use actual served pages for available data and explicitly labelled controlled HTTP/component states for unavailable active states; no live business fixture/write. jsdom by itself is not a browser geometry proof.
Keep existing actor/shop fences, exact bytes, allowed route+body identity and backend actionCodes/lifecycle. Published content must use existing lawful return-to-draft/review, seller never self-approves.

## C — actual Catalog query proof (not a fake implementing the algorithm)
R4 FakePublishedSearch implements filtering/count/paging itself. Its SamplerUsed is never assigned. Those assertions prove adapter delegation, NOT CatalogPublishedProductSearchLookup.
Exercise the ACTUAL owning CatalogPublishedProductSearchLookup with >40 products: 55 eligible Published rows plus excluded rows, search match beyond page one, disjoint pages, count from same eligible set, nonpublic exclusion. No Development sampler registration. Retain R4 seller Offer/title adapter isolation proof; add ownership evidence only if A changes that path.
Use canonical PostgreSQL rollback-only fixture if the repo's existing harness supports it; otherwise a genuinely controlled EF provider on the REAL Catalog query with explicit provider limits (not fake published-search port) suffices for filtering/page/count proof. ZERO committed fixture rows, no second DB/EnsureCreated/seed.
Do not convert this into full Catalog recertification.

## D — validation, truthful evidence, served owner handoff
Run only new/affected focused FE units/mounts, tsc, Next build and affected backend tests/guards if contract/source changed. Build/publish backend ONLY if production backend changed; use TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1. Declare reruns/failures truthfully. No whole repository/Host/AMSC baseline sweep. Known unrelated critical-storefront bg-white failure remains disclosed; no guard weakening.
Preserve historical reports; add corrections that R4 mount opened edit only and R4 fake catalogue test was delegation evidence. Do not rename old proof as stronger than it is. Ledger A-D -> actual production method/test/provider and observed result; remaining gaps explicit.
Refresh affected served artifacts; record final Host5088/Next3000 identity, BUILD_ID, routes/health/ready/store-alpha. No claimed live creation/upload/submission/moderation walkthrough when only controlled routes were exercised.
Short Persian user-review handoff:
- /fa/admin/stories: title first/actions fixed; new main/shop story; existing slide edit/product.
- /fa/vendor-panel/stories: fixed shop, own product, send for review.
- /fa: actual approved eligible Story/public product/shop link after OWNER performs real test.
Explain only user actions, no technical jargon. Worker PASS != Architect ACCEPT != user acceptance. Stop for review; no next task automatically.

## Boundaries / repository recovery
Canonical tooba_alpha@127.0.0.1:5432 SingleStore/store-alpha, Host5088 FE3000 only. ZERO migration/DDL/backfill/seed/reset/cleanup/restore/live Story-Media mutation/provider/notification/money/stock/key operation. No demo business rows. Controlled/rollback-only tests only, committedRows=0. Generic Media pipeline unchanged.
Preserve all user data, Returns acceptances, payout backlog, separate contained key state. No config/edition/grant/connection changes. No screenshots/delegation/parallel agent.
Verify Expected-Base ancestry and incoming delta; docs-only issuance permitted. Normal commit/push/fetch parity; no force/rebase/amend/reset/stash, no keys/scratch/binaries staged.
Exact issued TASK.md + receipt at docs/ai/tasks/<Task-ID>/TASK.md and docs/architecture/evidence/<Task-ID>/TASK.md. If Bridge content unavailable, use committed exact envelope and disclose it; do not reconstruct scope.
Update CURRENT in AGENTS, TOOBA-RECOVERY-CONTEXT, START-HERE, Story plan and seller requirement consistently: this one worker result awaits Architect then OWNER test. User asked: after each result, Architect reviews and either announces ready for test or prepares a narrowly justified next task. Worker does NOT issue that next task.
Return code/evidence/final SHAs, fresh origin parity and honest limits. STOP_FOR_ARCHITECT_AND_USER_REVIEW.

END_TOOBA_TASK
