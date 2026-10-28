PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R5
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R4
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — fresh-cart functional closure and handoff
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: COMPLETE_FRESH_CART_FORM_DOUBLE_CLICK_AND_FAILURE_RETRY_WITHOUT_IMAGES
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0 — Close remaining functional gap, not another backend wave

Source74af687bf243213e54a1d511f6da989fa03f26a4, evidence/main829c032bed767f534709ed1d60bcab7930f38e39. Safe fast-forward isolated checkout to issue commit.
R4 real owner recovery, partial/invalid/clear coupon, mixedOrder, same-key manual replay and Order-only immutable history are ACCEPTED WITHIN THE MANUAL CONNECTED SCOPE after root source/receipt review. Do NOT repeat that whole matrix or refactor completed backend.
Only remaining functional scope: normal NEWcart payment form, actual UI Continue/finalclick same-tick request counts, failed-destination explicitretry functional check, usable authenticated handoff. Small source-confirmed UI state fixes below are allowed.
Focusbox15 minutes, checkpoint5 newbuyer/newcart session ready, checkpoint10 real precommitpayment form and no localguard. Not a new delivery promise. Original240-minute deadline MISSED. No automatic241 or another helper-only PASS.
NO SCREENSHOTS/IMAGES/VISUAL REVIEW/VIEWPORT MATRIX. User expressly withdrew them. Use HTTP+DOM state+persisteddata; user judges appearance.

## 1 — Reuse all existing commissioned data, retain the usable R4 handoff

TaskMarketplaceHost5190 / Development / TenantNULL / tb239-marketplace / DBtooba_marketplace_tb239 is ready.
R4 usable old-order URL is http://127.0.0.1:3102/fa/payment?checkoutId=01a18209-cd78-7000-a9b6-02dadf377e55 in profile D:\temp\tb240r4-review-chrome/CDP9342. Treat it as handed off: do not enumerate/read/automate/reload/close that profile or trigger watched-source HMR there.
Use a NEW task-owned profile/target on verified freeCDPport and same running FE3102 for read-only/no-source-change exercise; if editing frontend is necessary, use separate isolated checkout+freeFE3103 pointed at SAMEHost5190 so existing3102watcher/window is not modified. Reuse dependencies/scripts, no new automation framework/install/backendrebuild. Record actual paths/ports.
All real offers/stock/types/bindings/captures/seller roles already exist. SellerA's DEV freight is NOW68000+3000=71000 after R4 intendedpublication change, not old65000. Read actual current quote/echo/revision; do not restore old rates to fit expected data.
Original order01a18209-... and R4 coupon order01a18230-b7c1-7000-9983-6d588a5b0c08 stay untouched. No seed/reset/migration/catalog/permission investigation.

## 2 — A normal fresh session is explicitly authorized; missing prior password is NOT a blocker

Prefer already known taskbuyer credential safely retained on task disk and create normal NEWactivecart through existing APIs. Otherwise register ONE new task-owned synthetic email account with random private password through normal PUBLIC register, retain password temporarily outsidegit, login via existing BFF CSRF/password route inside NEWbrowser profile, verify /api/auth/me.
Do NOT recover/reset the previous coupon buyer, invent session/cookie, impersonateuser, use devactor or read USERbrowser. New normal account/cart/data setup is already authorized and is the exact cure for the reported lost-password/convertedcart impasse.
Use normal cart endpoints with actual current version and real3 offers/stock. Keep this in the authenticated browser/BFF context (same taskorigin) so browser currentcart/session is real; do not merely create an unrelated API cart and then claim its UI.
No fabricated values or localStorage keys as authority. Response-driven browser cache may be written by the product's own normal code; no driver value/proof injection to get past guards.

## 3 — Execute the actual remaining product steps, functional receipts only

A. New3-line cart -> existing shipping UI -> current verified geography -> three priced groups with OWN method/date -> real Continue/save. Network counter starts before clicking the actual control twice in the same tick: exactlyONE selectionPUT; root revision advanced once, no Order yet, no partialgroup or mixedlegacyshorthand. Let the page navigate normally to /fa/payment?cartId=... .
B. BEFORE finalclick record DOM: actual freshcart paymentform,3 own group summaries,current totals/selectionRevision,manual method actually enabled, payment-submit exists, no payment-error, zero checkout rows for that cart. This is the primary missing acceptance; no screenshot needed.
C. Exercise real final button twice in same tick (native input/control handlers, not direct payfunction/private-ref invocation). Networkreceipt<=1 StartPayment POST, normal real manualPendingOrder, same checkout/payment/attempt on explicit samekey retry, no extra localpayment/reservationcycle. Preserve noPSP/provider-exactlyonce limitation. A fulfilled/convertedcart page cannot stand in for stepB.
D. Before consuming the last freshcart if possible, trigger ONE genuine destination request failure on this tasktarget then explicitretry: old money/date not selectable after failure, typed recipient/address unchanged, retry causesONEcurrentidentity projection, currentprice returns. A prior accepted230-R1 fact is history, not a new run. No secondfetchframework or boguspostfailureprice.
If a real product defect blocks a step, fix only the owning existing UI/coordinator seam, record failingthenworkingHTTP/DOMstate. Do not turn automation timeouts into a new backend survey.

