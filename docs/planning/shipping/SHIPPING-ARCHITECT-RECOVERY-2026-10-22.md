# CURRENT — 228 PARTIAL: assignment authority stored; storefront projection NOT wired — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228/{RESULT.md,TASK.md}.
Base `d292e88b6`. EXECUTED — PARTIAL, not accepted; no live trial, no migration applied, no Host restart.

Two-family policy stays exactly as decided (docs/architecture/shipping/STOREFRONT-RESPONSIBILITY-DECISION-2026-10-27.md):
Seller delivery or Platform(Store/Marketplace) services, per-group fee/dates/interval, existing storefront UI.
A delivered: nullable explicit `FulfillmentResponsibility` + real `FulfillmentShippingTypeId` on `SellerOffer`
with one guarded `AssignFulfillment` (no inference, no archived, no type-without-responsibility, no empty id);
additive nullable migration `20261027120000_AddOfferFulfillmentAssignment` written and NOT applied; ONE-query
batch Contracts read `IOfferFulfillmentAssignmentLookup`; single admin write route
`PUT /v1/admin/offers/{offerId}/fulfillment` (existing panel gate + existing `fulfillment.manage`, authorize
before body, presence-aware body, real active Fulfillment type check, no seller route). Codes, FA/EN resources,
catalogue and the Offer manifest updated (Admin un-forbidden with justification; Storefront still forbidden).
NOT delivered and NOT to be claimed: B (cart offer ids → one assignment batch read → partition by responsible
owner+real type with seller boundaries → real catalog bindings incl. service with NULL option → one 219 read per
owner+binding → ONE 218 ComposeAsync) and C (per-group results into the existing group/delivery cards with that
date's own interval, default-OFF read opt-in). 218/219/220 stay unconsumed, so no captured candidate is
selectable and the explicit SaveSelection/Commit refusal must ship together with C.
Proof: Offer.Endpoints + Host build 0 errors; 8/8 new focused facts; Offer suite 92/93 where the only failure is
a PRE-EXISTING missing ProductWorkspace guard file (untouched by this task). Elapsed ≈82 min vs HARD40 =
NONCOMPLIANT, not a process PASS.
Next exact step: apply the migration on store-alpha via the real runner plan, then B+C in one change.
227 accepted for search/populated-branch shake with the open mobile empty-note14px residual; no full visual
acceptance. 225-R2/226 accepted within limits. No admin repair replay.

# SUPERSEDED — Architect issued Storefront228 — 2026-10-27

Active: TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228. User clarified TWO responsibility families:
Seller delivery or Platform(Store/Marketplace) shipping services. Each group independent; existing Storefront
cards/controls/DOM/CSS reused. No new visual design or hardcoded supermarket speed.
Architect assignment decision: explicit nullable responsibility + real type on seller-specific Offer,
admin-scoped write, batch Contracts read; absent legacy assignments unavailable, no seller=fulfillment guess.
See docs/architecture/shipping/STOREFRONT-RESPONSIBILITY-DECISION-2026-10-27.md.
228 is IMPLEMENT first real read projection, not audit/helper-only: trusted assignments ->219 selected capture
->218 composer -> existing storefront group cards. Selection/commit of captured path remains explicitly
blocked pending parity/compact-order evidence. Actual live connection is NOT delivered before worker proof.
Task: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228/TASK.md.
HARD40/source25/final15, one correction, report PARTIAL at limit rather than invent authority.
227 accepted for search/populated-branch shake with open mobile empty-note14px residual; no full visual
acceptance. 225-R2/226 accepted within limits. No admin repair replay.
Shared immutable capture/compact order-group evidence/no historical repricing remain mandatory.
User nonproduction authorization persists; required scoped migration/verified Host refresh allowed, no
reset/reseed/user-draft overwrite. Task-owned browser only;225-R1 lost unsaved user-tab edits, no recovery.
TB185paused/default-OFF/PendingAllocation preserved. Previous checkpoints below are historical.

# CURRENT — Architect reviewed 227 — 2026-10-27

227 ACCEPTED_WITH_RECORDED_LIMIT for in-popup search and reproduced populated-branch loading shake; admin
user retest allowed, not user Visual ACCEPT/full pixel-stability. Root viewed 1440 and390 popup screenshots
and source diff at a32d99ee47d445f6715df3436ca3a5689f6c114e. Remaining: 52px status min-height grows to66px for longest empty-branch note on
mobile; retain this open limit. Service-area selectable options not live-proven. Worker69/69+typecheck are
worker execution, not Architect execution.
Review: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227/ARCHITECT-REVIEW.md.
Active implementation: NONE. Storefront remains NOT connected. Next user-authorized priority is integration
PER GROUP using existing Storefront UI; source review confirms inspected Cart/Offer contracts carry seller
identity but not explicit fulfillment responsibility/required shipping type. Do not equate seller with
fulfillment owner or selected publication with customer eligibility. Clarify explicit product/offer
responsibility policy before inventing that authority. No228 implementation task issued yet.
Compact shared immutable snapshot/compact per-order-group evidence/no historical repricing stays required
and unimplemented. No full authored JSON per order.
225-R2 money and226 week acceptance retained; no replay. 225-R1 lost unsaved LOCAL user-tab edits: no recovery
claimed; fresh task-owned browser profile/target only. Default-OFF/TB185 paused/PendingAllocation and real
draft/pointer/runtime preserved. Historical checkpoints below remain historical.

# CURRENT — 227 executed: in-popup search + pixel-stable destination modal — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227/{RESULT.md,TASK.md}.
Base `2d744fc67df34dc9427b2f0db56e2946c1344784`. Status: EXECUTED, NOT accepted. 225-R2 money and 226 week untouched.
The shake was MEASURED on the unchanged source (dialog top 299→271→257 = 42px, height 303→387→359 = 84px
during the country→province read; page and inner scroll 0): conditional rows inside a UA-centred `<dialog>`.
Now the four fields never change identity, every transient line shares ONE reserved status slot
(min-height 3.25rem), and the popup is an out-of-flow, searchable dropdown portalled INSIDE the dialog —
2230 frames at 1440 keep top 273 / height 355; 390 → 171/503; 360 → 139/503; all with ZERO travel.
Search is inside each popup over ONLY the verified branch's loaded rows with an honest `{shown} / {total}`,
Escape closes only the dropdown, country clear still empties+disables descendants, مرند's service area is
honestly `0 از 0`. Limits: no live service-area options exist in this installation (test-proven only); the
longest no-provinces note needs 66px at ≤390 (one recorded +14px empty-branch shift); dead picker file left.
ONE clean tsc, 69/69 affected tests. No money/week/backend/geography change, no Apply/PUT/seed/migration,
no restart, no user tab touched, default-OFF/TB185 paused/PendingAllocation preserved. ≈15 min vs HARD25.
NO user Visual ACCEPT claimed.
NEXT PRIORITY (Architect issues it): concrete Storefront integration PER SHIPPING GROUP using the EXISTING
storefront cards/controls/DOM/CSS — 219 selected immutable revision + 218 composer + real trusted
fulfillment owner/type/membership + existing projection/selection/commit; never browser owner or price,
never seller=fulfillment inference, unknown physical facts stay null. Storefront is currently NOT wired/live.
Compact order history remains mandatory and unimplemented: shared immutable capture, compact per-order-group
reference and final money/receiving facts, no full authored JSON per order, no historical repricing.
Safety continuity: 225-R1 reloaded a user tab and lost unsaved LOCAL edits; no recovery claimed. Browser
proof uses a fresh task-owned profile and target ONLY, never user tabs.
Task: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227/TASK.md.

## SUPERSEDED — Architect issued 227 — 2026-10-27

Active: TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227. User reports destination selection shake/jump and requests search INSIDE all four dropdown popups. Reproduce/measure cause, reuse existing design-system combobox, preserve DB cascade/current-generation evidence and modal Add semantics. Task is bounded 25 minutes; no money/week/backend reopening.
225-R2 money integrity and 226 mobile week presentation remain accepted within recorded proof limits; no user Visual ACCEPT is implied.
NEXT USER-AUTHORIZED PRIORITY after 227 review: concrete Storefront connection PER SHIPPING GROUP with existing Storefront cards/controls/DOM/CSS. No invented design or generic audit/calculator loop. Selected immutable revision + trusted fulfillment membership/owner/type + existing composer and projection/selection/commit are required. Storefront is currently NOT wired/live.
Preserve compact shared-snapshot order evidence decision; do not copy authored JSON per order or reprice historical orders.
Safety continuity: 225-R1 reloaded a user tab and lost unsaved LOCAL edits; no recovery claimed. Future browser proof uses fresh task-owned profile and target ONLY, never user tabs. Preserve real draft, pointer, runtime, default-OFF/TB185 paused/PendingAllocation.
Task: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227/TASK.md.
Prior checkpoints below are historical.

## CURRENT —226 mobile week reviewed; admin form READY FOR USER RETEST —2026-10-26

Architect reviewed source9734e77ac81eacb8d158988bd2f3ca2f65f4cde4 and VIEWED actual native225? NO:
226/screens/226-390-view.png,226-390-edit.png,226-360-view.png,226-1440-view.png.
Mobile closed rows now one line/one Closed status; working view range compact adjacent labels+values;
edit paired controls and checkbox visible; desktop four-column view retained. Accept bounded226 presentation
repair for ADMIN USER RETEST, not user Visual ACCEPT/global UI certification. Root read exact diff: only week
presentation and225R2 test helper type annotation. Root didn't execute Windows typecheck/browser; worker
one clean typecheck and mounted toggle-restoration receipts are evidence. R2's prior typecheck claim predates
its test helper and was incomplete;226 corrected TS2683 with no behavior change.
225-R2 money integrity ACCEPTED; no further money/proof loop. 225 modals/cascade/rate duplicate-edit validation
accepted within documented focused/helper+mounted limits, not full save or live service-area matrix.
CALL USER NOW: http://127.0.0.1:3000/admin/shipping-services ; draft01a16eeb-70b3-7000-87c2-564c82835496
view/edit via actual row. Ask actual user feedback, no next implementation task without it.
Active NONE / Automatic-Next-Task NONE. Preserve user's tabs and edits; prior R1 local tab loss remains
unrecovered, no claim otherwise. New browser only task-owned profile/created target.
Limits: real Tehran has no service areas so fourth-level Add test-only; no new Apply/PUT/save replay in225R2/
226, live stored-fraction/legacy conflicts/multi-area360 not proved. No Storefront integration/activation,
compact per-order-group evidence still unimplemented, seller-admin separate. No blanket completion claim.
226 actual10min inside15; older overruns remain recorded. Applied schema/catalog/capture/defaultOFF preserved.

## CURRENT —225-R2 money ACCEPTED;226 mobile-week-only repair issued —2026-10-26

Architect read afbdadc7beb363393eba8aa6c988beacf827db24 actual money model/field and R2 mount-receipts.
Repeated invalid-token laundering closed, Chrome actual input/caret/0/5million/nonemptyMessageBox/cancel
receipts support bounded acceptance. Root didn't rerun tests/browser. No more money/proof-only loop.
Root VIEWED225 desktop destination modal,390rate modal and390weekview. WEEK STILL VISUALLY INCOMPLETE:
Closed shown twice (status+extra span), working From/To detached/spread. Screenshot large canvas with narrow
content, not actual390PNG. User's mobile complaint not fully closed. No user Visual ACCEPT.
Active TB-SHIPPING-ADMIN-MOBILE-WEEK-VISUAL-226, presentation-only authoring-week-editor, HARD15/source8.
Read TASK and225-R2/ARCHITECT-REVIEW.md. Preserve all225 money/modals/cascade logic; no broad tests.
Await fixed week screenshot then Architect/user trial handoff. No Storefront consumer/compact order evidence
implementation or seller-admin work. R1 unsaved-user-tab loss remains unrecovered; only new task Chrome target.
Automatic-Next-Task NONE; STOP_FOR_ARCHITECT after226.

## CURRENT —226 executed: mobile week is one Closed badge + one coherent hours line, native PNGs —2026-10-26

Read docs/architecture/evidence/TB-SHIPPING-ADMIN-MOBILE-WEEK-VISUAL-226/{RESULT.md,TASK.md}. Base
afbdadc7b, fast-forwarded to 94ea7ed2e (docs-only). 225-R2 money integrity stays ACCEPTED and untouched.
Task status: executed, NOT accepted; user trial still waits for Architect.

Presentation only, in ui/authoring-week-editor.tsx: a CLOSED day no longer emits the status word AND a
second span across the bound columns — narrow VIEW is now `weekday` + ONE Closed badge, one line, no dash,
no bounds (28px rows, measured). A working narrow VIEW day is `weekday` + ONE working status, then ONE
inline range whose labels sit immediately next to their own values (RTL measured `از`/`09:00` adjacent,
`تا`/`17:00` adjacent; EN `From 09:00 – To 17:00`), 47px. Narrow EDIT keeps the stored controls: weekday +
working checkbox on line one, the two equal accessible time inputs on line two (138px at 390, 123px at
360), and a closed day is a single compact line with its checkbox and no extra Closed label;
onDayWorking/onDayTime/model guards and every existing test id are unchanged. Desktop VIEW keeps the
accepted four-column geometry (header and rows both day x=481 w=607, status x=337 w=136, from x=209 w=120,
to x=81 w=120) and now shows `تعطیل` only in the status column with an honest `—` in the bound columns,
which is the per-column truth 210-R1 described. Narrow padding tightened only (`py-1`, `gap-y-0.5`).
No time model/validation, dirty/cancel, area/rate/destination modal, money policy or global CSS touched.

Proof: one clean `tsc --noEmit`; no 188/76/42/full-frontend/backend/solution sweep and no invented CSS tests.
Mounted in a NEW task-owned Chrome (155.0.8059.85, `--headless=new`, port 9334, fresh profile, every page in
a target created by this task), real FE3000 traffic, no writes/interception/restart, real draft week read
as-is (only Monday 09:00–17:00 working; nothing invented). Native PNGs at 390×844 and 360×780 (view +
edit, RTL/fa) plus EN/LTR 390 via the real `/en` prefix and one 1440 desktop shot; dimensions read from the
PNG headers and each file inspected by eye. `scrollWidth == innerWidth`, `overflowX false` at 360/390.
Toggle round-trip on the real working day: closed → 0 time inputs/1 line, reopened → `09:00`/`17:00`
restored, Apply still disabled, no Apply ever pressed.
HONEST: `?locale=en` does not switch this admin locale (prefix/cookie does) — the first EN capture was wrong
and re-done; the `/en` visit wrote `tooba_locale` and briefly overwrote the 390 FA PNGs, which were cleared
and re-captured (shipped FA bytes are the re-captured RTL ones). A pre-existing typecheck break in the
ACCEPTED 225-R2 test helper (`this` in a default parameter, TS2683 — that task's tsc ran before the file
existed) was fixed with a bare annotation, no behaviour change.

Actual elapsed ≈10 min vs HARD15 — box respected in wall-clock terms, still NOT a process PASS.
Not claimed: user Visual ACCEPT, the form as a whole, Storefront wiring, seller-admin entry, Tehran
service-area gap. Active task: NONE. Next: Architect review only. STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. Compact-order-evidence invariant still unimplemented; TB185 paused, default-OFF,
PendingAllocation preserved.

## SUPERSEDED —225-R2 executed: money provenance is field state; proof in an isolated task Chrome —2026-10-26

Read docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R2/{RESULT.md,TASK.md} and the
correction appended to 225-R1/RESULT.md. Base 1d0625218 (clean; the three backend line-ending drifts and no
other file staged). Task status: executed, NOT accepted. 225/R1/R2 are NOT accepted and NO user trial was
called; Architect reviews first.

A — the R1 defect was real: trust was re-inferred from the text, so `12,34` (invalid) + `5` gave the
structurally well-grouped `12,345` and the NEXT key took the trusted branch and accepted `123456`. Now
`MoneyInputProvenance { raw, untrusted }` is real edit state: `interpretMoneyTyping(previous,nextText,caret)`
takes it, every entry point propagates `MoneyEdit.untrusted` (paste/edit/backspace/delete), and
`authoring-money-field.tsx` owns ONE ref keyed by the committed token through a single `commit(edit)` used by
change/Backspace/Delete, dropped the moment the value prop no longer matches (external reseed, other row,
reopened modal). Operator text therefore stays invalid through ANY number of appended keys until the
operator removes/replaces the offending grouping; blur/re-render/other-field change cannot launder it; the
model still receives the raw text and the parent still blocks it. No second sanitizer; rate shape,
self-exclusion, legacy fraction and MessageBox semantics untouched.

RED measured on the unchanged R1 module (probe executed): `12,34`→`12,345`→`123456` ACCEPTED at the second
appended digit. GREEN 8/8 new R2 facts driving the same state transitions (invalid through 5/6/7/8 + blur;
explicit correction; reseed/other-row; middle insert `12,934` caret 4; `5,000,000`; `0`; malformed/negative/
exponent; comma-adjacent Backspace/Delete) + 42/42 across the three 225 classes + 21/21 in 224-R1 money and
209-R2 + ONE clean tsc.

Mounted gate in a FRESH task-owned Chrome (installed Chrome 155, `--headless=new`, dedicated port 9333,
fresh profile, target created by this task: 75081F7DB2547954825D0574BD0D1869; only that profile's processes
stopped, port silent): real keys throughout — `1,234`→insert 9→`12,934` caret 4, `0`, `5,000,000`; pasted
`12,34` invalid then +5/+6/+7 and +8 after another field change stayed `12,34567`/`12,345678` with
aria-invalid, Confirm showed a non-empty localized reason with the modal open (screenshot), Cancel restored
`120,000`/`5,000`, delivery `1–3`, Apply disabled. NO Apply/PUT/publication/catalog/migration/restart; draft
and pointer untouched.
HONEST: the pasted token used the element's real paste event plus CDP Input.insertText (clipboard commands are
denied here), so it is the paste CODE PATH, not an OS clipboard round-trip; typing a comma into a valid value
still cannot produce one (pre-existing grouped-field behaviour, recorded not silently changed).

B — USER TAB INCIDENT (recorded, NOT recovered): R1's mounted detour reloaded an existing user editor tab and
discarded that tab's unsaved local edits. The stored draft was untouched but an unsaved local edit cannot be
rebuilt from the server, so "user tabs untouched" is withdrawn for that step and no recovery is claimed. This
task never read, enumerated or manipulated that tab; proof used the isolated browser above.

Actual elapsed ≈45 min vs HARD20 — recorded, NOT a process PASS.
Not claimed: user Visual ACCEPT, Storefront/checkout consumer, seller-admin entry, Tehran service-area live
gap. Active task: NONE. Next: Architect review only. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
Compact shared-snapshot order-evidence decision still locked/unimplemented; TB185 paused, default-OFF,
PendingAllocation preserved.


## SUPERSEDED —225-R1 not accepted;225-R2 money provenance + isolated Chrome proof issued —2026-10-26

Current main1d06252188a3278127401e804f53b263725c47e1/source2107a63db inspected by Architect.
Stored-fraction current-value validation and editable guard corrected. Still defective TWO-key transition:
invalid12,34 ->append5 raw12,345 validfalse ->append6 reclassified trusted ->accepted123456. Validity/
provenance must persist, not infer trusted from shape. Read225-R2/TASK.md and225-R1/ARCHITECT-REVIEW.md.
Active TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R2. No user trial before Architect acceptance.
R1 mounted real keys did NOT reach field; caret/typing only unit-proven. Fresh installed Chrome task-owned
profile/created target only, no Electron/user tab interaction. INCIDENT: R1 mistakenly reloaded user edit
tab and lost unsaved LOCAL changes; DB unchanged is not preservation/recovery. Disclose truthfully.
Keep225 modals/cascade/mobile/rate validation, no more redesign. Tehran no service areas, live fourth level
not proven. Compact-order-evidence/Storefront consumer still unimplemented; no seller-admin work.
No broad tests/Bridge/known historical script. HARD20/implementation10; STOP_FOR_ARCHITECT afterR2.
## SUPERSEDED —225-R1 executed: money edit provenance + caret mapping + modal tolerance; 225 still awaits review —2026-10-26

Read docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1/{RESULT.md,TASK.md} and
225/ARCHITECT-REVIEW.md. Base84999b4f9 (clean fast-forward; the three backend line-ending drifts untouched).
Task status: executed, NOT accepted. 225 is NOT accepted and NO user trial was called; Architect reviews first.

A — invalid operator text can no longer become different money. `interpretMoneyTyping` is now
(previousRaw,nextText,caret): it reads the field's LAST COMMITTED token, and only when that token was LAWFUL
(dropping separators as display-only) — when it was INVALID, a still-present separator keeps the text verbatim
and invalid, so `12,34` + `5` stays `12,345` invalid instead of the accepted `12345`; only the operator's own
deletion/replacement makes it lawful. The field selects the strict paste reader by `inputType`
(paste/pasteAsQuotation/drop/replacementText) AND by an `onPaste` flag for environments that omit it; a real
key clears a stale flag.
B — the caret is mapped through that same separator removal (`withoutDisplayGrouping` returns the logical
offset of every display offset), so a middle insertion into `1,234` (`1,2934`, caret4) anchors 3 digits and the
re-grouped `12,934` keeps the caret after the typed `9`. New symmetric `interpretMoneyDelete` handles Delete
directly before a separator like `interpretMoneyBackspace` does behind it.
C — the rate modal no longer receives an open-time tolerance list. It receives the EXACT stored tokens
(`StoredRateAmounts`/`storedRateAmounts`) and calls the new `rateConfirmIssue(row,otherRows,currency,stored,
editable)` INSIDE the confirm handler: tolerance is re-derived on the current row, self is excluded by stable
row key, the duplicate/overlap scope is the same area, and a view/busy state refuses silently in the handler
(no dialog, no parent mutation). Stored `100.50` confirms untouched; editing it to `200.75` is refused with the
integer-rial reason. Parent Apply stays the only persistence call and the final validator.

Proof: RED measured on the reviewed module (healed `12345`, caret off by one digit, stale tolerance accepting
`200.75`); GREEN 12/12 new focused facts + 76/76 affected feature tests (225, 225-R1, 224+224-R1 money,
209-R1/R2 with the two truthful seam corrections) + ONE clean tsc. Not run: whole188/backend/guards.
Mounted on FE3000/Host5088 (real traffic): the paste path left `12,34` verbatim with aria-invalid and a
deliberate Confirm showed the localized amountInvalid reason; the stored baseline (row120,000/5,000, delivery
1..3, Apply disabled) was unchanged afterwards — no Apply/PUT/publication/catalog/migration/restart.
HONEST LIMITS: automated real keystrokes did not reach the field in this Electron webview (caret/Delete facts
unit-level only); one earlier mount showed an unexplained EMPTY refusal reason that did not reproduce (all ten
codes are mapped) — recorded, not claimed; the navigation that reloaded the tab hit an existing edit tab and
discarded its unsaved LOCAL edits (no server write). No screenshot matrix, no live stored-fraction record
(model fixture labelled). Not claimed: user Visual ACCEPT, Storefront/checkout consumer, seller-admin entry.
Actual elapsed exceeded HARD20 (repair round + mounted detour) — recorded, NOT a process PASS.
Active task: NONE. Next: Architect review only. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. Compact
shared-snapshot order-evidence decision still locked/unimplemented; TB185 paused, default-OFF,
PendingAllocation preserved.
## SUPERSEDED —225 NOT ACCEPTED;225-R1 narrow money/modal integrity repair issued —2026-10-26

Root independently read225 source2e6aed0951a5e1ef75d5206d77c4df9f433f55b1. Preserve modals/cascade/mobile work.
Blockers: typing strips commas from prior invalid pasted text (12,34 then5 ->12345); caret offset from grouped
text wrongly reused after commas stripped; modal stored-fraction tolerance not revoked when value changes.
Active TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1; read TASK and225/ARCHITECT-REVIEW.md.
User must NOT be called to test before Architect accepts repaired result. Worker PASS188 is not acceptance.
No root runtime/test execution. Existing Tehran service areas absent, so fourth-stage live proof pending;
breadcrumb branch reads not constant-count performance proof. No whole-suite confidence loop.
No Storefront/seller-admin/schema changes in repair. Preserve compact-order-evidence decision, user draft/
capture/tabs/runtime. HARD20/implementation12. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

## SUPERSEDED — 225 executed (worker report): destination cascade modal, rate-row modal, IRR integer UI, mobile week — 2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225/{RESULT.md,TASK.md}`.
Base `fb2999cfa880ceafa985c57eede47ff5107ba44a`. Active task NONE; next NOT ISSUED — Architect review comes
before any user trial.

A — the inline picker/level selector is REPLACED by ONE `Dialog` with four fixed labelled cascade fields
(country → administrative area → city → service area). Dependents start disabled, a parent change clears every
dependent stage, superseded completions are dropped by the existing generation-settled decoder, and `Add`
submits ONLY the deepest verified terminal as ONE `(kind,id)`; ancestors are the path, never extra coverage.
Duplicate/overlap is refused only on the explicit Add through the existing MessageBox, with the modal and the
chosen path kept; staging is not an addition and does not dirty the form. Stored chips rebuild their breadcrumb
after reload from the EXISTING Geography reads in bounded set-based queries (one per parent level, no per-chip
N+1, no fabricated segment, country-wide null-admin cities keep their honest two-segment path, a country
selection is never `ایران › ایران`). HONEST GAP (live): this installation's real `تهران` city has NO service
areas, so the fourth stage is honestly empty («برای این شهر ناحیهای ثبت نشده است.») and the service-area Add is
test-proven only. No Geography schema/data change, no seed.
B — rate rows remain inside the area card as compact summaries with Edit/Delete; Add/Edit open an isolated
modal draft and Confirm validates against the OTHER rows of the same area (weight/count/merchandise AND within a
row, half-open `[min,max)`, touching endpoints lawful, exact duplicate tuple refused even with different prices,
self excluded by stable row key — never by index), zero freight/packaging valid and empty not; a failed Confirm
shows the MessageBox with the owning-field reason and keeps the typed values; the parent Apply stays the only
persistence call. Mirror lives in `model/editor-rates.ts`; backend enforcement is untouched.
C — money: `interpretMoneyTyping` vs the strict paste/commit path fixes the screenshot's grouping corruption
(`1,200,000`, never `1,200000`) while `-100`/`1e3`/`12,34` stay exactly as typed with `aria-invalid`; IRR is
integer-rial in the UI (`120000.00` → `120,000`, raw wire token untouched) and a stored NON-zero fraction is
refused only on edit, never rounded or truncated; no Number/parseFloat money math, currency stays server-owned.
D — weekly view/edit is compact at 360/390: one aligned day+status line, working hours paired with From/To
context on one line, a closed day as a single badge with no empty inputs (live edit mode exposes exactly 2 time
inputs for the one open day); both modals fit the viewport and scroll internally (390: 352×566 inside 844;
`scrollWidth == clientWidth` at 390 and at 360). Desktop shared column alignment unchanged.
Proof: 22/22 new focused facts; ALL feature tests enumerated recursively 188/188; ONE clean `tsc --noEmit`;
architecture guards 10/11 files with `FE-SIZE` failing only on the four documented pre-existing drifts (none
touched; edited screen 798 LOC). Live on the real rev2 draft with real traffic: cascade with disabled
dependents, no phantom chip and Apply still disabled after staging, breadcrumb `کشور: ایران`, duplicate rate
condition refused with values kept, `-100` refused with the integer-rial reason, clean Cancel raised NO discard
dialog and the stored baseline (`1 محدوده`, `1..3`, `120,000/5,000`, one destination) was intact. NO
Apply/PUT/publication/catalog/migration/seed; draft revision and pointer untouched.
Screens: `screens/225-1440-destination-modal.png`, `225-390-rate-modal.png`, `225-390-rate-refusal.png`,
`225-390-week-view.png`, `225-390-week-edit.png`.
NOT claimed: user Visual ACCEPT, live service-area/legacy-conflict rendering, any Storefront/checkout consumer
(`Selected` is still selection intent), price/activation/readiness. Elapsed exceeded the HARD45 box across a
chat-loss and correction rounds — recorded, NOT a process PASS. Compact-order-evidence decision stays locked and
unimplemented; TB185 paused, default-OFF, PendingAllocation preserved. STOP_FOR_ARCHITECT.

## SUPERSEDED —225 issued after user rejects picker/rate UX, IRR formatting and mobile —2026-10-26

Active task TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225. Read225/TASK.md. No new user trial until Architect review.
224-R1 integrity acceptance remains bounded, NOT UI Visual ACCEPT; latest user screenshots show grouping unusable for ordinary editing (5,000000), duplicate row checks absent/poor feedback, and mobile week whitespace. Reproduce/repair, don't claim previous166 tests prove these.
User requires destination Add in persistent modal with FOUR fixed DB-backed cascade fields country/area/city/service; parents disabled until selected, changing parent clears descendants+stale requests. One deepest-terminal selection/chip with full actual breadcrumb+remove, ancestors not extra coverage. Add multiple without modal closing. No terminal-level dropdown. Actual overlap prohibition remains.
Rate Add/Edit in modal, compact main summaries; validate against other rows in SAME area, exclude only actual self on edit;0 valid, blank invalid. Use existing MessageBox on explicit failed action, accessible field indication, not red text wall/keypress popups.
IRR is integer display/input per user; existing integral .00 displayed without fractional suffix but exact untouched wire retained. No rounding nonzero fractions or globally forcing other currencies. Repair generated grouping vs pasted invalid handling; preserve negative/exponent refusal and caret.
Mobile view/edit week needs compact coherent day/status/hours, not merely no horizontal overflow. Check actual modal/weekly screenshots360/390/1440.
No owner/seller redesign or Storefront feature in225; compact shared-snapshot order-evidence decision still mandatory/unimplemented. Preserve user draft/capture/tabs and applied migrations/catalog. HARD45/stop30, narrow tests only, no166/whole-suite replay/Bridge. STOP_FOR_ARCHITECT after225.

## CURRENT —224-R1 integrity repairs accepted for bounded ADMIN USER TRIAL —2026-10-26

Architect independently inspected e79bd74c53a0f6b514785b480739e60709eeb43c: editor-area-range, authoring-intent, money-text, screen controlled state/reseed/commit/hasNoIssues wiring, MoneyField and rate validator. The two acceptance blockers from224 are closed for user trial: invalid/partial shared range now joins localDirty/issues and blocks Apply; unsupported money input remains invalid instead of becoming a different accepted amount. Separator Backspace and decimal-side caret repaired as worker-reported mounted proof.
Evidence: worker11 new focused facts, recursive166/166, clean typecheck; worker actual isolated mounted invalid-range+valid-edit blocks Apply, row-less range block, invalid money/decimal typing and discard/reentry. Root did NOT rerun tests or execute local browser/Apply. No blanket save/error/legacy-conflict/browser matrix certification. Legacy unequal durations remain test-only; shared duration is still repeated per row, no group-level schema change. No user Visual ACCEPT.
USER MAY NOW TEST admin panel: http://127.0.0.1:3000/admin/shipping-services and actual draft .../authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=edit. Read current before later writes; worker preserved rev2/pointer and user's tabs, no PUT. Host actual path per worker D:\temp\tb221-build\out\Tooba.Host.exe on5088, FE3000; historical PID26600/28288 must be reverified.
Current active task NONE, next NOT ISSUED. Await user feedback, don't reopen proof-only loops. UI acceptance only, NOT Storefront delivery: actual consumer/projection/selection/commit and compact per-order-group provenance/final money still unimplemented. Keep locked compact order evidence decision. Seller-admin entry from seller list is separately pending. Four unrelated FE-size drifts remain; elapsed50min vs25 was NONCOMPLIANT, not processPASS.
No Bridge/historical scripts; preserve shared migrations/catalog/captures, TB185paused/defaultOFF/PendingAllocation.

## CURRENT — 224-R1 executed: typed range is form state, invalid money stays invalid — 2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224-R1/{RESULT.md,TASK.md}` and the
append-only correction appended to `.../TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224/RESULT.md`.
Base `63b0a3c3c3b588769e37b31d0e9200b8a3dbcba6` (clean fast-forward; pre-existing backend line-ending drifts and a
stray 0-byte root file were never staged — the stray file was deleted).

224's two accepted-as-claims defects are repaired in the frontend authoring feature only:
(A) `ui/use-area-range-edits.ts` is deleted and the TYPED area range became controlled form state
(`AreaRangeDrafts` + `areaRangeEdit`/`areaRangeDraftIssues`/`areaRangeDraftsDirty` in `model/editor-area-range.ts`)
that `buildAuthoringIntent(..., areaRanges)` folds into the blocking issues, so blank/partial/invalid text can
never submit stale row values, a row-less staged range is refused with an area-scoped message, and one
`applyLocalDrafts` helper reseeds form+groups+rates+area ranges+week from the same baseline on load and confirmed
cancel. The DTO/capture still repeats days per row — a group-level persisted duration stays a schema change.
(B) `model/money-text.ts` stopped rewriting unsupported input AND stopped swallowing a Backspace at a grouping
separator (found mounted by this task: the deleted comma was re-grouped back with the same caret, a permanent
no-op; `interpretMoneyBackspace` now removes the digit with the separator, so `120,000` → `12,000` and
`1,000` → `000` visibly invalid): only Persian/Arabic-Indic digits, the Arabic decimal
separator, correct 3-digit grouping and edge whitespace are normalized; anything else keeps its raw text,
`valid:false`, `aria-invalid` and a disabled Apply. `MoneyCaret{whole,fraction,inFraction}` gives decimal-side-aware
caret anchoring, so `12`→`.`→`5` really is `12.5`.

