PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228
Parent-Task: TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: IMPLEMENT_FIRST_REAL_STOREFRONT_GROUP_PROJECTION
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD40 minutes / source stop25 / final15 focused proof+evidence+recovery+commit.
One correction round. No source audit report or another standalone calculator/unused helper as substitute.

USER POLICY — clarified 2026-10-27, do not ask again
Exactly TWO fulfillment responsibility families: Seller delivery, or Platform delivery (the Store itself
in SingleStore, Marketplace in Marketplace edition). Platform can have several real DB shipping types
and methods, e.g. supermarket/ordinary/heavy/extra-heavy; a type NAME never implies a hardcoded speed.
Each shipping group owns its methods, fee, receiving dates and daily interval independently.
Reuse OUR EXISTING Storefront cards/controls/DOM/CSS; the Digikala screenshot is behavioral reference,
not authorization for a new visual design. Storefront was not connected before this task.

READ FIRST / KEEP CLOSED
Read TOP AGENTS/recovery, shipping decisions section29 and compact order-evidence decision.
227 accepted with recorded mobile empty-note residual; no admin money/week/picker work here.
213 calendar, 216 exact-capture Contracts quote, 217 whole-cart financial facts, 218 composer with R1
request-wide binding consistency, 219 selected capture reader, and 220 publication API are accepted
primitives: consume them rather than copy/recalculate them. No lossy capture-to-legacy conversion.
Inspect existing StorefrontShippingService ProjectAsync/SaveSelectionAsync/CommitAsync and existing
storefront-shipping.tsx, storefront-shipping-group-cards.tsx, storefront-shipping-delivery-card.tsx,
storefront-shipping-group-horizon.ts before editing. Use existing module skills/guards.

CONCRETE OUTPUT — FIRST REAL READ PROJECTION, NOT WHOLE CHECKOUT CERTIFICATION
Connect a real server-derived group plan to the EXISTING storefront shipping projection and its existing
cards. No fixture-only candidate plan; no direct frontend call to the admin draft API. This slice is read
projection only: captured candidates MUST NOT be selectable/committable until the later parity/order-
evidence slice is delivered. Existing customer UI must clearly use its existing unavailable/disabled
state for that limitation; do not disguise a quoted-but-uncommittable candidate as checkout-ready.

A. EXPLICIT STORED RESPONSIBILITY + TYPE (architectural assignment decision)
Source inspection already confirms inspected Cart/Offer Contracts have seller identity but NO explicit
fulfillment responsibility/type. SellerPartyId alone is NOT permission or fulfillment responsibility.
Use the existing Offer aggregate as the authoritative assignment point for SELLER-SPECIFIC goods:
one explicit nullable delivery responsibility (Seller / Platform) and nullable real ShippingTypeId.
Unassigned legacy offers stay unassigned, not implicitly ordinary/Platform/Seller. Reuse existing Offer
revision/authorized update mechanism where available; do not create a second offer settings framework.
If persistence is absent, add the MINIMAL additive nullable fields/migration in Offer and expose through
an Offer.Contracts BATCH read keyed by exact OfferIds. No foreign DbContext/schema join/FK/type text copy.
Validate non-null type through Fulfillment Contracts and real active DB taxonomy; store only the id.
Initial write entry is authorized ADMIN only in its real commerce scope; do not grant sellers a new right
to assign Platform responsibility. Reuse existing admin Offer authorization/body/MediatR patterns; no
new permission catalogue/auth bypass. The responsible Seller identity comes from the actual owned Offer,
not a browser seller id. Platform owner = trusted edition+commerce context (Store tenant or Marketplace),
NEVER user account. Conflicting/unknown/inactive/missing assignments refuse explicitly.
Scope this authority work to the smallest path needed by the real projection; no unrelated Offer UI redesign.
If actual aggregate constraints make this impossible within source stop, report the exact unresolved
change as PARTIAL; do not invent a mapping or spend another hour producing an audit.

