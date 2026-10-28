PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-USER-TRIAL-DELIVERY-CHOICE-240-R7
Parent-Task: TB-SHIPPING-TEST-SESSION-HANDOFF-240-R6
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — repair the user's actual delivery-choice trial
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: REPRODUCE_MISSING_DELIVERY_TIME_AND_DISABLED_CONTINUE_THEN_HAND_OFF_TWO_SELLERS
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0 — User defect report, not another successful-code report

User says: "فقط یک فروشگاه بود زمان ارسال نداشت وقتی هم انتخاب کردم دکمه پرداخت روشن نشد".
Treat this as an OPEN user-facing defect; no VisualACCEPT and no blame that the user failed to find required inputs. R5/R6 previous scopedacceptance does not close this report.
Three outcomes: correct independent delivery date + authored receiving interval visible for each group; real selection commits controlled state and enables Continue when form is valid; when blocked, user sees the SHORT exact owning reason instead of a silent grey button. Handoff must contain TWO actual sellers, not the one-seller leftover cart.
Focus20minutes, checkpoint8 exact reproduced predicates + event/state trace, checkpoint15 corrected realUIContinue200/paymentform. Original240deadline alreadyMISSED; no reset/newpromise. No screenshots/images/visualreview/viewportmatrix. Only HTTP/DOM/events/persistedfacts; appearance left to user.
No new owner/quote/calendar/slot engine, no backendwaiver to force payment, no invented dates/times. Keep accepted API/order/coupon/history authority unchanged.

## 1 — Preserve user's NOW active edits; isolate diagnosis and fixes

Base3ba69d239fddacf850253c820d89a3bdce5a170a; appsource763506b3487de1b80a799bffe91c00b96dac4e7f + earlieracceptedshippingfiles.
R6 VISIBLE USER window is installedChrome PID15632, profileD:\temp\tb240r5-chrome/CDP9343, FE3103, titleاطلاعات ارسال|توبا.
User has now entered/selected data there. DO NOT enumerate/read/automate/reload/close that window/profile, change its cart/account/destination or edit the checkout its FE3103 watches/HMR. Do not discard user's new LOCAL work to reproduce.
Original3000/5088, handoff3100/5188+tb238-handoff-chrome,R4profile9342/FE3102 likewise frozen. No process kill-by-name/globalDockerrestart.
Use isolated NEWworktree from issuance + freeFE3104 pointing to SAME taskHost5190/MarketplaceTenantNULL/tooba_marketplace_tb239. Reuse dependencies; no install/newautomationframework/backendrebuild/migrations/seed/reset.
New task-owned profile/freeCDPport ONLY, normal authenticated buyer: reuse privately available normaltestcredential in NEWprofile if safe, but do not modify currentR5buyer's cart; preferably ONE new publicregistered syntheticbuyer/BFFsession, randompasswordTEMP only. This already-authorized setup prevents mutating usercart. No OTP/authpolicy change, sessioninjection,devactor,fakeheader or secret inresults.

## 2 — SOURCE-CONFIRMED risk seams, live cause still must be measured

Root inspected current:
app/storefront/storefront-shipping.tsx:
isValid = required first+last (or savedrecipient), contactMobile, province,city,postalAddress,postalCode AND capturedSelectionComplete(currentPreview,choices).
Continue is disabled={!isValid || busy}; there is NO displayed owning reason for a missing required field/date/confirmedresponse.
Do NOT simply change isValid to true, drop address fields, accept stale response, or auto-submit firstmethod/date.
Capture exact validity facts/field-completeness booleans (not personal values) + current responseidentity + selection choices before/after actual click. Identify whether user's reported unavailable date/time is unresolveddestination, pendingresponse, absentserverdates, stale calendar or display/controlledstate mismatch; don't infer it from "tests passed".

app/storefront/storefront-shipping-group-cards.tsx/CapturedMethodRow:
active = selectedDate ?? method.earliestEligibleDate ?? method.availableDates[0]?.value ?? "".
DateChipStrip uses active for selectedstyling even if selected=false and no capturedChoices entry exists. This is a SOURCE-CONFIRMED apparentselection/formstate mismatch: a shown nearestdate can look chosen without a real confirmedchoice.
Current method/date clicks call onSelectMethod(groupKey,methodKey,date); both should update SAME controlledchoice and each group's own interval. Inspect actual loadedsource parity, handler and any capturedContextKey invalidation. No repeatedclick/render may clear lawful selection if context truly unchanged.
Current interval line derives activeDate.intervalText, so missing interval must be traced from REAL method.availableDates/decoder/currentstate, not filled by a constant09:00–17:00.

## 3 — Narrow repair on EXISTING page/cards, one validation authority

A. Show unselected state honestly: nearest marker can remain a recommendation, not selected/aria-pressed until that group's method+date is actually committed in parentstate. A user click on a realdate must select that method+date atomically for ONLY that serverGroupKey; preserve othergroup's choice. Methodchoice may select serverearliest only through an explicit usermethod action if existing policyretained; no render/effect auto-selection.
Use existing controls/template, no newcalendar/popupframework. Provide accessible selected semantics so DOMfunctionalassertions match state.
B. Each group exposes its OWN available receiving dates and chosen day's OWN authored interval clearly as زمان تحویل / بازه تحویل. If the server has no lawful dates or interval, show the actual short reason, do not hide the control silently or invent a schedulablehour. Weekclosed dates remain excluded, no new slotduration/preparation/capacity/globalpromise.
C. Derive enabledstate AND a short localized owning blocker from ONE shared validation result (extract a small pure helper only if it actually replaces the repeateddecision). Reuse existing capturedSelectionBlocker and existing field requirements; no shadowvalidator.
When blocked, put a concise reason next to Continue and/or owning field with aria-describedby/aria-invalid appropriate to interaction, e.g. choosecity, completepostalcode, selectdate for namedgroup, currentquote pending. No giantchecklist, no technicalsnapshot/tokens in productflow. Do not ask user to guess whatgreybuttonmeans. For complete address+all realgroupchoices+confirmedresponse, buttonMUSTbeenabled.
Keep endpointbusinessvalidation finalauthority, exactechoes/goods/rootrevisionTEXT and allgroupsrequired. No price/date normalization, no bypassunsupportedgroup, no defaultIRR/zero fabricated.
D. Cover single-group and two-seller paths (R6 usercart wasonegroup, cannot only prove multicart happy path). Use same modified helpers/render path, no separate demoUI.
If live proves the actual cause is only incomplete requiredaddress, still correct the apparentselecteddate and silentblocker experience; do not return "user needs to type" as the repair.