Proof: RED 8/8 defect facts on the reviewed baseline; GREEN 11/11 new R1 facts + 166/166 feature tests, one clean
`tsc`, guards 7/7 except the four documented pre-existing `FE-SIZE` drifts (untouched, edited screen 798 LOC ≤ 800).
Mounted on the untouched local stack (FE3000 PID28288, Host5088 `D:\temp\tb221-build\out`, health 200, no restart,
isolated tab — the user's tabs were not reloaded) with real traffic: partial max blocks Apply with an area issue,
row-less `4..9` blocks Apply then moves into the first row, `12`→`12.` refused→`12.5` caret 4, `-100`/`1e3`/`12,34`
stay invalid, reject-cancel keeps drafts and confirm-discard restores `1..3` + `120,000.00`/`5,000.00`, and a
read-only document GET still reports revision 2 / `updatedAt 2026-10-24T19:05:52.452036Z` with the selected pointer
unchanged. No Apply/PUT/publication/catalog/migration/seed. 1440×900 + 390×844 screenshots stored.
NOT claimed: Architect acceptance, user Visual ACCEPT, live legacy-conflict rendering, any Storefront consumer.
Active task: NONE. Next: Architect review; do NOT hand the user a trial before acceptance.
The shared-snapshot / compact per-order-group decision stays locked and NOT implemented; STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. Elapsed ≈50 min vs HARD25 — recorded, NOT a process PASS.

## SUPERSEDED — 224 executed: one delivery range per area, Add-time duplicate, level-aware picker, exact money — 2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224/{RESULT.md,TASK.md}`.
Base `920b51a7f2dc193afb47366daf9a531415d21373` (clean fast-forward; only the four pre-existing
backend line-ending drifts and the new evidence folder were untracked/unrelated and never staged).

A — the customer-receiving duration is now ONE range of the WHOLE delivery area, placed above the rows.
`model/editor-area-range.ts` derives `empty|uniform|conflict` from the rows, writes a chosen pair to exactly
that area's rows (same identity when nothing changes), and gives a new row only its own area's range
(`withAddedRowInheriting`). Unequal stored rows are a LEGACY CONFLICT: values are preserved, the control
starts empty and only a deliberate lawful entry unifies them. HONEST BOUNDARY: the existing DTO/snapshot
still repeats the days per row, so the shared range is serialized as the same pair on every row of that
area; a row-less area persists nothing. A true group-level persisted duration is a schema change, NOT done.
`ui/use-area-range-edits.ts` keeps the staged pair (first-row inheritance) and forgets it on a new baseline.

B — duplicate/overlap is checked ONLY on a submitted Add, never on render/onChange. Staging an already
stored country (the screenshot's Iran) shows no error and keeps the branch explorable; the refusal appears
inline on Add and is cleared by any level/path change, and the staged path plus the stored chips survive.
`destinationAddAvailable` (verified evidence + ancestry, deliberately ignoring "already stored") and
`isDuplicateSelection` are the executed helpers; authoritative overlap rules are untouched.

C — the four level buttons became ONE localized level select, and only the controls that terminal needs are
rendered (country / +province / +city / +service area). Country-wide null-admin cities stay lawful; parents
are never fabricated or substituted.

D — exact money display: `model/money-text.ts` groups for display and hands the RAW canonical token to the
model (Persian/Arabic-Indic digits, grouped paste, Arabic decimal separator, digit-anchored caret). Applied to
freight, packaging and both merchandise bounds; read summaries use the same string grouping. No Number
round-trip, no forced `.00`, no scale guess, currency stays server-owned; grouping never dirties or saves.

Verified: one clean `tsc`; 155/155 feature tests (11 new) ; guards 7/7 except the four documented pre-existing
`frontend-source-size` drifts (untouched; the edited screen was kept at 798 LOC ≤ 800). Live FE3000/Host5088
reused with no restart and real traffic: grouped `120,000.00`/`5,000.00`, one `1..3` area range with zero
per-row duration inputs, stored-country staging without error → Add shows the localized duplicate/overlap
message and keeps the path, City level renders country+province+city with the error cleared, `4..9` applied to
the area, `1850000.00` → `1,850,000.00`, Persian `۵٬۰۰۰` → `5,000`, and cancel→discard→re-enter restored the
exact baseline (1 row, `1..3`, `120,000.00`) with no residue. 1440×900 and 390×844 screenshots stored
(390 `scrollWidth == clientWidth == 390`). NO Apply/PUT/publication/catalog write, no migration/seed/reset;
the user draft `01a16eeb-70b3-7000-87c2-564c82835496`, its revision, publication pointer and open tab are
preserved. NOT claimed: user Visual ACCEPT, live legacy-conflict rendering (test-proven only), and any
Storefront consumer/projection/selection/commit. Elapsed ≈55 min vs HARD40 — recorded, NOT a process PASS.
Active task: NONE. Next: Architect review; then the seller-admin entry (planned separately) and the still
unwired Storefront path under `SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md`
(one shared immutable snapshot per owner+draft+revision; compact per-order-group facts; no full authored JSON
per order; no historical repricing). STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

# Tooba — Agent Rules

## CURRENT — 223 executed: compact publication restored + separate owner surface; await review — 2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-PUBLICATION-OWNER-PAGES-223/RESULT.md` and `TASK.md`.
Base `d62bab9a4f31dc1966aed8c06b4f7ea08a366df3` (the untracked 223 evidence folder collided with that commit's
tracked TASK.md: moved aside, fast-forwarded, artifact restored; no reset/stash/rebase/force-push).

A — publication is BACK, compact. Model + transport recovered BYTE-EXACT from 221 (exact
`expectedRevision`+`expectedSelectionVersion`, strict decode, synchronous in-flight gate, stale/unknown latches
cleared only by an explicit successful read, no editor reseed, no auto-retry). New panel is deliberately small:
one status line, at most one short reason, one honest note, Publish/Withdraw + confirmation. Snapshot id,
selection counter and format version are NOT in the normal UI. Live trial on the real draft
`01a16eeb-70b3-7000-87c2-564c82835496` in view mode: `Selected`, matches current revision ⇒ Publish DISABLED with
«همین بازبینی از قبل منتشر شده است.» (the same capture is not republished), Withdraw available. NO write sent;
draft revision and pointer were not touched by this task.

B — the TB222 single-option owner combo is GONE (live DOM `combo:false`, `ownerNote:false`). A separate route
`/admin/shipping-services/marketplace` renders the SAME list feature (`surface="marketplace"`) with its own saved
views/grid namespace; the store surface is `/admin/shipping-services` and links to it. The route is a UI boundary
only: the authorized owner (`/v1/admin/shipping-services/authoring-owner`) is read FIRST and no drafts are read
when that owner is not the surface's owner. SingleStore live: the marketplace route renders the honest
«مارکت‌پلیس در این نصب در دسترس نیست» state, no list, no fabricated owner; the reverse mismatch has its own
honest state. No owner id from query/body, no edition change, no seller/tenant inference.

C — the compact order invariant stays as recorded in
`docs/architecture/shipping/SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md` (one shared
immutable snapshot per owner+draft+revision; only compact per-order-group reference/final money/receiving facts;
no full authored JSON per order; no historical repricing). NOT implemented by this task.

Proof: one clean `tsc`; 144/144 feature tests (restored publication facts + 3 new owner-surface facts); 7/7
architecture guards; live FE3000/Host5088 with no restarts; 1440 + 390 screenshots, `overflowX 0`; closed-day
inputs still disabled; type options عادی/سنگین/فوق‌سنگین intact. Not replayed: 127/194/backend suites; the four
pre-existing source-size drifts untouched.
NOT claimed: Storefront/checkout consumer, projection/selection/commit, multi-owner live proof (impossible on
SingleStore — needs a server authorized-owner SET route), whole-method creation, any price/activation claim.
Actual ≈40 min vs the HARD30 box — recorded, NOT a process PASS.
Trial URLs: `http://127.0.0.1:3000/admin/shipping-services`,
`/admin/shipping-services/marketplace`,
`/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view`.
Active task: NONE. Next: Architect review, then hand the user the trial. STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. TB185 paused, default-OFF, PendingAllocation preserved.

## SUPERSEDED — TB223 issued: compact publication restore + separate owner pages — 2026-10-26

User explicitly requests publication restored, separate Marketplace page reusing SAME form instead of owner combo, and compact order evidence recorded before Storefront connection. TB222 source063aa05fd reviewed narrowly: closed-day UI+handler guard correct; heavy/extra_heavy real catalog writes worker-reported; no-op single-owner combo is NOT multi-owner delivery. Publication UI removal is now superseded by restore instruction; backend/pointer preserved.
Active task TB-SHIPPING-ADMIN-PUBLICATION-OWNER-PAGES-223. Read its TASK.md and docs/architecture/shipping/SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md.
Order MUST reuse one shared immutable snapshot, compact per-order-group reference/final monetary/receiving facts; NO full authored JSON per order, NO historical repricing. This is a required future integration invariant, NOT implemented yet.
TB223 restores UI first, real authorized separate owner entry, no copied form/no fake edition/owner switching. Live SingleStore cannot prove Marketplace multi-owner behavior. Publication still intent, NOT Storefront activation. After223 review move toward actual existing projection/selection/commit, not another calculator/audit.
Preserve user data/unsaved tabs, real catalog, backend snapshot/pointer, applied migrations. No new Bridge probe. STOP_FOR_ARCHITECT after task; no automatic next task.

## CURRENT — 222 user feedback executed: closed-day guard, honest owner control, real type catalog — 2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-USER-FEEDBACK-222/RESULT.md` and `TASK.md`. Base
`472384b81e5cde3e9e2b45b3e3c7aa2a5c1172f8` (local was one docs-only commit behind; fast-forwarded, no
reset/stash/rebase/force-push). The pending frontend cleanup reported by the user was inspected FIRST,
preserved, and committed by this task: publication UI/API/model removed from the editor, owner line and
repetitive explanatory text removed, simple «نام»/«توضیح» labels, DB-native language labels, Saturday-first
DISPLAY order, and an unsaved-work warning only when the session is dirty. Backend publication/capture/
pointer capability, routes, migration and data were NOT deleted.
Executed: (1) a closed day now really disables BOTH `type="time"` bounds AND `withDayTime` refuses an
unknown or closed day at the handler boundary returning the same state (grey input is presentation, not the
protection); toggling closed preserves the exact remembered bounds, the ISO weekday and the stored time
text, and reopening restores them — proven live (08:30/12:45 → disabled+preserved → restored). Two old week
facts that typed bounds before marking the day working were corrected to the truthful order, no assertion
weakened; new `closed-day-222.test.ts` 3/3.
(2) the primary list now shows a REAL owner control populated only from the server-authorized
`GET /v1/admin/shipping-services/authoring-owner`, with strict decode, generation-guarded loads (rows
cleared, superseded scope never wins) and scope-keyed grid identity for saved views/export. This
installation is SingleStore/store-alpha, so exactly one lawful option exists (`همین فروشگاه`) with an
honest note; no fake Marketplace entry, no guessed GUID, no legacy row import, no owner picker in the
editor. Documented gap: an ALTERNATE authorized owner (Marketplace listing its real authorized sellers)
needs a server route returning the actor's authorized owner SET plus owner-keyed list/context reads — an
authorization-surface change, deliberately not faked with client filtering.
(3) the real type catalog was completed through the existing catalog API only: `heavy` →
`01a1785e-01f3-7000-b50d-d6372e756f89` and `extra_heavy` → `01a1785e-6b50-7000-a98f-2e1c80544815`, created
inactive then activated (both `isActive:true`, revision 2, sort 20/30) with fa-IR/en-US/ru-RU labels from
the DB registry; `standard/عادی` untouched and no type auto-assigned. `extra-heavy` is not a lawful code
(domain rule is `a-z0-9_`). The editor selector now lists عادی/سنگین/فوق‌سنگین from the API.
Verified: typecheck clean once after final source; ALL feature tests 127/127; import/feature/flat-folder/
test-discovery guards 7/7; live trial on FE:3000 with real traffic (no `/v1` mocks) for the closed-day
facts, owner options, direct View/Edit and type options; desktop 1440 + 390 screenshots stored (390 has
`overflowX 0`). User draft `01a16eeb-70b3-7000-87c2-564c82835496` still revision 2, `updatedAt` 2026-10-24,
1 region group, pointer `selectionVersion 3`; no Apply, no publication write, no migration reapply
(214/219 already shared-applied by 221).
NOT delivered / NOT claimed: Storefront integration (still no live selected-plan consumer), multi-owner
list (server authorization gap above), and any price/eligibility/activation/readiness statement. Backend
unchanged; three backend files in `git status` are line-ending-only drift and were NOT staged.
Active task: NONE. Next: Architect review, then hand the user the trial. STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. Elapsed ≈60 min vs the 30-minute box — recorded, NOT a process PASS. TB185 paused,
default-OFF, PendingAllocation preserved.
## CURRENT —221 bounded ADMIN USER TRIAL READY; WAIT FOR USER FEEDBACK —2026-10-25

Architect reviewed443c75817dca4d84065be73c4089fd29e9a96eba against87a138a224a3f12b3f1e4b481a9e4cc3980f7fd5, production/test diff, live receipts and screenshots01/07/08. Accept for ADMIN USER TRIAL, not full functional/visual/Storefront certification. Worker14 focused tests/typecheck/7 architecture facts green; root did not rerun. Four pre-existing source-size failures untouched.221 elapsed not reported: timing compliance UNKNOWN, not PASS.
USER TEST NOW: http://127.0.0.1:3000/admin/shipping-services
Draft: http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Primary list is real owner's existing drafts; post/NULL draft appears by server draftId, stored default-language name پیشتاز. View/Edit route DIRECTLY to new editor. Old catalog/create remains explicit ?legacy=1, not silently merged. New method/draft creation is not complete in this main workflow; don't tell user all legacy functionality was replaced.
Live221 target127.0.0.1:5432/tooba_alpha, SingleStore/store-alpha. BOTH214 AuthoringRevisionSnapshots and219 SelectedCapturedRevision migrations now APPLIED; runner current20261025130021_SelectedCapturedRevision pending0. Do NOT reapply nine old migrations/reset schema.
Host5088 Development PID26600, FE3000 PID28288 per worker221 receipts, health200. Current Host isolated executable path was NOT recorded: reverify from actual process if restart needed, do NOT assume obsolete207 binary. Ports/processes are time-of-receipt facts, not permanent PIDs.
Draft fields remain revision2, updatedAt2026-10-24T19:05:52.452036Z, preserved. Live publication wrote capture01a17389-c201-7000-8512-c2950a6e913a and pointer absent->selected1->cleared2->selected3. FINAL SELECTED3, source draft revision2; not restored to initial absent. Startup transport also consumed pending catalog/pricing messages. Shared changes are NOT NONE.
221 did NOT Apply ordinary draft edits; earlier209 save proof is history, not rerun. User should test list->Edit->areas/money/week->Apply->VIEW publish. Publication is disabled in EDIT; in VIEW same already-selected revision is intentionally disabled. No claim all error/race/lifecycle branches mounted-tested.
Small decoder/lifecycle hardening and disabled-state explanation can follow ACTUAL feedback; no speculative proof-only task now. Screenshot narrow content is inside a larger PNG canvas, not a native390px export; DOM reports390 no overflow, user visual acceptance pending.
Still NOTLIVE Storefront/checkout: no selected-plan consumer, actual fulfillment owner/type/group-membership/measurement source, projection/selection/commit connection; physical unknown remainsNULL. No price/activation/readiness effect. §29 and211–220 bounded decisions closed, TB185paused/defaultOFF/PendingAllocation.
Active task NONE. Next task NOT ISSUED. USER TRIAL GATE: call user now and wait for feedback before more Storefront implementation. No222 envelope. Recovery source review at221/ARCHITECT-REVIEW.md; original task receipt added there by Architect (worker omitted TASK.md). No Bridge/historical scripts. Older CURRENT headings are history.

## CURRENT —221 delivered: primary authoring list + publication controls; real local trial ready —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-LIST-PUBLICATION-USER-TRIAL-221/RESULT.md` and `receipts-221.txt`. 221 is a frontend feature slice plus ONE bounded backend consistency fix, then the REQUIRED local runtime/schema refresh and a real mounted trial.
Delivered: `/admin/shipping-services` is now the owner-authorized list of EXISTING authoring drafts (existing `authoring-drafts` GET) on `AppDataGrid`+saved views; View/Edit push straight to `/admin/shipping-services/authoring/{draftId}?mode=view|edit`; names come from the authoritative `authoring-context` default language, carried by an additive `MethodTranslations` on `ShippingAuthoringDraftItem` (one GET, no per-row N+1); a missing name is an honest fallback and duplicate bindings are flagged. The old catalog is reachable ONLY behind the explicit `?legacy=1` link and is never a silent fallback.
Publication panel inside the existing editor feature (GET state / POST publish with expectedRevision+expectedSelectionVersion / POST withdraw): synchronous in-flight gate, VIEW-only, 403/409/validation ⇒ stale latch, unestablished outcome ⇒ unknown latch, both cleared ONLY by an explicit successful read; a publication write never reseeds the form, advances the draft revision or discards typed work. Issued fix executed: a cleared result with no row now reports `Absent`; a real cleared pointer stays positive `Cleared`.
Executed live on the existing local `tooba_alpha`/store-alpha after an exact plan: Shipping `20261025130021_SelectedCapturedRevision` and the 214 snapshot migration applied (runner reports every module pendingCount=0), Host rebuilt isolated and running 127.0.0.1:5088, FE :3000, existing Development dev-actor identity. Real draft `01a16eeb-70b3-7000-87c2-564c82835496` (revision 2, «پیشتاز»): publish → Selected v1 → withdraw → Cleared v2 → publish → Selected v3, snapshot `01a17389-c201-7000-8512-c2950a6e913a`, always matching revision 2, and the DRAFT itself was never modified (revision/updatedAt unchanged, no Apply pressed). 1440/390 screenshots + receipts stored; 390 has zero horizontal overflow. Focused 14/14 pass, typecheck clean; import/flat/feature/test-discovery guards pass. `frontend-source-size` still fails on FOUR files this task did not touch (pre-existing baseline drift) — recorded, not repaired.
Active task: NONE. Next: Architect review + USER TRIAL at `http://127.0.0.1:3000/admin/shipping-services`; do NOT issue a Storefront implementation task before that trial. Publication remains selection INTENT only: no Order/Settlement/Storefront consumer, no projection/selection/commit, no price/eligibility/readiness claim. Creating a NEW method is still the legacy catalog. §29 and 211–219 bounded behavior preserved; TB185 paused/defaultOFF/PendingAllocation. Older CURRENT headings are history.## SUPERSEDED —220 bounded ACCEPT; 221 was the only issued task —2026-10-25

Architect independently reviewed69a3daa635b2386e6766a0d601fce1fe69d70145 against4fc9ccac044aa2a084a833172b645e6b0191f6f8: delegates authorize before body/ISender; validators distinguish missing/null/zero; owned draft derives binding; capture+select are two commits, no post-select dependency, current pointer CAS retained; withdrawal explicit. Worker8 real15432 endpoint facts passed TWICE (8 facts, not16 distinct), builds0 errors; no root reruns. 120>35 minutes and multiple corrections are NOT process PASS.
Retain220, no R1 loop. Small contract consistency carried into221: Clear absent currently returns Cleared/version0, contradicting DTO's 'zero only Absent' and nextGET Absent. Return Absent when cleared.Value.Exists==false, keep true cleared pointers positive. No schema/new policy.
USER PRIORITY: deliver a testable admin panel BEFORE further Storefront development. User rejects hours of invisible work. Only issued TB-SHIPPING-ADMIN-LIST-PUBLICATION-USER-TRIAL-221,parent220: direct new-editor View/Edit from service grid, explicit empty-draft setup only if necessary, publication controls in existing feature, then REQUIRED current local runtime/schema refresh and real mounted inspection for user trial. No further price/calendar/audit feature.
221 may apply ONLY existing pending214 snapshot +219 pointer migrations to nonproduction tooba_alpha/store-alpha after exact plan; build current Host/runner isolated and refresh exact Host5088 as necessary under user's ALREADY EXPLICIT authorization. Preserve frontend3000 unless truly requires task-scoped fix; preserve user browser/unsaved edits and draft fields. No resets/drops/reseeds/auth changes. Record actual startup/background effects, don't pretend zero data writes.
After221 result Architect reviews screenshots/source and calls user to test. Do NOT issue222 Storefront implementation before giving user this trial. Current list's primary actions still old until221 runs; no claim of replacement yet.
Still NOTLIVE Storefront: selected-plan consumer and real fulfillment owner/type/group membership/physical measurement authority + projection/selection/commit remain. Selected publication is intent, not customer-ready; use honest UI note. Physical unknown staysNULL. 211–219 bounded behavior and§29 closed; TB185paused/defaultOFF/PendingAllocation.
Active221 only, await result. No Bridge/historical post-result script; exact-path staging. Nine old migrations applied;214/219 still pending shared AT THIS CHECKPOINT (221 will record actual application). Host5088/FE3000 identities must be reverified, not assumed from old PIDs. Older CURRENT headings below are history.

## CURRENT —220 publication API delivered; NOT live —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-PUBLICATION-API-220/RESULT.md` (task verbatim in
`TASK.md`). 220 added the authorized publication HTTP actions for an EXISTING draft on BOTH
`/v1/admin` and `/v1/seller` families: `GET .../publication`, `POST .../publication`,
`POST .../publication/withdraw`. Thin real delegates only — existing owner authorizer (`View` for GET,
`Manage` for POSTs) FIRST, then the existing `ShippingEndpointBody`, then typed MediatR. Contracts DTOs use
`[JsonRequired]` with a NULLABLE expected selection version, so an omitted token is never version 0. GET
reads the owned draft, derives the binding from stored data, then reads the TB-219 pointer once and returns an
explicit Absent/Cleared/Selected state with version 0 only for Absent plus a pure DATA equality against the
current saved revision. POST rejects a foreign/stale draft BEFORE capture, rejects a KNOWN stale pointer token
BEFORE capture, then performs ONE existing capture and ONE existing select; withdraw performs ONE existing
clear. Capture + select remain TWO explicit commits: a lost pointer CAS returns 409, keeps the OLD pointer and
may leave a staging capture (asserted, no combined atomicity claim). Adjacent hygiene: `ReloadAsync` of a
missing row that leaves EF `Detached` now returns null (source guard, branch NOT executed).
`selected_capture.conflict`/`snapshot_unavailable` are 409 with FA/EN resources; `binding_mismatch`/`invalid`
stay 400. Executed: 0 build errors; ONE new class
`ShippingAuthoringPublicationEndpointTests` 8/8 green twice on the disposable
`127.0.0.1:15432/shipping_tb214` (container removed). No 12/9/43/18/8/119/194/16 or suite replay.
**NOT WIRED / NOT LIVE**: no Storefront/checkout consumer, no FE list/editor controls, no Order projection/
selection/commit; `Selected` is selection INTENT only — never eligibility, readiness, activation or price.
No new schema/migration, no shared migration, no user-draft write, no Host5088/FE3000 restart, no flag/seed.
214/219 migrations still NOT shared-applied. §29 and 211–219 accepted behavior preserved; TB185 paused,
default-OFF, PendingAllocation, nine applied migrations, real user draft/unsaved tabs untouched.
Actual ≈120 min vs the 35/23/12 boxes — recorded, NOT a process PASS.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next slice = publish/withdraw + direct
new-editor access from the service LIST, then real fulfillment owner/type/group-membership/measurement
authority through projection/selection/commit). No further calculator or generic audit; no 221 envelope.

## SUPERSEDED —219-R1 bounded ACCEPT; only220 authorized publication API —2026-10-25

Architect source-reviewed4fa863c9150c79759d43a5b1624e4eaed2e856cc againste4e3a1cf7b6fcc0e9ba7e84c77eb1526a49bb101. Fresh pointer reload precedes version/no-op decisions; Domain next-version validation now precedes target/time assignment; typed domain refusal preserved. Worker red3/green12 real15432 storage facts/build0; root source review, no reruns. Time26>20/stop12 deviation remains.
219/219-R1 accepted for the two demonstrated defects and ordinary select/clear/capture-pointer behavior; no R2 proof loop. Reload disappearance claim is NOT proven: EF can detach missing row without throwing;220 must add the small post-Reload Detached-state guard before returning tracked reference. This is adjacent hygiene, no schema/engine rewrite.
Only issued TB-SHIPPING-ADMIN-DRAFT-PUBLICATION-API-220,parent219-R1: real GET publication / POST capture-and-select / POST withdrawal for EXISTING draft, admin and seller thin authorized delegates + MediatR/validators/resources, reusing214 capture and219 selected pointer. No new publication store or lossy copy. Capture + select are TWO explicit commits; on pointer-race failure an immutable staging capture may remain but current pointer unchanged. Never claim combined all-or-nothing or activation readiness.
Active220. No221 issued. After review wire publication controls + direct new-editor access from service list, then actual fulfillment-owner/type/membership plan into Storefront projection/selection/commit. No further generic audit or calculator task. Physical weight/count unknown stayNULL.
NOTLIVE: current Storefront/checkout and old service-list workflow not replaced. User requested complete usable delivery; do not claim current internal primitives are live. Earlier2–4h was an estimate, not a verified deadline.
Preserve§29,211–219 accepted bounded behavior, actual user draft/current revision/unsaved tabs, Host5088FE3000. Nine old migrations applied;214/219 migrations NOT shared-applied.220 no shared migration/runtime/flag/FE operation. TB185paused/defaultOFF/PendingAllocation. Existing nonproduction authorization applies later; do not reask. No Bridge/historical post-result scripts; exact paths only. Older CURRENT blocks are history.

## CURRENT —219 review: two bounded write defects; only219-R1 issued —2026-10-25

Source eca53dae9c56bfb4b7f2be91eb54da64e1464319 independently compared with4972c6c8af39c000bceaebe4f0846540ec37f107. Main verified exact fullSHA (worker's HEAD line was truncated typo). Retain selected-pointer architecture/schema/Contracts reader and existing219 proofs, but do not close219 before R1.
Defect1: SelectAsync/ClearAsync use tracking ScopedPointer queries; a clean stale tracked pointer survives materialization. After another context advances it, an old expected version can return false no-op success (Select old target or Clear stale-cleared) and a current expected version can be rejected incorrectly.
Defect2: Domain Select/Clear mutate SelectedSnapshotId BEFORE NextVersion can throw at long.MaxValue. Exception occurs before TrySave cleanup; later caller SaveChanges can leak the rejected target/clear without a version advance.
Only issued TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219-R1,parent219: authoritative fresh pointer before comparison + exception-atomic domain mutation, focused real15432 regressions. Preserve tracked object identity/unrelated caller work; refresh only clean owned pointer deliberately, never ChangeTracker.Clear.
Active219-R1, await result. No220. No generic proof loop or new publication architecture. 219 worker9 real PostgreSQL +9 affected216 class reported green; root source review only. New cases absent from those tests. Hard30 respected total29 but implementation-stop20 exceeded; no process PASS.
Still NOTLIVE: no admin publication action/Order selected-plan consumer/projection-selection-commit. After repair close219 and proceed actual publication/fulfillment-plan integration, no new calculator. Physical measurements unknown; §29/211–218 remain closed.
214 and219 migrations NOT shared-applied, nine old migrations applied. Preserve actual draft/revision/unsaved tabs, Host5088/FE3000, TB185paused/defaultOFF/PendingAllocation. Local development authorization remains valid later. No Bridge/historical post-result script/external message; exact paths only. Historical CURRENT blocks below are superseded.

## CURRENT —219-R1 stale-pointer + rejection-atomicity repaired; NOT wired / NOT live —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219-R1/RESULT.md` (task
verbatim in `TASK.md`) and `219/ARCHITECT-REVIEW.md`. The two source-confirmed defects are repaired; the 219
schema/architecture/Contracts/owner-resolver are retained.
(1) `ShippingSelectedCapturedRevisionDirectory` now obtains the AUTHORITATIVE persisted pointer through a new
private `LoadPersistedPointerAsync` — the one owner-scoped tracked entry is explicitly `ReloadAsync`ed BEFORE
any expected-version or no-op decision (identity preserved, unrelated caller state untouched); a row that
vanished between query and reload is treated as absent (only that entry detached) and a stale leftover for a
provably absent row is detached so it can never act as a live token. `RequireCleanTracker` still refuses
dirty caller work first, with no refresh/flush/clear; the real CAS token and exact unique matcher still guard
writes.
(2) `Select`/`Clear` compute and validate the whole next state BEFORE any assignment (strong exception
safety), so a rejection at an exhausted version leaves target/version/`UpdatedAt` unchanged, the entry
`Unchanged`, a later `SaveChanges` at 0 and the DB unchanged; the adapter maps that domain rejection to a
normal `selected_capture.invalid` result instead of letting it escape.
Executed on the task-owned disposable `127.0.0.1:15432`/`shipping_tb214`/`tb214` (identity verified before
DDL, container removed, port verified silent): RED on unchanged production 3 failed / 9 passed, then GREEN
12/12 (the 9 accepted 219 facts + stale-select, stale-clear and max-version no-leak facts, each with an
independent DB read and preserved unrelated tracked identity). Build 0 errors; only the affected storage
class executed.
**NOT WIRED / NOT LIVE**: no HTTP route, no admin publication UI, no Order consumer, no
projection/selection/commit; selection remains publication intent only (not readiness/eligibility/
quotability).
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next work = real publication/selected-plan
integration + real fulfillment owner/type/group membership and physical measurement authority, then
projection/selection/commit; not another audit/calculator).
Preserve §29 decisions and accepted 211–219, real user draft/current revision/unsaved tabs, Host5088/FE3000.
Nine old migrations applied; the 214 snapshot migration AND the 219 pointer migration are NOT shared-applied.
TB185 paused/default-OFF/PendingAllocation. User nonproduction authorization stays valid for later necessary
live refresh. No Bridge interaction/historical post-result script/gitadd-A; exact-path staging only. Older
CURRENT sections are history.

## SUPERSEDED —219 executed; two write-correctness defects found in review, 219-R1 issued —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219/RESULT.md` (task
verbatim in `TASK.md`). 219 added ONE Shipping capability: `ShippingSelectedCapturedRevision` (one row per
FULL owner + canonical service/option) pointing at an **already existing** TB-214 immutable snapshot, table
`shipping.selected_captured_revisions` + ONE additive migration `20261025130021_SelectedCapturedRevision`,
a `NULLS NOT DISTINCT` unique owner/binding index (service/option persisted canonical), owner-coherence and
version CHECKs; `selection_version` is a real concurrency token. `IShippingSelectedCapturedRevisionDirectory`
(internal Select/Clear with `ExpectedSelectionVersion`, 0 = no row) and the Contracts-only
`IShippingSelectedCapturedRevisionLookup.ReadAsync` are served by ONE scoped adapter in `ShippingModule`.
Target resolution reuses the existing exact `GetCapturedRevisionAsync`+`ReadPayload` and NEVER captures; the
persisted binding is **derived** from the verified payload (missing/foreign/corrupt ⇒ `snapshot_unavailable`,
declared-binding mismatch ⇒ `binding_mismatch`). Create only from version 0; stale expected version ⇒
`selected_capture.conflict` before any mutation (no last-write-wins/auto-retry); same target at the current
version is a truthful no-op preserving version AND `UpdatedAt`; clear retains row/version and advances once
when changed; a pre-clear version cannot resurrect; rollback = deliberately selecting an OLDER existing
capture at the current pointer version. Unrelated dirty tracker is refused first; one `SaveChanges`; only the
exact 23505 on the named unique index and `DbUpdateConcurrencyException` map to conflict, the attempted entry
is detached on failure while caller exception/token propagate. 216's owner/edition/tenant coherence mapping
was extracted to `ShippingCapturedOwnerScopeResolver` and is reused by 216+219 only (216 unchanged, its class
re-executed 9/9).
Executed on the task-owned disposable `127.0.0.1:15432`/`shipping_tb214`/`tb214` (identity verified before
DDL, container removed): new class 9/9 real-PostgreSQL facts (schema/uniqueness, exact provenance + later
draft edit changes nothing selected, newer select then older rollback, no-op vs stale conflict, clear/no-ABA,
foreign+missing+binding-mismatch refusal with casing-same-pointer and owner separation, two-context
one-winner, sentinel/cancellation/dirty-tracker with later `SaveChanges`=0, Contracts reader
absent/cleared/scope/invalid plus provider+cancellation propagation); build 0 errors.
**NOT WIRED / NOT LIVE**: the 218/218-R1 composer does NOT consume this reader yet; no HTTP route, no
MediatR write, no admin publication UI, no projection/selection/commit, no Order consumer. Selection is
publication intent ONLY — not readiness, eligibility, customer permission or quotability; no Active/
CanCheckout flag, no Store-only restriction, no seller=fulfillment inference.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next slice = explicit admin publication wired
to this pointer + real fulfillment owner/type/group membership and physical measurement authority, then
projection/selection/commit; not another audit).
Preserve §29 decisions (customer receipt days, closed days skipped, same-group post-discount tax-inclusive
goods), accepted 211–218, the real user draft/current revision/unsaved tabs, Host5088/FE3000. Nine old
migrations applied; the 214 snapshot migration AND this new 219 migration are NOT shared-applied. TB185
paused/default-OFF/PendingAllocation. User nonproduction authorization stays valid for later necessary live
refresh. No Bridge interaction, no historical post-result scripts, exact-path staging only. Older CURRENT
sections are history.

## SUPERSEDED —219 issued; 218-R1 accepted; result awaited —2026-10-25

Architect reviewed dfdeae8afb1150d9beda10de43ec673f09c44aba against 3a583ab2c8a173bd4021046036074465218d75d1. Request-wide typed binding key/equality/hash and same-revision multi-group control are correct. Worker executed red3/green5 plus full composer16, build0 errors; not root reruns. Time26>12/two fixture corrections not process PASS. The casing regression also includes a whitespace-invalid service (not isolated casing execution); equality implementation independently inspected, no R2 proof loop.
218 composition is closed for this bounded behavior, NOT LIVE. Canonical recovery was absent from worker diff again; this TOP checkpoint is authoritative.
Only issued TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219, parent218-R1: Shipping-owned versioned pointer to an EXISTING immutable capture, optimistic select/clear and exact Contracts-only read by server owner+binding; rollback can select an older existing capture. One new Shipping table/migration, no legacy flattening, no price/calendar rewrite, no invented readiness. No HTTP/UI/shared migration/runtime/gate flip in219. Pointer selection is explicit staging/publication intent, NOT customer permission/eligibility.
Active task219. No220 envelope. Next after review: wire explicit admin publication and actual fulfillment owner/type/membership into current projection/selection/commit; use219 reader instead of browser/manual candidate revision. Physical weight/count stay NULL until real authority exists. Avoid further generic audits or old suite loops.
Preserve §29 decisions (customer receipt days, closed days skipped, same-group post-discount tax-inclusive goods), accepted211–218, user draft/current revision/unsaved tabs, Host5088/FE3000. Nine old migrations applied;214 snapshot migration still NOT shared-applied and219 will NOT apply its migration shared. TB185 paused/default-OFF/PendingAllocation. User nonproduction authorization remains valid for later necessary live refresh, do not ask again.
No Bridge interaction, no historical post-result scripts, exact-path staging only. Older CURRENT sections are history.

## CURRENT —218 reviewed; only218-R1 cross-group revision consistency repair —2026-10-25

Architect source-reviewed 6b3f3d2e940641f5b735d97b9da975acd2dcc05f and the exact base comparison.
218's composition of 217 whole-cart group facts, 216 Contracts quotes and 213 method-local receiving calendar is retained. Worker reports 11 new + 9 foundation facts/build green; Architect did not rerun them. This is NOT live Storefront acceptance. Time 33>25 and two corrections remain process deviations.
Concrete open defect: ValidateCandidates creates seenBindings INSIDE EACH group and uses case-sensitive delimiter strings although response binding uses case-insensitive service/option comparison. Consequently the same owner+service/option may select different captured draft/revisions in two cart groups (or evade detection with casing/destination changes).
Only issued TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218-R1, parent218: one request-wide typed binding->(draft,revision) registry, normalized service/option with existing semantics; same exact revision across groups MUST remain lawful, owners remain independent. Test actual ComposeAsync before any financial or Shipping call. No new architecture/publication/schema/HTTP/flags or old-suite replay.
Active task: 218-R1. Await its result; no219 issued. After review the next implementation must supply server-selected captured revision and real fulfillment owner/type/membership authority, then connect current projection/selection/commit. Physical weight/count unknown remain NULL; do not infer from decimal quantity. Do not advertise current composer as active/eligible.
Preserve §29 decisions, accepted 211–217, 218 composition, real user draft/revision/unsaved tab, Host5088/FE3000, nine applied migrations; snapshot migration214 NOT shared-applied. TB185 paused, default-OFF, PendingAllocation unchanged. No Bridge calls or historical post-result scripts. Older CURRENT headings below are history.

## CURRENT —217 boundedACCEPT; only218 compose realgroup money + capturedquote + receivingcalendar —2026-10-25

Read217/ARCHITECT-REVIEW.md. a213bd0e288f3155019118e9b4fa199d25721e5c acceptedreal217financialauthority: CartAccess/version/partition, existingPreviewPromotion->Tax, exactCartLineIdmap, ΣTaxInclusiveSnapshot pergroup. Worker9new/194retained/buildpass;notrootreruns;timeover25recorded. Physicalweight/itemcountunknownremainNULL, notquantitycast.
Onlyissued TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218,parent217;25min/stop17. ONEreadonlyOrdercompositionservice combines217factsONCE with216Contractsquotes perexacttrustedcandidate and213calendar inmethodtimezone. Candidateplanexplicitserverinput, NOTbrowser/publish/activeauthority. No guessedSellerowner/type/measurement orlegacymoney/windowfallback. Availability≠monetaryQuoted. No newHTTP/liveflagyet.
Nextafterreviewmustsupplyactualowner/type/capturedrevisionselection andphysicalsource thenconnectprojection/selection/commit; don'tcallcomposerStorefrontcomplete. Needusertrialonlyafterrealpathwired. Noanothersourceaudit. §29decisionsresolved/previousstagesaccepted;no replay. Preserveactualuserdraft/revision/unsavedtabs/Host5088FE3000;214tablenotsharedapplied,9oldmigrationsdone. TB185paused/defaultOFF/PendingAllocation. Await218result;olderCURRENTsuperseded.

## CURRENT —216 boundedACCEPT; only217 real same-group goods amount authority —2026-10-25

Read216/ARCHITECT-REVIEW.md. Source5f7aed605ea0225888a7d66e13e51b8399402b9c exactcapturedContracts+verifiedGeo+scopedDIaccepted;worker9new/11legacy/23compatgreen. Source-reviewedbranches!=fullrealGeoexecution;time43>25notPASS. NoStorefrontconsumer/publishselectionyet.
ROOTFOUNDrealexistingmoneyauthority: CheckoutDirectory.PreviewAsync -> QuoteSellerOrdersAsync -> Pricing/Promotion -> Tax; TaxInclusiveSnapshot ispostdiscounttaxinclusivegoodslineamount. _orderLineIdByCartLineId isexactexistingmap. NoCartSubtotalExclusive substitution, noOfferIdjoin/proportionalallocation.
Onlyissued TB-SHIPPING-GROUP-GOODS-QUOTE-FACTS-217,parent216;25min/stop17. NewreadonlyinternalOrderpreviewgroupfactsreaderreusesSAMEquotepipeline, authenticcart/version/completepartition/cartlineidentity, pergroupsumalreadycomputedTaxInclusiveSnapshot; unknownphysicalmeasurementsremainnull. No losslycopy/schema/OrderliveShippingconsumer/flag yet. Nextwirecapturedselection+realgroupfacts+213calendaracrossprojection/selection/commit afterreview; noanotheraudit.
§29decisionsresolved,211FE119/213calendar/214capture/215manualquoteaccepted; no replay. Realuserdraft/revision/unsavedtabs andHost5088/FE3000preserved;214tablenotsharedapplied.9oldmigrationsapplied, no routineupdate. TB185paused/defaultOFF/PendingAllocation. Await217result;olderCURRENTsuperseded.

## CURRENT —215 boundedACCEPT; only216 exact-capture Contracts quote edge + verified destination —2026-10-25

Read215/ARCHITECT-REVIEW.md. fff92bb4552d0a9e5c48aa362ddd36c9059fb5cf manualengineextensionaccepted;worker11new+23compatpass. Same-rowfreight+packaging,3ANDaxes,taxinclusivegroupamountinput,Prepaid/CODsplit. No active/runtimeconsumer yet;Free/Carrierunavailable, EligibilityNotEvaluated.
Onlyissued TB-SHIPPING-CAPTURED-QUOTE-CONTRACTS-EDGE-216,parent215;25min/stop17. ThinShippingContractsrequest/result+scopedadapter exactowner/draft/rev, no latestfallback, actualGeographyparentclosure/nulladmin/inactive+identityvalidation, existingengineonly. Correctpotentialmissingalternativecase andverifieddeepadminpathinthissameedge task. Focusedtests, no anotheraudit/replay.
NOpublicpublish/pointer/Orderconsumer/sharedmigration/flagflip yet. Carttaxdiscountgroupamount/measurementauthority and activationselection stillopen, notsyntheticdefaults. §29userdecisionsresolved,213calendar/214capture/211FE119accepted, no reasking. Preserveactualdraft/revision/unsavedtabs,Host5088/FE3000,9appliedmigrations;214tableNOTsharedapplied. TB185paused/defaultOFF/PendingAllocation. Await216result;olderCURRENTsuperseded.

## CURRENT —214-R2 boundedACCEPT; only215 captured MANUAL quote evaluation —2026-10-25

Read214-R2/ARCHITECT-REVIEW.md. Source8790165bd95bc48f64ecdc69a8b03d777edf3110 ownentrycleanup/dispose, actualstalectx/concurrency/sentinel/cancel/realuniqueproofaccepted;worker8/8realPostgres. Capturefoundationclosedforboundedbehavior, noR3loop. R2timeover12notprocessPASS. NewdisposalhelperneedsEXACTtype-onlyguardentry+narrowcompatcheckin215, nofileallowlist.
Onlyissued TB-SHIPPING-CAPTURED-MANUAL-QUOTE-215,parent214-R2;25min/stop17. ExtendexistingShippingrateenginewithpureexactcapture evaluation, 3ANDaxes/perrowfreightANDpackaging/PrepaidCODsplit/metadata, meaningfulfocusedtests. No readliveDraft/latestfallback, no lossyconversion/newpriceengine/Orderliveconsumer/publishpointer. Puretrustedinputonly; taxinclgroupamountcomesfromownerlater, no fakeweight.
Nextafterreview: trustedowner+selectedcapturedrevisionContractsedge and realgroupprice/measurementsource thenprojection/selection/commit connection. Notanothersourceaudit. §29decisionsresolved,213calendar/211119FEtestsacceptedandnotreplayed. CapturesNOTLIVE, tableNOTsharedapplied; preserveactualdraft/revision/unsavedtab, Host5088/FE3000 and9appliedmigrations. TB185paused/defaultOFF/PendingAllocationunchanged. Await215result;olderCURRENTsuperseded.

## CURRENT —214-R1 partialrepairs; only214-R2 exactcleanup + truthful concurrencyfacts —2026-10-25

Read214-R1/ARCHITECT-REVIEW.md. Rootobservedmainfixes but testsoverclaim: B-tokenfreshcapture usesNEWfreshInA notactualstaleA;duplicatecapturebarrierblocks synchronouslybeforeawait and ignoresSignalAndWaitbool. CaptureownAddedentrysurvivesrollback(false);exactunique catchnullstransactionwithoutDisposebeforeread. Fixonlythose, no newarchitecture.
Only issued TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R2,parent214-R1;12min/stop7/setup<=3. Existingisolated15432target; no schema/newAPI/flags/userdata/sharedmigration/Hostrestart. Trueconcurrenttasks+gatearrivalassertions, actualstalecontext, fault/cancel/uniquerecovery leavescallerstatebutnoattemptedentry/laterSave0. Awaitresult; no215yet.
43/43+18/18workerreportednotblanketproof;R1time48>20recorded. §29usersemanticsresolved;213calendar/211FE119accepted, no replay. CaptureNOTLIVE, no lossylegacyconversion/defaultflip. Preserveactualdraft/unsavedtab,Host5088/FE3000,9appliedmigrations;TB185paused/PendingAllocation. OlderCURRENTsuperseded.

## CURRENT —214 PARTIAL; only214-R1 capture correctness + isolated providerproof —2026-10-25

Read214/ARCHITECT-REVIEW.md. Source747036455 notacceptedforintegration. RootfoundstaleEFtrackedgraphdespiteDBlock, dirtytrackerSaveleak, firstreturnJSONB/time notprovider-normalized, substringconstraintcatch, missingexacthistoricalreader. Storagefactseeds2groupsbutSingle() so cannotbeclaimedgreen; onlydomain28green, relationalblocked. Codecidentitydefaultgap. 70minvs25timedeviationrecorded.
Only issued TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R1,parent214;20min/stop12. Scopedcorrections+realPostgresonTASKOWNED127.0.0.1:15432/shipping_tb214/tb214 parsedandliveverified, setup3minmax. Existing55432helperunchanged; no arbitrarysharedtarget/fallback/netsh/portreset. NOsharedmigration/runtimechange. Awaitresult; no215yet.
Oldcapture reader mustworkafterdraftadvances; allpersistedfields/ownerskept, dirtycallerworknevercleared/saved. No broadtests/genericarchitectureloop. Userneedsquickcorrectfinish, don't repeat70min.
Do NOT mapcaptureinto legacy via warnings (againrejected). No publication/livepointer/Orderconsumer. Calendar213acceptedbutnotwired;119FEtestsandcontrastaccepted. §29userdecisionsresolved; no reasking. Preserveactualuserdraft/unsavedtab, Host5088/FE3000,9alreadyappliedmigrations, defaultOFF/TB185paused/PendingAllocation. OlderCURRENTsuperseded.

