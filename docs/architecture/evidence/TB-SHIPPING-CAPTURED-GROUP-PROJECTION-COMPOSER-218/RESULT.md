# TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218 — RESULT

Mode: BOUNDED_ORDER_READONLY_CAPTURED_GROUP_QUOTE_COMPOSITION
Base: `f8b77aebb8377b82ac2c5f94355f36023b0a2374` (fetch verified, `HEAD == origin/main` before work)

## Verdict

`COMPOSED_PRIMITIVES_ONLY / NOT_WIRED / NOT_LIVE`.
One cohesive read-only Order.Application service now composes the three accepted primitives for an
**explicit trusted server plan**: the real 217 same-group goods amount (after discount AND tax), the 216
`IShippingCapturedRevisionQuoteLookup` captured-revision quote, and the 213 customer receiving calendar.
No recalculator, no legacy/static-window fallback, no activation, no public route, no writes.

## Task (verbatim)

```
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218
Parent-Task: TB-SHIPPING-GROUP-GOODS-QUOTE-FACTS-217
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: f8b77aebb8377b82ac2c5f94355f36023b0a2374
Mode: BOUNDED_ORDER_READONLY_CAPTURED_GROUP_QUOTE_COMPOSITION
HARD25min/STOPimplementation17/final8focusedproof+evidence+commitpush. Onecorrectionmax.
No generic sourceaudit, wholeStorefront194/Shipping/Host/solution/confidence loops.

READ TOPAGENTS/recovery/217Architectreview+§29decisions, applicablebackend/Contracts skill.
217realgoodsfacts/216ShippingContracts/213calendar ACCEPTED; composeTHEM, no recalculators/copyrules.
No lossycapture->legacyconversion, no draftlive read/latestfallback.

ONE CONCRETE COMPOSITION SERVICE
Add ONEcohesive Order.Application.Storefront service (plus typedmodels asneeded) registered scoped in
OrderModule. It combinesexisting217reader +216IShippingCapturedRevisionQuoteLookup +213calculator +IClock.
No foreignDomain/Infrastructure orShippingDbContext, no separateprice/calendarengine.
Input is explicitly INTERNAL TRUSTED SERVER PLAN:
normalSubmitCheckoutCommand/CartAccess+version, complete groups ofCartLineIds with stableGroupKey, andpergroup
candidateplans containing FULLserverderivedownerkey, exactDraftId/SourceRevision andexpectedservice/option.
NotHTTPbody/bearercredential. This task DOESNOTderivefulfillmentresponsibility fromSeller ordecideactive
revision. Sourceplanmustultimatelycomefromreviewedserverpublication/Offer/fulfillmentauthority, NEVERbrowser.
No publicroute/ISenderrequest/liveconsumer needed yet; keepcurrentStorefront methods unchanged.

FLOW
1 Reject malformed/duplicategroup orcandidateidentities; no silentdedup/smallerlist. Foroneowner+binding,
don'toffer twocapturedrevisions atonce. DoNOTguess owner/service frommethodCode splitting.
2 Call217ICheckoutShippingGroupGoodsFactsReader ONCE forwholecart/partition withnormaltrustedCartAccess
andrealversion. UseEXACTgroups+memberids/currency+FinalGoodsAmountAfterDiscountAndTax fromreturnedfacts.
Do nottrustplanmoney, recomputeTax, addDiscount, orcallPreviewpergroup.
ValidatefactsCartId/version/groupkeys/membercorrelation, no foreignextra/missinggroups accepted.
3 ForEACHcandidateofthatgroup call216QuoteAsync ONCE, exactowner/draft/revision/destination, samecurrency,
realgroupgoodsamount. Weight/itemcountmustremainnullif217returnsunknown; neverQuantityTotal/linecount/
ceil/truncate estimate. Ifphysicalcounttypeddecimal? andnotapprovedintegerauthority remainsunknown, notcast.
CallerCancellationToken forwarded unchanged. No price/legacy/staticwindowfallback.
4 Validatequote/provenancebinding EXACTexpectedservice/option/draft/sourceRevision and currency forQuoted;
malformed/wrongrevision/crossgroupresponsefailclosed, notadoptasprice. Membershipgroupidsarenot
ShippingareaGroupId (differentidentities); preservebothseparately.
5 ForQuotedcandidate with receivingweek, useREALmethodtimezone andONEserverUtcNow readfromIClock
percomposition, convert toreferenceLOCALDateOnly (canonicalTimeZoneInfo/knownresolver; no browserzone).
MapISO1..7toDayOfWeek correctly; validateweek/missingworkingbounds withoutfabricatinghours.
Use213BuildCustomerReceivingCalendar withMinDeliveryDays directly (NOsellerprepaddedagain).
Explicitinput AvailableDateCount maps consciously to213argument, whichcountsavailabledates, NOTcalendarspan;
doNOTreuseShippingMethodsOptions.DeliveryHorizonDays silently ascount. No newmagicdefault/limit.
KeepMaxDeliveryDays metadata, notprepday/slotcount/latestbookingcutoff (policyunapproved).
No cutoff/pastslottrimming/slotduration/reservation policy invented. Closeddayplaceholder ifneededonly
forignoredinternalcalendarmodel MUSTNOTappearasstoredhours; resultkeepsoriginalnullableweekmetadata.
6 No week/invalidtimezone/invalidworkinginterval/allclosed => noselectabledates with explicitreason,
neverlegacystatic9-12/fakeddefault. Currency/quoteconfigurationrefusals perCANDIDATEleaveothergroups
independent; doNOTwipeothergroups. Realdependencyexception/cancellationpropagate, no catch-to-empty.
7 ResultonecohesiveGROUPprojection: realmember/finalgoodsfacts, eachcandidate exactmonetarycomponents+
checkout/CODsplit, capture/snapshot/row/areaGroup provenance/type/settings/receivingweek,minANDmaxdays,
earliest+alloweddateintervals and explicitfailure/availabilityreason.
MonetaryQuoted/HasReceivingDates isNOTcustomeractivation/authorization; Eligibility staysNotEvaluated.
Do not exposea'CanCheckout'/'Active'true withoutactualselectedrevision/fulfillmentpermission source.
No writes/SaveChanges/order/reservation/payment/Cartconversion.

FOCUSED EXECUTED FACTS
Use REAL217CheckoutDirectory viaexistingShippingGroupGoodsFactsHarness forfinancialcase, controlled
ShippingCONTRACTSfixture (explicitpreparedresponse/no fakeactualproviderclaim), real213calculator.

twogroupssameSeller: groupgoods99/198 throughrealPromotion->Tax, passedtocorrespondingShippingcalls,
quotecomponents/provenance keptseparate; ONEwholecartquote (foreignportcallcounts).
min2Saturday/Sunday1/Mondayclosed/Tuesday2 date fromstoredweek, differentweekdayhours/fractions.
timezonecrossUTCdate changesreferencecorrectly, eachgroupowntimezone/minimum; no sharedmax/prep.
candidateunknownmeasurement => explicitunavailable/notzero; knowngroupnoquotesdoesn'tremoveothergroup.
wrongrevision/binding/currency/missingorfabricatedfacts failclosed; no legacyfallback.
allclosed/missingweek/badtimezone/workingbounds => no dates, explicitreason.
callerexception/tokenidentity retained, no replay/autoRetry/no write.
DIoneScopedcomposition andsame217reader, existingfoundationharnesswhenavailable; nonewframework.
BuildOrderTests+exactnewclass once. No194factreplay, no8/18/43/119oroldguards unlessnewconcernneedsnarrowcase.
No DB/container/browser/HTTP/merchantwrite/sharedrestart/configflag/schema/migration/frontend/CSS.

FINISH
Taskverbatim+docs/architecture/evidence/TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218/RESULT.md.
Recordactualgroup99/198flow+Tuesdayexample, exacttests/counts andNOTLIVE/remainingphysical/ownerpublication
selection+Offer/typecompatibility source thenrealprojection/selection/commitconnection.
TOPcanonicalrecovery+AGENTS+newchatguide accurateprogress; §29resolved, TB185paused/defaultOFF/
PendingAllocation/userdraft/currentrevision/unsavedtabs/Host5088FE3000preserved.
StageEXACTpathsONLY, no gitadd-A orhistoricalpostresultscript. Scopedcommit/push/fetchparity,
actualtime/corrections. STOP_FOR_ARCHITECT,Automatic-Next-TaskNONE; no219envelope.
```

