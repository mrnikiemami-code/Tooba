# CURRENT — 239-R1 EXECUTED PARTIAL: REAL DEV ADMIN BOUND (login200 + protected seed200, seller403); owner flow blocked on missing seller data —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-ADMIN-BOOTSTRAP-AND-LIVE-FLOW-239-R1/{TASK.md,RESULT.md,RECEIPTS.md}.
Status EXECUTED/PARTIAL, NOT accepted, no userVisualACCEPT, no activation. Bridge row 210643f4-ada5-4997-ae78-101494d2aa4c.
Base fe963e325; same task Marketplace 5190 / Edition=Marketplace / TenantNULL / DB tooba_marketplace_tb239 (NOT reset).
THE DECIDED GAP IS CLOSED, with real receipts: a dedicated account registered through normal POST /v1/auth/register
(userId 01a1818b-5257-7000-bada-84233a2b21fb) was powerless BEFORE the opt-in (login200 then admin seed403
admin.authorization.denied traceId dc4bdc78…). Host restarted once with private opt-in env
(Tooba__Development__MarketplaceAdmin__{Enabled,Identifier}, nothing checked in) and the new seam bound THAT real
Identity principal to the EXISTING tenant marketplace-platform / member relation through IAuthorizationTupleWriter
(resolving via IIdentityAuthenticationService.FindUserIdByIdentifierAsync; no invented id, no credential reset,
no seller promotion, no fake header/JWT/SQL grant). After binding: the SAME account login200 and the official
POST /v1/admin/catalog/demo/reset-and-seed EXECUTED and finished 200 (890.6s, 566 products); seller-actor-a on the
SAME route stays 403 admin.authorization.denied traceId 2a7401a0…; a third freshly registered account stays 403.
Idempotency proven by a real restart (InMemory authorization store EMPTY afterwards; admin GET
/v1/admin/catalog/demo/status 200 with rootsDemo15/categories116/brands22/tags36/attributes41/products565 draft,
seller 403). Source seam: NEW Configuration/MarketplaceAdminDevOptions.cs (Enabled default false, Identifier only,
no password), extended Development/MarketplaceAdminDevBootstrap.ProvisionConfiguredAsync (refuses unless real
IsDevelopment + Edition Marketplace + marketplace connection + explicit opt-in + nonblank resolvable email),
MarketplaceDevelopmentBootstrap.ApplyAsync(…,IHostEnvironment), Program options binding + environment pass, NEW
Development/MarketplaceAdminDevBootstrapTests.cs. No Identity/AccessControl EF, no DbContext/SaveChanges/Migrate,
no new endpoint/permission catalogue, no Production/SingleStore behaviour change, no migration/schema edit.
Focused 10/10 (5 new provisioning facts + 5 affected MarketplaceDevelopment guard facts); Host.Tests 0 errors.
OWNER FLOW NOT DELIVERED — concrete stage, not permission: after the authorized seed the task DB holds
catalog.products=566 (all Draft), party.parties=0, offer.offers=0; GET /v1/storefront/home 200 but seller
GET /v1/seller/offers 403 seller.authorization.denied because no seller business scope exists. Root cause: the
Marketplace Development path never runs the Party/offer seeds used by SingleStore (SettingsFoundationDevelopmentSeedHost
/ PartyOrganizationProfileDevelopmentSeed / IWorkspaceDemoSeed / WorkspaceDemoMarketplaceSeed are called only from
DevelopmentSchemaMigrator) and NO HTTP route creates seller organizations (Party exposes only GET /v1/admin/sellers
and POST /v1/admin/sellers/query), so SellerDevContextBootstrap.EnsureAsync hits its documented early return.
Therefore no Platform/SellerA/SellerB publication, offer responsibility/type, stock, draft/rates/week/publication,
cart line, projection, selection echoes, paymentpreview, StartPayment, Order or coupon-on-one-group was reachable.
NOT done: no browser/UI smoke (no payment page could be reached), no 390/360 captures, no marketplace Order, no
multi-owner evidence. Preserved: original3000/5088, handed-off3100/5188 and D:\temp\tb238-handoff-chrome untouched;
TB185 paused/defaultOFF/PendingAllocation; checked-in gates OFF; 238 unknown 22001 correlation failure still
unclassified (no schema fix invented); 227 mobile empty-note and 225-R1 lost LOCAL edits retained. Process ledger
40544→27428→22832 (final left running), binary D:\temp\tb239r1-build\out SHA256 prefix B31E1048BA6AB101.
Elapsed ≈45min vs HARD35 with one in-task correction — NONCOMPLIANT, disclosed.
NEXT: wire an existing module-owned Development Party+offer+stock seed for the Marketplace branch (or an authorized
module-owned seed route) and then run the mixed-owner Platform+SellerA+SellerB shipping/selection/paymentpreview/
StartPayment/Order + coupon-on-one-group slice and the existing UI smoke.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

# CURRENT — 239 Marketplace boot accepted; issued239-R1 REAL DEV ADMIN COMMISSIONING + OWNER FLOW —2026-10-28

Read239/ARCHITECT-REVIEW.md + TB-SHIPPING-MARKETPLACE-ADMIN-BOOTSTRAP-AND-LIVE-FLOW-239-R1/TASK.md.
Root reviewed8f5126b docs+actualbootstrap/IdentityContracts/HostAdminPanelAccess/sellerEnsure. Real Marketplace5190/
tooba_marketplace_tb239/tenantNULL/home200 viaofficialrunner and real seller403 accepted only; noownercheckout.
SOURCEgap: MarketplaceAdminDevBootstrap grants fixedorphanccccactor, no registeredlogin. Identity.Contracts
already has Register/Find/auth; SellerEnsure returns early when Partiesmissing, legacyPublishpair isn't realA/B.
Architect NOW authorizes opt-in Development+Marketplace-only binding of dedicated normallyregistered DEV admin
to SAMEexisting marketplace-platform Member relation viaIdentityContracts+tuplewriter. Legitimatecommissioning,
NOT forbiddennewprivilegeframework. No sellerpromotion/fakeheader/JWT/SQLgrant/productionpolicy/newendpoint.
Enableddefaultfalse+explicitidentifier; exactlookup; nofallback/credentialreset; idempotent; outsideguards no
additionalbinding. Then adminnormal200/protectedseed200 withsellerstill403, officialPartydata+realSellerreconcile,
continue actualthreeownerbuyer shipping/save/preview/order/coupon/history. APIsfirst, noformdriverdetour.
Keep5190actualtarget, no repeatmigrationreset; all33schemasalreadyofficialmigrated.Original3000/5088+USER3100/
5188profile/windows/source nevertouched; checkedshippinggatesOFF/localtaskON.231/232/236appliedstore-alpha.
UNKNOWN23822001 staysunclassified, no correlationcolumnblame/schemawidening/concurrencycertificate.
FullMarketplace/coupon/practical requirement remainsOPEN untilactualproof. No production/paid/PSP/VisualACCEPT.
TB185paused/defaultOFF/PendingAllocation;227/225-R1lostLOCAL retained. Active239-R1 only;STOP_FOR_ARCHITECT;
automaticNextImplementationTask NONE. Prior recovery/history follows.

# CURRENT — 239 EXECUTED PARTIAL: real Marketplace target live; authoring admin identity missing —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-FIRST-LIVE-ACCEPTANCE-239/{TASK.md,RESULT.md}. Status
EXECUTED/PARTIAL, NOT accepted, no userVisualACCEPT, no activation. Bridge row 0326ae9f-5fa8-45ab-b33f-79e743cb7fcb.
REAL Marketplace edition target BOOTED: Host 127.0.0.1:5190 PID40544, /health 200, Edition=Marketplace,
DeploymentId tb239-marketplace, Tenant NULL, NEW task-owned DB tooba_marketplace_tb239 on 127.0.0.1:5432 (existing
DBs untouched), binary D:\temp\tb238-build\out\Tooba.Host.dll SHA256 2A2485DC2107 from 600a96f1a (no rebuild).
Development Marketplace bootstrap migrates only 12 schemas and seeds 2 dev sellers; official Tooba.MigrationRunner
plan/apply then migrated 33 schemas (catalog/offer/inventory/promotion/shipping/fulfillment/geography/tax/pricing/
cart/address_book included). GET /v1/storefront/home was 500 (42P01 catalog.categories) before, 200 real payload
after. Real seller logins on this DB 200 (seller-actor-a/b). Real denial evidence: seller token calling official
POST /v1/admin/catalog/demo/reset-and-seed = 403 admin.authorization.denied (no role/header injection attempted).
BLOCKER (actual target): fresh Marketplace DB has 0 parties/catalog products/offers; the bootstrap seeds the
platform-admin auth tuple but NO admin identity/credential, so no authorized principal can author Platform+2 seller
publications, offers, stock, drafts or coupons; the sanctioned demo seed needs that missing admin (403). Inventing
identity/grant or direct business SQL is forbidden. Therefore NOT produced: Platform+SellerA/B groups on one
binding, per-group captures/owner responses, mixed 3-group cart read->save->preview->Order, per-group coupon,
post-order publication immutability receipt, payment 390/360 captures. SingleStore NOT relabelled as Marketplace.
238 CORRECTIONS: (a) the "invalid coupon read 200 tolerant" was a 200 WITHOUT any priced group -> not a priced
acceptance; only the selection/commit checkout.coupon.not_applicable 409 is verified. (b) the 22001 attribution to
inventory.outbox_messages.correlation_id is NOT source-confirmed: CorrelationIdContext.TryNormalize/MessagingCorrelation
accept a GUID and emit 32 chars vs column 128, and no saved PostgresException column metadata existed; nothing
enlarged/truncated/swallowed/bypassed and simultaneous idempotency is NOT certified. Client payInFlightRef stays but
cannot close a server-side failure.
No source change (Code-SHA==Base-SHA 4567c1ad598f4215e61941cf74b7a45d02e1a101); no migration/schema/baseline/seed
authored; no suite replayed. Elapsed ~42min vs HARD35 NONCOMPLIANT; two corrections. Preserved: 3000/5088,
handed-off 3100/5188 + profile D:\temp\tb238-handoff-chrome, 230 DEV publication/draft/rates/week/pointer, TB185
paused/defaultOFF, 231/232/236 shared applied state. Marketplace runtime left running on 5190.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. No240.
# SUPERSEDED — 238 was current before 239 —2026-10-28

Read238/ARCHITECT-REVIEW.md and TB-SHIPPING-MARKETPLACE-FIRST-LIVE-ACCEPTANCE-239/TASK.md.
Root reviewed600a96f actualpayment/ref/current-onlyintent+responseidentity source. Retain SingleStore real DBcoupon
HTTP read/save/preview/final Order/manualPending1reservation (goods8537194+fee125000=8662194IRR); NOT browser
coupon certification, partialapplicability/Marketplace/paid. User3100/5188reviewprofile NEVER touched; new3101/
5189task target exists.238 had NO Marketplace target; formdriver trouble isn't anedition/config blocker.
NEW reportedtwoHTTPsubmit failures22001 NOT passing duplicate proof. Actual sharedOutbox normal correlation=32Guid,
columnmax128; exactfailingcolumn/parameter/binary must be verified, no blind schema widening/truncate/ignore.
239 boots actualMarketplaceDB/Host/ownerAPI FIRST, then normal threeowner buyer flow/couponpartial/orderhistory,
sameUI smoke + actualpayment390/360capture.238 onlyshippingPNGs; paymentlayout source NOT mobileproof.
Invalidread200 "tolerant bydesign" unproven: mayearlyrefuse beforequote; record fullrequest+status/body.
In-flight newidentity stillSKIP/waits; no allraceclosed claim. Freezeinputs atrequeststart ifliveblocker.
Normalnonprod setup authorized; no authbypass/directbusinessSQL/realmoney/newframework. Existingowner/evidence/
financialcore retained, allcheckedgatesOFF/localnamedON;231/232/236alreadyAPPLIED store-alpha nohistoryrewrite.
All-owner finalgate remainsopen. No genericreview/retestloop.Original3000/5088+user3100window/sourcefrozen.
TB185paused/defaultOFF/PendingAllocation;OfferAPI/227note/225-R1lostLOCALwork retained.
Active239 only;STOP_FOR_ARCHITECT;automaticNextImplementationTask NONE. Prior history follows.

# SUPERSEDED — 238 was current before 239 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-LIVE-COUPON-MARKETPLACE-COMPLETION-238/{TASK.md,RESULT.md}.
Status EXECUTED/PARTIAL, NOT accepted, no userVisualACCEPT, no activation. Bridge row b2dc1b7e-317c-44c5-af54-a593ffb97d9e.
Preserved: 237-R1 handoff FE3100/Host5188 + profile D:\temp\tb238-handoff-chrome + cart
01a1807a-e7ef-7000-82d1-285f4bd5c982 never automated/read/reloaded; original 3000/5088 untouched. New work only in
D:\temp\tb238 on 3101/5189.
COUPON LIVE (real DEV promo via actual seller API, code TB238-264452 10%): read no-coupon goods 9,485,771 -> with
coupon 8,537,194 (discount 948,577) freight120000+packaging5000 -> selection200 rev1 -> payment-draft200 discount
870,254 shipping125,000 payable 8,662,194 -> start-payment200 checkout 01a18165-04b7-7000-8764-09fb8c0bf4d4 Pending
manual, 1 reservation. Invalid code tolerated by read, refused at selection/commit with
checkout.coupon.not_applicable (409). Explicit coupon clear/change is a NEW response identity: old priced answer
becomes pending, no card money/date, typed address preserved, ONE new read.
DOUBLE CLICK measured: two same-tick start-payment calls both failed at inventory outbox (22001 varchar128) creating
ZERO checkout/reservation; a later single call created exactly ONE checkout and replay returned it. Client fix:
payInFlightRef closed BEFORE the first await, released in finally.
Source (existing paths only): payment-handoff payInFlightRef + stepper flex-wrap/min-w0/text-[11px] sm:text-sm/
px-2.5 sm:px-4/chevron hidden below sm; shipping-destination-refresh shippingResponseIdentityKey(destination,coupon)
+ NO_DESTINATION_IDENTITY; storefront-shipping keys preview mode/plan/retry on responseIdentity and builds
isValid/intent only from capturedPreviewForIntent (current only), explicit refusal instead of raw-projection fallback.
PROOF: tsc clean; destination-refresh 8/8 (incl coupon-identity fact); payment page classes 8/8; test:critical-storefront
ONCE with ONE pre-existing unrelated red (storefront-landing-blocks.tsx:286 bg-white). Measured viewports
1440/390/360 => innerWidth==requested, clientWidth==scrollWidth<=innerWidth, visualViewport consistent, PNG exactly
1440x900 / 390x844 / 360x800 in screens/.
NOT DELIVERED: NO Marketplace edition live trial (no 3102/5190 target, no Platform+2 Seller groups on one binding, no
merchant Manage/View denial) -> edition claim stays OPEN; NO payment-page native screenshot at 360/390 (stepper fix is
code+tsc only, payment metrics NOT claimed); no partial-applicability mixed-cart coupon; no post-order publication
change + Order GET immutability receipt; manual Pending only, no PSP/funds/VisualACCEPT/activation.
Elapsed far over HARD40 (NONCOMPLIANT). No migration/schema/baseline/seed; 231/232/236 applied state untouched;
checked-in gates OFF, preview/selection/checkout ON only on the named task processes; TB185 paused/defaultOFF;
227 mobile note + 225-R1 lost LOCAL edits retained. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.
# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read237-R1/ARCHITECT-REVIEW.md and TB-SHIPPING-LIVE-COUPON-MARKETPLACE-COMPLETION-238/TASK.md.
Root reviewede02c1a6 source+actualPNG: SingleStore Store/store-alpha Manual/no-coupon real normal-buyer
read->selection200->paymentpreview200->startpayment200->Order+1reservation/manualPending accepted WITHIN SCOPE.
NOT PSP/paid/fullrelease/Marketplace edition (saleschannelMarketplace != edition). Actual group125000 +
goods9485771=payment9610771IRR; compactacceptedrefs/date/ticks persist. ActiveDEVcart01a1807a-e7ef-7000-82d1-285f4bd5c982.
ReviewURL http://127.0.0.1:3100/fa/shipping +payment?cartId=...; taskHost5188 PID22404/FE3100 PID37492.
Normal authenticated dedicated visible buyer handoff required; URL in other browser doesn't carry task session.
231/232/236 migrations NOW APPLIED on verified nonproductiontooba_alpha/store-alpha via normalrunner; no reset.
237R1 original3000/5088/window untouched. Freeze237R1worktree/runtime too AFTER userhandoff;238 changes/trials
inNEW isolatedworktree/ports. Userapproved normalnonprodsetup remains; checkedingatesOFF, namedtasklocalON.
Pending LIVE: actual couponvalid/invalid/clear, MarketplacePlatform+twoSeller scopes, doubleclick/failedrefresh/
emptykey, OrderGETimmutableafterDEVpublication.238 finishes those, minimal existingstate/syncpay/mobile fixes only.
Mobilepayment claim NOT accepted: PNG360reportsinnerWidth378,390reports393; require exactCSSwidth+nooverflow.
Sharedcriticalguardbaselinebg-white retained;38missing-disposable-env test failures NOT relational passes.
Full owner/coupon/practical completion still mandatory. No generic audit/algorithm/retestloop.
ALL3TOPs updatedhere (planningrecovery was stale inworkercommit). CanonicalResult needs autoNext field afterSHA.
TB185paused/defaultOFF/PendingAllocation; OfferAPI/227note/225-R1lostLOCALwork retained; labelmap repaired.
Active238 only; STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. Prior recovery/history follows.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read `docs/architecture/evidence/TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1/` (TASK+RESULT). Status PASS
scoped, NOT accepted, nothing activated. FE `127.0.0.1:3100` + Host `127.0.0.1:5188` live; real order
`01a1807a-81f0-7000-a1be-091f589a6c1b` / `TB-20261028030754-01-faee55` (full accepted shipping evidence, 1
reservation, manual payment Pending). Wire now sends the server `expected*` keys; root CAS comes from the persisted
draft revision, not an invented `"0"` (the live `storefront.captured_selection.stale` regression is fixed and was
reproduced first). Still open: live coupon trial, double-click/failed-refresh negatives, Marketplace/each-real-seller
owner trial, PSP success. Screens 1440/390/360 at exact viewports. 231/232/236 APPLIED on non-production
`tooba_alpha`. User FE `:3000`/Host `:5088` and review window untouched. STOP_FOR_ARCHITECT; no next task.

# SUPERSEDED — 237 NOT CONNECTED reviewed; issued237-R1 BOOT FIRST + exact wire/payment completion —2026-10-28

