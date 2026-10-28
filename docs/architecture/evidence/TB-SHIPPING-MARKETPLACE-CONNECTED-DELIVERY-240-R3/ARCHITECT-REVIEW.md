# Architect review — 240-R3

Reviewed sourcedf5a21d05a0ffd9499ca55dd3c17c97b3731ea98, actual forward migration/planner/materializer and payment-access path. Root did not run Windows tests, listener or browser, and did not inspect screenshots because user explicitly removed that requirement.

ACCEPT WITHIN SOURCE SCOPE: migration replaces both known cart/finalOrder indexes with legacy-only WHERE accepted_snapshot_id IS NULL, no extraownerfield/JSON/table. Down fails atomically on unsafe duplicates. Exactlinepartition and evidence-bearing materializer now support sameSeller/sameMethod distinctobligations while legacy rule remains. Worker reports real3-groupselection/paymentdraft/StartPayment/persistedOrder manualPending and ownGET200; no independent root runtime certification, full delivery still PARTIAL.

UI defect source confirmed: loadStorefrontCheckout throws local403 without resolveCommittedCheckoutAccess.cartId before HTTP; browser caches know only previously committed local proof. Yet existing StorefrontCheckoutService.GetOwnedForPaymentResultAsync authorizes actual actor against stored checkout and returns true CartId for authenticated owner; guest path still requires secret/ownership. Existing endpoint requires cartId locator even though service ignores it. Permit narrow owner-authorized recovery in SAME read path, then derive related reads from successful server response. Handoff Promise.all currently starts wallet read while the record is still unrecovered, so checkout-only fix is insufficient. No activecartfallback, fakeguestproof or weakened foreignaccess.

NO SCREENSHOTS: user's newer explicit instruction overrides all earlier capture/viewport/visualproof requirements. Do not ask Worker to create/inspect/upload images; functionality via actual HTTP/DOM/data, appearance left to user. Existing R3 images are not proof of payment form; do not spend work reviewing them.

Remaining binding acceptance: usable authenticated existingpayment surface (oldOrderresume + freshcartform), livepartial/invalid/clearcoupon and immutableoriginalOrder after taskpublicationchange + foreigndenial, duplicate/failurefunctionalchecks. ManualPending only, no PSP/providerexactlyonce: provider still invoked before contested SaveChanges. checkoutReplayedfalse is retained.

Migration ledger wording: R3 attributes execution to MarketplaceDevelopmentBootstrap, not officialrunner plan/apply. Earlier237-R1 recorded231/232/236 applied onstore-alpha; do not repeat NOTAPPLIED without evidence. No userDBinspection authorized merely for wording.

Original240-minute completion promise MISSED at reported243 (~3over), not fulfilled or reset. Root's estimate was optimistic. Continuing focused repair under standingauthorization is not a new on-timepromise; actual cumulativeoverrun must remain visible. No VisualACCEPT or fullreleaseclaim.
