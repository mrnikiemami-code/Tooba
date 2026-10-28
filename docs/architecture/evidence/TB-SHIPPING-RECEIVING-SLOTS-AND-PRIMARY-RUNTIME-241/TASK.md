PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-RECEIVING-SLOTS-AND-PRIMARY-RUNTIME-241
Parent-Task: TB-SHIPPING-USER-TRIAL-DELIVERY-CHOICE-240-R7
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — restore configured receiving time slots on one primary runtime
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: RESTORE_HOURLY_RECEIVING_SELECTION_END_TO_END_AND_CONSOLIDATE_3000_5088
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0 — Latest user authorization supersedes old no-cutover rule

User: "ساعت داشته باشد9تا10 10تا11 همینطور البته با تنظیمات"; "قبلا داشتم انتخاب زمان تحویل الان ندارم";
NOW: "سریع این مشکل رو برطرف کن و بیار روی نسخه اصلی...دو تا نسخه نباشه...ریکاوری هم به روز کن".
This is a user-reported REGRESSION to restore, not optionalnewfeature. Outcome: real configurable hourly receiving slots, independentpergroup, valid Continue/payment/order, ONE primary user surface http://127.0.0.1:3000 backed by5088, recoveryaccurate.
This expressly authorizes safe source/runtime promotion and stop/restart of verified Tooba primary/task processes for consolidation. It SUPERSEDES earlier task-local absolute bans on updating original3000/5088/watchers or stopping extra taskruntimes, but DOES NOT authorize discarding unsaveduserwork, closing arbitrarytabs, account/datareset, forcegit or globalDockerrestart.
No screenshots/images/visual-worker-review/viewportmatrix. User's uploadedimage may be reference but no new images requested.
Original240-minute deadline remains MISSED. No invented replacementpromise. Operational executionbox60minutes with checkpoint10 exactselectedslot/configcontract, checkpoint35 end-to-endsave+finalsame-slotacceptance, final20 primarycutover+functionalchecks/recovery; report slippage at checkpoint, no silenthourlongdetour. No code-onlyPASS or automatic242.

## 1 — ONE worker, retain the now-executed240-R7 corrections

Read ALL3TOPs, slot-duration userrequirement/correction,240-R7TASK/RESULT and ARCHITECT-REVIEW. Current main/evidence db5ae3a6738bcf2fff45cd3b19ca769cb07623c5, R7 appsource acb7f16e631927ea6bd4b99d34f84720f23fb9ba.
R7 has NOW executed and its datechoice/onevalidationreason fixes are accepted WITHIN THAT SCOPE; keep them in this implementation. Safe-fast-forward to this issue commit; no second worker, discard/reset/stash, duplicatevalidator or replay of completedR7engineering. Record actualbase/parity, noforce/rebase/amend.
240-R7's former no-backend-calendar scope is superseded by THIS explicit full-flow authorization; it alone cannot close missinghourlyslots.
Do not repeat completed seller/reference-data/multiowner/coupon/history waves, rebuildtestframeworks or create new experimentalportfamilies. Work in isolated buildpreparation until primaryready; primarycutoverbelow is finalauthorizeddelivery, not anotherpermanentfork.

## 2 — Small explicit scheduling policy, persisted in existing authoring owner