Active TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1. Read237/ARCHITECT-REVIEW.md + newTASK.
Root reviewedaffef4029 actual page/API/backendDTO/paymenthandoff: captured WRITE incorrectly uses confirmation*
instead of expected*; page sends forbidden top-level legacy method; builder skips unavailable groups and doesn't
check all tokens. Payment realpath final-click-api/handoff still lacks expectedSelectionRevision entirely.
These are SOURCE blockers, not environment anxiety.237 has NO runtime/URL/migration/trial;6units not delivery.
Retain backend mandatorygoods/couponApplied guard and choice scaffolding; do not reopen financial/owner core.
237-R1 reuses isolatedD:\temp\tb237, boots Host/FE FIRST, actual MigrationRunner pending231/232/236 and normal DEV
setup authorized already. Fix actualwire/allgroups/currentcontext/syncgate/paymentTEXT on SAME UI, realPUT200 ->
paymentpreview200 -> actualStartPayment stage. First SingleStoreusableURL, actualMarketplace remaining if notrun;
all-owner final delivery remains mandatory. Checked-ingatesOFF, coherenttasklocalON; no realmoney/authbypass.
Original watchedcheckout FE3000/Host5088 and USER unsavedreviewwindow untouched; noHMR/user-tab inspection.
Main target via isolatedtaskbranch normalHEAD:main, oldwatchedcheckout disk intentional. No new schema/engine/audit.
Again literalprobe sent byworker is a protocol violation; never diagnosticPOST toResults. Complete currentbody+
actualsource reviewed; no rootBridge interaction/transportrepair task. Requiredcriticalvisualcheck/screens notrun.
Known230emptykey/inflight tocloseinexistingcoordinator; rootcontextstrictness/paymentclear needed. Historic labelmap
already repaired, do not re-add that resolved gap. PreserveTB185paused/defaultOFF/PendingAllocation/225-R1lostLOCAL.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. Prior recovery/history follows.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-CONNECTED-PRACTICAL-TRIAL-237/{TASK.md,RESULT.md}.
Status EXECUTED/PARTIAL, NOT accepted, no userVisualACCEPT, no activation, no usable URL, no account handoff.
Base(issue)f9b873eee, isolated worktree task237, original watched checkout6d820e9ae intentionally untouched/behind.
A: `AssertGoodsEcho` now REQUIRES canonical non-negative decimal WIRE text per selected group ("0" lawful);
missing/blank/grouped/signed/exponent ⇒ `storefront.captured_selection.intent_invalid` BEFORE mutation; real
mismatch ⇒ `conflict`; AllowDecimalPoint only; server composed goods stay authority.
B: `CheckoutDirectory` quote path accumulates WHOLE-cart `promotion.Applied[].CouponCode` evidence; nonblank intent
with zero applied coupon promotion ⇒ `checkout.coupon.not_applicable` (409, fa/en). Partial applicability lawful;
NO Order-side coupon text matching, no amount==0 inference, no new engine/duplicate evaluator. 236/RESULT corrected
(it had wrongly claimed rejection handling existed).
C(frontend, existing page/components only): decoder keeps exact confirmationSnapshot/SourceRevision/
SelectionVersion/AreaGroupId/RateRowId + root selectionRevision through `asExactTokenOrNull`(no String/number
coercion); captured cards choose method+allowed date per SERVER GroupKey (method key = service/option WITHIN group);
selection hoisted to page; new `storefront-shipping-captured-selection.ts` builds the intent only from the successful
response (exact goods text, all echoes, that date's own interval, real lineIds) else null; PUT sends
capturedGroups+expectedSelectionRevision+same coupon and never mixes legacy shippingGroups; context key
(destination·coupon·cartVersion·stored revision) resets confirmations. Gates stay checked-in OFF.
PROOF: `tsc --noEmit` clean; new captured-selection test 6/6; Order.Infrastructure build 0 errors.
NOT DONE: NO live buyer trial/no migration plan OR apply/no taskHost/no taskFE/no DEV data/no browser or HTTP
read→save→preview→StartPayment/no screenshots/no SingleStore or Marketplace runtime/no URL. Payment page
StartPayment not exercised. `StorefrontFullOwnerVerticalTests.SelectionRequest` still omits ExpectedGoodsAmount ⇒
expected red under A; not updated and that class NOT run (no disposable PostgreSQL). No test:critical-storefront
visual run. Elapsed ≈95min vs HARD45 = NONCOMPLIANT, recorded.
Preserved: user review window/previewONcount7/live HostFE/data untouched; 231/232/236 sharedNOTAPPLIED; stale EF
snapshot; TB185 paused/defaultOFF/PendingAllocation. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.
Older TOPs are historical.

# SUPERSEDED — 236 valid coupon mechanics retained; issued237 CONNECT EXISTING UI + PRACTICAL LOCAL TRIAL —2026-10-28

Active TB-SHIPPING-STOREFRONT-CONNECTED-PRACTICAL-TRIAL-237. Read236/ARCHITECT-REVIEW.md and237/TASK.md.
Root reviewed6d820e9/source36c44: samecoupon217/selection/finalgoods confirmation and2nullable draft scalars
retained; nullable-date override/nonemptytype fixed.8/8+5/5+173/173 worker, NOT root/live certificate.
Two blockers inline237: ExpectedGoodsAmount currently OPTIONAL bypass, and actual quote path ignores Promotion
RejectionReasons (invalid coupon handling NOT already proven/implemented). Fix on practical path, no audit loop.
USER latest: connect quickly, test data not concern. Authorized normal LOCAL migrations/devdata/runtime setup,
no repeated routine permission.237 implements frontend cards/groupchoices/save/paymentpreview/StartPayment with
exact tokens/context and actual browser/HTTP trial. All owners/coupons remain final requirement; no false readiness.
Protect USER unsaved reviewwindow by ISOLATED implementation worktree + task-owned FE/Host ports; original watched
FE3000 checkout files/runtime untouched (intentional old branch allowed). Push normalHEAD:main, verify worktree parity.
Expected231/232/236 migrations MAY apply via actual MigrationRunner plan to verified nonproduction target, no
history rewrite/reset; new Marketplace taskDB via official bootstrap allowed. No production/realmoney/authbypass.
Checked-in gatesOFF; coherent ON only tasklocal. Normal buyer/admin APIs; DEV data may be created, originaluserdraft
untouched. Leave working URL; actual PSP/hold stages honest. ExistingCSS/cards, desktop1440/mobile360/390.
Known230emptykey/newkeywait addressed in same coordinator; crossowner labelmap already repaired, decoderstrictness
is separate. PreserveTB185paused/defaultOFF/PendingAllocation and225-R1unrecoveredLOCALwork record.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. Earlier no-runtime/no-frontend scopes were task-local,
NOT a ban on this newly authorized practical task. Prior history follows.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-CAPTURED-COUPON-CONTEXT-PARITY-236/{TASK.md,RESULT.md}. Status
EXECUTED, NOT accepted, no userVisualACCEPT, no activation; base607e12c17, code36c44a35; claim23:18Z→result23:55Z
≈34min inside HARD45 (source/proof split untimed) but the one-correction budget EXCEEDED (5 recorded fixture/test
corrections in RESULT §G). Bridge row 9a393fb5-c1b4-4f3d-b82c-d145a433ac8d.
LANDED on the EXISTING paths only: `StorefrontCouponIntent{Normalize,Same}` is a confirmation TOKEN (trim only, no
fold/length/code policy) — Promotion/Tax remain the only pricing authority. Coupon reaches read
(transport→endpoint→query→ProjectAsync→planner SubmitCheckoutCommand), selection (writer re-quotes the SAME intent in
the SAME 232 atomic/CAS/rollback boundary and stores it on the draft root), public payment-draft and public commit
(blanket `coupon_unavailable` removed where the coherent path exists). Final acceptance compares the current intent
with `SelectionCouponCode` and EVERY group's stored `ConfirmedGoodsAmount` with THIS submit's own quote before any
reserve/order/convert (A→B/clear/changed-goods ⇒ `storefront.captured_selection.conflict`, zero writes).
231-residuals closed: `ComposePreviewSnapshot` branches on the OVERRIDE OBJECT (intentional null dates stay absent,
old command dates never resurrect); `ResolveGroupOwner` requires a nonempty assigned type equal to stored evidence on
EVERY line. Schema: ONE additive Order migration 20261027230000 — `selection_coupon_code text` on
`order.cart_shipping_drafts` + `confirmed_goods_amount numeric(18,2)` on `order.cart_shipping_groups`, one shape CHECK
each, WRITTEN/sharedNOTAPPLIED, no JSON/table/backfill, legacy NULL = unconfirmed.
PROOF (worker execution, ONE disposable postgres:17-alpine tb236 127.0.0.1:15433/order_tb231/tb231, identity before
DDL, normal migration chain, container removed and port verified closed): StorefrontCapturedCouponParityTests 8/8
(coupon SAVE10 through read→save→preview→commit on 3 lawful owners/one binding with own 99/220/330 goods; changed
coupon with stale echo refused with zero writes; SAME code with changed goods refuses preview+commit at zero
reserve; idempotent replay returns the original order with zero extra quote/pointer/reserve; explicit date absence
never resurrects command dates; REAL inter-read boundary via an existing TEST port with root advanced 1→2, +40/group,
distinct dates, absent aggregate, zero writes; DB proves the two nullable columns, zero json, both CHECKs and
23514 refusals for untrimmed coupon/-1 goods; blank intent reaches Promotion as null and uses the base path) and
StorefrontShippingCouponEndpointTests 5/5 (the actual projection delegate forwards `" SAVE10 "`/`save10`/
`4294967296`/Persian text VERBATIM, gate once; payment-draft GET forwards `%20SAVE10%20` verbatim). Directly affected
focused set ONCE: 173/173; Order.Tests build 0 errors. NOT re-proven: Promotion/provider failure + caller
cancellation on the coupon path (§5.3 half), no HTTP/listener/live/browser/PSP/payment, no physical per-tenant DB,
no Order-only historical read, no frontend wiring, no desktop/390/mobile practical trial. Case-sensitive token
comparison is deliberate (Order must not invent Promotion's matching policy).
NO frontend/HMR/browser/user-window access, no Host/FE rebuild/restart/runtime gate change, no shared migration,
no live business write; USER VISIBLE review window is USER unsaved work and was never enumerated/read/automated/
reloaded/closed; local previewON/count7 unchanged; all checked-in Enabled/SelectionEnabled/CheckoutEnabled stay false;
231/232 sharedNOTAPPLIED and the stale EF snapshot gap retained; TB185 paused/defaultOFF/PendingAllocation.
Known 230 empty-key/in-flight, cross-owner label/decoder, Offer API, 227 mobile empty-note, 225-R1 lost LOCAL edits
retained. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. Older TOPs are historical.

# SUPERSEDED — CURRENT — 235-R1 owner vertical scoped accepted; issued236 shared coupon context —2026-10-27

Active TB-SHIPPING-CAPTURED-COUPON-CONTEXT-PARITY-236. Read235-R1/ARCHITECT-REVIEW.md and236/TASK.md.
Root reviewed9b765e81b/sourcecb3d054bb: real read/232selection/publicpreview/publiccommit/Order persistence for
Marketplace Platform+SellerB+SellerC on same binding with DISTINCT captures/labels/rows/dates/ticks125/73/94
is accepted as controlledContracts/realOrder integration, NOT live HTTP/PSP/physicalDB/visual release proof.
PayloadOfferId/defined responsibility/initialized line guards and presentation-cart reuse retained. Residual:
nullable type BOTHNULL still passes; preview nullable override ?? restores stale command date; fix inline236.
235-R1 replacement test advances writer BEFORE publicpreview (no inter-read boundary), dates unchanged; do NOT
claim deterministic stale-outer/new-inner proof.6/6 new worker facts, no measuredRED; broad412sweep out-of-scope.
236 implements SAME coupon across existingread/selection/paymentpreview/finalquote using existingPromotion/Tax/
goods builder. Minimal own-draftcoupon+pergroupgoods scalars/new additive migration permitted ONLY isolatedDB,
no JSON/fullsnapshot/perline copies. ExistingatomicCAS/rollback/idempotency and all owners retained.
NO frontend/HMR/browser/runtime/HostFE/livewrites/sharedDDL/gateactivation. USER reviewwindow unsavedwork
never enumerate/read/automate/reload/close; previewONcount7 untouched. AllcheckedgatesOFF;231/232sharedNOTAPPLIED/
EFsnapshotgap retained. Coupon/frontend/practical desktop360/390+mobile and Order-onlyhistory remain mandatory
final delivery, not exclusions. Known230/OfferAPI/227/225-R1lostLOCALwork retained. TB185paused/defaultOFF/
PendingAllocation. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. Older TOPs are historical.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Active TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1 (row5daf73f2-b83d-45e5-82ba-e2ac37383c62).
Read235-R1/TASK.md and235-R1/RESULT.md. Status EXECUTED/PARTIAL, NOT accepted, no userVisualACCEPT, no activation.
Base1662e6f92. A: ResolveGroupOwner now refuses an assignment payload carrying another OfferId, requires a DEFINED
responsibility and a non-empty assigned type equal to stored evidence on EVERY line, explicit initialized flag (null-first/
valid-second and reverse both refuse before reserve/order; sameOfferId multi-line lawful). B: ProjectPaymentDraftAsync
passes the already authorized CartPage into the captured facade (no second cart load; Directory keeps its own
authoritative cart/version check) and ComposePreviewSnapshot takes a typed CheckoutPreviewShippingOverride derived from
the ACCEPTED groups (sum, earliest minimum, shared method/date/window only when every group agrees, else explicit empty),
so money+dates+method describe ONE accepted root. C: new StorefrontFullOwnerVerticalTests 6/6 on disposable
15433/order_tb231 (identity before DDL, normal migrations, container removed, port closed): real planner -> 232 writer ->
public preview -> public commit -> persisted Order owners/evidence for Marketplace Platform + two Seller business ids on
ONE binding with own capture/row/money/date/100ns ticks (125/73/94 own payer), one219 per full binding, one216 per group,
swapped owner responses refused with zero reserve/order, foreign payload and null-first/valid-second refused, and a REAL
selection advance (rev1->2, +40 each) yielding one coherent accepted preview (revision"2", money AND dates, zero writes).
Focused Checkout+Storefront run 412 total / 410 pass; the 2 reds are PRE-EXISTING guards (DateTimeOffset.UtcNow in the
229/231 planner, packaging allow-list). HARD35 ~23min but one-correction budget EXCEEDED; measured RED for the two
regressions NOT taken (source-derived expectation only) — disclosed in RESULT. NOT DELIVERED: no HTTP/live/browser/PSP/
payment/carrier, coupon parity still refuses, no test-only seam (real writer advance instead), no physical per-tenant DB
isolation claim. 231/232 sharedNOTAPPLIED/EFsnapshotgap, gatesOFF, no migration/schema/baseline edit. USER reviewwindow
unsavedwork/previewONcount7/Host/FE/data untouched. Known230emptykey/inflight/OfferAPI/227mobile/225-R1lostLOCALwork
retained. TB185paused/defaultOFF/PendingAllocation. STOP_FOR_ARCHITECT after235-R1; Automatic-Next-Task NONE.


# SUPERSEDED — corrected235-R1 task envelope and architecture contract —2026-10-27

Same active task TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1; NOT a new implementation task.
The earlier ef929373 task artifact was malformed: missing BEGIN_TOOBA_TASK, complete routing header and RESULT
CONTRACT. This corrected issuance supersedes that artifact, preserves implementation scope and adds explicit
existing Order file/namespace/Contracts/DI/error boundaries. Read corrected TASK.md and235/ARCHITECT-REVIEW.md.
Resolve ISSUE_COMMIT_CONTAINING_THIS_TASK to THIS corrected issuance before editing. Do not execute two copies.
No implementation/runtime/gate/schema/data change by this correction. USER reviewwindow/unsavedwork untouched.
All prior full-owner/coupon/frontend/practical-trial requirements and safety/preservation limits remain binding.
Active235-R1 only; STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. Prior recovery follows.

# SUPERSEDED — 235 PARTIAL reviewed; issued235-R1 per-line validation + coherent preview + actual owner vertical —2026-10-27

Read 235/ARCHITECT-REVIEW.md and TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1/TASK.md.
Root reviewed1662e6f92 actual code, not worker tests execution. Retain shared typed owner policy, owner+binding
labels in source, accepted-group amount, exact revision, separate OrderLineIds and absent-history no fallback.
BLOCKERS: ResolveGroupOwner lost assignment.OfferId==lookup key; null responsibility used as uninitialized means
first missing assignment may be overwritten by later valid line. Validate EACH before owner derivation.
Preview amount fixed but ComposePreviewSnapshot retains old command dates/method; deterministic Order-root
replacement proof missing. Reuse first authorized presentation CartPage (current facade adds another load).
Marketplace edge manually seeds accepted owners; pointer fake gives same capture for every owner; NOT proof of
read->selection->preview->commit with distinct owner captures/labels. Issue235-R1 closes these with one strict
Marketplace Platform+SellerA+SellerB actual existing-path fixture, controlled Contracts/realOrder, not live.
Worker fullOrder564/566 was out-of-scope; do not replay. Worker commit did not update recovery TOPs; fixed here.
NO frontend/browser/HMR/runtime/HostFE/livewrites/sharedDDL. USER reviewwindow unsavedwork never touched.
All checked-in gatesOFF; localpreviewONcount7 unchanged;231/232 sharedNOTAPPLIED/EFsnapshotgap retained.
FULL final delivery still BOTH editions/all owners + coupon parity + existing frontend + complete practical
desktop/mobile trials; not delivered by this repair. Known230/OfferAPI/227/225-R1lostLOCALwork retained.
TB185paused/defaultOFF/PendingAllocation. Active235-R1 only; STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
Prior recovery statements below are historical when conflicting with this TOP.

# SUPERSEDED — issued235 full-owner flow + necessary preview contract repairs —2026-10-27

Active TB-SHIPPING-FULL-OWNER-FLOW-AND-PREVIEW-CONTRACT-235. Read234/ARCHITECT-REVIEW.md and235/TASK.md.
FULL release scope Store/Marketplace/eachrealSellerStore + coupon context + actual existingUI/practical trial
remains binding;234 SingleStore/noncoupon mechanics are NOT final delivery. Root reviewed3ed9f4279 actual code.
Scoped retained: coherent public commit delegating to actual Directory, idempotent Order replay, read-only
payment validation, exact-text token inprocesscomposer. NO endpointdelegate/listener/PSP proof;5/5+200/200worker.
Concrete mapping blockers: paymentpreview StubCartPage => version0/empty productmetadata; old command totals
canmixwithfreshacceptedgroups; OrderLineIds wrongly emitted as CartLineIds; missing historical groups fallback
to CURRENT draft. Fix in235 alongside required owner support, no helper-only audit/replay loop.
235 share real Order owner resolution across read/selection/final: Storetenant, Marketplace, Seller actualOffer
businessSellerPartyId, neverloginuser; close cross-owner labelmap; preserve merchant auth/buyer cart boundaries.
Use real existing business relationships; no inventedStoreId/framework. No arbitrary admin delegation claim.
Coupon remains current-safe refusal ONLY until next required common-context implementation; not final exclusion.
NO frontendHMR/runtime/HostFE/livewrites/sharedDDL/browser; USERreviewwindow unsavedwork untouched, previewONcount7.
231/232 sharedNOTAPPLIED/EFsnapshotgap; allcheckedgatesOFF; no activation/fullcheckout/VisualACCEPT. Known
230emptykey/inflight/OfferAPI/227mobile/225-R1lostLOCALwork retained. TB185paused/defaultOFF/PendingAllocation.
STOP_FOR_ARCHITECT after235; Automatic-Next-Task NONE. Prior full delivery scope/recovery follows.

# CURRENT DELIVERY SCOPE — BOTH editions + all real seller/store owners + coupon context required —2026-10-27

Latest explicit user instruction REJECTS final delivery limited to SingleStore/noncoupon.
URGENT PRIORITY: real usable vertical trial for sale; after234review prioritize actual owner/frontend/coupon
flow and task-owned practical desktop/mobile run. No generic audit/helper-only or broad test-confidence loops.
Read docs/architecture/shipping/SHIPPING-COMPLETE-OWNER-AND-COUPON-DELIVERY-SCOPE-2026-10-27.md.
Required final flow: independent SingleStore stores, Marketplace-owned fulfillment AND each authorized seller/store
owned fulfillment, mixed cart per-group methods/prices/dates, server permissions and actual business owner identity,
SAME coupon context in preview/selection/final quote, existingUI desktop/mobile and actual complete trials.
No login-user ownership or invented StoreId/SellerPartyId mapping, no fake ownerpicker, no blanket final denial of
required owner/coupon paths. Reuse same separate surfaces/forms/cards and real Contracts/policy authorities.
CURRENT ACTIVE234 remains bounded public backend edge, NOT final handoff. Do not broaden its claimed task mid-run;
after review implement missing full-owner/access, coupons, frontend and complete trials as focused slices.
231/232/233 prior acceptance remains SCOPED SingleStoreManual/noncoupon, not full final acceptance.
Earlier3-task/1-2hour estimate was for smaller scope and is superseded; no unverified replacement promise.
This docs-only scope update changes NO runtime/source/gates/schema/data and issues NO235. USER reviewwindow
unsavedwork/HostFE/localpreviewONcount7 untouched; migrations231/232sharedpending/EFsnapshot gap retained.
TB185paused/defaultOFF/PendingAllocation. All prior recorded gaps retained. Detailed active-task recovery follows.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-CAPTURED-PUBLIC-COMMIT-EDGE-234/{TASK.md,RESULT.md}. Status
EXECUTED, NOT accepted, no userVisualACCEPT, no activation; base3f4ee29e6; ≈28min vs HARD35 (source/proof split
not timed); one-correction budget EXCEEDED (5 recorded rounds in RESULT §G).
LANDED on the EXISTING edge only: `StorefrontCapturedPreviewPlanner.IsCapturedCommitEnabled` =
Enabled&&SelectionEnabled&&CheckoutEnabled&&HasValidAvailableDateCount (all checked in OFF, untouched);
`StorefrontShippingService.CommitAsync` refuses `storefront.captured_preview.not_ready` BEFORE any read when the
gate is incoherent/OFF, routes a stored-evidence draft to the NEW
`StorefrontCheckoutService.SubmitCapturedSelectionAsync` (server-owned stored root/groups + real actor; NO
PrepareShippingAsync, NO live-cart preamble, NO legacy SaveSelectionAsync, NO second217/218, nonblank coupon
refused before delegation) which delegates to the EXISTING `ICheckoutDirectory.SubmitAsync`/
`CheckoutProcessManager` (access/idempotency first, then the 233 acceptance inside the established submit
transaction before reservation) and maps the response from the PERSISTED snapshot with no post-commit external
read. Preview-ON with NO stored evidence still refuses and never falls to legacy; a truly OFF deployment keeps
the legacy path. New read-only port `ICheckoutDirectory.ProjectCapturedPaymentDraftAsync` →
`CapturedPaymentDraftSnapshot` (ONE LoadPreviewCartAsync+explicit version check, ONE whole-cart
QuoteSellerOrdersAsync, the SAME `AcceptCapturedShippingCoreAsync` with requireExplicitSelectionRevision:false
and NO root lock, shared extracted `ComposePreviewSnapshot`) with zero SaveChanges/reserve/order/conversion/
abuse; the 217 goods reader was NOT taught final acceptance. Token path Body→Request→Composer→Commit→command
keeps `4294967296` as lossless TEXT; a JSON number/missing/whitespace/stale token refuses with
`storefront.captured_selection.intent_invalid` before reservation.
PROOF on ONE task-owned disposable postgres:17-alpine `tb234` 15433/order_tb231/tb231 (identity asserted before
DDL, normal Order migration chain, container removed, 15433 verified closed): new
`StorefrontPublicCommitEdgeTests` **5/5** — two-seller persisted order with own 125/75 money, own dates and
fractional 100ns ticks, 1 reservation / 1 conversion / 2 quotes and no legacy rewrite; PUBLIC replay after a
pointer advance + dropped saved address + Converted cart returns the SAME order with ZERO new quote/pointer/
address/reserve/convert and a foreign actor denied with no new rows; read-only payment draft with a REAL saved
address and conflict/destination_unverified/not_ready negatives at zero writes; the real StartPayment body with
the large revision TEXT reaching the accepted commit and stopping honestly at `HoldPreparationFailed` with
Payment never called; and the legacy-OFF control (draft without evidence stops on its own
`shipping.first_name_required` with zero pointer/address/reserve, while preview-ON-without-evidence refuses
`not_ready`). Directly affected captured/edge/storefront/shipping classes re-ran ONCE: **200/200**. Order
Application/Infrastructure/Endpoints/Tests build 0 errors.
NOT DELIVERED: no HTTP/listener/browser/live-cart exercise (in-process and model-bound only), no PSP or payment
write, no Marketplace/Seller/carrier path, no coupon parity, no history/repricing endpoint, no activation/
readiness/VisualACCEPT, and no legacy positive order end-to-end. Two PRE-EXISTING red guards remain disclosed
(architecture `DateTimeOffset.UtcNow` in the 229/231 planner, packaging allow-list) and were proven red at the
base commit — not repaired, not widened. The remaining user-visible step is still the frontend/live trial.
No migration/schema/baseline edited; 231/232 migrations sharedNOTAPPLIED and the stale EF snapshot gap retained.
No frontend/HMR/browser/Host-FE/runtime change; the USER VISIBLE review window is USER unsaved work and was
never enumerated/read/automated/reloaded/closed; local previewON/count7 unchanged. Known 230 emptykey/inflight/
crossownerlabelsdecoder/OfferAPI/227mobile/225-R1lostLOCALwork retained. TB185 paused/defaultOFF/
PendingAllocation. STOP_FOR_ARCHITECT after234; Automatic-Next-Task NONE. History retained below.

# SUPERSEDED — 233 core captured submit ACCEPTED; issued234 public commit edge —2026-10-27

Active TB-SHIPPING-CAPTURED-PUBLIC-COMMIT-EDGE-234. Review233-R3/ARCHITECT-REVIEW.md,234/TASK.md.
Root reviewed1f0ee603e/source897fc673c actual address/provenance/catalog/core/comparison/tests. Accepted within
SingleStore Store Manual NONCOUPON controlledContracts/realOrder scope: same-binding sharedcapture/area, real
99/198, own rows/dates/fractionalticks, typedpointercache, fresh address/country and activebinding, actualOrder
write/replay/rollback/rowlock.25/25+68/68 are WORKER execution not root. Coarse isolatedDB lockwaiter probe not PID
certificate; first winner rawSQL, second real232adapter; DI test equivalentregistrations notfullOrderModule.
234 connects EXISTING publicCommit/PaymentDraft/StartPayment to acceptedcore behind coherent defaultOFF gates.
No legacy replay/staticrepricing for captures; public idempotency must survive convertedcart/deletedaddress/
changedpointer; PaymentDraft real validation read-only,217goodshelper stayspure. Actualrequest token TEXT
forwarding +realOrderDBproof, controlled downstreampayment only. NO newcoupon engine/parity/historyendpoint.
NO frontendHMR/runtime/HostFE/livewrites/sharedDDL/browser. USER VISIBLE reviewwindow USERunsavedwork:
no enumerate/read/automate/reload/close; localpreviewONcount7 unchanged. 231/232 sharedNOTAPPLIED/EFsnapshotgap.
Current publicguards not_ready until234review; no activation/fullcheckout/payment/VisualACCEPT. Known230emptykey/
inflight/crossownerlabelsdecoder/OfferAPI/227mobile/225-R1lostLOCALwork retained. TB185paused/defaultOFF/
PendingAllocation. STOP_FOR_ARCHITECT after234; Automatic-Next-Task NONE. History retained below.

# HISTORICAL WORKER RESULT — 233-R3 executed: real country/address authority + corrected real submit proof —2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-CAPTURED-ADDRESS-BINDING-AND-PROOF-233-R3/{TASK.md,RESULT.md}.
Status EXECUTED, NOT accepted, no userVisualACCEPT, no activation; base2218873b9; elapsed≈20min from the
Bridge claim(17:06:43Z) to report — inside HARD40, source/final split not timed; one-correction budget exceeded. Final acceptance resolves a canonical NONBLANK ISO country from CURRENT trusted country
data matching the priced stored country id (unique match else destination_unverified; no IR literal, no
first-country, no label guess) and passes it to ResolveLegacyAsync; resolved country/area/city ids must equal the
stored priced destination and serviceArea stays explicitly unsupported. SavedAddressId is read through
IAddressBookCheckoutLookup.GetAsync with the REAL server actor (cart owner == PlacedByUserId == CartAccess.UserId),
and missing/foreign/edited records refuse BEFORE reserve; the accepted command province/city/postal/mobile must
equal the priced root exactly, so the persisted Order address cannot differ from the priced one. HONEST LIMIT:
the command carries NO country field, so country identity is asserted on the priced root + resolved ids.
Server-only StorefrontCheckoutPlacement.SourceSavedAddressId (trailing optional) keeps snapshot SavedAddressId
null and the232 writer persists it into the EXISTING draft column; unknown stays unknown, no new column/backfill.
ONE IShippingCatalogReader read per submit requires each stored service/option ACTIVE (exact trimmed code, null
option = absent, no fuzzy); typed StorefrontCapturedPublicationBinding.For for the219 pointer cache; the pointer
must be Selected and equal DraftId/SourceRevision/SelectionVersion/SnapshotId/FormatVersion/ShippingTypeId and
echo the requested binding; the quote must repeat SnapshotId/FormatVersion/DraftId/SourceRevision. Gate adds
HasValidAvailableDateCount; a missing optional authority is an explicit unavailable; the true legacy passage is
unchanged with zero foreign calls. Corrected fixture (same class, no new framework): TWO sellers on ONE Store
binding, ONE shared snapshot/draft/revision/area group with own rate rows, REAL 100-10+10%=99 and 200-20+10%=198
through the existing pricing/tax registration, ONE shared stored week with fractional 100ns ticks on two lawful
dates, strict Geography (null/wrong code or name => Unresolved, actual IR asserted), genuinely changed-pointer
replay, 16 exact negative codes, barrier rollback, provenance-through-preparation, module-scope resolution.
PROOF on ONE task-owned disposable postgres:17-alpine /tb231 15433/order_tb231 (identity before DDL, container
removed, 15433 verified closed): CapturedFinalSubmitIntegrationTests 25/25, all StorefrontCaptured* classes 68/68
(writer/storage/transaction subset 32/32), Order.Tests build 0 errors; no124/36/28/31/50/119/188/194/285/310/312/
fullOrder/Host/solution/guard sweep. Bounded concurrency on the same target: an uncommitted selection advance
before the root lock is an exact conflict with zero reservation/order; the REAL232 selection writer at the same
expected revision is proven BLOCKED on the accepted root (pg_stat_activity waiter probe, no assumed sleep),
released once, then the persisted order evidence survives while the revision advances. NOT delivered: public
CommitAsync/ProjectPaymentDraftAsync stay not_ready, checked-in gates OFF, coupon read/selection/commit parity
unimplemented, no HTTP/live/StartPayment/provider/payment or Marketplace proof, point-in-time lock only (no
serializability claim), 231/232 migrations sharedNOTAPPLIED/EFsnapshotgap retained. Two test-hygiene corrections
beyond the single allowed round (fixture window length, missing usings, replay assertion).
USER VISIBLE review window is USER unsavedwork: never enumerate/read/automate/reload/close; localpreviewON/count7,
Host/FE/runtime/data/cart/addressbook/draft/rates/week/pointer/assignments untouched. Known230emptykey/inflight/
crossownerlabelsdecoder/OfferAPI/227mobile/225-R1lostLOCALwork retained. TB185paused/defaultOFF/PendingAllocation.
NEXT (Architect): review this proof, then coupon read/selection/commit parity and any live submit/StartPayment
parity slice if required. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

# HISTORICAL — 233-R2 integration scoped accepted; issued233-R3 address/binding/proof —2026-10-27

Active TB-SHIPPING-CAPTURED-ADDRESS-BINDING-AND-PROOF-233-R3. Read233-R2/ARCHITECT-REVIEW.md and233-R3/TASK.md.
Root reviewed2ef0125cd/source1d2106dd2: reader-free core/fixedgroupkeys/commandfacts/actualOrder persistence/
ordinary replay/barrier rollback accepted as MECHANICAL integration with controlledContracts ONLY.
Evidence corrections: fixture has post/standard +tipax/express NOTsamebinding;pointercalls2;SAMEdate/wholeminute
hours NOTfractional;prices99/198 tax0 NOT100-10+9;replaydoesNOTmutatepointer;DI manuallyregistered equivalent
types NOTrealOrderModule.AddServices. Do not repeat these false-expanded claims. Samebinding+destination
must share SAMEareaGroupId; independent rate rows/dates on sharedweek required (Architect clarification).
RealGeography nullcountry=>Unresolved, finalsource ALWAYSnull; savedAddressnotowned/re-read and PrepareShipping
dropssourceSavedAddressId. R3 preserve serverprovenance existingdraftcolumn, realcountry/addressauthority,
activebinding/snapshotformatchecks, correctedactualpositive+changedpointerreplay and boundedroot contention.
NOcoupon scope expansion; publicCommit/PaymentDraft stay CLOSED, checked-in flagsOFF. NofrontendHMR/runtime/
HostFE/livewrites/sharedDDL. USER VISIBLEreviewwindow USERunsavedwork: no enumerate/read/automate/reload/close.
LocalpreviewONcount7 unchanged. 231/232 accepted; migrationssharedNOTAPPLIED/EFsnapshotgap retained.
No fullcheckout/paymentactivation/VisualACCEPT/fullJSON/order/historyrepricing. Known230emptykey/inflight/
crossownerlabelsdecoder/OfferAPI/227mobile/225-R1lostLOCALwork retained. TB185paused/defaultOFF/PendingAllocation.
STOP_FOR_ARCHITECT afterR3; Automatic-Next-Task NONE. History below retained.

# HISTORICAL WORKER RESULT — 233-R2 EXECUTED: real positive submit through the actual process — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-CAPTURED-SUBMIT-INTEGRATION-233-R2/{TASK.md,RESULT.md}; a correction
was appended to 233-R1/RESULT.md. Status: **EXECUTED**, NOT accepted, no user Visual ACCEPT, no checkout/payment
activation, no frontend/HMR/browser/Host-FE/runtime change, no shared migration/schema/baseline edit, no live write.
Base = the issue commit; elapsed ≈100 min vs HARD45 — recorded NONCOMPLIANT (positive reached, so the
"stop at source cutoff" branch was not taken).
LANDED: the four reviewed source blockers are repaired by MOVING code — `StorefrontCapturedGroupCompositionCore`
(reader-free, scoped) owns ValidatePlan/ValidateFacts/candidate/216/calendar so `CheckoutDirectory` and the 218
composer share it with no DI cycle (real module-scope resolution fact); `EnsurePlanIsValid` runs BEFORE the cart/facts
read; facts/plans/accepted groups all key by the persisted `ShippingGroupId.ToString("N")`; `ComposeFromFactsAsync`
validates against `request.Command.CartId/ExpectedCartVersion`; `sellerByOffer.ToDictionary` is gone (two lines may
share one OfferId; per-line key/offer/seller checks only). Acceptance is now invoked INSIDE
`CheckoutProcessManager`'s existing TransactionScope, before reservation, with a narrow Order-row
`SELECT selection_revision … FOR UPDATE` compare of the caller's parsed revision (point-in-time lock, NOT
distributed serializability, no Shipping-schema lock).
EXECUTED PROOF on ONE task-owned disposable postgres:17-alpine 15433/order_tb231/tb231 (identity asserted before the
normal migration chain; container removed, port verified closed): new `CapturedFinalSubmitIntegrationTests` **12/12**
— real public `CheckoutDirectory.SubmitAsync` positive for TWO sellers on ONE Store binding/shared snapshot with
distinct persisted groups/line sets/area+row/date/fractional 100ns hours/fees, final goods 99/198 reaching the
matching 216 calls, persisted checkout + SellerOrder lines + order shipping groups carrying compact `AcceptedEvidence`
(payable vs carrier collect kept separate), one whole-cart quote, zero recursive 217 preview; lawful duplicate
`OfferId` across two lines; SEVEN parameterized early refusals (gate OFF, owner, stale pointer, changed money,
changed hour, coupon, wrong cart/version) each with zero reservation/order/conversion; barrier failure after the order
write leaves zero checkout/group rows and the SAME exception instance; repeat submit after a pointer change returns
the ORIGINAL checkout with no extra quote/pointer calls and one reservation. Directly affected classes ran ONCE with
it: **124/124**. Affected Order projects build 0 errors. Two in-task corrections (assert order, shared-offer
discriminator) exceeded the one-correction budget — recorded.
NOT DELIVERED (PROMINENT): **AddressBook saved-address authority is NOT implemented** — the destination is still
resolved with `IGeographyLookup.ResolveLegacyAsync` passing `countryCode: null`, and `SavedAddressId` is not re-read
through AddressBook Contracts, so command display strings are not verified against the address book and a permissive
geography fake can make the destination match; the true concurrent-advance case is only a sequential stale
pointer/revision control; the 219 read is an Order-side Contracts port, never live/HTTP; coupon read/selection/commit
parity, public `CommitAsync`/`ProjectPaymentDraftAsync` (still `not_ready`), payment provider and every frontend path
remain closed. No live storefront/StartPayment exercise, no final-checkout certification.
PRESERVED: 231/232 migrations WRITTEN/sharedNOTAPPLIED, stale EF snapshot gap retained, no baseline edit;
`CheckoutEnabled`/preview/selection checked in OFF; the user's visible review window, live Host/FE, previewON/count7,
runtime data, cart, address book, draft, rates, week, pointer and assignments untouched (never enumerated/read/
reloaded/closed). Retained unfixed: 230-R1 empty-key refresh/retry + in-flight new-key wait, cross-owner label
map/decoder limits, Offer API proof gaps, 227 mobile empty-note residual, 225-R1 lost unsaved user LOCAL edits.
TB185 paused/default-OFF/PendingAllocation.
NEXT (Architect): issue a bounded slice for AddressBook saved-address authority (real actor, real country code),
true concurrent advance, and coupon read/selection/commit parity — or accept this as the submit-integration proof.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.


# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Active TB-SHIPPING-CAPTURED-SUBMIT-INTEGRATION-233-R2. Read233-R1/ARCHITECT-REVIEW.md and233-R2/TASK.md.
Root source-reviewedce7356217: no positive actual submit. NOT accepted as wiring-only. Confirmed DI cycle:
CheckoutDirectory -> capturedComposer ->217reader factory ->sameDirectory. Build != container resolution.
Facts groupGUID keys vs planMethodCode keys make positive impossible; same binding also duplicates plan keys.
ComposeFromFacts checks facts against THEMSELVES instead of command. R2 reader-free SAME composition core,
stable group key on facts+plan, actual command validation, actual ProcessManager/provider two-group test.
Trust gaps explicitly open: evidence.Owner/currenttenant, assignment identity/seller, duplicateOffer-line
dictionary, active catalog, savedAddress ownership/current contents, storedcart version, quote refs, coherentgates.
Acceptance still outside TransactionScope with no root revision retention: result claim corrected. R2 fixes same
real acceptance; no new placeholder/parser-only finish, no second pricing/calendar engine.
231/232 accepted scopes retained. Public Commit/PaymentDraft CLOSED; no frontendHMR/runtime/HostFE/livewrites/
sharedDDL. USER VISIBLE review window USER unsavedwork: no enumerate/read/automate/reload/close, previewONcount7.
231/232 migrations sharedNOTAPPLIED/staleEFsnapshotgap retained, checked-in gatesOFF. No checkoutactivation/
fullJSONperorder/historyrepricing/VisualACCEPT. Known230emptykey/inflight/crossownerlabelsdecoder/OfferAPI,
227mobile,225-R1lostLOCALwork retained. TB185paused/defaultOFF/PendingAllocation. STOP_FOR_ARCHITECT afterR2,
Automatic-Next-Task NONE. History retained below.

# HISTORICAL WORKER RESULT — 233-R1 EXECUTED PARTIAL: bypass closed + real acceptance wired; positive submit NOT proven — 2026-10-27


Read docs/architecture/evidence/TB-SHIPPING-CAPTURED-FINAL-QUOTE-ACCEPTANCE-233-R1/{TASK.md,RESULT.md}.
Base `388ca996f`. Status: **EXECUTED — PARTIAL**, NOT accepted, no user Visual ACCEPT, no live/HTTP/browser run,
no frontend/StartPayment/payment-provider activation, no shared migration/schema change.
LANDED (0 build errors, focused 36/36): the direct-caller bypass is closed — the acceptance ALWAYS reads the
own-Order persisted draft marker (`AsNoTracking` + groups + lines) and treats stored-captured OR caller evidence
OR nonblank revision intent as the captured route (gate OFF ⇒ `storefront.captured_preview.not_ready`; caller
evidence/intent with no stored capture ⇒ the same refusal); genuine legacy still returns the exact caller plans
with zero FOREIGN calls. `TryParseSelectionRevision` now controls acceptance (strict digits, `"0"` lawful);
nonblank coupon ⇒ `coupon_unavailable`; stale root revision ⇒ `conflict`; all before the existing
reserve/persist/convert boundary. 217 membership/lines/group-facts were extracted ONCE into
`ShippingGroupGoodsFactsBuilder` and are shared by preview and the final acceptance over the SAME submit's
already-quoted `SellerOrder` lines (via `_orderLineIdByCartLineId` only) — no requote, no second calendar/rate
engine; `ComposeFromFactsAsync` is the typed facts-input composer entry sharing ValidatePlan/ValidateFacts/
candidate/calendar with `ComposeAsync` and calling no 217/Preview/Pricing/Promotion/Tax. Saved-choice
revalidation uses existing authorities: complete stored partition, real cart version, SingleStore Store owner ==
current trusted tenant, one batch Offer assignment read (Platform + assigned type == stored type), batched active
types, ONE 219 per distinct owner+binding with no latest fallback, and an exact comparison of every stored 231
scalar (refs/area/row/type/currency/money/date/100ns ticks/timezone/min+max) with the current quote plus the
customer's own chosen date; destination resolves through `IGeographyLookup.ResolveLegacyAsync` against the
persisted priced destination scalars. Manual charge = payable, carrier collect separate.

NOT DELIVERED (PROMINENT): the REQUIRED POSITIVE real-submit fact was NOT executed — no end-to-end
`CheckoutProcessManager.SubmitAsync` run exists, because the acceptance depends on the quote-derived
cart-line→order-line map and the available harnesses either stop before the submit boundary or throw on the first
reserve/convert call; building that fixture is what HARD40 did not allow. Also unexecuted: stale/changed
token-cart-partition-address-pointer-type-row-money-date-hours matrix, coupon captured refusal, preview-vs-final
goods change, order-write/cart-convert rollback of compact evidence, idempotent replay after pointer change
(source fails closed, no relational fact run). Public Commit/PaymentDraft stay `not_ready`; `CheckoutEnabled`
stays checked-in false. 231/232 migrations sharedNOTAPPLIED, stale EF snapshot retained. Point-in-time pointer
observation only — no distributed lock/serializability claim. Elapsed ≈75 min vs HARD40 — NONCOMPLIANT.
BOUNDS: no frontend/HMR/browser, no Host/FE restart/rebuild, no live write, no schema/migration/baseline edit,
USER VISIBLE review window untouched, local previewON/count7 unchanged. Known 230 empty-key/in-flight,
cross-owner label/decoder, Offer API proof, 227 mobile empty-note, 225-R1 lost LOCAL edits remain. TB185
paused/default-OFF/PendingAllocation. NEXT (Architect): issue a fixture-only task for the real
SubmitAsync positive, or accept this as wiring-only. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

# SUPERSEDED — 233 PARTIAL, NOT ACCEPTED; issued233-R1 actual final-quote acceptance —2026-10-27

Active TB-SHIPPING-CAPTURED-FINAL-QUOTE-ACCEPTANCE-233-R1. Read233/ARCHITECT-REVIEW.md and233-R1/TASK.md.
Root source-reviewed1c42a43f7: additive final gate/text token/parser/real submit hook retained as scaffolding ONLY.
ON hook still unconditionally not_ready; no actual final facts/revalidation/persistence. Reported28facts not
positive submit/provider/HTTP proof. GateOFF+caller no evidence skips stored capture check: direct-caller bypass.
R1 finishes SAME actual CheckoutDirectory/ProcessManager hook, uses already-quoted final SellerOrder goods via
shared217facts/218composition, current server address/Offer/type/pointer/quote/calendar vs exact231evidence,
Order-root revision at established atomic boundary, REAL persisted two-seller same-binding positive proof.
Own-Order marker read allowed evenOFF to reject omitted-evidence downgrade; zero FOREIGN calls genuinelegacy.
Public Storefront Commit/PaymentDraft stay CLOSED inR1; public hookup later AFTER core acceptance review.
No new placeholder-only finish, no new price/calendarengine, no frontend/HMR/HostFE/runtime/livewrites/sharedDDL.
USER VISIBLE review window is USER unsavedwork: no enumerate/read/automate/reload/close; localpreviewONcount7.
231/232 acceptedwithinrecordedscopes, migrationssharedNOTAPPLIED/EFsnapshotgap retained; all gatesdefaultOFF.
No fullauthoredJSON/order/historyrepricing/paymentactivation/fullcheckout/VisualACCEPT claim. Known230emptykey/
inflight/crossownerlabeldecoder/OfferAPI,227mobile,225-R1lostLOCALwork retained. TB185paused/defaultOFF/
PendingAllocation. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. History below.

# HISTORICAL WORKER RESULT — 233 executed PARTIAL: final captured gate + revision token + submit-seam hook; revalidation NOT delivered — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-CAPTURED-ORDER-COMMIT-PARITY-233/{TASK.md,RESULT.md}. Base
`1bcc3c41a` fast-forward. Status: **EXECUTED — PARTIAL**, NOT accepted, no user Visual ACCEPT, no checkout
activation, no live/HTTP/browser run. ≈70 min vs HARD45 — NONCOMPLIANT. LANDED: `CheckoutEnabled` (default OFF)
in `StorefrontCapturedPreviewOptions`; `ExpectedSelectionRevision` carried as TEXT from
`StorefrontStartPaymentBody`→`StorefrontStartPaymentRequest`→`CommitAsync`→`SubmitAsync`→`BuildCommand`→
`SubmitCheckoutCommand`, parsed only by `CheckoutDirectory.TryParseSelectionRevision` (digits only; `"0"` lawful;
`4294967296` preserved); `ICheckoutSubmitHost.AcceptCapturedShippingAsync` called by `CheckoutProcessManager`
after the quote and BEFORE TransactionScope/reserve/persist/convert, its RESULT feeding `PersistCheckoutAsync`.
Gate OFF returns the same plans with zero extra read; evidence is refused `storefront.captured_preview.not_ready`
before any read; every captured passage refuses the same code because revalidation does not exist — the gate
never opens a captured write. NOT DELIVERED: saved-address/Geography destination comparison, cart/version/line
membership, 228 owner/type/seller + active binding, 219 snapshot/revision/selection comparison, same 216 quote +
213 calendar + money/date scalars, final-`SellerOrder`-derived goods facts (typed input into 217/218, no recursive
Preview/Submit), compact evidence persistence in the checkout atomic boundary, PaymentDraft readiness, and every
requested execution fact (positive two-seller groups, refusal matrix, changed-quote proof, forged direct caller,
order-write/cart-convert rollback, relational 15433 run). Gate-ON branch UNPROVEN. Builds 0 errors; focused
28/28 (gate facts + strict revision parses + unchanged 232 write guard + unchanged materializer). No frontend/
HMR/browser/Host-FE rebuild, no live write, no schema change, no shared migration apply, 231/232 sharedNOTAPPLIED,
stale EF snapshot retained, user review window untouched, previewON/count7 unchanged, new gates OFF. Known 230-R1
empty-key retry/in-flight, cross-owner label/decoder, Offer API proof, 227 mobile empty-note, 225-R1 lost LOCAL
edits remain. TB185 paused/default-OFF/PendingAllocation. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

# SUPERSEDED — 232/R1/R2 captured draft selection ACCEPTED; issued233 final Order parity — 2026-10-27

Active: TB-SHIPPING-CAPTURED-ORDER-COMMIT-PARITY-233. Review232-R2/ARCHITECT-REVIEW.md,233/TASK.md in evidence.
Root reviewed98c1c4ee0 actual adapter/tests. Accepted WITHIN SCOPE: per-group commitments, fresh clean preflight,
explicit root CAS, compact231 evidence, coupon refusal, atomic replacement and owned transaction cleanup.
Reported31/31+builds are WORKER execution, not root execution. Scripted rollback fault is not real outage;
EF boundary cancellation not OS abort; diagnostic counts bounded by fixture window; sequentialCAS not race.
232 has NO live/HTTP/frontend writer proof. 231/232 migrations sharedNOTAPPLIED; EFsnapshot gap retained.
233 connects actual CheckoutProcessManager final submit to fresh cart/assignment/type/pointer/address/quote/date
validation, using ACTUAL final SellerOrder quoted goods (no second inconsistent preview) before reservation;
persist same compact evidence on existing Order groups in established atomic boundary. Actual StartPayment
path must forward explicit captured selection revision. New final gate defaultOFF; coupon unsupported.
No UI/runtime enabling/payment provider/history endpoint in233. USER VISIBLE review window is USER unsaved work:
no enumerate/read/automate/reload/close; NO frontendHMR/HostFErefresh/live write. LocalpreviewON/count7 unchanged.
Current final Commit/PaymentDraft remain not_ready until reviewed defaultOFF capability; no checkoutcertification.
No full authoredJSON/order/historyrepricing; compact sharedcapture invariant locked. TB185paused/defaultOFF/
PendingAllocation. Known230emptykey/inflight/cross-ownerlabelsdecoder/OfferAPI,227mobile,225-R1lostLOCALwork
remain. STOP_FOR_ARCHITECT after233; Automatic-Next-Task NONE. History retained below.

# HISTORICAL WORKER RESULT — 232-R2 executed: owned transactions are disposed once, primary failure never masked — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-CAPTURED-SELECTION-TRANSACTION-CLEANUP-232-R2/{TASK.md,RESULT.md} and
232-R1/ARCHITECT-REVIEW.md. Base `92e1d9a03` (fast-forward; the three pre-existing Shipping line-ending drifts
never staged). Status: **EXECUTED**, NOT accepted, NO user Visual ACCEPT, no live/HTTP writer, no frontend/Host/
runtime change, no shared migration apply. R1's accepted group correlation/preflight/CAS/coupon/ordinary rollback
were NOT reopened; this task repaired ONE adapter lifetime/failure contract.

A — every transaction THIS adapter opens is now owned and consumed exactly once: one awaited `finally` on success,
rollback-then-dispose on failure, handle nulled immediately so `finally` cannot double-dispose, begin-failure
disposes nothing, and a secondary `DisposeAsync` failure can never change the primary result. A caller-owned
transaction (`Database.CurrentTransaction` non-null) is refused BEFORE any mutation with the existing
`storefront.captured_selection.pending_work` code — never nested, adopted, rolled back or disposed. Rollback always
uses `CancellationToken.None`; only "already completed" and real rollback failures are treated as UNCONFIRMED and
never re-thrown. Previous groups+lines and attempted groups+lines are captured before the first mutation and
detached exactly, AFTER the rollback attempt, so cleanup runs even when rollback throws. The rolled-back root is
re-read only when rollback was CONFIRMED and only after our transaction is disposed; unconfirmed rollback or a
failing recovery read leaves the attempt detached with NO fabricated graph and preserves the primary exception.
Conflict mapping stays narrow (concurrency + existing exact root-PK uniqueness); unexpected provider errors become
neither success nor conflict.

B — proof on ONE task-owned disposable `postgres:17-alpine` 15433/order_tb231/tb231 (identity before DDL, normal
migrations, container removed, port verified closed), using real EF seams only: test-only `ISaveChangesInterceptor`
/ `DbCommandInterceptor` / `IDbTransactionInterceptor` plus EF's own DiagnosticSource `...Database.Transaction.*`
events (EF has NO disposal interceptor, and a cleared `CurrentTransaction` is explicitly not accepted as proof).
New class `StorefrontCapturedSelectionTransactionTests` 5/5: create AND replacement each emit exactly ONE
`TransactionDisposed` and the same context can start/commit its own next explicit transaction; caller CTS
cancelled INSIDE the second `SaveChangesAsync` ⇒ thrown `OperationCanceledException` carries the caller token,
rollback ran with a non-cancelled token, owned transaction disposed, no pending attempt, unrelated draft
`Unchanged`, DB still revision 1; a real duplicate-line provider failure plus an injected `TransactionRollingBack`
fault ⇒ the SAME primary instance propagates, no recovery read (rollback unconfirmed), attempt still detached
(0 pending), transaction disposed, unrelated graph untouched; recovery-SELECT failure ⇒ primary instance preserved;
open caller transaction ⇒ refused before mutation, still current, not disposed, still commits, stored revision 1.
RED→GREEN measured: on the reviewed R1 adapter the lifetime fact fails `Expected 1, Actual 0` — the leak is real.
Existing 232 storage+writer+guard classes ran ONCE with the new class: 31/31. Order.Infrastructure/Tests build 0
errors 0 warnings. No 285/310/312/full Storefront/Shipping/Host/solution/guard sweep.

HONEST: the rollback fault is scripted (a `TransactionRollingBack` exception), not a real database outage, and is
distinguished from "already closed"; in that fixture the following `DisposeAsync` performs the implicit rollback,
so the DB ends old while the ADAPTER treats recovery as unconfirmed and fabricates nothing; an unconfirmed rollback
has no restored in-memory graph (caller must re-read) — recorded limitation, not "recovered context"; cancellation
is proven at the `SaveChangesAsync` boundary via a test interceptor, not an OS-level abort. Writer stays
model-bound (never HTTP/live), `DestinationServiceAreaId` NULL, sequential CAS not a simultaneous race, 231/232
migrations WRITTEN/sharedNOTAPPLIED, stale EF snapshot gap retained, Commit/ProjectPaymentDraft stay not_ready with
SelectionEnabled checked-in false. Retained unfixed: 230-R1 empty-key retry/in-flight, cross-owner label/decoder,
Offer API proof, 227 mobile empty-note, 225-R1 lost unsaved LOCAL edits. The user's visible review window, live
Host/FE, previewON/count7, runtime data, buyer/cart, draft/rates/week/pointer/assignments were NOT touched. Elapsed
≈62 min vs HARD20 — recorded NONCOMPLIANT, not a process PASS. No amend/force-push.
NEXT (Architect): accept/reject this cleanup proof, then commit-time destination/cart/pointer/quote revalidation and
coupon read-write-final parity, then final order composition / Order-only historical read. STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. TB185 paused/default-OFF/PendingAllocation preserved.
# SUPERSEDED — 232-R2 issued; 232-R1 data repairs accepted within scope — 2026-10-27

Active: TB-SHIPPING-CAPTURED-SELECTION-TRANSACTION-CLEANUP-232-R2.
Root source-reviewed5c38f9761; review232-R1/ARCHITECT-REVIEW.md and task232-R2/TASK.md in evidence.
ACCEPTED WITHIN SCOPE: per-group seller/binding/membership evidence, fresh detached captured preflight,
pending-work refusal, explicit CAS intent, mixed legacy refusal, nonblank coupon unavailable, ordinary rollback.
Overall232 writer NOT ACCEPTED: both transaction-allocating methods lack disposal; rollback/recovery secondary
errors can skip cleanup and mask primary error. R2 fixes ONLY this adapter lifetime/failure contract.
R1 cancellation test pre-cancels before call; NOT write-boundary cancellation proof. R2 real staged cancellation
and scripted rollback-failure proof required; no generic audit/new engine/233.
NO frontend/HMR/runtime/Host/FE/live write/shared migration apply. USER VISIBLE review window is USER unsaved
work: no enumerate/read/automate/reload/close. Local previewON/count7 unchanged; checked-in gatesOFF.
231/232 migrations WRITTEN/sharedNOTAPPLIED; staleEFsnapshot gap retained. Commit/PaymentDraft not_ready even
allflagsOFF for captured evidence; no final checkout/payment/history endpoint or activation claim.
Compact231 accepted; no full JSON/order/historical repricing. Known230emptykey/inflight/cross-ownerdecoderlabels/
OfferAPI gaps,227mobile residual,225-R1 lost LOCAL edits unrecovered. TB185paused/defaultOFF/PendingAllocation.
STOP_FOR_ARCHITECT after boundedR2; Automatic-Next-Task NONE. History preserved below.

# HISTORICAL WORKER RESULT — 232-R1 executed: per-group evidence + attempt-only rollback proven — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232-R1/{TASK.md,RESULT.md} and
232/ARCHITECT-REVIEW.md. Base `71b0e71d4` (local `520c508d7` fast-forwarded; the three pre-existing Shipping
line-ending drifts never staged). Status: **EXECUTED**, NOT accepted, NO user Visual ACCEPT, no live/HTTP writer,
no frontend/Host/runtime change, no shared migration apply.

A — evidence is GROUP-scoped, not method-scoped: each server group yields one `CapturedCommitment` (server
`GroupKey`, authoritative seller of that group's own cart lines, method/service/option, exact `LineIds`, evidence
from that group's own quote/slot) and `AttachEvidenceToOneGroupEach` requires exactly one match per group and one
group per commitment by FULL obligation; zero/second/leftover ⇒ whole write refused. Two sellers on the SAME Store
binding keep their own row/date/money (same-price control green).
B — clean caller + fresh graph: `GetForCapturedSelectionAsync` refuses unrelated Added/Modified/Deleted work with
`storefront.captured_selection.pending_work` BEFORE any query/transaction/mutation and preserves it, detaches only
that cart's tracked drafts and returns the persisted authoritative root/children AsNoTracking. The adapter re-reads
the tracked root, derives CAS and reusable groups from it, and cheap intent validation (keys, legacy shorthand,
captured+`ShippingGroups` mix, explicit revision text, coupon) runs before any foreign quote read.
`ExpectedSelectionRevision` must be explicit: blank/omitted/malformed/negative ⇒ intent_invalid, `"0"` remains
lawful absence.
C — rollback is attempt-only: non-cancelled transaction rollback that cannot mask the original
exception/cancellation, detach of ONLY this attempt's and this draft's previous groups/lines/root (no context
clear, no global line sweep) and a re-read of the rolled-back root, so Current AND Original values are the
pre-attempt persisted row and nothing rejected can be published later. The earlier hand-restore was PROVEN to
corrupt the tracker (previous group re-attached as Added ⇒ duplicate-key identity conflict); detach-and-reload
replaced it.
D — coupon fails closed: nonblank `CouponCode` refuses `storefront.captured_selection.coupon_unavailable` before
any quote/write (blank = absent); the planner's coupon passthrough was removed, so no path can quote a discount the
read never showed. Couponed captured checkout stays unsupported until read+selection+final checkout share one
context.
Proof focused only on the task-owned disposable `postgres:17-alpine` 15433/order_tb231 (identity before DDL,
container removed, port verified closed): captured-selection classes 26/26 (two-seller same-binding writer fact red
on the reviewed version then green, own-money control, coupon/write-count facts, stale-context exact token, stale
token conflict, full-scalar between-stages failure with originals/HasChanges0/SaveChanges0 and a later lawful edit
publishing nothing rejected, unrelated pending work refused and preserved, caller cancellation). Affected
Storefront+Checkout run ONCE 285/286 with the single failure being the PRE-EXISTING packaging allow-list guard
(red at base). Order projects build 0 errors.
HONEST: writer still model-bound (never exercised over HTTP/live), `DestinationServiceAreaId` always NULL, no
commit-time revalidation, sequential concurrency fixtures are not a simultaneous race, 231/232 migrations
WRITTEN/sharedNOTAPPLIED, EF snapshot gap retained, CommitAsync/ProjectPaymentDraftAsync stay not_ready with
SelectionEnabled checked-in false. One test-hygiene change: the two relational classes now share one non-parallel
xUnit collection because their whole-table resets wiped each other. Retained unfixed: 230-R1 empty-key
retry/in-flight, cross-owner label/decoder, Offer API proof, 227 mobile empty-note, 225-R1 lost unsaved LOCAL edits.
The user's visible review window, live Host/FE, previewON/count7, runtime data, buyer/cart, draft/rates/week/
pointer/assignments were NOT touched. Elapsed ≈29 min vs HARD35 with one in-task correction; no amend/force-push.
NEXT (Architect): review, then commit-time destination/cart/pointer/quote revalidation and coupon read-write-final
parity, then final order composition / Order-only historical read. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
TB185 paused/default-OFF/PendingAllocation preserved.

# SUPERSEDED — 232 NOT ACCEPTED; issued232-R1 group isolation + failure atomicity — 2026-10-27

Active: TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232-R1.
Read 232/ARCHITECT-REVIEW.md and 232-R1/TASK.md under docs/architecture/evidence/.
Root reviewed actual d2307889f / evidence520c508d7. Shared resolver/compact231 direction retained; writer NOT accepted.
Confirmed: binding-only evidence dictionary overwrites across sellers; captured load uses stale tracking graph;
no clean-caller guard; writer mutates before adapter/BEGIN; rollback originals accepted rejected scalars;
global non-keep line detach touches unrelated drafts; cancelled cleanup can mask failure.
R1 fixes same writer with full per-group correlation, fresh clean preflight, explicit CAS text, entire owned
graph rollback incl cancellation/BEGIN failure and unrelated tracker preservation. Captured nonblank coupon
explicitly refused until read/write/final money context matches; no silent discount/price replacement.
No233/final checkout/history endpoint. Commit/PaymentDraft remain not_ready even flagsOFF for captured drafts.
NO frontend/HMR/Host/FE/runtime change, no live DB/API/business write; user VISIBLE review window is USER
unsaved work: never enumerate/read/automate/reload/close. Local previewON/count7 unchanged; checked-in gatesOFF.
231/232 migrations WRITTEN/sharedNOTAPPLIED; stale EF snapshot remains disclosed, no scaffolding claim.
Worker amended/force-with-lease own232commit against task rule: recorded violation; R1 no history rewrite.
Read-only user trial remains accepted, no user Visual ACCEPT/full Storefront certification. Compact evidence
shared snapshot refs only, no full authoredJSON/order, no historical repricing. Known emptykey refresh/inflight,
cross-owner labels/decoder/OfferAPI proof,227mobile residual,225-R1 lost LOCAL edits retained. TB185 paused,
default-OFF/PendingAllocation. Automatic-Next-Task NONE; STOP_FOR_ARCHITECT after R1. History below.

# HISTORICAL WORKER RESULT — 232 executed: server-revalidated captured selection is written atomically — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232/{TASK.md,RESULT.md}.
Base `54b01303b`. Status: **EXECUTED**, NOT accepted, NO user Visual ACCEPT. No checkout/reservation/payment/
cart conversion, no historical read, no UI, no runtime refresh, no shared migration apply.

A — ONE resolution: `StorefrontCapturedPreviewPlanner.ResolveAsync` is now the single body (current cart/
access/version → one 228 assignment batch → real active types/bindings → one 219 pointer read per distinct
full owner+binding → ONE 218 compose with the one 217 whole-cart goods read and one 216 quote per candidate).
The read projection (`BuildAsync`) and the new writer consume the SAME typed context; the optional
`CouponCode` intent of the selection request goes through that same 217 path (trimmed; blank = absent).

B — intent is only untrusted keys: complete server `GroupKey`, catalog binding, ISO date and the expected
snapshot/source-revision/selection-version/area-group/rate-row echo tokens plus `ExpectedSelectionRevision`.
Every value is re-derived server-side; mismatches refuse with stable `storefront.captured_selection.*`
codes (disabled/intent_invalid/unavailable/date_unavailable/conflict/destination_unverified/
evidence_present) and FA+EN resources. All-or-none: every server group must be selected, so unknown/
unassigned/unquoted groups block the whole write and stay visible in the read. Captured+legacy mixture is
refused; option OFF refuses instead of falling back. Only `Store` owner is writable on SingleStore;
Marketplace/Seller refuse `unavailable` (read unchanged, nothing guessed).

C — 231 evidence is created from that same quote/slot (owner, snapshot/draft/source revision/format/
selection version, matched area+row, optional real type, freight/packaging/payable/collect exactly as
quoted, chosen date and slot ticks/timezone) and attached to the real `CartShippingDraftGroup` built by the
existing planner; legacy `ShippingAmount` keeps its payable-at-checkout meaning, carrier collect lives only
in evidence.

D — destination identity: the EXISTING `PrepareShippingAsync` server-bound address is resolved with
`IGeographyLookup.ResolveLegacyAsync`; the verified ids persist as four scalars on the existing draft root
(`selection_revision bigint NOT NULL DEFAULT 0` + 4 nullable uuid) in the ONE additive migration
`20261027180000_AddCapturedSelectionRevisionAndDestination` with
`ck_cart_shipping_drafts_captured_selection_shape`. `ReplaceCapturedSelectionAsync` is the ONE named atomic
store member: one Order-owned transaction, delete+insert inside it, `SelectionRevision` as a real EF
concurrency token (monotonic, expected 0 only for absence), exact-root-uniqueness ⇒ conflict, stale token ⇒
409 with no auto retry, and rollback that restores/detaches ONLY this attempt's graph (no context clear, no
leak into later SaveChanges). A captured draft can never be downgraded by the legacy path (`evidence_present`).

E — final order/payment stay closed: `CommitAsync` AND `ProjectPaymentDraftAsync` refuse
`storefront.captured_preview.not_ready` for a stored captured draft even with all options OFF, and
`StorefrontStartPaymentComposer` reaches final order only through `CommitAsync`. No captured checkout/
activation claim.

Proof (focused only, isolated disposable `postgres:17-alpine` 15433/order_tb231, identity asserted before
DDL): 24/24 — 13 writer facts (incl. positive single AND two-group evidence saves, echo/date/pointer/
address refusals with zero writes, Marketplace refusal, coupon reaching the 217 promotion path), 5 real
relational facts (first save persists revision+destination+evidence atomically; second replacement
increments and replaces the complete graph; zero expected revision conflicts; concurrent advance conflicts
with the old selection intact; no duplicate lines/mixed graph; and a forced BETWEEN-stage failure that
rolls back, restores the revision and leaves no Added graph for a later SaveChanges), plus 5 Commit/
projection guard facts. Order.Tests and Host build 0 errors. Storefront+Shipping suite 310/312 with TWO pre-existing red guards at base
(architecture `DateTimeOffset.UtcNow` in the 229/231 planner and the packaging-boundary allow-list) — NOT
repaired here, disclosed in RESULT.md.

HONEST: HTTP reachability of the writer via the existing `PUT /v1/storefront/shipping/selection` is
model-bound only — never exercised live. `DestinationServiceAreaId` is always NULL (no trusted service-area
name is ever supplied) and no commit-time destination revalidation exists yet. The captured READ has no
coupon field, so a couponed selection can differ from the read's discounted goods facts. Insert-stage-only
failure injection is not separately proven (same transaction/rollback path). EF snapshot still omits the
draft table (pre-existing stale baseline). Migrations 231 and 232 remain WRITTEN/shared NOT APPLIED.
Retained unfixed: 230-R1 empty-key retry/in-flight wait, cross-owner label/decoder, Offer API proof, 227
mobile empty-note, 225-R1 lost unsaved user LOCAL edits. Host/FE/previewONcount7/data/cart/draft/pointer/
assignments and the user's visible review window were NOT touched. Elapsed ≈54 min vs HARD40 — recorded
NONCOMPLIANT, not a process PASS.
NEXT (Architect): the decision on closing the coupon asymmetry + commit-time destination/evidence
revalidation, then final order composition / historical read. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
TB185 paused/default-OFF/PendingAllocation preserved.

# SUPERSEDED —231/R1/R2 compact evidence ACCEPTED; issued232 real selection writer —2026-10-27

Active: TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232. Save captured shipping DRAFT choices through actual
existing selection service, freshly revalidated cart/Offer/type/pointer/quote/address/date,231evidence and
ONE atomic Order replacement+CAS. No new unusedcomposer/quoteengine, no full checkout.
Review: docs/architecture/evidence/TB-SHIPPING-COMPACT-EVIDENCE-NULLSAFE-CHECKS-231-R2/ARCHITECT-REVIEW.md.
Task: docs/architecture/evidence/TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232/TASK.md.
Root read231-R2actualSQL/directtests; corrected basicCHECKs accepted within scope. WorkerRED4/10GREEN10/10
not rootexecution. Compact22scalarfields/fullowner/longtokens/exactticks/manualmoney accepted;numericcoercion,
btrim(space-only), staleEFsnapshot/future-scaffolding limitations remain.
Migration23120261027150000 WRITTEN/sharedNOTAPPLIED.232 mayaddminimal rootselectionCAS/destination fields,
isolatedproof only. SelectionEnabled defaultOFF; Commit/StartPayment not_ready persists, no runtimeenablement.
Current legacygroupreplacement has TWOcommits; captured cannotreuse it asatomic. Actual finalpayment calls
StartPaymentComposer->ShippingService.CommitAsync; futureparity must coverthis path.
DedicatedvisibleUSERreviewwindow/Host/FE/previewONcount7/data alluntouched;232 nofrontendHMR/runtimechanges.
Read-only usertrial accepted, no VisualACCEPT/fullStorefront certification. NoJSONperorder/historyrepricing.
Known emptykeyrefresh/inflightwait/crossownerlabel/decoder/OfferAPIproof gaps retained;227mobileresidual;
225-R1lostunsaveduserLOCALtabedits,no recovery. TB185paused/defaultOFF/PendingAllocation. History below.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

231-R1 value/owner/token/time/money contract remains accepted within its recorded proof; the SQL shape CHECK was
the only open blocker and is repaired in place in the SAME still-unshared migration.
Optional delivery days now require BOTHNULL or BOTH explicitly NOTNULL (half pair is FALSE, not UNKNOWN),
Store tenant must be non-blank/already-trimmed/never lowercased, required + optional-present UUIDs must be
non-empty and currency must be `^[A-Z]{3}$` — on BOTH cart and order group tables. Same22columns/model/Domain.
Direct Postgres proof on ONE task-owned `15433/order_tb231`: RED4/10 on the reviewed CHECK, DB recreated empty,
GREEN10/10, every refusal=SQLSTATE23514+exact constraint name with the row intact; container removed, port
closed. Migration20261027150000 still WRITTEN/sharedNOTAPPLIED, no shared history rewrite, no writer/parity.
Read: docs/architecture/evidence/TB-SHIPPING-COMPACT-EVIDENCE-NULLSAFE-CHECKS-231-R2/RESULT.md.
Next (Architect): trusted server composer (selected revision + cart/version + Offer/type/quote/date) writing the
compact evidence inside the existing atomic boundary, then the Order-storage-only historical read.
Selection/commit stays `storefront.captured_preview.not_ready`; no fullJSONperorder, no historicalrepricing.
Review: docs/architecture/evidence/TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1/ARCHITECT-REVIEW.md.
Task: docs/architecture/evidence/TB-SHIPPING-COMPACT-EVIDENCE-NULLSAFE-CHECKS-231-R2/TASK.md.
Migration20261027150000 is WRITTEN/sharedNOTAPPLIED; correct this unsharedunacceptedmigration only,
never sharedhistory. Broader staleEFsnapshot remains disclosed, scopedruntimeModelDB proof accepted withinlimit.
Still no livewriter, selection/commitnot_ready, compacthistoricalreader/parityunimplemented.
Userread-onlytrial accepted; dedicatedvisibleREVIEWwindow is user'sunsavedwork: no automation/reload/close.
Host/FE/localpreviewONcount7/data/account/cart/drafts/pointers/assignments unchanged by231lineage.
Known emptykeyUIrefresh/cross-ownerlabel/decoder/OfferAPI gaps retained;227mobileresidual;225-R1unsavedLOCAL
user-tab loss,no recovery. TB185paused/defaultOFF/PendingAllocation preserved. History below retained.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1/{RESULT.md,TASK.md}.
Base `5cd72434e`. Status: **EXECUTED**, NOT accepted, no user Visual ACCEPT, no live writer/parity/UI/runtime.
The five blocking 231 review facts are repaired inside the SAME unshared value/mapping/migration.

A — `SourceRevision`/`SelectionVersion` are `long` (bigint) end-to-end; `3_000_000_000` and `4_294_967_296`
persisted and read back exactly through BOTH draft and confirmed group (and from raw column text). Format stays1;
non-positive tokens still refuse. B — optional responsibility replaced by required Order-owned
`ShippingGroupOwner` Store(tenant)/Marketplace/Seller(sellerId): all-or-none, tenant Ordinal (never lowercased),
Seller must equal the group's selling party, Store/Marketplace never rewritten as that seller, no inferred owner.
C — accepted `ReceivingDate` + opening/closing bounds as bigint TimeOnly ticks (0..TicksPerDay-1, 7-digit
fraction proven exactly, no time/timestamp truncation) + method timezone; same-day positive interval; group's
chosen date required and `ApplySchedule` refuses a contradicting schedule BEFORE mutation; delivery days stay
optional metadata, never mapped onto endpoints/LeadDays. D — amounts refused unless exactly numeric(18,2) and
`freight+packaging == payable+collect` (Manual slice only); explicit0/max/split lawful; nothing recomputed; the
historical-replacement fixture is now a coherent `157,000` quote. E — migration231 corrected IN PLACE:
22 plain (no IFNOTEXISTS) columns on BOTH existing tables plus two CHECK constraints each (all-or-none + shape),
still WRITTEN/sharedNOTAPPLIED; EF snapshot left stale by decision, runtime model proven equal to migrated
columns/types/nullability/indexes for the two touched tables.
Proof only: 50/50 focused (11 domain + 5 provider + 12 target-isolation + directly affected materializer/planner
facts) on the task-owned disposable 15433/order_tb231/tb231, container removed and port verified closed; Order
Domain/Infrastructure/Tests build 0 errors.
HONEST: no production composer writes evidence yet (real groups stay NULL; `not_ready` still guards
SaveSelection/Commit), no historical reader exists, `ApplySchedule` has no production caller (proved via
InternalsVisibleTo), snapshot baseline gap retained. Retained unfixed: 230-R1 empty-key retry gap, cross-owner
label map/decoder, Offer API proof gaps, 227 mobile empty-note residual, 225-R1 lost unsaved user LOCAL edits.
Live Host/FE/previewONcount7 and the user's visible review window + runtime data untouched. Elapsed ≈16 min vs
HARD35 — still NOT a process PASS.
NEXT (Architect): trusted server composer writing this evidence inside the existing atomic boundary, then the
Order-storage-only historical read; captured checkout stays not_ready. STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. TB185 paused/default-OFF/PendingAllocation preserved.
# SUPERSEDED — Architect did NOT accept231 contract; issued231-R1 —2026-10-27

Active: TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1. Fix existing evidence: long(bigint) source/selection
tokens, FULL owner key, actual chosen-date TimeOnly interval (exactticks, not two invented DateOnly endpoints),
financial representability/conservation and coherent group schedule/amount transfer. No live writer/parity.
Review: docs/architecture/evidence/TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231/ARCHITECT-REVIEW.md.
Task: docs/architecture/evidence/TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1/TASK.md.
231 provider facts useful but incomplete/invalid-pricefixture; do not call contract/history ready.
231 migration20261027150000_AddShippingGroupAcceptedEvidence is WRITTEN/sharedNOTAPPLIED; correction of this
unaccepted/unshared migration explicitly allowed, no earlier/shared migrations modified. EF snapshot already
lacks group entities; no wholesale baseline regeneration or claim of future scaffolding safety.
Dedicated VISIBLE task-profile Chrome review window handed to USER on /fa/shipping; NEVER automate/reload it.
230/R1 read-only user trial accepted within stated limits, no userVisualACCEPT. LiveHost/FE/previewONcount7
and user's pending local input unchanged by231/R1. Current captured selection/commit not_ready stays.
No authoredJSON perorder, one sharedcapture+constant compact group facts. Fullowner/date/money fidelity
must be accepted before connecting writer. No historicalrepricing. 228migration alreadyAPPLIED.
Known empty-keyrefresh/cross-ownerlabel/decoder/OfferAPI gaps retained;227mobile residual;225-R1 lostunsaved
userLOCALtabedits,no recovery. TB185paused/defaultOFF/PendingAllocation preserved. Prior history below.

# SUPERSEDED — 231 executed PARTIAL (was CURRENT before 231-R1) — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231/{RESULT.md,RECEIPTS.md,TASK.md}.
Base `5a3070567`. Status: **EXECUTED — PARTIAL**, not accepted, no capture checkout, no user Visual ACCEPT.
230-R1 stays accepted FOR READ-ONLY user retest only.

A — ONE cohesive Order-owned value `ShippingGroupAcceptedEvidence` (Order.Domain/Checkout): shared snapshot
reference (SnapshotId, DraftId, SourceRevision, FormatVersion=1 only, SelectionVersion), matched Shipping
AreaGroupId/RateRowId, optional ShippingTypeId, optional existing `OfferFulfillmentResponsibility` (unknown stays
null, never inferred), accepted Freight/Packaging/Currency/PayableAtCheckout/CarrierCollect as exact decimals,
accepted ReceivingStart/End + TimeZone + optional Min/Max delivery days. All-or-none: null (legacy) or complete;
`Create` refuses empty ids, nonpositive revision/token/version, unsupported format, undefined enum, negative
money, non-3-letter currency, inverted interval, empty timezone, half/inverted days — all BEFORE mutation, with
stable `shipping.group.evidence.*` faults. No id/timezone/owner invented, no amount clamped or recomputed.
Wiring is the EXISTING path: `CartShippingDraftGroup.AcceptedEvidence` + `ReplaceAcceptedEvidence` (replaces only
that group, refuses currency/payable mismatch), trailing optional `OrderShippingGroupPlan.AcceptedEvidence`,
`OrderShippingGroup.Create` stores it privately (no public mutation), and `StorefrontShippingService
.BuildOrderGroupPlans` carries it. No Shipping.Domain/Infrastructure reference, no HTTP/DTO accepting evidence
from a browser, no Payment/Settlement payer change, no new quote authority.

B — storage: ONE additive migration `20261027150000_AddShippingGroupAcceptedEvidence` adds the SAME 19 nullable
`accepted_*` columns to the EXISTING `order.cart_shipping_groups` and `order.order_shipping_groups`, plus ONE
small `ix_order_shipping_groups_accepted_snapshot_id`. No cross-module FK, no snapshot copy, no history
subsystem, no backfill (legacy rows stay NULL). Measured: 19 scalar columns per group row, zero json/jsonb,
constant size per GROUP not per line. Owner/method/charge/currency/dates/lead-days are REUSED from existing
columns, not duplicated. Migration is WRITTEN and NOT applied to shared `tooba_alpha`/store-alpha; it exists only
on the task-owned disposable DB. `OrderDbContextModelSnapshot.cs` is byte-identical (the tool re-scaffolded the
pre-existing stale baseline, so that generated migration was deleted; hand-written additive SQL follows the
20261016 precedent). No existing migration edited, no baseline regenerated.

C — proof only on the task-owned disposable PostgreSQL `127.0.0.1:15433/order_tb231/tb231` (identity verified
before DDL, normal Order migration chain, `TOOBA_TB231_ORDER_POSTGRES` fails closed). 21/21 focused facts: two
groups on ONE SnapshotId keep distinct area/row/money/date through materialization AND persist/readback with
unchanged group/line membership; a later draft replacement (new snapshot/amount/selection version) never
rewrites the stored order's old snapshot reference, money, receiving start or selection version (read uses Order
storage only); legacy groups persist NULL evidence; 15 invalid/partial + incoherent cases refuse with no partial
mutation; storage shape = 19 nullable accepted_* columns, 0 json/jsonb, one snapshot index; replacing/clearing one
group leaves the sibling untouched. Unchanged materializer class re-run 9/9. Order.Infrastructure, Order.Tests
and Host build 0 errors. No whole-suite/guard wildcard loop.