## 4 — Actual end-to-end functional proof, NOscreenshots

Use realPDP/addcart or normalAPIunder samebrowser session to form a NEW2seller cart with Arman+Digistyle (existing3offers/data), not mockcard or relabeledseller. Do not say "only2products" prevents2sellers.
Record exact currentservercart/groupcount and sellers; each group's actual dates+interval (readcurrentDEVrate, SellerA68000+3000, SellerB47000+2500 if unchanged, never assume them).
At each step record DOM + requestcounts:
1 incomplete address or no selectedmethod/date => Continueblocked with EXACT user-readable reason and no fakehighlight;
2 pickdate/groupA => parentgroupAchoice+owninterval correct, Bunchanged and Continue explains B or remainingfield;
3 pickdate/groupB + complete validform => Continueenabled, displayownhours for each, no render clears choices;
4 click actualContinue => ONEselectionPUT200, allgroupswithownechoes + rootrevisionTEXT, normalexistingpaymentform reached200 with ownfees; no checkout/order before finalclick.
Also single-group same state/validation path works. Keep typedaddress throughout destination/date transitions and reject staleresponse. No need repeat full coupon/history/providercheckouttest matrix or fullsuite.
Run only narrow meaningful facts for confirmedregression/dateparentstate/owningreason/groupisolation + necessarytypecheck; no fullHost/Order/Shipping/visualsuite.
Do not spend box on unrelated oldguards, reference-data bootstrap or paymentframework.

## 5 — Correct handoff, not an API-only or headless-only success

Leave a NEW VISIBLE installedChrome window NORMALSIGNEDIN at the corrected isolatedsurface with TWO real seller groups and currentlawfuldates/intervals. Prefer leave form unsubmitted so usertestsactualselection; do not consume reviewcart by finalOrder. A separate testcart may be used to prove Continue200/paymentform; second normalcart under same freshbuyer authorized.
Verify actual /api/auth/me200 sameintendedbuyer, currentcart200 twoactualsellers, route200 noOTPwall. No password/token/cookie/guestsecret printed. Taskprivatecredentialretain outsidegit.
Report exact URL/profile/windowtitle, actualcartgroupcount, field/date completionstate and visibleblockeror enabledbutton at handoff. Do not announce "ready" from healthcheck/headlesspage/onegroup leftovers.
Afterhandoff freeze NEWwindow/sourcewatcher; do not automate/reload/HMR it again. Currentuser3103 remains unchanged; do not silently redirect theirwindow or cart.

## 6 — Evidence/recovery/protocol

TASKverbatim docs/ai/tasks/TB-SHIPPING-USER-TRIAL-DELIVERY-CHOICE-240-R7/TASK.md + evidenceTASK. RESULT/RECEIPTS in evidencefolder, NOscreensrequired.
ALL3TOPrecovery updated as currentUSERTRIALFIX, scopedolderacceptance not erased; originaldeadlineMISSED/timeledgerhonest.
KnownR6DockerDesktoprestart affected shared engine even though applicationPIDs not restarted: do not call infrastructure whollyuntouched or repeat globalrestart. Actualruntime unhealthy => reportowningfailure, not blindreset.
No schema/migration/reseed/directSQLwrite/crossDBcopy; normal newtaskbuyer/cart only. Checked-ingatesOFF/localtaskenvON; TB185paused/defaultOFF/PendingAllocation.
Knownexternalproviderconcurrency/238unknown22001/227mobileempty-note/225-R1LOCALloss retained. No inferredVisualACCEPT/PSP/paid/releaseclaim.
Exactpathcommit/pushHEAD:main/fetchparity; no amend/force/reset/stash/rebase/CRLFchurn. No automaticnewtask.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

## RESULT CONTRACT — canonical actual result, no probe

ActualFULLSHAsorNONE, no editorialnotes in SHAfields. Appendtask-endmarker as finaltransmittedline.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-USER-TRIAL-DELIVERY-CHOICE-240-R7
Parent-Task: TB-SHIPPING-TEST-SESSION-HANDOFF-240-R6
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — repair the user's actual delivery-choice trial
Mode: REPRODUCE_MISSING_DELIVERY_TIME_AND_DISABLED_CONTINUE_THEN_HAND_OFF_TWO_SELLERS
Status: PASS or PARTIAL or BLOCKED or RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: actualfullissuecommit
Base-SHA: actualfullreceiptbase
Code-SHA: actualfullcodecommit or NONE
Evidence-SHA: actualfullevidencecommit
automaticNextImplementationTask: NONE

UsableVISIBLEauthenticatedURL/twoactualsellers, measuredrootcauseandbefore-afterdate/validationstate,
realContinue200/paymentform, handoffcartunsubmitted, exactelapsed/overrun, preservation. NOscreenshots.
END_TOOBA_WORKER_RESULT
```

END_TOOBA_TASK
