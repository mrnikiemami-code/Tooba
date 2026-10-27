PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232
Parent-Task: TB-SHIPPING-COMPACT-EVIDENCE-NULLSAFE-CHECKS-231-R2
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: REAL_SERVER_REVALIDATED_CART_SHIPPING_SELECTION_ONLY
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD40min / source25 / focused provider proof+recovery15. One correction.
ONE real writer slice, not full checkout/parity/history UI. Report PARTIAL at limit instead of adding hours.

FIRST / DO NOT DISTURB USER
Read TOP,231-R2 ARCHITECT-REVIEW, compact order-evidence decision, section29.
231/R1/R2 VALUE+TRANSFER+STORAGE ACCEPTED within scoped proof, migration WRITTEN/sharedNOTAPPLIED.
Dedicated visible REVIEW browser now holds user work: no enumeration/automation/reload/close. NO frontend
edits (HMR could change their page), Host/FE restart, live DB migration, cart/assignment/draft/pointer writes.
Proof exclusively in existing isolated231 test DB15433/order_tb231/tb231 and controlled Contracts harness.
DefaultOFF/TB185paused/PendingAllocation/localreadpreviewONcount7 and existing not_ready final-write guard stay.

DELIVERABLE
Make EXISTING SaveSelectionAsync/HTTP selection path capable of saving server-revalidated captured group
choices+231 evidence under a NEW explicit SelectionEnabled option in existing CapturedPreviewOptions,
defaultfalse and requiring Enabled+valid configuration. This option is for selection DRAFT only, NOT checkout.
Initial write support explicitly SingleStore active Store-owned groups; Marketplace/Seller writes remain
explicitly unavailable pending their permission/display matrix (read support unchanged). Do not guess owner.
No new endpoint family, price engine, standalone planner with no consumer, UI or payment behaviour change.

A SHARED CURRENT RESOLUTION + UNTRUSTED INTENT
Refactor existing planner minimally so read mapping and selection validation consume the SAME typed resolved
cart partition/candidate/216quote/receiving data. No parse-back of display-money/labels into authority.
Current actual cart/access/version -> one228assignment batch -> active real type/bindings ->219selected pointer
per DISTINCT fullowner+binding ->ONE218 ->ONE217wholecartgoods ->216once/candidate. Unknown measurements null.
Pass the actual requested CouponCode intent through the same217Pricing/Promotion/Tax path and use existing
StorefrontCheckoutService.StorefrontTaxJurisdiction. No cartsubtotal/exclusive or hardcoded discount substitute.
No foreign lookup or price recomputation after the first durable draft write.

Minimal additive selection INTENT body only: complete server GroupKeys, stable catalog binding choice,
selected ISOdate and expected publication/snapshot/matched-row identity. NO owner/seller/tenant, money,
eligibility or accepted-evidence object from browser. Browser keys locate current server plan, not authorize it.
Expose required per-group/per-method confirmation identity in additive READ metadata (not global provenance
collapsed across pointers): exact SnapshotId/SourceRevision/SelectionVersion/areaGroup+row and currency as
needed. New long tokens must travel to JS as exact decimal strings, never rounded numbers.
Client echo is UNTRUSTED comparison only. Resolve everything again server-side; mismatch in cart version,
complete membership/type/owner, selected pointer token/snapshot, matched area/row or available receiving date
=> stable conflict/refusal, no silent price adoption, no partial group acceptance, no auto retry.
Captured Manual immutable row+snapshot and expectedmatchedrow guard price/config changes without accepting
client amounts. Unknown/unassigned/unquoted group blocks the whole selection; preserve it in the read view.
Reject ambiguous mixture of captured intent and legacy method/group fields. OptionOFF captured intent refuses,
does not fall through to legacy.

B ADDRESS MUST BE THE DELIVERY ADDRESS ACTUALLY PRICED
Reuse existing PrepareShippingAsync owned saved-address/recipient handling. The address read is server-bound.
Current AddressBook Contracts carry Country/Province/City text, not canonical ids. Reuse existing
IGeographyLookup.ResolveLegacyAsync/GetByIdsAsync to resolve uniquely in the existing supported Storefront
country context; unresolved/ambiguous or supplied tuple contradicting the prepared address => refusal.
Do not use an arbitrary browser tuple for price while saving different ProvinceName/CityName/address.
No fuzzy first match, no new Geography data/schema, no invented parent/country/default beyond existing
supported regional policy. Persist the verified destination identity needed for later commit revalidation in
the EXISTING cart selection root (small scalars, no geography JSON), or prove exact re-resolution policy.
Every selected date must be from that group's current real213 calendar; derive its exact ticks+timezone from
server result (no client interval as authority). No extra prep/legacy9-12/cutoff/reservation policy.