D — FIRST USER HANDOFF: local preview runtime left untouched (Host 5088 PID 24660, FE 3000 PID 28288, previewON/
count7, no rebuild/restart/migration). ONE VISIBLE installed-Chrome window opened with the EXISTING task-owned
`D:\temp\tb229r2-chrome` profile on `http://127.0.0.1:3000/fa/shipping` (PID 34788) after stopping ONLY that
profile's own headless browser (PID 33524/port 9336); normal logged-in session/cart, no password/token in message
or git, no destination/address/cart/rate changed. That is the dedicated REVIEW window: not automated, reloaded or
tab-enumerated afterwards. All 231 proof used the isolated DB, never that window or the live runtime.

HONEST: no production writer composes evidence yet, so every real draft/order group stores NULL and
SaveSelection/Commit still refuse with `storefront.captured_preview.not_ready` (unchanged). No historical reader
endpoint exists; "no mutable Shipping draft needed" is a storage fact, not an implemented read. Retained and NOT
fixed: 230-R1 empty-key destination failure/retry gap + in-flight new-key wait, cross-owner label map/decoder
id-coercion limits, Offer API proof gaps, 227 mobile empty-note residual, unrecovered 225-R1 user LOCAL tab edit
loss. SingleStore trial does not certify Marketplace. Elapsed ≈16 min vs HARD35 with one correction round — still
NOT a process PASS. NEXT (Architect): trusted server composer (selected revision + cart/version + Offer/type/
quote/date revalidation) writing this evidence inside the existing atomic boundary, then the Order-storage-only
historical read; captured checkout stays not_ready until then. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
TB185 paused/default-OFF/PendingAllocation preserved.

