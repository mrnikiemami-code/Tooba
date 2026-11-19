PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R4
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R3
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: One merchant-ready Story editor, Orders-style list, and final S1/S2 closure
Mode: CONSOLIDATED_USER_REPORTED_UX_AND_REMAINING_S1_S2_ONLY
Expected-Base-SHA: f35f476c51346858bb1d61e96bb5e7d2068c5a79
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Outcome / one task
Finish the user's concrete admin/seller Story experience AND the two remaining R3 obligations together. Do not split into another discovery task or ten repair phases. Read current AGENTS, applicable existing feature/architecture instructions, OPERATOR-UX-ACCEPTANCE, seller integration requirement and R3 ARCHITECT-REVIEW. Execute one worker only; no delegation, screenshots, polling loop or automatic next task. This issuance is not a claim that the worker is running.

User contract (explicitly confirmed):
- Same complete, simple Story form in BOTH panels. Seller owns only its current shop/products and submits for approval. Admin sees all stories, selects a named shop OR main storefront on creation, and moderates.
- List like Orders: title FIRST data column in RTL reading order, actions LAST and pinned at logical END during horizontal scroll.
- Every slide can have its own selected product, with a clear product name, and can be edited later without delete/re-upload.
- Compact buttons, named selections and understandable Persian. Existing screen is NOT accepted merely because hidden code exists.

## A — Orders-style list in both panels
Use the actual Orders AppDataGrid configuration and row-action primitive as reference. StoryLiveGrid currently defines title first but actions lacks sticky end. Compare the runtime layout, not just the array.
Title (thumbnail + title link) first data column after the existing selection checkbox; business columns in between; operations last with logical-end pin and Orders-style compact view/menu. Do not pin title differently from Orders without a concrete need. Correct RTL pin behavior, header/body alignment and scroll; title must not appear after operations.
Use existing saved-view/state authority. Handle old Story column-order/pinning state so the required title-first/actions-last/end constraint is applied; preserve user's unrelated filters/sort/width/preferences, scope by actor/role/shop, no global localStorage reset or Orders restyle.
No raw GUID/market/locale technical columns in the default merchant view. Keep truthful shop/status/slides/date cells, Persian/Jalali formatting and scoped server query. Verify admin AND seller, default AND an old saved column-order fixture.

## B — one shared merchant editor, correct role differences
Keep the purchased template's EXISTING VendorShell, /vendor-panel/stories menu and story.view authority. No duplicate seller panel/menu/login/AdminShell, no admin credentials/headers in seller path.
Admin creation: obvious named choice "ویترین اصلی" / "برای یک فروشگاه"; named searchable shop picker required for the second. No raw shop id input. Seller: current shop shown by NAME, fixed to its authenticated context, no shop selector.
Both create/detail screens show a clear shop/main-storefront identity. Existing Story ownership and uploaded media must NOT be silently reassigned to a different shop to make a selector look editable. Changing ownership of existing stories is out of scope; explain current owner plainly.
Make title/language save compact (remove full-row stretching from md:col-span-2, use a normal action area). Reuse template/DS buttons, Dialog, field spacing and current single react-toastify container; field validation inline. Replace visible browser-English native file control with the existing localized upload pattern, retaining accessible file input.
Both panels share the same slide form/component behavior, with responsive grouped fields rather than long unexplained full-width controls. Preview before final send/publish; cancel/Escape sends nothing. All new action targets >=44px and no document overflow at 390/1440.
Do not turn this into a global theme rewrite or a new Story designer.

## C — clear per-slide product selection AND editing existing slides
Each slide card displays thumbnail, caption, duration, and its named destination/product (or "بدون لینک"), plus clear "ویرایش" / remove / reorder controls where server actionCodes allow.
New slide and edit slide use the SAME shared DS dialog/form. Offer readable destination choices and searchable product selection. A merchant must not discover product selection by guessing a hidden technical enum. Keep explicit "بدون لینک" choice; do not force every story to be a product advertisement.
Seller/shop-owned admin story: ONLY the owning shop's lawful eligible products. Platform story: actual eligible published catalogue. Product choice uses names and existing server search/page/count; selected name survives pagination/empty/error states. No raw product id entry, development sampler or client first-page-only search.
Editing an EXISTING slide can change its caption, image duration and product/destination using the existing AdminUpdateItem/SellerUpdateItem owning route and reviewed version/occurrence. Preserve existing media identity unless the merchant deliberately replaces media using the existing safe upload path; no forced delete/re-add/re-upload to change a product. Inspect actual endpoint DTO before wiring; no guessed fields.
Carry product name through lawful existing reads/lookup contracts if snapshot only has an id. Keep existing valid targets visible even if a picker page does not currently include them; unavailable target is explicit, never silently replaced or erased.
Published/reviewed content respects owning lifecycle and actionCodes: where editing is forbidden, show the existing lawful stop/return-to-draft/review step instead of bypassing it. An empty "Active" legacy story is not falsely advertised as publicly playable. Seller cannot self-approve. Retain backend publication/ownership/media/CTA gates and actual canonical public product/shop links.
If a small additive DTO/action token is required for existing update-item, implement end-to-end with the actual owner, validators/catalogue and focused proof; no schema, new ownership model or permissions workaround.

