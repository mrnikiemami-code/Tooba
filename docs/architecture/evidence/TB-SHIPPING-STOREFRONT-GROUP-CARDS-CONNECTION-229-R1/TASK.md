PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229-R1
Parent-Task: TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: REPAIR_CONFIRMED_READ_PROJECTION_DEFECTS_AND_REAL_TRIAL
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD35min / source stop20 / final15 proof+recovery+commit. One correction max.
No next parity implementation until this READ path is actually reviewed and observed.

READ FIRST
TOP AGENTS/recovery, 229 ARCHITECT-REVIEW.md, section29 and compact-order evidence.
229 is NOT accepted. 228 assignment migration is ALREADY shared-applied; do not replay migration.
No new assignment/domain/schema/price/calendar framework; no standalone audit/helper as substitute.
No default-OFF/pausedTB185/PendingAllocation change. Existing explicit captured write guard stays.

SOURCE-CONFIRMED BLOCKERS — fix inside existing229 path
1 MONEY / STRICT PREVIEW DECODE
storefront-shipping-api.ts asNullableNumber compares NaN === NaN (always false), then calls asNumber with
fallback0. Root independently executed the expression: "abc" ->0, explicit0 ->0, missing ->null.
Fix NEW captured-preview boundary only; do not disturb legacy decoder.
Use exact decimal wire strings for the new monetary fields and an existing/reused exact text formatter,
or the smallest exact string display extension if existing formatter accepts only number. No Number/
parseFloat arithmetic or numeric JSON rounding for new captured money. No hardcoded IRR when missing.
Unknown/not-evaluated goods amount is NULL, never0. Early planner branches currently construct group goods0
without217; change this new DTO field to nullable and show unknown/omit the amount until actually proven.
Explicit zero remains valid. Invalid money/shape must become explicit untrusted preview, not another amount.
Current decoder silently skips malformed groups/methods and defaults missing currency/membership:
present-but-invalid preview must be distinguishable from absent/OFF, show an honest unavailable state and
NEVER render the legacy selectable cards as fallback. Validate required shapes/ids, unique group/method
identities, actual line membership/currency correspondence at the existing boundary. No silent shorter list.

2 CANDIDATE IDENTITY IS NOT A TRANSLATED LABEL
Planner passes SelectLabel(...) into StorefrontCapturedCandidatePlan.MethodCode. Composer rejects duplicate
or untrimmed MethodCode, so TWO legitimate bindings with the same translated name can refuse the whole cart.
Use actual stable catalog binding/method identity for MethodCode; carry localized MethodLabel separately
for display (a per-request identity->label map is sufficient). Two methods with same label remain distinct.
Reuse existing typed StorefrontCapturedPublicationBinding equality for full-owner+binding cache/compare:
219/218 accept trim+OrdinalIgnoreCase service/option, but planner currently compares trim-only strings with
case-sensitive != and caches delimiter-concatenated keys. Exact tenant identity remains case-sensitive.
Do not turn display names or different group destinations into publication identity.

3 REAL EXISTING UI, NOT DUPLICATED PREVIEW CARDS
Current StorefrontShippingGroupCards renders a new StorefrontShippingCapturedPreview BEFORE all existing
legacy groups. Preview ON therefore shows both authorities, and storefront-shipping.tsx only passes it
when isGroupedCart=true: the single-line path never shows the captured preview.
Make ON-preview use the EXISTING group/card/line/date controls as the ONE shipping presentation for BOTH
single-line and multi-line carts. Reuse/extract their markup and callbacks where needed, no duplicate
parallel card tree merely copying CSS, no separate mockup. Render actual group CartLineIds' product lines/
images, not seller/type headings with the items omitted. Keep address and normal summary style.
Legacy card/method/calendar authority remains only when preview genuinely absent/OFF; malformed-present
preview shows failure, never legacy alternatives. Preview summary must not display legacy freight/date
as though computed by the captured path. Unknown amounts remain unknown.
Each group/method independently browses its own dates and THAT date's interval. Use existing localized
date formatting; do not label raw Gregorian yyyy-MM-dd digits as Jalali date chips.
Keep explicit preview-only/no checkout notice compact; server SaveSelection/Commit refusal is authoritative.