# SUPERSEDED — 231 issued; 230-R1 accepted READ user trial — 2026-10-27

Active: TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231. Add compact evidence to EXISTING cart/order shipping
groups/materializer; shared capture reference+accepted money/date, no full authored JSON perorder/line.
No captured checkout enablement. not_ready guard stays;231 migration isolated test only, not sharedapply.
230-R1 accepted for demonstrated populated-destination failure/retry, IRR integer display and intervalbidi.
Root read source and VIEWED390fail/retryimages. Read-only user trial /fa/shipping is now allowed; no VisualACCEPT.
231 first may open dedicated VISIBLE task-profile review window with existing normal buyer/cart; after handoff
never automate/reload it. Never user browser/tab. No credentials committed or printed.
Review: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-READ-FAILURE-INTEGRITY-230-R1/ARCHITECT-REVIEW.md.
Task: docs/architecture/evidence/TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231/TASK.md.
Known empty-key no-destination failure sentinel means retry not yet covered/accepted; in-flight newkeys wait.
Earlier cross-ownerlabel/decoder/OfferAPIproof gaps still named, no Marketplace/fullcheckout certification.
DEVdraft01a17d0d-e033-7000-af43-49a1daa28822/post:standard/heavy/snapshot01a17d0e-0419-7000-b575-f507d2f89abc,
source2/selection1; buyer01a17cf7-e38d-7000-82a5-00c57929bc54/cart01a17cfd-c1b1-7000-b10d-8cf21258a1cd.
Host5088/FE3000 retained localpreviewON/count7. Keep runtime/data unchanged while user reviews.
228 migrationAPPLIED; no replay. User post/NULL draft/rates/pointer untouched perworker.
Compact history/parity still unimplemented BEFORE231; do not preclaim task result. Next after review:
same authoritative quote+selected revision revalidation through actual selection/commit, then final order proof.
225-R2/226 accepted;227empty-note residual;225-R1 unsaveduserLOCALtabloss,no recovery.
TB185paused/default-OFF/PendingAllocation preserved. Historical checkpoints below retained.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Active: TB-SHIPPING-STOREFRONT-READ-FAILURE-INTEGRITY-230-R1. Root VIEWED actual1440/390 priced cards.
Configured SingleStore read positive accepted: real group freight120000+packaging5000, actual dates skipFriday,
second unassigned group survives. This is NOT selection/commit/checkout or user Visual ACCEPT.
Root found current refresh exception clears pending and re-exposes previous destination quote; same key
already marked requested then skipped.230-R1 fixes only current failed/retry identity state plus integerIRR
display(all-zero fraction only) and bidi-safe interval. No another backend/auth/setup/calculator loop.
Review: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-DESTINATION-CONFIGURED-TRIAL-230/ARCHITECT-REVIEW.md.
Task: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-READ-FAILURE-INTEGRITY-230-R1/TASK.md.
DEV draft01a17d0d-e033-7000-af43-49a1daa28822 post:standard/heavy; snapshot01a17d0e-0419-7000-b575-f507d2f89abc,
sourceRevision2/selectionVersion1; Offer0442a1fa-1d95-4b24-b9d9-129fc19942d2 Platformheavy.
Normal trial buyer01a17cf7-e38d-7000-82a5-00c57929bc54/cart01a17cfd-c1b1-7000-b10d-8cf21258a1cd(v11historical).
Host5088 retainedtb230build/PID24660historical,FE3000/localpreviewONcount7; reverify not restart needlessly.
User post/NULL draft untouched.230 evidence publication-write chronology contradictory, do not certify a
new write solely from prose; actual selected snapshot read quoted. No re-publish for proof.
228 migrationAPPLIED. Shared immutable capture/compact order-group evidence/nohistoricalrepricing remain
mandatory/unimplemented. not_ready guard stays; no parity task before narrow repair review.
Existing cross-ownerlabel/decoder andOfferAPIproof gaps retained, no Marketplace certification.
225-R2/226 accepted;227empty-note residual;225-R1 lost unsaveduserLOCALedits,no recovery.
Task-owned browser only;TB185paused/default-OFF/PendingAllocation preserved. Historical checkpoints below.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-READ-FAILURE-INTEGRITY-230-R1/{RESULT.md,TASK.md}.
Base `6d0a506b2`. Status: **EXECUTED — one real failed-refresh defect repaired + two display fixes**, not
accepted, no user Visual ACCEPT.

A — the captured destination coordinator no longer relies on a loading boolean. One explicit identity state
(`confirmedKey` = the destination the current successful preview actually describes, `requestedKey`,
`failedKey`, `inFlight`) lives in `storefront-shipping-destination-refresh.ts`
(`beginDestinationRefresh`, `completeDestinationRefresh`, `failDestinationRefresh`,
`canRetryDestinationRefresh`, `resolveDestinationPreviewMode`, `planDestinationRefresh`), and
`storefront-shipping.tsx` drives it through ONE ref+state pair while the existing `refreshProjection` takes
`options.destinationKey`. The old `appliedDestinationKey` ref and `capturedDestinationPending` boolean are
deleted. A tracked request is only "confirmed" by its own keyed success; a stale success OR a stale error
returns the snapshot unchanged, so it cannot mutate the current projection/error/pending state. When the
current identity differs from the confirmed one, or its refresh failed, the card hides money and dates and
shows one compact reason (`destination_pending` / `destination_failed`) plus ONE explicit
«تلاش دوباره» retry button, gated on the synchronous snapshot so a double click sends ONE request and nothing
auto-retries. Typed recipient/address is never re-hydrated (`preserveTypedAddress:true`). Legacy OFF still
short-circuits to `skip` and its card path is unchanged.

B — display only: IRR all-zero fractions display as the integer (`۱۲۰,۰۰۰ ریال`, `۰ ریال`) while the raw token
is untouched; a non-zero fraction is preserved exactly (never rounded/truncated); other currencies keep their
decimal text. The captured interval text is wrapped in `<bdi dir="ltr">` so `09:00–17:00` reads correctly in
RTL; stored hours untouched.

C — proof: 22/22 focused facts driving the SAME coordinator the page uses; ONE clean `tsc`. Mounted at 390 on
the task-owned profile (FE 3000 PID 28288 / Host 5088 untouched, no rebuild/restart/migration): real priced
card `کرایه ۱۲۰,۰۰۰ ریال`; REAL Chrome offline emulation + province change ⇒ no priced card, no old price, no
dates, retry visible; network restored ⇒ two same-tick clicks produced exactly ONE real projection POST with
the exact new destination ids and the priced card returned with `intervalBdiDir ltr`,
`scrollWidth == clientWidth == 390`, card bounds 59/786 in 844.
HONEST: offline blocks before `requestWillBeSent`, so the failure is proven by the resulting state; while
pending/failed only the compact notice renders (product lines/typed inputs preserved and returned with the
confirmed answer); one width mounted. No draft/rate/week/pointer/assignment/cart/auth write, no
selection/save/commit. Elapsed ≈10 min vs HARD20 with one in-flight in-module correction. Compact
per-order-group immutable order evidence and server-selected revision revalidation across selection/commit
remain mandatory and unimplemented. TB185 paused/default-OFF/PendingAllocation preserved; Storefront is NOT
relabelled as delivered.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

## SUPERSEDED — 230 EXECUTED: captured destination refresh + ONE real configured priced read — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-DESTINATION-CONFIGURED-TRIAL-230/{RESULT.md,TASK.md}.
Base `281c21d88`. Status: **EXECUTED — positive configured read reached live**, NOT accepted, no user Visual
ACCEPT, selection/commit still blocked.

A — the captured destination really never refreshed. `storefront-shipping.tsx` had no reaction to a newly
VERIFIED geography destination (only the hidden legacy method path ever sent ids), and the header still
promised legacy prep+lead. Now three pure primitives own it
(`storefront-shipping-destination-refresh.ts`: `destinationIdentityKey`, `planDestinationRefresh`,
`createRequestGeneration`), the page calls the EXISTING `refreshProjection` with the exact verified
destination and `rehydrateDraft:false` (typed recipient/address is never overwritten), every response is
gated by the request generation so a superseded/out-of-order answer cannot win, the last applied key is a
stable identity ref (no render loop), clearing a parent clears the child ids, and while a new destination is
pending the cards show `storefront.captured_preview.destination_pending`. Legacy OFF short-circuits to `skip`
forever — unchanged. Captured mode now shows `shippingDaysLabel = "—"`, never the legacy delivery promise.
TWO real defects blocked the live price and are fixed in the same path: the planner quoted goods with
`cart.Market` instead of the storefront's own `StorefrontCheckoutService.StorefrontTaxJurisdiction`
(`IR-NAT`) — every configured read was 409 `checkout.tax.unavailable`; and `storefront-geography-api.ts`
sent the literal `?languageId=null` which the server binds to `Guid?` and rejects with 500, so no verified
identity could exist.

B — explicit DEV test data through normal APIs only: DEV draft `01a17d0d-e033-7000-af43-49a1daa28822`
«آزمایش ارسال۲۳۰» on the unused real binding `post:standard`, type `heavy`
`01a1785e-01f3-7000-b50d-d6372e756f89`, Manual+Prepaid, IRR, ONE real Country ایران destination
`e3451206-e503-51a8-91ad-afa9ab70e330`, ONE unconditional row freight `120000.00` / packaging `5000.00` /
receiving `2..3` days, week Asia/Tehran Sat–Thu 09:00–17:00 with Friday closed, published to ONE shared
snapshot `01a17d0e-0419-7000-b575-f507d2f89abc` (`selectionVersion 1`, revision 2). Offer
`0442a1fa-1d95-4b24-b9d9-129fc19942d2` assigned `Platform`+`heavy` through the existing admin fulfillment
API; offer `1b56d013-5817-4db1-99a3-010e12dc0631` stays unassigned. The existing user draft
`01a16eeb-...` was NOT touched (bounds/rates/week/type/pointer untouched).

C — real page `http://127.0.0.1:3000/fa/shipping` in the task-owned Chrome (CDP 9336, buyer
`01a17cf7-e38d-7000-82a5-00c57929bc54`, cart `01a17cfd-c1b1-7000-b10d-8cf21258a1cd` v11, FE 3000 PID 28288
untouched, Host 5088 PID 24660 `D:\temp\tb230-build\out`). Choosing province/city through the existing UI
selects (no method selector) sent the destination POSTs and 200 returned a PRICED group:
`cap|platform|store|...|heavy`, type سنگین, real goods `9485771` after discount+tax, freight `120000.00`,
packaging `5000.00`, payable `125000.00`, IRR, earliest `2026-10-29`, dates 10-29,10-31,11-01..11-05
(Friday 10-30 skipped by the authored week, min 2, no extra seller prep), snapshot
`01a17d0e-0419-7000-b575-f507d2f89abc`, while the second group stayed visible and refused
(`storefront.captured_preview.unassigned`). Clicking the 10-31 chip moved selection to that chip and showed
that date's own interval; reload kept the authored data and the normal session. Card scrolled to
`block:'center'` (top 59 / bottom 786 in 844, intersects), `scrollWidth == 390 == clientWidth`, legacy
duplicate cards `0`. Screenshots 1440×900 and 390×844 under the 230 evidence `screens/` inspected by eye.
Proof: 15/15 focused storefront tests (4 new destination-refresh facts), ONE clean `tsc`, Order.Application
build 0 errors. No migration, no seed, no reset, no user-draft/pointer change, no select/save/commit/order.

HONEST: a uniform authored week means a per-date interval DIFFERENCE is not live-provable (each date carries
its own interval in the JSON; independence was unit-proven in 229-R1). No BEFORE artifact file for the offer
assignment was kept. Legacy OFF was proven by test/control, not an OFF screenshot. Three corrections were
needed where the task allowed one. Actual ≈23 min vs HARD30 wall-clock — still NOT a process PASS.
Selection/commit parity, compact per-order-group immutable evidence and no-historical-repricing remain
unimplemented. TB185 paused/default-OFF/PendingAllocation preserved; checked-in preview default stays OFF.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

## SUPERSEDED — reviewed229-R2 auth/unavailable only; issued230 configured read — 2026-10-27

Active: TB-SHIPPING-STOREFRONT-DESTINATION-CONFIGURED-TRIAL-230. Reuse R2 normal buyer/profile/cart/driver.
Root VIEWED1440/390 screenshots: hero/address only, group cards out of viewport. No card Visual ACCEPT.
R2 closes normal local buyer auth +200destination-gated unassigned read, NOT configured shipping quote.
Root source found missing captured destination-change refresh (only hidden legacy method path sends ids);
legacy3days prep+lead header still displayed.230 fixes these two narrow UI wiring issues and proves ONE
task-owned configured manual DEV method through real normal APIs, priced group + independent refusal,
screenshots scrolled to actual cards. No another auth/driver/audit loop.
Review: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-AUTHENTICATED-READ-TRIAL-229-R2/ARCHITECT-REVIEW.md.
Task: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-DESTINATION-CONFIGURED-TRIAL-230/TASK.md.
Local previewON/count7 perR2; actual HostPID36952/pathretainedR1,FE28288 are historic,reverify.
Buyer01a17cf7-e38d-7000-82a5-00c57929bc54/cart01a17cfd-c1b1-7000-b10d-8cf21258a1cd/taskprofiletb229r2-chrome.
One NEW clearly named DEV draft/capture on UNUSED real binding plus bounded taskOffer assignment explicitly
authorized in230 if needed. NEVER alter user's post/NULL draft/rates/week/pointer or fabricate weight.
228 migration alreadyAPPLIED; no replay/reset/reseed. R1 money/binding fixes accepted within proof.
Selection/commit NOTready guard remains. Compact shared snapshot/order-group evidence/parity unimplemented.
Known cross-owner label-map/decoder limits retained; SingleStore test not Marketplace certification.
Fresh/task-owned browser only;225-R1 lost unsaved userLOCALedits,no recovery.227 empty-note residual retained.
TB185paused/default-OFF/PendingAllocation preserved. Prior checkpoints historical.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-AUTHENTICATED-READ-TRIAL-229-R2/{RESULT.md,TASK.md}.
Base `e7bc126f4c44499e113fdb357854006666eb2efb`. No source change needed; NOT accepted, no user Visual ACCEPT.
Login/commerce question CLOSED: the R1 "Commerce resolution failed" warning was the SAME exception as the 401 —
`TenantResolutionMiddleware` catches every `SemanticException` from `_next`, so the identity gate's
`checkout.authentication_required` (`ActorId=(null)`) was re-logged there while `TenantId=store-alpha` and
`StoreId=store-alpha` were already resolved. Authentication was the ONLY blocker; no middleware/guard weakened.
Real normal session: PUBLIC `POST /v1/auth/register` (Email kind, synthetic `@example.invalid`, random password,
id `01a17cf7-e38d-7000-82a5-00c57929bc54`) then the EXISTING BFF `/api/auth/csrf` + `POST /api/auth/login` from
the app origin; `/api/auth/me` returned that account on SingleStore store-alpha. No bypass/dev-actor/SQL/roles.
Live: Host PID 36952 `D:\temp\tb229r1-build\out` preview ON (`AvailableDateCount=7`), FE 3000 PID 28288 unchanged;
real offers merged into the account (cart v6 two lines → v9 one line); `POST /v1/storefront/shipping/projection`
returned **200** with `capturedPreview.enabled=true` and real per-offer groups (honest `destination_required`,
goods amount absent never 0). ONE groups surface, `legacyMethodCards=0`, 2 captured cards (multi-line) and 1
(single-line), product line + seller + type + «مبلغ کالا نامشخص» + short reason each, read-only notice visible,
`scrollWidth==innerWidth`/`overflowX=false` at 1440 and 390. PNGs `screens/229r2-{1440,390}-{2line,1line}.png`.
Not claimed: live positive two-group financial quote, selection/commit/order/payment, `not_ready` exercised,
activation/price/eligibility/readiness, Marketplace cross-owner. R1 limits carried (identifier String-coercion,
ISO-shape not calendar validity, label map keyed service/option not owner). Compact order-group evidence and the
parity slice remain mandatory/unimplemented. Local Host deliberately LEFT ON preview for the Architect/user
visual test (read-only, blocked from checkout); checked-in Enabled still false; no migration/reset/reseed; user
draft/rates/pointer untouched; trial password only in local `%TEMP%\tb229r2-acct.txt`, never committed.
≈105 min vs HARD20 = NONCOMPLIANT, not a process PASS. NEXT (Architect): review, then parity (selected captured
revision + SaveSelection/Commit) + compact shared-snapshot order evidence. STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. TB185 paused/default-OFF/PendingAllocation preserved; 227 residual retained.

# SUPERSEDED — R1 repairs accepted for live verification; issued229-R2 — 2026-10-27

Active: TB-SHIPPING-STOREFRONT-AUTHENTICATED-READ-TRIAL-229-R2. Runtime-only gap: normal task-owned buyer
registration+existingBFFlogin, verify commerce context, observe actual captured cards1440/390. HARD20min.
Root source confirms POST/v1/auth/register plus existingCSRF-protected/api/auth/login with normal cookies;
no user credentials needed initially. Visible OTP-only login is not the only existing auth API.
Four229-R1 repairs accepted within tested scope; NOT live storefront/VisualACCEPT. Worker18backend+11frontend
andtypecheck reported; root source review only. R1 stopped atlogin/401 plus commerce-resolution log;
both causes require correlation, not an assumption that planner/auth alone caused the failure.
Review: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229-R1/ARCHITECT-REVIEW.md.
Task: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-AUTHENTICATED-READ-TRIAL-229-R2/TASK.md.
Runtime preview restoredOFF perR1; reverify actual process/environment/settings instead of historicPIDs.
228 assignmentmigration sharedAPPLIED. No migration replay. AssignmentAPI negatives still not provider-proven.
Known scoped limits: decoder identifier String-coercion/calendar-shape only; cross-owner label map currently
binding-only (not Marketplace display certified). No repeated generic audit or unrelated repairs.
Selection/commit remains blocked; compact order evidence/parity mandatory/unimplemented. No price eligibility
or readiness claim. Existing money/week/admin acceptance and227 mobileempty-note residual retained.
Task-owned browser only;225-R1 unsaveduser-local-tab loss,no recovery. TB185paused/defaultOFF/PendingAllocation.
Historical checkpoints retained below.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229-R1/{RESULT.md,TASK.md}.
Base `304ac4da8059e91673cd54306e0930b9bfd94378`. Not accepted; NO live card observation and NO screenshots.
Repaired in one path: exact decimal WIRE money (invalid never 0/another amount, explicit 0 lawful, unknown stays
null, missing/foreign currency untrusted, no IRR fallback, malformed/duplicate group·method·date untrusted);
method identity = stable serviceCode+optionCode binding with the label only for display (two methods sharing a
label both survive, case variants one 219 read); the parallel captured component DELETED and the preview drawn
inside the EXISTING StorefrontShippingGroupCards for single-line AND multi-line carts with the legacy duplicate
row suppressed; explicit validated AvailableDateCount 0..90 required when ON (never DeliveryHorizonDays, default
OFF), unknown responsibility refused, contradictory rows fail closed, known refusals surface a precise Refused
view, dependency errors and cancellation propagate, SaveSelection/Commit refuse first on
`storefront.captured_preview.not_ready` when ON and keep the legacy path when OFF.
Proof focused only: 18/18 real tests (15 planner incl. positive composition through the REAL 218 composer + REAL
217 CheckoutDirectory harness — real goods 99.00/198.00, two groups independent, third unassigned group visible,
label-sharing bindings both surviving, one pointer read for case variants, dependency failure propagating;
3 write-guard facts on the actual service methods), 11/11 frontend decoder facts
(`node --experimental-strip-types --test`), one clean `tsc --noEmit`, Order test build 0 errors.
REAL TRIAL BLOCKED: current source built isolated and served on `127.0.0.1:5088` with the option ON + explicit
date count, but storefront shipping needs an authenticated buyer — the page redirects to
`/fa/login?returnTo=/fa/shipping` and `POST /v1/storefront/shipping/projection` returned 401
`checkout.authentication_required`. No desktop/390 captured-card evidence exists; single-card/no-duplicate/
no-overflow is source- and test-proven only. Original Host restored (PID 27588, `D:\temp\tb221-build\out`,
preview OFF, `/health` 200); no write/migration/user tab.
Not claimed: acceptance, user visual ACCEPT, live quote/cards, parity selection+commit, compact order evidence,
price/activation/readiness. ≈39 min vs HARD35 = NONCOMPLIANT, not a process PASS.
NEXT (Architect): live authenticated-buyer desktop+390 observation (task-scoped session or credentials), then the
parity slice replacing the not_ready refusal; compact order-group evidence still mandatory/unimplemented.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. TB185 paused/default-OFF/PendingAllocation preserved; 227 residual
recorded; 225-R1 lost unsaved user-tab local edits remain unrecovered.

# SUPERSEDED — Architect REJECTED229 read delivery; issued229-R1 — 2026-10-27

Active: TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229-R1. New read-path SOURCE exists but is NOT accepted
or observed live. Root confirmed money invalid->0 bug (isolated JS reproduction), label used as method
identity, duplicate legacy/new cards and missing single-line UI, unknown goods shown0, silent legacy horizon
reuse, unknown responsibility default, and empty-on-composition-error. Correct these in same path then
execute positive real-composer financial case and actual desktop/mobile page trial; no next parity task yet.
Read229 ARCHITECT-REVIEW.md and229-R1 TASK.md under their evidence folders.
228 Offer assignment migration20261027120000_AddOfferFulfillmentAssignment is shared-APPLIED per229 runner
evidence. Do not replay. Offer API negative/relational proof is still incomplete; no CAS claim.
229 tests9/9 refusal/planning only; no positive218/217 composition, no refreshed Host/FE or live screenshots.
Checked-in captured preview defaultOFF, Save/Commit source refusal exists; runtime remains old until refresh.
229-R1 must make preview one existing UI presentation for BOTH single/multi-line, no invented parallel cards.
Unknown monetary values nullable/exact text; no0/IRR/legacy fallback. Default legacy branch stays whenOFF.
Compact shared snapshot/order evidence and selection/commit parity remain mandatory/unimplemented.
225-R2/226 accepted;227 accepted with recorded empty-branch mobile residual. No admin replay.
Fresh task-owned Chrome only;225-R1 lost unsaved user-tab LOCAL edits,no recovery. Real draft/pointer intact.
TB185paused/default-OFF/PendingAllocation preserved. Historical checkpoints below retained.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229/{RESULT.md,TASK.md}.
Base `02250f55f`. Status EXECUTED — PARTIAL, not accepted, NO live browser trial.
`StorefrontShippingService.ProjectAsync` now: real cart → ONE Offer assignment batch → explicit partition
(responsibility + real assigned type + real offer seller) → catalog bindings → ONE 219 read per distinct
owner+binding → ONE 218 composition for the whole partition. Additive nullable `CapturedPreview` on the
projection; `null` when OFF (legacy response unchanged). FE renders it inside the EXISTING group cards.
Option `Tooba:StorefrontCapturedPreview:Enabled` default OFF = zero Offer/Fulfillment/Shipping calls.
With the option ON, `SaveSelectionAsync`/`CommitAsync` throw `storefront.captured_preview.not_ready` FIRST.
Migration `20261027120000_AddOfferFulfillmentAssignment` **APPLIED** on tooba_alpha/store-alpha (plan showed one
pending Offer module; apply result=ok, pendingCount=0); legacy columns stay NULL.
Proof: two Order projects build 0 errors; new `StorefrontCapturedPreviewPlannerTests` 9/9; FE tsc clean.
NOT done: live URL/page/screenshots, positive composition run, Offer admin API integration class.
Elapsed ≈70 min vs HARD45 ⇒ NONCOMPLIANT, not a process PASS.
NEXT: live ON-option Storefront GET + screenshots in a task-owned browser, then compact-order-evidence +
selected-capture commit parity. TB185 paused/default-OFF/PendingAllocation.

