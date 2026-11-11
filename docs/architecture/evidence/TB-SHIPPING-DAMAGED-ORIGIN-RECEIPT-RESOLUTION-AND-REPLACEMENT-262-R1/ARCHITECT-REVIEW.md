# Architect review — TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262-R1

Date: 2026-11-11
Decision: ACCEPT_WITHIN_ISSUED_THREE_GAP_SCOPE — WAIT_FOR_USER_FUNCTIONAL_TRIAL
Reviewed repository revision: 90edf63404f5f114bd52ec350b1e32de8c120fc1
Worker code: e6cb0378edaf0986c8f7c1f656494ed6e8abc569
Worker evidence: 84cb0dcc9794d0bdca127e61bc8f0029a76d913d

## Review basis and findings
Independent repository source/evidence inspection; the architect did not execute the Windows runtime or independently rerun the reported tests.
1. Order RunDamagedResolutionAsync checks current actor, checkout/fulfillment/parcel ownership and exact recorded intent. Only Fulfilled or Referral answers the recorded result directly; incomplete Recorded replacements continue through the owning execution. NotRecorded retains the ordinary projection gate. Referral completion here means recording a referral, never financial or customer-obligation completion.
2. DamagedGoodsResolution.RetainStockBeforeDispatch compares cancelledShipmentId to its current ReplacementShipmentId before detaching or advancing Attempt. A repeated or obsolete cancellation cannot erase the newer parcel link or release its hold.
3. Inventory ReserveForDamagedReplacementAsync passes expiresAt:null to the owning ReserveAsync. The order-bound replacement hold starts non-expiring at its first durable commit, removing the former reserve/link crash window.
4. The operational section passes the server-recorded quantity/outcome/occurrence into damagedResume, allowing the existing review to resume without relying solely on local storage. Reported focused public-command/storage and mounted-component evidence covers pending stock, continuation, retained hold and stale cancellation. No new broad test cycle required.

## Canonical database reclaimer disclosure
RECEIPTS.md discloses exploratory calls to the shared global expiry reclaimer with a future instant. The raw tb262-relational.txt records both exploratory G entries as reclaimerReleased=0; later committed G entries use reclaimerCandidate=0 and a read-only predicate. Thus the supplied logs report zero releases, not an unrecorded positive release. This is worker-recorded evidence, not an independent database audit.
Do not run a global mutating expiry/reclamation job against the shared canonical database as a fixture test again. Keep future proof narrowly scoped/read-only and preserve all active-order holds. No repair loop is issued for the disclosed zero-release runs.

## Binding user rule
Until an authorized order cancellation, an unconsumed order-bound replacement reservation stays attached to that order. Parcel cancellation, return, retry, timeout and UI closure never release it. Dispatch consumes the appropriate hold once; consumption is distinct from release. Original consumed sale reservation is not recreated or consumed twice. Damaged goods do not become healthy stock.

## Acceptance and stop
The three identified blockers of 262 are closed by this bounded review of 262-R1. Existing accepted 261/262 behavior is preserved within its documented scope. User visual/functional acceptance is still pending; all-shipping completion is NOT claimed.
STOP_FOR_USER_TRIAL. automaticNextImplementationTask: NONE. Do not issue 262-R2 or another implementation task absent a concrete reproduced defect or an explicit user request.

Trial: /fa/admin/fulfillments -> target operations -> damaged origin receipt -> replacement decision. Check pending/no-stock continuation, linked replacement creation, parcel cancellation retaining the same hold, and continuation after reload.
Notifications: use the existing transient react-toastify success notification; field errors, blockers and retained unresolved attempts remain contextual. Persian carrier-return labels remain distinct from customer RMA.

## Limits retained
No refund/financial automation. Referral remains pending customer-service/financial work. Parent common-package return/damage orchestration is unsupported. Authorized whole-order cancellation after dispatch remains unavailable, so the real damaged-return hold remains attached to the active order. No live provider/Tipax proof or real Marketplace checkout (255 remains parked). One database tooba_alpha only; no new database, reset or fabricated user data.
