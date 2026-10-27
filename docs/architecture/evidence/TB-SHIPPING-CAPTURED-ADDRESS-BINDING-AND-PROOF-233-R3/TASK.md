PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-ADDRESS-BINDING-AND-PROOF-233-R3
Parent-Task: TB-SHIPPING-CAPTURED-SUBMIT-INTEGRATION-233-R2
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: CLOSE_ADDRESS_COUNTRY_BINDING_AND_CORRECT_REAL_SUBMIT_PROOF
HARD40 / source25 / final15. One correction. STOP at40 with exact remaining work, no100min extension.
Read233-R2/ARCHITECT-REVIEW.md BEFORE editing. No Bridge/enqueue/wake/post-result script, no234.

RETAIN / DO NOT REOPEN
233-R2 mechanical actual submit integration/core extraction is accepted WITHIN REVIEWED SCOPE. 231/232 accepted. Same facts builder, same reader-free216/213core, same ProcessManager transaction and materializer; no new calculator, helper framework, schema or coordinator.
Public CommitAsync/ProjectPaymentDraftAsync remain CLOSED, checked-in gatesOFF, no coupon work (nonblank still unavailable), no frontend/StartPayment/payment-provider activation.

1 COUNTRY + OWNED SAVED ADDRESS (ONE ACTUAL FIX)
Root read GeographyPlaceLookup.LegacyResolution: null/blank countryCode ALWAYS returns Unresolved. Current final path always passes null, so source cannot reproduce the permissive-fake positive with real Geography.
Resolve canonical nonblank country code from current trusted country data matching the priced stored country id; verify supplied/saved country identity agrees. Existing Geography Contracts only, unique exact resolution; no hardcoded replacement/default/first country/cross-country search, no label guessing. Service area stays explicitly unsupported.
Use IAddressBookCheckoutLookup.GetAsync(realActorUserId,savedId,ct) when saved-id provenance exists. Require real cart actor/access coherence; never userId from browser owner fields. Missing/foreign/deleted saved record or changed country/province/city/postal-address/phone data must refuse BEFORE reservation; actual persisted Order address cannot differ from the address that was priced/revalidated. Preserve existing lawful recipient-name override policy; do not invent new address semantics.
IMPORTANT: PrepareShippingAsync currently sets prepared.Shipping.SavedAddressId=null. Add a narrowly typed SERVER-only source-id provenance on StorefrontCheckoutPlacement (trailing optional field) and have232 captured writer persist that real id in the EXISTING draft SavedAddressId column. Preserve ordinary legacy prepared-snapshot semantics and no second AddressBook read there. No body authority/new column/backfill. Old null source remains unknown/unsaved, never guessed.
For unsaved addresses compare final accepted command address to stored priced address and resolve the exact stored country/path. If necessary share one small pure canonical comparison helper with the existing preparation code, not a DI cycle or a second CheckoutService. Forward caller token unchanged.

2 FINISH EXISTING BINDING / RESPONSE CHECKS (NO NEW CAPABILITY)
Use existing IShippingCatalogReader once per final submit to require each SAVED service/option active (NULL option exact, no fuzzy fallback). Missing optional authority => explicit unavailable, true legacy does not call it.
Use existing typed StorefrontCapturedPublicationBinding for pointer cache/full owner identity; no new delimiter-based tenant identity.
Check current pointer's service/option equals requested binding and quote SnapshotId/FormatVersion/DraftId/SourceRevision equals stored evidence, alongside existing money/row/type/date/hour checks. Contradictory provider response must not be accepted.
Gate uses HasValidAvailableDateCount as well as all3coherent flags. Do not refactor already-accepted pricing/calendar/owner rules.

