PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R4
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — usable owner-authorized payment and remaining acceptance
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: RECOVER_AUTHENTICATED_ORDER_ACCESS_AND_FINISH_COUPON_HISTORY_FLOW
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0 — Deadline MISSED; user OVERRIDE: NO screenshots or visual assessment

The original240-minute completion commitment has already been MISSED:53+70+62+31+27 ≈243 focused minutes,≈3 over before other actual focus. Do not reset the clock or describe the original promise as met. Continue necessary work under existing authorization; operational focusbox30 minutes with checkpoint10 usable real payment page and checkpoint25 coupon/history receipts, NOT a new delivery commitment or automatic extension. If the focusbox is exceeded, report exact blocker/remaining work promptly. No helper-only PASS or automatic241.

USER OVERRIDE is explicit and supersedes ALL screenshot requirements in240/R1/R2/R3, recovery and any visual skill:
"اسکرین شات نخواه مدل قوی نیست". DO NOT request, create, inspect, upload or wait for screenshots. DO NOT do visual acceptance, viewport capture/matrix, image review, or critical-visual-review tasks. Use real HTTP/DOM state/functionality, persisted data and narrow tests. UI appearance is for the user's own review at the usable URL. Mobile functionality remains relevant; no visual or screenshot certification is requested. No inferred userVisualACCEPT.

## 1 — Existing complete backend stage, reuse everything

Read current source df5a21d05a0ffd9499ca55dd3c17c97b3731ea98,240-R3 RESULT/RECEIPTS/ARCHITECT-REVIEW. Safe fast-forward isolated task checkout to issue commit.
Task Host5190 D:\temp\tb240r3-build\out Development/Marketplace/TenantNULL/tb239-marketplace DBtooba_marketplace_tb239; FE3102 and task-owned profile D:\temp\tb240r3-review-chrome/CDP9341 exist, verify actual PIDs before use/restart. All sellers/permissions/stock/reference data/3 captures are ready, do not bootstrap/reseed/recreate or run migrations again.

Real mixed checkout01a18209-cd78-7000-a9b6-02dadf377e55, manualPending payment01a18209-d01d-7000-a1f0-d59a53b1124f, shipping214500/payable5762600,2sellerorders,3lines,3groups. Newmigration20261028140000 applied on TASKMarketplaceDB; do not rerun or edit it. Preserve this original Order as the history reference.
Prior live3-group selection200/paymentdraft200/StartPayment200 and persisted original evidence are real milestones. They are NOT a usable UI completion; existing /fa/payment?checkoutId=... hits a client-local guard.

## 2 — Exact source-confirmed blocker and narrow repair, no ownership bypass

Root inspected:
- app/storefront/storefront-checkout-api.ts loadStorefrontCheckout calls resolveCommittedCheckoutAccess then throws403 LOCALLY if no browser-stored cart proof, before any server request.
- storefront-cart-api.ts resolver knows only local committed/paymentproof; API-created order or another authenticated browser has none.
- Order StorefrontOrderEndpoints.GetCheckoutAsync currently binds required Guid cartId query, although StorefrontCheckoutService.GetAsync explicitly ignores that locator after checking GetOwnedForPaymentResultAsync.
- The service already reads stored checkout using actual actor, allows authenticated OWNER under existing OrderAccess, and preserves committed-cart+guest-secret proof for guests.
- storefront-payment-handoff.tsx currently Promise.all(loadCheckout, loadMethods, loadWalletQuote); wallet/payment helpers also require the local resolver. Fixing only checkout fetch while parallel wallet still fails is incomplete.

