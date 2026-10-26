PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225
Parent-Task: TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224-R1
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Last-Verified-Base: fb2999cfa880ceafa985c57eede47ff5107ba44a
Mode: BOUNDED_REAL_USER_MODAL_CASCADE_RATE_VALIDATION_AND_MOBILE_REPAIR
HARD45 minutes / stop new implementation30 / final15 focused proof+recovery+commit.
One correction round; report partial scope if blocked instead of extending to another 2-hour loop.
User wants ALL described behavior, not screenshot-only/no-overflow claims. Architect review before new user trial.

USER EVIDENCE / WHAT SUPERSEDES PRIOR UI
Screenshots20261026-150419: mobile week giant whitespace/disconnected hours.
150643/151130: destination level-select/inline picker rejected; wants FOUR FIXED cascading fields in MODAL.
151649: duplicate/error wall text rejected; use existing MessageBox on explicit action.
151753/152046: malformed grouping "5,000000"/"7,0000000"; IRR has no fractional customer input,0 is lawful;
duplicate row conditions not caught cleanly; wants ADD/EDIT RATE ROW in modal with careful edit validation.
These supersede224's relevant-level-selector/inline-rate editing presentation, NOT accepted integrity/authority.

START
Read TOP AGENTS/recovery, applicable actual UI + Analyze->Migrate->Certify skills, existing MessageBox/Dialog
APIs and existing exact money input before coding. Persist task first; fetch safe fast-forward and inspect
diff. Preserve unrelated changes/backend line-ending drift, user tabs and drafts. No reset/stash/rebase/
force-push/git add-A/Bridge or historical scripts.
Keep area-level shared duration, controlled dirty/invalid-range guard, exact currency authority, native DB
languages, compact publication, closed-day guard, no-auto-retry/unknown latch and one document Apply.

A DESTINATIONS: ONE MODAL, FOUR FIXED CASCADING SELECTS, ONE PATH TAG
Replace inline picker and level dropdown/buttons with "افزودن منطقه" opening existing design-system Dialog.
Inside modal ALWAYS four labelled fields: کشور -> استان -> شهر -> منطقه (actual ServiceArea).
All names/options from real Geography DB-backed API, no hardcoded country/city/region list.
Initially country can load; descendants empty+disabled. Country selected -> ONLY its areas load; city
stays empty/disabled until area selected; city selected -> only that city's service areas load.
Change/clear country immediately clears area/city/service choice+rows; area clears city/service; city clears
service. Abort/invalidate dependent requests and ignore stale/out-of-order completions. Existing current-
generation settled active chain/strict decoder remains authority, not a fresh weaker coordinator.
No eager country-wide city read before an area selection in this requested standard four-stage branch.
Existing stored null-admin cities remain valid and visible, NOT auto-removed/reparented. If DB country has no
administrative areas but lawful direct cities exist, don't invent a province: report this representation
gap explicitly; do not silently re-enable country-wide branch against user's sequential requirement.
No Geography schema/data migration to satisfy UI.

Add submits ONLY deepest actual verified selected terminal as ONE selection (kind,id). Country/admin/city
ancestors describe the path; they must NOT become extra coverage chips/selections automatically.
User can choose country-only/area-only/city-only by leaving deeper fields blank, no separate level selector.
Validate actual terminal at Add; duplicate error only after attempted Add, not choosing parent.
Preserve authoritative no-overlap rule (stored CountryIran overlaps its descendants); don't fake success.
Use existing MessageBox, localized concise actionable error. Keep modal/path on rejection.
SUCCESS adds ONE chip to the area's destination list, with full verified breadcrumb:
Iran > Tehran province > Tehran city > Region3 [remove].
Add keeps modal OPEN, preserves parents for rapid subsequent region selection; retain or clear leaf with
clear visible feedback and duplicate guard, no automatic closing. Close only explicit close/finish;
Escape/outside policy from existing dialog must not close accidentally during dropdown interaction.
Remove deletes ONLY that terminal selection, not unrelated chips/parents/groups.