## SUPERSEDED — Architect reviewed228 PARTIAL; issued229 — 2026-10-27

Active: TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229. Finish228 B/C: real cart+batch Offer assignments
->219 exact selected revision ->ONE218 composer ->EXISTING Storefront group/date cards. No new design.
228 A/domain stored nullable responsibility+type accepted as integration basis only; endpoint/provider proof
pending in229. Migration20261027120000_AddOfferFulfillmentAssignment WRITTEN/NOTAPPLIED as of228.
228 delivered NO user-facing Storefront connection or live trial; do not call it completed.
229 requires real storefront URL/screenshots and explicit SaveSelection/Commit refusal with default-OFF
read-preview branch. No customer checkout readiness, activation or order certification implied.
Review: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228/ARCHITECT-REVIEW.md.
Task: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229/TASK.md.
Two-family policy resolved: Seller vs Platform(Store/Marketplace); each group independent, unknown physical
facts null, assigned DB type not inferred. Existing admin endpoint is not a CAS/versioned writer.
Compact shared snapshot and compact order evidence/no historical repricing remain required/unimplemented.
User nonproduction migration/verified refresh/bounded dev API authorization persists; no draft/pointer changes.
225-R2/226 accepted;227 accepted with recorded mobile empty-note residual; no replay.
Fresh task-owned browser only;225-R1 unsaved local-user-tab loss recorded,no recovery.
TB185paused/default-OFF/PendingAllocation preserved. Historical checkpoints below retained.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228/{RESULT.md,TASK.md}.
Base `d292e88b6`. Status EXECUTED — PARTIAL, not accepted; no live trial, no migration applied, no restart.

A is real: `SellerOffer.FulfillmentResponsibility` (Seller|Platform, nullable) +
`FulfillmentShippingTypeId` (nullable, uuid) with one guarded `AssignFulfillment` (no inference, no archived,
no type-without-responsibility, no empty id); additive nullable migration
`20261027120000_AddOfferFulfillmentAssignment` WRITTEN, NOT APPLIED; batch Contracts read
`IOfferFulfillmentAssignmentLookup` (one query, missing id absent); one admin write route
`PUT /v1/admin/offers/{offerId}/fulfillment` behind the EXISTING panel gate + EXISTING `fulfillment.manage`,
authorize-before-body, real active Fulfillment type validation, no seller route. Codes/resources/catalogue/
manifest updated. Admin folder un-forbidden for Offer.Endpoints with justification; Storefront still forbidden.
B NOT STARTED: no cart→assignments→partition→real-binding→219 read→ONE 218 ComposeAsync plan builder.
C NOT STARTED: existing storefront cards/transport/read opt-in untouched; 218/219/220 remain unconsumed, so no
captured candidate is reachable and the explicit SaveSelection/Commit refusal must land WITH C.
Proof: Offer.Endpoints + Host build 0 errors; 8/8 new focused facts; Offer suite 92/93 (single failure is a
PRE-EXISTING missing ProductWorkspace guard file, unrelated). Elapsed ≈82 min vs HARD40 = NONCOMPLIANT.
Next: apply migration on store-alpha via real runner plan, then B+C together with the captured refusal.
227 accepted for search/populated-branch shake with open mobile empty-note14px residual; no full visual
acceptance. 225-R2/226 accepted within limits. No admin repair replay.
Shared immutable capture/compact order-group evidence/no historical repricing remain mandatory.
User nonproduction authorization persists; required scoped migration/verified Host refresh allowed, no
reset/reseed/user-draft overwrite. Task-owned browser only;225-R1 lost unsaved user-tab edits, no recovery.
TB185paused/default-OFF/PendingAllocation preserved. Previous checkpoints below are historical.

# SUPERSEDED — Architect issued Storefront228 — 2026-10-27

Active: TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228. User clarified TWO responsibility families:
Seller delivery or Platform(Store/Marketplace) shipping services. Each group independent; existing Storefront
cards/controls/DOM/CSS reused. No new visual design or hardcoded supermarket speed.
Architect assignment decision: explicit nullable responsibility + real type on seller-specific Offer,
admin-scoped write, batch Contracts read; absent legacy assignments unavailable, no seller=fulfillment guess.
See docs/architecture/shipping/STOREFRONT-RESPONSIBILITY-DECISION-2026-10-27.md.
228 is IMPLEMENT first real read projection, not audit/helper-only: trusted assignments ->219 selected capture
->218 composer -> existing storefront group cards. Selection/commit of captured path remains explicitly
blocked pending parity/compact-order evidence. Actual live connection is NOT delivered before worker proof.
Task: docs/architecture/evidence/TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228/TASK.md.
HARD40/source25/final15, one correction, report PARTIAL at limit rather than invent authority.
227 accepted for search/populated-branch shake with open mobile empty-note14px residual; no full visual
acceptance. 225-R2/226 accepted within limits. No admin repair replay.
Shared immutable capture/compact order-group evidence/no historical repricing remain mandatory.
User nonproduction authorization persists; required scoped migration/verified Host refresh allowed, no
reset/reseed/user-draft overwrite. Task-owned browser only;225-R1 lost unsaved user-tab edits, no recovery.
TB185paused/default-OFF/PendingAllocation preserved. Previous checkpoints below are historical.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

227 ACCEPTED_WITH_RECORDED_LIMIT for in-popup search and reproduced populated-branch loading shake; admin
user retest allowed, not user Visual ACCEPT/full pixel-stability. Root viewed 1440 and390 popup screenshots
and source diff at a32d99ee47d445f6715df3436ca3a5689f6c114e. Remaining: 52px status min-height grows to66px for longest empty-branch note on
mobile; retain this open limit. Service-area selectable options not live-proven. Worker69/69+typecheck are
worker execution, not Architect execution.
Review: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227/ARCHITECT-REVIEW.md.
Active implementation: NONE. Storefront remains NOT connected. Next user-authorized priority is integration
PER GROUP using existing Storefront UI; source review confirms inspected Cart/Offer contracts carry seller
identity but not explicit fulfillment responsibility/required shipping type. Do not equate seller with
fulfillment owner or selected publication with customer eligibility. Clarify explicit product/offer
responsibility policy before inventing that authority. No228 implementation task issued yet.
Compact shared immutable snapshot/compact per-order-group evidence/no historical repricing stays required
and unimplemented. No full authored JSON per order.
225-R2 money and226 week acceptance retained; no replay. 225-R1 lost unsaved LOCAL user-tab edits: no recovery
claimed; fresh task-owned browser profile/target only. Default-OFF/TB185 paused/PendingAllocation and real
draft/pointer/runtime preserved. Historical checkpoints below remain historical.

# SUPERSEDED — 237-R1 checkpoint was current before 238 —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227/{RESULT.md,TASK.md}.
Base `2d744fc67df34dc9427b2f0db56e2946c1344784`. Status: EXECUTED, NOT accepted. 225-R2 money and 226 week untouched.
The shake was MEASURED on the unchanged source (dialog top 299→271→257 = 42px, height 303→387→359 = 84px
during the country→province read; page and inner scroll 0): conditional rows inside a UA-centred `<dialog>`.
Now the four fields never change identity, every transient line shares ONE reserved status slot
(min-height 3.25rem), and the popup is an out-of-flow, searchable dropdown portalled INSIDE the dialog —
2230 frames at 1440 keep top 273 / height 355; 390 → 171/503; 360 → 139/503; all with ZERO travel.
Search is inside each popup over ONLY the verified branch's loaded rows with an honest `{shown} / {total}`,
Escape closes only the dropdown, country clear still empties+disables descendants, مرند's service area is
honestly `0 از 0`. Limits: no live service-area options exist in this installation (test-proven only); the
longest no-provinces note needs 66px at ≤390 (one recorded +14px empty-branch shift); dead picker file left.
ONE clean tsc, 69/69 affected tests. No money/week/backend/geography change, no Apply/PUT/seed/migration,
no restart, no user tab touched, default-OFF/TB185 paused/PendingAllocation preserved. ≈15 min vs HARD25.
NO user Visual ACCEPT claimed.
NEXT PRIORITY (Architect issues it): concrete Storefront integration PER SHIPPING GROUP using the EXISTING
storefront cards/controls/DOM/CSS — 219 selected immutable revision + 218 composer + real trusted
fulfillment owner/type/membership + existing projection/selection/commit; never browser owner or price,
never seller=fulfillment inference, unknown physical facts stay null. Storefront is currently NOT wired/live.
Compact order history remains mandatory and unimplemented: shared immutable capture, compact per-order-group
reference and final money/receiving facts, no full authored JSON per order, no historical repricing.
Safety continuity: 225-R1 reloaded a user tab and lost unsaved LOCAL edits; no recovery claimed. Browser
proof uses a fresh task-owned profile and target ONLY, never user tabs.
Task: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227/TASK.md.
Prior checkpoints below are historical.

## SUPERSEDED — Architect issued 227 — 2026-10-27

Active: TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227. User reports destination selection shake/jump and requests search INSIDE all four dropdown popups. Reproduce/measure cause, reuse existing design-system combobox, preserve DB cascade/current-generation evidence and modal Add semantics. Task is bounded 25 minutes; no money/week/backend reopening.
225-R2 money integrity and 226 mobile week presentation remain accepted within recorded proof limits; no user Visual ACCEPT is implied.
NEXT USER-AUTHORIZED PRIORITY after 227 review: concrete Storefront connection PER SHIPPING GROUP with existing Storefront cards/controls/DOM/CSS. No invented design or generic audit/calculator loop. Selected immutable revision + trusted fulfillment membership/owner/type + existing composer and projection/selection/commit are required. Storefront is currently NOT wired/live.
Preserve compact shared-snapshot order evidence decision; do not copy authored JSON per order or reprice historical orders.
Safety continuity: 225-R1 reloaded a user tab and lost unsaved LOCAL edits; no recovery claimed. Future browser proof uses fresh task-owned profile and target ONLY, never user tabs. Preserve real draft, pointer, runtime, default-OFF/TB185 paused/PendingAllocation.
Task: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227/TASK.md.
Prior checkpoints below are historical.

## CURRENT —226 mobile week reviewed; admin form READY FOR USER RETEST —2026-10-26

Architect reviewed source9734e77ac81eacb8d158988bd2f3ca2f65f4cde4 and VIEWED actual native225? NO:
226/screens/226-390-view.png,226-390-edit.png,226-360-view.png,226-1440-view.png.
Mobile closed rows now one line/one Closed status; working view range compact adjacent labels+values;
edit paired controls and checkbox visible; desktop four-column view retained. Accept bounded226 presentation
repair for ADMIN USER RETEST, not user Visual ACCEPT/global UI certification. Root read exact diff: only week
presentation and225R2 test helper type annotation. Root didn't execute Windows typecheck/browser; worker
one clean typecheck and mounted toggle-restoration receipts are evidence. R2's prior typecheck claim predates
its test helper and was incomplete;226 corrected TS2683 with no behavior change.
225-R2 money integrity ACCEPTED; no further money/proof loop. 225 modals/cascade/rate duplicate-edit validation
accepted within documented focused/helper+mounted limits, not full save or live service-area matrix.
CALL USER NOW: http://127.0.0.1:3000/admin/shipping-services ; draft01a16eeb-70b3-7000-87c2-564c82835496
view/edit via actual row. Ask actual user feedback, no next implementation task without it.
Active NONE / Automatic-Next-Task NONE. Preserve user's tabs and edits; prior R1 local tab loss remains
unrecovered, no claim otherwise. New browser only task-owned profile/created target.
Limits: real Tehran has no service areas so fourth-level Add test-only; no new Apply/PUT/save replay in225R2/
226, live stored-fraction/legacy conflicts/multi-area360 not proved. No Storefront integration/activation,
compact per-order-group evidence still unimplemented, seller-admin separate. No blanket completion claim.
226 actual10min inside15; older overruns remain recorded. Applied schema/catalog/capture/defaultOFF preserved.

## CURRENT —225-R2 money ACCEPTED;226 mobile-week-only repair issued —2026-10-26

Architect read afbdadc7beb363393eba8aa6c988beacf827db24 actual money model/field and R2 mount-receipts.
Repeated invalid-token laundering closed, Chrome actual input/caret/0/5million/nonemptyMessageBox/cancel
receipts support bounded acceptance. Root didn't rerun tests/browser. No more money/proof-only loop.
Root VIEWED225 desktop destination modal,390rate modal and390weekview. WEEK STILL VISUALLY INCOMPLETE:
Closed shown twice (status+extra span), working From/To detached/spread. Screenshot large canvas with narrow
content, not actual390PNG. User's mobile complaint not fully closed. No user Visual ACCEPT.
Active TB-SHIPPING-ADMIN-MOBILE-WEEK-VISUAL-226, presentation-only authoring-week-editor, HARD15/source8.
Read TASK and225-R2/ARCHITECT-REVIEW.md. Preserve all225 money/modals/cascade logic; no broad tests.
Await fixed week screenshot then Architect/user trial handoff. No Storefront consumer/compact order evidence
implementation or seller-admin work. R1 unsaved-user-tab loss remains unrecovered; only new task Chrome target.
Automatic-Next-Task NONE; STOP_FOR_ARCHITECT after226.

## CURRENT —226 executed: mobile week is one Closed badge + one coherent hours line, native PNGs —2026-10-26

Read docs/architecture/evidence/TB-SHIPPING-ADMIN-MOBILE-WEEK-VISUAL-226/{RESULT.md,TASK.md}. Base
afbdadc7b, fast-forwarded to 94ea7ed2e (docs-only). 225-R2 money integrity stays ACCEPTED and untouched.
Task status: executed, NOT accepted; user trial still waits for Architect.

Presentation only, in ui/authoring-week-editor.tsx: a CLOSED day no longer emits the status word AND a
second span across the bound columns — narrow VIEW is now `weekday` + ONE Closed badge, one line, no dash,
no bounds (28px rows, measured). A working narrow VIEW day is `weekday` + ONE working status, then ONE
inline range whose labels sit immediately next to their own values (RTL measured `از`/`09:00` adjacent,
`تا`/`17:00` adjacent; EN `From 09:00 – To 17:00`), 47px. Narrow EDIT keeps the stored controls: weekday +
working checkbox on line one, the two equal accessible time inputs on line two (138px at 390, 123px at
360), and a closed day is a single compact line with its checkbox and no extra Closed label;
onDayWorking/onDayTime/model guards and every existing test id are unchanged. Desktop VIEW keeps the
accepted four-column geometry (header and rows both day x=481 w=607, status x=337 w=136, from x=209 w=120,
to x=81 w=120) and now shows `تعطیل` only in the status column with an honest `—` in the bound columns,
which is the per-column truth 210-R1 described. Narrow padding tightened only (`py-1`, `gap-y-0.5`).
No time model/validation, dirty/cancel, area/rate/destination modal, money policy or global CSS touched.

Proof: one clean `tsc --noEmit`; no 188/76/42/full-frontend/backend/solution sweep and no invented CSS tests.
Mounted in a NEW task-owned Chrome (155.0.8059.85, `--headless=new`, port 9334, fresh profile, every page in
a target created by this task), real FE3000 traffic, no writes/interception/restart, real draft week read
as-is (only Monday 09:00–17:00 working; nothing invented). Native PNGs at 390×844 and 360×780 (view +
edit, RTL/fa) plus EN/LTR 390 via the real `/en` prefix and one 1440 desktop shot; dimensions read from the
PNG headers and each file inspected by eye. `scrollWidth == innerWidth`, `overflowX false` at 360/390.
Toggle round-trip on the real working day: closed → 0 time inputs/1 line, reopened → `09:00`/`17:00`
restored, Apply still disabled, no Apply ever pressed.
HONEST: `?locale=en` does not switch this admin locale (prefix/cookie does) — the first EN capture was wrong
and re-done; the `/en` visit wrote `tooba_locale` and briefly overwrote the 390 FA PNGs, which were cleared
and re-captured (shipped FA bytes are the re-captured RTL ones). A pre-existing typecheck break in the
ACCEPTED 225-R2 test helper (`this` in a default parameter, TS2683 — that task's tsc ran before the file
existed) was fixed with a bare annotation, no behaviour change.

Actual elapsed ≈10 min vs HARD15 — box respected in wall-clock terms, still NOT a process PASS.
Not claimed: user Visual ACCEPT, the form as a whole, Storefront wiring, seller-admin entry, Tehran
service-area gap. Active task: NONE. Next: Architect review only. STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. Compact-order-evidence invariant still unimplemented; TB185 paused, default-OFF,
PendingAllocation preserved.

## SUPERSEDED —225-R2 executed: money provenance is field state; proof in an isolated task Chrome —2026-10-26

Read docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R2/{RESULT.md,TASK.md} and the
correction appended to 225-R1/RESULT.md. Base 1d0625218 (clean; the three backend line-ending drifts and no
other file staged). Task status: executed, NOT accepted. 225/R1/R2 are NOT accepted and NO user trial was
called; Architect reviews first.

A — the R1 defect was real: trust was re-inferred from the text, so `12,34` (invalid) + `5` gave the
structurally well-grouped `12,345` and the NEXT key took the trusted branch and accepted `123456`. Now
`MoneyInputProvenance { raw, untrusted }` is real edit state: `interpretMoneyTyping(previous,nextText,caret)`
takes it, every entry point propagates `MoneyEdit.untrusted` (paste/edit/backspace/delete), and
`authoring-money-field.tsx` owns ONE ref keyed by the committed token through a single `commit(edit)` used by
change/Backspace/Delete, dropped the moment the value prop no longer matches (external reseed, other row,
reopened modal). Operator text therefore stays invalid through ANY number of appended keys until the
operator removes/replaces the offending grouping; blur/re-render/other-field change cannot launder it; the
model still receives the raw text and the parent still blocks it. No second sanitizer; rate shape,
self-exclusion, legacy fraction and MessageBox semantics untouched.

RED measured on the unchanged R1 module (probe executed): `12,34`→`12,345`→`123456` ACCEPTED at the second
appended digit. GREEN 8/8 new R2 facts driving the same state transitions (invalid through 5/6/7/8 + blur;
explicit correction; reseed/other-row; middle insert `12,934` caret 4; `5,000,000`; `0`; malformed/negative/
exponent; comma-adjacent Backspace/Delete) + 42/42 across the three 225 classes + 21/21 in 224-R1 money and
209-R2 + ONE clean tsc.

Mounted gate in a FRESH task-owned Chrome (installed Chrome 155, `--headless=new`, dedicated port 9333,
fresh profile, target created by this task: 75081F7DB2547954825D0574BD0D1869; only that profile's processes
stopped, port silent): real keys throughout — `1,234`→insert 9→`12,934` caret 4, `0`, `5,000,000`; pasted
`12,34` invalid then +5/+6/+7 and +8 after another field change stayed `12,34567`/`12,345678` with
aria-invalid, Confirm showed a non-empty localized reason with the modal open (screenshot), Cancel restored
`120,000`/`5,000`, delivery `1–3`, Apply disabled. NO Apply/PUT/publication/catalog/migration/restart; draft
and pointer untouched.
HONEST: the pasted token used the element's real paste event plus CDP Input.insertText (clipboard commands are
denied here), so it is the paste CODE PATH, not an OS clipboard round-trip; typing a comma into a valid value
still cannot produce one (pre-existing grouped-field behaviour, recorded not silently changed).

B — USER TAB INCIDENT (recorded, NOT recovered): R1's mounted detour reloaded an existing user editor tab and
discarded that tab's unsaved local edits. The stored draft was untouched but an unsaved local edit cannot be
rebuilt from the server, so "user tabs untouched" is withdrawn for that step and no recovery is claimed. This
task never read, enumerated or manipulated that tab; proof used the isolated browser above.

Actual elapsed ≈45 min vs HARD20 — recorded, NOT a process PASS.
Not claimed: user Visual ACCEPT, Storefront/checkout consumer, seller-admin entry, Tehran service-area live
gap. Active task: NONE. Next: Architect review only. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
Compact shared-snapshot order-evidence decision still locked/unimplemented; TB185 paused, default-OFF,
PendingAllocation preserved.


## SUPERSEDED —225-R1 not accepted;225-R2 money provenance + isolated Chrome proof issued —2026-10-26

Current main1d06252188a3278127401e804f53b263725c47e1/source2107a63db inspected by Architect.
Stored-fraction current-value validation and editable guard corrected. Still defective TWO-key transition:
invalid12,34 ->append5 raw12,345 validfalse ->append6 reclassified trusted ->accepted123456. Validity/
provenance must persist, not infer trusted from shape. Read225-R2/TASK.md and225-R1/ARCHITECT-REVIEW.md.
Active TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R2. No user trial before Architect acceptance.
R1 mounted real keys did NOT reach field; caret/typing only unit-proven. Fresh installed Chrome task-owned
profile/created target only, no Electron/user tab interaction. INCIDENT: R1 mistakenly reloaded user edit
tab and lost unsaved LOCAL changes; DB unchanged is not preservation/recovery. Disclose truthfully.
Keep225 modals/cascade/mobile/rate validation, no more redesign. Tehran no service areas, live fourth level
not proven. Compact-order-evidence/Storefront consumer still unimplemented; no seller-admin work.
No broad tests/Bridge/known historical script. HARD20/implementation10; STOP_FOR_ARCHITECT afterR2.
## SUPERSEDED —225-R1 executed: money edit provenance + caret mapping + modal tolerance; 225 still awaits review —2026-10-26

Read docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1/{RESULT.md,TASK.md} and
225/ARCHITECT-REVIEW.md. Base84999b4f9 (clean fast-forward; the three backend line-ending drifts untouched).
Task status: executed, NOT accepted. 225 is NOT accepted and NO user trial was called; Architect reviews first.

A — invalid operator text can no longer become different money. `interpretMoneyTyping` is now
(previousRaw,nextText,caret): it reads the field's LAST COMMITTED token, and only when that token was LAWFUL
(dropping separators as display-only) — when it was INVALID, a still-present separator keeps the text verbatim
and invalid, so `12,34` + `5` stays `12,345` invalid instead of the accepted `12345`; only the operator's own
deletion/replacement makes it lawful. The field selects the strict paste reader by `inputType`
(paste/pasteAsQuotation/drop/replacementText) AND by an `onPaste` flag for environments that omit it; a real
key clears a stale flag.
B — the caret is mapped through that same separator removal (`withoutDisplayGrouping` returns the logical
offset of every display offset), so a middle insertion into `1,234` (`1,2934`, caret4) anchors 3 digits and the
re-grouped `12,934` keeps the caret after the typed `9`. New symmetric `interpretMoneyDelete` handles Delete
directly before a separator like `interpretMoneyBackspace` does behind it.
C — the rate modal no longer receives an open-time tolerance list. It receives the EXACT stored tokens
(`StoredRateAmounts`/`storedRateAmounts`) and calls the new `rateConfirmIssue(row,otherRows,currency,stored,
editable)` INSIDE the confirm handler: tolerance is re-derived on the current row, self is excluded by stable
row key, the duplicate/overlap scope is the same area, and a view/busy state refuses silently in the handler
(no dialog, no parent mutation). Stored `100.50` confirms untouched; editing it to `200.75` is refused with the
integer-rial reason. Parent Apply stays the only persistence call and the final validator.

Proof: RED measured on the reviewed module (healed `12345`, caret off by one digit, stale tolerance accepting
`200.75`); GREEN 12/12 new focused facts + 76/76 affected feature tests (225, 225-R1, 224+224-R1 money,
209-R1/R2 with the two truthful seam corrections) + ONE clean tsc. Not run: whole188/backend/guards.
Mounted on FE3000/Host5088 (real traffic): the paste path left `12,34` verbatim with aria-invalid and a
deliberate Confirm showed the localized amountInvalid reason; the stored baseline (row120,000/5,000, delivery
1..3, Apply disabled) was unchanged afterwards — no Apply/PUT/publication/catalog/migration/restart.
HONEST LIMITS: automated real keystrokes did not reach the field in this Electron webview (caret/Delete facts
unit-level only); one earlier mount showed an unexplained EMPTY refusal reason that did not reproduce (all ten
codes are mapped) — recorded, not claimed; the navigation that reloaded the tab hit an existing edit tab and
discarded its unsaved LOCAL edits (no server write). No screenshot matrix, no live stored-fraction record
(model fixture labelled). Not claimed: user Visual ACCEPT, Storefront/checkout consumer, seller-admin entry.
Actual elapsed exceeded HARD20 (repair round + mounted detour) — recorded, NOT a process PASS.
Active task: NONE. Next: Architect review only. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. Compact
shared-snapshot order-evidence decision still locked/unimplemented; TB185 paused, default-OFF,
PendingAllocation preserved.
## SUPERSEDED —225 NOT ACCEPTED;225-R1 narrow money/modal integrity repair issued —2026-10-26

Root independently read225 source2e6aed0951a5e1ef75d5206d77c4df9f433f55b1. Preserve modals/cascade/mobile work.
Blockers: typing strips commas from prior invalid pasted text (12,34 then5 ->12345); caret offset from grouped
text wrongly reused after commas stripped; modal stored-fraction tolerance not revoked when value changes.
Active TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1; read TASK and225/ARCHITECT-REVIEW.md.
User must NOT be called to test before Architect accepts repaired result. Worker PASS188 is not acceptance.
No root runtime/test execution. Existing Tehran service areas absent, so fourth-stage live proof pending;
breadcrumb branch reads not constant-count performance proof. No whole-suite confidence loop.
No Storefront/seller-admin/schema changes in repair. Preserve compact-order-evidence decision, user draft/
capture/tabs/runtime. HARD20/implementation12. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

## SUPERSEDED —225 executed (worker report): modal cascade, modal rate rows, IRR integer UI, mobile week —2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225/RESULT.md` + `TASK.md`.
Active task NONE; next NOT ISSUED; user must NOT be called to test before Architect review.

A — destinations are ONE modal with FOUR fixed labelled cascade selects (کشور → استان → شهر → ناحیه) over the
real Geography reads; descendants start disabled, a changed parent clears dependents and superseded completions
are ignored, and `Add` submits ONLY the deepest verified terminal as ONE `(kind,id)` with a MessageBox duplicate
refusal on the explicit action only (staging is not an addition and does not dirty). Stored chips rebuild their
breadcrumb after reload from bounded set-based ancestor reads (`model/destination-trail-resolver.ts`,
`ui/use-stored-trails.ts`), never per-chip N+1, never a fabricated segment; a country selection is not shown as
`ایران › ایران`. HONEST GAP observed live: the real `تهران` city has NO service areas, so the modal says
«برای این شهر ناحیهای ثبت نشده است.» — the service-area Add path is test-proven only, no seed/no schema change.
B — rate rows are compact summaries with Edit/Delete and an isolated modal draft; Confirm validates against the
other rows of the SAME area (AND axes, half-open, touching endpoints lawful, exact duplicate tuple refused,
self excluded by stable key), zero amounts valid, blank not; a failed Confirm shows the MessageBox and keeps the
typed values, and the parent Apply stays the only persistence call.
C — money: `interpretMoneyTyping` separates presentation grouping from logical edits (`1,200,000` not
`1,200000`), strict paste keeps `-100`/`1e3`/`12,34` visibly invalid, IRR is integer-rial in the UI (`120000.00`
→ `120,000`, wire untouched) and a stored non-zero fraction is refused only on edit, never rounded.
D — weekly view/edit is compact at 360/390 (paired From/To on one line, closed day as one badge with no empty
inputs; live edit exposes exactly 2 time inputs) and both modals fit/scroll internally (390: 352×566;
`scrollWidth == clientWidth` at 390 and 360).
Proof: 22/22 new facts, ALL feature tests recursively 188/188, one clean `tsc`; guards 10/11 with `FE-SIZE`
failing only on the four documented pre-existing drifts. Live on the real rev2 draft with no Apply/PUT/
publication/migration: cascade, no phantom chip, breadcrumb, duplicate refusal, invalid money refusal, clean
Cancel with no discard dialog and the stored baseline (`1..3`, `120,000/5,000`) intact. Screens in
`screens/` (1440 destination modal, 390 rate modal/refusal, 390 week view/edit).
NOT claimed: user Visual ACCEPT, live service-area or legacy-conflict rendering, any Storefront/checkout
consumer. Elapsed exceeded the HARD45 box across a chat-loss and correction rounds — recorded, NOT a process
PASS. Compact per-order-group order-evidence decision stays locked and unimplemented; TB185 paused, default-OFF,
PendingAllocation preserved. STOP_FOR_ARCHITECT.

## SUPERSEDED —225 issued after user rejects picker/rate UX, IRR formatting and mobile —2026-10-26

Active task TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225. Read225/TASK.md. No new user trial until Architect review.
224-R1 integrity acceptance remains bounded, NOT UI Visual ACCEPT; latest user screenshots show grouping unusable for ordinary editing (5,000000), duplicate row checks absent/poor feedback, and mobile week whitespace. Reproduce/repair, don't claim previous166 tests prove these.
User requires destination Add in persistent modal with FOUR fixed DB-backed cascade fields country/area/city/service; parents disabled until selected, changing parent clears descendants+stale requests. One deepest-terminal selection/chip with full actual breadcrumb+remove, ancestors not extra coverage. Add multiple without modal closing. No terminal-level dropdown. Actual overlap prohibition remains.
Rate Add/Edit in modal, compact main summaries; validate against other rows in SAME area, exclude only actual self on edit;0 valid, blank invalid. Use existing MessageBox on explicit failed action, accessible field indication, not red text wall/keypress popups.
IRR is integer display/input per user; existing integral .00 displayed without fractional suffix but exact untouched wire retained. No rounding nonzero fractions or globally forcing other currencies. Repair generated grouping vs pasted invalid handling; preserve negative/exponent refusal and caret.
Mobile view/edit week needs compact coherent day/status/hours, not merely no horizontal overflow. Check actual modal/weekly screenshots360/390/1440.
No owner/seller redesign or Storefront feature in225; compact shared-snapshot order-evidence decision still mandatory/unimplemented. Preserve user draft/capture/tabs and applied migrations/catalog. HARD45/stop30, narrow tests only, no166/whole-suite replay/Bridge. STOP_FOR_ARCHITECT after225.