## 4 — Narrow source review finding: payment-status failure must not reopen a fresh action

Root saw R4's existing checkoutbranch:
loadStorefrontPendingPayments().catch(() => null)
then startedPaymentId=null when readfailed. That is UNKNOWN status, not proof no payment started; it can reopen the payform for an already-started order if server capability still permitsinitiation.
Fix narrowly if this remains true in current source: distinguish verified no-existing-payment from failed statusread; on failure keep the existing-order action safely unavailable with explicitretry/error, never infer not-started. Preserve server canInitiatePayment/paid checks and manual continuation. Do not mark all orderspaid or invent paymentId/status.
Use existing provider/payment capability data; continuation must not stamp awaitingManual on nonmanual payment merely because a pending item has a paymentId. Do not redesign Payment or certify other providers. Add focused same-component/helper fact statusfailure->no freshaction and verifiedmanualPending->continuation, plus real reload/freshform as above.
Use isolatedsource+FE3103 if changing source, preserve3102handoff. No Host/DB/schema change expected. Only affected typecheck/facts, no fullsuites.

## 5 — Exact elapsed correction, evidence, finish

R4 claim10:38:34Z -> resultabout10:52Z is about14 minutes, not43. Its cumulative286 is therefore NOT established by those timestamps. Verify actual claim/start/end from existing recorded receipts/logs WITHOUT a new Bridge diagnostic/post or inventing a timestamp. If43includes earlier work, record actual earlierstart andbreakdown; otherwise correct the arithmetic by append-only note. If endpoints are actualexecution, previous243+14≈257 BEFORE this slice; if43is measured independently, cumulative286 needs that explicit basis. Until reconciled label exactcumulativeUNVERIFIED; original240deadline is missed in either case. Do not erase the overrun/resetclock.
Do not spend the task investigating timekeeping beyond recording actual local timestamp pair now and short reconciliation.

RESULT leads usable authenticated freshcart/form/resultURL + actualstage and three remaining functionalchecks. Prefer leave one verified NEWprecommitcart/form for user testing and identify completedOrder/result separately; do not submit the user's reviewcart after handoff. If needing a second normalcart for handoff, same accountnormalAPI is authorized, no new setupseed.
No image/screenspath deliverables. Source/HTTP/DOM/persistedfacts only; no VisualACCEPT, PSP/paid/activation. No relabel partial as final if remainingchecks fail.
Update ALL3TOP recovery + deadline ledger, preserve histories and accepted backendmilestones. TASK verbatim docs/ai/tasks/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R5/TASK.md + evidenceTASK; RESULT/RECEIPTS in ownfolder. Exactpathnormalcommit/pushHEAD:main/fetchparity, no amend/force/reset/stash/rebase/CRLFchurn.
Never touch USER3000/5088/handoff3100/5188+D:\temp\tb238-handoff-chrome/userunsavedtabs/watchers, originaldraft/rates/pointer/assignments,review230capture/handoffcart, or the newly handed-offR4profile. Newtaskprofile/isolatedFE3103 only for modifications.
Checked-ingatesOFF/localtaskenvON, no newmigration/reset/reseed/directSQL/crossDBcopy, TB185paused/defaultOFF/PendingAllocation. Retain unresolvedexternalprovider-concurrency,238unclassified22001,227mobileempty-note and225-R1unrecoveredLOCALedits; no broadenedreleaseclaim.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

## RESULT CONTRACT — canonical, no diagnostic probe body

ActualFULLSHA orNONE; no editorialnote in SHAfields. Appendtask-endmarker as final transmittedline.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R5
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R4
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — fresh-cart functional closure and handoff
Mode: COMPLETE_FRESH_CART_FORM_DOUBLE_CLICK_AND_FAILURE_RETRY_WITHOUT_IMAGES
Status: PASS or PARTIAL or BLOCKED or RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: actualfullissuecommit
Base-SHA: actualfullsourcestart
Code-SHA: actualfullcodecommit or NONE
Evidence-SHA: actualfullevidencecommit or NONE
automaticNextImplementationTask: NONE

UsableauthenticatedURL+stage; freshform/Continue+finalclickcounts/failure-retryfunctionalreceipts;
deadlineMISSED/exactelapsedreconciledorunverified; blockers/parity/preservation. NOscreenshots.
END_TOOBA_WORKER_RESULT
```

END_TOOBA_TASK
