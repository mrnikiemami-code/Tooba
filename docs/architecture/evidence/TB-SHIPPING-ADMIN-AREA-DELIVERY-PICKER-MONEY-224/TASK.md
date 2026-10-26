PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224
Parent-Task: TB-SHIPPING-ADMIN-PUBLICATION-OWNER-PAGES-223
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Expected-Base-SHA: 920b51a7f2dc193afb47366daf9a531415d21373
Mode: BOUNDED_USER_REPORTED_AREA_FORM_REPAIR
HARD40 minutes; implementation stop25; focused tests/live10; evidence+commit5. One correction round.
No extending into seller administration, Storefront, schema redesign, new quote/calendar or broad audit.

USER DEFECTS (actual screenshots reviewed by Architect)
1 Delivery from/to repeated per price row: user wants ONE working-day delivery range per WHOLE delivery area.
2 Selecting Iran prematurely displays "destination already added to this or another group", obstructing
country -> province -> city navigation. Validate terminal addition on Add, not parent exploration.
3 Monetary inputs show 120000.00 /5000.00: need readable 120,000.00 /5,000.00 without losing decimal fidelity.
4 Four terminal-level tabs PLUS all four dropdowns overwhelm the picker; show only necessary path controls.

START
Read TOP AGENTS/recovery, applicable UI/Analyze->Migrate->Certify skills and actual files narrowly.
Persist task verbatim first; fetch safe fast-forward, inspect git status, preserve unrelated work.
Do not replay190-223. Existing backend line-ending drift is unrelated: never stage it.
Retain compact publication, real types ordinary/heavy/extra_heavy, closed-day disabled guard, Saturday
DISPLAY ordering, native DB languages, alternating whole-area cards, dirty/discard/one Apply behavior.

A ONE DELIVERY RANGE PER AREA — NO SILENT DATA LOSS
Place min/max working-day duration in AREA header/body, OUTSIDE repeated rate rows, labelled clearly.
Rows show freight/packaging and optional conditions only. This range is customer receiving duration,
not carrier handoff/preparation; preserve §29 calendar meaning.
Minimal compatible implementation: keep current row-based backend DTO/storage/immutable snapshot format
in this task. Use a cohesive area-level draft editor helper; serialize an explicitly chosen shared range
to every row of THAT area only. New rows inherit the chosen area range, never another area's or a default.
Don't add a second persistent grouping store or uncontrolled state detached from dirty/discard/baseline.
Unchanged uniformly-valued groups roundtrip with no dirty edit or PUT. Raw numeric edit validation stays.
A loaded group with different row durations is a LEGACY CONFLICT, not permission to choose the first/min/max:
show short honest conflict + explicit area-range entry; preserve original row values until operator
deliberately sets range. Unrelated edits must not silently overwrite those legacy differences.
No physical row duration fields displayed as if still independent. If conflicting values require deliberate
normalization, clearly convey it and block the ambiguous shared-range operation until explicit choice.
An empty/new area may stage range locally before rows; no empty range auto-serialized as 0/0.
Discard reseeds shared range and row values from SAME baseline; area remove/reorder keeps correct ownership.
Do NOT claim a group-level schema invariant: current wire repeats area choice per row for compatibility;
document this exact boundary and any remaining zero-row persistence limitation.
No migration, no rewrite of existing captures, no live user-data normalization without user Apply.
If true group-level persisted duration cannot be delivered compatibly within box, report precise boundary,
not speculative backend overhaul. Finish B/C/D regardless.

B DUPLICATE CHECK AT ADD, NAVIGATION NEVER BLOCKED
Use actual current terminal level + coordinator-verified choice, not selected country alone.
Selecting country/area/city is staging; no duplicate/overlap error merely from onChange or render.
Allow lawful branch exploration even when an ancestor country is already stored.
On actual "Add this destination": verify current active settled terminal path FIRST, then exact duplicate
(kind,id) against proposed groups; show localized inline error only for that submitted attempt.
Keep staged path after rejection so operator can choose deeper level; changing path/level clears the stale
attempt error, NEVER removes stored chip.
Pending/unverified/inactive/contradictory options still disabled/fail closed; do not weaken coordinator
epochs/current-generation parent evidence, strict decoder or terminal ancestry.
Exact duplicate local rule and canonical parent/descendant overlap backend rule remain intact.
IMPORTANT screenshot's existing Iran COUNTRY covers Iran descendants: user may browse a city, but adding
overlapping coverage must still be rejected by authoritative overlap rules. Explain this actual conflict
at Add/Apply when relevant; do NOT bypass overlap or fabricate successful save.
No duplicate engine, no eager world preload, no disappearing selected branch or arbitrary GUID trust.