## CURRENT —224-R1 integrity repairs accepted for bounded ADMIN USER TRIAL —2026-10-26

Architect independently inspected e79bd74c53a0f6b514785b480739e60709eeb43c: editor-area-range, authoring-intent, money-text, screen controlled state/reseed/commit/hasNoIssues wiring, MoneyField and rate validator. The two acceptance blockers from224 are closed for user trial: invalid/partial shared range now joins localDirty/issues and blocks Apply; unsupported money input remains invalid instead of becoming a different accepted amount. Separator Backspace and decimal-side caret repaired as worker-reported mounted proof.
Evidence: worker11 new focused facts, recursive166/166, clean typecheck; worker actual isolated mounted invalid-range+valid-edit blocks Apply, row-less range block, invalid money/decimal typing and discard/reentry. Root did NOT rerun tests or execute local browser/Apply. No blanket save/error/legacy-conflict/browser matrix certification. Legacy unequal durations remain test-only; shared duration is still repeated per row, no group-level schema change. No user Visual ACCEPT.
USER MAY NOW TEST admin panel: http://127.0.0.1:3000/admin/shipping-services and actual draft .../authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=edit. Read current before later writes; worker preserved rev2/pointer and user's tabs, no PUT. Host actual path per worker D:\temp\tb221-build\out\Tooba.Host.exe on5088, FE3000; historical PID26600/28288 must be reverified.
Current active task NONE, next NOT ISSUED. Await user feedback, don't reopen proof-only loops. UI acceptance only, NOT Storefront delivery: actual consumer/projection/selection/commit and compact per-order-group provenance/final money still unimplemented. Keep locked compact order evidence decision. Seller-admin entry from seller list is separately pending. Four unrelated FE-size drifts remain; elapsed50min vs25 was NONCOMPLIANT, not processPASS.
No Bridge/historical scripts; preserve shared migrations/catalog/captures, TB185paused/defaultOFF/PendingAllocation.

## CURRENT — 224-R1 executed: typed range is form state, invalid money stays invalid — 2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224-R1/{RESULT.md,TASK.md}` and the
append-only correction in `.../TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224/RESULT.md`.
Base `63b0a3c3c3b588769e37b31d0e9200b8a3dbcba6` (clean fast-forward; the three/four pre-existing backend
line-ending drifts and one stray 0-byte root file were never staged — that file was deleted).

A — the TYPED area range is now the form state that owns dirty/issues/Apply/discard. `ui/use-area-range-edits.ts`
is DELETED; `model/editor-area-range.ts` adds `AreaRangeDrafts`/`areaRangeEdit`/`areaRangeDraftIssues`/
`areaRangeDraftsDirty`/`createAreaRangeDrafts`, `buildAuthoringIntent(..., areaRanges)` folds blank, partial and
invalid text into the blocking issues, `authoring-screen.tsx` keeps `areaRanges` in state and reseeds form +
groups + rates + area ranges + week from the SAME baseline through ONE `applyLocalDrafts` helper (fresh load and
confirmed cancel), and `AreaDeliveryRange` is fully controlled with an area-scoped message — a row-less area says
«یک ردیف اضافه کنید یا بازه را خالی کنید» instead of silently persisting nothing. HONEST BOUNDARY: the
DTO/capture still repeats the days per row; a true group-level persisted duration remains a schema change, NOT done.

B — `model/money-text.ts` no longer rewrites unsupported money, and its grouping no longer swallows a separator
Backspace (found by this task's OWN mounted gate: a Backspace right after a comma deleted only the separator,
grouping re-inserted it with the same caret, so the key did nothing forever; `interpretMoneyBackspace` now
removes the digit with it — `120,000` → `12,000`, `1,000` → `000` visibly invalid). `readMoneyText` normalizes only Persian/
Arabic-Indic digits, the Arabic decimal separator, correctly formed 3-digit comma grouping and boundary
whitespace; signs, letters, exponent, multiple/malformed separators keep their raw text with `valid: false`,
render `aria-invalid` and block Apply with `amountInvalid`. `MoneyCaret { whole, fraction, inFraction }` +
`caretAnchorAt`/`caretOffsetFor` replace digit-count-only anchoring, so `12` → `.` → `5` really yields `12.5`.

RED on the reviewed baseline (probe): 8/8 defect facts — `-100`→`100`, `1e3`→`13`, `12.3.4`→`12.34`,
`12,34`→`1234`, `abc100`→`100`, digit-only caret with no decimal side, and NO typed-range form state.
GREEN: 11/11 new R1 facts, 166/166 feature tests (recursive), one clean `tsc`; guards 7/7 except `FE-SIZE` on the
four documented pre-existing drifts (none touched; the edited screen is 798 LOC ≤ 800).

Mounted (FE3000 PID28288 / Host5088 `D:\temp\tb221-build\out`, health 200, no restart, isolated new tab — the
user's two tabs were never reloaded): partial maximum disabled Apply with a visible area issue while a valid
packaging edit was staged; `2.5` refused; a row-less area's staged `4..9` blocked Apply on its own, then moved
into the first row; freight `12` → `12.` (invalid, blocked) → `12.5` caret 4; pasted `-100`, `1e3`, `12,34`
stayed exactly as typed with `aria-invalid` and a disabled Apply; reject-cancel kept every draft, confirm-discard
restored the stored baseline (1 area, `1..3`, `120,000.00`/`5,000.00`), re-entry identical with no dialog. NO
Apply/PUT/publication/catalog/migration/seed; a read-only document GET returned `revision 2`,
`updatedAt 2026-10-24T19:05:52.452036Z`; the draft and the selected pointer are untouched. Screenshots stored
(1440×900 and 390×844, `scrollWidth == clientWidth`, `overflowX 0`).
NOT claimed: user Visual ACCEPT, Architect acceptance, legacy-conflict live rendering, any Storefront effect.
Actual ≈50 min vs HARD25 — recorded, NOT a process PASS.
Active task: NONE. Next: Architect review; do NOT call the user to test before acceptance.
The compact-order-evidence decision stays locked and unimplemented; STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

## SUPERSEDED — 224 executed: one delivery range per area, Add-time duplicate, level-aware picker, exact money — 2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224/{RESULT.md,TASK.md}`.
Base `920b51a7f2dc193afb47366daf9a531415d21373` (clean fast-forward; only the four pre-existing
backend line-ending drifts and the new evidence folder were untracked/unrelated and never staged).

A — the customer-receiving duration is now ONE range of the WHOLE delivery area, placed above the rows.
`model/editor-area-range.ts` derives `empty|uniform|conflict` from the rows, writes a chosen pair to exactly
that area's rows (same identity when nothing changes), and gives a new row only its own area's range
(`withAddedRowInheriting`). Unequal stored rows are a LEGACY CONFLICT: values are preserved, the control
starts empty and only a deliberate lawful entry unifies them. HONEST BOUNDARY: the existing DTO/snapshot
still repeats the days per row, so the shared range is serialized as the same pair on every row of that
area; a row-less area persists nothing. A true group-level persisted duration is a schema change, NOT done.
`ui/use-area-range-edits.ts` keeps the staged pair (first-row inheritance) and forgets it on a new baseline.

B — duplicate/overlap is checked ONLY on a submitted Add, never on render/onChange. Staging an already
stored country (the screenshot's Iran) shows no error and keeps the branch explorable; the refusal appears
inline on Add and is cleared by any level/path change, and the staged path plus the stored chips survive.
`destinationAddAvailable` (verified evidence + ancestry, deliberately ignoring "already stored") and
`isDuplicateSelection` are the executed helpers; authoritative overlap rules are untouched.

C — the four level buttons became ONE localized level select, and only the controls that terminal needs are
rendered (country / +province / +city / +service area). Country-wide null-admin cities stay lawful; parents
are never fabricated or substituted.

D — exact money display: `model/money-text.ts` groups for display and hands the RAW canonical token to the
model (Persian/Arabic-Indic digits, grouped paste, Arabic decimal separator, digit-anchored caret). Applied to
freight, packaging and both merchandise bounds; read summaries use the same string grouping. No Number
round-trip, no forced `.00`, no scale guess, currency stays server-owned; grouping never dirties or saves.

Verified: one clean `tsc`; 155/155 feature tests (11 new) ; guards 7/7 except the four documented pre-existing
`frontend-source-size` drifts (untouched; the edited screen was kept at 798 LOC ≤ 800). Live FE3000/Host5088
reused with no restart and real traffic: grouped `120,000.00`/`5,000.00`, one `1..3` area range with zero
per-row duration inputs, stored-country staging without error → Add shows the localized duplicate/overlap
message and keeps the path, City level renders country+province+city with the error cleared, `4..9` applied to
the area, `1850000.00` → `1,850,000.00`, Persian `۵٬۰۰۰` → `5,000`, and cancel→discard→re-enter restored the
exact baseline (1 row, `1..3`, `120,000.00`) with no residue. 1440×900 and 390×844 screenshots stored
(390 `scrollWidth == clientWidth == 390`). NO Apply/PUT/publication/catalog write, no migration/seed/reset;
the user draft `01a16eeb-70b3-7000-87c2-564c82835496`, its revision, publication pointer and open tab are
preserved. NOT claimed: user Visual ACCEPT, live legacy-conflict rendering (test-proven only), and any
Storefront consumer/projection/selection/commit. Elapsed ≈55 min vs HARD40 — recorded, NOT a process PASS.
Active task: NONE. Next: Architect review; then the seller-admin entry (planned separately) and the still
unwired Storefront path under `SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md`
(one shared immutable snapshot per owner+draft+revision; compact per-order-group facts; no full authored JSON
per order; no historical repricing). STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

# Tooba — Agent Rules

## CURRENT — 223 executed: compact publication restored + separate owner surface; await review — 2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-PUBLICATION-OWNER-PAGES-223/RESULT.md` and `TASK.md`.
Base `d62bab9a4f31dc1966aed8c06b4f7ea08a366df3` (the untracked 223 evidence folder collided with that commit's
tracked TASK.md: moved aside, fast-forwarded, artifact restored; no reset/stash/rebase/force-push).

A — publication is BACK, compact. Model + transport recovered BYTE-EXACT from 221 (exact
`expectedRevision`+`expectedSelectionVersion`, strict decode, synchronous in-flight gate, stale/unknown latches
cleared only by an explicit successful read, no editor reseed, no auto-retry). New panel is deliberately small:
one status line, at most one short reason, one honest note, Publish/Withdraw + confirmation. Snapshot id,
selection counter and format version are NOT in the normal UI. Live trial on the real draft
`01a16eeb-70b3-7000-87c2-564c82835496` in view mode: `Selected`, matches current revision ⇒ Publish DISABLED with
«همین بازبینی از قبل منتشر شده است.» (the same capture is not republished), Withdraw available. NO write sent;
draft revision and pointer were not touched by this task.

B — the TB222 single-option owner combo is GONE (live DOM `combo:false`, `ownerNote:false`). A separate route
`/admin/shipping-services/marketplace` renders the SAME list feature (`surface="marketplace"`) with its own saved
views/grid namespace; the store surface is `/admin/shipping-services` and links to it. The route is a UI boundary
only: the authorized owner (`/v1/admin/shipping-services/authoring-owner`) is read FIRST and no drafts are read
when that owner is not the surface's owner. SingleStore live: the marketplace route renders the honest
«مارکت‌پلیس در این نصب در دسترس نیست» state, no list, no fabricated owner; the reverse mismatch has its own
honest state. No owner id from query/body, no edition change, no seller/tenant inference.

C — the compact order invariant stays as recorded in
`docs/architecture/shipping/SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md` (one shared
immutable snapshot per owner+draft+revision; only compact per-order-group reference/final money/receiving facts;
no full authored JSON per order; no historical repricing). NOT implemented by this task.

Proof: one clean `tsc`; 144/144 feature tests (restored publication facts + 3 new owner-surface facts); 7/7
architecture guards; live FE3000/Host5088 with no restarts; 1440 + 390 screenshots, `overflowX 0`; closed-day
inputs still disabled; type options عادی/سنگین/فوق‌سنگین intact. Not replayed: 127/194/backend suites; the four
pre-existing source-size drifts untouched.
NOT claimed: Storefront/checkout consumer, projection/selection/commit, multi-owner live proof (impossible on
SingleStore — needs a server authorized-owner SET route), whole-method creation, any price/activation claim.
Actual ≈40 min vs the HARD30 box — recorded, NOT a process PASS.
Trial URLs: `http://127.0.0.1:3000/admin/shipping-services`,
`/admin/shipping-services/marketplace`,
`/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view`.
Active task: NONE. Next: Architect review, then hand the user the trial. STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. TB185 paused, default-OFF, PendingAllocation preserved.

## SUPERSEDED — TB223 issued: compact publication restore + separate owner pages — 2026-10-26

User explicitly requests publication restored, separate Marketplace page reusing SAME form instead of owner combo, and compact order evidence recorded before Storefront connection. TB222 source063aa05fd reviewed narrowly: closed-day UI+handler guard correct; heavy/extra_heavy real catalog writes worker-reported; no-op single-owner combo is NOT multi-owner delivery. Publication UI removal is now superseded by restore instruction; backend/pointer preserved.
Active task TB-SHIPPING-ADMIN-PUBLICATION-OWNER-PAGES-223. Read its TASK.md and docs/architecture/shipping/SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md.
Order MUST reuse one shared immutable snapshot, compact per-order-group reference/final monetary/receiving facts; NO full authored JSON per order, NO historical repricing. This is a required future integration invariant, NOT implemented yet.
TB223 restores UI first, real authorized separate owner entry, no copied form/no fake edition/owner switching. Live SingleStore cannot prove Marketplace multi-owner behavior. Publication still intent, NOT Storefront activation. After223 review move toward actual existing projection/selection/commit, not another calculator/audit.
Preserve user data/unsaved tabs, real catalog, backend snapshot/pointer, applied migrations. No new Bridge probe. STOP_FOR_ARCHITECT after task; no automatic next task.

## CURRENT — 222 user feedback executed: closed-day guard, honest owner control, real type catalog — 2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-USER-FEEDBACK-222/RESULT.md` and `TASK.md`. Base
`472384b81e5cde3e9e2b45b3e3c7aa2a5c1172f8` (local was one docs-only commit behind; fast-forwarded, no
reset/stash/rebase/force-push). The pending frontend cleanup reported by the user was inspected FIRST,
preserved, and committed by this task: publication UI/API/model removed from the editor, owner line and
repetitive explanatory text removed, simple «نام»/«توضیح» labels, DB-native language labels, Saturday-first
DISPLAY order, and an unsaved-work warning only when the session is dirty. Backend publication/capture/
pointer capability, routes, migration and data were NOT deleted.
Executed: (1) a closed day now really disables BOTH `type="time"` bounds AND `withDayTime` refuses an
unknown or closed day at the handler boundary returning the same state (grey input is presentation, not the
protection); toggling closed preserves the exact remembered bounds, the ISO weekday and the stored time
text, and reopening restores them — proven live (08:30/12:45 → disabled+preserved → restored). Two old week
facts that typed bounds before marking the day working were corrected to the truthful order, no assertion
weakened; new `closed-day-222.test.ts` 3/3.
(2) the primary list now shows a REAL owner control populated only from the server-authorized
`GET /v1/admin/shipping-services/authoring-owner`, with strict decode, generation-guarded loads (rows
cleared, superseded scope never wins) and scope-keyed grid identity for saved views/export. This
installation is SingleStore/store-alpha, so exactly one lawful option exists (`همین فروشگاه`) with an
honest note; no fake Marketplace entry, no guessed GUID, no legacy row import, no owner picker in the
editor. Documented gap: an ALTERNATE authorized owner (Marketplace listing its real authorized sellers)
needs a server route returning the actor's authorized owner SET plus owner-keyed list/context reads — an
authorization-surface change, deliberately not faked with client filtering.
(3) the real type catalog was completed through the existing catalog API only: `heavy` →
`01a1785e-01f3-7000-b50d-d6372e756f89` and `extra_heavy` → `01a1785e-6b50-7000-a98f-2e1c80544815`, created
inactive then activated (both `isActive:true`, revision 2, sort 20/30) with fa-IR/en-US/ru-RU labels from
the DB registry; `standard/عادی` untouched and no type auto-assigned. `extra-heavy` is not a lawful code
(domain rule is `a-z0-9_`). The editor selector now lists عادی/سنگین/فوق‌سنگین from the API.
Verified: typecheck clean once after final source; ALL feature tests 127/127; import/feature/flat-folder/
test-discovery guards 7/7; live trial on FE:3000 with real traffic (no `/v1` mocks) for the closed-day
facts, owner options, direct View/Edit and type options; desktop 1440 + 390 screenshots stored (390 has
`overflowX 0`). User draft `01a16eeb-70b3-7000-87c2-564c82835496` still revision 2, `updatedAt` 2026-10-24,
1 region group, pointer `selectionVersion 3`; no Apply, no publication write, no migration reapply
(214/219 already shared-applied by 221).
NOT delivered / NOT claimed: Storefront integration (still no live selected-plan consumer), multi-owner
list (server authorization gap above), and any price/eligibility/activation/readiness statement. Backend
unchanged; three backend files in `git status` are line-ending-only drift and were NOT staged.
Active task: NONE. Next: Architect review, then hand the user the trial. STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. Elapsed ≈60 min vs the 30-minute box — recorded, NOT a process PASS. TB185 paused,
default-OFF, PendingAllocation preserved.
## CURRENT —221 bounded ADMIN USER TRIAL READY; WAIT FOR USER FEEDBACK —2026-10-25

Architect reviewed443c75817dca4d84065be73c4089fd29e9a96eba against87a138a224a3f12b3f1e4b481a9e4cc3980f7fd5, production/test diff, live receipts and screenshots01/07/08. Accept for ADMIN USER TRIAL, not full functional/visual/Storefront certification. Worker14 focused tests/typecheck/7 architecture facts green; root did not rerun. Four pre-existing source-size failures untouched.221 elapsed not reported: timing compliance UNKNOWN, not PASS.
USER TEST NOW: http://127.0.0.1:3000/admin/shipping-services
Draft: http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Primary list is real owner's existing drafts; post/NULL draft appears by server draftId, stored default-language name پیشتاز. View/Edit route DIRECTLY to new editor. Old catalog/create remains explicit ?legacy=1, not silently merged. New method/draft creation is not complete in this main workflow; don't tell user all legacy functionality was replaced.
Live221 target127.0.0.1:5432/tooba_alpha, SingleStore/store-alpha. BOTH214 AuthoringRevisionSnapshots and219 SelectedCapturedRevision migrations now APPLIED; runner current20261025130021_SelectedCapturedRevision pending0. Do NOT reapply nine old migrations/reset schema.
Host5088 Development PID26600, FE3000 PID28288 per worker221 receipts, health200. Current Host isolated executable path was NOT recorded: reverify from actual process if restart needed, do NOT assume obsolete207 binary. Ports/processes are time-of-receipt facts, not permanent PIDs.
Draft fields remain revision2, updatedAt2026-10-24T19:05:52.452036Z, preserved. Live publication wrote capture01a17389-c201-7000-8512-c2950a6e913a and pointer absent->selected1->cleared2->selected3. FINAL SELECTED3, source draft revision2; not restored to initial absent. Startup transport also consumed pending catalog/pricing messages. Shared changes are NOT NONE.
221 did NOT Apply ordinary draft edits; earlier209 save proof is history, not rerun. User should test list->Edit->areas/money/week->Apply->VIEW publish. Publication is disabled in EDIT; in VIEW same already-selected revision is intentionally disabled. No claim all error/race/lifecycle branches mounted-tested.
Small decoder/lifecycle hardening and disabled-state explanation can follow ACTUAL feedback; no speculative proof-only task now. Screenshot narrow content is inside a larger PNG canvas, not a native390px export; DOM reports390 no overflow, user visual acceptance pending.
Still NOTLIVE Storefront/checkout: no selected-plan consumer, actual fulfillment owner/type/group-membership/measurement source, projection/selection/commit connection; physical unknown remainsNULL. No price/activation/readiness effect. §29 and211–220 bounded decisions closed, TB185paused/defaultOFF/PendingAllocation.
Active task NONE. Next task NOT ISSUED. USER TRIAL GATE: call user now and wait for feedback before more Storefront implementation. No222 envelope. Recovery source review at221/ARCHITECT-REVIEW.md; original task receipt added there by Architect (worker omitted TASK.md). No Bridge/historical scripts. Older CURRENT headings are history.

## CURRENT —221 delivered: primary authoring list + publication controls; real local trial ready —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-LIST-PUBLICATION-USER-TRIAL-221/RESULT.md` and `receipts-221.txt`. 221 is a frontend feature slice plus ONE bounded backend consistency fix, then the REQUIRED local runtime/schema refresh and a real mounted trial.
Delivered: `/admin/shipping-services` is now the owner-authorized list of EXISTING authoring drafts (existing `authoring-drafts` GET) on `AppDataGrid`+saved views; View/Edit push straight to `/admin/shipping-services/authoring/{draftId}?mode=view|edit`; names come from the authoritative `authoring-context` default language, carried by an additive `MethodTranslations` on `ShippingAuthoringDraftItem` (one GET, no per-row N+1); a missing name is an honest fallback and duplicate bindings are flagged. The old catalog is reachable ONLY behind the explicit `?legacy=1` link and is never a silent fallback.
Publication panel inside the existing editor feature (GET state / POST publish with expectedRevision+expectedSelectionVersion / POST withdraw): synchronous in-flight gate, VIEW-only, 403/409/validation ⇒ stale latch, unestablished outcome ⇒ unknown latch, both cleared ONLY by an explicit successful read; a publication write never reseeds the form, advances the draft revision or discards typed work. Issued fix executed: a cleared result with no row now reports `Absent`; a real cleared pointer stays positive `Cleared`.
Executed live on the existing local `tooba_alpha`/store-alpha after an exact plan: Shipping `20261025130021_SelectedCapturedRevision` and the 214 snapshot migration applied (runner reports every module pendingCount=0), Host rebuilt isolated and running 127.0.0.1:5088, FE :3000, existing Development dev-actor identity. Real draft `01a16eeb-70b3-7000-87c2-564c82835496` (revision 2, «پیشتاز»): publish → Selected v1 → withdraw → Cleared v2 → publish → Selected v3, snapshot `01a17389-c201-7000-8512-c2950a6e913a`, always matching revision 2, and the DRAFT itself was never modified (revision/updatedAt unchanged, no Apply pressed). 1440/390 screenshots + receipts stored; 390 has zero horizontal overflow. Focused 14/14 pass, typecheck clean; import/flat/feature/test-discovery guards pass. `frontend-source-size` still fails on FOUR files this task did not touch (pre-existing baseline drift) — recorded, not repaired.
Active task: NONE. Next: Architect review + USER TRIAL at `http://127.0.0.1:3000/admin/shipping-services`; do NOT issue a Storefront implementation task before that trial. Publication remains selection INTENT only: no Order/Settlement/Storefront consumer, no projection/selection/commit, no price/eligibility/readiness claim. Creating a NEW method is still the legacy catalog. §29 and 211–219 bounded behavior preserved; TB185 paused/defaultOFF/PendingAllocation. Older CURRENT headings are history.## SUPERSEDED —220 bounded ACCEPT; 221 was the only issued task —2026-10-25

Architect independently reviewed69a3daa635b2386e6766a0d601fce1fe69d70145 against4fc9ccac044aa2a084a833172b645e6b0191f6f8: delegates authorize before body/ISender; validators distinguish missing/null/zero; owned draft derives binding; capture+select are two commits, no post-select dependency, current pointer CAS retained; withdrawal explicit. Worker8 real15432 endpoint facts passed TWICE (8 facts, not16 distinct), builds0 errors; no root reruns. 120>35 minutes and multiple corrections are NOT process PASS.
Retain220, no R1 loop. Small contract consistency carried into221: Clear absent currently returns Cleared/version0, contradicting DTO's 'zero only Absent' and nextGET Absent. Return Absent when cleared.Value.Exists==false, keep true cleared pointers positive. No schema/new policy.
USER PRIORITY: deliver a testable admin panel BEFORE further Storefront development. User rejects hours of invisible work. Only issued TB-SHIPPING-ADMIN-LIST-PUBLICATION-USER-TRIAL-221,parent220: direct new-editor View/Edit from service grid, explicit empty-draft setup only if necessary, publication controls in existing feature, then REQUIRED current local runtime/schema refresh and real mounted inspection for user trial. No further price/calendar/audit feature.
221 may apply ONLY existing pending214 snapshot +219 pointer migrations to nonproduction tooba_alpha/store-alpha after exact plan; build current Host/runner isolated and refresh exact Host5088 as necessary under user's ALREADY EXPLICIT authorization. Preserve frontend3000 unless truly requires task-scoped fix; preserve user browser/unsaved edits and draft fields. No resets/drops/reseeds/auth changes. Record actual startup/background effects, don't pretend zero data writes.
After221 result Architect reviews screenshots/source and calls user to test. Do NOT issue222 Storefront implementation before giving user this trial. Current list's primary actions still old until221 runs; no claim of replacement yet.
Still NOTLIVE Storefront: selected-plan consumer and real fulfillment owner/type/group membership/physical measurement authority + projection/selection/commit remain. Selected publication is intent, not customer-ready; use honest UI note. Physical unknown staysNULL. 211–219 bounded behavior and§29 closed; TB185paused/defaultOFF/PendingAllocation.
Active221 only, await result. No Bridge/historical post-result script; exact-path staging. Nine old migrations applied;214/219 still pending shared AT THIS CHECKPOINT (221 will record actual application). Host5088/FE3000 identities must be reverified, not assumed from old PIDs. Older CURRENT headings below are history.

## CURRENT —220 publication API delivered; NOT live —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-PUBLICATION-API-220/RESULT.md` and `TASK.md`. 220
added the authorized publication actions for an EXISTING draft on BOTH `/v1/admin` and `/v1/seller`:
`GET|POST .../authoring-drafts/{draftId}/publication` and `POST .../publication/withdraw`. Thin real
delegates: existing owner authorizer (`View` GET / `Manage` POST) FIRST, existing `ShippingEndpointBody`,
then typed MediatR. BOTH tokens are `[JsonRequired]` and the expected selection version is NULLABLE, so an
omitted field is never version 0. GET derives the binding from the owned draft and reads the TB-219 pointer
once, returning explicit Absent/Cleared/Selected, version 0 only for Absent, and a pure DATA equality against
the current saved revision. POST = owned draft read → stale saved revision refused BEFORE capture → KNOWN
stale pointer token refused BEFORE capture → ONE existing capture → ONE existing select. Withdraw = ONE
existing clear. Capture and select stay TWO explicit commits: a lost pointer CAS is 409 with the OLD pointer
kept and a staging capture possibly left (asserted; no combined atomicity). Adjacent hygiene: a
`ReloadAsync` that leaves EF `Detached` now returns null (source guard, branch NOT executed).
`selected_capture.conflict`/`snapshot_unavailable` → 409 with FA/EN resources; `binding_mismatch`/`invalid`
stay 400. Executed: 0 build errors; ONE new class `ShippingAuthoringPublicationEndpointTests` 8/8 green TWICE
on disposable `127.0.0.1:15432/shipping_tb214` (container removed). No 12/9/43/18/8/119/194/16 replay.
**NOT WIRED / NOT LIVE**: no Storefront/checkout consumer, no FE list/editor controls, no Order projection/
selection/commit. `Selected` is selection INTENT only — never eligibility, readiness, activation or price.
No schema/migration, no user-draft write, no Host5088/FE3000 restart, no flags/seeds; 214/219 migrations
still NOT shared-applied. §29 and 211–219 preserved; TB185 paused, default-OFF, PendingAllocation.
Actual ≈120 min vs 35/23/12 boxes — recorded, NOT a process PASS.
Active task: NONE. Next: NOT ISSUED — STOP_FOR_ARCHITECT (wire publish/withdraw + direct new-editor from the
service LIST, then real fulfillment owner/type/group-membership/measurement through projection/selection/
commit). No generic audit/calculator task; no 221 envelope.

## SUPERSEDED —219-R1 bounded ACCEPT; only220 authorized publication API —2026-10-25

Architect source-reviewed4fa863c9150c79759d43a5b1624e4eaed2e856cc againste4e3a1cf7b6fcc0e9ba7e84c77eb1526a49bb101. Fresh pointer reload precedes version/no-op decisions; Domain next-version validation now precedes target/time assignment; typed domain refusal preserved. Worker red3/green12 real15432 storage facts/build0; root source review, no reruns. Time26>20/stop12 deviation remains.
219/219-R1 accepted for the two demonstrated defects and ordinary select/clear/capture-pointer behavior; no R2 proof loop. Reload disappearance claim is NOT proven: EF can detach missing row without throwing;220 must add the small post-Reload Detached-state guard before returning tracked reference. This is adjacent hygiene, no schema/engine rewrite.
Only issued TB-SHIPPING-ADMIN-DRAFT-PUBLICATION-API-220,parent219-R1: real GET publication / POST capture-and-select / POST withdrawal for EXISTING draft, admin and seller thin authorized delegates + MediatR/validators/resources, reusing214 capture and219 selected pointer. No new publication store or lossy copy. Capture + select are TWO explicit commits; on pointer-race failure an immutable staging capture may remain but current pointer unchanged. Never claim combined all-or-nothing or activation readiness.
Active220. No221 issued. After review wire publication controls + direct new-editor access from service list, then actual fulfillment-owner/type/membership plan into Storefront projection/selection/commit. No further generic audit or calculator task. Physical weight/count unknown stayNULL.
NOTLIVE: current Storefront/checkout and old service-list workflow not replaced. User requested complete usable delivery; do not claim current internal primitives are live. Earlier2–4h was an estimate, not a verified deadline.
Preserve§29,211–219 accepted bounded behavior, actual user draft/current revision/unsaved tabs, Host5088FE3000. Nine old migrations applied;214/219 migrations NOT shared-applied.220 no shared migration/runtime/flag/FE operation. TB185paused/defaultOFF/PendingAllocation. Existing nonproduction authorization applies later; do not reask. No Bridge/historical post-result scripts; exact paths only. Older CURRENT blocks are history.

## CURRENT —219 review: two bounded write defects; only219-R1 issued —2026-10-25

Source eca53dae9c56bfb4b7f2be91eb54da64e1464319 independently compared with4972c6c8af39c000bceaebe4f0846540ec37f107. Main verified exact fullSHA (worker's HEAD line was truncated typo). Retain selected-pointer architecture/schema/Contracts reader and existing219 proofs, but do not close219 before R1.
Defect1: SelectAsync/ClearAsync use tracking ScopedPointer queries; a clean stale tracked pointer survives materialization. After another context advances it, an old expected version can return false no-op success (Select old target or Clear stale-cleared) and a current expected version can be rejected incorrectly.
Defect2: Domain Select/Clear mutate SelectedSnapshotId BEFORE NextVersion can throw at long.MaxValue. Exception occurs before TrySave cleanup; later caller SaveChanges can leak the rejected target/clear without a version advance.
Only issued TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219-R1,parent219: authoritative fresh pointer before comparison + exception-atomic domain mutation, focused real15432 regressions. Preserve tracked object identity/unrelated caller work; refresh only clean owned pointer deliberately, never ChangeTracker.Clear.
Active219-R1, await result. No220. No generic proof loop or new publication architecture. 219 worker9 real PostgreSQL +9 affected216 class reported green; root source review only. New cases absent from those tests. Hard30 respected total29 but implementation-stop20 exceeded; no process PASS.
Still NOTLIVE: no admin publication action/Order selected-plan consumer/projection-selection-commit. After repair close219 and proceed actual publication/fulfillment-plan integration, no new calculator. Physical measurements unknown; §29/211–218 remain closed.
214 and219 migrations NOT shared-applied, nine old migrations applied. Preserve actual draft/revision/unsaved tabs, Host5088/FE3000, TB185paused/defaultOFF/PendingAllocation. Local development authorization remains valid later. No Bridge/historical post-result script/external message; exact paths only. Historical CURRENT blocks below are superseded.

## CURRENT —219-R1 stale-pointer + rejection-atomicity repaired; NOT wired / NOT live —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219-R1/RESULT.md` (task
verbatim in `TASK.md`) and `219/ARCHITECT-REVIEW.md`. Two source-confirmed defects repaired; the 219
schema/architecture/Contracts/owner-resolver are retained.
(1) The adapter now obtains the AUTHORITATIVE persisted pointer via a new private `LoadPersistedPointerAsync`
— the one owner-scoped tracked entry is explicitly `ReloadAsync`ed BEFORE any expected-version or no-op
decision (identity preserved, unrelated caller state untouched); a vanished row is absent (only that entry
detached), and a stale leftover for a provably absent row is detached so it can never act as a live token.
`RequireCleanTracker` still refuses dirty caller work first; real CAS token and exact unique matcher still
guard writes. (2) `Select`/`Clear` compute/validate the whole next state BEFORE any assignment, so a rejection
at an exhausted version leaves target/version/`UpdatedAt` unchanged, entry `Unchanged`, later `SaveChanges` at
0 and DB unchanged; the adapter maps that rejection to a normal `selected_capture.invalid` result.
Executed on the task-owned disposable `127.0.0.1:15432`/`shipping_tb214`/`tb214` (identity verified before
DDL, container removed, port silent): RED on unchanged production 3 failed / 9 passed → GREEN 12/12; build 0
errors; only the affected storage class executed.
**NOT WIRED / NOT LIVE**: no route, no admin publication UI, no Order consumer, no
projection/selection/commit; selection is publication intent only.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next = real publication/selected-plan
integration + real fulfillment owner/type/group membership and measurement authority).
Preserve §29 decisions and accepted 211–219, real user draft/revision/unsaved tabs, Host5088/FE3000. Nine old
migrations applied; 214 snapshot migration and the 219 pointer migration are NOT shared-applied. TB185
paused/default-OFF/PendingAllocation. No Bridge interaction/historical post-result script/gitadd-A; exact-path
staging. Older CURRENT sections are history.

## SUPERSEDED —219 pointer executed; two write-correctness defects found in review, 219-R1 issued —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219/RESULT.md` (task
verbatim in `TASK.md`). 219 added ONE Shipping capability: `ShippingSelectedCapturedRevision` (one row per
FULL owner + canonical service/option) pointing at an **already existing** TB-214 immutable snapshot, table
`shipping.selected_captured_revisions` + ONE additive migration `20261025130021_SelectedCapturedRevision`,
a `NULLS NOT DISTINCT` unique owner/binding index (service/option persisted canonical), owner-coherence and
version CHECKs; `selection_version` is a real concurrency token. `IShippingSelectedCapturedRevisionDirectory`
(internal Select/Clear with `ExpectedSelectionVersion`, 0 = no row) and the Contracts-only
`IShippingSelectedCapturedRevisionLookup.ReadAsync` are served by ONE scoped adapter in `ShippingModule`.
Target resolution reuses the existing exact `GetCapturedRevisionAsync`+`ReadPayload` and NEVER captures; the
persisted binding is **derived** from the verified payload. Create only from version 0; stale expected version
⇒ `selected_capture.conflict` before any mutation; same target at the current version is a no-op preserving
version AND `UpdatedAt`; clear retains row/version and advances once when changed; a pre-clear version cannot
resurrect; rollback = selecting an OLDER existing capture at the current pointer version. Unrelated dirty
tracker refused first; one `SaveChanges`; only the exact 23505 on the named unique index and
`DbUpdateConcurrencyException` map to conflict; caller exception/token propagate. 216's owner/edition/tenant
coherence mapping was extracted to `ShippingCapturedOwnerScopeResolver`, reused by 216+219 only.
Executed on the task-owned disposable `127.0.0.1:15432`/`shipping_tb214`/`tb214` (identity verified before
DDL, container removed): new class 9/9 real-PostgreSQL facts; build 0 errors. No shared migration, runtime,
HTTP, FE, flag or user-draft change.
**NOT WIRED / NOT LIVE**: the 218 composer does not consume this reader yet; no route/MediatR write/admin
publication UI/projection/selection/commit. Selection = publication intent ONLY, never readiness/eligibility/
quotability.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next slice = explicit admin publication wired
to this pointer + real fulfillment owner/type/group membership + physical measurement authority, then
projection/selection/commit).
Preserve §29 decisions and accepted 211–218, real user draft/revision/unsaved tabs, Host5088/FE3000. Nine old
migrations applied; 214 snapshot migration and the new 219 migration are NOT shared-applied. TB185
paused/default-OFF/PendingAllocation. No Bridge interaction/historical post-result script/gitadd-A; exact-path
staging. Older CURRENT sections are history.