3 CORRECT THE EXISTING REAL SUBMIT FIXTURE (NOT A NEW FRAMEWORK)
Root read the actual test: A=post/standard and B=tipax/express, PointerCalls2; both dates same, whole-minute09..17; prices99/198 with tax0; replay never changes pointer. Append honest corrections to233-R2RESULT + all recoveryTOPs, not just new green claims.
Replace/add the REQUIRED coherent positive:
- TWO different sellers, SAME Store service/option, ONE shared immutable snapshot/draft/revision and SAME area group for the SAME destination; distinct rate rows selected by group-goods amount. Exactly ONE219read, TWO216quotes.
- Real existing quote path100-10+9=99 and200-20+18=198 (controlled Pricing/Promotion/Tax Contracts, actual Order computation); not prices99/198 with tax0.
- ONE shared stored week; choose two lawful different dates with different weekday hours incl fractional100ns ticks. Do NOT invent two different weeks for one snapshot/binding.
- Persist/read BOTH actual Order groups/lines/evidence, assert each group's own row/money/date/ticks/owner and mapping. Same area group is CORRECT, not a failure of independence.
- Strict Geography fixture rejects null/wrong country and wrong names (do not fabricate resolved for arbitrary args); record actual nonblank code. Label controlled Contracts vs real OrderDB honestly.
- Replay: actually mutate pointer to another revision BEFORE second authorized submit, assert no new pointer/quote/reserve/order and same persisted historical evidence. Test name/docs must match executed steps.
Retain lawful duplicateOfferId-line control, exact original exception rollback proof. For negative cases assert EXACT owning stable codes, not merely nonblank.

4 SMALL BOUNDED CONCURRENCY PROOF ON SAME ORDER TARGET
Independent contextB advances selection before contextA acquires root lock => canonical exact conflict with zero reserve/order.
Second case A holds accepted Order root lock (pause using existing test Contracts/barrier), independent B uses actual232selection adapter at SAME expected revision; prove B is blocked on that root while A persists, release once, then show Order retains accepted old evidence and later selection cannot mutate it. Bound barriers/SQL commands10-30s, one run each, no confidence loop or unbounded waits. Ensure independent ambient transactions/ExecutionContext; don't accidentally enlist both into A's TransactionScope. Use actual PostgreSQL blocking evidence or deterministic interceptor coordination, not sleep-and-assume. No production hooks/new cross-module transaction. Shipping pointer freshness remains point-in-time Contracts observation.

FOCUSED EXECUTION ONLY
Existing disposable231target15433/order_tb231/tb231, identity beforeDDL, one existingpostgres17-alpine container, normal migrations, remove/portclosed.
Run new address/binding/concurrency facts + corrected exact CapturedFinalSubmitIntegrationTests and any directly changed placement/selection-writer cases ONCE. AffectedOrder builds only. No124/36/28/31/50/119/188/194/285/310/312/fullOrder/Host/solution/guards replay.
Mandatory facts: actual saved-address id survives preparation->captured draft; owned unchanged saved address positive; foreign/missing/edited record and mismatching command/country refuse pre-reserve; active/NULLoption binding control and inactive/wrong snapshot-format refuse; coherent same-binding99/198 positive and genuinely changed-pointer replay; the two bounded concurrency cases. No fake optional skipped provider tests. If not executed, prominently leave acceptance pending.

PRESERVE / FINISH
No frontendHMR/browser/HostFE runtime restart/rebuild, live cart/address/assignment/draft/pointer/payment write, shared migration apply/schema/baseline edit. 231/232 sharedNOTAPPLIED/EFsnapshotgap retained. USER VISIBLE review window is USER unsavedwork: never enumerate/read/automate/reload/close; localpreviewONcount7 unchanged.
Public checkout guards stay CLOSED; no activation/readiness/Marketplace/fullcheckout/VisualACCEPT claim. Known230emptykey/inflight/cross-ownerlabelsdecoder/OfferAPI/227residual/225-R1lostLOCALwork retained. TB185paused/defaultOFF/PendingAllocation.
Task verbatim+RESULT in this folder, append-only correction233-R2RESULT, ALL3TOPs. Work from ISSUE commit FIRST: if drifted, safe fast-forward before source (do not repeat implementing on olderbase then merge issue conflict later). Exact-path commit/push/fetch parity, no amend/force/reset/stash/rebase, CRLFdrift untouched. Realcounts/timing/corrections; STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
