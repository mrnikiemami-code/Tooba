# Architect review — TB-227 — 2026-10-27

Reviewed source: a32d99ee47d445f6715df3436ca3a5689f6c114e. Architect independently read the source diff (field, modal, search helpers,
localized strings and tests) and viewed committed 1440 city-popup and native 390 popup images.
Architect did not execute the worker's Windows tests/browser and does not claim independent runtime execution.

## Verdict
ACCEPTED_WITH_RECORDED_LIMIT for internal popup search and removal of the reproduced branch-loading shake;
ready for user retest of those behaviors. NOT full pixel-stability/Visual ACCEPT.
Conditional in-flow status rows changed the UA-centred dialog height. The worker's before/after receipts
report 42px top travel before and zero on the sampled populated paths after. Search is visibly inside the
popup; a dialog-contained portal avoids the outside-dialog inertness issue.
The 52px reservation is a MINIMUM, not an invariant height: the recorded empty-branch note can grow to
66px at <=390. This +14px status-region growth remains open. Do not label every branch pixel-stable.
Service-area selection lacks live options in this installation; only the empty branch and helper tests
were exercised. No fourth-stage populated live acceptance is inferred.
New field is feature-local instead of direct shared-combobox reuse; its modal portal constraint is documented.
Two source comments still incorrectly mention body/outside-dialog portal; actual code uses closest(dialog).
This documentation drift is nonblocking and should be corrected alongside the next relevant edit.
No accepted money/rate/week semantics reopened. No draft/publication write is claimed.

## Storefront handoff — concrete unresolved product authority
Architect read existing StorefrontShippingService, CartPresentationContracts, OfferReference/OfferQueryDtos/
OfferSelectionCandidate, canonical Fulfillment method Contracts and section 29 of the shipping decisions.
Actual Cart lines carry seller/offer/product identity but no fulfillment responsibility or required shipping
type. The inspected Offer contracts also expose no such assignment. Seller identity is not proof that the
seller fulfills a line. Publication selection alone is not product compatibility or responsibility.
Consequently no safe complete server plan can be claimed yet. Do not turn a selected pointer into a global
method list or infer owner from SellerPartyId to make a demo appear connected.
Next implementation must establish explicit trusted per-line fulfillment responsibility/type authority and
consume the selected capture through the accepted composer, then existing projection/selection/commit.
Before choosing new policy, clarify whether Marketplace sets responsibility per product/offer (seller vs
Marketplace); a SingleStore owner remains the trusted store context, not a user account.
Existing Storefront cards/controls/DOM/CSS must be reused per group. Compact order evidence remains mandatory,
unimplemented; never full authored JSON per order or repricing historical orders.
No executable Storefront task is issued by this review: the source/product gap is named, not hidden by
another calculator/helper task.

## Continuity
225-R2 and 226 remain accepted within their proof limits. 225-R1 unsaved-user-tab loss remains recorded;
no recovery claimed. Future mounted work uses task-owned profile/target only.
TB185 paused/default-OFF/PendingAllocation, real draft, selected pointer, runtime and migrations preserved.