## CURRENT —213 calendar accepted; only214 immutable owner-scoped revision capture —2026-10-25

213source27a8f834005cabfbf6ecfbd5e0dc7b21c2d64298 ACCEPT_BOUNDED_CALENDAR_PRIMITIVE,worker11/11+build; NOTWIRED. Read213/ARCHITECT-REVIEW.md for horizons count-vs-span,timezone/zerooffset/maxduration limits. Userdecisions§29 remain: receiptfromorder,closedskip, samegroupgoodsAFTERdiscountANDtax, authoredweekreceivingnotpickup. Do not reask.
Only issued TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214,parent213;25min/stop17. Code:completeimmutableShippingownedrevisioncapture+strictowner/revisionreader, no livepointer/publicpublishroute/Orderconsumer. Store ALL authoredfacts including translations/type/rowpackaging+amountbounds/deliveryrange/week/settings; do not flattenlegacy or blanketrejectSeller. Existing owner/editionauthorization reused. This is capturedcandidate, NOT activation/readiness proof. No anotheranalysis task.
No liveDB migration/runtime changes until214review; disposableonly DB proof withinbox ifavailable. StageEXACTpaths, no gitadd-A or historicalhelperexecution. Preserveactualuserdraft/revision/unsavedtabs, Host5088/FE3000,9existingmigrations,119acceptedFEtests. TB185paused/defaultOFF/PendingAllocation unchanged. Await214result; olderCURRENTsuperseded.

## CURRENT — user runtime semantics resolved; only213 customer receiving calendar calculation — 2026-10-25

User clarified: delivery1–3days is CUSTOMER RECEIPT promise from order, prep+carrier inside it; amountcondition uses samegroup final goods amount AFTER discount AND tax; authoredweek is CUSTOMER RECEIVING availability, not legacycarrierpickup. Canonicalrules§29 records exact statements and limits. Do not reask/revert these. Examplemin2workingdays: nextactive day#1, nextactive#2 earliest; skipclosed, use configured dayhours.
Only issued TB-SHIPPING-STOREFRONT-CUSTOMER-RECEIVING-CALENDAR-213,parent212-R1;20min/stop12. Add pure calculation to existing OrderStorefrontShippingCalculator with explicit validatedweek/timezone/reference date/min+provided horizon; no current consumer/gate/default change yet. Focusedcalendar tests only. Not withdrawn old snapshotcopy213; this is a DISTINCT exactTaskID. No new publicationtables/pointers/flags/engine.
Preserve maxdeliverydurationmetadata; no cutoff/slot-size/holiday/capacity policy guessed. No runtime/daypromise fallback to legacy pickup. Next afterreview: wire truthful owner-scoped published/config seam and projection/selection/commit, not generic audit.
211UIcontrast/119tests accepted, no replay. Real draft/currentrevision/userunsavedtab and Host5088/FE3000 preserved;9migrations applied. Userlocaldev authorization persists. DefaultOFF/TB185paused/PendingAllocation unchanged. Await213result; olderCURRENTsections superseded.

## CURRENT — 212-R1 facts accepted; THREE product semantics needed, no active worker — 2026-10-25

Read212-R1/ARCHITECT-REVIEW.md. Corrected runtimeDI/adapters accepted; lossy legacy conversion rejected. Snapshot alternative is NOT implementation approval: moving livepointer beforeactivationdefined, omittingtranslations/type and blanketrejectingSeller/Marketplace not accepted. NO213 issued. ActiveNONE/awaitingworkerNONE; next is productanswers, not anotheraudit.
Canonical rules6/8 explicitly defer runtime bases. Ask user only:
1 authored method week = dispatch/working hours or customer's receiving hours?
2 min/max delivery days counted from handoff/dispatch or from orderconfirmation (sellerprep is separate)?
3 merchandise-condition amount for SAME shippinggroup = afterdiscountbeforetax, beforediscountbeforetax, or aftertax?
Don't reask independent modes/free definition/owner authorization (alreadyapproved rules2/4/7/13/18). Recommend explicit publication separate from SAVE as architecture, no live activation yet. Onceanswersarrive recordthem in canonicaldecision, then one executable integration slice, no analysisloop.
211contrast +119/119all14featuretests/typecheck/7guards accepted; no replay/retestrequest. ExistingStorefront shipping exists, newdraftintegrationpending. Userneedsfastdelivery; don't hide gaps or invent policies.
Preserve realdraft01a16eeb-70b3-7000-87c2-564c82835496/currentrevision/userunsavedwork; Host5088/FE3000 and9appliedmigrations. Localdev authorizationpersists, no productionclaim;TB185paused/defaultOFF/PendingAllocation unchanged.
212-R1 gitadd-A included old executablepost-result.js/receipts; preservehistorybutNEVERexecute. Futurestageexactpaths. OlderCURRENTsections below superseded.

## CURRENT — 213 customer receiving-calendar primitive done / NOT wired — 2026-10-25

Rules §29 owner decisions are implemented as one additive pure primitive in the already-existing
`Order.Application/Storefront/Services/StorefrontShippingCalculator.cs`
(`BuildCustomerReceivingCalendar` + `ResolveReceivingWeek` + `FindEarliestReceivingDate`) with four cohesive
records appended to `Storefront/Models/StorefrontOrderModels.cs`. Receipt promise from the order date, closed
days skipped in counting and never offered, each date carries its own configured interval (no static 9-12,
no browser clock), all-closed week = explicit no availability, malformed week/interval/offset/horizon rejected
through the existing `shipping.delivery.invalid` code (no clamp, no default week). Legacy members unchanged.
`dotnet build` clean; 11/11 focused tests. **Not wired**: nothing consumes it, published-week source absent,
gate default-OFF, no Storefront integration/completion claim. Next single step (NOT issued): reviewed wiring
from the published method week, per shipping group, only after published-revision authority exists.
Remaining: published-revision authority (212-R1 slice), owner/edition authorization, same-group goods amount
after discount+tax, tax/discount allocation, money/day mapping. Active task: NONE. Next task: NOT ISSUED —
STOP_FOR_ARCHITECT. Read
`docs/architecture/evidence/TB-SHIPPING-STOREFRONT-CUSTOMER-RECEIVING-CALENDAR-213/RESULT.md`.

## CURRENT — 212-R1 corrected source facts posted / awaiting Architect review — 2026-10-25

Docs-only correction under the 10-minute box (no src/flag/schema/DB/build/test/browser change). Retractions:
211/212's "Shipping Contracts only / no implementation" is false — `ShippingModule.AddServices` registers
`IShippingPackagingFeeLookup`, `IShippingShipmentCostLookup` and `IShippingOperationalQuoteLookup`, and
`Infrastructure/Operations/ShippingOperationalQuoteLookup.cs` loads the four module-owned slices then calls
`ShippingOperationalQuoteEvaluator.Evaluate` (freight + packaging, payer split, stable reason codes).
Service/option/geography/currency = applicability, not tenant/seller authorization (tenant binding is real
via `ShippingDbContext` + `ICurrentCommerceContext`; seller scope remains the gap). Operating schedule =
carrier pickup/dispatch windows, NOT a customer delivery promise.

Rejected: publishing a draft by copying into `RatePolicy`/`CoverageGrant`/`PackagingPolicy`/
`OperatingSchedule`. Actual source proves lossy representation (per-row packaging vs scope-only
`ShippingPackagingFeeResolver`, merchandise bounds, delivery min/max vs prep+lead, `CalculationMode`
Free/CarrierApi, owner/seller identity, week semantics), and partial activation with warnings is forbidden.

Chosen next slice (NOT issued, no implementation authorized): Shipping-owned immutable **published revision
snapshot** — owner identity, per-row freight+packaging, matched conditions, delivery range, week and settings,
read through new Shipping Contracts; one transaction, `ExpectedRevision` optimistic guard, rollback by
pointer, existing runtime rows never deactivated, and **no consumer/activation** in that slice so no product
policy is guessed. Named blockers: week meaning, `CalculationMode` runtime semantics, seller/marketplace
authorization, activation semantics. Read
`docs/architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212-R1/RESULT.md` and the
appended correction in `.../TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212/RESULT.md`.
Proposed TB213 (copy-into-legacy publish) is withdrawn. Active task: NONE. Next task: NOT ISSUED —
STOP_FOR_ARCHITECT. Gate stays default-OFF, TB185 paused, nothing connected. Older CURRENT headings below
are superseded.

## SUPERSEDED — 212 publication proposal REJECTED; only212-R1 exact source correction — 2026-10-25

Root independently read ShippingModule+OperationalQuoteLookup: packaging/operational implementations and scopedDI ALREADY EXIST.212 claimed none: false. Root also read operational contract: week means carrierpickup, not customer delivery promise. Don't duplicate adapters or implement proposed213 snapshotcopy.
Read212/ARCHITECT-REVIEW.md. Other blockers: per-row packaging cannot flatten onepolicy/currency, owner != geography and tenantcontext isolation exists, IsActive flags don't prove snapshot provenance/rollback, partial publication of unsupported delivery/amount/mode is forbidden. Store-only restriction is not approval for Marketplace/Seller architecture.
Only issued TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212-R1,parent212;10min/stop6, source-only targetedcorrection. NO213implementation authorized. Await result, then one concrete safe runtime implementation slice, not generic audit. Runtime gateOFF/TB185paused remain.
211119/119featuretests/typecheck/7guards and contrast accepted; no replay. Actual draft/user unsaved data, Host5088/FE3000 and applied9migrations preserved. Prior explicit localdev authorization persists. User needs quick correct integration, not unsafe partial price. Older CURRENT headings below superseded.

﻿## CURRENT — 212 bridge contract posted / awaiting Architect review — 2026-10-25

212 (source-only, no code change) grounded the draft→storefront path in existing authority instead of a new
calculator. Authority chain: Shipping.Contracts.Rates.IShippingRateQuoteLookup →
Shipping.Infrastructure/Rates/ShippingRateQuoteLookup (reads Shipping's RatePolicies/CoverageGrants/
MethodCoverages) → ShippingRatePolicyEngine; Order consumes it only through the gated
StorefrontShippingOwnedRateResolver (default OFF, StrictOwnedOption, parcel always null, currency/destination
fail-closed). IShippingPackagingFeeLookup and IShippingOperationalQuoteLookup already exist as Contracts with
NO implementation/consumer. Runtime writes already exist as Shipping.Application commands (rate policy,
coverage grant, packaging policy, operating schedule).
Publish decision: a draft publish is a WRITE that copies one revision snapshot into those Shipping aggregates via
the existing commands; runtime readers never read drafts; unsaved edits cannot leak; rollback = reactivate prior
rows; active state = existing IsActive (no new flag, no Fulfillment-owned publication). Exact representable
mapping: destinations (one policy+grant per node), freight→ChargeAmount with packaging as its own policy (never
summed), weight/count conditions, weekly hours+timezone→OperatingSchedule, FreightPaymentMode→packaging
FreightPayerMode. Not representable: merchandise-amount axis, CalculationMode, per-language rate labels,
seller ownership in RatePolicy, and min/maxDeliveryDays → PreparationDays/LeadDays (undecided policy).
Selection/commit parity is structural: ProjectAsync and CommitAsync share LoadEligibleMethodsAsync
(commit replays SaveSelectionAsync).
Next slice (not issued): TB-SHIPPING-ADMIN-DRAFT-PUBLISH-SNAPSHOT-213, Shipping-only publish command with
ExpectedRevision guard, UnrepresentableAxes[] honest reporting, Store-owned drafts only, no Order change;
then a trustworthy parcel source, admin publish UX, real local end-to-end, then customer UI. Active task: NONE.
Next task: NONE — await Architect review. User Visual ACCEPT PENDING; no quote engine, no flag change.
## CURRENT — 211 accepted; only212 runtime bridge contract/reuse decision — 2026-10-25

211 contrast accepted after source+desktopPNG review. Worker119/119all14recursive feature files, typecheck clean, guards7/7; one exact stale gram-label assertion correction. Mobile screenshot shows only first area (not proof of both colors), no extra screenshot-only task. User said better; User Visual ACCEPT not blanket granted.
Read211/ARCHITECT-REVIEW.md. Important runtime correction: StorefrontShippingService already calls gated StorefrontShippingOwnedRateResolver -> Shipping.Contracts.Rates.IShippingRateQuoteLookup; runtime is NOT config-only. Must reuse existing seam, no second engine. New authoring draft type/areas/fees/week not integrated. 'Read-only activation' and foreign Fulfillment-owned publication state NOT approved.
Only issued TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212,parent211;12min/stop8,source-only. Trace existing authority/owner/currency/measurement/week and approve one concrete minimal implementation plan from source; no code/flags/data/runtime changes. Await212 result; next implementation chosen after review, not another generic audit.
Live draft01a16eeb-70b3-7000-87c2-564c82835496, actual revision/user unsaved work preserved. Host5088/FE3000 running;9migrations already applied. Prior local nonproduction authorization persists.190–211 don't replay; TB185paused/defaultOFF/PendingAllocation unchanged. Older CURRENT headings below are superseded history.

﻿## CURRENT — 211 posted / awaiting Architect review — 2026-10-25