B. SERVER PLAN -> SELECTED CAPTURE -> EXISTING COMPOSER
Load current cart through existing access/version guard and read assignments for DISTINCT real OfferIds
in one batch, validating every returned identity and Offer seller against cart. Partition EXACT cart lines
server-side by responsible owner + real type; retain seller boundaries needed by 217 (its group must have
one seller), even if two sellers use the same Platform method/type. No lost/duplicate lines, no guessing
type from name/weight, no grouping by user id. Stable server group keys; distinct from Shipping area GroupId.
Discover candidate bindings through existing actual catalog identities, including service with NULL option;
do not fuzzy-match post/NULL to post:standard or split a display code to invent a binding.
Read 219 selected pointer once per DISTINCT full-owner+canonical binding per request; reuse it across groups,
preserve SelectionVersion/SnapshotId provenance for the later selection/commit parity. No newest draft,
latest snapshot, mutable draft read or auto-publish. A selected candidate's stored type must match the
actual assigned type; validate active taxonomy through its Contracts authority.
Compose through 218 ONCE for the complete cart partition -> 217 ONCE whole cart -> 216 once per candidate.
Unassigned or unavailable groups stay visible with an explicit reason; do not price them at zero or quietly
fall back to legacy. Avoid omitting a partition group merely because it has zero viable candidates: make
the minimum needed additive adjustment to the existing composer if its current nonempty-candidate
validation prevents an honest unavailable group. Preserve all prior revision-consistency checks.
Group goods = AFTER discount AND tax, excluding shipping/packaging. Physical weight/item count stay null
unless a REAL existing authoritative source is proved; decimal quantity/line count are NOT item count.
215/216 may refuse measurement-gated rows (the existing user draft has weight bounds); report that honestly,
do not erase its bounds or fabricate kilograms to make a green screenshot.
Use stored method timezone, 213 working-day minimum and actual per-day interval. No global max/preparation
addition, hardcoded 9-12, fake supermarket speed, booking cutoff or slot policy.

C. EXISTING STOREFRONT VIEW
Feed each group's real result to existing group cards/date controls via minimal additive transport/adapters.
Do NOT use a single page-level price/calendar for all groups. Preserve title/images/address/summary/style.
Dates from different weekdays may carry different intervals: existing date selection must choose only that
date's interval(s), not all method dates' union.
Labels use real DB content-language/type/method data. Technical ids and publication counters stay out of
normal customer UI. Keep freight and packaging exact and separate in data; existing money display only.
Use a narrowly scoped captured-projection opt-in, default OFF in checked-in configuration, so legacy
installations are not silently cut over. Do not repurpose TB185/legacy gates. Local Development trial can
enable this new read branch explicitly after confirming its scope; no production flag changes.
SaveSelection/Commit MUST explicitly refuse captured-plan selection until parity/order evidence exists.
Keep the old branch unchanged when the read opt-in is OFF. Do not let a method code accidentally cross
into the legacy selectable path because the visual labels happen to match.

PROOF / LOCAL TRIAL
Focused tests of real call chain (controlled foreign Contracts responses clearly labelled where used):
two independent groups with different fee/week; one refused/unassigned group does not erase the other;
wrong owner/type/selected revision rejected; changed cart membership rejected; exact 99/198 financial facts
remain per group; unknown physical facts => no quote rather than zero; no per-line Offer/selection N+1;
selection/commit captured path explicitly blocked. Existing default-OFF behavior remains intact.
Build only changed projects and one focused test class; affected frontend typecheck if edited. No full
119/188/194/backend/solution/guard wildcard loops.
Existing NONproduction authorization covers required additive migration through MigrationRunner plan/apply
with explicit store-alpha, isolated build and exact verified Host restart if required. Inspect actual pending
migration set first; no reset/drop/reseed/unrelated migration adoption/kill-by-name. Preserve current dev actor.
One disposable DEV cart/Offer assignment may be made through authorized existing/new normal APIs for a
meaningful trial, with original values read/recorded first and exact writes disclosed. Never overwrite the
user's real draft/rates/publication pointer; no automatic capture/publish. If no lawful published method can
quote the test assignment, report actual refusal and prove positive composition with focused fixtures
clearly labelled; do not claim a live positive quote.
Fresh task-owned Chrome profile + created target only; never user's tabs (225-R1 unsaved-edit loss).
Show real storefront route at desktop +390 with existing cards; zero interception; report actual GET/POST
receipts, no real order/payment/reservation. No invented successful checkout.
Stop at timebox with truthful partial status if a prerequisite is still open; no unbounded correction loops.

HISTORY / PERFORMANCE LOCK
One shared immutable snapshot per owner+draft+revision, not authored JSON copied per order.
Future final order group stores only stable capture/method/area/row refs plus final money/receiving facts.
Do not add order-schema fields or historical repricing in this read slice. Retain references for the NEXT
selection/commit slice; selected publication is intent, not customer eligibility/activation.
No duplicate price/calendar engines, no Offer pricing arithmetic, no per-group whole-cart quote.

FINISH
Persist verbatim task and RESULT in docs/architecture/evidence/TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228/.
Update ALL THREE recovery TOPs (AGENTS, START-HERE-IF-CHATGPT-IS-LOST, canonical shipping recovery):
two-family user policy, actual new stored authority/migration state, actual read connection versus still
blocked selection/commit, tested URL and exact remaining step. Record elapsed/limits, not self Visual ACCEPT.
Exact-path staging/commit/push/fetch parity, preserve unrelated line-ending drift; no git add -A or history
rewrite or Bridge/post-result script execution.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. No task229 envelope.