C RELEVANT-LEVEL PICKER
Keep a clear terminal selection (country/province/city/service area), localized and understandable.
Render only controls needed for that terminal: country only; country+province; city path as required by
actual API (country-wide null-admin cities remain lawful); service adds verified city+service area.
A hidden optional province must not fabricate a filter or substitute highlighted parent into city ancestry.
Changing terminal/path preserves lawful useful staged parents, clears obsolete children/requests as accepted.
No redundant all-four disabled selector row. Maintain compact desktop and stacked mobile/RTL layout.
No new framework/design-system/global CSS.

D EXACT MONEY FORMATTING, EVERY MONEY FIELD IN THIS FEATURE
Use existing exact-decimal/string primitives. Visible thousands grouping in freight, packaging and
merchandise condition bounds; also truthful read summaries. No JS Number/parseFloat arithmetic/rounding.
Preserve digits/fraction tokens incl.0.50,1850000.00, huge safe supported decimals and deliberate blank.
Wire values must be raw canonical decimal tokens, never commas or localized display digits.
Handle paste of grouped values and Persian/Arabic digits deterministically, caret/backspace and decimal
editing usable. Prefer existing design-system money input if it meets exactness; no new package.
Grouping alone must not mark dirty, change decimal scale or trigger save. Currency remains server-owned.
No forced .00, decimal limit guessed from currency, currency conversion or monetary policy change.

FOCUSED PROOF
Only affected helper/model tests + one typecheck + required narrow boundary guards, one run after final edits.
No144/127/194/full backend/solution/confidence loop; historical four source-size failures untouched.
Tests must execute actual helper/component behavior, not CSS/source-regex-only replacement:

common range updates all and only that area's rows; new row inheritance; unequal legacy preserved
until explicit choice; cancel restores; unchanged uniform baseline clean;
country already stored can be staged to reach province/city; no early duplicate error; actual same
terminal Add rejects with path preserved; existing overlap remains rejected (do not assert city inside
stored Iran should save);
correct terminal ancestry/current active guard retained;
money 120000.00 ->120,000.00 visible ->120000.00 wire,0.50 exact, paste/delete/blank/local digits.
Real isolated installed-browser on FE3000/Host5088, no /v1 interception:
perform screenshot-shaped branch exploration; actual duplicate Add feedback; shared range local edits;
money typing/grouping+optional bounds; cancel/re-enter proves no residue.
No user-draft Apply, publication/catalog write, migration/seed/reset required. Preserve user's tab.
If save proof necessary use existing approved synthetic harness clearly labelled, NOT write user data
merely for evidence. Report real vs fixture boundaries.
Capture native viewport-sized1440 and390 screenshots; no horizontal page overflow; do not claim user Visual ACCEPT.
Reverify runtime identities, reuse live stack, no restart for frontend-only work.
Standing local nonproduction authorization persists; do not ask permission again for already authorized work.

FINISH
Evidence docs/architecture/evidence/TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224/{TASK.md,RESULT.md}.
Update ALL THREE recovery TOPs: AGENTS.md, docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md,
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md.
Record exact changed behavior, duration compatibility vs schema truth, overlap limitation, executed counts,
live URLs, user's draft/pointer preserved, and remaining Storefront compact order evidence invariant.
Commit/push authorized scoped paths, fetch parity. No git add-A/history rewrite/Bridge/historical scripts.
No seller-admin access work in224; planned entry from seller list remains separate next scope.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. Report elapsed honestly, don't exceed box to polish evidence.