C COMPOSE231EVIDENCE IN ACTUAL GROUPS
Use exact fullowner/snapshot/draft/revision/format/selectionversion/type/matchedarea+row and freight/
packaging/checkout/collect from current quote, chosen date and100nsticks/timezone from current slot.
Create existing231value and attach to actual CartShippingDraftGroup, preserve group/line stable identity.
Existing ShippingAmount for captured group represents PAYABLE AT CHECKOUT; freight collected by carrier is
stored separately in evidence and never also charged at checkout. Keep legacy semantics unchanged, do not
alter Payment/Settlement or assert new funders/Free/CarrierApi support.
No DateOnlyrange invention, duplicate fullJSON/week/rate rules or perline capture. One shared Shipping snapshot.

D ATOMIC REPLACEMENT + CONCURRENCY, REAL STORE CALLER
Current legacy replace is ClearGroups+Save,AddGroups+Save (TWOcommits). Captured path MUST use ONE atomic
Order-owned adapter operation so partial deletion never becomes durable. Reuse foundation UoW/transaction
pattern if sufficient; if an explicit transaction is needed, exactly ONE named captured-selection member in
existing StorefrontShippingDraftStore/adapter, no generic transaction framework or file-wide guard exemption.
All foreign reads/validations BEFORE this boundary. No cross-module DB transaction/FK.
Guard replacement against concurrent selection changes as well as cart version: use a positive monotonic
long selection revision on existing CartShippingDraft, real EF CAS token, expected0 for absence/legacy token0
only. Add minimal additive Order migration if needed, no prior migration edit. No ABA/reset and no auto retry;
stale expectedselection token =>409 unchanged. Creation race maps only exact root uniqueness violation.
Use fresh persisted root/graph, not stale identity-map facts. On failure rollback OWN attempted graph and
detach/restore ONLY own changes, so later SaveChanges cannot leak a rejected selection and unrelated caller
work is never flushed/cleared. Keep first durable success result provider-authoritative.
No deletion/insertion outside transaction, no preflight acknowledged as durable success.
Captured evidence cannot be silently downgraded into legacy selection if flags are later switchedOFF:
existing captured draft + incompatible legacy path must explicitly refuse, not reprice/drop evidence.

E FINAL ORDER AND PAYMENT REMAIN CLOSED
CommitAsync stays not_ready for captured drafts/preview regardless of SelectionEnabled. Normal real final
entry is StorefrontStartPaymentComposer ->CommitAsync, not only the unused commitShippingToPayment FE helper.
ProjectPaymentDraftAsync must not expose legacy-repriced captured shipping as though final payment ready.
No captured checkout/reservation/cart conversion/payment or historical endpoint in232. Legacy noncaptured
defaultOFF path remains compatible. Do not advertise CanCheckout/Active/customer eligibility from a draft save.

FOCUSED EXECUTED PROOF — ACTUAL SAVE + REAL ORDER DB
Use existing real217/218 harness and controlled Offer/Shipping Contracts, actual new writer and normal Order
migration chain on ONE isolated231 PostgreSQL target (identity beforeDDL, existing localimage, cleanup).
- successful two-group selection persists EXACT231facts and correct pergroup checkout/COD split/100nsticks;
- complete partition, no client money/owner adoption; stale cart, pointer/version/row/type, closed/wrongdate,
  mismatched address, unassignedgroup =>no write, existing selection preserved;
- two same expectedrootrevision replacements =>one winner/one conflict, one complete graph, no mix;
- forced failure between delete/insert stages rolls back to full old selection; laterSaveChanges0/no leak,
  caller token/exception identity and unrelated tracked state preserved;
- accepted evidence flows to existing groupplan/materializer but no real order created by live/API path;
- SelectionEnabled defaultOFF guard, captured intent legacy-fallback refusal and Commit/StartPayment still
  denied before order/reservation/payment even with SelectionEnabledON; storedcaptured draft remains blocked
  if flagsOFF. Existing legacy focused controls unchanged.
Run exact new/affected tests, changed Order builds only. No full50/119/188/194/Shipping/Host/solution sweep.
Migration231 plus any232revision/destination additions remain sharedNOTAPPLIED. No runtime refresh/UI tests.

FINISH
docs/architecture/evidence/TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232/{TASK.md,RESULT.md}.
Update THREE TOPs with actual writer/storage result, exact new migration/columns/tokens, tests and limits,
SelectionEnabled defaultOFF and local runtime unchanged, remaining finalcommit/payment/historical read.
Record known empty-keyrefresh/inflightwait/crossownerlabel/decoder/OfferAPI gaps, no claim repaired here.
Exact-path scoped commit/push/fetch parity, noadd-A/historyrewrite/Bridge/post-resultscript.
STOP_FOR_ARCHITECT. Automatic-Next-TaskNONE. No233envelope or userVisualACCEPT.