211 executed the user's positive-feedback follow-up, frontend + verification only.
Contrast: ui/authoring-groups-editor.tsx alternates the WHOLE area card by display position — even keeps
g-surface-elevated, odd uses the existing theme-aware 
gb(var(--color-section-alternate)) token
(247 248 250 light / 22 22 26 dark). Measured live card0 
gb(255,255,255) and card1 
gb(247,248,250) at
1440 and 390, both w=1057, no overflow. No new token/CSS/model/persistence.
Checks: ALL 14 feature test files enumerated recursively (incl. nested pi/ and session/) and passed by
explicit path — 119 tests, 119 pass, 0 fail, 0 skipped. First run had one stale assertion (Weight: vs the
truthful Weight (g):); it was corrected against the real rendered text, no assertion weakened. typecheck
clean; import/feature/flat-folder/test-discovery guards 7/7. No backend/solution build, no source-size loop.
Live: the real draft currently has ONE stored area; a second area was added only locally in Edit, captured,
then discarded via the app's own confirm; stored 120000.00/5000.00 and 1 area unchanged, no Apply/HTTP write.
Storefront status (source only): storefront/checkout shipping choice + projection endpoints exist
(/v1/storefront/shipping/* → Order StorefrontShippingService), but they read configuration
ShippingMethodsOptions / ShippingMethodRegistry plus the Fulfillment catalog; Modules/Order has ZERO
references to the authoring draft, so authored type/areas/fees/week do not reach customers and no
publish/activation seam exists. Next slice (not implemented) = read-only publish/activation projection.
Active task: NONE. Next task: NONE — await Architect review; User Visual ACCEPT PENDING; default-OFF intact.
## CURRENT — user says UI better; task211 alternating delivery areas + editor checks — 2026-10-25

User feedback: "better"; requested alternating white/very-light-gray DELIVERY AREA cards so adjacent areas are distinguishable, requested fast tests/all editor parts working, asked whether Storefront is done. This is bounded positive feedback, not blanket production/visual certification.
Only issued TB-SHIPPING-ADMIN-AREA-CONTRAST-AND-EDITOR-CHECKS-211,parent210-R1;20min/stop12. Minimal card background by current display position; preserve all accepted alignment/behavior. Run ALL feature tests recursively (include nested api/model/session), one typecheck, existing four boundary guards, short actual two-area local-unsaved visual inspection. No business write/reset/restart or broad backend suite. Source-only trace of draft->runtime/storefront seam, exact implemented/missing facts; no unsolicited activation/quote implementation.
Current Storefront integration of this NEW authoring flow has NOT been accepted/delivered; don't imply existing storefront/checkout is absent. TB185paused/defaultOFF/PendingAllocation persists. Await211 result; no second task meanwhile.
Live URL http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Preserve actual draft revision/user edits, Host5088/FE3000 and applied9migrations. Standing local-dev authorization persists.190–210 functional/visual bounded reviews remain accepted; no replay. Read TOP recovery first in a new chat. Older CURRENT sections below are superseded history.

## CURRENT — 210-R1 reviewed; ready for USER VISUAL RETEST — 2026-10-25

Architect ACCEPT_BOUNDED_WEEK_PRESENTATION: source58f484e83b6b2665e4d2a3bb92cd166cd3ccb574 plus all5 actual PNGs independently viewed; read210-R1/ARCHITECT-REVIEW.md. Desktop common widths/tabs/seven-row alignment210 accepted; mobile paired labelled bounds and truthful view columns now complete, FA/EN actual week evidence inspected. User Visual ACCEPT PENDING; prior10/20 rejection is not overwritten by self-grading.
Active task NONE. Awaiting worker NONE. Next automatic task NONE. Call user for visual retest now; no proof-loop tasks or190–210 replay.
URL http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Preserve current content/revision/unsaved user browser. Worker observed no HTTP writes, fees120000.00/5000.00 unchanged; actual revision not independently re-proven. Prior209 real save accepted; do not replay it. Host5088/FE3000 remain preserved; historical PID10800/19108 must be freshly verified only if runtime trouble. All9 migrations208 already applied, no routine reapply/rebuild/reset.
Standing local nonproduction authorization persists, no repeated approval. No quote/activation/whole-method-create/production/certification claim; TB185paused/defaultOFF/PendingAllocation unchanged.210 timing32>20 remains deviation;R1 typecheck twice disclosed, no broad sweep. Older CURRENT headings below are superseded history. Continue only from actual user feedback.

﻿## CURRENT — 210-R1 posted / awaiting Architect review — 2026-10-25

210-R1 finished exactly the three review items, frontend only. ui/authoring-week-editor.tsx: narrow screens
render TWO lines per day (day+status, then a paired equal-width From/To line with visible localized labels)
instead of three; each bound is a <label> whose accessible name is "<weekday> From/To"; view mode is
truthful per column (Status = «روز کاری»/«تعطیل» text, From/To = stored bounds, a closed day shows an honest
dash) with zero inputs and no duplicated formatted range. Desktop 210 geometry is retained exactly
(day 481/607, status 457, start 209/120, end 81/120; all seven rows identical, header matches).
Evidence at docs/architecture/evidence/TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210-R1/ now contains REAL
390x844 viewport bytes with the week scrolled into view (the 210 file defect is not repeated) plus a bounded
EN/LTR cookie check. No Apply/HTTP write/migration; stored 120000.00/5000.00 unchanged; typecheck clean.
Active task: NONE. Next task: NONE — await Architect review; User Visual ACCEPT PENDING.
## CURRENT — 210 desktop accepted; only 210-R1 mobile week completion — 2026-10-25

Architect viewed all four210 PNGs and source. Desktop common widths/tab geometry/seven row columns accepted within scope; User Visual ACCEPT remains PENDING after prior10/20 rejection.
Actual mobile file shows TOP General, not week; source has three lines/day and no visible mobile From/To labels. View puts time range under Status with empty From/To cells. EN evidence absent.
Only issued task TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210-R1,parent210: complete paired labelled mobile times, truthful view columns, actual week screenshot at390 plus bounded EN/LTR check; preserve desktop. Hard12min/stop7. No save/domain/backend/runtime restart/proof replay.
Read210/ARCHITECT-REVIEW.md and task. Await210-R1 result; do not issue another task meanwhile. Preserve real draft01a16eeb-70b3-7000-87c2-564c82835496, actual revision/unsaved work, Host5088 and FE3000. Local-dev authorization persists.190–209 functional accepted, do not replay. TB185paused/defaultOFF. Older CURRENT sections below are historical and superseded.

## تسک جاری210: کاربرهم‌ترازیUIراردکرد/10از20 —2026-10-25

کاربردوتصویرواقعیپسازR2داد:تب‌ها/فیلدها/کنترلروزکاری‌تعطیلهماهنگنیست؛نمره10/20. **UserVisualACCEPT=REJECTED_ALIGNMENT.** پذیرشfunctional209ذخیرهومدلخطاهابرقرار،نهپذیرشUI.
ArchitectتصاویرراواقعاًدیدوکدموجودGeneral/weekراخواند:Generalmaxw4xlزیرouter6xl،typemaxwsmجدا،tabactiveborderنامتقارن؛weekmd2columnsوinnerjustifybetweenچک‌باکس‌هاوساعت‌هاراپخشکرده.
**Only issued TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210**؛parent209-R2؛20min/stop13. فقطgrid/width/tabgeometry/7rowweekfixedcolumnsوformspacingدرfeatureموجود. نهbackend/data/savepolicy/sessionrewrite.
معیار:desktopmatched1440x900هردوتبباهمانsidebar،DOMboundingboxمحورهاو7status/start/end<=2pxاختلافستونی،mobile390بدونoverflow؛یکtypecheck،نهsuite/testساختگیCSS. userدوبارهjudgeمی‌کند؛مدلخودشبهUIنمرهندهد.
Draftواقعی01a16eeb-70b3-7000-87c2-564c82835496وactualrevisionمحتواحفظ؛userbrowserunsavedreloadممنوع؛0write/nomigration/Host5088/FE3000محفوظ. مجوزdevپایدار،proof/saveتکرارنشود.
پسازنتیجه210reviewتصاویروترازواقعی؛تاresultتسکدیگریصادرنشود.اینبخشتمامhandoffNONEقدیمیراsupersedeمی‌کند؛issued≠claimed.

## مرجع قطعی چت جدید:209-R2 پذیرفته؛ اکنون تست کاربر —2026-10-25

**Architect ACCEPT_BOUNDED_NESTED_RATE_ERROR_REPAIR / READY_FOR_USER_RETEST**؛sourcecebc455147e779245c7317a2106afce041e885e5؛[reviewR2](../../architecture/evidence/TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R2/ARCHITECT-REVIEW.md).
mainپیشreview0011d86fفقطpostresult/contractبعدی،اجرانشد.
**Current phase USER_RETEST_LOCAL_LIVE_EDITOR. Active task=NONE؛awaiting result=NONE؛next task=NONE. کاربرراالانبرایretestصدابزن؛تسکاثباتی/visualجدیدخودکارنساز.**

URLواقعی:
http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view

### چه چیزهایی انجام شده

208:9migrationمحلیاعمال،Hostجدید5088وFE3000متصل،خواندنواقعی؛پذیرشفقطread.
209:ذخیرهواقعی+GET+reloadپذیرفته؛typeواقعیعادی/nameپیشتاز/محدودهIran/IRRfees120000و5000/weight1000–2000گرم/delivery1–3/AsiaTehranMonday09–17درrev2snapshot. کشورنامدار/zonevalidselect/خطایtimezoneعلتروشن.
209-R1:Architectهر4PNGراواقعاًدید؛RegionsVIEWبدونinputs/delete،Generalدوبلtypeحذف،4feeprimary+optionalconditions،discardمحلیبدونwrite. userVisualACCEPTهنوزنداده.
209-R2:groupIndex+rowIndexدقیقبرایrateerrors/dedup/selector،openingفقطdetailsوالد؛focused26/typecheckworkerreported؛browser/writeتکرارنشده. Rootrelatedknownrateinputscopeپذیرفتهنههمهخطاها.

### وضعیت حفظ‌شده و حدادعا

Draftوrevisionراresetنکن؛rev2مشاهدهقبلیاست،اگرکاربرتغییردادهactualread. HostآخرینمشاهدهPID10800،`D:\temp\tb207-build\out\Tooba.Host.exe`،launcher`D:\temp\tb208-host\run-host.cmd`؛FE3000آخرPID19108؛اولfreshprocessverifyاگرURLقطعشد،نهmigrate/buildازاول.
مجوزصریحuserبرایdevnonproductionمهاجرت/Hoststartup/repairمحفوظ؛همینکارهادوبارهapprovalنمی‌خواهد. نهDBreset/drop/unrelateddelete/externalproduction.
Limits:group-levelerrorهنوزsummary-onlyوgroupcontainerfocusاثباتنشده؛rateindicesبعدreorderبعدerrorشناسهپایدارنیست؛pickerهنوز4levelنمایش؛Generalviewdisabledinputs. این‌هاreviewثبتشده‌اند،بهخودیتسکجدیدنساز.
R1زمان37>30وR2~12>10/stopimplementation~8>6انحراف‌اندنهPASStime. UserVisual/Production/fullShipping/quote/activation/wholemethodcreate/certificationPENDING.
بعدtestفقطfeedbackواقعیبررسیورفعمحدود؛190–209replayممنوع،proofloopممنوع،TB185paused/defaultOFF/PendingAllocationمحفوظ. تمامpointerهایپایینتاریخچه‌اند.

---

## مرجع جاری:209-R1 ظاهر/مشاهده پذیرفته؛ فقطخطایمحدوده209-R2 —2026-10-24

209-R1commit893f35bf/mainb79669a1(postresult+contractonly)؛[review](../../architecture/evidence/TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R1/ARCHITECT-REVIEW.md). Architectهر4PNGراواقعاًدید:RegionVIEWtextبدونcontrols،Generaltypecardدوبلندارد،4feeprimary+optionalconditions،mobileviewخواناتر. پذیرشمحدودبرایretestنهuserVisualACCEPT. liveSave209rev2محفوظ؛R1هیچwriteنکرد.
**Only issued task TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R2**؛10min/stop6؛exactsourcebug:mappergroupIndexرادورمی‌اندازد،MaprowIndexبرایهمهgroups/globalNthfocus،پسgroup1.row0بهgroup0می‌رودودوerrorcollapseمی‌شوند؛optionalconditiondetailsبستهfocusنمی‌گیرد.
فقطgroup+rowplacement/focus/dedup+focusedregression/typecheck؛نهlayoutدوباره/wholeproof/browser/save/migration. پسازreviewاگرسبزuserرابرایretestهمانURLصدابزن؛تسکvisual/proofتازهخودکارنساز.
Pickerهنوز4selectهمهlevelsراrenderمی‌کند؛relevant-onlyخواسته‌شدهانجامنشدهوpresentationlimitationاست،نهscopeجدیدبرایحلقه. R1زمان37>30صریحnoncompliance.
Draft01a16eeb-70b3-7000-87c2-564c82835496populatedrev2snapshot؛usercontentرامحفوظنگه‌دار. Host5088/FE3000روشن،مجوزdevبدونتکرارapprovalمحفوظ.190–209scopeهایپذیرفتهدوبارهبازنشوند.issued≠claimed.

## مرجع جاری:209-R2 اجرا شد — منتظر بررسی معماری —2026-10-24

209-R1 به‌صورت bounded پذیرفته شد؛ تنها نقص واقعیِ باقی‌مانده اتصال خطای ردیف به گروه/ردیف درست بود.
209-R2 آن را اصلاح کرد: [RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R2/RESULT.md).
`ServerFieldIssue.groupIndex` از خودِ مسیر سرور (`regionGroups[i]`) استخراج می‌شود، dedup شامل group+row+input است،
نگاشت صفحه با کلید ترکیبی `rateRowKey(g,r)` به همان گروه/ردیف جاری می‌رسد، focus فقط از گروهِ مقصود → ردیفِ مقصود →
input واقعی می‌رود (بدون Nth سراسری)، و اگر فیلد داخل disclosure بستهٔ شرایط اختیاری باشد فقط همان باز می‌شود
(فقط نمایش؛ بدون تغییر مقدار/dirty/revision). ایندکس خارج از محدوده صادقانه کاری نمی‌کند و به row0 هدایت نمی‌شود.
typecheck پاک؛ 10/10 تست جدید دومجموعه‌ای + 26/26 مجموع سه فایل تمرکزی. بدون اجرای مجدد save/seed/migration/اسکرین‌شات،
draft واقعی rev2 دست‌نخورده. زمان ≈12min > 10min (ثبت صادقانه). Active task=NONE. Next task=NONE — منتظر بررسی معماری؛
سپس retest کاربر روی همان URL، بدون حلقهٔ بصری/اثبات جدید. Host5088/FE3000 محفوظ؛ TB185 متوقف.

## مرجع قبلی:209-R1 اجرا شد — منتظر بررسی معماری —2026-10-24

209-R1 نتیجه دارد: [RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R1/RESULT.md).
VIEW حالا واقعاً فقط‌خواندنی است (rows با RateRowSummary، title به‌صورت متن، هیچ input/button در پنل مناطق؛
گارد mode/busy در خود callbackها)، General فشردهٔ حرفه‌ای (بدون کارت تکراری نوع، صاحب یک خط، عرض محدود)،
ردیف هزینه چهار فیلد اصلی + شرایط اختیاری جمع‌شو با خلاصهٔ شرط، و نقشه‌برداری خطای سرور دقیق
(description→description، rows[i].minDeliveryDays→همان ردیف، timezone.required≠invalid، کد ناشناخته بدون حدس،
diagnostics جمع‌شو). typecheck پاک، 10/10 تست جدید + 6/6 تست سازگارشده. live mounted بدون هیچ نوشتن؛
مقادیر ذخیره‌شده دست‌نخورده (freight 120000.00 بعد از تغییر محلی و discard بازگشت). زمان ≈37min > 30min hardbox
(ثبت صادقانه؛ یک دور اصلاح). Active task=NONE. Next task=NONE — منتظر بررسی معماری؛ retest کاربر تا تأیید ایشان نه.
Host5088/FE3000 محفوظ؛ migration/seed/save دوباره اجرا نشد؛ TB185 متوقف.

## مرجع قبلی:209 ذخیره پذیرفته، UI هنوز ناقص؛ فقط209-R1 —2026-10-24

209source21e78d8f/main69f70e74(result-contractonly)برای**live save repair پذیرفته**؛[review209](../../architecture/evidence/TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209/ARCHITECT-REVIEW.md). کاربرUIراردکرده؛VisualACCEPTنداریم.
واقعاًdraft01a16eeb-70b3-7000-87c2-564c82835496درrev2باtypeعادی/پیشتاز/IRR/Iran/fees120000-5000/weight1000–2000/day1–3/AsiaTehranMonday09–17ذخیره+GET+reloadworkerreported. دوبارهseed/migrate/saveproofنساز.
ArchitectدوPNGdesktopراواقعاًدید(باgithub_fetch_file encodingbase64،whitespaceحذف):Viewهنوزinputوdeleterowدارد؛Generalکشیده/ریزودوبل،rateدهفیلدیکردیف؛compactUIتحویلنشده.
Sourceerrors:description→name؛regexdaysقبلratesباعثminDeliveryDays→weekly؛ratestargetAddGroup؛reasonregexناشناخته‌هاحدس؛inlineerrorsنیست. Timezone10suggestionنهconfiguredmarket. claim18:51:55→19:55حدود63min>45،RESULTwithin45متناقض؛PASStimingنیست.
**Only issued task TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R1**؛30min/stop22؛frontendفقطprofessionalcompactlayout+truthfulview/busyguard+exacterrormapping. realmountedpixelsGeneralEDIT/RegionsEDIT+VIEWوموبایل،بدونsaveدوباره؛currentdraftusercontentحفظ،0write،Host5088/FE3000محفوظ.
بعدنتیجهreview؛تااصلاحایننواقصواقعیuserراretestکاملصدانکن. نهfullproofloop/بازکردنbackendیا190–208؛مجوزdevمحفوظ. issued≠claimed.

## نتیجه209: کار واقعی ذخیره می‌شود — retest آماده —2026-10-24

**Worker result `READY_FOR_USER_RETEST`** (نه user Visual ACCEPT). Evidence کامل: [RESULT209](../../architecture/evidence/TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209/RESULT.md).
ریشه‌های اثبات‌شده: (1) چیپ مقصد تازه از `index.labelOf` خوانده می‌شد که فقط fact‌های ذخیره‌شده را دارد → `withPickedLabels` از ردیف واقعاً انتخاب‌شده؛ (2) PUT واقعی با timezone «تهران» دقیقاً `400 shipping.schedule.timezone.invalid` با `errors` خالی برمی‌گرداند → نقشهٔ `server-field-error.ts` روی فیلد/تب/فوکوس + کد فنی در diagnostics؛ (3) ورودی متنی timezone با selector شناسهٔ معتبر جایگزین شد؛ (4) catalog واقعاً خالی بود و یک type فعال «عادی» از API موجود ساخته/فعال شد؛ (5) «گروه»→«محدودهٔ ارسال» و واحد وزن روشن شد.
**Real evidence:** روی همان draftکاربر `01a16eeb-70b3-7000-87c2-564c82835496` (ابتدا خالی، revision1) از UI واقعی: nameپیشتاز/typeعادی/Prepaid+Manual/Countryایران/fee120000-5000/weight1000-2000/delivery1-3/Asia/Tehran/دوشنبه09-17 → **یک PUT** revision2 و reload همان مقادیر. 93+6 تست، typecheck0، mobile390 بدون overflow.
backend5088 PID10800 و FE3000 دست‌نخورده؛ migration تکرار نشد. active209=نتیجه ارسال شد؛ **next task=NONE**؛ فقط پس از review/بازخورد کاربر ادامه. 199–207 proof بازنشوند؛ TB185 paused/defaultOFF محفوظ.

## تسک209 (تاریخی): کاربر UI و ذخیره را رد کرد —2026-10-24

کاربر پس ازتستواقعی208اعتراضکرد: «گروه»نامفهوم،typeempty،انتخابمنطقه/مبلغ/ذخیرهکارنکردوUIنامناسب. دوscreenshotمشاهدهشد:Generaltypeempty/timezone«تهران»؛Regionsgenericservervalidationerror،newgroupتست،countrychip«نامموجودنیست»،pickerهای—،fees120000/5000،weight1..2/days1..3.
**208 فقطruntime/schema/readmount پذیرفته،نهuserUI/fullsaveacceptance. User Visual ACCEPT=REJECTED_CURRENT_UI.**
**Only issued task TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209**؛parent208؛45min/stop35. Source-main0e2fa09e. هدفرفعهمینworkflowواقعی+UXدرfeatureموجود؛نهproofreplay.
Rootcauseهاهنوزاثباتنشده:timezoneانسانیممکنinvalidباشد؛newchipindexفقطbaselineاست،pickedlabelsاحتمالجامانده؛خطایserverفقطgenericنمایشمی‌یابد. تشخیصازPUTactual/geoواقعیقبلrepair.
مجاز:renamevisiblegroupبه«محدودهارسال»بدونdomainchange،compactreadableform/destination+rates،inlineactionablefieldvalidation،lawfultimezoneselect،یکtypeواقعی«عادی»ازAPIمجازدرdevفقطاگرcatalogempty،realApply+GET+reload. نهcatalogCRUDجدید/حدسroute/SQLbusinessseed.
مجوزdevکاربرمحفوظ؛existingdraft01a16eeb-70b3-7000-87c2-564c82835496ممکنusercontentجدیدداشتهباشد،overwrite/reset/reloaduseractiveeditsممنوع. backend5088/FE3000روشن،9migrationsalreadyapplied؛تکرارنشوند.
پسازنتیجه209review،فقطاگرrealnames/type/destination/fees/weeksave+readbackکارکردبرایretestصداکن. active209،نتیجهدرانتظار؛نهtaskبعدییاpermissionloop.199–207proofبازنشوند؛TB185paused/defaultOFFمحفوظ.

## مرجع قطعی چت جدید:208 پذیرفته؛ تست کاربر روی مسیر واقعی —2026-10-24

**Architect ACCEPT_LOCAL_EXISTING_DRAFT_READ_TRIAL_HANDOFF**؛source/evidence `f2fa49d5432d38663aa67ea057583c51bf0bb01e`؛[review208](../../architecture/evidence/TB-SHIPPING-ADMIN-LOCAL-STACK-REFRESH-AND-USER-TRIAL-208/ARCHITECT-REVIEW.md).
**Current phase=USER_TRIAL_LOCAL_LIVE_READ_READY. Active task=NONE؛ awaiting worker result=NONE؛ next task=NONE.** کاربر اکنون برای تست فراخوانده شود؛تسکاثباتی/تحلیلی تازه نساز.

### مسیر دقیق و وضعیت واقعی

http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view

Draftواقعی:`01a16eeb-70b3-7000-87c2-564c82835496`؛binding`post`/optionCode=null؛Store/store-alpha؛revision1هنگامhandoff؛groups[]؛بدونترجمه؛settings/weekunconfigured. نوع‌هاواقعاًخالی‌اند،نهخطایخواندن؛زبان‌هایconfiguredسه‌تاfa-IR/en-US/Russian. دادهکاربرپسازاینممکنتغییرکند؛revision1راresetنکن.
واقعاًAPI200+mount+Edit/CancelcleanبدونPUTمشاهدهشده؛**ذخیرهApplyرویاینdraftواقعیPENDING**. توضیحجهتتست:روی«ویرایش»بزند،بخش‌هایموردنظرراپرکند،ذخیرهوحفظپسازخواندندوبارهرابررسیکند؛پیشاپیشموفقیتwriteادعانشود.

### اقدام‌های انجام‌شده؛ دوباره اجرا نشوند

Runner planکل33moduleدقیقاً9pending؛applyموفق:Shipping7تا20261022195929،Fulfillment2تا20261022182208؛historyShipping13/Fulfillment12وobjectsوجوددارند. هیچmigration/historyeditنیست.
Hostقدیمی5088تعویض؛HostجدیدمشاهدهPID10800،`D:\temp\tb207-build\out\Tooba.Host.exe`؛Development؛5088؛contentrootهمانout؛ShippingOneLevelCatalogEnabled=true؛FE3000PID19108دست‌نخورده. launcherمحلی`D:\temp\tb208-host\run-host.cmd`،runtime/outputبرایکاربرنگه‌داشتهشده. PIDهاsnapshotاند،هنگامtroubleshootدوبارهverify.
Frontendfixفقطdecodecanonical`owner.ownerKind`بهinternal`owner.kind`+wiretestfixture؛adapter7/7workerreported. actualOwnerViewApplicationresponseاستنهContracts؛auth/DTOتغییرنکرده.
DBواقعاًتغییرکرد:9migration/schema/history/index/check+یکdraft+startupMassTransitSQLschema. «فقطdraftتغییرکرد»درworkerfieldدقیقنیست؛کلstartup/workerrowwritesشمرده‌نشده،zeroادعانشود. ایناثراتdevمجازبودند.

### مجوز/حدادعا/قدم بعد

مجوزصریحکاربر«DBproductionنیست،همهکارهایلازمdevانجامشود،توقفوتأییدتکراری‌نمی‌خواهم»معتبرومحفوظ؛برایهمیناقداماتدوبارهapprovalنگیر.
اولفقطبازخوردتستکاربر. اگرURLقطعشد:process/launcher/originراread-onlyverify؛نهrebuild/migrate/resetازابتدا. اگرایرادواقعیreportشد،repairمحدودهمانایرادباskillمربوطونتایجfocused؛190–205دوبارهبازنشوند.
Historical205syntheticserializedfixturesowner.kindناصحیحداشتند؛بعدfix208آن‌هارابدوناصلاحwireدوبارهاجرا/اثباتتازه معرفینکن. آرشیوhistoryحفظ؛proofloopنساز.
Production/fullshippingdelivery/quote/activation/wholemethodcreate/certification/userVisualACCEPTادعانشود. تحویلهمینexisting-drafteditorبرایusertrialاست؛backendintegrationreadاکنونreal،livewriteهنوزتستنشده. TB185paused/defaultOFF/PendingAllocationمحفوظ.
Architectdiff/harness/OwnerView/decoderراreviewکرد؛localhostWindowsرامستقلراننکرد. همهpointerهایپایینتاریخچه‌اند،نهتسکفعال.

---

## نتیجه اجرای208 — READY_FOR_USER_TRIAL روی استک توسعهٔ محلی — 2026-10-24

208 اجرا و به Bridge گزارش شد؛ Worker، base`822a8ea8`، فقط یک تسک. [evidence208](../../architecture/evidence/TB-SHIPPING-ADMIN-LOCAL-STACK-REFRESH-AND-USER-TRIAL-208/RESULT.md).
**مجوز کاربر ثبت شد و تکرار نمی‌شود:** دیتابیس توسعه است؛ migration+اثرات startup Host+حداکثر یک draft تست، روی همین هدف محلی مجاز است و برای همین اقدامات approval دوباره گرفته نمی‌شود.
**Migrationها:** plan تمام33descriptor؛ pending دقیقاً 9 (Shipping7+Fulfillment2)؛ apply با Runner فعلی (build ایزوله `D:\temp\tb208-runner\out`) exit0؛ اکنون Shipping13 و Fulfillment12 و جدول‌های authoring/type موجود. backup ابزاری موجود نبود و طبق تسک مانع نبود. هیچ migration/history دست‌کاری نشد.
**Runtime:** Host قدیمی PID24304 (bin\Debug، 22Oct) پس از verify با force متوقف شد (بستن graceful مؤثر نبود — deviation)؛ binary آمادهٔ207 با hash`9987f7a5…` بدون build دوباره اجرا شد: **PID10800**, `D:\temp\tb207-build\out`, Development, port5088, content root همان پوشه؛ startup بدون error (3 warning) و Language bootstrap skipped طبق ordering مستند. رستارت با `D:\temp\tb208-host\run-host.cmd`. FE`3000` (PID19108) دست‌نخورده.
**Draft واقعی تست:** `01a16eeb-70b3-7000-87c2-564c82835496`، binding`post`/`optionCode=null`، revision1، regionGroups خالی؛ فقط از POST مجاز ساخته شد و همان تنها نوشتن business این تسک است. types واقعاً خالی است و seed نشد.
**URL تست کاربر:** `http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view`
**نقص واقعی integration و repair محدود:** backend مالک را `owner.ownerKind` می‌دهد ولی decoder فرانت `owner.kind` می‌خواست → صفحه با GETهای200 خطای «خواندن پیش‌نویس ناموفق» می‌داد. repair در `editor-document-decode.ts` + fixture تست، بدون تغییر API؛ focused test 7/7. mount واقعی بدون هیچ Fetch-interception: General/زبان‌های واقعی/نوعUnconfigured/تنظیمات و هفتهٔ واقعاً پیکربندی‌نشده، ویرایش و خروج پاک، بدون PUT.
**محدودیت‌ها:** type catalog خالی است؛ write acceptance روی draft واقعی سنجیده نشد؛ هیچ quote/activation/whole-method/certification ادعا نمی‌شود.190–205 دست‌نخورده، بدون replay؛ TB185 paused/defaultOFF.

## تسک جاری208 — مجوز صریح ادامه روی دیتابیس توسعه — 2026-10-24

کاربر صریحاً اعلام کرد دیتابیس production نیست، تغییرات لازم انجام شود، توقف/تأییدهای تکراری نمی‌خواهد و فقط وقتی مسیر واقعی آماده شد برای تست صدایش کنیم. **این مجوز جایگزین holdهای approval قبلی207 است.**
**Only issued task: TB-SHIPPING-ADMIN-LOCAL-STACK-REFRESH-AND-USER-TRIAL-208**؛parent207؛40min/stop implementation30،هدف سریع تحویل واقعی،نهproofloop. issued≠claimed.
مجازرویهمانdevlocal127.0.0.1:5432/tooba_alpha/tenant-alpha/store-alpha: planکلRunner،اعمالmigrationهایchecked-inلازم(9شناخته‌شدهوpendingعادیاضافیهمانstackپسreview)،Host-onlyrefresh5088باbinaryآماده207وstartupseed/backfill/workers موجود،realAPI/mount،ساختحداکثر1draftتستازPOSTمجازواقعیفقطاگردraftوجودندارد. FE3000محفوظ؛موفقHostرابرایکاربرروشننگه‌دار.
backupسریعاگرابزارحاضر،عدمابزارbackupمانعاینdevنیست. هیچمجوزdrop/resetDB،حذفکارنامرتبط،تغییرauth/editionیاdeploymentخارجینداریم. Runnermodulefilterندارد؛targetراverifyوpendingکلراببین،ولیبرایصرفاًcount!=9تأییدمجددنگیر.
بعدنتیجه208reviewسریع: اگرrouteواقعیآمادهURL+تستکاربر؛اگرtechnicalblockerواقعیrepairمحدود،نهpermissionloop. runtime/schema/auth/draftهنوزتااجراثابتنیست؛تیکزودهنگامممنوع.190–205بسته،TB185paused/defaultOFFمحفوظ.
مجوزکاربردرچتجدیدهممعتبر؛برایهمیناقداماتdevدوبارهسؤالنشود.

## مرجع جاری:207 بررسی شد؛ منتظر تأیید تغییر schema محلی — 2026-10-24

207 build و preflight محدود پذیرفته؛ [review207](../../architecture/evidence/TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207/ARCHITECT-REVIEW.md) و [طرح اجرای دقیق](../../architecture/evidence/TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207/EXECUTION-PLAN.md).
Worker149faf7c/basea932fa29؛Host جدیددرD:\temp\tb207-build\out ساخته‌شده ولیrunنشده. Shipping7pending+Fulfillment2pending رویtooba_alpha گزارش‌شده؛همهmoduleهاpending-checkکاملنشده. draft/auth/live readinessUNKNOWN.
**Active worker task=NONE؛ next task NOT ISSUED؛ state AWAITING_EXPLICIT_LOCAL_SCHEMA_APPROVAL.**
تأییددرخواست‌شدهفقط9migrationنام‌بردهدرplanرویtenant-alpha/store-alpha محلی،مشروطبهpendingکلدقیقاًهمین9،backupقابل‌استفاده/identity/collisioncheck. هیچHostrestart/seed/backfill دراینمجوزنیست.
اصلاحArchitect:Runner --moduleندارد؛applyهمهdescriptorهاست،پسقبلapplyplanکللازم. ShippingConstraintParity پنجCHECKراreplaceمی‌کند؛create-onlyنیست. buildintermediatesخارجrepoاثباتنشده؛sourcebuilda932استنهresultcommit. oldexe restartفقطbinaryrollbackاستنهDBrollback؛liveenvUNKNOWN. هیچTask208یااجراییقبلتأییدصادرنشده.
پسازتأییدکاربر: یکtaskکوتاهschema-onlyباهمینشرط‌ها؛اگرpendingاضافیSTOPباidها. سپسruntime/startupeffectsجداگانهreview،بدونtaskاثباتیUI. کاربرعجلهدارد؛accepted190–205دوبارهبازنشود.ProductionPENDING،TB185paused/defaultOFFمحفوظ.
بخش‌هایپایینتاریخچه‌اند.

## تسک جاری207: ساخت Host جدید و پیش‌نیاز راه‌اندازی — 2026-10-24

206 برای **تشخیص مانع پذیرفته شد**؛ source26e2b31a،main394fdf040 (فقطresult-contract بعدی). [review206](../../architecture/evidence/TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206/ARCHITECT-REVIEW.md).
باینری Host5088 قدیمی است؛ routeهایauthoring404،FErewrite سالم. draft/auth/schema/live readiness هنوزUNKNOWN؛ devcontext200 مجوزshipping نیست.
Architect کدProgram را خواند: Development startup حتی با RunLegacyBootstraps=false مایگریشن و چندseed/backfill دارد. restart کور مجاز نیست.
**Only issued task: TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207**،parent206،20min/stop14: source-grounded startup effects + READ-ONLY migration history و outbox-index collision + ONE isolated currentHost build<=6min،طرح دقیقrefresh/rollback. هیچsharedrestart/DDL/DML/migration/seed در207؛binaryجدیدrunنشود. buildoutputمحلیبرایمرحلهrefreshنگه‌داری وhash/sourceثبت.
پس ازنتیجه207: review اثرواقعیstartup/pendingids و کوچک‌ترین اقدامرفع؛ نهtaskتحلیلیخودکار/تکرارUI. تانتایج207تسکدیگریصادرنشود. آماده تست‌بودنکاربرهنوزتأییدنشده؛accepted190–205محفوظ،ProductionPENDING،TB185paused/defaultOFFمحفوظ.
این بخش تمام pointerهای قدیمی206/NONE را supersede می‌کند؛ issued≠claimed.

## مرجع جاری: 207 preflight — build فعلی آماده، اما 7 مهاجرت Shipping اعمال نشده — 2026-10-24

**TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207** روی base `a932fa2915fed4827635d0cb8dbb0260baf35945` اجرا و به Bridge تحویل شد؛ [RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207/RESULT.md).
نتیجه: **PREFLIGHT_READY_PENDING_APPROVAL**. build ایزولهٔ فعلی Host از main موفق شد (0 error، 42 warning، ~45s؛ `D:\temp\tb207-build\out`، `Tooba.Host.dll FD7C37F4CC13E95F`)، bin/Debug و obj مشترک دست‌نخورده.
واقعیت schema با SQL **read-only** روی `tooba_alpha` (db/user/read-only تأیید شد): shipping 6 اعمال‌شده و **7 pending** (AuthoringDraftPersistence…AuthoringDraftTypeAssignment)، fulfillment 10 و **2 pending** (ShippingTypeCatalog، AddFulfillmentOutboxPendingIndex)، geography کامل. مهم‌ترین یافته: Shipping هیچ `IModuleSchemaMigrator` ندارد، پس restart Host آن 7 مهاجرت را **هرگز** اعمال نمی‌کند و صرفاً swap باینری، 404 امروز را به `42P01` تبدیل می‌کند.
جدول اثرهای startup کامل ثبت شد؛ `RunLegacyBootstraps=false` فقط seed قدیمی Catalog را خاموش می‌کند و مهاجرت/Content/Support/Wallet/one-level backfill و hosted workers (Outbox dispatcher، Order expiry، Payment reconciliation، Cart expiry، Authorization schema، Language bootstrap) را نه. **حالت startup بدون mutation وجود ندارد.**
برخورد ایندکس: `ix_outbox_messages_pending` در 29 schema حاضر است ولی در `fulfillment` نیست → collision=NO، ریسک fail-closed R10 با نام تکراری فعال نمی‌شود.
مسیر تأیید پیشنهادی: (1) اجرای Shipping-only با `Tooba.MigrationRunner` و راستی‌آزمایی 7 id، سپس (2) swap باینری جدید روی 5088؛ ترتیب 1→2 الزامی است. rollback = همان `bin\Debug\net8.0` فعلی. هیچ restart/migration/seed/DDL/DML انجام نشد؛ 3000/5088 دست‌نخورده. ProductionPENDING، Stop-State STOP_FOR_ARCHITECT، Automatic-Next-Task NONE.

## مرجع جاری: 206 اجرا شد — BLOCKED روی باینری قدیمی Host — 2026-10-24

**TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206** روی base `8fcd9df9e44f7bc283e0e6ccf0c6cd95d9b0d8c2` اجرا و به Bridge تحویل شد؛ [RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206/RESULT.md).
نتیجه: **BLOCKED_STALE_BACKEND_BINARY**. Host واقعی روی `5088` (PID24304، باینری ساخته‌شده 2026-10-22 06:41) و FE مشترک روی `3000` سالم‌اند، اما `/v1/admin/shipping-services/authoring-drafts` و `authoring-types` هر دو **404 route-not-found** می‌دهند (هم مستقیم، هم از rewrite واقعی `:3000`)، در حالی که main فعلی این مسیرها را در `ShippingEndpointModule.cs:33` ثبت می‌کند و منبع `ShippingAuthoringDraftEndpoints.cs` آخرین‌بار در `be639a473` (2026-10-23 02:15) تغییر کرده — یعنی باینری در حال اجرا قدیمی‌تر از مسیرهاست. مسیر قدیمی‌تر `/v1/admin/shipping-services` هنوز 401 می‌دهد (route در باینری هست، auth برقرار)، پس 404 نه auth است نه connection نه schema نه داده خالی نه proxy. موجودیت draft/binding تا rebuild **UNKNOWN** است، نه «0 draft».
همه بررسی‌ها read-only واقعی بودند؛ PUT/POST/PATCH/DELETE=0؛ هیچ mock/interception، هیچ migration/seed، هیچ build/restart، هیچ تغییر shared port/process.
کوچک‌ترین repair (خارج از bounds این تسک): rebuild+restart کنترل‌شده Host از main در پنجره مجاز، با تأیید صریح درباره migration در startup (وضعیت migration عمداً بررسی نشد)، سپس همان سه GET read-only و بعد trial mounted.
ProductionPENDING، live write PENDING، Stop-State STOP_FOR_ARCHITECT، Automatic-Next-Task NONE. تا review این نتیجه تسک دیگری صادر نشود.

## تسک جاری: 206 اتصال واقعی پیش‌نویس موجود — 2026-10-24

کاربر پس از پذیرش205-R3 صریحاً خواست قبل از تست خودش اتصال واقعی بررسی شود و سریع تحویل بگیرد.
**Only active/issued task: TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206**؛ parent205-R3؛ mode BOUNDED_REAL_ENVIRONMENT_EXISTING_DRAFT_READ_ONLY_HANDOFF.
20دقیقه hardbox، stop-new-work14؛ reuse installed tooling، بدون proof replay. تا نتیجه206 تسک دیگری صادر نشود.
هدف: تشخیص runtime/backend/auth واقعی، خواندن لیست و binding دقیق یک draft مجاز، GET document/types واقعی و mount route بدونmock؛ خروجی URL آماده تست read-only یا اولین blocker دقیق و کوچک‌ترین repair.
هیچ merchant write، ساخت draft، seed، migration، shared process restart، packageinstall، broadbuild/suite یا production code change مجاز این task نیست.
Historical FE3000/Host5088 فقط hint؛ readiness قبلی205 synthetic بود. اگر draft/runtime/auth/schema وجود ندارد، BLOCKED همان لایه با شاهد؛ نتیجه سبز جعل نشود.
تسک دانلودی به worker از pipeline معمول تحویل می‌شود؛ این ثبت به معنای claim/start worker نیست.
بعد از نتیجه: Architect review؛ green → exact URL برای user trial با live write PENDING؛ blocked → یک repair محدود مبتنی بر علت واقعی، نه بازکردن190–205.
Latest accepted stage205-R3 در750b4d73e1؛ accepted ledger زیر محفوظ. این بخش بر Active task=NONE قدیمی مقدم است.

## نقطه شروع قطعی چت جدید — 2026-10-24 — SHIPPING USER TRIAL

این بخش مرجع وضعیت جاری است؛ تمام «تسک فعال/منتظر نتیجه» در بخش‌های پایین تاریخچه‌اند. چت جدید نباید از روی آن‌ها کار جدید یا تکراری صادر کند.

### الان کجا هستیم

- Repository: `mrnikiemami-code/Tooba`؛ branch: `main`.
- آخرین پذیرش Architect: **TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R3**.
- Worker source/evidence: `d6fd1ba0c7e43fe3493f91b77a6bd5a6402c3e76`.
- پذیرش و تحویل ثبت‌شده در commit `750b4d73e1e3561d53ddd7b2e7170136e2933f5a`. این checkpoint در commit بعدی همین شاخه ثبت می‌شود؛ در بازیابی HEAD تازه را بخوان، SHA تاریخی را HEAD فرض نکن.
- **Active implementation task=NONE؛ Awaiting worker result=NONE؛ Automatic next task=NONE.**
- **Current phase=USER_TRIAL_EXISTING_DRAFT_AUTHORING**؛ این به معنای پایان کل برنامه Shipping یا تأیید production نیست.
- خواسته فعلی کاربر: ریکاوری کامل برای چت جدید، سرعت تحویل و جلوگیری از دوباره‌کاری/حلقه اثبات.

### دفتر پذیرش؛ دوباره اجرا نشود

| کار | آخرین repair پذیرفته‌شده | دامنه بسته‌شده |
| --- | --- | --- |
| 190–194 | reviews تاریخی مربوط | owner-local draft، region/rate، ترجمه، هفته و تنظیمات مستقل |
| 195/198 | تحلیل با اصلاحات Architect | مالکیت نوع و قرارداد ویرایشگر اتمیک |
| 196 | 196-R13-R1 | کاتالوگ نوع Fulfillment، API/Contracts، schema/storage/race |
| 197 | 197-R1 | انتساب نوع روی root پیش‌نویس Shipping، failure/retry و no post-commit lookup |
| 199 | 199-R1 | document اتمیک موجود، preflight کامل، یک revision و transaction/rollback |
| 200 | 200-R2 | adapter دقیق decimal/time، baseline preservation، epoch و sticky unknown |
| 201 | 201-R2 | destination hydration از Geography.Contracts؛ repro قبلی fake نامعتبر بود و پس گرفته شد |
| 202 | 202-R1 | General واقعی، raw dirty، discard و lifecycle |
| 203 | 203-R1 | نرخ‌ها و هفته واحد؛ per-bound time و reset همه فرم‌ها |
| 204 | 204-R2 | گروه/مقصد، coordinator درخواست، evidence نسل جاری و active-path Add |
| 205-R2 | bounded happy-path review | mounted synthetic composite save، duplicate gate، response adoption، FA/EN captures |
| 205-R3 | bounded work-protection review | invalid+discard،409 حفظ کار،transportUnknown+reconcile صریح بدون replay |

مرجع آخرین پذیرش:
[205-R3 ARCHITECT-REVIEW](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R3/ARCHITECT-REVIEW.md).
مرجع happy-path:
[205-R2 ARCHITECT-REVIEW](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2/ARCHITECT-REVIEW.md).
نیازی به بازخوانی همه نتایج از196 یا اجرای دوباره suiteها برای بازیابی نیست.

### تحویل موجود و مسیر ورود

ویرایش **پیش‌نویس مجاز موجود** شامل متن چندزبانه/نوع/تنظیمات مستقل، گروه‌های مرتب/مقصد/عنوان، ردیف‌های هزینه و یک هفته روی root، با ONE PUT و expectedRevision مشترک آماده تست کاربر است.
ورود: پنل روش‌های ارسال → نمایش روش موجود → تب عمومی → لینک پیش‌نویس، فقط اگر binding دقیق serviceCode+nullable optionCode به یک پیش‌نویس مجاز برسد.
مسیر واقعی: `/admin/shipping-services/authoring/[draftId]?mode=view|edit`.
شناسه‌های synthetic نتایج205 برای داده واقعی قابل استفاده نیستند. صفر پیش‌نویس = حالت no-draft صادقانه؛ هیچ POST/create خودکار وجود ندارد.
Backend editor API: `GET|PUT /v1/admin/shipping-services/authoring-drafts/{draftId}/editor-document`؛ seller نیز composition مجاز خود را دارد.

### دقیقاً قدم بعد چیست

1. ابتدا HEAD فعلی main، همین checkpoint، AGENTS و دو review بالا را بخوان؛ اگر HEAD جدید است فقط delta جدید را بررسی کن.
2. هدف بعدی **تست عملی کاربر و تأیید اتصال محیط واقعی برای همین پیش‌نویس موجود** است؛ تکمیل UI را با آماده‌بودن runtime و داده واقعی یکی نگیر. دسترسی/سلامت Host، FE، schema جاری، language/type/geography registry و وجود draft مجاز در محیط واقعی در این مرحله هنوز تأیید نشده‌اند.
3. اگر کاربر مشکل ورود/لود گزارش کرد یا خواست ادامه اتصال اجرا شود: فقط همان مسیر واقعی و اولین blocker را بررسی کن؛ ابتدا read-only و source-grounded. نبود draft را با ساخت یا seed خودسرانه دور نزن؛ محیط/شناسه/مالک را از منبع متصل به دست آور، حدس نزن. اگر دسترسی کافی نبود فقط اطلاعات واقعاً مفقود را بپرس.
4. اگر نیاز به کار Worker بود، یک فایل دانلودی .task.md کوتاه با Task-ID/base/scope/timebox/STOP و معیار اتمام مشخص صادر کن؛ بعد از نتیجه review. این checkpoint خودش مجوز deployment/merchant write یا یک task صادرشده نیست.
5. بعد از تست کاربر، بازخورد واقعی را به repair محدود تبدیل کن؛ full geography matrix، visual replay، confidence loop یا suite سنگین تازه صرفاً برای پرکردن شواهد نساز. اگر blocker ندارد، برای این slice کار مصنوعی اضافه نکن.
6. ساخت روش کامل، activation، cutoff/carrier/funding و runtime quote مراحل مستقلِ هنوز تحویل‌نشده‌اند؛ بدون تعیین دامنه واردشان نشو. برنامه کلی به اشتباه COMPLETE علامت نخورد.

### حد ادعا و شواهد

همه /v1 مرورگر205 synthetic/intercepted و fail-closed بودند؛ source واقعی route/component mounted شد، نه merchant/backend integration واقعی. Production-Functional-Acceptance=PENDING، live registry/user Visual ACCEPT/release/deployment/whole-module certification اثبات نشده.
R2: 1PUT/rev7→8/new IDs/no post-saveGET؛ 6FA/ENdesktop/mobile captures فقط REVIEW_EVIDENCE_ONLY. Architect به‌علت محدودیت UTF8 connector پیکسلPNG را مستقل ندید.
R3: سه کنترل طبق DOM/count worker و harness بررسی و پذیرفته شد؛ Architect مرورگرWindows را دوباره اجرا نکرد. عبارت reuse Visual ACCEPT در RESULT ناصحیح است. timing انحراف R2 و stop-minute10 درR3 حفظ شده؛ برای آن‌ها proof task مجدد صادر نشود.
ماتریس کامل geography/group move-remove در مرورگر تأیید نشده؛ این گپ باعث بازکردن دوباره199–204 نیست.

### محیط و حفاظت‌های ادامه

205 runtime موقت FE3517 +ChromeCDP9555 پاک شده و طبق worker پورت‌ها آزادند؛ shared3000 دست‌نخورده بود. آماده‌بودن فعلی پورت3000/Host5088 را از تاریخچه فرض نکن.
Runtime browser test قبلی فقط frontend copy، installed deps/junction، `TOOBA_HOST_ORIGIN=http://127.0.0.1:1` و همه/v1 interception بود؛ برای live trial همین mock را واقعی معرفی نکن.
sharedDB/data/flag تغییر نکرده؛ TB185 paused و default-OFF/PendingAllocation محفوظ.
Bridge فقط transport؛ هیچ dummy/probe/result ساختگی و هیچ اجرای تاریخی post-result.js. یک task هم‌زمان، بدون reset/stash/rebase/force-push یا دست‌زدن به کار نامرتبط.
کاربر عجله دارد: گزارش کوتاه فارسی، taskهای کوتاه و محدود، بدون توقف غیرضروری یا تست‌سازی بی‌پایان. هیچ تیک تکمیل قبل از پایان و review کار واقعی زده نشود.

---

## مرجع جاری: تحویل ویرایش پیش‌نویس موجود برای تست کاربر — 2026-10-24

**205-R3 پذیرفته شد: ACCEPT_BOUNDED_MOUNTED_WORK_PROTECTION** در source/evidence `d6fd1ba0c7e43fe3493f91b77a6bd5a6402c3e76`؛ [بازبینی Architect](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R3/ARCHITECT-REVIEW.md). diff مستقل فقط docs/harness؛ production بدون تغییر. سه کنترل mounted طبق DOM/count ثبت‌شده: invalid/رد و تأیید لغو با بازگرداندن کل General+rates+week؛409 حفظ کار بدون retry؛transportUnknown با latch و reconcile صریح دقیقاً1GET و بدون replay. Architect harness/RESULT را خواند، مرورگر Windows را دوباره اجرا نکرد. stop-minute10 حدود0.6دقیقه رد شد؛ انحراف فرایندی ثبت، نه timingPASS. chronology دقیق پایان از commitها اثبات نشده است.

**مرحله فعلی USER_TRIAL؛ تسک فعال/بعدی NONE.** مسیر موفق ذخیره ترکیبی و duplicate gate در205-R2 و حفاظت کار در205-R3 برای همین دامنه پذیرفته‌اند؛ 199–204 بسته‌اند. تسک اثباتی یا full geography matrix تازه صادر نشود. گام بعد فقط تست/بازخورد کاربر روی دامنه موجود؛ در چت جدید از همین بخش و review شروع شود.

تحویل محدود: ویرایش پیش‌نویس مجاز موجود شامل متن چندزبانه/نوع/تنظیمات مستقل، گروه و مقصد و عنوان، ردیف هزینه و هفته واحد، ONE atomic Apply. ورود از لینک پیش‌نویس در نمایش روش موجود با binding دقیق؛ مسیر واقعی `/admin/shipping-services/authoring/[draftId]`. نبود پیش‌نویس مجاز = حالت صادقانه بدون ساخت خودکار.

**Production-Functional-Acceptance=PENDING؛ userVisual/release/live integration تأیید نشده.** مرورگر با همه/v1 synthetic بود؛ تست با داده/registry واقعی و deployment انجام نشده. ساخت روش کامل، activation/readiness، cutoff/carrier/funding و runtime quote جزو تحویل این slice نیست. VisualR2 فقط REVIEW_EVIDENCE_ONLY؛ عبارت «reuse Visual ACCEPT» در RESULT-R3 پذیرفته نیست. sharedDB/data/flag تغییر نکرد؛ TB185 paused و default-OFF/PendingAllocation محفوظ. بخش‌های پایین تاریخچه‌اند و اختیار تسک تازه نیستند.

## مرجع پیشین: 205-R3 اجرا شد و در انتظار review Architect — 2026-10-24

**TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R3** روی base `61043bb5d699a55145ec18fb8d03ce3bd82e2f28` اجرا و به Bridge تحویل شد؛ نتیجه در [RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R3/RESULT.md). سه کنترل mounted روی runtime/harness موجود: (1) invalid name + Apply disabled/dirty + reject نگه‌داشتن کار + confirm بازگرداندن name/rate/week/groups و ورود مجدد بدون residue، 0 PUT؛ (2) canonical 409 `shipping.authoring.draft_revision_conflict` با حفظ name/dirty/edit، put=1 بدون retry و بدون post-save GET؛ (3) transportUnknown با reconcile صریح، latch حتی با ویرایش محلی بعدی، سپس GET delta +1 و پاک‌شدن uncertainty با حفظ کار و بدون replay. یعنی هر سه کنترل PASS; نه full matrix/visual/real-data. یک harness restart مجاز مصرف شد؛ کنترل3 بعد از stop-minute10 اجرا شد و در RESULT به‌عنوان timedeviation ثبت است. ProductionPENDING، shared/data/flag NONE، Stop-State STOP_FOR_ARCHITECT. تا review این نتیجه تسک دیگری صادر نشود.

## مرجع پیشین: 205-R2 مسیرموفق پذیرفته / فقط205-R3 کنترل‌کارکاربر — 2026-10-24

205-R2 در `4dddb7265e98a5ec012d11be0a7d2b7b982f982e` برای **mounted synthetic happy-path ACCEPT**؛[review](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2/ARCHITECT-REVIEW.md). main `c7cb141c3d1e091d66d8b7e2bed3cb54991eeb75` فقطpostresultبعدی.1PUT/rev7->8/newgroup-rowids/noGET/duplicategate گزارشbrowserworker coherentباharness+fixtureprevalidated. assertساختاریJSON+rawtokens است،نهbyte-for-byte؛requestareasWRITEtupleنهREAD.

6screenshotFAENdesktop/mobileوجوددارد،ولیconnectorPNGbinaryreadراUTF8-onlyردکرد؛Architectpixelsندیده،VisualREVIEW_EVIDENCE_ONLYنهowneraccept.26min>25 ثبتtimedeviation،نهretestloop. ProductionPENDING؛نهrealDBwrite/deploy/quote.

تنها **TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R3**،parent205-R2،15دقیقه/stop10: mounted invalid+reject/confirmwhole-discard،canonical409preserve،transportUnknown+explicitGETreconcile/noReplay. reuseharness/runtime،setup<=3؛نهhappy/visualreplay/wholegeographymatrix/newfeature. بعدreview اینکنترل‌ها handoffexistingdraftبرایusertrialباremaininglimits،نهتسک‌سازیبی‌پایان. تا نتیجه205-R3 تسکدیگری صادرنشود.
199-204بسته،TB185paused/defaultOFF/PendingAllocationمحفوظ. اینبخش مقدم برpointerتاریخی.

## مرجع جاری: 205-R1 هنوزجزئی / فقط205-R2 باfixtureآماده — 2026-10-24

205-R1 در `e827c5d60aabe52cc243369df6fa4a73e5f2be86` فقطشواهدتعاملجزئیثبتشد،**205بسته‌نیست**؛[review](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1/ARCHITECT-REVIEW.md). main `c33103f2fa7be66194389dad2a95b086cd5617fc` فقطpostresultبعدی. workerreport: FAnames+ENcontentedits حفظ،1PUT/rev7/decimalexact/noGET؛responsemockinvalid باعثunknownدرست‌شدنهsuccessfuladoption.5FAscreens؛ENinterface/B/C/E/doubleclickNOTRUN؛productioncodeunchanged.

علتfixture: WRITEregionGroups (areatuple/titlepatch/noordercurrencynewid) مستقیمبهREADdocumentinjectشد. Architect اینبار **205-R2/scripts/save-scenario.mjs** آماده و باproductiondecoder/wireواقعی preflightاجراکرد: initial7/saved8هردوdecode؛newgroup/newrowreadids/factsconsistent؛negativeWRITEasREADردشد؛rawmoney/timeحفظ. اینpreflightاستنهbrowserPASS.

تنها **TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2**،parent205-R1،25دقیقه/stop18: reuseCDP+fixtureآماده،exactcompositeflow namesFAEN+MondayEND18:15+Country2newgroup/faعنوان+row0.50/0.00/day1..2؛ONEPUT/rev8/newidsadoption+pendingdoubleclick و4FAENdesktop/mobilecaptures. نهgenericmockengine/productionfix/setuploop. باقیA-E اگراجرانشدصریحOPEN،نهfullaccept.
تا نتیجه205-R2 تسکدیگری صادر نشود.199-204بسته‌نهreplay؛ProductionPENDING؛TB185paused/defaultOFF/PendingAllocationمحفوظ. اینبخش برpointerتاریخی مقدم است.

## مرجع جاری: 205 فقطruntimeجزئی / فقط205-R1 تعامل و visual — 2026-10-24

205 در `926c3a6c397d0d0bd83d9bd494c6ef0ecb08a7d4` برایruntimeevidence ثبتشد، **PARTIAL؛ A-E/visualپذیرفته‌نیست**؛[review](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205/ARCHITECT-REVIEW.md). main `fb4f63e33957a9e29dbf3fc9cd2f15a8ed11ae03` فقطpostresultartifactبعدی. workerreport: Nexttaskowned3517+Chrome9555،mountواقعی،0PUT،1FA screenshot؛A-EونمایFAEN/mobileاجرایشده‌نیست. منابعproductionتغییرنکرده.

Architect fixturesاجراکرد: nulladminparent متن"null" است؛group1country+descendants overlapping است؛cityfilteredfixture ثابتناصحیح؛PUTfixtureفقطrevisionراreplaceمی‌کندنهedits/newids. authoring-types متعلقfeature است وبنرentryFailed ازهمینfailureمی‌آید،نهصرفاًshell. reviewدقیق،نهproductdefectازmockناقص.

تنها **TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1**،parent205،30دقیقه/stop22؛reusecommittedharness/knownstartup؛fixtureهایقانونی+typelookup/shellwhitelist grounded وfailclosed؛interactionواقعی+4FAENdesktop/mobilecaptures. نهproductionfix/Nodeproofreplay/newframework یاsetuploop. setup<=5min؛اگرنشدexactBLOCKED. sharedapps/DBبدونتغییر؛syntheticinterceptedPUTفقط؛no merchantwrite.
تا نتیجه205-R1 تسکدیگری صادرنشود.199/200/201/202/203/204بسته‌نهreplay؛ProductionPENDING؛TB185paused/defaultOFF/PendingAllocationمحفوظ. اینبخش مقدم برpointerتاریخی.

## 205-R2 اجرا شد: PASS محدود — منتظر بازبینی Architect، بدون پذیرش — 2026-10-24

**TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2** از base `0c8f2803462442b8eda8aa51823420f196d7519f`
اجرا و به Bridge گزارش شد؛ **Architect-accepted نیست**. نتیجه:
[docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2/RESULT.md](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2/RESULT.md).
سناریوی آمادهٔ Architect بدون تغییر استفاده شد (یک preflight PASS). روی مسیر واقعی: نام FA/EN ویرایش و پس از
switch حفظ شد، فقط پایان دوشنبه→18:15 با شروع دست‌نخورده، یک گروه جدید با عنوان «گروه آزمایشی» و کشور آلمان
و ردیف 0.50/0.00/1..2، سپس **Apply**: PUT نگه‌داشته‌شده با `assertScenarioRequest` **PASS** (expectedRevision7،
هر دو گروه/ردیف/ارز حفظ‌شده، توکن‌های خام `0.50`/`1850000.00`، هفتهٔ کامل با start `09:00:00.500`)، کلیک دوم
روی کنترل `در حال ذخیره…` درخواست دوم نساخت (**putCount=1**)، سپس پاسخ آماده → view با revision8، هر سه گروه و
هفتهٔ adopted، و **بدون GET بعد از ذخیره**. چهار+دو شات با کوکی `tooba_locale` و reload واقعی: FA/RTL و EN/LTR
(‏`lang/dir` تأییدشده) در 1440×900 و 390×844. گپ‌های باقی‌مانده صریح‌اند: blank-name/discard، 409/transport/
reconcile، ماتریس کامل geography و move/remove اجرا نشد؛ A–E کامل ادعا نمی‌شود. Production/DB/flag NONE؛
3517/9555 آزاد و `:3000` دست‌نخورده.

## 205-R1 اجرا شد: PARTIAL — منتظر بازبینی Architect، بدون پذیرش — 2026-10-24

**TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1** از base `be286fcb788aee83c3fd5733221065bc470d1e15`
اجرا و به Bridge گزارش شد؛ **Architect-accepted نیست**. نتیجه:
[docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1/RESULT.md](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1/RESULT.md).
شش نقص harness/آرایهٔ 205 رفع شد (null-admin parent به null واقعی، انتخاب‌های terminal ناسازگار حذف شد،
city-list بر اساس query تفکیک شد، پاسخ PUT سند کامل آمادهٔ همان سناریو شد، authoring-types با پوشش واقعی
`{types:[...]}`، خطای CDP دیگر success تلقی نمی‌شود). **اصلاح 205 تأیید شد**: بنر «بررسی پیش‌نویس ناموفق
بود» از read نوع‌های feature می‌آمد و با envelope واقعی ناپدید شد (banner:false)؛ نه نقص محصول.
تعامل واقعی: ویرایش نام در هر دو زبان محتوا با حفظ مقدار پس از switch، dirty و فعال‌شدن Apply؛ سپس
**دقیقاً یک PUT** با `expectedRevision=7`، هر دو گروه/ردیف/شناسه حفظ‌شده و توکن‌های عددی `0.50`/`1850000.00`،
بدون GET بعد از ذخیره. adoption نهایی اثبات **نشد** چون پاسخ آمادهٔ من با `destinationFacts` هم‌خوان نبود؛
UI درست fail-closed شد (outcome-unknown، بدون retry خودکار، reconcile صریح). هفتهٔ بدون تغییر در body ارسال
نمی‌شود. group/geography، ویرایش هفته، 409/transport و اسکرین‌شات EN/LTR اجرا نشد. پنج شات FA/دسکتاپ+موبایل
شاهد مرور است، نه Visual ACCEPT. Production/DB/flag NONE؛ پورت‌های 3517/9555 آزاد و `:3000` دست‌نخورده.

## 205 اجرا شد: PARTIAL — منتظر بازبینی Architect، بدون پذیرش — 2026-10-24

**TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205** از base `b7f7ead6df5129a9b0a62ee201592c4a2888f1ae`
اجرا و به Bridge گزارش شد؛ **Architect-accepted نیست**. نتیجه:
[docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205/RESULT.md](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205/RESULT.md).
وضعیت **PARTIAL** صادقانه: runtime تک‌وظیفه‌ای frontend-only روی `127.0.0.1:3517` (کپی task-owned با
junction روی node_modules، `.env.local` جایگزین‌شده با `TOOBA_HOST_ORIGIN=http://127.0.0.1:1`، بدون install)
و Chrome نصب‌شده headless با CDP آماده شد؛ مسیر واقعی `/admin/shipping-services/authoring/<guid>?mode=edit`
mount و رندر شد و همهٔ `/v1` با `Fetch` روی whitelist synthetic پاسخ گرفت و بقیه محلی fail شد
(editor GET=2، countries=4، PUT=0، blocked=6). سناریوهای A–E و بازبینی بصری FA/EN دسکتاپ/موبایل
اجرا **نشدند**: کل بودجهٔ ۳۰ دقیقه صرف bring-up و یک نقص BOM در harness شد. بنر
«بررسی پیش‌نویس ناموفق بود» به‌احتمال زیاد واکنش fail-closed به سه read shell مسدودشده است و هیچ
نقص محصولی ادعا نمی‌شود. Production/DB/flag NONE؛ tsc-as-visual ممنوع؛ هیچ فایل production تغییر نکرد.
پورت 3517/9555 آزاد و dev سرور قبلی `:3000` دست‌نخورده. تا نتیجهٔ 205 تسک دیگری صادر نشود.

## مرجع جاری: 204 ازطریقR2 پذیرفته / فقط205 مرورگر — 2026-10-24

204-R2 در `ec957367fadd94ceb14045e1b013d2ec308bade2` برایgroup/destination **ACCEPT**؛ [review](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2/ARCHITECT-REVIEW.md). mainپسازکد `eb93df3be7000fe1e5bff3a51c767a1ac335b753` فقطpost-resultartifact دارد. freshparentgeneration/currentactiveeligibility/strictdecoder بسته. Architect دوreproقدیمی را مستقلNodePASS اجراکرد؛8new/29focused/tsc/7guards گزارشworker،نهrerun. beginStage settledfix productionبوده ثبتشد. mounted/browser/visual/liveهنوزNOTRUN.

تنها **TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205**،parent204-R2،30دقیقه/stop22؛صفحهٔواقعی `/admin/shipping-services/authoring/[draftId]`،FA/EN desktop/mobile،تعاملواقعیmounted،syntheticAPIinterception برایGET/PUT،بدونهیچmerchantwrite یاbackend/sharedservicechange. runtimeinventoryمحدود؛اگرموجودنبودفقطfrontend/browser task-ownedisolated وinstalleddependencies مجاز،setupحداکثر5دقیقه،نهinstall/restart/shared.next. همه /v1fixturefailclosed؛noauthbypass/login/credentials.

هدف205 ارزیابیactualReactflow وvisual دربرابر186-R2مرجع،نهتستخالصدوباره. اگرsetupنشدمتوقفباexactBLOCKEDنهوقت‌پرکردن/تسکتازه؛نقصپیداشدreportنهunboundedfix. تا نتیجه205 تسکدیگری صادرنشود. ProductionPENDING؛whole-methodcreate/status/cutoff/carrier/funding/quote همچنانخارجscope؛TB185paused/defaultOFF/PendingAllocationمحفوظ.199/200/201/202/203/204 بسته،نهproofreplay. اینبخشبرpointerتاریخی مقدم است.

## مرجع جاری: 204-R1 دو ایراد باقی / فقط204-R2 — 2026-10-24

204-R1 در `0b1f9abcbad8c1a5fc244ef5354413510dcf1cd8` reviewشد؛ [review](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1/ARCHITECT-REVIEW.md). mainفعلی `909e43ad26298a253f6e9cb41712bc76571b10d0` فقطpost-resultartifactبعدی دارد،productionیکسان. **REPAIR_REQUIRED؛ 204 بسته نیست.** siblingepoch/clear/terminalpath/label fixes حفظ.

دوrepro مستقل Architect رویماژول‌بدونتغییر: Aareas موفق سپسB سپسA، پیشfreshAareas شهرAدوبارهخوانده‌می‌شود چونverifiedAreasقدیمی reuseمی‌شود؛ activecity باparentinactiveadmin وstagedadminnull درchoiceChainActive true. Addبایدcurrentverifiedpath/activity را ببیندنهفقطchoiceflags؛decoderهنوزnullparentقبلfilter قبولمی‌کندوabsentknownsetfailopenاست. report12+9/68root/tsc/7guards worker،نهrerun؛Visual/mounted/liveNOTRUN.

تنها **TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2**،parent204-R1،20دقیقه/stop14: freshcontextparentprerequisite،countrywideactualactiveparent،currentgenerationAddguard+smallstrictdecoderpredicate؛no newfeature/backend/schema/broadtest/proofloop. تا نتیجه204-R2 تسکدیگری صادر نشود؛سپسreviewوvisual/livevalidationمحدود.199/200/201/202/203بسته‌نهreplay؛ProductionPENDING؛TB185paused/defaultOFF/PendingAllocationمحفوظ. اینبخش برpointerقدیمی مقدم است.

## مرجع جاری: 204-R2 اجرا شد — منتظر بازبینی، بدون پذیرش — 2026-10-24

**TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2** (parent204-R1) از base `65d1842c5ab889a10815b7d03c29cf1713fb4aba`
اجرا و به Bridge گزارش شد؛ **Architect-accepted نیست**. نتیجه:
[docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2/RESULT.md](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2/RESULT.md).
`verifiedAreas` map حذف شد؛ evidence اکنون یک رکورد نسل‌محور است که با شروع هر areas جدید (حتی همان
`language|country`) پاک می‌شود و city کشورگرد فقط وقتی پذیرفته می‌شود که key آن با areas جاری برابر باشد؛
state یک `settled` صریح گرفت (موفقیت خالی هم settled است، begin/hold/reset/failure نه). Add با helper واحد
`destinationAddEligibility(choice, currentEvidence)` گیت می‌شود که canAdd و handler (از `coordinator.getState()`)
هر دو از آن استفاده می‌کنند: membership/فعال‌بودن از ردیف‌های جاری، parent واقعی city کشورگرد باید ردیف
active جاری باشد، null-admin روی read تأییدشده lawful، service فقط از city تأییدشده، و Country با خطای
فرزند نامرتبط بلاک نمی‌شود. adapter نیز filter صریح را اول چک می‌کند و نبودِ known set را fail-closed
می‌داند (مجموعه خالی تأییدشده null-admin را مجاز نگه می‌دارد).
8 تست جدید + 12 coordinator + 9 group fact = 29/29، typecheck و 7 guard سبز؛ root68/nested/source-size
عمداً اجرا نشد؛ mounted/browser/Visual NOTRUN. production/visual/live PENDING؛ فقط پس از review204-R2.


**TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1** (parent204) از base `5eaf479417a1d771f6c428f242e901e35b2d76f3`
اجرا و به Bridge گزارش شد؛ **Architect-accepted نیست** و گام بعدی مجاز نیست. نتیجه:
[docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1/RESULT.md](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1/RESULT.md).
picker اکنون یک coordinator مشترک دارد (`ui/destination-read-coordinator.ts`): token و branch-key
مستقل برای هر stage، پس پاسخ درست areas با شروع cities باطل نمی‌شود؛ cities کشورگرد فقط بعد از
موفقیت areas همان `language|country` خوانده می‌شود و مجموعهٔ تأییدشده — از جمله مجموعهٔ خالیِ واقعی،
fail-closed — به decoder داده می‌شود؛ شکست prerequisite شهرها را untrusted و بدون read نگه می‌دارد.
`choiceAncestry` برای terminal نوع AdministrativeArea از ردیف خودش استفاده می‌کند (نه parent شهر)،
`choiceChainActive` زنجیرهٔ active و coherence را در Add دوباره اعمال می‌کند، row غیرفعال نمایشاً
disabled است، clear-area/city/service واقعاً cascade می‌کند و label خالی «نام در دسترس نیست» می‌شود
(هرگز code/GUID). گروه/عنوان/rates/week/General و ONE PUT و discard دست‌نخورده.
12 تست جدید coordinator/chain + 204 facts با case اجرایی out-of-order (جای regex قدیمی) = 68/68 feature-root،
typecheck و 7 guard سبز؛ nested adapter/session و source-size عمداً اجرا نشد؛ mounted/browser/Visual NOTRUN.
production/visual/live همچنان PENDING؛ فقط پس از review204-R1.

## مرجع جاری: 204 نیازمند اصلاح / فقط204-R1 — 2026-10-23 (تاریخی، با R1 جایگزین شد)

204 در `790c2583c12953986666b493d30122aeca603ce4` بررسیشد؛ [review](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204/ARCHITECT-REVIEW.md). **REPAIR_REQUIRED؛ تیک پایان نخورد.** group/title/rates/ONEintent حفظ؛picker اشکال دارد: globalepoch درخواستareas را باcities باطل می‌کند؛cities ازareas قدیمی/undefined والد را fail-open می‌پذیرد؛isActive درAdd guardندارد؛برگشتlevelAdminازcity ancestryغلط؛clearplaceholderبی‌اثر وlabelfallbackcodeمخالفگزارش.

Architect دوchoicecase را مستقلNodeاجراکرد: inactiveCountry ancestry معتبرمی‌دهد؛adminپسازnulladmincity terminalداردولیancestrynull. sourceeffects raceبررسیشد،نهmountedReact.8focused/55root/tsc/7guardsگزارشworker؛55glob روت،نهnestedadapter/session؛sourceRegexاثباتraceنیست. مسیر واقعی `/admin/shipping-services/authoring/[draftId]` است،نهdrafts/{id}. Visual/mounted/liveNOTRUN.

تنها **TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1**،parent204،30دقیقه/stop22: pickercoordinatorواقعی باdeferredproof،cityparentverificationبعدareassuccess وemptyfailclosed،activechainguard،terminalexactpath،clearcascade/stalelist وunavailablelabel. noBackend/schema/route/newfeature/proofloop. تا نتیجه204-R1 تسکدیگری صادر نشود؛بعدreview visual/livevalidationمحدود.199/200/201/202/203بسته‌نهreplay؛ProductionPENDING؛TB185paused/defaultOFF/PendingAllocationمحفوظ. اینبخش برpointerهایقدیمی مقدم است.

## pointer: 204 اجرا شد — منتظر بازبینی، بدون پذیرش — 2026-10-23

**TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204** (parent203-R1) از base `06466c7546aa2801690f888015583db958866e4e`
اجرا و به Bridge گزارش شد؛ **Architect-accepted نیست**. نتیجه:
[docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204/RESULT.md](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204/RESULT.md).
collectionگروههمانdraft (add/remove/reorder/titlepatch فعال + chipمقصد) + pickerواقعیGeography با strictfeatureadapter روی همان routeهای موجود
+ rates203 داخل گروه‌های جدید + week ریشه‌ای، همه در همان ONE composed intent/PUT. newgroups clientkey با groupId:null؛ currencyserverowned؛
titlepatch فقط pair فعال تغییرکرده و null صریح؛ inactive stored echo نمی‌شود. resolver = 201facts + pickهای تأییدشده همان session؛
blocked فقط روی tree پیشنهادی.8 تست جدید + 55 تست feature + tsc + 7 guard سبز؛ mounted/browser/Visual NOTRUN؛ source-size sweep
عمداً اجرا نشد. production/visual/live هنوز PENDING؛ فقط پس از review204.

## مرجع جاری: 203 ازطریقR1 پذیرفته / فقط204 گروه و مقصد — 2026-10-23

203-R1 در `bf7cb78e471010be540e3613d489b284d5b02140` برای نرخگروه‌موجود/هفته **ACCEPT**؛ [review](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1/ARCHITECT-REVIEW.md). per-boundprecision/whole-documentdiscard/pendingtimezone dirty/fullconditiondisplay بسته. Architect weekcasesمستقلNodePASS؛9+7/tsc/7guards گزارشworker،نهrerun؛mounted/browser/VisualNOTRUN. source-size4oldfail untouched؛productionPENDING.

تنها **TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204**،parent203-R1،45دقیقه/stop35: collectionگروههمانdraft add/remove/reorder+titlepatch فعال + مقصدواقعیGeography،همانONEeditorPUT؛rates/week/General حفظ. ExistingroutesGeo countries/admin/cities/serviceareas باlanguageIdازregistry واقعیموجودند؛backendendpoint sourceتأییدشد. app-ownedlegacy picker فقطsource参考،نهfeature->appimport؛sharedauthhelpersوstrictfeatureadapter،no newbackendseam.

مقصدانتخابیcanonicalparents ازواقعیoptionparent+requestcontext؛nulladmincity/serviceareaحفظ؛parentchange پاسخقدیمیignore؛stored201facts retained؛duplicate/overlapcanonicalserverauthority،نهguess/worldscan. newgroupsclientkeys/noinventedGUID؛currencyserverowned؛titlelanguageDBactivepatchonly؛reset/unknown/reconcileandintentmergeهمهcapabilities. تا نتیجه204 تسکدیگری صادر نشود.

پس از review204،visual/livevalidationمحدود با محیط مجاز جدا؛whole-methodcreate/status/cutoff/carrier/funding/quote هنوز خارجscope و کامل‌اعلام‌نشوند. 199/200/201/202/203بسته،نهproofreplay؛TB185paused/defaultOFF/PendingAllocationمحفوظ. این بخش برactivepointerتاریخی مقدم است.

## مرجع جاری: 203 نیازمند اصلاح / فقط203-R1 — 2026-10-23

203 در `ceeaa609b0c1875a992cb65d115f00cba2a243ff` reviewشد؛ [review](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203/ARCHITECT-REVIEW.md). **REPAIR_REQUIRED؛ پایان تیک نخورد.** rate/weekUI و composedONEintent حفظ؛7new+48existing/tsc/7guards گزارشworker،source-size4oldfail بیرونscope؛Visual/mountedNOTRUN.

دو شاهد مستقل Node روی week بدونتغییر: تغییر فقطstart روزذخیره‌شده، end راinvalid/null می‌کند چون hoursEdited مشترک است؛ timezoneتایپ‌شده قبلConfigure dirty=false است. source: CancelEdit فقطGeneral resetمی‌کند وrates/weekمیمانند؛ read-only rates شرطcount/amount را حذف و noCondition نشانمی‌دهد.

تنها **TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1**،parent203،25دقیقه/stop18: per-boundtime حفظprecision؛discard همهcapabilityها ازیکbaseline؛pendingtimezone rawdirty بدونweekwrite؛viewهمهconditionaxes. نهnewfeature/backend/schema/sharedservice/broadtest؛بعدنتیجهreview سپسgroup/destinationUI. تا نتیجه203-R1 تسکدیگری صادرنشود. 199/200/201/202بسته‌نهreplay؛ProductionPENDING؛TB185paused/defaultOFF/PendingAllocationمحفوظ. این بخش بر pointerهای تاریخی مقدم است.

## pointer: 203-R1 اجرا شد — منتظر بازبینی، بدون پذیرش — 2026-10-23

**TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1** (parent203) از base `f4f95bc8afada6ac528c124a8951ffa9622709c8`
اجرا و result آن به Bridge ارسال شد؛ **Architect-accepted نیست**. چهار نقص review شده اصلاح شد: provenance
per-bound ساعت (ویرایش فقط شروع/پایان، توکن کسری طرف دیگر دقیق می‌ماند، مقایسهٔ بازه بر اساس مقدار واقعی)،
Cancel Edit تأییدشده کل سه draft را از همان baseline بازنشانی می‌کند (`model/authoring-drafts.ts`)، متن timezone
در انتظار Configure به‌عنوان کار محلی dirty و blocker است ولی wrapper نمی‌سازد، و نمای فقط‌خواندنی ردیف‌ها همهٔ سه
محور وزن/تعداد/مبلغ را نشان می‌دهد (`ui/rate-presentation.ts`). 9 تست جدید + 7 fact تسک 203 + tsc سبز؛
4 guard لمس‌شده سبز. Visual/mounted NOTRUN. شواهد:
`docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1/RESULT.md`.
تا review این اصلاح، تسک 204 یا envelope بعدی صادر نشود. 199/200/201/202 بسته و replay نشوند.

## pointer: 203 اجرا شد — منتظر بازبینی، بدون پذیرش — 2026-10-23

**TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203** (parent202-R1) از base `44a220dea77c87e3d8c1a80e07b22af4e2846e45`
اجرا و result آن به Bridge ارسال شد؛ **Architect-accepted نیست**. محدوده: ردیف‌های نرخ زیر گروه‌های موجود
(add/remove/edit با متن دقیق پول، بدون Number)، برنامهٔ هفتگی ریشهٔ روش یک‌بار (configured/unconfigured صریح،
۷ روز ISO، TimeText کسری دست‌نخورده)، و ترکیب General+rates+week در یک `EditorEdits` واحد و یک PUT.
هویت/ترتیب/مناطق/ارز فقط‌خواندنی؛ overlap/price سیاست سمت سرور. 7 تست جدید + 48 تست موجود + tsc سبز؛
feature/import/test-discovery/flat-folder guard سبز؛ فقط `frontend-source-size` با ۴ تخلف **از پیش موجود** در
فایل‌های خارج از این تسک شکست خورد. Visual/browser NOTRUN. شواهد:
`docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203/RESULT.md`.
199/200/201/202 بسته و replay نشوند؛ تا review این نتیجه تسک دیگری صادر نشود.

## مرجع جاری: 202 ازطریقR1 پذیرفته / فقط203 نرخ و هفته — 2026-10-23

202-R1 در `86c0a10932f48760978681dec4d606ddbf24b33e` برای اصلاحات محدود General **ACCEPT**؛ [review](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202-R1/ARCHITECT-REVIEW.md). raw-invalid dirty داخلهمانsession،settingsuntouched/revert،تأییدCancelEdit/Back،busyguard،formSeed و lifecycle/key اصلاحشده‌اند. Architect formpurecases را مستقل اجرا کرد؛48feature/tsc/5guards گزارشworker،نهrerunArchitect. mountedReact/browser/Visual هنوز اجرا نشده؛پذیرش محدود،نهproduction/certification.

تنها تسک بعدی **TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203**،parent202-R1؛35دقیقه/stop25. UI نرخ‌های گروه‌های موجود + یک برنامه هفتگی ریشه روش، همانONEeditorPUT. ترتیب/هویت/مقصد/ارز گروه‌ها فعلاًreadonly؛ویرایش General و rates/week باید باهم حفظ شوند،نهoverwrite edits. اعداد پولی exactdecimaltext،ورودی نامعتبر dirty و blocked،timezone/day مطابق backend. نهbackend/schema/Host/sharedservices،نهGeographynewseam،نهcreate/activation/cutoff/carrier/funding.

بعدازنتیجه203 review؛سپسویرایشگروه/انتخابمقصد باauthorityموجود،وvisual/livereviewمحدود. تا پایان203 هیچتسکدیگری صادر نشود. 199/200/201/202 بسته‌اند،نهproofreplay. ProductionPENDING؛TB185paused/defaultOFF/PendingAllocationمحفوظ. این بخش بر pointerهای تاریخی اولویت دارد.

## pointer: 202-R1 اجرا شد — منتظر بازبینی، بدون پذیرش — 2026-10-23

**TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202-R1** (parent202) از base `a66573615d902df3ceeefc43adf571a2db59179f`
اجرا و result آن به Bridge ارسال شد؛ **Architect-accepted نیست** و گام بعدی مجاز نیست. محدوده: raw-dirty
جدا از wire-patch (localDirty + formHasLocalChanges؛ untouched settings omit/revert؛ blankname و
description-only و one-sided settings واقعاً dirty و invalid)، CancelEdit/Back از یک exit-plan با
Dialog و inert هنگامsave/reconcile و حفظcommitUncertain، lifecycle واقعی hook (per-draft slot، dispose،
StrictMode revival) و seed کنترل‌شده فرم، adoption-only برای savedRevision/formSeed. 48featurePASS
(38 قبلی حفظ) +10جدید، typecheck و 5guards سبز؛ Visual NOTRUN؛mount/browser test ادعا نشده.
شواهد: `docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202-R1/RESULT.md`.
199/200/201/202 بسته و replay نشوند؛ هیچ تسکی تا review این اصلاح صادر نشود.

## مرجع جاری: 202 نیازمند اصلاح / فقط202-R1 — 2026-10-23

202 در `41dc0a7fcf94e56209c8a87b84aab18a66f907eb` بررسی شد؛ [Architect review](../../architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202/ARCHITECT-REVIEW.md). **REPAIR_REQUIRED؛ تیک پایان نخورد.** مسیر واقعی/general/entry/201facts/null-admin حفظ؛38featurePASS/tsc/5guards گزارشworker،نهrerunArchitect؛VisualNOTRUN،productionPENDING.

اجرای مستقل Architect روی editor-form بدون تغییر: settings تنظیم‌شده و untouched باز هم wrapper می‌دهد؛ نام خالی یا یک‌طرف settings به edits={} تبدیل می‌شود، پس session falsely-clean و احتمال reset/خروج بدون هشدار؛ description-only زبان بدوننام حتی issue ندارد. CancelEdit مستقیم reset می‌کند و هنگامsave هم فعال است. hook فقط draftId اول را نگه می‌دارد، cleanup ندارد؛ route هم key ندارد. review شامل شاهد دقیق است.

تنها تسک بعدی **TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202-R1**،parent202،35دقیقه/stop25: dirty واقعی ورودی نامعتبر مستقل از wire-validity ولی داخل همان sessionauthority؛ untouchedsettings omit/revert؛ CancelEdit با تأیید و حفظ کار هنگامcancel/inflight؛ lifecycle واقعی React/draftId/StrictModecleanup. frontend feature + thin route identity درصورتنیاز،نهbackend/transport/schema/sharedservices یاregions/rates/weekediting. regressionsمتمرکز+feature/typecheckیکبار،نهproofloop/newpackage/browser setup. تا نتیجه202-R1 تسک دیگری صادر نشود.

199/200/201 بسته‌اند و replay نشوند؛ بعد review این اصلاح، مرحله بعد regions/rates/week UI و سپس visual/live پذیرش محدود است. cutoff/status/fullcreate/carrier/funding/quote همچنان تصمیم/کار جدا؛ TB185paused/defaultOFF/PendingAllocation محفوظ. این بخش بر active pointers تاریخی اولویت دارد.

# Tooba — ریکاوری جاری معماری ارسال / 2026-10-22

## pointer: 202 اجرا شد — منتظر بازبینی، بدون پذیرش — 2026-10-23

TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202 از base `439c43017ab38bdd6fb6999e26976796365e8140`
اجرا و result آن به Bridge ارسال شد؛ **Architect-accepted نیست** و هیچ گام بعدی مجاز نیست.
محدودهٔ تحویل: فقط مسیر نازک `/admin/shipping-services/authoring/[draftId]`، فرم general
(نام/توضیح چندزبانهٔ فعال، نوع واقعی، دو تنظیم مستقل، یک PUT اتمیک) و نمایش فقط‌خواندنی
Regions/Week؛ decoding سخت destinationFacts، اصلاح مجاز null-admin برای City/ServiceArea در
request builder، و entry فقط‌VIEW در `flat-method-detail-dialog` بدون ایجاد/POST خودکار.
ویرایش مناطق/نرخ/هفته، ساخت روش کامل، فعال‌سازی و کنترل‌های cutoff/carrier/funding عمداً پیاده نشد.
Visual: NOTRUN (بدون dev app/restart). شواهد: `docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202/RESULT.md`.
Production activation/quote/UI certification ادعا نشده؛ 199/200/201 بسته و replay نشوند.

## مرجع جاری: 201 ازطریقR2 بسته / فقط202 فرمواقعیgeneral — 2026-10-23

201 readbridge در9120f0cba36071a4c6748d1176819c032284c2ca **ACCEPT**؛[review](../../architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2/ARCHITECT-REVIEW.md). fakeDuplicateCityعلتقراردادشکسته‌بودونقصدومproductionردشد؛IsActiveserviceareaاکنونlawfultestپوشش‌دارد.15readfacts+7endpointPASS گزارشworker،نهArchitectrerun؛EFInMemoryنهSQL/schemaPG. no-postcommitازcounter1پوشش‌دارد(poisonبعدPUTفعال)،OCEidentity/naturalcancelassertنشده؛reviewدقیق،نهproofloop. timingcomplianceحدس‌نشود. prior4PGrefusalsهمانhistoryمی‌مانند.

تنها **TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202**،parent201-R2،45دقیقه/stop35؛frontendexistingfeature+thinroute /admin/shipping-services/authoring/[draftId]. فرمgeneral: databaseLanguageIdname/description،realtypechoices،دوsettingsمستقل؛onePUTexpectedrevision/sessionguards.201destinationFactsstrictdecode+resolver؛City/ServiceAreaadminnullableواقعتطبیقFEsinglepredicateمجاز،نهguess. Regions/rates/weekفعلاًreadonlyمحفوظ؛editingبعدتسکمحدود.

entryدرlegacyFlatMethodDetailDialogفقطVIEWوbindingresolved؛ازهمانserverbinding+ownerdraftlistuniqueبهrouteبرود،نهsplitmethodCode،نهPOSTخودکار،نهخروجازدیتlegacydirty. fallbackmissingdraft/ambiguous/deniedخطایصادق،نهfakecreate. قالبpreviewمرجعvisualنهfixture/datatypeauthority. noBackend/schema/sharedservices/activation؛livebrowserفقطreadonlyاگرappموجودوآماده،نهrestart/loginprobe/merchantwrite. بعدresult202review؛تسکدیگرتاresultصادرنشود.200/199بسته‌نهreplay؛TB185paused/defaultOFF/PendingAllocationمحفوظ.


## مرجع جاری: 201-R1 partial؛ فقط201-R2 fixture+integration — 2026-10-23

main0c692f35732420c785916bb28e11bd02910270df reviewشد؛[review](../../architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1/ARCHITECT-REVIEW.md). IsActiveserviceareaاضافه‌شده‌حفظ،اماtestبهassertنرسیده. 6green narrowfixturesفقط؛EF/DI/Listener/newhandlerهنوزاجرانشده؛overrun/staleassemblyثبت. 201بسته‌نیست،UI202صادرنشود.

تشخیصArchitectازsource: reproدوserviceareaباCityمشترکدارد؛owningCityIds=[City,City]؛CountingSnapshots.GetCitiesبدونDistinctدوشهریکسانمی‌دهد؛ReadVerified.TryAddدرستconfiguration_unavailableمی‌دهد. قراردادواقعیGeoReaderداخلDistinctIdentifiersdedupمی‌کند. پس«نقصدومclosure»اثبات‌نشده؛fakeقراردادراشکسته. JSشکلcollectionتاییدشد،نهاجرایC#Architect. اضافه‌کردنancestorkeyعلتduplicateراازبیننمی‌برد.

تنها **TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2**،parent201-R1،30دقیقه/stop23؛tests/docsفقط،noProductionfix. fakeقانونیDistinctوpositiveoneSA/sharedcityactiveinactive+negativebadduprows؛یکharnessrealGeo+Shipmodule/EFInMemoryبرایreaderDIlistener؛editornewportpoisonsecondreadproofدرharnessموجود. test-onlyGeoInfraProjectReferenceمجوزقبلی‌حفظ،productionContracts-only. اگرlawfulfixtureبازfailشدredtestبماند exactrows/callstageSTOPنهchase.

نهPostgres/storage/fullsuite/Host/UI/schema/sharedservices؛200و199بسته‌نهreplay. بعد201R2review:UI202. cutoff/status/fullcreate/quoteباز؛TB185paused/defaultOFF/PendingAllocationمحفوظ.


## مرجع جاری: 201 نیازمنداصلاح؛فقط201-R1 — 2026-10-23

201 درe0c2a2651e413aa7b5b0af3f5011eb7cd9955828 بررسی شد؛[review](../../architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201/ARCHITECT-REVIEW.md). readbridge/Contracts/sharedancestry/prewriteprojectionحفظ،اما201بسته‌نیست. ServiceArea branch فاقد !serviceArea.IsActive است؛ناحیه‌غیرفعالباوالدفعالاشتباهResolvedمی‌شود. proofهایEFreader/realDI/ActivityListener/newhandlerpoisonedsecondcall اجرا نشده؛6facts+10editorPASS و4storageconnectionrefusedگزارشworker،نهgreenیاregressionabsence. deadlineگذشته ثبت.

تنها **TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1**،parent201؛20دقیقه/stop14. productionفقطهمانserviceareaactivityguard. test-onlyیکProjectReference بهGeography.InfrastructureدرShipping.TestsمجازبرایownEFInMemoryreader+realmoduleDI؛productionedgeهمچنانContracts-only؛نهnewproject/package/sharedDB. ActivityListener/actualhandlerprewriteproofتکمیل؛فقطexactnonrelationalfilters نهwildcardEditorDocumentstorage یاfullsuite. اگرنقصدیگریپیداشدSTOPنهscopegrow. بعدreview:UI202.

200/199بسته،نهreplay؛frontend/schema/Host/quote/activationدست‌نخورده؛cutoff/status/fullcreate/sellerpickerباز؛TB185paused/defaultOFF/PendingAllocationمحفوظ. آخرینبخشمرجعجاری‌است.


## مرجع جاری: 201 اجرا شد؛ منتظر review — 2026-10-23

TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201 روی base `b1c89e20f495e326a932e9c0b497105b0d89bec6` اجرا و به Bridge ارسال شد؛
[RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201/RESULT.md).
Geography یک درز Contracts فقط-خواندنی bulk-by-key (`IGeographyPlaceSnapshotReader`) روی snapshotهای موجود گرفت
(بدون route/schema/HTTP جدید، بدون تغییر `IGeographyLookup`)؛ Shipping یک projection افزودنی `destinationFacts` روی
GET سند ویرایشگر و پاسخ PUT گرفت که فکت‌های مقصد را **پیش از** commit می‌خواند و پاسخ را از ریشهٔ ذخیره‌شده می‌سازد.
قاعدهٔ canonical زنجیرهٔ اداری یک helper مشترک است و resolver نوشتن به آن delegate می‌کند.
اجرا: build موفق؛ 6/6 آزمون متمرکز فکت مقصد PASS؛ مجموعهٔ editor document 10 PASS و 4 failure فقط به‌خاطر نبود
Postgres در 127.0.0.1:55432 (محیط، نه رگرسیون). Geography EF proof و DI/tracing/handler composition واقعی اجرا
نشد (نبود Geography test project + محدودیت DB) و صادقانه در RESULT ثبت شد. زمان از سقف 30 دقیقه گذشت؛ انحراف ثبت شد.
هنوز Architect-accepted نیست؛ UI/202 بدون review صادر نشود.

## مرجع جاری: 200 ازطریقR2 بسته / فقط201 hydrationخواندنی — 2026-10-23

200frontendfoundation در64f3f307cf259aeb147663f13011832c24fa1195 **ACCEPT**؛[review](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2/ARCHITECT-REVIEW.md). guardهماندraftdirty/saving وselectorunknown اصلاحشد؛10PASSگزارشworker،نهrerunArchitect. backend199بسته؛R1~27>25تاریخی‌ثبت.

تنها **TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201**،parent200-R2،30دقیقه/stop23: GeographyContractsbulkreadbyid+ownadapter؛Shippingeditorread/prewriteprojectiondestinationFactsشاملcanonicalparents/label/resolution. شاهدنیاز: readkind/idفقط؛IGeographyLookup.GetByIdsمجبورCountryIdمی‌گیردوvalidationاستنهhydrate؛HTTPفقطلیستhierarchyدارد. مرورگرنبایدallworldscan/guessparentکند. seamجدیدreadonlyContractsداخلGeo،noHTTPGeo/newDB/schema/crossFK/foreignAppDomainInfra. labelازIGeographyLabelLookupپیشفرضزبانcontentDB،نهrawguidنمایشی. recursiveadmin/cycle/inactiveوCityبدونadminواقعیپوششداده‌شود.

201 نهUIوnotbackendproofreplay؛PUTهیچforeignreadپسازcommitندارد؛readfactsقبلSaveیکباربدست‌آیدوصرفاًcomposeبعدSave. بعد201review task202 فرمواقعیgeneral باlookupها،سپسregion/rate/weekUI وliveacceptanceمحدود؛تسکدیگری‌تاresult201صادرنشود. sharedDB/Host/frontendفعلاًدست‌نخورده. cutoff/status/fullcreate/quote/carrier/funding/arbitrarysellerpickerباز؛TB185paused/defaultOFF/PendingAllocationمحفوظ. آخرینبخشمرجعجاری‌است.


## مرجع جاری: 200-R2 اجرا شد؛ منتظر review — 2026-10-23

TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2 روی base `03271c75af1f25ca0faf598ee93b4570deb2dce6` اجرا و به Bridge ارسال شد؛
[RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2/RESULT.md) و اصلاح append-only در
[R1 RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1/RESULT.md).
فقط `session/editor-session.ts`: (1) `loadStarted` همان draft هنگام dirty/saving دیگر session را restart نمی‌کند و همان
شیء state را برمی‌گرداند (epoch/baseline/edits/saveRequestId حفظ، save در جریان باطل نمی‌شود)؛ تصمیم discard برای draft
متفاوت و مسیرهای صریح discard/reconcile دست‌نخورده. (2) `hasUnsavedWork` اکنون commit نامعلوم و save در جریان را هم
پوشش می‌دهد؛ state تمیز loaded همان `false`. public API بدون تغییر.
regression-first: 1/4 PASS قبل از اصلاح، 4/4 بعد؛ اجرای متمرکز session 10/10. tsc/build/feature25 عمداً اجرا نشد
(scope محلی reducer/boolean). چهار اصلاح R1 حفظ و بازآزمایی نشد. هنوز Architect-accepted نیست؛ UI بدون review صادر نشود.

## مرجع جاری: 200-R1 اصلاحاتاصلی‌حفظ؛فقط200-R2کوچک — 2026-10-23

کد99d13021087353e3a226f860b2abb63f8d714706 review شد؛[review](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1/ARCHITECT-REVIEW.md). baselinepreserve/explicit[]/epoch/unknownlatch/wirecoherenceاصلاحاتحفظ؛25PASS/tscگزارشworker،27>25انحرافثبت‌شده. backend199بسته،نهreplay.

تنها **TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2**،parent200-R1؛10دقیقه/stop6؛session/editor-session.ts+regressiontests/docsفقط. loadStartedبرایهمانdraftوقتیdirtyیاsavingاستاکنونfallthroughمی‌کند؛loadSucceedededitsراپاک‌می‌کندوسیوinflightنادیده‌می‌شود. هماندraftبایدSAMEstateبماند،سوییچdraftدیگرباتاییدdiscard. hasUnsavedWorkبایدcommitUncertain/inflightsaveراهمپوشش‌دهد؛edited({})unknownراwarningfalseنکند. explicitdiscard/reconcile/freshloadمسیرقبلی‌حفظ.

نهdecoder/builder/API/UI/backend/schema/auth/sharedserviceتغییر؛نهتست25تکرارییاbroadbuild. بعدR2reviewوUIتسک201،قبلشصدورنشود. currentبربخش‌هایقدیمی‌برتری‌دارد؛cutoff/status/fullcreate/quoteباز؛TB185paused/defaultOFF/PendingAllocationمحفوظ.


## مرجع جاری: 200-R1 اجرا شد؛ منتظر review — 2026-10-23

TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1 روی base `29cf0117eeaa087c7edc5e4cf1a0240d1a9f5e8e` اجرا و به Bridge ارسال شد؛
[RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1/RESULT.md) و اصلاح append-only در
[200 RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200/RESULT.md).
regression-first روی 200 بدون تغییر اجرا شد (0/12) و رفتار واقعی ثبت شد؛ سپس R200-01 (baseline-aware whole-tree،
`[]` فقط با intent صریح)، R200-02 (epoch یکتای هرگزتکرارنشده در load/save/reconcile)، R200-03 (latch عدم‌قطعیت commit تا
reconcile صریح یا discard + fresh load) و R200-04 (coherence سند و تطابق terminal id انتخاب) اصلاح شد.
25 focused PASS (12 repair + 13 قبلی) و `tsc --noEmit` صفر خطا؛ فقط frontend feature و evidence docs تغییر کرد.
هنوز Architect-accepted نیست؛ UI wiring بدون review صادر نشود. هیچ visual/live/quote/activation ادعا نمی‌شود.

## مرجع جاری: 200 نیازمنداصلاح؛ فقط200-R1 — 2026-10-23

کد200 در44d74284874964d24929a426094d2264c8faefd8 بررسی شد؛ [review](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200/ARCHITECT-REVIEW.md). backend199بسته؛200هنوزACCEPTنیست،UIتسک201فعلاًصادرنشود.13PASS/typecheck گزارشworkerنقص‌هایواقعی‌زیرراپوششنداده‌اند.

تنها **TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1**،parent200،25دقیقه/stop18،featureفقط: R200-01 ادیتنام/settingsباgroupsغایب اکنونregionGroups[]می‌سازد=حذفکلدرخت؛builderبایدbaseline-awareوهمیشهfullgroupsبگیرد،explicit[]تنهاclear. R200-02 generationدرA→B→Aبه1برمی‌گرددوsaveactiongenerationندارد؛epochهرگزreuseنشود. R200-03 outcomeUnknownپسازeditقابلApplyاست؛stickyblockتاreconcile/freshGETمجازی. R200-04 readshapezeroGUID/nestedtypeid/registrydefault/day7/time وancestryterminalid mismatchپذیرفتنی‌نیست؛readonlystrictboundaryنهpricingengine. inactiveeditreject،untouchedinactivepreserve.

حفظprecision/nullclear/noautoput/onePUT/feownership. نوبتUIفقطپسازاصلاح200؛backend/schema/preview/livepage/sharedappDBممنوع. تکرار199وbackendproofممنوع.3correction>2تاریخیثبت؛source-sizeunrelatedbaselineبدونتغییرمی‌ماند. cutoff/status/fullcreate/sellerpicker/quoteباز؛TB185paused/defaultOFF/PendingAllocationمحفوظ.


## مرجع جاری: 200 اجرا شد؛ منتظر review — 2026-10-23

TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200 روی base `ccca74258fb70c3eb64be5c743b9ee2d93235e5a` اجرا و به Bridge ارسال شد؛
[RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200/RESULT.md).
فقط frontend: feature جدید `src/frontend/features/admin-shipping-authoring/` (typed decode/encode editor-document، یک PUT اتمی، session reducer)
با reuse `lib/admin/admin-result` و rewrite موجود `/v1`؛ هیچ app route/screen/design-system/shared lib/backend/schema/package/config تغییری نکرد.
13 focused PASS، `tsc --noEmit` صفر خطا؛ guardها 7/8 و تنها failure مربوط به growth قدیمی و نامرتبط چهار فایل دیگر است (baseline تغییر نکرد).
Geography read `(kind,id)` هنوز ancestry ندارد و عمداً با `needsResolution` بلاک می‌شود؛ UI wiring تسک بعدی بعد از review است.
200 هنوز Architect-accepted نیست؛ تسک بعدی بدون review صادر نشود. UI/preview/live page/owner acceptance/quote/activation باز و غیرفعال‌اند.

## مرجع جاری: 199 ازطریقR1 بسته / فقط200 — 2026-10-23

TB199 backendexistingdraft atomiceditor درcode be639a473700d771cbf7b3f90f011814cb82bfeb باtest/evidence98274e4ff7643bfddf1c60e5eca29e7ae6839f5a **ACCEPT**؛ [review](../../architecture/evidence/TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1/ARCHITECT-REVIEW.md). 31focusedPASS گزارشworker،نهrerunArchitect/whole-suite؛4correction>2 ثبتprocessdeviationبدونproofloop.645قبلیhistorical. snapshotsemantic/callbackcount/callerCTproofمحدودیت‌هایreviewرا دارد. TB19928.8min طبقBridgeداخل45/stop35است؛minute23متعلقR1بود. completionR1درpayloadنبوده؛حدس‌نشود.

تنها تسک بعد **TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200**،parent199-R1؛typedAPI+sessionstate برایdraftموجودadmin،30دقیقه/stop23. مهارتfrontendوstandardالزامی؛featureownerجدیدadmin-shipping-authoring باreuse lib/admin transport؛نهclientlegacy/fixture. strictdecodeunknown،حفظtype/languages/time/currency،writeیکPUTeditor باexpectedRevision،conflict/outcomeunknownبدونretry،late-responseisolation،dirty/discard وselected-languagepatch. geographyreadkind+id≠writeancestry؛حدسparentممنوع،writeبایدresolvedselectionگیرد. UI/preview/livepage/backend/schema/sharedDBفعال‌نشوند.

پساز200 review فقطیکUIwiringتسک،بعدliveconfiguredregistry/owneracceptance.199/196/197/198replayممنوع. cutoff/status/fullcreate/carrier/funding/arbitrarysellerselector/quoteفعال‌نشده؛TB185paused/defaultOFF/PendingAllocationمحفوظ. اینبخشبرپاراگراف‌هایقدیمیفعالیتبرتری‌دارد.


## مرجع جاری: 199-R1 اجرا شد؛ منتظر review — 2026-10-23

TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1 روی base `b0b3bb804228826a1144b9db86ee0c1362b372bf` اجرا و به Bridge ارسال شد؛ نتایج در
[RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1/RESULT.md) و اصلاح append-only در
[199 RESULT](../../architecture/evidence/TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199/RESULT.md).
tests/docs فقط: production بایت‌به‌بایت دست‌نخورده؛ هیچ Domain/Application/Infrastructure/Contracts/Endpoints/DI/Host/schema/frontend تغییر نکرد.
اجراشده: deny-before-body برای admin view-only و SingleStore/scoped seller، catalog یک‌خواندنی با sentinel (handler جدید)، snapshot کامل درخت
(NO-OP با stamp قدیمی، late-invalid same-context، race با loser واقعی، failure/caller-token/same-context retry) و guard تراکنش با caller/shape negatives.
31 focused PASS (6 endpoint + 4 storage + 21 compatibility)؛ بدون rerun 645/Host/solution. container موقت حذف و پورت55432 آزاد است.
199 هنوز Architect-accepted نیست؛ تسک بعدی بدون review صادر نشود. UI/full-create/cutoff/status/live registry/quote باز و غیرفعال‌اند؛ TB185 متوقف.

## مرجع جاری: 199 اجرا شد؛ فقط199-R1 تکمیل محدود — 2026-10-23

main/result be639a473700d771cbf7b3f90f011814cb82bfeb بررسی شد؛ [review](../../architecture/evidence/TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199/ARCHITECT-REVIEW.md). پیاده‌سازی prospectiveplan/sharedcores/یکrevision/یکcommit و4route حفظ می‌شود،اما199 هنوز بسته نیست.645PASS گزارشworker است نهrerunArchitect. timeoutگذشته ثبت بماند؛زمان22:5x دقیق نیست و نبایدحدس زد.

تنها تسک **TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1**،parent199؛tests/docs فقط،30دقیقه/stop23،دوcorrection. تکمیلnewroute deny-beforebody + SingleStore seller/scopedgrant،catalog throw-second no-postcommit proof؛تقویتهمانstoragefacts باsnapshotکامل seededweek/type/rates/text/timestamps،samecontextretry،exactcallerCT،freshreadبعدهرfailure،actualwinner/loser،no-opباstampقدیمی. transactionguardهم‌اکنونفقطbeginمی‌شمارد؛exactcaller/shape negativeproof لازم،نهگسترشallowlist. productionتغییرمجازنیست؛اگرنقصواقعیfailشدگزارشSTOP.

DBdisposable55432فقط؛focusededitor/compatibilityfilters،نهwhole645/Host/solution/Fulfillmentconfidence. recoveryفقطاینcontinuation،تسک200صادرنشود.196/197/198بسته‌اند. پس199-R1 review بعدUIadapter/liveacceptance؛fullcreate/cutoff/status/carrier/funding/quoteفعال‌نشده؛TB185paused/defaultOFF/PendingAllocation محفوظ.


## مرجع جاری: 198 بسته / فقط199 — 2026-10-23

تحلیل198 در main 66db0f405e039fcb6a9a53ab9137c93094a4124d بررسی و با اصلاحات پذیرفته شد؛ [review](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198/ARCHITECT-REVIEW.md). src تغییر نکرده؛196/197 بسته می‌مانند. تنها تسک بعد **TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199**، parent198: GET/PUT سند اتمیک draft موجود در Shipping،45دقیقه/stop35،noUI/noSchema/noQuote. تا نتیجه همین تسک،تسک جایگزین صادر نشود.

اصلاحات الزامی198: یک COMMIT نه یک SaveChanges؛ reorder به دو مرحله در transaction موجود نیاز دارد. Applyهای فعلی هرکدام revision می‌افزایند؛ mutationcore مشترک و یک advance لازم است، reset ممنوع. preflight گروه جدید+rates/titles باید prospective و بدون mutation tracked باشد. bound متن presentation=200/2000/title200 از کد واقعی؛128/512 متعلق بهtypecatalog. nullable top-level schedule/settings clear مجاز نیست؛omitted=preserve؛translation patch inactive حفظ شود. type wrapper explicitnullclear،nestednewgroupsserverids،No fallible postcommit lookup.

199 اجازه اشتراک boundary تراکنش module-owned موجود با همان یک begin site را دارد؛guard فقط exactmember جدید وnegativeproof به‌روز شود،نهfileexemption. DB disposable55432 فقط؛noHost/solution/Fulfillment run/sharedDB. meaningful focused API/preflight/noop/newgroup/reorder/race/providerfailure/cancellation tests؛یک fullShippingrun پس ازfocusedpass در صورت وقت. cutoff/status/carrier/funding/arbitraryadminsellerselector وatomicfullcreate هنوز باز،proposal نه مجوزاستنتاج. بعد199 review سپس frontendadapter/UI وliveacceptance؛TB185paused/defaultOFF/PendingAllocation محفوظ.


## مرجع جاری 2026-10-23:197 بسته شد / فقط198

197-R1 درcode`0b0a10758f000ead3846d0869b9be3225fe6b658`،evidence`cca76737c8096b9dff6cbd54c93a4ff478007ddb`،main`51ac960375a6b4783100050372b2aa5e63c3bbdb` برایpostcommitfix/providerfault/samecontext/DIparent **ACCEPT**؛ [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1/ARCHITECT-REVIEW.md). TB197backendassignment وTB196catalogبسته‌اند؛626قبلی+11/31فیلترجدیدگزارشWorker،نه rerunArchitect ونه لزوماً42تستdistinct. دوcorrectionBudgetانحرافتاریخی‌می‌ماند؛proofloopصرفPARTIALممنوع.

تنها تسک بعد **TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198**،Parent=197-R1،source-analysis/docsفقط: قراردادconcreteاتصالفرانت وread/create/saveاتمیککلeditor. قواعد9: چندPUTمستقلازمرورگرحتیsequential/PromiseallجایApplyیکپارچه‌نیست؛capabilityendpointهاهرکدامrevisionراadvanceمی‌کنند. field-to-API/domainmap،cutoff/status/canonicalbindinggap وsmallestatomicownShippingplan باreuseقواعدبایدشفافشود؛نمونهpreview منبعبیزنسdefault/حد/seedنیست.

سقف20/stop15؛بدونsrc/build/test/browser/DB/container/ef/DDL/تصویبDTOاجرایی. خروجیproposalبراییکimplementationبعدی،نهتسکبعدخودکار. sharedDB/Host/frontend/activationدست‌نخورده. بعدازتحلیلreviewفقطیکتسک؛merchantUI/bindings/languagesDB/atomicSave وuseracceptanceهنوزباز. cutoffمعنیlabel «پیشازشروع(ساعت)» دارد،min0/max48previewpolicyقطعی‌نیست؛حد/precision/runtimeeffectحدس‌نشود. TB185متوقف؛quote/carrier/funding/activation/deploymentجدا.



## مرجع جاری پس از197 / فقط197-R1

197 در`962897207bdc8f21bb07251108973a8776275722` **PARTIAL/REPAIR_REQUIRED**؛ [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197/ARCHITECT-REVIEW.md). owner/rootnullable/Contracts/schema/routes/raceدرحدsourceوگزارش626PASSپذیرفته،اماsliceبسته‌نیست.7correctionوproductionreadbackfix فراترbudgetثبتشد.196کاتالوگبسته‌می‌ماند.

ایرادactual: PUTپسازSaveدوبارهGetByIdsمی‌کند؛failureخواندندوم=خطا درپاسخپسازثبت. تنها تسک **TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1**،Parent197: reuseprevalidatedfactبرایپاسخPUTبدونforeignreadپسازcommit؛GETemptyواقعیMissingولیnonemptyinconsistent503. tests: rejectedscalarدرsamecontext/save0،realDBfault+fullgraphrollback/cleanretry،realShippingModuleDI وtracingباparent/correlation/sameexception،no-opزمانnonvacuous. مجوزفقط2Applicationfiles+tests/docs؛نهmigration/Domain/auth/DTOwire/routes/frontend. DBfaulttriggerفقطنامدارtaskownedدرcontainerدورریختنی55432ومحدودبهdraftتست،سپسحذف.

سقف30دقیقه/stop23/setup8/commands120؛یکcorrection،بعدSTOP. نهwhole626confidence/Host/solution/Fulfillmentrun؛ evidenceدیگرreuseباشناختsource. بعدنتیجهreviewویکتسکبعد؛cutoff/UIفعلاًمجازنیست،quote/activation/deploymentجدا؛TB185متوقف. پایینتاریخچه‌است.



## مرجع جاری: تحلیل قرارداد یکپارچهٔ ویرایشگر (198) ارسال شد / منتظر بررسی Architect

DOC-ONLY: تسک **TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198** روی `e33ef067` اجرا شد (هیچ کد/آزمون/داکیومنت تولیدی تغییر نکرد). خروجی: ماتریس فیلد↔API↔مالکیت، پیشنهاد یک قرارداد سند اتمیک GET/PUT برای Apply، و فهرست تصمیم‌های باز (cutoff، وضعیت/فعال‌سازی، انتخاب فروشنده برای ادمین، حامل/funding). یافتهٔ کلیدی: شش capability فعلی هر یک مستقل commit و Revision ریشه را جلو می‌برند، پس ذخیرهٔ اتمیک تصمیم۹ امروز وجود ندارد؛ نقطهٔ بازاستفادهٔ درست، جداسازی موجود `Preflight*`/`Apply*` در Domain است.

[ماتریس](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198/EDITOR-FIELD-API-MATRIX.md) · [قرارداد پیشنهادی](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198/ATOMIC-EDITOR-CONTRACT-PROPOSAL.md) · [نتیجه](../../architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198/RESULT.md). این سند مجوز پیاده‌سازی نیست؛ فقط slice بعدی A (بک‌اند سند اتمیک) پیشنهاد شده است.

## مرجع قبلی: نتیجه197-R1 ارسال شد / منتظر بررسی Architect

پس از review تسک197 (VERDICT: PARTIAL/REPAIR_REQUIRED) تسک **TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1** روی main `cc24c9c1b84e6283c0eed15f895e6b5143621395` اجرا و نتیجه به Bridge داده شد. اصلاح فقط در دو فایل مجاز Application:

- PUT دیگر هیچ فراخوانی خارجی پس از commit ندارد؛ پاسخ از همان یک فکت فعالِ تأییدشده پیش از نوشتن + ریشهٔ واقعاً ذخیره‌شده ساخته می‌شود (ثبت‌شده: pre-fix 500 روی نوشتن موفق، post-fix 200 با دقیقاً یک read).
- GET: فقط پاسخ واقعاً خالی «missing» است؛ پاسخ غیرخالی ناسازگار (شناسهٔ دیگر/تکراری) با کد موجود `shipping.authoring.configuration_unavailable` (503) بسته شکست می‌خورد.
- اثبات‌های جدید: same-context rejection با tracker پاک و SaveChanges=0، no-op با timestamp قدیمیِ seed‌شده از سرور، شکست واقعی نوشتن با trigger موقتِ task-only (P0001) + پاک‌سازی + retry موفق در همان context، و DI واقعی ماژول با parent Activity/correlation/TraceId و همان instance استثنا.

[نتیجه](../../architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1/RESULT.md). اجراها: 11/11 (focused C/D/E) و 31/31 (endpoint+error/classification guards). container دورریختنی حذف و پورت 55432 آزاد. دو correction harness (بیش از سقف یک) صادقانه ثبت شده است. schema/migration/Designer/snapshot/adapter/DTO بدون تغییر؛ شواهد owner/schema/race تسک197 بازاستفاده می‌شود.

قدم بعد فقط review این نتیجه است؛ هیچ feature بعدی/UI/quote/activation/deployment مجاز نیست. TB185 متوقف و default-OFF/PendingAllocation دست‌نخورده.

## مرجع قبلی: نتیجه197 ارسال شد / منتظر بررسی Architect

Worker تسک **TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197** (Parent=196-R13-R1) را روی main `3f05917bd0e71297bdd1fd973ad8dcd8b6687f93` اجرا و نتیجه را به Bridge داد. سه‌گانهٔ یک use case (choices/current/replace) روی شش route admin+seller با مجوزهای موجود، root-only nullable `shipping_type_id` + CHECK مختص Shipping، خواندن Contracts-only از `IShippingTypeReader` پشت `IShippingTypeCatalogReader` با یک span فرزند برای هر خواندن واقعی، presence صریح (`omitted` رد، `null` تنها clear)، active-only PUT، no-op واقعی، stale 409 قبل از I/O خارجی و خواندن صادقانهٔ inactive/missing. [نتیجه](../../architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197/RESULT.md).

اجراهای واقعی: focused 9/9 و کل Shipping.Tests 626/626 روی postgres دورریختنی `127.0.0.1:55432`؛ container حذف و پورت آزاد. correctionها (۷ مورد، خارج از سقف «یک correction») و تغییر include مسیر نوشتن برای readback کامل در RESULT صادقانه ثبت شده‌اند. classification از route/handler/validator واقعی به‌روز شد (49/33/29) بدون promotion/certification.

قدم بعد فقط review وی‌کت این نتیجه است؛ هیچ task بعدی/UI/quote/activation/deployment مجاز نیست. کاتالوگ196 همچنان بستهٔ محدود است و این slice آن را recertify نمی‌کند. TB185 متوقف و default-OFF/PendingAllocation دست‌نخورده. پایین تاریخچه است.

## مرجع قبلی: کاتالوگ196 بسته شد / فقط197

پس ازبررسی R13-R1 درcode`b1d88ac59463f7786e856048bf02ec99baf96223` وmain`f04a13dd44a95e2ff40d1db3d39a77c6e25d3cf4`، سهproof اصلاح‌شده **ACCEPT** و کل196 برای boundedFulfillmentcatalog/API/Contracts/schema/storage **بسته شد**؛ [بررسی/lineage](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1/ARCHITECT-REVIEW.md). WorkerPARTIALبابتcorrection/reset/trace-rerun همچنانتاریخچهصادقانه است؛3PASS گزارشWorker وsourceبررسی‌شده،نه اجرایمجددArchitect. هیچدلیل برایتکرارکاتالوگ/proofloopsنیست. اینacceptance باUI/merchantDB/quote/release/AMSCrecertificationیکی نیست.

تنها تسک بعد **TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197**،Parent=196-R13-R1: انتساب اختیاریShippingTypeIdرویdraftrootShipping +choices/currentread ازFulfillment.Contracts.IShippingTypeReader؛ serverowner/permission/sharedRevision،بدونcopycatalog/FK/join/foreignimplementation. تصمیم27rules مرجع: nullصریحclear،omittedرد،non-nullPUTفقطactive،خواندنinactive/missingصادقانه وبدونحذفمرجع،no-opactive/stale409،بدونactivation. migrationفقطShippingرویDBدورریختنی؛Fulfillmentتغییرنکند. سقف45/stop35/setup8/commands120؛فقطShippingchecks،noHost.

بعدازنتیجه197reviewویکتسکبعد: مواردباقیconfig/cutoff،UIAPI/languagesDB/SingleStore-Marketplace وuseracceptance. runtimebasis/carrier/freefunding/quote/activation/deployment مجوزجدا؛TB185متوقف. recovery/AGENTSبه‌روز،یکactive task؛ پایین تاریخچه است.



## مرجع جاری پس از R13-R1 / منتظر بررسی مستقل Architect

R13-R1 روی main `7c19e63403d8ab66405b9762ba620bd76ede04e0` فقط test/evidence: سه fact اصلاح شد. uniqueCreate حالا `CreateOutcome(Participant,Guid?,ErrorCode)` دارد و persistedid با Guidبازگشتی‌واقعی و کلtree با proposalبرنده مقایسه می‌شود؛ revisionrace دopropسالهٔ سه‌زبانه/Sort/IsActive، دو `OriginalValues["Revision"]=1` واقعی، winnerاز typed outcome، revision exact2، CreatedAtثابت و UpdatedAtجلو‌رفته؛ rollback حالا `TreeSnapshot` کامل (همهroot + همهtranslation با equalityساختاری) قبل/بعد failure و cancellation و retry. `AttemptedWriteCount=2/completed=1` برای هر دو مسیر. اجرای متمرکز: 3/3 PASS، 1.67s. statusفرایندی PARTIAL (یک correction + resetمحیط + rerunثبتtrace). production/migration byte-identical. [نتیجه](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1/RESULT.md).

تصمیم بعدی فقط با Architect: بررسی مستقل این سه اثبات و سپس verdict closureکاتالوگ. تا آن زمان هیچ taskجدید، promotionmanifest،certification،assignmentShipping،UI/API/languagesDB،quote/activation/deployment مجاز نیست؛ TB185متوقف و default-OFF/PendingAllocation دست‌نخورده. پایین تاریخچه است.

## مرجع جاری پس از R13 / فقط R13-R1

R13 درcode`86c698fe251a1a45095205b7f9dcb272bae866a2` وmain`ad7d7a3c77d55a39358137d63e9d01e25f78aba4` برای phaseschema/storage/rawconstraints/defaulttrace **ACCEPT محدود**؛ [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13/ARCHITECT-REVIEW.md). model/current218 وpretype/down192 مستقلاًbyte-equal؛35PASS گزارشWorker،دوcorrectionانحراففرایندی. whole196هنوزبسته‌نیست.

سه ادعای باقی دقیق‌اند: uniqueCreateGuid توسطFunc<Task>دورانداخته‌می‌شود؛ revisionrace فقطیکlanguage راassertمی‌کند نهcompletewinner؛ rollbackRawRowهمهroot/alltranslation را ندارد. تنها تسک **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1**،Parent=196-R13: فقط3fact/helper/evidence اصلاح؛ winneroutcome typed،تمامroot+translation وcounts،snapshotکاملپسازfailure/cancellation وsecondwrite. production/migration/metadata ممنوع؛ آزمون‌های قبلیreusedنهfull35rerun. سقف20دقیقه،stop15،setup8،commands120،یکcorrection.

بعدازنتیجه بررسی مستقل و تصمیمclosureکاتالوگ،بعدassignmentShippingازContracts وUI/API/languagesDB؛ هیچparallel/replacementtask تاcompletion؛ quote/activation/deploymentجدا،TB185متوقف. پایین تاریخچه است.



## مرجع جاری پس از R12 / فقط R13

R12 پس از review در main `e4fdcf587facb9bef1d28045c00744bbffb17244` برای modeling5default/schema0diff **ACCEPT محدود**؛ WorkerPARTIAL به‌دلیل4harnesscorrection+modelDBreset ثبت می‌ماند. [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12/ARCHITECT-REVIEW.md). دوHasDefaultValueSql("0") فقط برایdecimalها صریحاً ratifyشد؛ValueGeneratedNever حفظ؛manifestهای218خط byte-identical مستقل تأیید شد. case-summary برابرSQLtrace نیست؛ bindings وunconditionalmetadata وprefixnegative درtestبعد کامل شود، نه تکرارbaseline/production.

تنها تسک بعد **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13**،Parent=196-R12: TEST/EVIDENCE ONLY برای fixturechronology واجرای باقی relationalproof. stalePascalCaseSQL وgateقبلR10 وbarrierبدونoriginaltoken وinterceptorشمارندهSELECTها ازsource مشخص‌اند؛ فقطtestsاصلاح. ترتیب: historicalpretype ->typeUpدقیق2table ->R10indexUpدقیق1index ->strictcurrentmodel0diff ->storage/constraints/races/atomicity ->Downمعکوس باproofواقعی. هیچsrcproduction/migration/metadata/business/guardwaiver مجازنیست؛ productionfailure=>STOP/BLOCKED نهfix.

سقف45دقیقه،stop35/setup8/commands120؛ یکcorrection. شمارش تست و اجراهایشده/نشده دقیق؛ noHost/fullsolution/fullmodule/skip/sharedDB. whole196 هنوزBLOCKED تاreviewrelational؛ سپسassignmentدرShippingازContracts،بعدUI/API/languagesDB/SingleStore-Marketplace وuseracceptance. quote/activation/carrier/funding/runtimebasis/deployment مستقل؛TB185متوقف. پس ازنتیجهفقطreviewویکتسکبعد؛ پایین تاریخچه است.



## مرجع جاری پس از R11 / فقط R12

R11 در `3fef1455ae12ca612ced7ac4720e7c679e6206c1` برای دوCHECK/دهنام موجود **ACCEPT**؛ [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11/ARCHITECT-REVIEW.md). manifestهای218خط مستقلاً مقایسه و فقط10خطِ پنجdefaultpair باقی بود؛6+5+1PASS گزارش Worker،نه اجرای مستقل Architect. helper فعلی counterpartdefault و historyprefix را exhaustivelyاثباتنمی‌کند؛ در تستnext باید strictfull-equality/exactidentitySQL شود، نه whitelist.

تنها تسک صادرشده **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12**،Parent=196-R11: توصیف پنج serverdefault تاریخی در مدل با typedHasDefaultValue و explicitValueGeneratedNever تا ارسال صریحEFحفظشود. قبل/بعد INSERT SQL+parameters برایzero/empty وnondefault وfreshreadback اجباری؛ metadata/savebehavior ثابت. فقط5propertymapping/currentSnapshot/latestR10TargetModel؛ هیچDDL/newmigration/defaultdrop/تغییرbusiness. fullchainR10 درDBدورریختنی وmodelprojection باید بدونexemptionدقیقاًبرابر شوند. sharedDB/apps/flagsممنوع.

سقف35دقیقه،stop28/setup8/commands120،یکcorrection. whole196/fullstorage/races/atomicityهنوزBLOCKED؛ fullschemafixtureقدیمی preR10chainراreplayمی‌کند و integrationآن مجوزجدا می‌خواهد. پس ازنتیجهR12بررسی مستقل،سپس فقطیکتسک؛assignment/UI/quote/deploymentهنوزمجازنیست؛TB185متوقف. پایین تاریخچه است.



## مرجع جاری پس از R10 / فقط R11

R10 در `d49ce7b2d2cd748e567f51ccd2a4c2377400b6ab` برای migration تک‌ایندکس و شواهد محدود **ACCEPT**؛ [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10/ARCHITECT-REVIEW.md). Source وSQL واقعاً schema-qualified و manifestbaseline/down برابرند؛3+6PASS گزارش Worker، نه اجرای مستقل Architect. دو harness correction برخلاف سقف یک مورد، انحراف فرایندی ثبت شد؛ تکرار خودکار مجاز نیست.

تنها تسک بعد **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11**، Parent=196-R10: مدل دوCHECK تاریخیshipping_methods و10نامFK/index را دقیقاً توصیف کند؛ currentSnapshot و فقط latestR10Designer metadata canonical همسو شوند. runtime فقط annotationهای همین12object؛ هیچDDL/newmigration/DBrename/حذفCHECK یا تغییرdefault/ValueGenerated نیست. همه executablemigrationها و همهDesignerهای قدیمیimmutable. proof offline + disposable historical-chain/modelprojection باید فقط5defaultpair باقی نشان دهد. تست latest/current بایدactual latestR10را بسنجد، نه مجبورکردنTargetModel تاریخی196به مدل آینده.

سقف35دقیقه،stop28،setup8،commands120؛ یک correction، بعدSTOP/PARTIAL. sharedDB/apps/flags ممنوع. پنجdefault وfullstorage/races/whole196 هنوزBLOCKED؛ هیچignore/baselinepermit نیست. پس ازنتیجهR11 بررسی مستقل، سپس فقطیکتسک؛assignment/UI/quote/deployment هنوزمجازنیست؛TB185متوقف. پایین تاریخچه است.



## مرجع جاری پس از بررسی R9 / تنها تسک بعد R10

R9 در main `49f5b8721290a436d82df7160811b6a62b626dd6` برای تحلیل محدود **ACCEPT با اصلاح**؛ [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9/ARCHITECT-REVIEW.md). شمارش مستقل دو فایل: actual192/model191 (نه216)، missing16/unexpected17؛ دسته‌بندی3+5+10 برقرار. هیچ کد/DB درR9تغییر نکرد.

تنها تسک صادرشده **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10**، Parent=196-R9: یک migration افزایشی برای pending outbox index موجود در shared model ولی مفقود در chain، با CreateIndex عادی transactional و Down محدود، metadata جدید canonical و proof PostgreSQL دورریختنی. پیشنهاد IF NOT EXISTS رد شد: همنام اشتباه را پنهان می‌کند و مالکیت Down را اثبات نمی‌کند. همنام موجود باید fail-closed شود؛ هیچ adoption/shared deployment/concurrent rollout مجاز نیست. runtime/model/snapshot و همه migration/designer قبلی byte-identical؛ فقط migration تازه و target metadata آن، tests محدود و docs. سقف35دقیقه، stop28، setup8،commands120s. بعد از تحویل R10 منتظر بررسی Architect؛ هیچ تسک موازی/جایگزین.

کل196 هنوز BLOCKED؛ CHECK/default/name و storage/race/rollback کامل باز. پیش‌فرض‌ها با HasDefaultValue تغییر semantics می‌دهند؛ مجوز blanket mapping/DDL/baseline ندارند. بعد از کاتالوگ، assignmentسپس UI/API/languagesDB/SingleStore-Marketplace؛ quote/activation/deployment مستقل؛TB185متوقف. پایین تاریخچه است.



## تسک واحد صادرشده پس از پذیرش R8 — R9

به درخواست صریح مالک، تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9**، Parent=196-R8، صادر می‌شود: تحلیل source/evidence اختلاف schema تاریخی و مدل canonical و پیشنهاد کوچک‌ترین reconciliation امن. فقط docs؛ بدون تغییر runtime/metadata/migration/test، DB/container/DDL یا اجرای suite. سقف20دقیقه، توقف تحقیق جدید در15؛ counts قدیمی3+5+10 فرض قطعی نیستند و باید object-by-object از منبع بازبینی شوند. خروجی proposal است و مجوز تغییر اجرایی نیست. پس از دریافت نتیجه، Architect مستقل بررسی می‌کند و فقط سپس تسک بعدی؛ هیچ نسخه جایگزین یا تسک موازی صادر نشود. R8 تمام و accepted محدود است؛ کل196 و assignment/UI/activation همچنان blocked. این بخش بر «هیچ تسک بعدی» در checkpoint پایین مقدم است؛ پایین تاریخچه است.



## مرجع جاری Architect پس از دریافت و بررسی R8 — 2026-10-22

این بخش بر وضعیت‌های پایین مقدم است؛ آن‌ها تاریخچه‌اند. **R8 تمام شده و برای اصلاح metadata پذیرفته شد**؛ checkpoint کد `ad1aa1b987f8a7f0037230afbb72b7a57f6c8fdd`، main بررسی‌شده `8aeeaa385a240a46e72a4f5297325d8e651d4f58`. [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8/ARCHITECT-REVIEW.md).

- دو metadata artifact با مدل canonical هماهنگ‌اند؛ هر دو فهرست baseline برابر191عملیات نام‌گذاری و after صفر است. تست واقعی differ/initializer و normalized metadata از source بررسی شد؛ نتایج3+28PASS گزارش Worker است و Architect دوباره اجرا نکرده.
- runtime model/factory، executable Up/Down، مهاجرت‌های قدیمی، business/API/فرانت/manifest هیچ تغییری ندارند. پذیرش Up/Down محدود R7 با source یکسان باقی است؛ schema تاریخی هنوز برابر مدل اثبات نشده.
- کل196 همچنان **BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE**. index outbox مفقود در migration، دوCHECK تاریخیِ خارج مدل، پنجdefault و دهname اختلاف بازند؛ storage/unique/revision race/rollback/cancellation و constraint-lookalike هنوز شواهد اجراشده لازم دارند.
- **هیچ تسک فعالِ صادرشدهٔ بعدی در این checkpoint نیست. R8 را دوباره اجرا نکنید.** گام بعدِ آماده‌سازی: تحلیل محدود reconciliation تاریخی با provenance دقیق هر اختلاف و پیشنهاد کوچک‌ترین تصمیم امن، قبل از هر تغییر اجرایی legacy. این یادداشت مجوز اجرا یا تسک جدید نیست.
- طبق تذکر مالک، قبل از اتمام و بررسی تسک جاری فایل تسک تازه یا نسخه جایگزین ندهید. R8 اکنون بررسی و بسته شده؛ فقط یک تسک بعدی می‌تواند جداگانه صادر شود.
- تصحیح گزارش Worker: فاصله Expected abf7072 تا Base8741f3c دقیقاً **یک** کامیت Architect فقط docs (+41خط) بود، نه دو. ancestry و تغییرات آن مستقلاً بررسی شد؛ این مورد مجوز نادیده‌گرفتن mismatchهای آینده نیست.
- ادامه پس از بسته‌شدن کاتالوگ: assignment در Shipping فقط از Fulfillment.Contracts، سپس UI/API و زبان‌های واقعیDB و SingleStore/Marketplace و پذیرش مرورگر/مالک. quote/activation/runtime amount-count basis/carrier/free funding/cutoff و deployment مستقل و باز؛TB185متوقف.




## آخرین checkpoint برای بازیابی چت — درخواست مستقیم مالک، 2026-10-22

این بخش مرجع جاری است؛ توضیحات پایین تاریخچه‌اند. هدف: تکمیل Admin Shipping، سپس Storefront و استقرار. Backend/Frontend clean architecture و AMSC/feature skills اجباری؛ فقط یک تسک دانلودی با preview در هر نوبت، بعد انتظار نتیجه و بررسی مستقل. Worker PASS برابر Architect ACCEPT نیست.

### دقیقاً کجا هستیم
- ریپو mrnikiemami-code/Tooba، شاخه main. آخرین کد بررسی‌شده و پذیرفتهٔ محدود: R7 در `d78cfa136689f68bdbaeba97a51a401d105ae1fa`. آخرین handoff پیش از این checkpoint: `abf7072aef02783cca6576180c8344bfe3ad09d1`.
- 190 با R1/R2: draft مالک‌محور و گروه مناطق پایدار؛191-R1: ردیف تعرفه دستی؛192-R1: متن‌های LanguageId؛193-R1: برنامه مستقل هرروز؛194: دو انتخاب مستقل پیش/پس‌کرایه و دستی/API/رایگان پذیرفته‌اند، صرفاً authoring.
- 195: تصمیم D2؛ تعریف نوع ارسال در Fulfillment و انتساب آینده به draft در Shipping. رجیستری تعریف متعلق به commerce DB/نصب حل‌شده است؛ Admin مدیریت می‌کند، Seller فقط انتخاب مجاز آینده دارد. چهار برچسب نمونه enum/seed نیستند؛ نوع هیچ سیاست قیمت/پوشش/برنامه ارثی ایجاد نمی‌کند.
- کل196 هنوز **BLOCKED / NOT ACCEPTED**. R2 اصلاح Domain؛ R3+R4 شواهد API/auth/body/validator/no-op؛ R5+اصلاح fixture درR6 شواهد DI/language/listener؛ R7 اصلاح design-time factory/naming و Up/Down مستقل دو جدول جدید پذیرفته شده‌اند.
- مهاجرت جدید قبلاً ستون PascalCase و CHECK snake_case داشت؛ R7 فقط همین مهاجرت unshipped را اصلاح کرد. factory اکنون ToobaNpgsql canonical را مصرف می‌کند. هیچ migration مشترک/production اعمال نشده.
- snapshot/latest Designer هنوز metadata قدیمی ناسازگار با مدل canonical دارند؛ آخرین گزارش191operation است، نه برابری. جدا از آن، اختلاف تاریخی واقعی: index outbox pending در مدل ولی نه migration؛ دوCHECK shipping_methods در migration ولی نه مدل؛ پنجdefault و دهname اختلاف ثبت شده. هیچ‌کدام مجوز حذف/اصلاح خودکار ندارند.

### تنها تسک فعال و کار بعد
**TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8**، Parent=196-R7، Program=TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY. **صادر شده؛ نتیجه هنوز دریافت/بررسی نشده است.** وضعیت claim/اجرای Worker از روی این یادداشت معلوم نیست؛ بدون بررسی نتیجه/Bridge تسک تکراری نسازید.
Scope R8 فقط بازتولید metadata دو artifact با generator canonical:
1. current FulfillmentDbContextModelSnapshot؛
2. BuildTargetModel آخرین 20261022101415_ShippingTypeCatalog.Designer.
مدل runtime، factory، executable Up/Down، همه migration/designer قدیمی و همه business/APIها باید byte-identical بمانند. هیچ DDL/DB/container/Host/فرانت در R8 نیست. اثبات لازم: snapshot و latest TargetModel هر دو با initialized canonical IDesignTimeModel صفر اختلاف relational؛ provenance generator و diff معنایی ثبت شود. سقف25دقیقه، stop کار جدید در20.
این checkpoint فقط docs است. برای claim جدید، نسخهٔ به‌روز **همان فایل R8** با Expected-Base-SHA پس از این checkpoint ارائه می‌شود؛ Task-ID/Scope عوض نشده و تسک تازه‌ای صادر نشده است. اگر قبلاً R8 claim/تمام شده، آن را دوباره اجرا نکنید؛ اول نتیجه و SHA واقعی را بازیابی و فقط docs concurrent را طبق قرارداد امن بررسی کنید. هیچ mismatch را کورکورانه نادیده نگیرید.

### پس از رسیدن نتیجه R8 — برنامه، نه مجوز اجرا
1. source/evidence/diff و صفر بودن اختلاف هر دو metadata artifact را مستقلاً بررسی کن؛ فقط همین slice را قبول/اصلاح کن.
2. اگر R8 پذیرفته شد، اختلاف مدل با schema تاریخی را از metadata mismatch جدا کن. برای reconciliation دقیق defaults/CHECK/names و index outbox پیشنهاد محدود و مجوز بعدی ثبت کن؛ no blanket ignore/baseline refresh، no legacy DDL بدون تسک جدا.
3. سپس شواهد هنوز اجرا‌نشدهٔ PostgreSQL: raw constraints، unique-code/revision races با original token1 و دقیقاً یک برنده، loser tracker، mid-save rollback/cancellation و constraint-lookalike را با target دورریختنی کامل کن. assertion کامیت‌شده برابر proof اجراشده نیست.
4. فقط بعد از بسته‌شدن کاتالوگ196، انتساب owner-local نوع در Shipping از Contracts reader؛ بعد اتصال UI مصوب به API/زبان‌های DB و SingleStore/Marketplace و browser/user acceptance.
5. مبنای authoritative مبلغ/تعداد/اندازهٔ گروه، cutoff، carrier/مختصات، free cost/funder accountability، quote/activation/rollout هنوز gateهای مستقل‌اند. انتخاب CarrierApi یا Free اجرای واقعی ایجاد نمی‌کند. Storefront/استقرار بعد از Admin؛ TB185 performance همچنان متوقف.

### منابع و خطوط قرمز بازیابی
- AGENTS.md و این فایل اول خوانده شوند؛ docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md بخش26، flat-method clarification و SHIPPING-STOREFRONT-UX-REFERENCE.md الزامی‌اند.
- بررسی‌های Architect در docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R*/ARCHITECT-REVIEW.md مرز پذیرش‌اند؛ گزارش Worker به تنهایی SoT پذیرش نیست.
- هیچ cross-module implementation/DbContext/join/FK، Host business، جغرافیای دوباره، زبان ثابتfa/en، IRR حدسی، owner/edition از مرورگر یا ارث سیاست بین مالکان ایجاد نشود.
- shared :3000/:3419/:5088/:5432، DBها، bin/obj/.next/cache، dirty/untracked و stashes دیگران محفوظ؛ no reset/stash/rebase/force-push، no shared restart، no credentials، no dummy Bridge result.
- شواهد آزمون این چت توسط Architect دوباره اجرا نشده‌اند؛ source/diff بررسی شده و شمارش‌ها گزارش Worker است. certification تاریخی، acceptance محصول، visual و release مجزا هستند.



> **وضعیت جاری Architect پس از196-R7:** اصلاح factory/naming و Up/Down مستقل دو جدول **ACCEPT**؛ [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7/ARCHITECT-REVIEW.md)، checkpoint `d78cfa136689f68bdbaeba97a51a401d105ae1fa`.6+1 PASS و probe مسدود گزارش Worker، source بررسی شد. مانع snapshot هنوز191operation گزارش دارد؛ این با اختلاف واقعیschema قدیمی یکی نیست. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8** (Parent=196-R7): فقط بازتولیدcanonical metadataِ currentSnapshot و آخرینDesigner از مدل فعلیِ بدون‌تغییر و proof differ=0؛ استثنای صریح دوartifact، نه DDL/مهاجرت تازه/تغییرmapping یا مهاجرت قدیمی. اختلاف تاریخیoutbox/CHECK/default/name و full storage/races هنوز BLOCKED؛ whole196 NOT ACCEPTED و assignment/UI/quote/استقرار ممنوع؛TB185متوقف. پایین تاریخچه است.


> **وضعیت جاری Architect پس از196-R6:** **BLOCKED تأیید شد**؛ [بررسی/مجوز محدود](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6/ARCHITECT-REVIEW.md)، checkpoint `f30eaee17b046f45413a17d85645f883b442db86`. مهاجرت196 ستونPascalCase می‌سازد ولیCHECK snake_case دارد؛ factory design-time از تنظیم canonical runtime استفاده نمی‌کند. تست snapshot209اختلاف را ثبت کرده ولی برابری را ثابت نکرده و برخلاف Gate1 پس ازاختلاف ادامه داده؛ این scope deviation است. fixture یک‌زبان اصلاح و corner R5 بسته شد. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7** (Parent=196-R6): اصلاح factory و فقط مهاجرت جدیدِ unaccepted و type-blockهای Designer/snapshot + Up/Down مستقل دو جدول و fail-fast fixture؛ استثنای صریح محدود، نه بازنویسی مهاجرت قدیمی یا full snapshot. اختلاف legacy outbox/check/default/name و برابری snapshot هنوز BLOCKED و مجوزDDL ندارند؛ R7PASS پذیرش196نیست. assignment/UI/quote/استقرار ممنوع و TB-185 متوقف. پایین تاریخچه است.


> **وضعیت جاری Architect پس از196-R5:** DI واقعی و چرخهٔ registry/listener در محدودهٔ fixture **ACCEPT با یک اصلاح نمونه**؛ [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5/ARCHITECT-REVIEW.md)، checkpoint `0bd17a8f704d782bd45f0763df7e066685ff3fa1`؛22/22 گزارش Worker، اینجا اجرا نشده. تست «یک فعال» درواقعfa+ar هر دوفعال دارد؛ default غیرفارسی/nonfirst را ثابت می‌کند ولی count1 نه. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6** (Parent=196-R5): اصلاح همانfixture + proof PostgreSQL/snapshot/constraint/unique/revision-race در محیط دورریختنی؛ بدون تغییرکد محصول/مهاجرت/snapshot. schema تاریخی در برابر snapshot باید مستقل سنجیده شود؛ اختلاف=>BLOCKED و نه بازسازی legacy. whole196 هنوز PARTIAL؛ assignment Shipping/UI/quote/استقرار ممنوع، TB-185 متوقف. پایین تاریخچه است.


> **وضعیت جاری Architect پس از196-R4:** سه اصلاح proof **ACCEPT** و شواهد APIِ R3 با R4 پذیرفته شد؛ [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R4/ARCHITECT-REVIEW.md). code `a638266bc`، HEAD transport `726ae303473f916b10ae2cf95bb95ab15fa8e045`؛45تست گزارش Worker و source بررسی شد، اینجا اجرا نشد. no-op nonempty با clock متغیر، root/childcode و mapping مشاهده‌شده از Sender/DI بسته شد. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5** (Parent=196-R4)، test-only برای registry lifecycle از adapter واقعی، listener و DI واقعی FulfillmentModule؛ نه کپی registration و نه DB. constraint-lookalike و PG/snapshot/unique/revision-race هنوز gate جدا؛196 **PARTIAL_PENDING_REMAINING_PROOF**، assignment Shipping/UI/quote/استقرار ممنوع و TB-185 متوقف. پایین تاریخچه است.


> **وضعیت جاری Architect پس از196-R3:** بخشی از proof API/auth/body پذیرفته، کلR3 **PARTIAL / REPAIR_REQUIRED**؛ [بررسی](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3/ARCHITECT-REVIEW.md)، code `33c056dee` و HEAD `cc8059688982d5ed647119b8377735cf009821ec`. گزارش42/42 اجرای Worker است؛ بررسی source نشان داد no-op ادعایی nonempty درواقع[] است و clock ثابت، validation فقط400 بدون childcode، classification نگاشتrequest را از جدول حدس می‌زند نه SenderRequests هرdelegate. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R4**، Parent=196-R3، فقط اصلاح همین سه شواهد با harness موجود/سقف20دقیقه و بدونکد محصول. ثبت Infra DI در harness کپی شده، نه proof composition کامل؛ fixture دو زبانfa/en است نه default غیرفارسی. gateهای registry/listener/constraint/PG/snapshot/race هنوز باز؛196 NOT ACCEPTED، Shipping assignment/UI/quote/استقرار ممنوع، TB-185 متوقف. پایین تاریخچه است.


> **وضعیت جاری Architect پس از196-R2:** دو اصلاح محدود Domain **ACCEPT**؛ [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2/ARCHITECT-REVIEW.md)، checkpoint `1b42b7e7e02f147e323433a5db1a20993d601349`. مرزصفر/حداکثر و atomic guardهای متن/شناسه از source/test بررسی شد؛54+1 تست گزارش Worker، اینجا اجرا نشد. اختلاف زمان16/19دقیقه در دو گزارش ثبت شد، هر دو زیر20؛ اثبات مستقل زمان نیست. کل196 هنوز **PARTIAL_PENDING_API_TRACE_AND_RELATIONAL_PROOF**. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3** (Parent=196-R2): فقط تست delegates واقعی API/auth/body/validation و set-equality چهارمسیر، بدون کد محصول/DB/Host/full-suite. ماتریس کامل زبان، listener، constraint-lookalike و snapshot/PostgreSQL/race هنوز gate جدا هستند. assignment Shipping/UI/quote/فعال‌سازی/استقرار مجاز نیست؛ TB-185 متوقف. پایین تاریخچه است.


> **وضعیت جاری پس از196-R1:** **PARTIAL / NOT ACCEPTED**؛ [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1/ARCHITECT-REVIEW.md)، checkpoint `d5406396e8032b32d22244f29d20336f4c4017fb`. اصلاح ورودی/preflight/no-op در source هست ولی proof API/adapter زبان/listener/set-equality هنوز اجرا نشده؛85دقیقه از45 گذشته و build Host خارج scope بود. رگرسیون تازه: Create ترتیب صفر مجاز را رد می‌کند؛ guardهای متن/شناسهٔ Domain هم حذف شده‌اند. وابستگی Domain→Contracts ممنوع می‌ماند، حذف قواعد راه‌حل نیست. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2** (Parent=196-R1)، فقط مرزهای Domain و regression هدفمند با سقف20دقیقه، نه همهٔ backlog. snapshot/PG/race و API/tracing هنوز gate جدا و باز؛ بدون assignment Shipping/UI/فعال‌سازی/استقرار، TB-185 متوقف. پایین تاریخچه است.


> **وضعیت جاری پس از 196:** **PARTIAL / NOT ACCEPTED**؛ [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196/ARCHITECT-REVIEW.md). code `7f1e1e080`؛ HEAD transport `c9f711dd415423f5a929b078a16381028d81c419`. کد نوع‌ها نوشته شده ولی proof API/زبان/tracing/PostgreSQL کامل نیست و63دقیقه از45 تجاوز کرده. نقص‌های source: حذف nullهای ترجمه قبل validation، no-op نادرست برای متن یکسان، mutation قبل preflight کامل، انتخاب اولین default از چند default، read input بی‌validator و namespace/path mismatch. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1** (Parent=196) برای اصلاح رفتار/API/tracing و proof in-process؛ بدون DB یا migration. تغییر گسترده snapshot و هم‌ارزی زنجیره مهاجرت/رقابت PostgreSQL در R196-06 باز است و بعداً تسک جدا می‌خواهد. R1 PASS پذیرش196 نیست. Shipping assignment/فرانت/quote/استقرار ممنوع؛ TB-185 متوقف. پایین تاریخچه است.


> **وضعیت جاری Architect پس از 195:** تحلیل محدود **ACCEPT WITH CORRECTIONS**؛ [بررسی/تصمیم](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195/ARCHITECT-REVIEW.md). تعریف نوع در Fulfillment و انتساب آینده در Shipping؛ Admin همان دیتابیس commerce/نصب مرجع تعریف، Seller صرفاً انتخاب‌کنندهٔ مجاز، بدون رجیستری جهانی یا سیاست ارثی. rules بخش26 و clarification قفل flat ثبت شد. شباهت نام جدول/ترجمه دلیل مالکیت نیست؛ مالکیت تصمیم صریح Architect است. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196** (Parent=195): کاتالوگ قابل مدیریت create/read/update/status و Contracts reader، فقط Fulfillment؛ نه assignment Shipping یا فرانت. permission واقعی view/manage لازم؛ seam قدیمی Fulfillment فقط Admin gate دارد. unavailable را empty-success نکنید. Structure handoff پیاده‌سازی REQUIRED، بدون recertification. تحلیل آزمون/DB نداشت؛ gateهای UI/نوع‌گذاری کالا/فعال‌سازی/مبلغ‌وتعداد/carrier/funder/استقرار باز و TB-185 متوقف؛ پایین تاریخچه است.


> **نتیجهٔ 195 (در انتظار تصمیم Architect):** تحلیل محدود D2 انجام و از Bridge ارسال شد؛ [شواهد](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195/RESULT.md). owner فعلی نوع ارسال وجود ندارد؛ پیشنهاد (PROPOSED، نه APPROVED): تعریف مشترک taxonomy با متن `LanguageId` در Fulfillment و انتساب مالک‌محور nullable روی ریشهٔ draft در Shipping از طریق Contracts reader، بدون FK/join. قفل قدیمی no-Types-of-Service برای دامنهٔ خودش معتبر است؛ تنها تنش واقعی، اشتراک نوع میان چند روش یک مالک با isolation §B/§C است. تصمیم‌های باز: authority تعریف، الزام‌بودن، اثر سازگاری روش/کالا، scope مارکت‌پلیس و اصلاح یک‌خطی قفل. هیچ کدنویسی/DB/فرانت انجام نشد و تسک بعدی صادر نشده است.
> **وضعیت جاری Architect پس از 194:** دو انتخاب مستقل پیش/پس‌کرایه و دستی/API/رایگان در ریشهٔ draft **ACCEPT**؛ [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194/ARCHITECT-REVIEW.md). checkpoint `6fde6ee768a9b6639d1f4df10600e19c327bd9c1`؛ 616/616 گزارش Worker، اینجا اجرا نشده. آزمون رقابت و قیدهای PostgreSQL از source بررسی شد. اصلاح گزارش: routeها39→43؛ timestamp در no-op از source پشتیبانی دارد ولی assert اجرایی ندارد؛ سه ویرایش پسینی نرخ/ترجمه/برنامه در تست تازه اجرا نشده‌اند (ویرایش مناطق اجرا شده). انتخاب حالت، اجرای مالی/حامل/فعال‌سازی نیست. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195**، Parent=194، تحلیل محدود مالکیت و reuse نوع ارسال و سازگاری با قفل قدیمی no-Types-of-Service؛ بدون پیاده‌سازی/DB/فرانت. نتیجه را برای تصمیم Architect برگردانید. سرفصل TB-194 در rules به25 تصحیح شد؛16 قبلی حفظ است. سایر gateهای واقعی UI/زبان DB، taxonomy/cutoff، مبنای مبلغ/تعداد گروه، carrier/funder، rollout باز؛ TB-185 متوقف. یادداشت‌های زیر تاریخچه‌اند.


> **وضعیت جاری پس از نتیجهٔ 194:** تسک **TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194** (Parent=193-R1) اجرا و نتیجهٔ آن
> از Bridge ارسال شد؛ وضعیت اعلام‌شده **PARTIAL** است فقط به‌دلیل عبور از سقف 45 دقیقه‌ای، نه به‌دلیل نقص اثبات.
> تمام واقعیت‌های فنی الزامی اجرا شد: 7 تست جدید (4 API + 3 ذخیره‌سازی/رقابت) و کل مجموعهٔ Shipping **616/616** روی
> PostgreSQL 17 یک‌بارمصرف؛ رقابت settings-vs-rate با barrier واقعی، دقیقاً یک برنده/یک تعارض، revision=2، نبود
> حالت ترکیبی و tracker خالی بازنده. شواهد در
> [RESULT.md](../../architecture/evidence/TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194/RESULT.md). این یادداشت پذیرش
> Architect نیست؛ منتظر بررسی مستقل بمانید و هیچ تسک بعدی خودکار صادر نمی‌شود. اتصال حامل، هزینه/تأمین مالی
> رایگان، اجرای قیمت، UI و فعال‌سازی همچنان بازند؛ TB-185 متوقف است.

> **وضعیت جاری پس از 193-R1:** برنامهٔ هفتگی draft با اصلاح اثبات رقابت **ACCEPT** شد؛ [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1/ARCHITECT-REVIEW.md). checkpoint `fce7fab3082435eb9fb92092b1b885a73bf98335`؛ شرط دو read از revision=1، دقیقاً یک برنده/یک تعارض، revision نهایی2 و نبود حالت ترکیبی/تغییر معلق بازنده از source بررسی شد. 609/609 و پنج اجرای متوالی گزارش Worker است، نه اجرای مستقل Architect. عبارت45دقیقه در نتیجه اشتباه نگارشی است؛ سقف واقعی R1 بیست دقیقه و زمان گزارش‌شده8.5دقیقه است. تنها تسک بعدی **TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194**، Parent=193-R1، صرفاً ذخیرهٔ مستقل پیش/پس‌کرایه و دستی/API حامل/رایگان در ریشهٔ draft؛ نه اتصال حامل، محاسبهٔ واقعی یا اثر پرداخت. انتخاب رایگان جای ثبت هزینهٔ واقعی و تأمین‌کنندهٔ صریح آن را نمی‌گیرد. تعرفه‌ها/جغرافیا/زبان‌ها/برنامهٔ هفتگی با تغییر حالت حذف نمی‌شوند. اتصال Admin به دادهٔ واقعی و پذیرش UI، taxonomy/cutoff، مبنای مبلغ/تعداد گروه، carrier و تأمین مالی/فعال‌سازی/استقرار بازند؛ TB-185 متوقف است. تسک194 را صادر کنید و تا نتیجهٔ همان صبر کنید. یادداشت‌های زیر تاریخی‌اند.

> **وضعیت جاری پس از بررسی 193:** برنامهٔ هفتگی پیاده شده ولی Architect = **PARTIAL_PENDING_DETERMINISTIC_CONCURRENCY_PROOF**؛ [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193/ARCHITECT-REVIEW.md). تست رقابت فعلی قبل از read شروع را هماهنگ می‌کند، موفقیت هر دو را می‌پذیرد و tracker بازنده را نمی‌سنجد؛ ادعای گزارش دربارهٔ این تست پذیرفته نشد، نقص production هنوز اثبات نشده است. تنها تسک بعدی **TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1** (Parent=193) برای اصلاح همان تست/گزارش، نه فیچر جدید؛ 194 صادر نشده. 609/609 گزارش Worker است و اینجا اجرا نشده. پذیرش190/191-R1/192-R1 حفظ؛ TB-185 متوقف و فعال‌سازی محصول همچنان باز است. وضعیت‌های پایین تاریخی‌اند.

> **وضعیت جاری Architect پس از 192-R1:** ارائهٔ چندزبانهٔ مالک‌محور **با R1 پذیرفته شد**؛ [پذیرش مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1/ARCHITECT-REVIEW.md). checkpoint `0cd2ced0df02730a75300fa8fc2bacf5a76db8ac`، کد `df743ddf160e45c64a62998389c0a04477ce60cb`؛ 589/589 گزارش Worker و بررسی Architect از source/diff است، نه اجرای مستقل آزمون. تنها تسک بعدی **TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193** (Parent=192-R1): برنامهٔ هفتگی مشترک روش با ساعات مستقل هر روز، تعطیل/تنظیم‌نشدهٔ متمایز و timezone صریح؛ بدون جغرافیای تازه/تغییر legacy/وعدهٔ مشتری. منتظر نتیجهٔ همان تسک بمانید. اتصال واقعی UI/رجیستری فروشگاه، taxonomy/حالت‌ها/cutoff، مبنای مبلغ/اندازهٔ گروه و فعال‌سازی بازند؛ TB-185 متوقف است. یادداشت‌های پیشین تاریخچه‌اند.

> **وضعیت جاری پس از بررسی 192:** پیاده‌سازی ارائهٔ چندزبانه موجود است ولی Architect = **PARTIAL_PENDING_REQUIRED_PROOF**، هنوز ACCEPT نشده. [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192/ARCHITECT-REVIEW.md): دو تست جدید مستقیماً directory را اجرا کرده‌اند؛ شواهد presentation GET/PATCH واقعی، یک/دو/سه‌زبان، تغییر فعال/پیش‌فرض و مجوزها کامل نیست. تنها تسک بعدی **TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1** (Parent=192)، برای تکمیل شواهد و اصلاح توضیح/گزارش؛ نه قابلیت تازه. 579/579 گزارش Worker است؛ Architect اینجا آزمون اجرا نکرده. پذیرش‌های190و191-R1 پابرجا؛ 193 صادر نشده و TB-185 متوقف است. زبان‌ها همچنان از مرجع دیتابیس، بدون فهرست ثابت‌اند. وضعیت‌های پایین تاریخی‌اند.

> **وضعیت جاری Architect پس از 191-R1:** ذخیرهٔ ردیف‌های تعرفهٔ دستی **با اصلاحات R1 پذیرفته شد**؛ [پذیرش مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1/ARCHITECT-REVIEW.md). checkpoint `7588dd80d2f916dc35b46fd7f0e0bea7efeb4073`، کد `7fc3b0bc6352cadd6be3a2290ef55ef7dcd1c44d`؛ 577/577 و PostgreSQL گزارش Worker است، Architect اینجا اجرا نکرده. تنها تسک بعدی **TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192** (Parent=191-R1): نام/توضیح مالک‌محور روش و عنوان اختیاری گروه با languageId واقعی دیتابیس. منتظر نتیجهٔ آن بمانید. کاتالوگ Fulfillment تغییر نمی‌کند؛ زبان‌ها ثابت نیستند. سابقهٔ PARTIAL/تخلف Host در191 حفظ و انتساب شکست‌های Host همچنان تأییدنشده است. مبنای مبلغ/اندازهٔ گروه در اجرای قیمت، UI، taxonomy/حالت‌ها/تقویم و فعال‌سازی هنوز بازند؛ TB-185 متوقف است. یادداشت‌های pending/repair قبلی زیر تاریخی‌اند.

> **وضعیت جاری پس از بررسی 191:** نتیجهٔ Worker = PARTIAL؛ Architect = **REPAIR_REQUIRED**، نه صرفاً به علت زمان. [بررسی مستقل 191](../../architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191/ARCHITECT-REVIEW.md): CHECKهای nullable/سقف حداقل مبلغ/قالب ارز با دامنه برابر نیستند و رقابت هم‌زمان نرخ با ویرایش مناطق هنوز تست نشده است. تنها تسک بعدی **TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1**، Parent=191؛ منتظر نتیجهٔ آن بمانید. 574/574 Shipping گزارش Worker است؛ Architect آزمون اجرا نکرده. اجرای ممنوع Host.Tests و عبور 60 از45 دقیقه ثبت شد؛ ادعای pre-existing بودن 132 شکست Host **تأیید نشده** و مجوز اجرای دوباره/اصلاح Host نیست. پذیرش 190 پابرجاست؛ 191/باقی Admin/Storefront/استقرار هنوز بازند. وضعیت‌های پایین checkpoint تاریخی‌اند.

> **وضعیت جاری Architect — پس از بررسی R2:** پایهٔ **190 + R1 + R2 پذیرفته شد، فقط برای پیش‌نویس مالک و گروه مناطق**؛ [پذیرش مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2/ARCHITECT-REVIEW.md). checkpoint بررسی‌شده `4895ff8888a0bc920d9b030525273bd9bf7efca8` و کد R2 `b29b228e2c49d042984a32ce507b35c30c17098f` است. 558/558 و PostgreSQL گزارش اجرای Worker است، اینجا دوباره اجرا نشده. تنها تسک بعدی **TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191** (Parent=190-R2)، برای ردیف‌های شرط/هزینه/مدت تحویل زیر group_id پایدار است؛ منتظر نتیجهٔ آن بمانید و تسک موازی/تکراری صادر نکنید. مقداردهی واقعی کالاهای گروه و مبنای تخفیف/مالیات در اجرای قیمت، اتصال UI، حالت‌ها/تقویم/taxonomy و فعال‌سازی هنوز بازند. یادداشت‌های pending/blocked قدیمی زیر تاریخچه‌اند؛ TB-185 متوقف می‌ماند.


> **آخرین وضعیت پس از 190-R2 (نتیجهٔ Worker، بدون پذیرش Architect):** گارد تراکنش به مرز دقیق مجاز اصلاح شد، جداسازی اتصال با پارس واقعی اثبات شد و مسابقه/لغو/برگشت روی PostgreSQL واقعی اجرا شد. Worker اعداد اندازه‌گیری‌شدهٔ خود را جدا از تاریخ 538/1/539 منتشر کرده است. این خط پذیرش Architect، تکمیل محصول یا فعال‌سازی نیست؛ بازبینی مستقل R2 لازم است و تسک بعدی صادر نشده است. [نتیجهٔ R2](../../architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2/RESULT.md).

> **آخرین وضعیت پس از 190-R1:** Worker نتیجهٔ BLOCKED با 538 pass / 1 fail گزارش کرد؛ اصلاح null/preflight و تراکنش دو‌مرحله‌ای بررسی شد، اما پذیرش نهایی صادر نشده است. تصمیم Architect: حفظ همان تراکنش مجاز و اصلاح محدود گارد قدیمی، بدون migration تازه. هم‌زمان شواهد race و اتصال آزمایشی باید دقیق شوند. [بررسی R1](../../architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1/ARCHITECT-REVIEW.md). تنها تسک بعدی **TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2** است؛ نتیجهٔ آن را بررسی کنید. اشاره‌های زیر به انتظار R1/190 تاریخی‌اند. نرخ/فرانت/فعال‌سازی و پذیرش کامل محصول همچنان باز است.

> **وضعیت جدید پس از نتیجهٔ 190 — 2026-10-22:** نتیجه دریافت و کد تا `b84877f4f633714989d6458f2be9a7aeda59f8f5` بررسی شد؛ **ARCHITECT = REPAIR_REQUIRED**. عبارت‌های «190 منتظر نتیجه» در checkpoint اولیهٔ زیر تاریخی‌اند. سه مانع: null گروه‌ها از validator عبور می‌کند، preflight شناسهٔ گروه‌ها پیش از mutation کامل نیست، و swap ترتیب با UNIQUE فوری سازگار نیست. [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190/ARCHITECT-REVIEW.md). فقط **TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1** برای اصلاح محدود آماده/صادر می‌شود؛ منتظر نتیجهٔ آن بمانید، تسک 191 صادر نشده است. نرخ/فرانت/فعال‌سازی/پذیرش PostgreSQL هنوز بازند. Worker گزارش 526 تست سبز دارد؛ اینجا آزمون اجرا نشده است.


> **نقطهٔ شروع جاری Shipping بعد از پاک‌شدن چت.** این سند بر اشاره‌های قدیمی «کار بعدی» در ریکاوری 2026-10-16، START-HERE و تاریخچهٔ فازهای عمومی اولویت دارد. تاریخچه حذف نشده است.
> این commit فقط مستندسازی/ریکاوری است؛ هیچ قابلیت، مهاجرت، پرچم یا پذیرش تولید را اجرا نمی‌کند.

## ۱. وضعیت دقیق همین لحظه

| مورد | مقدار |
| --- | --- |
| Repository / branch | `mrnikiemami-code/Tooba` / `main` |
| Protocol / channel / worker | `BRIDGE-WAKE-V1` / `tooba-main` / `tooba-worker-01` |
| Program جاری | `TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY` |
| آخرین نتیجهٔ بررسی‌شده و پذیرفته‌شده | `TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189`، پذیرش از بررسی کد/شواهد؛ نه E2E فروشگاه |
| HEAD مشاهده‌شده پیش از این مستندسازی | `676b7efcb9d4f402e662d5f955e82a33b252c835`؛ همیشه HEAD را دوباره fetch کنید |
| تنها تسک صادرشده و منتظر نتیجه | `TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190` |
| Parent تسک 190 | `TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189` |
| Mode تسک 190 | `BOUNDED_OWNER_ISOLATED_METHOD_DRAFT_AND_REGION_GROUP_PERSISTENCE` |
| وضعیت 190 | **ISSUED / AWAITING_WORKER_RESULT — نتیجه دریافت نشده؛ پیاده‌سازی/پذیرش آن ادعا نمی‌شود** |
| وضعیت claim/اجرای فعلی Bridge | **UNKNOWN**؛ از نبود commit نتیجه نگیرید Worker شروع نکرده است |
| کار بعدی Architect | دریافت نتیجهٔ همان 190، بررسی diff/شواهد، سپس ACCEPT / REPAIR / BLOCK و فقط یک تسک بعدی |
| فعالیت خودکار بعدی | `NONE`؛ تسک 191 یا تکرار 190 خودکار مجاز نیست |
| وضعیت محصول | Admin جدید هنوز کامل و یکپارچه نیست؛ Storefront/استقرار بعد از پذیرش Admin |
| وضعیت اجرای دیتابیس واقعی | برای 188/189 اثبات نشده؛ schema جدید 190 هنوز نتیجه ندارد |
| عملکرد عمومی Storefront | TB-185 **PAUSED**؛ PERF-CRIT-001 بسته نشده است |

**درخواست آخر مالک:** «ریکاوری را همین الان با کارهای انجام‌شده و کارهای بعدی در ریپو ثبت کن تا چت از بین رفت آماده باشی». این سند اجرای همین درخواست است، نه تسک پیاده‌سازی تازه.

## ۲. پروتکل بازیابی و ادامه؛ بدون تکرار کار

1. `AGENTS.md`، همین سند، تصمیم‌های محصول، مهارت مرتبط و HEAD تازه را بخوانید.
2. روی main، منشأ و diff تغییرات تازه را بررسی کنید؛ SHAهای نوشته‌شده checkpoint تاریخی‌اند، نه مجوز reset.
3. ابتدا مشخص کنید آیا نتیجه/رسید 190 رسیده یا همان تسک در Bridge Pending/Claimed است. یک بار بررسی وضعیت مرتبط با تسک مجاز است؛ polling دائمی Worker idle ممنوع.
4. اگر 190 هنوز فعال/بی‌نتیجه است، هیچ تسک جدید، re-claim، clone task یا تسک هم‌زمان صادر نکنید. اگر نتیجه رسیده، کد/شواهد همان نتیجه را بررسی کنید؛ Worker PASS برابر Architect ACCEPT نیست.
5. Parent، Task-ID، Program، Mode، Expected-Base-SHA، receipt/evidence و Result envelope را عین قرارداد حفظ کنید. نام هر فایل باید `<Task-ID>.task.md` باشد؛ فایل واقعی دانلودی و قابل preview، نه فقط متن inline.
6. قبل از اجرای کد، HEAD/origin/main/expected base و dirty/stashes را مطابق تسک بررسی کنید؛ mismatch/divergence را با reset/stash/rebase/force حل نکنید.
7. فقط یک تسک محدود، حداکثر **45 دقیقه HARD**؛ فرمان build/test حداکثر 180 ثانیه. توقف و گزارش PARTIAL بهتر از overrun/حلقهٔ تست است.
8. فایل‌های تاریخی `docs/ai/tasks` و نسخهٔ issued زیر، مجوز اجرای خودکار نیستند؛ Worker فقط تسک واقعاً claim‌شده از Bridge را اجرا می‌کند.

### نکتهٔ مهم دربارهٔ این commit مستندات و بیس 190

نسخهٔ اولیهٔ صادرشدهٔ 190 بیس `676b7efcb9d4f402e662d5f955e82a33b252c835` دارد. به‌روزرسانی ریکاوری main را فقط در فایل‌های مستندات جلو می‌برد. آن نسخهٔ اولیه برای حفظ متن دقیق در [آرشیو issued](issued/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190.task.md) ذخیره شده؛ **رسید claim یا نتیجهٔ اجرا نیست**.

پس از این commit، Architect نسخهٔ دانلودی همان Task-ID را با Expected-Base-SHA تازه refresh می‌کند؛ این **تسک جدید/دوم نیست**. فقط پیش از claim از نسخهٔ تازه استفاده شود. اگر نسخهٔ قبلی از قبل Claim/شروع شده، آن را دوباره enqueue/replay نکنید؛ وضعیت واقعی و قاعدهٔ بازیابی همان تسک را بررسی و در صورت تعارض STOP_FOR_ARCHITECT کنید. هیچ معافیت عمومی «هر تغییر docs قابل قبول است» ساخته نشده است. اگر چت پیش از refresh قطع شد، نخست فقط نسخهٔ بیسِ همان تسک را پس از بررسی HEAD و diff به‌روز کنید؛ scope و ID را خودسرانه عوض نکنید.

## ۳. مراجع الزام‌آور

- [تصمیم محصول Shipping Admin](../../architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md) — `SHIPPING-ADMIN-RULES-001`، اکنون بخش‌های 1–18؛ تصمیم اجرایی draft در خود 190 است تا رسید/نتیجه ثبت شود.
- [تجربهٔ نهایی مشتری](../../architecture/shipping/SHIPPING-STOREFRONT-UX-REFERENCE.md) — `SHIPPING-STOREFRONT-UX-001` و تصویر اصلی دیجی‌کالا؛ هدف UX، نه کپی برند یا الگوریتم فرضی.
- [سیاست مالی ترکیبی ارسال](../../architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md) — ضمیمهٔ مصوب بر متن قدیمی «بسته‌بندی تصمیم‌گیری نشده» اولویت دارد.
- [ریکاوری تاریخی با جزئیات Shipping / performance](SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md) — بخش‌های قدیمی next-task/چهار تب تاریخی‌اند.
- [مهارت‌های مخزنی](../../ai/AGENT-SKILLS.md) و `.cursor/skills/tooba-backend-feature/SKILL.md`، `.cursor/skills/tooba-frontend-feature/SKILL.md`.
- AMSC: `Analyze → Migrate → Structure → Certify`. `tooba-architecture-complete` فقط repair ویژه است، موج پنجم اجباری نیست.
- منابع معماری: `TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md`، `TMAR-architecture-locks.md`، `TOOBA-REFERENCE-MODULE-PATTERN.md`، `tmar-current-state.json` و `tmar-module-structure-manifests.json`.
- تصاویر baseline نامطلوب Admin: `docs/architecture/shipping/evidence/SHIPPING-ADMIN-UI-BASELINE-2026-10-21/`. فایل محلی upload ممکن است پاک شود؛ به نسخهٔ اصلی ذخیره‌شده در ریپو رجوع کنید.

## ۴. تصمیم‌های مالک که نباید دوباره گم یا اشتباه شوند

### محصول و ظاهر
- Admin اول؛ سپس Storefront و استقرار. ظاهر TB-186-R2 توسط مالک «اوکی» شد؛ این پذیرش فقط preview است، نه اتصال production.
- یک پنل مشترک با قواعد جدا برای فروشگاه/پلتفرم/فروشنده. در SingleStore انتخاب marketplace/seller مخفی و قابلیت مربوط در سرور غیرفعال باشد؛ دو موتور/دو پنل موازی نسازید.
- فهرست AppDataGrid موجود؛ نام خوانا و بدون GUID/کد فنی به‌عنوان عنوان مشتری. ویرایش صفحهٔ کامل با دو تب «اطلاعات روش» و «مناطق و هزینه‌ها»، نه مودال بلند چهار تب.
- نوع ارسال بالای نام روش. نمونه‌ها عادی/سنگین/فوق‌سنگین/سوپرمارکتی‌اند، نه فهرست جهانی بسته؛ مالکیت taxonomy و پیکربندی اجرایی هنوز تعیین/پیاده‌سازی کامل نشده است. Plus اگر مزیت عضویت است متعلق به Promotion است.
- روش‌ها، نام/توضیح و عنوان اختیاری گروه چندزبانه؛ زبان رابط مستقل از زبان محتوای درحال ویرایش. هر دو تغییر باید draft را حفظ کنند.
- زبان‌ها تعداد ثابت ندارند: یک، دو، سه یا بیشتر؛ فعال/پیش‌فرض/جهت/نام/ترتیب از دیتابیس مرجع می‌آیند. fa/en در preview فقط fixture است؛ زبان جدید را از کشور/مرورگر حدس نزنید.

### یک انتخاب جغرافیا، چند شرط/تعرفه
- گروه مناطق چند مقصد می‌گیرد؛ در هر سطح می‌توان توقف کرد. انتخاب ایران همهٔ فرزندان را می‌پوشاند؛ افزودن استان/شهر زیرش دوباره مجاز نیست.
- اگر گلپایگان انتخاب شده، افزودن اصفهان/ایران ممنوع؛ مقصد مستقل مانند قم مجاز. منع تکرار/والد/فرزند میان گروه‌های همان مالک/روش نیز برقرار است.
- **Browse با Add فرق دارد**: والد متعارض باید همچنان قابل پیمایش باشد تا به خواهر/برادر مستقل برسیم؛ فقط Add متعارض غیرفعال و علت خوانا باشد.
- geography برای هر ردیف نرخ/بسته‌بندی/برنامه دوباره وارد نمی‌شود.
- چند ردیف زیر یک گروه: وزن/تعداد/مبلغ کالاهای همان گروه؛ AND داخل ردیف، alternative میان ردیف‌ها، نه جمع نرخ‌ها یا انتخاب خودکار اولین/ارزان‌ترین ردیف.
- وزن ذخیره گرم، UI کیلو؛ بازه‌های صریح نیم‌باز `[min,max)`. نبود شرط با نبود نرخ یکی نیست؛ unmatched یا بیرون پوشش هرگز صفر/رایگان نیست.
- زمان آماده‌سازی فروشنده، مدت حمل و بازهٔ تحویل مشتری مفاهیم جدا هستند. min/max مدت تحویل preview را به PreparationDays+LeadDays تعبیر نکنید.
- هر روز هفته وضعیت فعال/تعطیل و ساعت مستقل دارد؛ مشترک‌بودن برنامه یعنی اشتراک مناطق همان روش، نه ساعت یکسان تمام روزها. تعطیلی نباید draft ساعت تایپ‌شده را پاک کند. دادهٔ legacy چندپنجره‌ای را به تک‌پنجره collapse نکنید.

### محاسبه و مالی
- پیش‌کرایه/پس‌کرایه محور مستقل از دستی/API/رایگان است؛ پس‌کرایه، COD بهای کالا نیست.
- ارز از StoreContext معتبر، مستقل از locale؛ پول و واحد تومان/ریال را حدس نزنید.
- API حامل نیازمند مرجع واقعی مبدأ/مقصد/مختصات و مشخصات بسته است؛ بدون اتصال و مستندات معتبر quote/زمان ساختگی تولید نکنید.
- رایگان پایهٔ روش با آفر ارسال رایگان Promotion جداست؛ آفر پوشش ممنوع را مجاز نمی‌کند.
- تصمیم مصوب: هزینهٔ ترکیبی حمل+بسته‌بندی+دیگر هزینه‌های ارسال در یک ردیف فاکتور، اجزا مستقل/گزارش‌پذیر. رایگان همهٔ اجزای هزینهٔ مشتری را صفر می‌کند؛ هزینهٔ واقعی و تأمین‌کنندهٔ صریح حفظ می‌شوند.
- این تصمیم اجرای مالی را اثبات نمی‌کند. PendingAllocation، مالیات/تسویه/بازپرداخت و consumer مالی معتبر را دور نزنید؛ پول/PSP/COD واقعی مجاز نشده است.
- گروه مشتری فقط seller-only نیست؛ یک فروشنده می‌تواند چند گروه کالا با مبدأ/شرایط متفاوت داشته باشد. هر گروه هزینه و روز/بازهٔ مستقل دارد؛ تغییر یکی دیگری را پاک نکند.

## ۵. آنچه واقعاً انجام و بررسی شده است

تمام counts زیر **گزارش اجرای Worker** هستند؛ Architect در این چت کد/diff/شواهد را مستقل خوانده، اما آزمون C# را اینجا دوباره اجرا نکرده است. PASS فنی با پذیرش صاحب محصول یا اجرای DB/browser واقعی برابر نیست.

| تسک | نتیجه/تصمیم Architect | خروجی واقعی و checkpoint |
| --- | --- | --- |
| TB-186 UI preview | بررسی/اصلاح شد | route مستقل `/admin/shipping-services/design-preview`؛ state حافظه، بدون business write؛ کد `1bd2ad26a2215f50d7507acd667bc703bc812e53`، checkpoint `4b7744eb0f7ba79d93e1ab063802bfa25aa1f14a` |
| 186-R1 | پذیرفته | browse در برابر Add، تعارض میان گروه‌ها، امنیت draft/owner، validation خام و first-submit؛ کد `b0ebe77985df14e771c869e093f3d203481d01a4`، شواهد `4a37697eef577963c08519616f44ff035ceb4cb1` |
| 186-R2 | ظاهر توسط مالک پذیرفته | ساعت مستقل روزها؛ 21 تست مدل؛ کد `65b2cfbd4e8e9e1ff64e4d319f2af2954977a2db`، شواهد `fefc38ce9ae9c8e741776782006b3041774a1279` |
| 187-W0 Analyze | پذیرفته با تصحیحات | نقشهٔ مسئولیت/قرارداد/legacy و D1–D7؛ کد production نداشت؛ checkpoint `e5c155ff910f817ce027023346c4483587cd9dc8` |
| 187-W1 | پذیرفته | 44 خطای معنایی در کاتالوگ/منابع canonical، اصلاح edition/tenant مجوزدهی؛ منبع `7e6b14798de7695716fee7567baa98d7654ca478`، checkpoint `a41e4274789848fc89ce7895dcce597d8bc997a7`؛ 29 تست جدید و 3 catalog guard طبق گزارش |
| 187-W2 Structure | پذیرفته برای ساختار | ShippingEndpointModule به root صحیح منتقل شد؛ guard و manifest غیرcertified؛ کد `59ce36250f109fec62572284049c607cd0f74c1f`، شواهد `0d9936f07045c9f7568a6e1f27336ac2d15ea7c2`؛ 74 focused |
| 187-W2-R1 | پذیرفته | اصلاح گارد تاریخی TB-076 با مجوز TB-079/080 برای Order shadow فقط‌خواندنی؛ exact مصرف‌کنندگان+تست منفی، بدون prod change؛ کد `6c54c8215540597f9e0a1ed630c3714ca54ae6a4`، شواهد `8fa3b7842985575ccb611eb7411be765caee00b1`؛ Shipping 382 و shadow Order 26 |
| 187-W3 | **رد شد؛ گواهی اولیه پذیرفته نشد** | Worker نبود IModuleCallTracer را optional خواند؛ skill صریحاً برای فراخوانی هم‌فرآیند هم لازم می‌داند. رکورد تاریخی حفظ/ابطال و توسط R1 جایگزین شد؛ checkpoint `2605344c10e6cd497349493357e9dcf3e6d666a4` |
| 187-W3-R1 | اصلاح پذیرفته | سه read خارجی در دو adapter با span واقعی، listener/correlation/exception/DI proof؛ کد `3b39c90ffe567cef06a5b9d048cfa92014085187`، شواهد `21d3900503475ce26052c33b97bb420cd8ec765c`؛ Shipping 407. baseline معماری موجود certified؛ نه محصول جدید |
| TB-188 | از بررسی کد پذیرفته | GET authoring-context با edition/ارز/زبان‌های فعال واقعی از Contracts؛ snapshot زبان metadata افزایشی؛ کد `f12e3e1ae2942e4dfa4ef1d8687d2dfacf9383cb`، checkpoint `5b74601fe6db10bde1575eb680b8104f2ac553a5`؛ Shipping 451 و LanguageLookup 6 |
| TB-189 | از بررسی کد پذیرفته | مالک سرورساخته Store/Marketplace/Seller و مجوز عملیات همان seller؛ دو GET و یک use case؛ کد `504df9888238ad85d0b183fcba4bc41ff5a1b5ec`، شواهد `0582151550634dfb591b60e36c77635fc82d1754`، checkpoint `676b7efcb9d4f402e662d5f955e82a33b252c835`؛ focused149، Shipping511 |
| TB-190 | **فقط صادر شده؛ نتیجه منتظر** | owner-isolated draft + گروه/مقاصد، CRUD، revision، ancestry و مهاجرت افزایشی آماده‌سازی‌شده؛ هیچ نتیجه‌ای را پیشاپیش PASS ندانید |

منابع هر ردیف: `docs/architecture/evidence/<Task-ID>/` و receipt در `docs/ai/tasks/<Task-ID>.task.md`، در صورت ثبت Worker. اصل task190 فعلاً در آرشیو issued است.

تخلفات/محدودیت‌های ثبت‌شده:
- 188 برخلاف محدوده build کامل solution اجرا کرد؛ در review ثبت و 189 تکرار نکرد. build گسترده ارزش پذیرش اضافی ندارد.
- W3 به‌اشتباه یک الزام tracing را debt اختیاری اعلام کرد؛ R1 به‌صورت صریح اصلاح و certificationHistory نگه‌داری شد. این تصمیم نمونهٔ مجوز تضعیف skill نیست.
- شواهد Source/DOM/fixture را به screenshot واقعی، مرورگر کاربر، PostgreSQL یا identity DB واقعی تبدیل نکنید. preview اولیه screenshot قابل‌اتکا نداشت؛ ادعای گالری تصاویر نشده است.
- متغیرهای عمومی «D1–D7 هنوز pending» در نتایج قدیمی پس از 188/189 باید با وضعیت تفکیکی زیر تفسیر شوند، نه دوباره از صفر تحلیل شوند.

## ۶. نقشهٔ مالکیت و مسیرهای فعلی کد

| مسئولیت | مرجع/مسیر فعلی |
| --- | --- |
| هویت سرویس/روش و binding، shared catalog و ترجمه‌های اصلی | Fulfillment.Contracts.Shipping؛ Shipping حق catalog/seed موازی ندارد |
| نرخ/پوشش/بسته‌بندی/برنامهٔ موجود | Shipping؛ `shipping` schema و ShippingDbContext؛ legacy موجود فعلاً global/بدون مالک جدید |
| هویت و ancestry مکان | Geography.Contracts.Places؛ رابطهٔ ParentAdministrativeAreaId می‌تواند عمق دلخواه داشته باشد |
| فعال/پیش‌فرض/جهت/نام/ترتیب زبان | Localization.Contracts.Ports.ILanguageLookup؛ bridge از LanguageDirectory روی LocalizationDbContext |
| بازار/ارز پیش‌فرض/کانال | StoreContext.Contracts.Current.ICurrentStoreCommerceContext |
| edition/tenant | BuildingBlocks ICurrentEdition/ICurrentTenant/ICurrentCommerceContext و resolver پلتفرم |
| actor و seller مجاز | canonical ISellerPanelAccess؛ candidate header به‌تنهایی مجوز نیست |
| مجوز مؤثر همان seller | IPlatformEffectiveAccessReader(actor, Seller, sellerPartyId)، permission موجود، فقط GlobalWithinOwner و بدون DeniedByCeiling |
| قیمت پذیرفته‌شدهٔ مشتری/سفارش/پرداخت | Order/Payment و snapshot معتبر؛ draft/operational quote حق تصاحب آن را ندارند |

مسیرهای مهم:
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/`: context/owner query، model و port داخلی.
- `...Shipping.Infrastructure/Authoring/ShippingAuthoringContextSource.cs`: read از Contracts Localization/StoreContext، با tracing.
- `...Shipping.Endpoints/Admin/ShippingInstallationContext.cs`: helper مشترک edition/active-tenant، نه mode authority جدید.
- `.../Admin/ShippingAuthoringOwnerAuthorizer.cs`: admin owner gate و seller gate+مجوز همان owner با span AccessControl.
- `.../ShippingEndpointModule.cs`: ترکیب HTTP/ارائه؛ Host فقط registration/map.
- `...Shipping.Infrastructure/Rates/{GeographyDestinationScopeValidator,FulfillmentShippingMethodOptionValidator}.cs`: سه call خارجی traced؛ tracer اجباری است.
- `...Shipping.Tests/Certification/ShippingRequestClassificationTests.cs` و `docs/architecture/shipping/shipping-request-classification.json`: manifest اکنون requestمحور است؛ هر request می‌تواند چند route داشته باشد.
- `...Shipping.Tests/Compatibility/OperationalQuoteCompatibilityTests.cs`: گارد سه نقش مجاز Order + محتوای مصرف، تست منفی اجرای غیرمجاز.
- `...Shipping.Tests/Observability/ShippingModuleCallTracingTests.cs`: listener واقعی، parent/correlation، exception/cancellation/PII.

APIهای جدید فعلی:
- `GET /v1/admin/shipping-services/authoring-context`: 188، edition/defaultCurrency/active contentLanguages/defaultContentLanguageId؛ پارامتر override ندارد.
- `GET /v1/admin/shipping-services/authoring-owner`: Store/Marketplace scope مجاز، بدون tenant داخلی در پاسخ.
- `GET /v1/seller/shipping-services/authoring-owner`: فقط Marketplace و seller مجاز با fulfillment.view همان مالک؛ SingleStore پیش از seller resolution با 404 بسته می‌شود.
- 189: 19 route، 18 request، 16 request validator، 2 NO_VALIDATOR_REQUIRED، 6 nested validator؛ اعداد snapshot‌اند، guard باید مجموعهٔ واقعی را اثبات کند نه فقط count.
- خطاها: 44 legacy همان 400/Business/Warning؛ configuration_unavailable =503؛ owner_scope_denied=403؛ owner_scope_unavailable=404؛ descriptor/resource یک مالک و fallback canonical.

## ۷. وضعیت تصمیم‌های D1–D7 و برنامهٔ وابستهٔ ادامه

| موضوع | وضعیت و آنچه هنوز لازم است |
| --- | --- |
| D1 مالک | authority model و read gate در 189 موجود؛ **stored owner isolation و rule writes هنوز موجود نیست**؛ 190 برای شروع آن صادر شده |
| D2 taxonomy نوع ارسال | طرح اجرایی/مالکیت/config/ترجمه/اثر عملیاتی باز؛ مثال‌های UI را enum جهانی یا حدس از وزن نکنید |
| D3 گروه مناطق و legacy | قرارداد هدف مصوب؛ 190 draft/group/area و prepare-only migration را خواسته؛ نتیجه هنوز نداریم. legacy attribution/backfill نیازمند تسک و تصمیم صریح |
| D4 محاسبه/پرداخت | دستی/API/رایگان و پیش/پس مستقل مصوب‌اند؛ مدل نویسندگی و اجرای جدید کامل نیست. free-above legacy را خودکار Promotion/free mode نکنید |
| D5 برنامه/cutoff | ساعات مستقل روزها در preview؛ legacy operating windows موجود؛ closed/absent/cutoff و اتصال واقعی نیازمند طراحی/پیاده‌سازی محدود، بدون collapse داده |
| D6 زبان | 188 API فعال/پیش‌فرض/metadata از منبع DB را فراهم کرده؛ **اتصال UI واقعی و پذیرش runtime DB هنوز pending**؛ در Marketplace رجیستری نصب مشترک است، تنظیم per-seller ساخته نشود |
| D7 اجرای مهاجرت | 190 فقط آماده‌سازی SQL/مدل/مهاجرت افزایشی، نه apply مشترک؛ proof PostgreSQL و rollout واقعی هنوز task/gate جدا دارد |

ادامهٔ منطقی پس از **بررسی نتیجهٔ 190**، فقط برنامه و نه تسک اجرایی/مجوز خودکار:
1. رفع هر نقص محدود 190؛ تأیید مالکیت ذخیره‌شده، ancestry واقعی، revision/atomic replacement و SQL/محدودیت proof.
2. افزودن ردیف‌های شروط/تعرفه زیر group IDs پایدار: وزن/تعداد/مبلغ گروه، حمل/بسته‌بندی و مدت تحویل؛ AND/full-tuple overlap و بدون جغرافیای تکراری. مبنای مبلغ قبل/بعد تخفیف/مالیات باید صریح شود، نه حدس در کد.
3. تکمیل metadata/نام‌های چندزبانهٔ مالک، taxonomy صحیح، محور محاسبه و پرداخت، برنامه هفتگی/cutoff با منابع و legacy compatibility. ترتیب دقیق slice بر اساس نتیجهٔ 190 تعیین شود؛ scope پُرحجم را در یک task 45 دقیقه‌ای جمع نکنید.
4. اتصال UI مصوب به API واقعی، زبان‌های DB و حالت نصب؛ SingleStore بدون انتخاب marketplace، View بدون write، draft safety/RTL/LTR و read-after-write واقعی. از preview state/fixture به‌عنوان production store استفاده نکنید.
5. اثبات relational واقعی با محیط test-owned ایزوله و مهاجرت امن، مرورگر واقعی Admin، create/edit/reload/conflict/access-denied، بررسی کاربر. EF InMemory اثبات UNIQUE/transaction/concurrency PostgreSQL نیست.
6. سپس Storefront با گروه‌ها/هزینه/زمان‌های مستقل و authority معتبر؛ سپس rollout/استقرار با gateهای مالی و عملیاتی واقعی. نه تغییر پرچمِ پنهانی و نه ادامهٔ Home performance.

برآورد قبلی به مالک: حدود **6–9 تسک محدود پس از 189** برای تکمیل Admin، نه تعهد زمانی/تعداد قطعی. با هر نتیجه بازبینی شود؛ Storefront و deployment داخل این برآورد نبودند. چیزی برای حفظ این برآورد از acceptance حذف نشود.

## ۸. محدودهٔ دقیق تسک فعال 190

اصل صادرشده: [TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190.task.md](issued/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190.task.md).

- draft مستقل Shipping با binding موجود و immutable؛ نه catalog دوم و نه روش operationally active.
- گروه‌های مناطق با IDs پایدار، چند مقصد در هر گروه، منع duplicate/ancestor/descendant میان تمام گروه‌های همان draft.
- مسیر ancestry واقعی از Geography؛ GUID equality/نام/prefix و ancestors ارسالی client کافی نیست. admin areas تودرتو، city→area و serviceArea→city بررسی شوند.
- CRUD مشترک admin/seller با authority 189؛ view/manage جدا؛ full owner predicate در تمام read/list/update حتی در DB جداگانه.
- replace کامل region tree با revision واقعی و یک مرز atomic SaveChanges؛ خطای گروه آخر نباید گروه‌های قبلی را پاک/نیمه‌ذخیره کند.
- migration افزایشی فقط روی جدول‌های جدید draft/group/area و own-FK؛ snapshot تازه مجاز، migrationهای قدیمی byte-identical. بدون backfill/legacy attribution.
- SQL/model/concurrency/EF roundtrip proof و مرز InMemory شفاف؛ **عدم اجرای Postgres نباید به‌عنوان اثبات runtime معرفی شود**.
- نرخ/metadata کامل/ترجمهٔ مالک/حالت/تقویم/quote/activation/frontend در scope 190 نیستند.
- no shared DB/app mutation؛ Task 190 apply به `:5432/tooba_alpha` را مجاز نکرده است.

## ۹. خطوط قرمز معماری و ایمنی در هر ادامه

- هر قابلیت در ماژول صاحبش؛ foreign Application/Infrastructure/Domain/Endpoints، DbContext/DbSet/schema join/foreign FK/transaction مشترک ممنوع.
- Host composition/platform فقط؛ business/API ماژول/persistence را به Host برنگردانید.
- API module-owned، ISender/CQRS واقعی، FluentValidation با input provenance، Result/SemanticError/ApiResponseFactory، error descriptor تک‌مالک و resources/resolver canonical.
- هر call بین ماژول‌ها tracing canonical می‌خواهد؛ MediatR parent span یا precedent sibling جای IModuleCallTracer نیست.
- فرانت قبل هر تغییر مهارت frontend و استانداردش را بخواند؛ API/state/UI جدا، بدون موتور قواعد backend در مرورگر، design system و AppDataGrid موجود، frozen surfaces محفوظ.
- skill توسعه feature برای پیشگیری از debt است، مجوز AMSC مجدد یا recertification همه‌چیز نیست. Certify بدون proof همهٔ prerequisiteهای applicable PASS نشود.
- آزمون هدفمند، یک تعمیر deterministic و rerun محدود؛ نه حلقهٔ full build/test یا اصلاح نامرتبط برای سبزشدن.
- `:3000/:3419/:5088/:5432`، `tooba_alpha`، Redis/RabbitMQ، shared bin/obj/.next، stashes و dirty/untracked دیگران را دست نزنید.
- PID/URL preview ثبت‌شده تاریخی `127.0.0.1:3419/admin/shipping-services/design-preview` است؛ زنده‌بودن سرور/PID قدیمی را فرض نکنید.
- no reset/stash/rebase/force-push، no secret/credential logs و no dummy/probe به Bridge /api/results.
- user approval بصری، worker technical PASS، architecture certification، relational runtime proof و production release هر کدام gate مستقل‌اند.

## ۱۰. متن کوتاه برای شروع چت بعدی

«ریکاوری Tooba را از `docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md` بخوان. Admin Shipping اولویت دارد؛ 189 پذیرفته و 190 صادر شده و منتظر نتیجه است. قبل از تسک تازه نتیجه/وضعیت همان 190 و HEAD واقعی را بررسی کن. مسیر Home performance بعد از185 متوقف است. معماری تمیز frontend/backend، Contracts-only، زبان‌های دیتابیس و SingleStore/Marketplace را حفظ کن. فقط یک task دانلودی/preview در هر نوبت، بعد review نتیجه و task بعدی.»

## ۱۱. وضعیت واقعی TB-191 (2026-10-22)

- TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191 اجرا و نتیجه‌اش به Bridge ارسال شد: ردیف‌های نرخ دستی زیر
  `group_id` پایدار، بازه‌های نیم‌باز سه‌محوره، ارز trusted و اتمی بودن یک `SaveChanges` با نشانگر هم‌زمانی
  ریشهٔ مشترک. اثبات رابطه‌ای روی PostgreSQL دورریختنی همان تسک اجرا و کانتینر پاک شد.
- معنای ذخیره‌شده و دروازهٔ باز در `SHIPPING-ADMIN-RULES-DECISION.md` بخش ۲۱ ثبت شده‌اند؛ جزئیات اجرا در
  `docs/architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191/`.
- **دروازهٔ باز:** مبنای authoritative اندازه/مبلغ کالا حل نشده است. هیچ quote/ارزیاب/فعال‌سازی/انتشار/
  ویرایشگر کامل مجاز نیست تا Architect آن را صریح باز کند. Worker PASS تنها اثبات فنی است و Architect ACCEPT
  نیست؛ تسک بعدی خودکار نیست.

## ۱۲. وضعیت TB-191-R1 (2026-10-22)

- اصلاح هم‌ارزی قیدهای ذخیره‌سازی (R191-01) و اثبات اجرایی مسابقهٔ «نوشتن ردیف نرخ» با «جایگزینی درخت
  منطقه/حذف گروه» (R191-02) انجام و نتیجه به Bridge ارسال شد؛ جزئیات در
  `docs/architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1/`.
- قاعده‌ها در `SHIPPING-ADMIN-RULES-DECISION.md` بخش ۲۲ ثبت شده‌اند. تعریف قیدها/مهاجرت اصلاحی فقط روی
  دیتابیس دورریختنی همان تسک اجرا شد.
- **دروازهٔ باز:** مبنای authoritative اندازه/مبلغ کالا همچنان حل نشده است؛ quote/ارزیاب/فعال‌سازی/انتشار
  و ویرایشگر کامل مجاز نیست. Worker PASS اثبات فنی است، نه Architect ACCEPT؛ تسک بعدی خودکار نیست.

## 13. TB-192 / TB-192-R1 outcome pointer

TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192 delivered bounded persisted language-keyed owner
presentation (method name/description + optional group titles) with one additive migration and
executed relational/race proof (579/579 whole Shipping suite). This is not Admin/production
acceptance, not editor activation, and not module certification; configured-registry runtime/UI
acceptance remains a later integration gate. Runtime-Merchandise-Basis-And-Measurement-Gate stays
OPEN_NOT_IMPLEMENTED. Await Architect review; no automatic next task.

TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1 (worker, 2026-10-22) closed the R192-01/R192-02
proof gaps: real admin/seller presentation GET/PATCH through the real authorizer, validation pipeline,
handlers/projection and directory; one/two/three-plus dynamic-language matrix with deactivate/reactivate/
default-change/raw-text retention; selected clear and true no-ops; interface-culture invariance;
pre-dispatch denials and malformed-input preflight; and an independent raw PostgreSQL 17 storage matrix
on the two new tables plus retained-text reorder/rate-edit proof. Executed 589/589 whole Shipping suite;
production executable behavior unchanged (one XML remark corrected). Worker PASS only — not Architect
ACCEPT; no TB-193 and no automatic next task.

## 14. TB-193 outcome pointer (2026-10-22)

TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193 delivered the bounded D5 per-weekday draft operating
schedule: one owner-scoped schedule attached ONCE to the draft root, explicit validated timezone, exactly
seven unique ISO weekdays with explicit working/closed flags and independent same-day intervals,
remembered closed-day hours, truthful UNCONFIGURED versus configured-all-closed, whole-week preflight plus
one atomic SaveChanges on the shared root revision. Two new Shipping-owned tables plus one additive
migration; authorized admin/seller GET/PUT through the real owner authorizer; executed PostgreSQL 17
roundtrip, raw constraint (SQLSTATE + owned constraint) and schedule-vs-rate race proof; whole Shipping
suite 609/609. Legacy multi-window schedule rows and errors unchanged (74/74 legacy schedule tests).
Rules recorded in `SHIPPING-ADMIN-RULES-DECISION.md` section 24; details in
`docs/architecture/evidence/TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193/`.

**Open gates unchanged:** cutoff/holiday/capacity/customer-promise are NOT implemented; drafts remain
inactive configuration; Runtime-Merchandise-Basis-And-Measurement gate stays OPEN_NOT_IMPLEMENTED and
TB-185 stays paused. Worker PASS is technical proof only, not Architect ACCEPT; no automatic next task.

TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1 (worker, 2026-10-22) closed R193-01 by replacing the
weak race fact with the established real `SavingChanges` two-reader barrier and strict assertions:
both participants proven to arrive with the original root token 1, exactly one winner and exactly one
loser with the canonical `AuthoringDraftRevisionConflict`, revision advanced exactly once, the actual
winner's capability asserted present and the loser's absent in both orders, raw day/rate row counts
proving no hybrid, and the ACTUAL losing context proven empty and re-reading the winner. Test-only
repair plus evidence correction: no executable production, contract, schema or migration change.
Re-executed 609/609 whole Shipping suite; the repaired fact passed 5/5 consecutive focused runs.
Worker PASS only — not Architect ACCEPT; no automatic next task.

TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196 (worker, 2026-10-22, PARTIAL) delivered the additive
Fulfillment-owned operational shipping-type catalog: domain identity/code/sort/active/revision,
LanguageId-keyed translations, four Admin routes (/v1/admin/shipping-types) behind a new module-owned
independent fulfillment.view/manage gate, stable shipping_type.* errors/resources, one additive
migration, and a read-only Contracts IShippingTypeReader. Builds clean; 21 focused rule tests pass.
NOT executed inside the expired 45-minute timebox: real PostgreSQL migration/raw-SQL proof, route
harness denials, revision/unique-code races, module-trace assertion. Worker PARTIAL only - not
Architect ACCEPT; STOP_FOR_ARCHITECT; no automatic next task.
TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1 (worker, 2026-10-22, PARTIAL) repaired R196-01..05 in
source: presence-aware canonical body reader (authorize -> read -> ISender), no null filtering, parameterless
list plus a real Guid.Empty guard, whole-state store preflight with a true no-op (Revision and UpdatedAt
preserved) and stale-identical conflict, strict registry default cardinality with a new ambiguous-language
error, mandatory module-call tracing on the Localization read, and capability-first relocation to
Application/Shipping secondary axes with strict whitespace code rejection, typed error codes and exact
constraint identity matching. 43 focused tests + 26 existing structural guards pass. NOT executed: the
in-process route/authorizer denial matrix, real ActivityListener assertions, exact-constraint negative,
real ILanguageLookup adapter fixture, and all PostgreSQL/migration-chain/snapshot/race proof (R196-06
PENDING). Hard timebox was exceeded (~79 min). TB-196 remains PARTIAL_PENDING_RELATIONAL_SNAPSHOT_PROOF;
worker PARTIAL only - not Architect ACCEPT; STOP_FOR_ARCHITECT; no automatic next task.
TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2 (worker, 2026-10-22, PASS for scope) repaired ONLY the two
Domain boundary regressions found in R1: ShippingType.Create now uses the same inclusive 0..1000000 sort
validation as Update (sort 0 was wrongly rejected; baseline failure captured before the fix), and public
translation factories/mutators plus ShippingType.Create again enforce atomic local invariants (non-empty row
and language identity, non-blank name <=128, description <=512 with null as the only clear form, complete-pair
validation before assignment, no collection change on invalid/duplicate add, no invalid aggregate
construction) using argument-exception convention only - no Domain -> Contracts reference and no boundary
guard weakened. 54 focused rule tests pass; the existing Domain boundary guard passes by exact name; one
EF InMemory sort-zero create/read/activate regression passes and is not HTTP/PostgreSQL proof. Still pending:
route/validator set equality, delegate auth/body proof, real language matrix, real ActivityListener,
lookalike-constraint negative, and PostgreSQL schema/snapshot/race proof. TB-196 remains
PARTIAL_PENDING_API_TRACE_AND_RELATIONAL_PROOF; worker PASS only - not Architect ACCEPT; STOP_FOR_ARCHITECT;
no automatic next task.

TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3 (worker, 2026-10-22, PASS for bounded scope) executed the
real-delegate proof only: production `MapFulfillmentEndpoints` route builder (built, never started) invoking
the actual four `/v1/admin/shipping-types` `RequestDelegate`s through the real `ShippingTypeAdminAuthorizer`,
`ShippingTypeBodyReader`, MediatR validation pipeline, handlers, `ShippingTypeStore` over EF InMemory,
`LocalizationShippingTypeLanguageFacts` with the production tracer, `ApiResponseFactory` and the real
Fulfillment error catalog. Facts: authorized create/list/detail/activate/deactivate/reactivate lifecycle,
true no-op repeat preserving revision+UpdatedAt, stale conflict 409, immutable code, independent view/manage
grants, admin-panel rejection and invalid edition/tenant contexts blocking with zero authorization checks and
zero dispatch, unauthorized malformed body with zero stream reads, full lossless input rejection matrix with
pre-handler counters, exactly-four route/request/validator set equality with negative detection, and
parameterless list immune to query/culture. 42 focused tests pass in the Fulfillment.Tests project only.
EF InMemory only. Still PENDING_NOT_RUN: full multilingual registry lifecycle, ActivityListener, exact
constraint negative, and PostgreSQL relational/snapshot/race proof. TB-196 remains PARTIAL; worker PASS only
- not Architect ACCEPT; STOP_FOR_ARCHITECT; no automatic next task.

TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R4 (worker, 2026-10-22, PASS for the three repaired claims only)
repaired exactly the three overstated R3 evidence claims in the SAME endpoint test file, test-only: (A) the
no-op fact now sends an IDENTICAL NONEMPTY patch built from the actually stored translation under a mutable
test clock advanced +5 minutes, asserting revision and persisted UpdatedAt are exactly the pre-repeat value and
not the advanced instant, plus a stale nonempty conflict; (B) every shaped-invalid POST/PUT and the
Guid.Empty GET now assert the canonical root `validation.failed` and the exact stable child code under its
property (plus a 129-char overlength-name case), with a test-only post-validation arrival counter proving valid
requests arrive (Arrivals=1) while validation rejections do not (Arrivals=0), and malformed bodies kept
distinct as `shipping_type.body.malformed`; (C) all four real delegates are invoked with valid input and the
OBSERVED dispatched request type plus real-DI IRequestHandler/validator are matched per route, with a
same-four-route swapped-mapping negative. 45 focused endpoint tests pass; only Fulfillment.Tests built.
EF InMemory only. Still PENDING_NOT_RUN: full dynamic registry lifecycle, real listener/DI verification,
exact-constraint lookalike and PostgreSQL relational/snapshot/race proof. TB-196 remains PARTIAL; worker PASS
only - not Architect ACCEPT; STOP_FOR_ARCHITECT; no automatic next task.
TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5 (worker, 2026-10-22, PASS for this bounded nonrelational slice only)
test/evidence only, one new Fulfillment test file: (A) the ACTUAL `FulfillmentModule.AddServices` entry is
invoked and only data boundaries are replaced (Npgsql `FulfillmentDbContext` option action -> EF InMemory,
`IClock` -> test clock, `ILanguageLookup` -> controlled fixture); the module's own store/reader aliases,
`LocalizationShippingTypeLanguageFacts`, canonical `ModuleCallTracer`, handlers and validator resolve from real
DI with one descriptor each and a single owned error-catalog contributor, and the migrator stays registered but
never resolved or executed. (B) the registered adapter now drives the registry lifecycle matrix: one active
non-fa default with no fa fallback, two active mixed RTL/LTR languages with byte-identical untouched text,
independent `null` description clearing and `[]` preservation, declared non-first default with an inactive row,
deactivate/reactivate raw-text retention with a canonical `language_invalid` edit rejection, default-switch
`name_required` gate before activation, eight invalid registries rejected with exact codes and zero persisted
rows, and culture-invariant raw language ids. (C) a real `ActivityListener` on `ToobaTelemetry` proves exactly
one `module.Fulfillment.Localization.ListLanguages` child span per real lookup with source/target/operation/
request-kind/correlation tags, parent+shared trace, restored ambient parent, one stop per call, business
validation after a successful lookup, exception-type-only error instrumentation, and identical exception/
cancellation identity with no sensitive names, ids, codes, tenant/currency or message exported. 22 focused
tests pass in Fulfillment.Tests only. EF InMemory + controlled Contracts registry fixture only. Still
PENDING_NOT_RUN: exact-constraint lookalike negative and PostgreSQL historical-schema/snapshot/constraint/
unique/revision-race proof. TB-196 remains PARTIAL; worker PASS only - not Architect ACCEPT;
STOP_FOR_ARCHITECT; no automatic next task.

TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6 (worker, 2026-10-22, BLOCKED on a real PostgreSQL defect)
test/evidence only on a disposable postgres:17-alpine at 127.0.0.1:55436 (role tb196, DBs fulfillment_tb196 and
fulfillment_tb196_model), parsed and live-proven before DDL; legacy chain (10 migrations) applies cleanly, then the
checked-in latest migration 20261022101415_ShippingTypeCatalog FAILS 5/5 with SQLSTATE 42703 `column "code" does not
exist`: its scaffolded CREATE TABLE uses quoted PascalCase identifiers ("Code", "SortOrder", ...) while the model and
its own CHECK body use snake_case (char_length(code) ...), i.e. scaffolding inconsistent with the production naming
convention used by ToobaNpgsql.ConfigureModuleContext. No production/migration/designer/snapshot/EF mapping file was
touched (hashes recorded). Consequently Gate 1E additive Up/Down, Gate 2 storage matrix, Gate 3 revision/unique races
and mid-save rollback/cancellation, and Gate 4 exact-constraint ownership are committed but UNEXECUTED. Historical
legacy manifest versus model projection was executed (24 missing + 28 unexpected lines) but parity is NOT claimed:
the differences are dominated by migration-time defaults, hand-written raw-SQL names, and a harness projection
defect; snapshot agreement is likewise recorded as UNPROVEN rather than drift. The R5 single-active non-fa fixture
was corrected (fa known-inactive, ar the only active default) and its exact test passes. Container removed, 55436
verified free. TB-196 is BLOCKED pending an authorized migration repair decision; worker BLOCKED is not Architect
ACCEPT and grants no repair or scope authority; STOP_FOR_ARCHITECT; no automatic next task.

R6 classification note (worker, same result): the legacy manifest comparison was finally classified with explicit
normalization: 3 SUBSTANTIVE legacy discrepancies, all pre-existing and none involving a shipping_type object —
(1) index ix_outbox_messages_pending is declared in the model/snapshot/designer but no migration ever creates it;
(2) ck_shipping_methods_code_canonical and (3) ck_shipping_methods_option_code_canonical exist in the migrated
schema from the hand-written 20261015120000 raw SQL but are not declared in the model. Plus 5 migration-time
DEFAULT differences and 10 constraint/index naming differences, reported explicitly instead of ignored. Gate 1D
therefore blocks regardless of the migration defect, and the teardown Down comparison is a no-op equality (the
latest migration never applied), not Down proof. Snapshot-model agreement remains UNPROVEN (the differ over an
initialized ModelSnapshot.Model reported 209 operation-level differences under a non-design-time path).

TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7 (worker, 2026-10-22, PASS for the bounded repair slice)
The design-time factory now reuses ToobaNpgsql.ConfigureModuleContext (snake_case + module history) with a
design-time-only placeholder and no connection. Only the latest unaccepted migration 20261022101415_ShippingTypeCatalog
was amended to the canonical identifiers (columns shipping_type_id/code/sort_order/is_active/revision/created_at/
updated_at and shipping_type_translation_id/shipping_type_id/language_id/name/description; pk_shipping_types;
pk_shipping_type_translations; fk_shipping_type_translations_shipping_types_shipping_type_id) with the owned index and
CHECK identities preserved; its Designer and only the two type entity/relationship snapshot blocks were aligned, and
every older migration/designer and unrelated snapshot block is byte-unchanged. Factory-vs-runtime type-surface parity
is asserted green (6/6 offline tests). The R6 full-schema fixture is now fail-fast: it stops with a concrete diagnostic
before model projection or latest Up when snapshot equivalence is unproved or substantive legacy discrepancies exist,
and marks DownExecuted only after a real latest history row and real Down (it currently stops at 191 unproven model
differences and records "latest Up applied=False; Down NOT executed"). An independent isolated proof on a fresh
disposable 127.0.0.1:55436 postgres:17-alpine (role tb196, db fulfillment_tb196) applied the ACTUAL historical chain,
applied the corrected migration once, verified only the two owned tables were added with no non-lowercase column and
with the canonical PK/FK/index identities, smoke-read a created type through a NEW context, then ran the real Down and
restored the legacy manifest exactly (1/1 passed). Container removed; 55436 has no listener. Legacy/snapshot parity,
the 3 substantive legacy discrepancies and the storage/constraint/race/atomicity battery remain OPEN, so TB-196 stays
BLOCKED_PENDING_LEGACY_SNAPSHOT_AND_RELATIONAL_ACCEPTANCE; worker PASS is not Architect ACCEPT; STOP_FOR_ARCHITECT;
no automatic next task.
### Worker status pointer — R8 (2026-10-22T17:54Z claim, metadata-only slice)

`TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8` is COMPLETE (worker `PASS`, metadata alignment only): the canonical
`FulfillmentDbContextFactory` model was regenerated through the existing EF design-time scaffolder and the resulting
metadata was written into ONLY `FulfillmentDbContextModelSnapshot.cs` and the
`20261022101415_ShippingTypeCatalog.Designer.cs` target model; the executable Up/Down, the context/factory, every
older migration/designer and all business code are byte-identical. The real `IMigrationsModelDiffer` on initialized
relational models now reports 0 operations for snapshot-vs-current-model and 0 for latest-TargetModel-vs-current-model
(baseline was a derived 191 naming-only operations: 135 column renames, 24 index renames, 15 PK and 1 FK rename pairs),
and a normalized relational-description regression guards both artifacts (3/3 new tests; 28/28 with the R7/R5 offline
sets). This proves metadata alignment, NOT equality with the historically migrated schema: the R6 legacy discrepancies
(missing outbox pending index in migrations, two migration-only `shipping_methods` checks, column defaults and names)
and the storage/constraint/race/atomicity battery remain OPEN and unexecuted. TB-196 stays
BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE; worker PASS is not Architect ACCEPT; STOP_FOR_ARCHITECT;
no automatic next task.
### Worker status pointer — R9 (2026-10-22T18:07Z claim, historical-schema reconciliation analysis only)

`TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9` is COMPLETE (worker `PASS`, analysis only, no implementation and no
database). The saved R6 manifests were re-read and re-counted (16 `missing-in-actual` + 17 `unexpected-in-actual`,
fully classified as 18 objects): 3 substantive (missing `ix_outbox_messages_pending`; two DB-only
`ck_shipping_methods_*_canonical` CHECKs), 5 column defaults (`items.quantity_packed`, `items.quantity_processing`,
`shipments.provider_metadata_version`, `shipments.shipping_method_code`, `shipments.shipping_method_label`), and 10
naming-only objects (1 FK + 9 indexes) with full shape verified equal. R8's 191 metadata renames are explicitly
excluded. All 10 model-side names were re-verified against the post-R8 snapshot, so R8 did not invalidate the model
facts. Recommended FIRST slice is the single additive, data-free outbox pending index migration
(`CREATE INDEX IF NOT EXISTS`, Down `DROP INDEX IF EXISTS`); checks/defaults/names are proposed as zero-DDL
"model follows DB" follow-ups gated on architect decisions D1-D5 recorded in
`docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9/RECONCILIATION-PROPOSAL.md`. Live-DB
presence of the outbox index and any domain-bypassing write path remain UNKNOWN_PENDING_EXECUTION. TB-196 stays
BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE; worker PASS is not Architect ACCEPT; STOP_FOR_ARCHITECT;
no automatic next task.
### Worker status pointer — R10 (2026-10-22T18:21Z claim, pending-outbox index slice)

`TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10` is COMPLETE (worker `PASS`). ONE additive migration
`20261022182208_AddFulfillmentOutboxPendingIndex` adds `fulfillment.ix_outbox_messages_pending`
(non-unique btree on `occurred_at`, predicate `processed_at IS NULL AND dead_lettered_at IS NULL`) via transactional
`CreateIndex`, with `Down` = its `DropIndex` only (no `IF NOT EXISTS`, no `CONCURRENTLY`). Its Designer `TargetModel`
was generated from the unchanged canonical design-time model; the snapshot, the context/factory, the
ShippingTypeCatalog migration/designer and all older artifacts are byte-identical. Offline: Up/Down operation universe
is exactly the index pair, and snapshot-vs-canonical-model plus NEW-TargetModel-vs-canonical-model are both 0
operations. Executed on a task-owned `postgres:17-alpine` at `127.0.0.1:55436` (chain applied through
`20261022101415_ShippingTypeCatalog`, three seeded outbox states): Up added exactly one index + its history row with
byte-identical normalized manifest otherwise, complete row values preserved, genuine Down restored the baseline
manifest/rows and removed the history row, reapply + migrate-to-same-target produced no churn, and both a wrong-shape
and an exact-shape preexisting same-name index failed closed with `42P07` and no history row/adoption. Container removed;
55436 has no listener. CHECK/default/naming reconciliation and the storage/relational battery remain OPEN, so TB-196
stays BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE; worker PASS is not Architect ACCEPT;
STOP_FOR_ARCHITECT; no automatic next task.
### Worker status pointer — R11 (2026-10-22T18:39Z claim, existing CHECK + historical object-name model alignment)

`TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11` is COMPLETE (worker `PASS`). The canonical Fulfillment model now
describes TWELVE objects that already existed in the database: the two `shipping_methods` CHECKs from
`20261015120000` (verbatim SQL, protection preserved, not replaced by the stricter domain validator) and ten historical
physical names pinned with `HasDatabaseName`/`HasConstraintName` (1 FK + 9 indexes, shapes verified unchanged: ordered
columns, uniqueness, partial filters, FK principal and `ON DELETE CASCADE`). Only the current snapshot and the R10
`20261022182208_AddFulfillmentOutboxPendingIndex.Designer.cs` TargetModel were regenerated with the unchanged factory;
the R10 executable Up/Down, every older migration and every older designer (including ShippingTypeCatalog) are
byte-identical, with no new migration id or history row. The R8 parity regression now selects the ACTUAL latest
migration (asserted = R10) instead of pinning obsolete equality on ShippingTypeCatalog, keeping real
initializer/differ zero-operation and normalized-semantics assertions. Offline: snapshot-vs-model 0 and latest-R10-
TargetModel-vs-model 0, plus a negative demonstration that renamed/missing CHECK or object names are detected, not
filtered. Executed on a task-owned `postgres:17-alpine` (127.0.0.1:55436, DBs `fulfillment_tb196` +
`fulfillment_tb196_model`): the real chain applied through R10 versus the current model projected by
`GenerateCreateScript` yields residual **0** and exactly the **10 raw default lines** for the five known defaults; the
primary manifest re-read byte-equal and the R10 pending index is present on both sides. Defaults were NOT touched
(no `HasDefaultValue`/value-generation/insert-behavior change) and stay visible as differences. Container removed;
55436 has no listener. TB-196 stays BLOCKED_PENDING_DEFAULTS_AND_RELATIONAL_ACCEPTANCE; worker PASS is not Architect
ACCEPT; no customer-facing or deployed-schema claim; STOP_FOR_ARCHITECT; no automatic next task.
## Worker pointer — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12 (PARTIAL, process limits declared)

R12 described the five remaining historical server defaults on the current model for exactly the five existing
Fulfillment properties, keeping explicit-write semantics: `ValueGenerated` stays `Never` and before/after save
behavior stays `Save`, proven by a real EF/Npgsql INSERT harness on a disposable historical target (zero/empty and
non-default values for all five, explicit column list, no `DEFAULT` token, new-context readback unchanged, explicit
NULL still rejected with 23502, raw omit-column writers still receiving the server defaults). Baseline was captured
before the mapping change and the post-change cases are asserted equal to it. The two `numeric(18,6)` properties use
`HasDefaultValueSql(""0"")` instead of typed `HasDefaultValue` because typed decimal formatting emits `0.0` (a
different default expression) and expression normalisation is forbidden; the three others use typed
`HasDefaultValue`. Metadata was regenerated with the existing tooling into only the snapshot and the R10 designer
`BuildTargetModel`; the scaffold DDL was never applied or retained; every executable migration and older designer is
byte-identical (R10 `C6600940CBEF7D07`, ShippingTypeCatalog `979328491371CD0F`/designer `50098BD09979F5F8`).
On a fresh `postgres:17-alpine` (127.0.0.1:55436) the real unchanged chain through R10 produced a 218-entry own-schema
manifest identical in both directions to the 218-entry manifest of the current canonical model projected by
`GenerateCreateScript` — **residual 0, strict `Assert.Equal` on the whole manifest, the five defaults present on both
sides**, with migration-history objects excluded by exact (schema, table) identity for columns, constraints and indexes.
The old residual classifier was replaced by that strict equality contract. Focused suites: 8 passed / 0 failed.
Declared process deviation: 4 harness-only corrections (stated budget was 1), so the result is reported PARTIAL
despite complete green evidence; the 35-minute timebox itself was respected (~9 min). Container removed and 55436
has no listener. Whole TB-196
remains BLOCKED_PENDING_FULL_RELATIONAL_ACCEPTANCE; the old full-schema fixture, storage/unique/race/atomicity gates
were not run; worker result is not Architect ACCEPT; STOP_FOR_ARCHITECT; no automatic next task.

## Worker pointer — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13 (PARTIAL, correction-budget declared)

R13 repaired the stale type-catalog harness in TESTS ONLY and then actually executed the PostgreSQL acceptance on a
task-owned `postgres:17-alpine` at 127.0.0.1:55436 (role tb196, DBs fulfillment_tb196 + fulfillment_tb196_model).
The fixture is now chronology-correct: pre-type checkpoint 192 objects -> historical type Up adds exactly the 25
objects owned by the two type tables (removed 0, every legacy object still present) -> accepted R10 pending-index Up
adds exactly one index ON fulfillment.outbox_messages (removed 0) -> STRICT both-direction whole-manifest equality
between the current real chain and the canonical GenerateCreateScript projection (218 vs 218, differences 0, no
classifier/default/name/count exception, history excluded by exact schema/table identity in the SQL). Teardown ran the
real inverse order with history-row gating: Down index restores 217 objects (0 diff) and Down type restores the 192
pre-type objects (0 diff), no orphan object, no no-op labelled Down. Storage matrix, revision race with two ACTUAL
per-context OriginalValues[Revision]=1 tokens and a winner identified from the real outcome, unique create race with
one complete winning tree, write-only mid-save failure plus REAL caller-token cancellation at the second-write
boundary (both followed by an unchanged tree and a clean retry), and exact/lookalike constraint ownership all passed.
R12 default proof was hardened without touching production: unconditional Never/Save/Save assertions and an actual
column->parameter->submitted-value binding trace persisted in insert-trace-captured.txt, plus executed negatives for a
changed default and a same-prefix non-history object. Executed 35 tests, 35 passed, 0 failed, 0 skipped; production
(mapping, all migrations/designers, snapshot) is byte-identical (DbContext F26466311348C46C, R10 C6600940CBEF7D07,
type catalog 979328491371CD0F/50098BD09979F5F8, snapshot D74FA5C166305E19). Declared process deviation: two
harness-only corrections were needed (shared id generator; Npgsql '@' prefix on parameter names) where the task
allowed one, so Status is PARTIAL despite all functional gates passing. Container removed and 55436 has no listener.
TB-196 remains PENDING_ARCHITECT_REVIEW; Production-Functional-Acceptance PENDING; no certification/manifest
promotion; worker PASS is not Architect ACCEPT; STOP_FOR_ARCHITECT; no automatic next task.

## Worker pointer — TB-199 executed (2026-10-23)

`TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199` was executed from base `8211c5e43eb9cf5a8a265c99f78c5c68f6758af9`.
Evidence: `docs/architecture/evidence/TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199/RESULT.md`.
Whole Shipping suite passed once on the disposable PostgreSQL target (645 passed), container removed and 55432 free.
Status is PENDING_ARCHITECT_REVIEW; Production-Functional-Acceptance PENDING; worker PASS is not Architect ACCEPT;
STATUS: PARTIAL only in that a dedicated throwing-catalog post-commit fault fact was not added. No automatic next task.

## مرجع جاری: 201-R1 PARTIAL — 2026-10-23

گارد فعالیت ناحیهٔ خدمت (تنها تغییر production) اعمال شد؛ اما سناریوی فقط-کلید-پایانه با
configuration_unavailable بسته می‌شود و اثبات pre/post و چهار اثبات الزامی (EF واقعی، DI واقعی،
ActivityListener، enrichment هندلر) اجرا نشد. جزئیات: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1/RESULT.md

## مرجع جاری: 201-R2 (test-only) — 2026-10-23

ریشهٔ خرابیِ repro قبلی، fixture بود نه production: fake هر id درخواستی را تکرار می‌کرد و پاسخ تکراری شهر
درست fail-closed می‌شد. با fixture قانونی، همان assertionهای فعالیت پاس می‌شوند (غیرفعال → Unresolved،
فعال → Resolved، فقط-کلید-پایانه → کل نیاکان کشف می‌شود، پاسخ تکراری عمدی → configuration_unavailable).
یک harness واقعی DI/EFInMemory هم ثبت‌های scoped، خواندن واقعی چهار محور، فعالیت تودرتو/کورلیشن و
انتشار استثنا/لغو را اثبات می‌کند؛ enrichment هندلر هم در harness اندپوینت اثبات شد.
production بایت‌به‌بایت تغییر نکرد. جزئیات: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2/RESULT.md