## Added (production code)

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/CapturedGroupProjection/`

- `StorefrontCapturedGroupProjectionContracts.cs` — typed request/plan/candidate models and one cohesive
  group projection result, plus Order-owned stable reason codes. Candidate plans carry the FULL server
  owner key (`ShippingCapturedQuoteOwnerKind` + tenant + seller), exact `DraftId`/`SourceRevision`,
  `DestinationIdentifiers`, expected `ServiceCode`/`OptionCode` and an explicit `AvailableDateCount`.
  `StorefrontCapturedCandidateProjection.OwnerResult` is the owner's own `ShippingCapturedQuoteResult`
  record (Contracts-only reuse: money components, checkout/COD split, capture/snapshot/branch provenance,
  settings, stored week, min/max days) — never a re-derived copy.
- `StorefrontCapturedGroupProjectionComposer.cs` — the single service, scoped registration in
  `OrderModule.AddServices`. Dependencies: `ICheckoutShippingGroupGoodsFactsReader`,
  `IShippingCapturedRevisionQuoteLookup`, `IClock`. No Shipping/Geography Domain or Infrastructure, no
  `ShippingDbContext`, no second price/calendar engine.

## Flow as executed

1. **Plan validation before any foreign call** (`storefront.captured_group.plan_invalid`): empty plan,
   blank/untrimmed/duplicate group key, empty group, duplicate cart line across groups, empty/unknown
   candidate, duplicate method code, exact duplicate candidate identity, one binding (owner + destination
   + service/option) offered twice with a different draft/revision, incoherent owner key
   (Store without tenant, Marketplace with tenant/seller, Seller without a real seller id), empty
   `DraftId`, non-positive `SourceRevision`, empty `CountryId`, out-of-range `AvailableDateCount`.
   Nothing is silently de-duplicated or shortened.
2. **One 217 call for the whole cart/partition** with the normal trusted `SubmitCheckoutCommand`
   (CartAccess/version untouched). Returned facts must match the request exactly — same `CartId`, same
   `ExpectedCartVersion`, same group count/order/keys and the same ordered cart-line membership per
   group, one non-blank currency on the whole result and on every group, non-negative group amount —
   otherwise `storefront.captured_group.facts_mismatch` (fail closed, no quote work).
3. **Per-candidate 216 call, exactly once**: exact owner/draft/revision/destination, the group's real
   currency and `FinalGoodsAmountAfterDiscountAndTax`. `GroupWeightGrams` is forwarded only when 217
   published one (today `null`); `GroupItemCount` is always `null` — `PhysicalItemCount` is a
   `decimal?` and is **never** cast or derived from `QuantityTotal`/line count.
   Caller `CancellationToken` is forwarded unchanged.
4. **Binding validation of `Quoted`** (`storefront.captured_group.binding_mismatch`, no money adopted):
   returned `ServiceCode`/`OptionCode` equal the expected ones, `DraftId` and `SourceRevision` equal the
   exact requested revision, `CurrencyCode` equals the group currency, and all five money components are
   present and non-negative. A non-`Quoted` verdict keeps the owner's own reason code and never becomes
   zero/free. Group membership keys and Shipping's stored area `GroupId` remain separate identities.
5. **Receiving calendar** from the quote's stored week only: ONE `IClock.UtcNow` read per composition,
   converted to the reference local `DateOnly` using the method's own stored `TimeZoneId` (canonical
   `TimeZoneInfo`, no browser zone), ISO 1..7 mapped to `DayOfWeek` (1 = Monday … 7 = Sunday), each
   working day must carry a positive non-inverted interval (closed days contribute no interval), missing
   week / missing-or-out-of-range minimum days / bad timezone / bad week / all-closed each produce an
   explicit reason with **no** dates and no fabricated hours or static window. 213
   `BuildCustomerReceivingCalendar` is called with `MinDeliveryDays` directly (no seller preparation
   re-added) and the explicit `AvailableDateCount` mapped to its horizon argument (the accepted argument
   counts available dates, not calendar span; `ShippingMethodsOptions.DeliveryHorizonDays` is not reused
   as a silent default). `MaxDeliveryDays` stays metadata only.
6. **Per-candidate independence**: a refusal/refusal-code for one candidate or one group leaves every
   other group's facts and quotes intact. Real dependency exceptions propagate untouched (no
   catch-to-empty, no retry). No writes, no `SaveChanges`, no order/reservation/payment/cart conversion.

`Eligibility` stays `NotEvaluated`; the projection exposes no `CanCheckout`/`Active` truth. No
published/selected captured revision, no fulfillment owner/type/membership is derived here.

## Focused executed facts (new class)

`Tooba.Order.Tests/Checkout/StorefrontCapturedGroupProjectionComposerTests.cs` — **11/11 passed**.
Uses the REAL 217 `CheckoutDirectory` through the existing `ShippingGroupGoodsFactsHarness` (real
Pricing → Promotion → Tax), a controlled Shipping **Contracts** fixture with explicitly prepared
responses (no claim of a real provider), the real 213 calculator and a mutable clock.

1. Two groups, same seller: real goods 99.00 (100 − 10 + 9) and 198.00 (200 − 20 + 18) reach the
   corresponding Shipping calls; freight/packaging 15000+2000 vs 25000+0 stay separate with their own
   row/snapshot provenance; cart read once, `Prices`/`Promotions`/`Taxes` exactly 2 each = one
   whole-cart quote; one clock read.
2. Minimum 2 working days from a stored week (Saturday/Sunday working, Monday closed, Tuesday working):
   order local date Saturday 2026-10-24 → earliest **Tuesday 2026-10-27**, then the next available dates
   carry their own day intervals (08:30–11:00, 09:00–12:00, 10:15–13:45); `MaxDeliveryDays` kept as
   metadata.
3. One UTC instant (`2026-10-24T21:00Z`) → Tehran reference Sunday vs UTC reference Saturday, each group
   with its own minimum (1 vs 3) and its own `MaxDeliveryDays` (4 vs 7); earliest 2026-10-26 vs
   2026-10-27.
4. Unknown measurement: `MeasurementRequired` → explicit `Unavailable` with the owner code and all money
   `null`; never zero.
5. One group `NotConfigured` does not remove the other group: both groups and both fact sets survive; the
   second keeps its quoted 7500 total.
6. Wrong revision / service / currency / draft in a `Quoted` answer → `binding_mismatch`, `OwnerResult`
   null (no money adopted), exactly one call per attempt, no legacy fallback.
7. Fabricated/mismatched goods facts (wrong version) → `facts_mismatch` exception, no adoption.
8. Malformed/duplicate plans (duplicate group key, untrimmed key, duplicate cart line across groups, two
   revisions for one binding, exact duplicate candidate, incoherent Seller owner key, empty plan) all
   fail with `plan_invalid` **before** any cart read or quote call (0 calls asserted).
9. Missing week / invalid timezone / inverted working interval / all-closed week / missing minimum days
   → no dates with the specific reason, while the monetary quote stays valid.
10. Dependency failure propagates as the same exception instance, one call only, caller token identity
    preserved through the 217 cart read and the Promotion/Tax ports, and no `OrderDbContext` access
    (providerless context still throws on any query).
11. DI: exactly one scoped `StorefrontCapturedGroupProjectionComposer`, resolvable in a scope, with the
    217 reader resolved to the SAME `ICheckoutDirectory` instance and the composer itself scoped.

Also executed once: `ShippingGroupGoodsFactsTests` (217 foundation class) **9/9 passed**.
Build: `Tooba.Order.Application` and `Tooba.Order.Tests` — **0 errors**.

## NOT done / remaining (explicit)

- **NOT LIVE / NOT CONNECTED**: no Storefront/checkout consumer, no public route/`ISender` request, no
  live flag, no projection/selection/commit step. Existing Storefront methods are unchanged.
- The candidate plan is still caller-supplied trusted data: **owner/draft/revision/service selection and
  fulfillment responsibility must come from a reviewed server publication/Offer/fulfillment authority**,
  never from a browser, and the composer deliberately does not derive them.
- Physical weight/item count have no authoritative source yet (stay `null`), so measurement-gated rows
  remain `MeasurementRequired`.
- No cutoff / past-slot trimming / slot duration / holiday / capacity / reservation policy invented; no
  min/max-day → preparation/lead conversion.
- No schema, migration, HTTP, MediatR, foreign Contracts/Domain, frontend/CSS, seed, config flag or
  user-draft change; no Host `:5088` / FE `:3000` restart; no shared DB, container or browser use.

## Time

Actual ≈ 33 min against the 25-min box / 17-min implementation stop; two correction rounds (one test
expectation for the explicit `AvailableDateCount`, one support-file using placement). Recorded honestly —
not a process PASS.

Task status for Architect: `STOP_FOR_ARCHITECT`. Automatic next task: NONE. No 219 envelope.

## APPENDED CORRECTION (TB-218-R1, 2026-10-25)

The claim above that the plan validator enforced one captured revision per binding is **superseded**: the
shipped 218 code checked this **per group only**, with Ordinal delimiter-string keys that included the
group destination and the raw service casing. `218/ARCHITECT-REVIEW.md` records the defect and
`../TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218-R1/RESULT.md` records the executed repair: one
request-wide typed `StorefrontCapturedPublicationBinding` (exact owner tenant/seller, trim +
OrdinalIgnoreCase service/option, no destination/method label) holding exact `DraftId` + `SourceRevision`,
enforced before any goods reader/Cart/Pricing/Shipping call, while the same binding with the same exact
draft+revision across groups stays lawful. Everything else described above (numbers, isolation, calendar,
remaining gaps, NOT_WIRED/NOT_LIVE status) still stands.