Breadcrumb must survive normal save/reload/language read, not just session-picked terminal label.
Use existing validated destinationFacts ancestry+labels; narrowly extend existing set-based read projection
with ancestor display facts if truly missing. Contracts-only Geography read, strict identity/parent proof,
no fabricated labels/ancestry, no per-chip sequential N+1 reads/cache subsystem. Display names are NOT pricing
authority and never sent as trusted owner/coverage. If path names unavailable, show honest unavailable
segment, not GUID/code guessed name. Retain only actual chain (no fabricated admin for null-parent city).
Document any narrow backend projection change and actual runtime binary needs; no broad architecture work.

B RATE ROWS: MODAL ADD/EDIT, SUMMARY LIST, VALIDATE AGAINST OTHER ROWS
Area card retains ONE shared working-day delivery range outside row modal.
Rows displayed as compact summaries: conditions + freight/packaging, clear Edit/Delete actions.
Add opens isolated modal draft; no blank row appended to main form until successful local confirmation.
Edit opens COPY of that exact row's current local values with stable row key/id. Cancel closes without
mutating main draft; Confirm validates then replaces THAT row only preserving identity/order. Parent Apply
remains only persistence call; modal confirmation is local, never POST/PUT/autosave.
Unknown-outcome/busy/view guards remain at handler boundaries. Disable behind-modal parent mutations.
Need no nested new row grids/framework.

VALIDATION OWNERSHIP
Reuse accepted current Domain authoring rules for shape, half-open intervals and ambiguity:
weight/count/merchandise axes AND within each row, no-axis unconditional. Check every other row of SAME
area; on editing exclude SELF by stable key/id, NEVER by index or "ignore all stored rows".
Reject exact duplicate condition tuples even if prices differ. Reject intersecting multi-axis coverage if
existing backend rules declare it ambiguous; don't reject disjoint alternatives or treat any shared weight
bound as duplicate when other AND axes make them disjoint. Touching half-open endpoints remain lawful.
No first/cheapest/prior-row priority invented. Missing upper bound/open interval follows real domain.
If frontend currently lacks this validation, add a focused feature-owned mirror consistent with actual
backend, not new pricing authority. Backend retains enforcement at Apply; foreign/corrupt stored rows not
silently normalized. No uniqueness across unrelated areas/owners.
Zero freight and zero packaging are VALID amounts. Empty is different from zero. Zero prices don't claim
CalculationMode.Free or erase real cost/funder semantics.
On failed modal Confirm show existing MessageBox with short owning-field reason; modal stays open with
exact typed values. Invalid fields retain accessible aria-invalid/local indication without permanent huge
red paragraphs. No MessageBox on every keypress. Parent Apply if invalid local state remains blocked; never
convert invalid text into valid accepted number. Server failure uses same existing error workflow rather
than generic browser alert; preserve data and unknown/conflict semantics.

C MONEY — REPAIR NORMAL TYPING, NOT JUST PASTE; IRR INTEGER UI, NO PRECISION LOSS
All monetary inputs including merchandise bounds share exact-string grouping and policy.
Source currency is server-owned group currency. For IRR user's requested UI is integer rial:0,5,000,
120,000,5,000,000; NO visible .00 or "max2decimal" IRR message. Existing stored120000.00 is numerically
integral: display120,000 without rewriting untouched raw token/dirty/scale. If existing stored fraction
is nonzero, show honest incompatible precision + explicit issue, NEVER round/delete/truncate it.
New/edited IRR value rejects fractional nonzero input with localized integer-rial reason. Boundary policy
for typed .00 must be explicit and consistent; no general rounding. Keep other currencies supported under
actual known metadata/existing contract; don't globally force integer or guess all currencies minor units.
No Number/parseFloat conversion of money, no currency conversion, no FX, no hardcoded frontend options.
Resolve screenshot root cause: editing around existing displayed comma changes grouping shape, while
strict paste validation wrongly treats generated display separators as user malformed input.
Separate known presentation grouping from raw logical edits using proven existing component/input events.
Normal typing1200000 must visibly yield1,200,000, not1,200000; replacing/pasting selection, middle insert,
Delete/Backspace at separators, selection replacement, Persian/Arabic digits and0 must work.
Pasted malformed12,34, signs,letters,exponents/multiple decimals remain visibly invalid/refused with
explicit feedback; never strip "-100"->100 or"1e3"->13. Caret tracks decimal side where supported.
Group separators purely display; no comma/localized digit in write tokens. Zero .00 baseline scale must
remain exact when untouched; don't mark dirty by mere opening/formatting/cancel.