## SUPERSEDED —219 issued; 218-R1 accepted; result awaited —2026-10-25

Architect reviewed dfdeae8afb1150d9beda10de43ec673f09c44aba against 3a583ab2c8a173bd4021046036074465218d75d1. Request-wide typed binding key/equality/hash and same-revision multi-group control are correct. Worker executed red3/green5 plus full composer16, build0 errors; not root reruns. Time26>12/two fixture corrections not process PASS. The casing regression also includes a whitespace-invalid service (not isolated casing execution); equality implementation independently inspected, no R2 proof loop.
218 composition is closed for this bounded behavior, NOT LIVE. Canonical recovery was absent from worker diff again; this TOP checkpoint is authoritative.
Only issued TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219, parent218-R1: Shipping-owned versioned pointer to an EXISTING immutable capture, optimistic select/clear and exact Contracts-only read by server owner+binding; rollback can select an older existing capture. One new Shipping table/migration, no legacy flattening, no price/calendar rewrite, no invented readiness. No HTTP/UI/shared migration/runtime/gate flip in219. Pointer selection is explicit staging/publication intent, NOT customer permission/eligibility.
Active task219. No220 envelope. Next after review: wire explicit admin publication and actual fulfillment owner/type/membership into current projection/selection/commit; use219 reader instead of browser/manual candidate revision. Physical weight/count stay NULL until real authority exists. Avoid further generic audits or old suite loops.
Preserve §29 decisions (customer receipt days, closed days skipped, same-group post-discount tax-inclusive goods), accepted211–218, user draft/current revision/unsaved tabs, Host5088/FE3000. Nine old migrations applied;214 snapshot migration still NOT shared-applied and219 will NOT apply its migration shared. TB185 paused/default-OFF/PendingAllocation. User nonproduction authorization remains valid for later necessary live refresh, do not ask again.
No Bridge interaction, no historical post-result scripts, exact-path staging only. Older CURRENT sections are history.

## CURRENT —218-R1 request-wide revision consistency done; NOT wired / NOT live —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218-R1/RESULT.md` (task
verbatim in `TASK.md`) and `218/ARCHITECT-REVIEW.md`. 218-R1 executed the ONE source-confirmed defect:
`ValidatePlan` now owns a single request-wide typed key `StorefrontCapturedPublicationBinding` (coherent
owner kind + exact tenant + seller + canonical trim/OrdinalIgnoreCase service/option; destination,
`MethodCode` label and group key are NOT publication identity) mapping to exact `DraftId` +
`SourceRevision`. The per-group `seenBindings` string dictionary is gone; same binding with a different
draft OR revision across groups ⇒ existing `storefront.captured_group.plan_invalid` BEFORE any goods
reader/Cart/Pricing/Shipping call, while the same binding + same exact draft/revision in two groups stays
lawful with each group's own facts/destination/one quote+calendar. `NormalizeOwnerText` delegates to the
key's single `Canonical` (no second normalization policy). Composition, 217 reader, 216 quote, 213
calendar, `OwnerResult`, one clock read and scoped DI untouched.
Executed: one OrderTests build 0 errors; new `StorefrontCapturedGroupProjectionRevisionConsistencyTests`
proven RED first (3 failed / 2 passed: the two-revision, case/destination and different-draft conflicts
were not rejected) then GREEN 5/5; full composer area 16/16 (11 218 + 5 R1). Four 218 fixtures that had
used two drafts of the SAME binding were moved to genuinely independent bindings; every accepted
financial/calendar assertion unchanged. No public API/Contracts/schema/migration/flag/frontend/shared DB,
no user-draft access, no Host/FE restart. Actual ≈26 min vs 12-min box / 7-min stop — recorded, not a
process PASS.
**NOT WIRED / NOT LIVE**: no Storefront/checkout consumer, no route, no projection/selection/commit; the
selected captured revision and fulfillment owner/type/membership must still come from a reviewed server
publication/Offer/fulfillment authority, and physical weight/item count stay unknown.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next slice = server-selected captured
revision + real fulfillment owner/type/membership + measurement sources, then projection/selection/commit;
not another audit). §29 decisions and 211–217 stay accepted; the real user draft/revision/unsaved tabs,
Host5088/FE3000, the 9 applied migrations and default-OFF / TB185 paused / PendingAllocation are
preserved. No 219 envelope.
## SUPERSEDED —218 reviewed; 218-R1 later executed cross-group revision consistency repair —2026-10-25

Architect source-reviewed 6b3f3d2e940641f5b735d97b9da975acd2dcc05f and the exact base comparison.
218's composition of 217 whole-cart group facts, 216 Contracts quotes and 213 method-local receiving calendar is retained. Worker reports 11 new + 9 foundation facts/build green; Architect did not rerun them. This is NOT live Storefront acceptance. Time 33>25 and two corrections remain process deviations.
Concrete open defect: ValidateCandidates creates seenBindings INSIDE EACH group and uses case-sensitive delimiter strings although response binding uses case-insensitive service/option comparison. Consequently the same owner+service/option may select different captured draft/revisions in two cart groups (or evade detection with casing/destination changes).
Only issued TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218-R1, parent218: one request-wide typed binding->(draft,revision) registry, normalized service/option with existing semantics; same exact revision across groups MUST remain lawful, owners remain independent. Test actual ComposeAsync before any financial or Shipping call. No new architecture/publication/schema/HTTP/flags or old-suite replay.
Active task: 218-R1. Await its result; no219 issued. After review the next implementation must supply server-selected captured revision and real fulfillment owner/type/membership authority, then connect current projection/selection/commit. Physical weight/count unknown remain NULL; do not infer from decimal quantity. Do not advertise current composer as active/eligible.
Preserve §29 decisions, accepted 211–217, 218 composition, real user draft/revision/unsaved tab, Host5088/FE3000, nine applied migrations; snapshot migration214 NOT shared-applied. TB185 paused, default-OFF, PendingAllocation unchanged. No Bridge calls or historical post-result scripts. Older CURRENT headings below are history.

## SUPERSEDED —218 composer posted (review later found the cross-group revision defect) —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218/RESULT.md`. 218 added ONE
read-only Order.Application service `StorefrontCapturedGroupProjectionComposer` (registered scoped in
`OrderModule.AddServices`) that composes the ACCEPTED primitives for an EXPLICIT TRUSTED SERVER PLAN:
ONE 217 `ICheckoutShippingGroupGoodsFactsReader` call for the whole cart/partition (exact
`FinalGoodsAmountAfterDiscountAndTax` reused, never a plan amount), ONE 216
`IShippingCapturedRevisionQuoteLookup` call per candidate (exact owner/draft/revision/destination/currency;
`GroupItemCount` always null, no quantity→count cast), and the 213
`BuildCustomerReceivingCalendar` with the REAL method timezone, ONE `IClock` read per composition and
`MinDeliveryDays` used directly (no seller preparation re-added, no static window). Plan, returned facts
and quote binding fail closed (`plan_invalid` / `facts_mismatch` / `binding_mismatch`); per-candidate
refusals never wipe other groups; dependency exceptions and the caller token propagate unchanged; no
writes/SaveChanges/order/reservation/payment. Worked facts: real 99.00 / 198.00 through Promotion→Tax
reaching the corresponding Shipping calls, Saturday order + min 2 → Tuesday 2026-10-27 with each day's own
interval, one UTC instant → different local dates per group, unknown measurement stays unavailable.
Executed: build 0 errors, 11/11 new focused facts (real 217 directory harness + controlled Shipping
Contracts fixture + real 213) and 9/9 of the 217 foundation class.
**NOT WIRED / NOT LIVE**: no Storefront/checkout consumer, no public route/MediatR/flag, no
projection/selection/commit; the candidate owner/draft/revision/service plan must still come from a
reviewed server publication/Offer/fulfillment authority (never a browser) and physical weight/item count
stay unknown. No schema/migration/HTTP/FE/Host change, no shared DB, no restarts. Actual ≈33 min vs the
25-min box / 17-min implementation stop — recorded, not a process PASS.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next slice = real captured-revision
selection + fulfillment owner/type/membership + measurement sources, then projection/selection/commit;
not another audit). §29 decisions and 211–217 stay accepted and are not reopened; the real user
draft/current revision/unsaved tabs, Host5088/FE3000, the 9 applied migrations and default-OFF /
TB185 paused / PendingAllocation are preserved. No 219 envelope.
## SUPERSEDED —217 boundedACCEPT; 218 later issued and executed compose realgroup money + capturedquote + receivingcalendar —2026-10-25

Read217/ARCHITECT-REVIEW.md. a213bd0e288f3155019118e9b4fa199d25721e5c acceptedreal217financialauthority: CartAccess/version/partition, existingPreviewPromotion->Tax, exactCartLineIdmap, ΣTaxInclusiveSnapshot pergroup. Worker9new/194retained/buildpass;notrootreruns;timeover25recorded. Physicalweight/itemcountunknownremainNULL, notquantitycast.
Onlyissued TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218,parent217;25min/stop17. ONEreadonlyOrdercompositionservice combines217factsONCE with216Contractsquotes perexacttrustedcandidate and213calendar inmethodtimezone. Candidateplanexplicitserverinput, NOTbrowser/publish/activeauthority. No guessedSellerowner/type/measurement orlegacymoney/windowfallback. Availability≠monetaryQuoted. No newHTTP/liveflagyet.
Nextafterreviewmustsupplyactualowner/type/capturedrevisionselection andphysicalsource thenconnectprojection/selection/commit; don'tcallcomposerStorefrontcomplete. Needusertrialonlyafterrealpathwired. Noanothersourceaudit. §29decisionsresolved/previousstagesaccepted;no replay. Preserveactualuserdraft/revision/unsavedtabs/Host5088FE3000;214tablenotsharedapplied,9oldmigrationsdone. TB185paused/defaultOFF/PendingAllocation. Await218result;olderCURRENTsuperseded.

## CURRENT —217 posted; real group money authority exists but is NOT wired —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-GROUP-GOODS-QUOTE-FACTS-217/RESULT.md`. 217 added ONE
read-only internal Order edge `ICheckoutShippingGroupGoodsFactsReader` (Application.Checkout.Contracts)
implemented on the EXISTING `CheckoutDirectory` and registered scoped to the SAME directory instance.
`PreviewAsync` was split into `LoadPreviewCartAsync` + `QuotePreviewSnapshotAsync` with byte-identical
legacy behavior (one authoritative whole-cart Pricing→Promotion→Tax quote, no persistence).
Server-validated complete partition of cart lines per group key (unknown/duplicate/shared/omitted/empty/
mixed-seller/untrimmed/duplicate-key fail closed), stale version → `PRICE_CHANGED`, one seller per group,
exact `_orderLineIdByCartLineId` join (no OfferId/proportional allocation), per group
Σ `TaxInclusiveSnapshot` = goods amount AFTER discount AND tax (99.00 / 198.00 worked example).
Currency must be sole and identical; negative authored amounts fail; explicit zero stays valid.
Physical `GroupWeightGrams`/`PhysicalItemCount` remain `null` (no authoritative measurement) and
`QuantityTotal` is metadata only.
Executed: build 0 errors, 9/9 new focused facts, 194/194 retained Storefront + infra organization/boundary
subset. No schema/migration/HTTP/MediatR/foreign Contracts/FE/flag/seed/user-draft change, no Host/FE
restart. **NOT WIRED**: nothing consumes the reader yet; no published/selected captured revision, no
fulfillment owner/type/membership, no projection/selection/commit, Storefront runtime unchanged.
Next single step after review: connect captured revision selection + these real group facts + the accepted
213 receiving calendar through projection/selection/commit; remaining gaps are publication/activation
ownership, fulfillment owner/type/membership and real measurement sources. Not another audit.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT. TB185 paused/default-OFF/PendingAllocation
unchanged; §29 decisions, 211 FE, 213/214/215/216 not reopened; Host5088/FE3000 and 9 applied migrations
preserved. Actual time exceeded the 25-minute box (recorded, not a process PASS).

## SUPERSEDED —216 boundedACCEPT; only217 real same-group goods amount authority —2026-10-25

Read216/ARCHITECT-REVIEW.md. Source5f7aed605ea0225888a7d66e13e51b8399402b9c exactcapturedContracts+verifiedGeo+scopedDIaccepted;worker9new/11legacy/23compatgreen. Source-reviewedbranches!=fullrealGeoexecution;time43>25notPASS. NoStorefrontconsumer/publishselectionyet.
ROOTFOUNDrealexistingmoneyauthority: CheckoutDirectory.PreviewAsync -> QuoteSellerOrdersAsync -> Pricing/Promotion -> Tax; TaxInclusiveSnapshot ispostdiscounttaxinclusivegoodslineamount. _orderLineIdByCartLineId isexactexistingmap. NoCartSubtotalExclusive substitution, noOfferIdjoin/proportionalallocation.
Onlyissued TB-SHIPPING-GROUP-GOODS-QUOTE-FACTS-217,parent216;25min/stop17. NewreadonlyinternalOrderpreviewgroupfactsreaderreusesSAMEquotepipeline, authenticcart/version/completepartition/cartlineidentity, pergroupsumalreadycomputedTaxInclusiveSnapshot; unknownphysicalmeasurementsremainnull. No losslycopy/schema/OrderliveShippingconsumer/flag yet. Nextwirecapturedselection+realgroupfacts+213calendaracrossprojection/selection/commit afterreview; noanotheraudit.
§29decisionsresolved,211FE119/213calendar/214capture/215manualquoteaccepted; no replay. Realuserdraft/revision/unsavedtabs andHost5088/FE3000preserved;214tablenotsharedapplied.9oldmigrationsapplied, no routineupdate. TB185paused/defaultOFF/PendingAllocation. Await217result;olderCURRENTsuperseded.

## CURRENT — 216 captured quote Contracts edge done / NOT connected — 2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-CAPTURED-QUOTE-CONTRACTS-EDGE-216/RESULT.md`. 216 exposes the
ACCEPTED 215 authority through a single scoped Contracts seam
(`IShippingCapturedRevisionQuoteLookup`) implemented once in Infrastructure and registered in
`ShippingModule.AddServices`. Exact owner+draft+revision (no latest/current/active), owner/edition
coherence from existing `ICurrentEdition`+`ICurrentTenant`, verified Geography path through
`IGeographyPlaceSnapshotReader` (active country/city, full active same-country admin chain, declared area =
city's real parent, service area belongs to city, duplicate/outside rows refuted, null admin city lawful),
one engine call, explicit Domain→Contracts map. Domain corrections executed: undetermined alternative ⇒
`MeasurementRequired` (known-outside control kept) and verified `AdministrativeAreaAncestors` so a stored
root area covers a deep city (ancestor+descendant groups = `GroupAmbiguous`). Executed: build 0 errors,
9/9 new edge cases, 11/11 exact 215 class, 23/23 exact compatibility class. No schema/migration/HTTP/
Order/FE/Host change. NOT CONNECTED to any customer path; owner key is data identity not permission;
caller money/measurements are not Cart-proven; no selected-capture publication and no projection/commit.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next slice = trusted owner + selected
captured revision authority and group financial/measurement authority, then projection/selection/commit).
Preserve real user draft/revision/unsaved work, Host5088/FE3000, 9 applied migrations; default-OFF,
TB185 paused, PendingAllocation unchanged; 213 calendar and 211 FE stay accepted-but-unwired.

## SUPERSEDED —215 boundedACCEPT; only216 exact-capture Contracts quote edge + verified destination —2026-10-25

Read215/ARCHITECT-REVIEW.md. fff92bb4552d0a9e5c48aa362ddd36c9059fb5cf manualengineextensionaccepted;worker11new+23compatpass. Same-rowfreight+packaging,3ANDaxes,taxinclusivegroupamountinput,Prepaid/CODsplit. No active/runtimeconsumer yet;Free/Carrierunavailable, EligibilityNotEvaluated.
Onlyissued TB-SHIPPING-CAPTURED-QUOTE-CONTRACTS-EDGE-216,parent215;25min/stop17. ThinShippingContractsrequest/result+scopedadapter exactowner/draft/rev, no latestfallback, actualGeographyparentclosure/nulladmin/inactive+identityvalidation, existingengineonly. Correctpotentialmissingalternativecase andverifieddeepadminpathinthissameedge task. Focusedtests, no anotheraudit/replay.
NOpublicpublish/pointer/Orderconsumer/sharedmigration/flagflip yet. Carttaxdiscountgroupamount/measurementauthority and activationselection stillopen, notsyntheticdefaults. §29userdecisionsresolved,213calendar/214capture/211FE119accepted, no reasking. Preserveactualdraft/revision/unsavedtabs,Host5088/FE3000,9appliedmigrations;214tableNOTsharedapplied. TB185paused/defaultOFF/PendingAllocation. Await216result;olderCURRENTsuperseded.

## CURRENT —215 captured manual quote primitive done / NOT wired —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-CAPTURED-MANUAL-QUOTE-215/RESULT.md`. 215 added ONE pure
additive capability on the EXISTING rate authority: `ShippingRatePolicyEngine.QuoteCapturedRevision(...)`
over an exact decoded captured revision, with its typed records in
`Domain/Rates/ShippingCapturedRevisionQuote.cs`. Legacy `Quote(...)` overloads are behaviourally unchanged.
Fail-closed order: identity (owner kind/tenant/seller + draft + revision) → stored settings required →
Manual only (CarrierApi/Free explicitly unavailable, never a zero price) → input sanity → ONE group by
stored area NodeKind+NodeId (0 = coverage missing, >1 = ambiguous, several areas in one group still one
group) → exact currency with no FX → three AND axes per row and alternatives across rows, half-open
`[min,max)`, blank axis unconditional, explicit 0 distinct from missing, invalid stored bounds/config fail
closed → exactly one row (0 = not matched or measurement required, >1 = ambiguous even with equal amounts)
→ freight AND packaging from that ONE row with the existing payer split (packaging always checkout, only
freight carrier-collectable). Result keeps group/row ids, draft/revision/format provenance, raw type id,
min+max delivery days, stored settings and stored week; `Eligibility` is always `NotEvaluated` — monetary
answer only, no activation/readiness/customer-eligibility claim.
Guard hygiene: `DisposeCaptureTransactionAsync` is the single named type-only member added to
`AllowedTypeMarkerMembers`; a begin inside it is still a violation and any other member mentioning the
type is still rejected (narrow negative proof executed). Executed: build 0 errors, 11/11 new quote cases,
exact `OperationalQuoteCompatibilityTests` class 23/23 (run once). No schema/migration/Contracts reader/
DI/endpoint/Order/FE/Host change, no shared DB, no user-draft write, no gate flip. Ambiguous rows and
invalid bounds are rebuilt with `with` because today's authoring guard refuses them; the evaluator fails
closed instead of resolving them.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT. Next concrete slice: a trusted-owner +
selected-captured-revision Contracts edge with the real group money/measurement authority, then
projection/selection/commit — not another audit. §29 decisions resolved; 213 calendar and 211 FE 119 tests
accepted and not replayed. Captures remain NOT LIVE and the 214 table is NOT shared-applied. Preserve the
actual draft/revision/unsaved tab, Host5088/FE3000 and the 9 applied migrations. TB185 paused, default-OFF,
PendingAllocation unchanged.

## SUPERSEDED —214-R2 boundedACCEPT; only215 captured MANUAL quote evaluation —2026-10-25

Read214-R2/ARCHITECT-REVIEW.md. Source8790165bd95bc48f64ecdc69a8b03d777edf3110 ownentrycleanup/dispose, actualstalectx/concurrency/sentinel/cancel/realuniqueproofaccepted;worker8/8realPostgres. Capturefoundationclosedforboundedbehavior, noR3loop. R2timeover12notprocessPASS. NewdisposalhelperneedsEXACTtype-onlyguardentry+narrowcompatcheckin215, nofileallowlist.
Onlyissued TB-SHIPPING-CAPTURED-MANUAL-QUOTE-215,parent214-R2;25min/stop17. ExtendexistingShippingrateenginewithpureexactcapture evaluation, 3ANDaxes/perrowfreightANDpackaging/PrepaidCODsplit/metadata, meaningfulfocusedtests. No readliveDraft/latestfallback, no lossyconversion/newpriceengine/Orderliveconsumer/publishpointer. Puretrustedinputonly; taxinclgroupamountcomesfromownerlater, no fakeweight.
Nextafterreview: trustedowner+selectedcapturedrevisionContractsedge and realgroupprice/measurementsource thenprojection/selection/commit connection. Notanothersourceaudit. §29decisionsresolved,213calendar/211119FEtestsacceptedandnotreplayed. CapturesNOTLIVE, tableNOTsharedapplied; preserveactualdraft/revision/unsavedtab, Host5088/FE3000 and9appliedmigrations. TB185paused/defaultOFF/PendingAllocationunchanged. Await215result;olderCURRENTsuperseded.

## SUPERSEDED —214-R2 exactcleanup + truthful concurrencyfacts executed; awaiting review —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R2/RESULT.md`
(and the appended correction note at the end of `214-R1/RESULT.md`). One existing member only:
`ShippingAuthoringDraftDirectory.CaptureRevisionAsync`.

Repaired in source: every non-committing path now detaches ONLY this method's attempted snapshot entry
(`DetachOwnedSnapshot`), so a later `SaveChanges` cannot publish a failed capture, and the caller's tracker
root/children are never cleared or detached. The exact-23505 catch now runs rollback → dispose OWN
transaction → `transaction = null` → detach OWN entry → **one** bounded winner recovery read, so no
completed EF `CurrentTransaction` survives; the `finally` releases any remaining own transaction.
Original exception and caller `CancellationToken` identity preserved; one transaction member only, so
`SaveAuthoringReplacementAsync` semantics, the exact unique identity filter and all no-write preflight
rejections are untouched. Capture still returns provider-persisted facts; historical reader unchanged.

Two overclaimed 214-R1 facts are now real: the stale-context success capture executes through the ACTUAL
stale context (old root/child identities, old revision, two old rows preserved while the payload is B's
full graph), and the duplicate-capture fact uses `Task.Run` + a bounded gate with an asserted arrival
count of exactly 2. Added three executed failure facts (sentinel before the capture save; real caller
token cancelled at the write boundary; a REAL 23505 caused by an independent task-owned winner committed
between the idempotent pre-check and the INSERT) each proving no row, no pending attempted entry and a
later `SaveChanges` of 0; the unique case also proves `Database.CurrentTransaction == null` and returns the
winner id.

Executed only on the task-owned disposable `127.0.0.1:15432` (`shipping_tb214` / `tb214`, existing local
postgres:17, container removed, port verified silent): build 0 errors, 8/8 storage facts. No shared
migration, no schema/domain/API/reader/flag change, no user-draft write, no Host/FE restart, no browser or
HTTP write. Timing exceeded the 12-minute box (recorded, not a process PASS).

Staged capture is still NOT live: no publish route, no live pointer, no Order/Storefront consumer,
default-OFF gate, TB185 paused, PendingAllocation unchanged. Rejected: mapping the capture onto legacy
aggregates / lossy partial publication. Active task NONE; next task NOT ISSUED — STOP_FOR_ARCHITECT.
Preserve real user draft/unsaved tab, Host5088/FE3000 and the 9 applied migrations; 213 calendar remains
accepted-but-unwired, 211 FE 119 tests and §29 owner decisions are not reopened.

## SUPERSEDED —214-R1 partialrepairs; only214-R2 exactcleanup + truthful concurrencyfacts —2026-10-25

Read214-R1/ARCHITECT-REVIEW.md. Rootobservedmainfixes but testsoverclaim: B-tokenfreshcapture usesNEWfreshInA notactualstaleA;duplicatecapturebarrierblocks synchronouslybeforeawait and ignoresSignalAndWaitbool. CaptureownAddedentrysurvivesrollback(false);exactunique catchnullstransactionwithoutDisposebeforeread. Fixonlythose, no newarchitecture.
Only issued TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R2,parent214-R1;12min/stop7/setup<=3. Existingisolated15432target; no schema/newAPI/flags/userdata/sharedmigration/Hostrestart. Trueconcurrenttasks+gatearrivalassertions, actualstalecontext, fault/cancel/uniquerecovery leavescallerstatebutnoattemptedentry/laterSave0. Awaitresult; no215yet.
43/43+18/18workerreportednotblanketproof;R1time48>20recorded. §29usersemanticsresolved;213calendar/211FE119accepted, no replay. CaptureNOTLIVE, no lossylegacyconversion/defaultflip. Preserveactualdraft/unsavedtab,Host5088/FE3000,9appliedmigrations;TB185paused/PendingAllocation. OlderCURRENTsuperseded.

﻿## CURRENT —214-R1 executed; capture correctness + real provider proof, awaiting review —2026-10-25

