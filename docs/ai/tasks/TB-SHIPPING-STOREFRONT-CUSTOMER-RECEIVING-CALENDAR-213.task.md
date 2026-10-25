PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-STOREFRONT-CUSTOMER-RECEIVING-CALENDAR-213
Parent-Task: TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 09051dcb7dd6cfb212f940bd5615cb6bbae0dafd
Mode: BOUNDED_EXISTING_CALCULATOR_CUSTOMER_RECEIVING_CALENDAR
Timebox:20min; stopimplementation12; final8 focusedtest/evidence/scopedcommit/push. One correction max.
This exactTaskID is DISTINCT from WITHDRAWN draftpublishsnapshot213. No publishcopy approved.

USER DECISION (canonical rules§29; do not reask)
Delivery1–3days means customerRECEIPT fromorder, prep+carrier insidepromise.
Amountcondition = finalGOODSamount of SAMEshippinggroup AFTERdiscount ANDtax (not wholecart).
Authoredweek = customerRECEIVING days/hours, NOT carrierpickup schedule.
LATEST explicit clarification: "به روز تعطیل ... روز تعطیل را بپرد از روش" — skipclosed days.
Reference orderdate offset0; nextworkingdate#1, next#2 earliest formin2.
Example orderSaturday, Sundayworking#1, Mondayclosed, Tuesdayworking#2 => earliestTuesday.
Selected dayhours must be its own configured interval, never static9-12 or browserclock.
No guessed cutoff, holiday calendar beyond storedweek, slot splitting/capacity, or maxrange->prep mapping.

READ
TOP AGENTS/recovery +canonicaldecision§6/8/12/29 +Orderexistingcalculator/associatedtests.
Use applicable repo Analyze/Migrate skill for selectedscope only; no broad moduleaudit.
Standinglocaldev authorization persists; no newapproval.
190–211accepted; don't replay. 212correctedfactsaccepted, proposedlossycopyrejected.

ONE CONCRETE CODE SLICE
Extend EXISTING Order.Application/Storefront/Services/StorefrontShippingCalculator.cs with an ADDITIVE
pure receivingcalendar calculation; use cohesive typedinput/output records in existing StorefrontModels
if necessary. Do NOT build another dateengine/service/store/framework.
Inputs are explicit, trusted future server-side calendar configuration:

reference local orderDate (DateOnly) already determined in METHODtimezone by caller;
minimumworkingdayoffset;
configuredweek: 7uniqueISOdays, isWorking, eachdayown start/end interval;
explicit callerprovided existing deliveryhorizon (do NOT infer horizon frommaxdeliveryrange).
Preserve timezone provenance asmetadata if relevant; do not use browserlocale/timezone, DateTime.Now,
implicitUTClocaldate or defaulttimezones. Existing canonical timezone resolver reused if conversion needed.
Output: earliesteligibleDate + availabledates with SAMEday's configured receiveinterval, NOT staticwindows.
Skip EVERY closeddate both when counting minimum and when building availabledates. Never offer closedday.
Allclosedweek => explicit noavailability (bounded,noinfinite loop), not fake allopen/defaultwindow.
Validate incomplete/duplicate/outofrangeweek/invalidworkingintervals via existing error/result conventions;
no silent repair/defaultdays. No lower/upper reversal/clamping. Preserve TimeOnlyfraction values.
For offset0, inputreferenceDate may be eligible if working; intradaycutoff/pastslot filtering NOT
implemented/claimed here; caller must not expose result as actual futurebooking without later policy.
No preparationdays addition: minimum is already CUSTOMERpromise, don't doublecount sellerprep.
Keep maxdeliveryrange intact in existing authoredmodels; this routine doesn't silently drop/remap it.

LEGACY/CONSUMER BOUNDARY
Existing CalculateMinimumDeliveryDate/BuildDeliveryDates/staticwindows callers remain byte-behavior
compatible. NO change to ProjectAsync/SaveSelectionAsync/CommitAsync, DI/flags, livecheckout price,
draftpublication, existing carrierpickupOperatingSchedule.
This slice is a tested computation primitive; it DOES NOT claim Storefront integration complete.
No ShippingforeignDomain/Infrastructure refs inOrder. Future connection only Contracts.
No price/Free/CarrierAPI policy, no schema/DbContext/migration/Host/frontend/storefrontCSS changes.

FOCUSED TESTS ONLY (exact new routine on existingOrdertestproject)

min2, allworking => reference+2;
min2, oneclosedinterveningdate => skipsit (explicitSaturday/Sunday/Mondayclosed/Tuesday case);
referenceonclosedday; consecutiveclosed/weekboundary; closeddate aftereligible not offered;
differentweekdayhours stayattached tocorrectdate, fractionalpeerpreserved;
allclosed => noavailability withoutloop;
malformedweek/workinghours rejected, not fakefallback;
twoindependentgroupinputs keepdifferentminimum/hours, no sharedmax or othergroupmutation;
existinglegacy calculator focusedfacts remain unchanged.
One projectbuild ifneeded and one bounded focusedtest run, no wholesolution/Host/645suite/reconfidence.
No browser/DB/container/HTTP/businesswrite, no runtime restart or packageinstall.

FINISH
Persist task verbatim and docs/architecture/evidence/TB-SHIPPING-STOREFRONT-CUSTOMER-RECEIVING-CALENDAR-213/RESULT.md
with exactfiles/tests/counts and example dates. Scope stage explicitpaths ONLY (NEVER gitadd-A); leave all
unrelatedhistoricalevidencehelpers untouched/untracked, don't execute post-result scripts.
Update TOP recovery+AGENTS: userdecisions resolved, calendarprimitive done/notwired, nextsingleintegration
step and remainingowner/publishedauthority/measurements/taxallocation; no generic audit.
Scopedcommit/push/fetchparity, actualtiming, STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
