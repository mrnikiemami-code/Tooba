PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1
Parent-Task: TB-SHIPPING-STOREFRONT-CONNECTED-PRACTICAL-TRIAL-237
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — boot and complete the existing connected buyer flow
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: BOOT_FIRST_FIX_EXISTING_WIRE_AND_DELIVER_CONNECTED_URL
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0. Required outcome / execution order
237 did NOT deliver a URL and its wire requests cannot pass current server validation. Finish SAME capability,
not a new audit/engine. Read237/ARCHITECT-REVIEW.md; reviewed source affef402945dbb80772b33137a14e465d07f149a.
Resolve full issuance SHA containing THIS TASK, fetch safely and persist received artifact verbatim.
HARD40: boot/setup10 / focused connection15 / actual buyer trial10 / recovery5. One correction, no silent
extension; report exact stage if blocked. DO NOT consume the whole box in unit tests without starting the app.
FIRST start task-owned backend/frontend and record both health/page responses; then fix using actual traffic.
Acceptance minimum: normal buyer sees configured real group, chooses method/date, successful real PUT selection,
navigates existing payment preview with real price+selection revision. Then final-click request reaches the
existing server Order/hold/payment boundary with exact token. Leave actual review URL/runtime, not only source.
Both editions/all owners/coupon practical completion remain final requirements; a usable SingleStore URL first
is an INTERMEDIATE handoff, not final waiver of Marketplace. No sellable/PSP/Visual ACCEPT fabrication.

## 1. Architecture + preservation (mandatory)
Read AGENTS/current recovery, .cursor/skills/tooba-frontend-feature/SKILL.md,
.cursor/skills/tooba-backend-feature/SKILL.md, FRONTEND-ARCHITECTURE-STANDARD.md and locked Shipping §29/UX/
CRITICAL-STOREFRONT-VISUAL-REVIEW. Reuse exact current page/cards/adapters, existing design-system/CSS/localization.
Allowed owning paths:
- app/storefront/storefront-shipping.tsx, shipping-api, captured-selection, group-cards and existing refresh model;
- app/payment/storefront-payment-handoff.tsx and app/storefront/storefront-final-click-api.ts / checkout decoder;
- narrow existing Order transport/validator changes ONLY if an actual wire discrepancy needs them.
No Host business logic, direct DB client, new screen/state framework/client, discount/calendar/owner algorithm,
foreign Domain/Application/DbContext/schema references or new permission. Keep Endpoints -> ISender -> handlers/
Result and typed error resources. Preserve server authority/CAS/rollback/idempotency; no guard weakening.
Reuse existing isolated D:\temp\tb237 branch/task worktree or safe successor. NEVER edit original watched source
D:\Users\User\source\repos\SarvNewVer/FE3000 or read/enumerate/reload/close USER review browser/window.
Local normal migration/dev-data/runtime approval was ALREADY given. No repeat permission for routine setup.
Use FREE verified task ports FE3100/Host5188 (or actual free alternatives), fresh task-owned browser profile only.
No kill-by-name, original5088/3000/other apps untouched. Checked-in flags stay OFF; local coherent gates ON only.
Target main; normal non-force HEAD:main from isolated branch, fetch isolatedHEAD==origin/main, original watched
checkout disk intentionally old. No amend/reset/stash/rebase/force, no mass add/unrelated CRLF changes.

## 2. Boot FIRST, use actual configured DEV authorities
Inspect actual local DB/server/user/tenant/edition and MigrationRunner plan. Apply expected231/232/236 pending
Order migrations ONLY through normal runner on VERIFIED NONPRODUCTION tooba_alpha/store-alpha (authorization
already granted); inspect unexpected pending set, no blind adoption/reset/drop/history edit. No new migration.
Build current Host ONCE into isolated output; start with Enabled+SelectionEnabled+CheckoutEnabled and valid
AvailableDateCount local env. Start worktree FE at task port with existing proxy/BFF pointing at task Host.
Identify PIDs/paths/ports and /health200 + app response. Keep env/secrets outside git.
Use existing official normal APIs/seed workflows for DEV buyer/login/CSRF/cart/address and valid stock/offers/
Platform type assignment + published Manual method. Existing DEV230 configuration can be reused if lawful;
never erase original user's weight conditions or invent measurements/prices/auth roles. If needed create new
named DEV records, authorized. No mock/intercepted browser responses/direct SQL business seed.
Normal buyer registration/login is permitted; no new auth bypass/developer header injection. Don't build another
browser driver for an hour; reuse supported browser/task-owned controls. Need concrete error if boot blocked.