Existing receiving-day opens/closes+timezone remain authority. Add an OPTIONAL explicit method-level receiving slot duration in MINUTES to SAME ShippingAuthoringWeeklySchedule + relevant read/write/editor-document Contracts and adminweeklyeditor. UI label e.g."مدت هر نوبت تحویل (دقیقه)", real operator choice/positiveinteger. No capacity/cutoff/holiday/funder or new schedulingmodule.
Decision for this restoration:
- Explicit positive whole minutes1..1440; blank/null means LEGACY FULL DAY WINDOW, not inferred60. Invalid/zero/negative/fraction/overlarge refuses before mutation with stable localizedfieldreason.
- Configured day09:00–17:00 +60 =>09–10,...16–17; +120=>09–11,...15–17.
- Generate complete nonoverlapping slots anchored to each authoredopening; do not overrunclose or create shorter tail. If an active day's duration yields zero completeslots, refuse configuration/publication honestly rather than publish an unusableworkingday. A non-full remainder is not selectable, clearly explained in adminhint; no silent rounding of hours.
- Closedday yieldsnoslots; methodtimezone and accepted213 workingday/minimum-day/available-DATE-count semantics unchanged. Countdistinctdates, not countofslots.
- Preserve storedseconds/fractional100nsticks, not HH:mm roundtrip businessmath. Slotlength is integer-minute ticks added to actualopening, no JSDate/floatmoney logic.
- Legacy/unset captures retain ONE exactoriginalopen..close choice per eligible date. User can still select a timewindow; they are NOT silentlysplit60.
Keep R7's helper/blocker instead of duplicating it; extend its time-slot blocker in the same authority. Avoid further growth of already-oversize storefront-shipping.tsx; put the slotcontrol in an existing-owned small component/helper instead of another large inline block. R7 per-group date currently hardcodes fa; pass actual locale for the touched date/time labels, no invented English/Jalali mismatch.
Use one nullableint column on existing Shipping weeklyschedule if relationalmapping needsit, ONE additive Shipping-ownedforwardmigration withshapeCHECK; no backfill/updateallsettings. Normalmigrationrunner plan/apply explicitlyauthorized on VERIFIEDlocaltarget only after reviewingactualpending set. No directSQLbusinesswrites/historyrewrite/baselineregenerate.

Adminconfig must be saved through existing ExpectedRevision/owner-auth/CQRS endpoints and normalApply path; editable/view modes/FA+EN/errors consistent. Existinguserdraft rates/week/pointer are not silentlyupdated just to manufacturehourlydemo. Explicitconfigure/publish one task-owned DEVmethod under its realowner(s) for proof; changingoperator slotduration invalidates staleconfirmedchoices.

## 3 — SAME immutable capture and SAME calendar, no browser-only hourly engine

Slotduration belongs to immutable selected authoringrevision. Root sourceconfirmed ShippingAuthoringRevisionWeek and weeklywrite currently contain onlytimeZone/days, codecCurrentFormatVersion1.
Use a VERSIONED backward-compatible capture change: new slot-configured captures use supportedformat2; existingformat1 and unsetlegacycaptures remain readable with exactoldhours. Decoderrejectsunknownformat/corruptduration and validatesrowheaders. Do not mutate oldpayloads or keepformat1 while giving oldreaders silentlydifferent schedulingsemantics.
Update allactual formatgates (Shippingcapture/selectedpointer/quoteContracts + OrderacceptedEvidence formatvalidation and DBshapeCHECK if it restricts1) to explicitly supportknown1and2 only. New forwardmigration for an existing restrictiveOrderCHECK is allowed ONLYif source actually requiresit; no appliedmigrationedit/newhistorytable. No hardcodedformat1testweakenedwithout dualformatproof.
Pricing remains216; quoteexports same immutable receivingweek plusduration. Extend SAME213 receivingcalendar/reader-free218core or existingcanonical helper once so bothread and finalacceptance use identical slots. No parallelShippingOperatingSchedule mutablefallback, secondquote/Promotion/Tax pass or calendarengine.
Server transport should group unique eligibleDATES with nested server-generated slots (stableidentity + exactstart/end + displaylabel), additivecompatibility fields only as needed. Avoid duplicate same-dateentries that currentstrictdecoder would reject. A legacy exactdaywindow maps to one explicitslot. Onegroup's dates/slots must never be unioned withanothermethod.
No defaultonehour in frontend, no subtractingpreparationtwice, no logical newslotcapacitypromise.

## 4 — Selection, paymentpreview, finalcommit and history carry the SAME chosen subinterval

