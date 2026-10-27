PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-STOREFRONT-DESTINATION-CONFIGURED-TRIAL-230
Parent-Task: TB-SHIPPING-STOREFRONT-AUTHENTICATED-READ-TRIAL-229-R2
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: FINISH_DESTINATION_REFRESH_AND_ONE_REAL_CONFIGURED_METHOD_TRIAL
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD30min / source stop12 / real trial14 / recovery+commit4. One correction max.
REUSE R2 account/profile/cart/retained binary/CDP driver. No new driver, auth setup, audit or price engine.

FIRST
Read TOP/review229-R2, accepted229-R1 corrections, section29 and compact-history decision.
R2 ACCEPTED only normal buyer authentication+200unavailable read. NO configured price/calendar or card
visual acceptance: existing screenshots are HERO/address crops, not group cards.
No migration replay (228 already applied), no backend rebuild unless a real backend change requires it.
Verify actual current Host/FE identity and previewON/count7, don't trust historic PIDs.
Task account01a17cf7-e38d-7000-82a5-00c57929bc54 and cart01a17cfd-c1b1-7000-b10d-8cf21258a1cd already exist.
Explicitly authorized to reuse ONLY task-owned D:\temp\tb229r2-chrome profile/session and its driver; never
enumerate/select user tabs. If session expired use existing BFF login with task credentials from nonrepo
%TEMP%\tb229r2-acct.txt; no secret in output/git. Do not register another account.

A SOURCE DEFECT — CAPTURED DESTINATION NEVER REFRESHED
In storefront-shipping.tsx, initial/province refresh calls omit destination. Derived destinationIdentifiers
changes do not trigger a refresh; only legacy onSelectMethod passes the destination, but captured mode hides
that selector. Reproduce with the existing page/network, then wire captured preview to changes in CURRENT
verified geography destination after asynchronous reads settle. No Save/Apply/method-selection prerequisite.
Clear parent => clear lower identity; discard superseded/out-of-order preview completions. Use stable identity
primitives/request generation, not object-reference render loops. Pending new destination must not leave old
quote looking current. Ordinary refresh must preserve typed recipient/address input; avoid repeated draft
rehydration overwriting current edits. Legacy OFF behavior stays unchanged.
Add focused regression for actual refresh coordinator/state path: destination transition causes exact current
ids; stale response ignored; no repeated request on unrelated render; clearing destination removes stale
price. Do not invent source-regex tests or a general fetch framework.
Header shippingDaysLabel currently displays legacy maxPrep+lead (R2 screenshot3days). When captured present,
show existing unknown dash or method-specific actual receiving facts; never legacy delivery promise. Keep
existing header/card styles. No invented aggregate min/max across independent groups.
ONE final frontend typecheck and only new focused check. No188/194/backend/solution replay.

B ONE CONFIGURED POSITIVE LOCAL READ — NORMAL BUSINESS APIS, EXPLICIT DEV TEST DATA
Existing user post/NULL draft01a16eeb-70b3-7000-87c2-564c82835496 is weight-gated. DO NOT remove its bounds,
edit its rates/week/type, publish another revision, alter its pointer, or invent physical measurements.
Read current catalogs/drafts/pointers first. If a separate existing genuinely suitable configured method
exists, use it without destructive edits.
Otherwise this task authorizes ONE clearly named task-owned DEV draft on an UNUSED existing active REAL
service/option binding, using normal owner-authorized create/editor-document/publication APIs.
No invented service/option/type ids, no catalog seed, no duplicate binding workaround/rawSQL.
Task-specific test configuration (not runtime defaults/product policy):
active existing type heavy (or another real type distinct from the ordinary existing user method);
FA name "آزمایش ارسال۲۳۰"; one real Iran country destination; IRR; Manual+Prepaid;
ONE unconditional row (all physical/merchandise conditions absent), freight120000, packaging5000;
shared receiving range2..3 working days; Asia/Tehran, explicit7days: Saturday..Thursday09:00–17:00,
Fridayclosed. These are intentionally AUTHORED TEST values through normal API, never hardcoded fallback.
Use actual returned revision ids/tokens. Publish/capture ONLY this task-owned draft through existing API.
Record exact write receipts and ids; one shared snapshot, not per cart/group. Do not hide it as production data.
If no unused lawful binding exists, STOP this setup with exact blocker; never repurpose a user-owned draft.
Read original Offer assignment then set ONE existing task-cart DEV offer through normal authorized228 API
to Platform+same real type. Keep/add a second real offer as an unassigned or independently refused group.
This may mutate only the bounded test Offer assignment and test cart; record before/after. No bulk backfill.
Read API values first; do not overwrite a non-task Offer already explicitly assigned without preserving/
restoring its original value or choosing a different unassigned DEV offer.
User standing local-development authorization covers these exact new test-data actions; do not ask again.
No auth bypass/role injection, no newframework/route, no production/external DB.
If positive setup cannot finish, truthfully report NOTproven; unavailable screen is not a substitute.

C ACTUAL PAGE / POSITIVE RECEIPTS
Using NORMAL UI choose a real country/province/city available in this DB; wait for verified Geography branch.
Without clicking an old method selector, confirm new projection POST carries exact destination ids and200
contains a priced group with freight"120000"/packaging"5000" (scale may be provider .00), actual current
group goods after discount+tax, exact selected capture provenance, and receiving dates from the authored
week/min2. Compare earliest against actual server clock in method timezone; closed Friday skipped, no extra
seller prep. A second unavailable/refused group must stay visible without erasing the priced group.
Click different offered dates: THAT date's interval shown, no common legacy9-12. Reload preserves authored
data and normal session, not synthetic request replacement.
Desktop1440 and390 screenshots MUST SCROLL GROUP CARDS INTO VIEW before capture. Read card bounds from DOM
and assert the card intersects viewport; inspect PNG bytes by eye. Native viewport crop is not enough if it
only shows hero! Show first priced card + interval, and other group's refusal (separate mobile scroll shots
allowed). Product lines/images visible, one source/no duplicate legacy cards, no horizontal overflow.
Receipt for single-line path already exists; only inspect if destination fix affects it, don't repeat cart
rebuilds for confidence. No user Visual ACCEPT from your own screenshots.
No mocked/intercepted traffic, DOM injected data or price. No select/save/commit/order/payment/reservation.
Existing server not_ready guard remains; local preview can stay ON for review, checked-in defaultOFF unchanged.

FINISH
Evidence: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-DESTINATION-CONFIGURED-TRIAL-230/{TASK.md,RESULT.md,
configured-read receipts, real card screenshots}. Scrub auth credentials/cookies, no sensitive logs.
ALL THREE recovery TOPs updated (AGENTS, START-HERE-IF-CHATGPT-IS-LOST, canonical shipping recovery):
actual source refresh fix, exact dev writes, method/draft/capture/Offer/cart ids, current runtime/option,
trial URL, precise positive/refusal proof and remaining parity/compact-history gap.
Retain known R1 cross-owner label-map/decoder limits,227 mobileempty-note residual and225-R1 unrecovered
unsaveduser-tab loss. No Marketplace/checkout acceptance implied.
Do not issue next parity task yourself. Exact-path scoped commit/push/fetch parity; no add-A/history rewrite/
Bridge or post-result scripts. Actual elapsed and blockers honest; STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