## 3. Exact wire fixes — implement while running, no backend relaxation
A READ confirmation* -> WRITE expected* mapping is currently WRONG. Actual backend group body is:
groupKey, serviceCode, optionCode, selectedDeliveryDate, expectedSnapshotId, expectedSourceRevision,
expectedSelectionVersion, expectedAreaGroupId, expectedRateRowId, expectedGoodsAmount.
Map actual read Confirmation* values to these WRITE keys. Do not send confirmation* as if server bound it.
Keep amounts/revisions exact strings; no Number/String coercion from malformed values. Verify actual request JSON.
B Captured request top-level shippingMethodCode/selectedDeliveryDate/selectedDeliveryTimeWindow MUST be empty/
absent as allowed by existing DTO, shippingGroups null. Current page sends capturedPrimary service as legacy
method => server intent_invalid. Clear legacy shorthand for captured ONLY, keep legacy path unchanged.
Server still derives all membership/owner/money; no client cartLineIds/label authority.
C Require EVERY server group, not only groups with methods. Unavailable group stays visible and blocks Continue
with its reason. Validate all five confirmation tokens + exact goods + allowed selected date in current method.
Do not skip unavailable group or send partial cart. Zero goods is lawful, absent isn't zero.
D Context currently snapshotId:null/root-only and raw old projection. Use confirmed current response identity
including each group's binding/snapshot/revisions/area/row/goods/calendar plus cart/destination/coupon generation.
Pending/failed blocks Continue and stale choices. Coupon change/explicit clear triggers same existing coordinator
refresh (not just local reset); explicit null never falls back to stored code. Handle empty-key retry/new key
during old in-flight in that existing coordinator, no second fetch framework. Keep typed address untouched.
Root CAS comes from actual saved draft/accepted payment receipt; preview DTO has NO selectionRevision property.
Only a server-proven absent draft may use "0"; malformed/missing expected stored token is not absence.
E Synchronous in-flight ref BEFORE first await for save and final click; release in finally. React busy alone is
not duplicate protection. Preserve stable idempotency through unknown outcome/reconcile, never automatic retry.
F Continue saves and navigates only, NO early commit. Capture save result/revision for proper handoff; payment
loads real accepted payment-draft. Actual payment owner files:
app/payment/storefront-payment-handoff.tsx -> app/storefront/storefront-final-click-api.ts.
Carry page.selectionRevision exact TEXT into StorefrontFinalClickInput and JSON expectedSelectionRevision with
same coupon and page.cartVersion. Missing/malformed captured revision refuses locally, not fabricated zero.
Existing loadCartPaymentDraft/startFinalClickPayment use coupon?.trim() || stored: fix explicit-clear semantics
(undefined may mean use current stored intent, explicit null/blank means cleared); use same confirmed context.
No new payment endpoint/page. Final stages go through actual existing StartPayment/hold/provider boundary.
G Read-only notice is currently unconditional despite active selection controls. Make capability presentation
honest using actual server coherent gate hint if needed (small additive field), or appropriate existing response;
don't say checkout is active just because preview.Enabled. Never authorize by JS. Local configured path must
actually allow legitimate selection. Keep current layout/group independence and configured locales/RTL.
H Fix directly affected old relational fixture's missing ExpectedGoodsAmount using actual returned group facts,
not fake0/default. Do not replay the whole suite. Move malformed goods cheap shape check before quote if needed
to preserve the stated preflight behavior; no new generic validator framework.