Read214/ARCHITECT-REVIEW.md then214-R1/RESULT.md. 214 defects repaired: capture now loads a complete
UNTRACKED graph while the root FOR UPDATE lock is held (identity map never consulted), refuses a caller with
an unsaved tracker BEFORE any transaction without clearing it, re-reads the persisted row inside its own
transaction so the first answer is provider truth (JSONB/timestamptz), matches SQLSTATE23505 + the EXACT
constraint/table/schema for idempotency, and adds exact GetCapturedRevisionAsync (owner+draft+revision, no
latest fallback). Codec rejects missing/incoherent identity and body-vs-rowheader mismatch; no fabricated
defaults and an empty draft is still a valid incomplete capture. The 214 storage fact's two-group Single()
gap is fixed (intended group selected by real server id) and the full typed roundtrip is proven.
Real PostgreSQL proof executed ONLY on the task-owned 127.0.0.1:15432/shipping_tb214/tb214 (postgres:17
existing local image, parsed+live-verified, container removed, port silent). 55432 family helper untouched; no
shared migration, no user-draft write, no Host/FE restart. Focused: 43/43 domain+guard+target-isolation, 18/18
storage+isolation, build0 errors. Timing ~48min vs 20 box = recorded deviation, not PASS on process.
REJECTED again (recorded in evidence): mapping the capture onto legacy aggregates with UnrepresentableAxes[];
partial lossy publication stays forbidden. Staged capture is NOT LIVE: no pointer, no publish route, no Order
consumer, gate default-OFF, TB185 paused, PendingAllocation unchanged. Active task NONE; next task NOT ISSUED
(STOP_FOR_ARCHITECT): a reviewed publish/selection design that does not flatten into legacy, plus the
same-group-after-discount-AND-tax amount authority, activation semantics and seller/marketplace activation
authorization. Preserve real user draft/unsaved tab, Host5088/FE3000, the 9 applied migrations and the 119
accepted FE tests. 213 calendar stays accepted-but-unwired; §29 decisions remain resolved, do not reask.
## CURRENT —214 PARTIAL; only214-R1 capture correctness + isolated providerproof —2026-10-25

Read214/ARCHITECT-REVIEW.md. Source747036455 notacceptedforintegration. RootfoundstaleEFtrackedgraphdespiteDBlock, dirtytrackerSaveleak, firstreturnJSONB/time notprovider-normalized, substringconstraintcatch, missingexacthistoricalreader. Storagefactseeds2groupsbutSingle() so cannotbeclaimedgreen; onlydomain28green, relationalblocked. Codecidentitydefaultgap. 70minvs25timedeviationrecorded.
Only issued TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R1,parent214;20min/stop12. Scopedcorrections+realPostgresonTASKOWNED127.0.0.1:15432/shipping_tb214/tb214 parsedandliveverified, setup3minmax. Existing55432helperunchanged; no arbitrarysharedtarget/fallback/netsh/portreset. NOsharedmigration/runtimechange. Awaitresult; no215yet.
Oldcapture reader mustworkafterdraftadvances; allpersistedfields/ownerskept, dirtycallerworknevercleared/saved. No broadtests/genericarchitectureloop. Userneedsquickcorrectfinish, don't repeat70min.
Do NOT mapcaptureinto legacy via warnings (againrejected). No publication/livepointer/Orderconsumer. Calendar213acceptedbutnotwired;119FEtestsandcontrastaccepted. §29userdecisionsresolved; no reasking. Preserveactualuserdraft/unsavedtab, Host5088/FE3000,9alreadyappliedmigrations, defaultOFF/TB185paused/PendingAllocation. OlderCURRENTsuperseded.

## CURRENT —213 calendar accepted; only214 immutable owner-scoped revision capture —2026-10-25

213source27a8f834005cabfbf6ecfbd5e0dc7b21c2d64298 ACCEPT_BOUNDED_CALENDAR_PRIMITIVE,worker11/11+build; NOTWIRED. Read213/ARCHITECT-REVIEW.md for horizons count-vs-span,timezone/zerooffset/maxduration limits. Userdecisions§29 remain: receiptfromorder,closedskip, samegroupgoodsAFTERdiscountANDtax, authoredweekreceivingnotpickup. Do not reask.
Only issued TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214,parent213;25min/stop17. Code:completeimmutableShippingownedrevisioncapture+strictowner/revisionreader, no livepointer/publicpublishroute/Orderconsumer. Store ALL authoredfacts including translations/type/rowpackaging+amountbounds/deliveryrange/week/settings; do not flattenlegacy or blanketrejectSeller. Existing owner/editionauthorization reused. This is capturedcandidate, NOT activation/readiness proof. No anotheranalysis task.
No liveDB migration/runtime changes until214review; disposableonly DB proof withinbox ifavailable. StageEXACTpaths, no gitadd-A or historicalhelperexecution. Preserveactualuserdraft/revision/unsavedtabs, Host5088/FE3000,9existingmigrations,119acceptedFEtests. TB185paused/defaultOFF/PendingAllocation unchanged. Await214result; olderCURRENTsuperseded.

## CURRENT — user runtime semantics resolved; only213 customer receiving calendar calculation — 2026-10-25

User clarified: delivery1–3days is CUSTOMER RECEIPT promise from order, prep+carrier inside it; amountcondition uses samegroup final goods amount AFTER discount AND tax; authoredweek is CUSTOMER RECEIVING availability, not legacycarrierpickup. Canonicalrules§29 records exact statements and limits. Do not reask/revert these. Examplemin2workingdays: nextactive day#1, nextactive#2 earliest; skipclosed, use configured dayhours.
Only issued TB-SHIPPING-STOREFRONT-CUSTOMER-RECEIVING-CALENDAR-213,parent212-R1;20min/stop12. Add pure calculation to existing OrderStorefrontShippingCalculator with explicit validatedweek/timezone/reference date/min+provided horizon; no current consumer/gate/default change yet. Focusedcalendar tests only. Not withdrawn old snapshotcopy213; this is a DISTINCT exactTaskID. No new publicationtables/pointers/flags/engine.
Preserve maxdeliverydurationmetadata; no cutoff/slot-size/holiday/capacity policy guessed. No runtime/daypromise fallback to legacy pickup. Next afterreview: wire truthful owner-scoped published/config seam and projection/selection/commit, not generic audit.
211UIcontrast/119tests accepted, no replay. Real draft/currentrevision/userunsavedtab and Host5088/FE3000 preserved;9migrations applied. Userlocaldev authorization persists. DefaultOFF/TB185paused/PendingAllocation unchanged. Await213result; olderCURRENTsections superseded.

## CURRENT — 212-R1 facts accepted; THREE product semantics needed, no active worker — 2026-10-25

Read212-R1/ARCHITECT-REVIEW.md. Corrected runtimeDI/adapters accepted; lossy legacy conversion rejected. Snapshot alternative is NOT implementation approval: moving livepointer beforeactivationdefined, omittingtranslations/type and blanketrejectingSeller/Marketplace not accepted. NO213 issued. ActiveNONE/awaitingworkerNONE; next is productanswers, not anotheraudit.
Canonical rules6/8 explicitly defer runtime bases. Ask user only:
1 authored method week = dispatch/working hours or customer's receiving hours?
2 min/max delivery days counted from handoff/dispatch or from orderconfirmation (sellerprep is separate)?
3 merchandise-condition amount for SAME shippinggroup = afterdiscountbeforetax, beforediscountbeforetax, or aftertax?
Don't reask independent modes/free definition/owner authorization (alreadyapproved rules2/4/7/13/18). Recommend explicit publication separate from SAVE as architecture, no live activation yet. Onceanswersarrive recordthem in canonicaldecision, then one executable integration slice, no analysisloop.
211contrast +119/119all14featuretests/typecheck/7guards accepted; no replay/retestrequest. ExistingStorefront shipping exists, newdraftintegrationpending. Userneedsfastdelivery; don't hide gaps or invent policies.
Preserve realdraft01a16eeb-70b3-7000-87c2-564c82835496/currentrevision/userunsavedwork; Host5088/FE3000 and9appliedmigrations. Localdev authorizationpersists, no productionclaim;TB185paused/defaultOFF/PendingAllocation unchanged.
212-R1 gitadd-A included old executablepost-result.js/receipts; preservehistorybutNEVERexecute. Futurestageexactpaths. OlderCURRENTsections below superseded.

## CURRENT — 212 publication proposal REJECTED; only212-R1 exact source correction — 2026-10-25

Root independently read ShippingModule+OperationalQuoteLookup: packaging/operational implementations and scopedDI ALREADY EXIST.212 claimed none: false. Root also read operational contract: week means carrierpickup, not customer delivery promise. Don't duplicate adapters or implement proposed213 snapshotcopy.
Read212/ARCHITECT-REVIEW.md. Other blockers: per-row packaging cannot flatten onepolicy/currency, owner != geography and tenantcontext isolation exists, IsActive flags don't prove snapshot provenance/rollback, partial publication of unsupported delivery/amount/mode is forbidden. Store-only restriction is not approval for Marketplace/Seller architecture.
Only issued TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212-R1,parent212;10min/stop6, source-only targetedcorrection. NO213implementation authorized. Await result, then one concrete safe runtime implementation slice, not generic audit. Runtime gateOFF/TB185paused remain.
211119/119featuretests/typecheck/7guards and contrast accepted; no replay. Actual draft/user unsaved data, Host5088/FE3000 and applied9migrations preserved. Prior explicit localdev authorization persists. User needs quick correct integration, not unsafe partial price. Older CURRENT headings below superseded.

## CURRENT — 211 accepted; only212 runtime bridge contract/reuse decision — 2026-10-25

211 contrast accepted after source+desktopPNG review. Worker119/119all14recursive feature files, typecheck clean, guards7/7; one exact stale gram-label assertion correction. Mobile screenshot shows only first area (not proof of both colors), no extra screenshot-only task. User said better; User Visual ACCEPT not blanket granted.
Read211/ARCHITECT-REVIEW.md. Important runtime correction: StorefrontShippingService already calls gated StorefrontShippingOwnedRateResolver -> Shipping.Contracts.Rates.IShippingRateQuoteLookup; runtime is NOT config-only. Must reuse existing seam, no second engine. New authoring draft type/areas/fees/week not integrated. 'Read-only activation' and foreign Fulfillment-owned publication state NOT approved.
Only issued TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212,parent211;12min/stop8,source-only. Trace existing authority/owner/currency/measurement/week and approve one concrete minimal implementation plan from source; no code/flags/data/runtime changes. Await212 result; next implementation chosen after review, not another generic audit.
Live draft01a16eeb-70b3-7000-87c2-564c82835496, actual revision/user unsaved work preserved. Host5088/FE3000 running;9migrations already applied. Prior local nonproduction authorization persists.190–211 don't replay; TB185paused/defaultOFF/PendingAllocation unchanged. Older CURRENT headings below are superseded history.

## CURRENT — user says UI better; task211 alternating delivery areas + editor checks — 2026-10-25

User feedback: "better"; requested alternating white/very-light-gray DELIVERY AREA cards so adjacent areas are distinguishable, requested fast tests/all editor parts working, asked whether Storefront is done. This is bounded positive feedback, not blanket production/visual certification.
Only issued TB-SHIPPING-ADMIN-AREA-CONTRAST-AND-EDITOR-CHECKS-211,parent210-R1;20min/stop12. Minimal card background by current display position; preserve all accepted alignment/behavior. Run ALL feature tests recursively (include nested api/model/session), one typecheck, existing four boundary guards, short actual two-area local-unsaved visual inspection. No business write/reset/restart or broad backend suite. Source-only trace of draft->runtime/storefront seam, exact implemented/missing facts; no unsolicited activation/quote implementation.
Current Storefront integration of this NEW authoring flow has NOT been accepted/delivered; don't imply existing storefront/checkout is absent. TB185paused/defaultOFF/PendingAllocation persists. Await211 result; no second task meanwhile.
Live URL http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Preserve actual draft revision/user edits, Host5088/FE3000 and applied9migrations. Standing local-dev authorization persists.190–210 functional/visual bounded reviews remain accepted; no replay. Read TOP recovery first in a new chat. Older CURRENT sections below are superseded history.

## CURRENT — 210-R1 reviewed; ready for USER VISUAL RETEST — 2026-10-25

Architect ACCEPT_BOUNDED_WEEK_PRESENTATION: source58f484e83b6b2665e4d2a3bb92cd166cd3ccb574 plus all5 actual PNGs independently viewed; read210-R1/ARCHITECT-REVIEW.md. Desktop common widths/tabs/seven-row alignment210 accepted; mobile paired labelled bounds and truthful view columns now complete, FA/EN actual week evidence inspected. User Visual ACCEPT PENDING; prior10/20 rejection is not overwritten by self-grading.
Active task NONE. Awaiting worker NONE. Next automatic task NONE. Call user for visual retest now; no proof-loop tasks or190–210 replay.
URL http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Preserve current content/revision/unsaved user browser. Worker observed no HTTP writes, fees120000.00/5000.00 unchanged; actual revision not independently re-proven. Prior209 real save accepted; do not replay it. Host5088/FE3000 remain preserved; historical PID10800/19108 must be freshly verified only if runtime trouble. All9 migrations208 already applied, no routine reapply/rebuild/reset.
Standing local nonproduction authorization persists, no repeated approval. No quote/activation/whole-method-create/production/certification claim; TB185paused/defaultOFF/PendingAllocation unchanged.210 timing32>20 remains deviation;R1 typecheck twice disclosed, no broad sweep. Older CURRENT headings below are superseded history. Continue only from actual user feedback.

## CURRENT — 210 desktop accepted; only 210-R1 mobile week completion — 2026-10-25

Architect viewed all four210 PNGs and source. Desktop common widths/tab geometry/seven row columns accepted within scope; User Visual ACCEPT remains PENDING after prior10/20 rejection.
Actual mobile file shows TOP General, not week; source has three lines/day and no visible mobile From/To labels. View puts time range under Status with empty From/To cells. EN evidence absent.
Only issued task TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210-R1,parent210: complete paired labelled mobile times, truthful view columns, actual week screenshot at390 plus bounded EN/LTR check; preserve desktop. Hard12min/stop7. No save/domain/backend/runtime restart/proof replay.
Read210/ARCHITECT-REVIEW.md and task. Await210-R1 result; do not issue another task meanwhile. Preserve real draft01a16eeb-70b3-7000-87c2-564c82835496, actual revision/unsaved work, Host5088 and FE3000. Local-dev authorization persists.190–209 functional accepted, do not replay. TB185paused/defaultOFF. Older CURRENT sections below are historical and superseded.

﻿# Tooba — Bridge-Wake-V1 Recovery Start

## CURRENT TASK —210 / user rejected alignment10/20 —2026-10-25

Only issued TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210,parent209-R2;20min/stop13.
Actual user screenshots show General/Regions width mismatch, unaligned tabs and weekday status/time controls.
Read TOP recovery+task; frontend feature skill/standard. One common form grid, equal tab geometry,
seven aligned week rows with fixed day/status/start/end columns; existing design-system only.
Matcheddesktop/mobile screenshot +DOM box evidence, one typecheck; no fullsuite/save/backend/data writes.
User Visual ACCEPT remains REJECTED/PENDING retest; functional209 accepted, do not reopen it.
Preserve current user draft/revision/unsaved browser edits, Host5088/FE3000. Await210 result.



## CURRENT HANDOFF —209-R2 ACCEPTED / USER RETEST —2026-10-25

Read TOP canonical Shipping recovery and209-R2/ARCHITECT-REVIEW.md first after chat loss.
209-R2 sourcecebc455147e779245c7317a2106afce041e885e5 accepted for bounded nested-rate error placement.
Live save/readback209 and visual/view/discard repairR1 remain accepted within their limits.
Active task NONE; awaiting worker result NONE; next task NONE. Call user for retest NOW; no proof-loop task.
URL http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Preserve actual user data/revision and Host5088/FE3000; no reapply/rebuild/reset as routine recovery.
User Visual ACCEPT/production/quote/activation/full-method-create not claimed.
Group-level banner/focus, index-after-reorder and picker/General-view presentation limits are in review.
Explicit local-dev authorization persists, no repeated approval. Await actual user feedback.
Older CURRENT/TASK headings below are historical and superseded.



## CURRENT TASK —209-R2 / exact nested error targeting only —2026-10-24

Only issued TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R2,parent209-R1;10min/stop6.
R1 presentation/view improvements accepted after Architect viewed all4screenshots; not userVisualACCEPT.
Actual remaining bug: groupIndex discarded; row errors/focus/dedup rowIndex-only and globalNth.
Fix composite group+row ownership +open targeted optional disclosure before focus; focused tests/typecheck only.
No UI rewrite/browser/save/data/migration/runtime/proof replay. After corrected result review, user retest;
no automatic visual/proof follow-up. Read TOP recovery and209-R1/ARCHITECT-REVIEW.md.
Prior localdev authorization persists; preserve populated draft/backend/FE.



## CURRENT TASK —209-R1 / live save accepted, UI incomplete —2026-10-24

Read TOP canonical recovery +209/ARCHITECT-REVIEW.md.
Only issued task TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R1,parent209;30min/stop22.
209 live save/readback accepted; user Visual ACCEPT absent. Architect viewed actualdesktopPNGs:
view-mode rate inputs/delete still rendered; requested compact professional layout not delivered.
Repair mode/busy mutation guards, compact General/Regions, optional condition organization,
exact description/nested delivery error targeting and inline feedback. Same feature/design-system.
No backend/migration/seed/new draft/repeat save; preserve current real populated draft/runtime.
Await209-R1 result; no full suite/proof replay. Timing209~63min contradicts45 claim; retain honestly.



## CURRENT TASK —209 / user rejected authoring UX and save —2026-10-24

Only issued task TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209, parent208;45min/stop35.
User screenshots/report show failed save, unusable destination/type experience and confusing "group" UI.
208 acceptance is read/runtime readiness ONLY; current UI is NOT user-accepted.
Read TOP recovery and task; frontend feature skill/architecture/rules mandatory.
Repair the real destination+fee+save path, actionable validation, "محدوده ارسال" terminology,
compact existing design-system layout and lawful timezone/type selection. No broad rewrite/new framework.
Must prove actual ONEPUT then real freshGET/reload with saved values, not just mount.
Explicit local-dev authorization persists. Preserve user edits/content; no replay of applied migrations or190–208 proof.
Await209 result before next task; report true blocker if not fixed, never premature READY.



## CURRENT HANDOFF —208 ACCEPTED / REAL LOCAL USER TRIAL —2026-10-24

Read TOP canonical Shipping recovery and208/ARCHITECT-REVIEW.md.
208 source f2fa49d5432d38663aa67ea057583c51bf0bb01e accepted for real existing-draft READ trial.
Active task NONE; awaiting result NONE; next task NONE. Call user now, do not create proof-loop tasks.
URL http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Real minimal draft post/null, Store, empty groups; actual type catalog empty, settings/week unconfigured.
9 migrations applied; Host5088 current binary left running in D:\temp\tb207-build\out,
launcher D:\temp\tb208-host\run-host.cmd; FE3000 preserved. Verify fresh PIDs if troubleshooting.
Live Apply persistence NOT exercised; current state is read+mount+cleanEdit/Cancel readiness.
Explicit user NONproduction local execution authorization persists: no repeated approval.
owner.ownerKind wire fix accepted; historical synthetic owner.kind fixtures not current wire proof.
No rebuild/reapply/reset/replay190–205 as routine recovery. Await actual user feedback.
Older CURRENT/ISSUED headings below are superseded history.



## CURRENT ISSUED TASK —208 / explicit local-development execution authorized —2026-10-24

Only issued task: TB-SHIPPING-ADMIN-LOCAL-STACK-REFRESH-AND-USER-TRIAL-208, parent207.
User confirms NON-production local DB and explicitly authorizes necessary checked-in dev migrations,
Host5088 refresh and normal existing startup seed/backfill/workers, minimal ONE trial draft via real API if absent,
and narrow relevant integration repair. Do not ask again for these same actions or retain old approval holds.
40min hardbox/implementation stop30; reuse207 build, preserve frontend3000, keep successful Host running.
Read TOP recovery and task. Target verification still required; no external/production/reset/drop/auth bypass.
Await208 result; user is called to test only after actual real API+mounted route readiness.
Old207 approval-pending paragraphs below are superseded historical context.



## CURRENT: 207 reviewed, schema approval pending — 2026-10-24

Read TOP canonical Shipping recovery and207/ARCHITECT-REVIEW.md + EXECUTION-PLAN.md.
No worker task active/issued. Await approval for nine named LOCAL schema migrations only, conditional
on full pending-set equality and backup/target checks. Host restart/seed is NOT approved.
Runner has no module filter; do not run generic apply before checking all descriptors.
Old task pointers below are historical. Existing-draft live readiness remains pending.


## CURRENT ISSUED TASK — 207 — 2026-10-24

Only issued task: TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207 (parent206).
206 blocker diagnosis accepted: stale Host5088 binary; live auth/schema/draft readiness unverified.
207:20min/stop14, read-only actual migration history + startup effects + ONE isolated currentHost build,
concrete refresh/rollback plan. NO shared restart, migrations/seeds, database writes or new production code.
Program.cs Development startup performs migration/seeds even with legacy bootstrap disabled.
Read TOP canonical recovery and206/ARCHITECT-REVIEW.md. Await207 result, no replay190–205.

## Historical task — Shipping206 — 2026-10-24

Only issued task: TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206.
Read the TOP canonical Shipping recovery and AGENTS. Real existing-draft read-only readiness,
20min/stop14; await its result before another task. Prior USER_TRIAL/NONE checkpoint is superseded.
No replay of accepted190–205, no merchant writes/migrations/shared restart.

## Prior Shipping checkpoint — 2026-10-24

Read the TOP checkpoint in [SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md](../planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md), then its 205-R3/R2 Architect reviews and root AGENTS.md.
Last Architect acceptance: TB205-R3; acceptance commit `750b4d73e1e3561d53ddd7b2e7170136e2933f5a`.
Current phase: USER_TRIAL_EXISTING_DRAFT_AUTHORING. Active task=NONE; awaiting worker result=NONE.
199–204 and bounded 205-R2/R3 are accepted; do not repeat their work or issue full-matrix proof tasks.
Next objective is user trial / real-environment connectivity for an existing authorized draft; live runtime/data/registry acceptance is pending.
Actual entry: existing method View → General → authorized draft link; route `/admin/shipping-services/authoring/[draftId]`.
No automatic draft create, deployment, live merchant mutation or runtime quote is authorized here.
The TOP recovery section contains the complete accepted ledger, remaining limits, runtime facts and continuation rules.
Everything below is historical project context, NOT the current phase/task. Fetch fresh main before proceeding.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
Current Phase: P07 — Advanced Catalog
Last Architect Accepted Gate: TB-P05-GATE
P05: COMPLETE
P06: COMPLETE
P07: IN_PROGRESS
Last Architect Accepted Task: TB-P06-T029
Last Implementation Task: TB-P07-T002
TB-P06-T011 = ACCEPTED
TB-P06-T011-R1 = ACCEPTED
TB-P06-T011-R2 = ACCEPTED
TB-P06-T011-R3 = ACCEPTED
TB-P06-T012 = ACCEPTED
TB-P06-T013 = ACCEPTED
TB-P06-T014 = ACCEPTED
TB-P06-T015 = ACCEPTED
TB-P06-T016 = ACCEPTED
TB-P06-T016-R1 = ACCEPTED
TB-P06-T017 = ACCEPTED
TB-P06-T018 = ACCEPTED
TB-P06-T019 = SUPERSEDED_BY_ARCHITECT_RESCOPE
TB-P06-T019-R1 = ACCEPTED
TB-P06-T020 = ACCEPTED
TB-P06-T021 = ACCEPTED
TB-P06-T022 = ACCEPTED
TB-P06-T023 = CLOSED_BY_REPAIR
TB-P06-T023-R1 = ACCEPTED
TB-P06-T024 = REPAIRED_BY_TB-P06-T024-R1
TB-P06-T024-R1 = FUNCTIONALLY_COMPLETE
TB-P06-T024-R2 = ACCEPTED
TB-P06-T025 = ACCEPTED
TB-P06-T026 = REPAIRED_BY_TB-P06-T026-R1
TB-P06-T026-R1 = ACCEPTED
TB-P06-T027 = ACCEPTED
TB-P06-T028 = REPAIRED_BY_TB-P06-T028-R1
TB-P06-T028-R1 = ACCEPTED
TB-P06-T029 = ACCEPTED
COMMERCIAL_READINESS_GATE = PASSED
TB-P07-T001 = SUPERSEDED
TB-P07-T001-R1 = SUPERSEDED
TB-P07-T001-R2 = SUPERSEDED
TB-P07-T001-R3 = SUPERSEDED
TB-P07-T001-R4 = SUPERSEDED
TB-P07-T001-R5 = ACCEPTED
TB-P07-T002 = AWAITING_ARCHITECT_ACCEPT
CURRENT_FOCUS = PROFESSIONAL_DATA_GRID
CANONICAL_GRID = APP_OWNED_AG_GRID_COMMUNITY_WRAPPER
AG_GRID_ENTERPRISE = FORBIDDEN
SERVER_GRID_QUERY_CONTRACT = LIVE
FIRST_REAL_ADMIN_INTEGRATION = LIVE
CATEGORY_ATTRIBUTE_SCHEMA = LIVE
CATEGORY_ATTRIBUTE_INHERITANCE = LIVE
PRODUCT_TYPED_ATTRIBUTES = LIVE
PRODUCT_VARIANT_AXES = LIVE
VARIANT_COMBINATION_FOUNDATION = LIVE
FULL_VARIANT_MATRIX = DEFERRED
FACETED_SEARCH_INTEGRATION = DEFERRED
SUPPORT_BACKEND = LIVE
CUSTOMER_TICKETS = LIVE
SELLER_TICKETS = LIVE
ADMIN_SUPPORT = LIVE
SUPPORT_DEMO_SEED = LIVE_DEV_ONLY
SUPPORT_USER_PREVIEW = READY
WALLET_LEDGER = LIVE
GIFTCARD = LIVE
CUSTOMER_WALLET_UI = LIVE
ADMIN_WALLET_UI = LIVE
WALLET_DEMO_SEED = LIVE_DEV_ONLY
WALLET_BROWSER_PROOF = LIVE
WALLET_USER_PREVIEW = READY
WALLET_CHECKOUT = LIVE
WALLET_CHECKOUT_FULL_PAYMENT = LIVE
REFUND_TO_WALLET = LIVE
WALLET_MIXED_TENDER = DEFERRED
WALLET_CHECKOUT_BROWSER_PROOF = LIVE
REFUND_TO_WALLET_BROWSER_PROOF = LIVE
WALLET_CHECKOUT_USER_PREVIEW = READY
ACCESS_CONTROL_CENTER = LIVE
ACCESS_CONTROL_DEMO_SEED = LIVE_DEV_ONLY
ACCESS_CONTROL_BROWSER_PROOF = LIVE
ACCESS_CONTROL_USER_PREVIEW = READY
PRODUCT_SALE_FLOW = LIVE
SELLER_PRODUCT_MANAGEMENT = LIVE
SELLER_OFFER_MANAGEMENT = LIVE
PRICING_PATH = LIVE
INVENTORY_PATH = LIVE
CART_CHECKOUT_PAYMENT = LIVE
SELLER_ORDER_FULFILLMENT = LIVE
CUSTOMER_ORDER_TRACKING = LIVE
ADVANCED_VARIANT_ARCHITECTURE = DEFERRED
SELLABLE_PRODUCT_FLOW_LIVE = YES
SELLABLE_DEMO = YES
PRODUCTION_PAYMENT_FOUNDATION_READY = YES
REAL_PSP_PROVIDER_CONFIGURATION_REQUIRED = YES
REAL_BANK_PAYMENT_PROVEN = NO
PRODUCTION_GO_LIVE_READY = NO
PUBLIC_LOCALE_ROUTING = PREFIXED
MULTILINGUAL_SEO = LIVE
HREFLANG = REAL_VARIANTS_ONLY
UNPREFIXED_PUBLIC_URLS = REDIRECTED
PAGE_COMPOSITION = LOCALE_ROUTE_COMPATIBLE
STORY_LIVE = WITH_EXACT_SHOPEIVA_UI
SHARED_STORY_MANAGEMENT = LIVE
COMMERCIAL_PANEL_WAVE1 = LIVE
COMMERCIAL_PANEL_WAVE2 = LIVE
SELLER_PROMOTIONS = LIVE
SELLER_REVIEWS = LIVE
ADMIN_PROMOTIONS = LIVE
ADMIN_REVIEWS = LIVE
TRANSACTIONAL_NOTIFICATIONS_LIVE = YES
REALTIME_NOTIFICATIONS = DEFERRED
FAKE_NOTIFICATIONS = FORBIDDEN
PANEL_NAVIGATION = HONEST_LIVE_ONLY
PRESENTATION_CRITICAL_FAKE_ACTIONS = FORBIDDEN
BLOG_CONTENT = LIVE
STOREFRONT_UI = LIVE_AUDITED
CUSTOMER_UI = LIVE_AUDITED
SELLER_UI = LIVE_AUDITED
ADMIN_UI = LIVE_AUDITED
CUSTOMER_PANEL = LIVE_AUDITED
SELLER_PANEL = LIVE_AUDITED
ADMIN_PANEL = LIVE_AUDITED
MULTILINGUAL_UI = AUDITED
RTL_LTR = AUDITED
MULTILINGUAL_SEO = AUDITED
VISUAL_CONTRACT = SHOPEIVA_LOCKED
NEW_UI_RULE = SOURCE_DERIVED_NATIVE_FIT
COMMERCIAL_UI_READINESS = AUDITED
FAKE_DATA_ACTION_AUDIT = AUDITED
SETTLEMENT_BACKEND = LIVE
RETURNS_BACKEND = LIVE
AUTHORIZATION = SPICEDB_REBAC
HOME_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
PDP_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
PRODUCT_FULLY_READY = NO
```

Evidence for current task: `docs/evidence/TB-P07-T001-R5/` (Admin+Seller UI/UX polish; Shopeiva vendor fidelity; DataGrid/AC UX; manual visual supervision). Flags: T001/R1/R2/R3/R4 = SUPERSEDED; R5 = AWAITING_ARCHITECT_ACCEPT; `CURRENT_FOCUS` = UI_UX_ONLY; `ADMIN_UI` = PRIORITY_1; `SELLER_UI` = PRIORITY_2; `BACKEND_FEATURE_EXPANSION` = FROZEN; `CATEGORY_ATTRIBUTE_*` / `PRODUCT_VARIANT_AXES` / `VARIANT_COMBINATION_FOUNDATION` = LIVE (kept); `FULL_VARIANT_MATRIX` = DEFERRED; `FACETED_SEARCH_INTEGRATION` = DEFERRED; `VISUAL_CONTRACT` = SHOPEIVA_LOCKED. Runtimes: Host `:5088`, FE `:3000`, Shopeiva `:3001`. Do **not** claim `USER_VISUAL_ACCEPTED`, `PRODUCT_FULLY_READY`, `PRODUCTION_GO_LIVE_READY`, `SELLER_PANEL_COMPLETE`, or `FULL_VARIANT_MATRIX_LIVE`.