D MOBILE — USER REJECTED CURRENT APPEARANCE
Fix weekly VIEW+EDIT at360/390 CSS widths: compact aligned day+status; working hours clearly paired in one
line with From/To context; closed day shows compact Closed, no giant empty From/To blocks.
No desktop regression: shared columns alignment retained; exact weekday/time state/ISO wire untouched.
Destination/rate modal fits viewport with readable/touch-usable controls; header+footer/close reachable,
internal scrolling rather than buttons offscreen. Tags wrap and remove stays reachable. Money summaries
readable. Respect RTL and existing design tokens; no globalCSS/framework/template redesign.
Visual proof is actual screenshot inspection, not only scrollWidth==innerWidth. No self Visual ACCEPT.

FOCUSED PROOF / REAL TRIAL
Before edits narrowly reproduce screenshot-shaped grouping and missing row validation on current UI.
Focused tests execute SAME real helpers/handlers:
- parent clearcascade + late obsolete response + no child read before lawful parent;
- Add only ONE deepest terminal, repeated Add modal stays open, whole-path display/removal, duplicate
attempt keeps choice; no overlap bypass;
- add/cancel no phantom row; edit self allowed and excludes only self; another matching row rejected,
disjoint AND-row accepted, boundary equality policy matches backend,0 accepted/blank not accepted;
- IRR stored120000.00 display120,000 untouched wire120000.00; actual typing5million groupable, valid0,
fractional nonzero refusal/no rounding, invalid paste not transformed, caret/delete/local digits;
- previous224-R1 partial range blocking and discard integrity retained through modals.
One final typecheck + exact affected tests + required narrow guards. Do NOT run166/fullfeature/194/
solution or repeatedly broad confidence sweeps. Four unrelated source-size drifts not part of task.
Actual installed isolated browser with real traffic, no /v1 interception for live proof:
fixed cascade+clearing; add modal stays open; one-path tag; rate Add/Edit0/duplicate/disjoint/cancel;
normal character-by-character money entry,0 and valid grouped values, MessageBox feedback, mobile
VIEW+EDIT weekly+both modals. If real geography lacks branch data report it; no seed or fabricated live proof.
No user draft Apply/publication/catalog writes merely for evidence. Local additions must be discarded using
normal app flow, preserve user's tabs. Capture native360/390 and1440 screenshots of actual relevant panels.
If backend projection changed, allowed local nonproduction isolated Host build/refresh only as needed,
verify actual process identity/config, preserve FE and user drafts; no stale binary claim. Standing user
authorization persists. No schema/migration/data rewrite/new flag/edition change.
Don't claim save roundtrip if only local DOM tested; body formation can be inspected with existing labelled
synthetic harness/strict model tests without merchant overwrite.

FINISH
Evidence docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225/{TASK.md,RESULT.md}.
Update all3 TOP recovery (AGENTS/new-chat guide/canonical) with actual outcome, unresolved gaps, routes,
live runtime if changed, user feedback superseding224 presentation, no Storefront connection claim.
Retain compact-order-evidence invariant: one shared immutable snapshot, compact order-group facts; no
historical repricing/fullJSONperorder. Seller-admin from seller-list is pending separate scope.
Exact-path commit/push/fetch authorized, no unrelated line-ending drift/artifacts.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. Architect reviews BEFORE user is told to test again.