## 4. Actual trial is the proof; do it BEFORE broad/unit replay
Task-owned normal buyer, real traffic/no /v1 mocks:
1 Real destination -> own method/date -> Continue. Capture PUT body with EXPECTED keys, status200, new exact root
revision and unchanged Active cart; no Order/reservation before final click.
2 Existing /payment?cartId=... preview200, exact current revision, per-group own fee/date and authoritative total.
3 Final click sends expectedSelectionRevision TEXT to existing start-payment endpoint. Record actual outcome and
Order id if committed, one reservation/conversion under actual services; normal existing sandbox/configured
provider only. No real-money call, fake successful hold, TB185 flip or invented inventory. If provider/stock/
Settlement config blocks, record EXACT code/stage and show usable selection/payment-preview URL anyway.
4 Valid and invalid coupon (existing authoritative coupon/Applied guard), explicit clear/change/reconfirmation,
double-click save, real failed-refresh retry; preserve typed address.
5 Desktop1440 + mobile390/360 actual screenshots inspect no overflow, useful visible cards/controls/payment
preview. No unit/static screenshot claim as Visual ACCEPT. User acceptance separate.
6 Existing authorized Order GET after changing DEV publication retains old Order money/date/group facts, if final
Order was created. No historical repricing/new history API.
Once SingleStore path is available, spend remaining time on actual Marketplace setup/mixed owner trial using
same code and official task-owned DB/bootstrap/config. Do not counterfeit a Marketplace screenshot from a
SingleStore actor, or block first usable URL waiting for that setup. Precisely list unfinished owner trial;
required final all-owner delivery remains OPEN, not silently excluded.

## 5. Validation / handoff / transport
Small focused wire serialization/decoder tests MUST use actual backend-shaped expected* payload and unavailable
group/token negatives; no tests mirroring the same incorrect frontend-only type. Narrow backend owning guard
tests for missing goods/partial coupon/provider failure only if changed. One frontend typecheck and required
npm run test:critical-storefront once because shared cards changed; read actual visual checklist.
No173/412/full Order/Shipping/solution/guard replay or extra audit. Healthy app + real positive PUT/payment preview
cannot be substituted by 6 parser tests. If cutoff reached, leave working runtime and report exact known failure.
Task+RESULT+real receipt/screens, append237 RESULT correction, ALL3 recovery TOPs with actual useful URL and source/
runtime/pending-migration truth. Exact path normal commits/push/fetch parity, original disk untouched.
Never POST literal "probe"/test/placeholder to Bridge Results. Check local complete result text first; one canonical
final result only through existing lifecycle. Do not run old poster/probe scripts or diagnose transport by dummy
POSTs. Root does not dispatch/enqueue/wake through Bridge. Worker PASS != Architect ACCEPT.
Leave task runtime usable and optionally dedicated visible task review window; once handed off, don't automate/
reload it. Credentials privately/local only, no password/cookie/token in result/git/screenshot.
Known nonblocking gaps remain honest; TB185paused/defaultOFF/PendingAllocation, originalpreviewONcount7 untouched.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE; no238 envelope.

## RESULT CONTRACT — fixed canonical envelope
Return one final real Result; fill ACTUAL_* values with verified facts/NONE, never transmit template/probe text.
Status exactly PASS|PARTIAL|BLOCKED|RECOVERY_CONFLICT. Full issue/source SHAs, no editorial text inside SHA fields.
Wire lines (without Markdown fences):
```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1
Parent-Task: TB-SHIPPING-STOREFRONT-CONNECTED-PRACTICAL-TRIAL-237
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — boot and complete the existing connected buyer flow
Mode: BOOT_FIRST_FIX_EXISTING_WIRE_AND_DELIVER_CONNECTED_URL
Bridge-Task-Row: ACTUAL_ROW_ID
Status: ACTUAL_ALLOWED_STATUS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: ACTUAL_FULL_ISSUE_SHA
Base-SHA: ACTUAL_FULL_SOURCE_START_SHA
Code-SHA: ACTUAL_FULL_CODE_SHA_OR_NONE
Evidence-SHA: ACTUAL_FULL_EVIDENCE_SHA_OR_NONE
automaticNextImplementationTask: NONE
ACTUAL_RESULT_BODY
END_TOOBA_WORKER_RESULT
```
Append the task-closing marker shown on this artifact's last line; nothing follows.
Body MUST lead with usable URL + exact completed buyer stage (or exact boot failure), then real process/DB/edition/
tenant/migration/data ledger, actual wire names/requests/statuses/save revision/payment-stage proof and screenshots.
State scope/owner/coupon positives vs gaps, exact tests run, actual elapsed/corrections, originalwindow preservation,
source/worktree/main/runtime gates and git parity. No unit-to-live/PSP/fullrelease/Visual ACCEPT inflation.
END_TOOBA_TASK