Implement SAME existing path:
A. Let authenticated users recover THEIR stored checkout through existing session/BFF and authorized server Order read, without browser-local proof as a prerequisite. Use checkoutId only as an UNTRUSTED locator, not permission. Make existing GET's cart locator optional for this authenticated read if needed (minimal endpoint/query/service change); retrieve actual CartId from authorized stored result, never activecart, fakezero/guessedcart or another user's browser storage. Preserve real foreign-user denial/missing response and guestsecret proof unchanged; do not weaken OrderAccess, identity gates, server scopes or create a bypass endpoint.
B. After a successful matching server response, use its actual CartId + authenticated session for related payment/wallet reads. Reuse existing access/proof utilities for caching only; local cache is a hint, NEVER authority. Do not manufacture guestsecret, rotate/create an active cart, detach a different currentcart or write a fake proof to pass the guard.
C. Sequence dependent work: owner-authorized checkout read FIRST, then same checkout's wallet/method/payment reads through actual returned context. Avoid Promise.all against an as-yet-unrecovered local record. Server failures/foreign/unverified data remain refusal, no fallback to stored totals.
D. Do not show an illegal fresh pay action for an already manualPending/paid/unpayable order. Use existing authorized payment/status/capability responses and existing page surfaces. For a fresh actual payment form use a normal NEW cart/payment-draft path, then existing final-click; do not recommit the old convertedcart.
No parallel authclient/page/framework, no Host business logic, no foreignDbContext, no query-as-proof, no logout/account manipulation.

Focused functional proof (no images): same real buyer normal BFF session + EMPTY committed/payment localcache loads own API-created checkout; refresh still works; API foreign buyer and guest/no-secret cannot read it; actual related wallet/payment reads use returned CartId without activecartfallback; existing guestproof path still works. Narrow tests of same helpers/endpoint plus one real browser DOM/network receipt, not only an in-process DTO.
Ready handoff means actual existing controls/summary/manualPending state visible with successful owner-authorized requests and no localguard error. Provide real authenticated URL/window after functional exercise; once handed off, stop automating it.

## 3 — Remaining REQUIRED acceptance, reuse current scripts and APIs

Do not repeat environment/migration/permission engineering. Use current real offers/captures, normal APIs and isolated task target.
- Create one task-only real Promotion coupon applicable to ONE eligible seller/group, other groups legitimately undiscounted. Real read -> exact all3group choice/echo save -> paymentdraft -> finalclick -> Order/manualPending under SAME coupon context, own financial/evidence facts. For groups of SAME SellerA, seller-wide eligibility may affect both Platform and Seller obligations: choose a real product/offer eligibility or SellerB-only coupon so "one group only" is actually proven, not assumed.
- Invalid coupon selection/final refuses checkout.coupon.not_applicable; explicit clear/change removes old acceptance, causes new current-context read and requires new confirmation, no resurrectedstoredcode. No newdiscountengine/fakePromotionports.
- After taking original authorized OrderGET baseline, change ONLY one task-owned DEV publication through normalAPI. Original checkout01a18209-... authorizedGET must preserve original groupIDs/owner/capture/row/money/dates/ticks using Order storage only; foreignbuyer denied. If existing Order response omits compact reference facts, extend its existing authorized DTO/mapping narrowly from Order storage, no new history subsystem/Shipping lookup/fullJSONcopy.
- Real UI Continue/save and finalclick can be invoked normally; DOM/network counters prove doubleclick emits≤1 request. Existing manual sequential/concurrent samekey proof stays bounded; samePayment+Attempt IDs and no duplicate localpayment rows, no PSP exactly-once claim. Real failed destination request+explicitretry hides staleprice and keeps typedaddress; functionalstate only.
- Run existing precommit payment form in a normal NEW cart so user has usable current shipping/payment route; handoff existing successful Order/manualPending route too if functional. No screenshots, no viewport work or appearance claims.

If any item cannot be finished, name the precise remaining real failure with request/status and source owner. No "no screenshot" blocker is allowed, and no unit-test coverage may stand in for a livecoupon/order or usablepage.

## 4 — Existing accepted source boundaries and honest runtime ledger

