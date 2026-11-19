PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R2
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R1
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Make exact recovery work, use lawful lookups/date controls, and serve the tested product
Mode: FOUR_SOURCE_CONFIRMED_BLOCKERS_ONLY
Expected-Base-SHA: d0c09e2ed340bdf82c9a6e4ae8eaeb8f7ed96d58
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## هدف و قواعد
فرم و routeها و Gate0 سالم R1 حفظ شوند. فقط B1–B4 بررسی معمار را تمام کن؛ ادعای PASS روی recovery نمایشی یا URL نسخهٔ سرو نشده ممنوع.
One worker; no delegation/subagents/screenshots/polling/automatic next task. Verify main/origin/base; docs-only issuance delta allowed. No rebase/amend/force/reset/stash. Exact Bridge bytes + receipt, no reconstructed task.
Read R1/ARCHITECT-REVIEW، AGENTS، feature skills و architecture standard برای فایل‌های متاثر؛ الزام VendorShell/story.view و محصول ساده برای کاسب محفوظ.
بازیابی معمار باید بعد از نتیجه دقیق باشد. هیچ task جدید یا پیش‌فرض 'wake ended' به‌عنوان blocker نساز.

## B1 — دکمهٔ ارسال مجدد واقعاً همان فرمان را بفرستد
Current pending button only shows toast and opens detail. Replace it with actual durable dispatch:
- One Story-owned executor takes a validated frozen envelope and sends its method/path/body BYTES + frozen actor/role/shop target scope. No arbitrary closure rebuilding body from current form or storage.
- All create/update/item/submit/review/schedule/stop/return use that executor, not ad-hoc handlers. Store complete actual DTO (including null/default transport fields), not a subset. Same key cannot change body/target/scope.
- Namespace includes actor+role+shop (and existing tenant context where available); read validates known action/route/method, required body/versions/target and current actor scope. Never persist bearer/cookies/keys. Same-actor transport authentication must still be checked.
- Pending/unreadable v1/v2 evidence never becomes missing or gets overwritten. Explicit recovery card survives failed fresh GET. A legacy envelope that lacks enough proof is blocked honestly, not guessed/rebuilt/deleted.
- Unknown then 4xx/auth/conflict retains evidence. Terminal handling needs validated real response, not ok=true + undefined. Compare-and-clear exact current envelope only. No blind discard.
- Lock begins BEFORE async digest; all same-tick action bursts one send. Fence GET, response handlers/toast/finally for target/actor/shop/unmount; old finally never releases another scope.
- upload journal must persist digest/name/type/size and every frozen add field (caption/duration/CTA/story/version/key). After upload success, atomically compare/update from exact previous journal to record asset id + exact next add body before sending add. After unknown add, retry ONLY add; no reupload/new key. Reload asks for SAME file only when unresolved upload really needs its bytes and checks digest.
- Immutable pending content vs explicit child advancement are separate operations; store does not allow arbitrary rewrite merely because key matches.
- Recovery button dispatches stored command even with fresh detail unavailable; request and successful resolution visibly clear only that evidence. Merchant labels, no action enum/key fragments in normal card copy.
Split executor/state/editor/lookup responsibilities into small Story-owned modules while touching them; current 1500-line screen is not an excuse to add more inline handlers. No new generic framework/Returns imports.

## B2 — تقویم و lookup قانونی، نه هدر نقش دیگر
- Replace raw YYYY text + year1404 example with existing standard DS Persian/Jalali date/time control and the accepted configured timezone authority. Remove browser-local Date(value) as conversion authority and hardcoded-zone label pretending to convert. Picked day/time -> correct canonical ISO; absent end explicitly means no end. Field errors keep draft and send nothing.
- Seller languages via existing permitted registry read or minimal owning read-only CQRS/Contracts adapter; NEVER loadAdminLanguages/adminHeaders inside seller. Enabled/default language from DB registry; no hardcode or grants/settings/seed changes.
- Admin-for-shop product selection via authorized admin/catalog read for selected shop, not seller header/session. Platform can select valid public products without a shop. Seller product list only own permissible offers; actual Catalog/Offer eligibility, not winning/cheapest product-card SellerPartyId.
- Minimal additive read contracts/adapter/query allowed; no schema/pricing/offer lifecycle redesign. Preserve correct /products/{slug} public href. Counterexample: shared product A+B with card winner B is still selectable/lawful for A if A has eligible offer; foreign-only product refused.
- Name picker/editor controls already present stay; don't rebuild unrelated panels.