## D — finish existing R3 S1/S2, do not reopen closed backend waves
Read R3 ARCHITECT-REVIEW at this Expected-Base.
S1: runDurableUploadAdd bypasses bindStoryEnvelopeRoute. Bind EVERY effective send stage (upload, add, update-item and recovery) at the actual fetch boundary to exact allowed role/method/path/target/body/version/occurrence. Anchor upload routes fully; reject appended/foreign paths. Do not duplicate the journal. Unknown/malformed/wrong-target 2xx keeps BYTE-IDENTICAL envelope, no success/navigation/child advance. Exact genuine replay clears only matching evidence. Keep actor/shop isolation, same-tick executor lock and upload/add child recovery.
S2: real published catalogue search source landed. Supply the missing owning-query scenario proof: >40 products, search match beyond page one, disjoint next pages and accurate count, two-shop eligibility isolation. A fetch stub returning totalCount 55 is not a 55-product proof.
Update evidence ADDITIVELY: R3's five tests are direct tests (mount/flush are unused), not workspace mounts; its named negative-control test only calls the new verifier. Do not rewrite original reports or repeat inaccurate claims. Actual precise negative control must fail the same retention fact against pre-fix production behavior, restore code, then green.

## E — small connected acceptance matrix, then serve for owner test
Use supported repository Node/tsx runners and REAL mounted shared editor/workspace plus actual grid harness (no disableLiveGrid bypass, no regex posing as mounted).
Focused cases:
1. Admin create for main storefront vs named shop; seller context fixed; no cross-shop product.
2. Admin/seller lists title first and actions last/end-pinned, including old saved view and runtime horizontal scroll at 390/1440.
3. Create slide with selected product; existing slide edit changes product/caption/duration via UPDATE only, exact version/key/body, no unwanted upload/delete; cancel zero requests.
4. Forbidden published edit offers owning lifecycle step; seller submit vs admin approve; truthful empty/unknown/read errors and no stale scope result painting.
5. Malformed/unknown mutation response leaves visible pending card, no toast.success/navigation; valid replay clears. Malformed upload never starts add; lost add retries only frozen add. Wrong upload/add/update-item route/method/target/body => zero fetch.
6. Actual owning catalogue >40/search/page/count and shop isolation (controlled/rollback-only fixtures, no live seed).
Tests should exercise real behavior, not mirror helpers. Do not require a whole-repo baseline sweep. Run affected Story guards, focused backend tests if backend changed, affected FE units/mounts, tsc and Next build. If backend changes, Release build/publish Host with existing skip-bootstrap. Declare real failures and final reruns truthfully.
Read-only served DOM/text at 1440/390: both list and editor layouts, compact save button, owner name, product picker/edit action, pinned operations and zero document overflow. Controlled active states must be labelled controlled; do not fabricate a live successful end-to-end walkthrough.
Refresh affected served artifacts, verify final Host5088/Next3000 identity/BUILD_ID/routes/health/ready/store-alpha. Known unrelated critical-storefront bg-white failure stays disclosed; do not restyle it or weaken its guard.
Handoff short Persian checklist with URLs: /fa/admin/stories, /fa/vendor-panel/stories, /fa. Owner tests creation/upload/submission/moderation/public rendering after result review. PASS != Architect ACCEPT != user acceptance.

## Boundaries / git / durable recovery
One canonical tooba_alpha 127.0.0.1:5432 SingleStore/store-alpha, Host5088 FE3000. ZERO migration/DDL/backfill/seed/reset/cleanup/restore; ZERO live Story/Media business POST/upload/provider/notification/money/stock/key operation. No demo business rows. Controlled fixtures only; PostgreSQL proof if used rollback-only committedRows=0, no second DB/Docker/EnsureCreated.
Preserve Returns acceptances, payout-method backlog, separate contained key state and all user work. No global grant/config/edition/connection change. Existing schema and generic Media pipeline untouched.
Verify expected-base ancestor and incoming delta before work; docs-only issuance delta permitted, source drift must be disclosed. Normal commit/push/fetch parity only; no force/rebase/amend/reset/stash. No keys/scratch scripts/binaries in git.
Persist exact issued task bytes + issuance receipt at docs/ai/tasks/<Task-ID>/TASK.md and evidence/<Task-ID>/TASK.md. If Bridge content absent, use committed exact envelope; declare source instead of reconstructing scope.
RESULT/RECEIPTS plus compact acceptance ledger: each A-E item -> real method/test/provider/served observation or explicit remaining defect. Keep historical evidence unchanged.
Update AGENTS CURRENT, TOOBA-RECOVERY-CONTEXT, START-HERE, Story plan and seller integration CURRENT consistently: this single task result awaiting review; no next task auto-issued. Record remaining defects honestly, not blanket PASS. STOP_FOR_ARCHITECT_AND_USER_REVIEW after result.

END_TOOBA_TASK