R3 migration/cart+final index predicate and strict planner/materializer correction are ACCEPTED WITHIN THAT SOURCE SCOPE after root review + worker receipts. Do not reopen them. Preserve legacy no-key grouping and captured exactline partition.
Payment gateway.InitiateAsync STILL precedes contested SaveChanges: both race branches may contact external provider before one loses. ManualPending persisted one-row proof does NOT certify externalprovider exactly-once, paid/PSP or production activation. Keep that recorded, no realprovidercalls or broadpaymentredesign in this UI/coupon/history slice. checkoutReplayedfalse remains known unless corrected narrowly in existing mapping with stored evidence, never fabricated.
Correct stale migration statements:231/232/236 applied to named tasktarget;237-R1 also recorded actual store-alpha application. Do not claim store-alpha NOTAPPLIED without a new read. No permission to inspect userDB just to settle wording. Taskmigration actually ran through MarketplaceDevelopmentBootstrap according to worker; do not call it official MigrationRunnerplan/apply without such receipts.

## 5 — Bounds, preservation and exact delivery report

User override NOscreenshots applies everywhere below. Real functionalbrowser use is allowed on TASK-owned profile only; user reviews appearance.
NEVER enumerate/read/automate/reload/close USER3000/5088, handoff3100/5188+D:\temp\tb238-handoff-chrome, unsavedtabs/watchedsource/HMR; preserve originaldraft/rates/week/pointer/assignments,review230capture,handoffcart. TaskFE3102/Host5190 only, verifiedPIDtaskrestart, no kill-by-name.
No reset/reseed/drop/migration/baselineregeneration/businessSQL/crossDBcopy. Checked-in shippinggatesOFF/localtaskenvON only. TB185paused/defaultOFF/PendingAllocation.
Known retained:238unclassified22001,227mobileempty-note,225-R1 unrecoveredLOCALeditloss. No secrets/password/token/cookie/guestsecret in git/results.
Use only focused new/affected facts + necessarytypecheck/build. No fullHost/Order/Payment/Shipping/solution/visual sweep.

Save TASK verbatim docs/ai/tasks/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R4/TASK.md + evidenceTASK. RESULT/RECEIPTS (NOscreens folder required) own evidencepath.
Update allTHREETOP recovery and deadline ledger, preserve histories: original240MISSED at~243+actual focus. Permanently mark screenshots/userVisualACCEPT not workerdeliverables per useroverride; functional usability/owner/coupon/history still binding.
Exactpathnormalcommit/push HEAD:main/fetchparity; no amend/force/reset/stash/rebase/CRLFchurn.
Result leads USABLE AUTHENTICATED URL + ACTUAL USER-VISIBLE stage; concise ledger own/foreign/guestaccess, partial/invalid/clearcoupon, preservedoriginalorder, duplicate/failurefunctionalchecks. Each actualrequest/status/id/artifact or unmetdefect. No screenshot requirement, no inferredVisualACCEPT, paid/PSP/activation or finalpartialclaim.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE; no automatic241.

## RESULT CONTRACT — canonical envelope, no placeholder/probe result

ActualFULLSHAs or NONE, no notes inside SHAfields. Append task-end marker as final transmittedline after template.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R4
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — usable owner-authorized payment and remaining acceptance
Mode: RECOVER_AUTHENTICATED_ORDER_ACCESS_AND_FINISH_COUPON_HISTORY_FLOW
Status: PASS or PARTIAL or BLOCKED or RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: actual full issue commit SHA
Base-SHA: actual full source start SHA
Code-SHA: actual full code SHA or NONE
Evidence-SHA: actual full evidence SHA or NONE
automaticNextImplementationTask: NONE

Usable authenticatedURL + functionalstage, requiredacceptanceledger, sanitizedreceipts,
original240-minute deadlineMISSED/cumulativeactualtime, remainingblockers/preservation/parity. NOscreenshots.
END_TOOBA_WORKER_RESULT
```

END_TOOBA_TASK