## B3 — real shell/grid/proof and scope change
- Subscribe to same-tab existing VendorShell actor/shop changes (or its actual existing state provider); reading localStorage on an unrelated render is not enough. Same-tab and unmount invalidate query/detail/forms/views/intent scope without polling or new login/panel.
- Remove production disableLiveGrid test bypass. Isolated editor tests may mock low-level grid at a neutral test boundary; full grid query/sort/filter/saved-view and REAL VendorShell story.view integration still require proof. Use neutral repository harness, no imports from another product feature's test helpers.
- Diagnose act storm with a bounded reproduction; stabilize callback/router/query identities where source causes rerenders. No arbitrary sleeps, skipped behavior, suppressing act warnings or artificial timeout to call it solved.
- Actual mounted tests: unknown create -> remount -> recovery CLICK sends identical bytes; upload success/add unknown -> only add replay; same-key changed body blocked; actor switch/malformed storage/throw/discard; old response/finally; real shell permissions and grid query scope; date conversion from chosen Jalali day independent of browser zone; admin vs seller lookup URLs; public player events/product destination.
- At least one critical B1 or date test must demonstrably fail on pre-fix implementation/focused negative control; restore then final pass. A test simply named negative control is not a broken-implementation experiment.
- Don't treat checking a button exists as full workflow proof. Test method -> actual assertion -> provider -> run result ledger.

## B4 — platform Media identity + actual serving
- Preserve deterministic platform asset id scoped tenant+actor+occurrence and existing stored checksum/name/type/size/creator intent binding; distinct object key per attempt. Remove SellerPartyId=Guid.Empty/platformShop occurrence rows for platform. No fake seller ledger scope. No migration: existing asset row/PK and stored metadata already carry the needed platform proof.
- Replay by computed asset identity binds exact actor/tenant namespace + stored metadata. Wrong content/name/type/size conflict. Asset PK collision only exact schema/table/constraint, winner metadata validated, loser object alone cleaned, lost save ack success/object preserved. Use existing pre-effect constrained-transaction guard. Generic upload/seller paths unchanged.
- Test same key same actor replay; same key different actor/tenant is independently scoped; conflicting bytes; race attempt object integrity; unknown commit read; no seller binding/occurrence manufactured; caller state preserved. No live upload.
- After FINAL change run focused new behavior suites and affected guards, tsc, Next production build, Release backend build + Host publish if backend changed. Actual serving Host5088/Next3000 must match final code; record PIDs, loaded path/DLL, BUILD_ID/chunks and health/ready/platform.
- Host uses existing process-only skip-bootstrap; no seed/auth/config/port/key change. REAL served read-only DOM/text at 1440/390 for admin list/editor and VendorShell entry, same published build; no live mutations. If seller session unavailable, state actual handoff need; mounted shell proof still required.
- critical-storefront: preserve and narrowly verify the known unrelated landing failure; no unrelated restyle/assertion weakening. Report exact scope rather than all-green.
No whole-repo/whole-Host baseline sweep. Focused rerun after real fix permitted, report accurately.

## Data / delivery
One canonical tooba_alpha 127.0.0.1:5432 SingleStore/store-alpha. ZERO migration/DDL/backfill/seed/reset/cleanup/restore/live Story-Media POST/upload/provider/notification/key operation. Controlled fixtures / canonical rollback-only committedRows=0; no new DB/Docker/EnsureCreated. Preserve user data, Returns acceptances/payout backlog/seller shell.
Existing R1 report unchanged; CORRECTION-ADDENDUM for recovery/date/lookup/runtime assertions. RESULT/RECEIPTS/current wire handoff + B1-B4 exact ledger + short Persian user-review URLs. Don't claim served readiness without current build/listener evidence.
AGENTS CURRENT/recovery/START-HERE/Story plan/seller requirement to accurate state, one task only. No mandatory omission may be downgraded to a PASS limitation; real blocker needs actual failed operation/evidence, no guesses.
Success: recovery button sends exact stored request, upload chain resumes correct child, context safe, merchant date selection correct, lookups use proper authority, no fake platform seller scope, real behavioral proof and current served user-review URLs.
ONE canonical result with exact Task-ID, Status, Expected/Base/Code/Evidence/Final SHA+fresh parity, B1-B4 states, validation+limits, User-Work-Preserved YES, User-Review-URLs, Recovery-Next-Task NONE, Next-Recommended-Task NONE, automaticNextImplementationTask NONE, STOP_FOR_ARCHITECT_AND_USER_REVIEW. Worker PASS != Architect/user acceptance. Then STOP.

END_TOOBA_TASK