Storefront existingCapturedGroupChoice extends date+method with ONE selectedslot serveridentity. DATE changes clear previousslot unless that exactslot exists on newdate AND user policyexplicitlyretains; defaultsafe choice requireselectionagain. METHOD/owner/capture/calendar/slotdurationchange invalidates stalechoice. Keep groupindependence and allgroupsrequired.
Afterdate show actualconfiguredslotradiochoices09–10/10–11/...; showselectedstate only when parentformreallycontainsit. Restore legacyhour-selection usability within existing Shopeiva cards, no newframework or globalredesign.
Continueenabled only completeaddress+currentquote+method/date/slot EVERYgroup; onevalidationresult and localized owningblocker fromR7 retained. No invisiblecheckbox/typedhour/hardcodeddefault.
Browser sends opaque server slotidentity or exactlosslesswindowecho associated with date/group; server MUST resolve it in SAME freshlycomposedcalendar, not trust formattedhumanlabel/start/endfromclient.
CapturedSelectionWriter.BuildEvidence stores CHOSEN receivingdate/openTicks/closeTicks/timezone, not the day's entire9–17bounds. Cartgroups/paymentpreview displayCHOSENwindow. FinalCheckoutDirectory.CapturedAcceptance finds/revalidates SAME exactchosen interval against currentselectedcapture/cart/owner/type/quote/calendar inside acceptedtransactionboundary. Changed/outside/unknown/date-mismatchedslotrefuses BEFOREreservation/order/conversion, no silentearliest/full-day replacement.
OrderShippingGroupPlan/materializer keep compactexistingAcceptedEvidence; no fullweekly/slotsJSON perorder and no redundantownerfield. Existingreceivingdate+100nsopen/close is historicaltruth; no newOrdercolumn just to store a displaylabel/slotindex if unnecessary.
Historicalorders/captures untouched and no repricing. Newformat2order may require updating documentedformatwhitelist/CHECK, not bypassingthem. Same-key replay stilloriginalorder/payment.
Legacyformat1withoutduration control proves stored9–17 can stillbe selected/committed exactly; configuredformat2 control proves14–15 stays14–15 through selection/preview/order/read after laterdurationchange. ManualPendingonly; providerexactlyonce/paidPSP stillnotclaimed.

## 5 — Main means ONE primary3000/5088, no more 310x user handoff

Inspect actualrunningprocess/source/edition/DB identities first; preparefiles/binary in staging, no usertabreloadduringprep.
Currentlyoriginal3000/5088 is oldSingleStore/store-alpha while currenttestedmarketplace uses5190/tooba_marketplace_tb239 and3103/3104. Do NOT falsely present a portchange as data/configparity.
For the user's currentMULTISELLER trial, promote the SAME testedMarketplace installation + SAME taskMarketplaceDB to primary5088 with primaryFE3000 on currentmain. This is explicit LOCALruntimepromotion authorization. Preserve originalstore-alpha DB ENTIRELY and privatelyarchive its previous runtimeconfiguration so SingleStorecan be selected again; do not merge/clone/move users/orders/drafts/editions, relabelTenantNULLasStore or silentlyrewriteprimaryDB. Report activeMarketplace/TenantNULL/database plainly in final. This does not certifySingleStorephysicalisolation or permanently remove supportededition.
Preserve originaldraft/review230data in theirdatabase; no originalrate/pointer edits. OldSingleStoreconfiguration/data retained, no seconduser-facingapplicationleft running as alternateversion.
Primarycode must be actualmain(pushedverifiedSHA); runningbinary and frontend mustmatchitsSOURCEtree. Iforiginalwatchedcheckout isdirty, noreset/stash or massstage; use stagedimmutablebuild fromsamecommittedmain served at3000/5088 and report actualcanonical path, not an extrauserport.
Stop primarydevwatcher BEFORE changingitsdisk/source to prevent uncontrolledHMR. Do not automaticallyreload/close user's tabs; preserve theircurrentDOM/localinput. Where needed snapshot ONLYrelevant Toobaform/cache state privately via authorizedexistingproductstate/formfields beforecutover; no serverApply to preserveunsavedadmindraft and no secret ingit. No claimingunknownunsavedstate preserved if notproved. User's request authorizes plannedcutover, not repeating225lostedits.
Verifyonly intendedoldPID/commandline/ports, stopthoseexactprocesses, then start preparedcurrentHost5088+FE3000. No kill-by-name/globalDockerrestart. Checkhealth,APIroute andactualproxy->5088 (not hidden5190), realcurrentedition+connection identities, language/geography/auth.
Use NEWtask-owned visiblebrowserprofile on PRIMARY3000, normalCSRF/BFFlogin for testbuyer, no phonewall/no cookie/JWTinjection/devactor. SameMarketplaceDBretains testbuyer/cart; do not switch the user's currentlyopenprofile/accountbehindtheirback.
Verifyconfig+date+hourchoices+Continue/paymentform with a TWO-real-seller normalcart on PRIMARY, separateproofcartfinalOrder14–15 persisted if appropriate. LeaveTWO-sellerunsubmittedreviewcart signedin on main, not onegroup leftovers. OnlyprimaryURL goes to user.
ONLY AFTERprimaryworks, stop verifiedTASK extra FE3100/3101/3102/3103/3104/... and Host5188/5189/5190/... processes created by thispipeline, preservingDBs/profiles/files and userbrowserwindows/tabs. No newpermanentports. Do not delete worktrees/profiles/DBs to make itlooksingleversion. RecordactualstoppedPIDs/ports andonly3000/5088active. Never killunrelatedprocesses. If a taskbackend is neededtemporarily for rollback, it is temporary and finalstate mustrecordit, not pretendconsolidated.