4 FINISH REAL PLAN HONESTY
Do not silently reuse ShippingMethodsOptions.DeliveryHorizonDays as AvailableDateCount: these are different
semantics, explicitly forbidden by218/228. Add an explicit validated preview date-count option (0..90)
required when preview is ON; set its local trial value consciously, record it as preview policy, no global
horizon reinterpretation or magic default. Checked-in Enabled remains false.
Unknown responsibility enum must refuse, not enter BuildPartition default Store/Platform case.
Reject contradictory/outside-request assignment/type rows; no seller/owner guess.
Do not catch ContractOperationException from composition and erase the cart into Empty with no groups.
Known expected business refusal can surface a precise explicit error/reason; dependency exception and caller
cancellation must propagate, never be recast as missing assignment or an empty successful preview.
Preserve group independence and one complete partition. Current composer ALREADY allows empty Candidates:
do not relax its validation again or remove groups. ONE217 financial quote, ONE218composition,219once per
distinct typed binding,216once per candidate. No extra price/working-day algorithm.

PROOF — MUST INCLUDE A POSITIVE COMPOSITION
Extend existing229 tests with focused actual-planner + REAL218 composer + REAL217 CheckoutDirectory harness
(reuse218 financial harness), controlled Shipping Contracts responses clearly labelled. Two groups goods
99/198, distinct fees and weeks; two distinct methods sharing a display label both survive; a third unassigned
group remains visible and does not erase quoted groups; case variants of same binding accepted consistently,
one pointer read per distinct binding. Method/currency/revision mismatch fail closed.
New decoder facts: invalid amount never0, explicit0 lawful, missing currency invalid, decimal precision stays
text-exact, malformed/missing/duplicate group or method not silently removed; absent vs untrusted distinct.
Verify SaveSelection and Commit ON refuse before dependent calls/write, OFF behavior unchanged, through
actual service methods (not source-regex assertions).
Run ONLY new/affected focused class(es) and frontend typecheck after final source. No fullOffer/194/188/
solution/Host repeated confidence loop. Build only changed output needed for runtime.

REAL LOCAL TRIAL — DO NOT OMIT
User already authorized local dev Host refresh/bounded assignment API write. Verify runtime identities,
build once isolated, refresh exact Host PID with preview Enabled=true and explicit preview date count.
No migration replay/reset/reseed. Preserve original local setting values in evidence. Check health.
Use actual cart + normal authorized DEV assignment API if needed, record exact ids/before/after; no mass
assignment, no user draft/rate/publication edit and no guessed physical measurements. Existing user draft
has weight conditions and may truthfully refuse: do not remove them for a green image.
Fresh task-owned Chrome profile/freeCDPport/created target ONLY, no user tabs, no interception.
Observe actual shipping GET payload and EXISTING Storefront cards at desktop and390 for single-line
and multi-line where available; confirm no duplicate legacy cards/summary and no page horizontal overflow.
Positive live quote unavailable => prove unavailable live state honestly, positive two-group behavior in
the real-composer harness; never claim a live positive quote from fixtures.
Record actual browsable local URL, native screenshots and read receipts. No Apply/publication/order/
reservation/payment/cart conversion; no fabricated buyer readiness. Leave reviewed local preview accessible
only if clearly blocked from checkout, document ON state; production/default checked-in stays OFF.

FINISH
TASK/RESULT under docs/architecture/evidence/TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229-R1/.
ALL THREE TOP recovery files updated with actual proof, URL, runtime/option state and exact remaining
selection/commit+compact-history task. Preserve227 residual and unsaved-user-tab incident; no recovery claim.
Exact-path scoped commit/push/fetch parity, no git add-A/reset/stash/rebase/force-push/Bridge/post-result script.
Report actual time; if source stop reached report PARTIAL, do not add hours of unrelated repair.
STOP_FOR_ARCHITECT. Automatic-Next-Task NONE. No user trial invitation before review.
