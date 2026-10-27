# Shipping completion scope — user-confirmed full ownership/edition delivery
Date:2026-10-27
Status: REQUIRED DELIVERY GATE, not an implementation/acceptance claim.
Authority: latest explicit user instruction: "باید همه رو اماده کنیم بدون اونها به درد نمی خورد هم باید مارکت پلیس هم فروشگاه های مختلف مسیر خودشون رو انجام بدن اینطوری ناقص میشه".
User rejected a final handoff limited to SingleStore/noncoupon. This supersedes that DELIVERY limitation, not already accepted bounded code/proof.

## Required product outcome
A complete shipping flow for every supported business owner in BOTH editions, reusing existing storefront/admin UI:
| Installation/responsibility | Required authoritative path |
| --- | --- |
| SingleStore / platform fulfillment | Active real store tenant, own service authoring/publication, group projection/selection/commit and historical evidence. Separate stores/DBs keep separate ownership. |
| Marketplace / platform fulfillment | Marketplace-owned service definitions/publications, marketplace authority and group fees/dates. Never manufacture a Store tenant in Marketplace. |
| Marketplace / seller-store fulfillment | Each real authorized seller/store business scope manages and uses its OWN shipping methods, groups and publications. Never infer shipping ownership from login-user id or authorize another seller/store by a client key. |
| Marketplace cart mixing responsibility families | Preserve complete partitions by actual fulfillment owner/type/seller boundary; independently selected method/date/money/evidence, no cross-group takeover, erasure or legacy fallback. |

"Store" in business language must not be silently equated to a login user, StoreId, SellerPartyId or tenant string.
Use the ACTUAL authoritative store/seller relationship in this repository. Existing SellerPartyId is a BUSINESS
identity, not a user account; if a seller owns several stores, review the real mapping/cardinality before declaring
those stores isolated. Do not invent StoreIds, automatically reassign existing drafts or perform a schema flip
from this document alone.

## Access and surfaces
Separate storefront/marketplace/seller-store management surfaces using the SAME form/components, as user requested,
rather than fake owner options or a client-driven scope switch. Server resolves permitted owner(s), actual offer
responsibility/type and publication scope. Authorized delegation can be implemented only through real server
permission/owner authority. Denied, inactive, foreign or mismatched scope must refuse without data/price leakage.
No blanket Seller/Marketplace refusal may remain as the final substitute for required supported flows.

## Coupon context is part of completion
The previous assistant handoff excluded coupons together with Marketplace. User rejected that incomplete scope.
Reuse existing Promotion/Tax/Checkout authority: the SAME valid coupon context must reach shipping read preview,
saved selection and final quote. Group goods remain after discount AND tax, excluding freight/packaging.
Do not invent discount allocation, round/pro-rate totals, reprice historical orders, or treat an unsupported coupon
as absent. Current explicit coupon_unavailable is safe INTERMEDIATE behavior, not final completion.
Changed coupon/cart/pricing/selection requires fresh confirmation under existing policies. Preserve compact group
evidence, existing line discount/tax snapshots and one whole-cart quote per evaluation.

## Completion evidence
Actual existing UI: destination -> each group's method/date -> save -> verified payment preview -> public submit.
Prove SingleStore and Marketplace configurations, platform/seller-store mixed cart, at least two independent
business scopes and denied cross-scope attempts, coupon changes, stale selections/failed refresh/retry, duplicate
submissions and immutable historical Order money/receiving facts. Desktop+360/390 mobile, existing CSS/cards.
Controlled Contracts tests and isolated Order PostgreSQL remain useful but are not a live multi-owner trial.
User Visual ACCEPT is separate and must never be invented.
No new Carrier API/funding rules are authorized by the word "all": only genuinely configured/supported methods may
be offered; unavailable capabilities must be honestly reported. Do not silently redefine the required owner or
coupon paths as unavailable to claim completion.

## Current truth and task sequencing
231/232 draft evidence/storage and233 core accepted only within recorded SingleStore Manual/noncoupon proof.
Active234 connects public backend edges only. Keep its reviewed bounded task unchanged while executing; do not
unilaterally broaden an already claimed task.234 is an INTERMEDIATE milestone and CANNOT be final user delivery.
After its review, issue focused implementation tasks for the remaining full-owner/access matrix, shared coupon
context, existing frontend connection and real complete trials. Implementation tasks may be bounded; final
acceptance must cover this whole document. No generic audit/duplicate engine/test-confidence loops.
The earlier "3 tasks / 1-2 hours" assistant estimate covered a smaller scope and is NOT a commitment for this
expanded completion gate. Do not promise a new fixed count/time without actual implementation evidence.

## Preservation
No activation, runtime changes, migration application or business write is performed by this scope update.
USER visible review window/unsaved work, existing Host/FE/localpreviewONcount7 and business data stay untouched.
No full authored JSON per Order; shared immutable capture + compact accepted group facts, no historical repricing.
Existing gate defaults, TB185paused/PendingAllocation, shared-pending231/232 migrations and EF snapshot limitation
are preserved until concrete subsequent task authorization changes them. Prior recorded defects remain visible.

## Latest execution priority — practical product trial first
User follow-up: "سریع تر مرحله عملی باید انجام بشه من باید خیلی سریع نرم افزار بفروشم اینطوری هیچوقت تموم نمیشه".
Optimize subsequent task boundaries around a usable VERTICAL buyer/owner flow, not independent helper completion.
234 remains the active bounded backend edge. After its review, prioritize existing frontend selection/save/payment
preview with actual owner/responsibility paths and shared coupon context, then a task-owned complete practical
trial on desktop/mobile. Do not add general audits, repeated broad test batteries, speculative frameworks or
unrelated cleanup. Fix demonstrated blockers in that same flow; keep exact focused safety checks.
Use explicit per-scenario go/no-go receipts (SingleStore, Marketplace platform, seller-store, mixed cart,
coupon and replay/history). A successful controlled unit test or build alone is not a sellable software claim.
Protect user's live review window and unsaved work: use separately owned trial surfaces/runtime when needed,
never seize or hot-reload that user's window as proof. New runtime/DB setup must be concrete, scoped and
documented under the issued task; this docs update itself authorizes no surprise activation.