## 6 — Focused functional proof, finalscope and recovery

No screenshots/visualwork. RealHTTP+DOM+storedfacts:
- normaladmin/owner save9–17/duration60,publish -> BOTHgroupcurrentdayhourchoices; selecting14–15 A leavesBunchanged;
- single-group andtwo-sellerform selectdate+time+address -> ONEselectionPUT200 -> currentpaymentform showsCHOSENhours;
- finalsubmit storeschosenexactdate/ticks/timezone inrealOrder, laterduration120newpublication neverrewritesoldOrder; stale60-slot intentrefuses ifno longerlawful;
- nulllegacyformat1roundtrip, close-day/no-fullslot/invalidduration/out-of-range/reforgedwindow controls.
Only changedfocusedtests+necessarytypecheck/build; no fullsuite or environmentrebuildloop. No broadcorrelation/payments/ACLrewrite.
Allimplementedpaths mustconnect; do not ship adminsettingonly/frontendbuttonswithparentfull-windowcommit andlabelPASS.
Receiptis exactSHA/runtime/edition/DB/migrationplan+apply/selectedslot and finalprimaryURL, no model/livetestconfusion.
No new readiness/paidPSP/VisualACCEPTclaim. Checkedingates remainOFF, localprimaryenv mayenableexisting3capturedflags for user'sauthorizedtest; TB185paused/defaultOFF/PendingAllocationunchanged.
Preserveallbusinessdata,nomigrationreset/seedSQL/historyrewrite/fullpayloadperOrder. Normal task-owned DEVconfiguration/cartwrites allowed. Unresolvedexternalproviderconcurrency/238unknown22001/227mobileempty-note/225-R1LOCALloss retained.

Save TASKverbatim docs/ai/tasks/TB-SHIPPING-RECEIVING-SLOTS-AND-PRIMARY-RUNTIME-241/TASK.md + evidenceTASK; RESULT/RECEIPTS ownfolder.
Update ALL3TOPs+deadlineledger: currentreceivingslotregressionstatus, integrationofR7, exactprimary3000/5088/edition/DB/SHA,
no activealternativeuserports, actualremainingdefects, missedoriginaldeadline(noreset), allprofiles/data preserved. No hiddennewfaketimepolicy.
Normalexactpathcommit/pushHEAD:main/fetchparity, no amend/force/reset/stash/rebase/CRLFchurn.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE, no automatic242.

## RESULT CONTRACT — actual canonicalresult, no probe

FullSHAsorNONE, no notes inSHAfields. Appendtask-endmarker aslasttransmittedline aftertemplate.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-RECEIVING-SLOTS-AND-PRIMARY-RUNTIME-241
Parent-Task: TB-SHIPPING-USER-TRIAL-DELIVERY-CHOICE-240-R7
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — restore configured receiving time slots on one primary runtime
Mode: RESTORE_HOURLY_RECEIVING_SELECTION_END_TO_END_AND_CONSOLIDATE_3000_5088
Status: PASS or PARTIAL or BLOCKED or RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: actualfullissuecommit
Base-SHA: actualfullreceiptbase
Code-SHA: actualfullcodecommit or NONE
Evidence-SHA: actualfullevidencecommit
automaticNextImplementationTask: NONE

PRIMARY3000URL+VISIBLEsignedinprofile, actualedition/DB/SHA, bothownerdate+slot selection and exactpersistedwindow,
adminconfig/capturecompat/negativecontrols, migrationtarget, stoppedtaskports and preservedalluserwork,
R7integration/elapsedoverrun/remainingdefects, NOscreenshots/noinferredVisualACCEPT/PSPclaim.
END_TOOBA_WORKER_RESULT
```

END_TOOBA_TASK
