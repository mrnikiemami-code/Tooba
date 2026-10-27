# CURRENT —230 positive read ACCEPTED; narrow230-R1 failure/display correction issued —2026-10-27

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

# CURRENT — 230 EXECUTED: captured destination refresh + ONE real configured priced read — 2026-10-27

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
pending the cards show `storefront.captured_preview.destination_pending` instead of the old quote. Legacy OFF
short-circuits to `skip` forever — unchanged. Captured mode now shows `shippingDaysLabel = "—"` (line 648),
never the legacy delivery promise; no aggregate min/max is invented across independent groups.
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

C — real page `http://127.0.0.1:3000/fa/shipping` in the task-owned Chrome (CDP 9336, FE 3000 PID 28288
untouched, Host 5088 PID 24660 `D:\temp\tb230-build\out`). Choosing province/city through the existing UI
selects (no method selector) sent the destination POSTs and 200 returned a PRICED group:
`cap|platform|store|...|heavy`, type سنگین, real goods `9485771` after discount+tax, freight `120000.00`,
packaging `5000.00`, payable `125000.00`, IRR, earliest `2026-10-29`, dates 10-29,10-31,11-01..11-05
(Friday 10-30 skipped by the authored week, min 2, no extra seller prep), snapshot
`01a17d0e-0419-7000-b575-f507d2f89abc`, while the second group stayed visible and refused
(`storefront.captured_preview.unassigned`). Clicking the 10-31 chip moved selection to that chip and showed
that date's own interval; reload kept the authored data and the normal session. Card scrolled to
`block:'center'` (top 59 / bottom 786 in 844, intersects), `scrollWidth == 390 == clientWidth`, legacy
duplicate cards `0`. Screenshots 1440×900 and 390×844 at `.../230/screens/` inspected by eye.
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

# CURRENT — 229-R2 EXECUTED: authenticated live read OBSERVED (1440/390, multi- and single-line) — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-AUTHENTICATED-READ-TRIAL-229-R2/{RESULT.md,TASK.md}.
Base `e7bc126f4c44499e113fdb357854006666eb2efb`. Status: **EXECUTED — live read OBSERVED**, no source change
needed, NOT accepted, NO user Visual ACCEPT, nothing activated for users.

A — the R1 blocker is CLOSED as authentication only, and the misleading log is explained: `TenantResolutionMiddleware`
wraps its whole `_next(httpContext)` in the same `catch (SemanticException)` used for resolution failures, so the
identity gate's `checkout.authentication_required` (401, `ActorId=(null)`) was re-logged as "Commerce resolution
failed" while `TenantId=store-alpha` / `StoreId=store-alpha` were already resolved. No middleware or guard was
weakened and the planner did not cause the 401.
B — a real normal buyer session, no bypass: one task-owned account created through the PUBLIC
`POST /v1/auth/register` (`IdentifierKind=Email`, synthetic `@example.invalid`, random password, id
`01a17cf7-e38d-7000-82a5-00c57929bc54`); the EXISTING BFF `GET /api/auth/csrf` + `POST /api/auth/login` from the
app origin set its own cookies and `GET /api/auth/me` returned the same account (`SingleStore`, `store-alpha`).
No dev-actor, role/SQL injection, invented CustomerPartyId, user credential or user tab.
C — the live trial itself: Host PID 36952 from `D:\temp\tb229r1-build\out` (backend parity with `df6ec9017`,
Development, `/health` 200, `Tooba__StorefrontCapturedPreview__Enabled=true`, `AvailableDateCount=7` as preview
policy, never `DeliveryHorizonDays`), FE `127.0.0.1:3000` PID 28288 unchanged. Real offers of seller
`01a030d1-40cb-7000-8abe-6d31739956c5` added through the normal cart APIs with the cart-version header and merged
to the account (cart v6, 2 lines, 16,013,018 IRR), then reduced to v9 / 1 line for the single-line case.
`POST /v1/storefront/shipping/projection` returned **200** with `capturedPreview.enabled=true`, one group per real
offer with the real seller and real cart line ids, and an honest `destination_required` reason with the goods
amount ABSENT (never 0). The page rendered ONE groups surface — `legacyMethodCards=0`, no duplicate legacy card —
2 captured cards for the two-line cart and 1 for the single-line cart, each showing its product line, seller,
«تعیین‌نشده» type, «مبلغ کالا نامشخص», and one short reason; the read-only notice is visible; `scrollWidth ==
innerWidth` and `overflowX=false` at 1440 and 390. Native PNGs `screens/229r2-{1440,390}-{2line,1line}.png`
(1440×900 and 390×844, dimensions read from the files).
Not claimed: a live positive two-group FINANCIAL quote (the live capture is destination-gated and these offers
carry no assignment, so the valid live state is unavailable — the positive 99.00/198.00 composition stays the R1
real-composer harness fact), selection/commit/order/reservation/payment, the `not_ready` write guard exercised,
Storefront activation/price/eligibility/readiness, Marketplace cross-owner (SingleStore). R1 recorded limits
carried: identifier helper String-coercion, ISO-shape check not full calendar validity, label map keyed by
service/option rather than owner. Compact shared-snapshot per-order-group order evidence and the
selection/commit parity slice remain mandatory and unimplemented.
State left: the local Host is deliberately LEFT ON with the preview enabled for the Architect/user visual test —
read-only and still blocked from checkout by `storefront.captured_preview.not_ready`; checked-in `Enabled` stays
false; no migration applied/reset/reseed; admin draft, rates, pointer and publication untouched; no assignment
write; the local trial password lives only in `%TEMP%\tb229r2-acct.txt` and is NOT in git.
≈105 min vs HARD20 (8/8/4) — recorded NONCOMPLIANT, NOT a process PASS.
NEXT (Architect): review these receipts, then the parity slice — server-selected captured revision plus
SaveSelection/Commit replacing the `not_ready` refusal — and the compact order-group evidence decision.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. TB185 paused/default-OFF/PendingAllocation preserved; 225-R2 money
and 226 week accepted; 227's mobile empty-note residual and the unrecovered 225-R1 user-tab local-edit loss
remain recorded.

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

# CURRENT — 229-R1 EXECUTED PARTIAL: four read-path defects repaired; live card trial blocked at buyer login

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229-R1/{RESULT.md,TASK.md}.
Base `304ac4da8059e91673cd54306e0930b9bfd94378`. Status: **EXECUTED — PARTIAL**, not accepted, NO live card
observation, NO screenshots, nothing refreshed for users.

Defects closed in the same read path. Money is exact decimal WIRE TEXT: no Number/parseFloat round-trip, an
invalid amount never becomes 0 or another amount, an explicit 0 is lawful, unknown goods stays NULL, missing or
foreign currency is untrusted (never IRR), and malformed/duplicate group, method or date identity is untrusted
instead of silently dropped. Method identity is the stable catalog binding serviceCode+optionCode
(case-insensitive, one 219 read per distinct binding) with the translated label kept only for presentation, so
two distinct methods sharing one label both survive. The parallel captured component is DELETED: the preview is
drawn inside the EXISTING `StorefrontShippingGroupCards` through one optional prop for BOTH single-line and
multi-line carts, with the legacy duplicate row suppressed and unknown shown as missing rather than 0. Plan
honesty: `AvailableDateCount` is an explicit preview option 0..90 required when ON and never re-derived from
`DeliveryHorizonDays` (checked-in `Enabled` stays false); an unknown responsibility enum refuses instead of
defaulting; contradictory/out-of-request rows fail closed; a known business refusal surfaces a precise Refused
view instead of an empty cart; dependency exceptions and caller cancellation propagate; `SaveSelectionAsync`
and `CommitAsync` refuse first on `storefront.captured_preview.not_ready` when ON and keep the legacy path when
OFF. 218 validation was not relaxed (empty Candidates lawful, groups never removed, ONE 217 read, ONE 218
composition, one 219 read per distinct binding, one 216 quote per candidate).
Proof focused only: 18/18 real tests — 15 `StorefrontCapturedPreviewPlannerTests` including the required POSITIVE
composition through the REAL 218 composer plus the REAL 217 `CheckoutDirectory` harness (real goods 99.00/198.00,
two independent fee/week groups, a third unassigned group still visible, label-sharing distinct bindings both
surviving, case variants collapsing to one pointer read, dependency failure propagating) and 3
`StorefrontCapturedPreviewWriteGuardTests` driving the actual service methods — plus 11/11 frontend decoder facts
via `node --experimental-strip-types --test`, one clean `tsc --noEmit`, and an Order test build with 0 errors.
HONEST GAP: the REAL local trial is blocked at authentication. The current source was built isolated and served
on `127.0.0.1:5088` with the option ON and an explicit date count, but the storefront shipping route redirects to
`/fa/login?returnTo=/fa/shipping` and `POST /v1/storefront/shipping/projection` returned 401
`checkout.authentication_required`; no buyer session or credentials exist for this task, so the captured cards
were never rendered live and desktop/390 screenshots do not exist. Single surface / no duplicate cards / no
horizontal overflow are therefore source- and test-proven only, not live-proven. The original Host was restored
(PID 27588, `D:\temp\tb221-build\out`, preview OFF, `/health` 200), the checked-in default stays OFF, and no
assignment/draft/rate/pointer/order write, migration, Apply or user-tab interaction was performed.
Not claimed: Architect acceptance, user Visual ACCEPT, live positive quote or live cards, projection/selection/
commit parity, compact shared-snapshot order evidence, seller-admin entry, any price/eligibility/activation/
readiness statement. Actual ≈39 min vs HARD35 — recorded NONCOMPLIANT, NOT a process PASS.
NEXT (Architect): the one blocking gap is a live authenticated-buyer observation at desktop + 390 (a task-scoped
buyer session or explicit credentials), then the parity slice — selected captured revision plus SaveSelection/
Commit — which may replace the `not_ready` refusal; the compact order-group evidence decision stays mandatory and
unimplemented. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. TB185 paused/default-OFF/PendingAllocation
preserved; 225-R2 money and 226 week stay accepted; 227's mobile empty-note residual and the unrecovered 225-R1
user-tab local-edit loss remain recorded.

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

# CURRENT — 229 EXECUTED PARTIAL: Storefront group cards read path wired behind default-OFF option — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229/{RESULT.md,TASK.md}.
Base `02250f55f` (fast-forwarded; the three pre-existing Shipping line-ending drifts never staged).
Status: **EXECUTED — PARTIAL**, not accepted, NO live browser trial, Host/FE not refreshed.

CONNECTED (read only): `StorefrontShippingService.ProjectAsync` now runs real cart/access/version → ONE
`IOfferFulfillmentAssignmentLookup` batch over distinct offer ids → explicit partition by responsibility +
real assigned type + real offer seller → catalog bindings from `IShippingCatalogReader` (active service with
NULL option or each active option) → ONE `IShippingSelectedCapturedRevisionLookup.ReadAsync` per distinct
full-owner + canonical service/option → ONE 218 `ComposeAsync` for the complete partition (which does the ONE
217 goods read and one 216 quote per candidate). Additive nullable `StorefrontShippingProjection.CapturedPreview`;
`null` when OFF, so the legacy response is unchanged. FE: new `storefront-shipping-captured-preview.tsx` mounted
inside the EXISTING `StorefrontShippingGroupCards` (one optional prop), same card classes, per-group seller/type/
post-discount+tax goods amount, per-method freight+packaging, that method's own dates, and on date change THAT
date's own stored interval. Unavailable groups stay visible with one short reason.
Option: `Tooba:StorefrontCapturedPreview:Enabled` = **default OFF** (absent ⇒ zero Offer/Fulfillment/Shipping calls).
Write boundary: with the option ON, `SaveSelectionAsync` and `CommitAsync` both throw
`storefront.captured_preview.not_ready` (FA/EN resources added) as their FIRST statement — before cart read,
selection, draft write, order/reservation/payment/cart conversion. Disabling the FE button is not the gate.
Migration: `20261027120000_AddOfferFulfillmentAssignment` **APPLIED** on real local `tooba_alpha`/store-alpha via
normal `Tooba.MigrationRunner` after a plan with exactly one pending module (Offer); `result=ok, pendingCount=0`;
legacy rows keep NULL in both columns. No reset/backfill/reseed, no Host restart.

Proof executed: Order.Application + Order.Infrastructure build 0 errors; new affected class
`StorefrontCapturedPreviewPlannerTests` **9/9 passed** (default-OFF zero-call control, missing/contradictory
assignment, unassigned group visible, inactive type, SingleStore Seller unsupported with zero 219 reads, one 219
read per distinct catalog binding, foreign captured type rejected, cleared pointer rejected, zero candidates ⇒
composer/217 never called); frontend `tsc --noEmit` clean.
HONEST GAPS: no live page/URL, no desktop/390 screenshots, no positive composition executed in this task (216/217/
218 only covered by their own earlier classes), no Offer admin API integration class, positive live quote not
proven. Elapsed ≈70 min vs HARD45 — NONCOMPLIANT, not a process PASS.
NEXT (Architect): live Storefront GET with the option ON in a task-owned browser + desktop/390 screenshots, then
the parity slice (compact shared-snapshot order evidence + selected-captured-revision commit) that may replace the
not_ready refusal. Compact-order-evidence decision stays mandatory; TB185 paused/default-OFF/PendingAllocation.

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

# CURRENT — 228 PARTIAL executed: Offer holds the explicit two-family assignment; storefront wiring NOT delivered — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228/{RESULT.md,TASK.md}.
Base `d292e88b6` (fast-forwarded; three pre-existing backend line-ending drifts never staged).
Status: **EXECUTED — PARTIAL**, not accepted, no live trial, no migration applied, no Host restart.

A — assignment authority is real and stored. `SellerOffer` now has nullable
`FulfillmentResponsibility` (Seller|Platform) and nullable real `FulfillmentShippingTypeId`; the ONLY mutation is
`AssignFulfillment`, which leaves legacy listings null (no inference from seller id/channel/name), refuses an
archived offer, refuses a type without responsibility and an empty type, and clears both only explicitly. One
additive nullable migration `20261027120000_AddOfferFulfillmentAssignment` (varchar(16) + uuid, no backfill, no
FK, no code/label copy) is WRITTEN BUT NOT APPLIED to any DB. Batch Contracts read
`IOfferFulfillmentAssignmentLookup` returns assignments for the distinct offer ids in ONE query (missing id is
absent, never defaulted). The only write entry is `PUT /v1/admin/offers/{offerId}/fulfillment` — authorize BEFORE
body, presence-aware body (absent key ⇒ malformed, explicit null ⇒ clear), then ISender; it reuses the EXISTING
admin panel gate + EXISTING `fulfillment.manage` (no new permission, NO seller route) and validates the type
through the real active Fulfillment taxonomy. Error codes + FA/EN resources + catalogue + Offer manifest
(Admin un-forbidden with justification, Storefront still forbidden) + endpoint-validator manifest 6→7.

B/C NOT IMPLEMENTED — this is the honest gap: no server plan builder (cart offer ids → partition by
owner+type+seller → real catalog bindings incl. service with NULL option → one 219 read per owner+binding → ONE
218 ComposeAsync), and no Storefront wiring (existing group cards/delivery card/date intervals/read opt-in all
untouched). The 218 composer, 219 reader and 220 publication API are still unconsumed. Captured-plan selection/
commit needs no refusal YET because no captured candidate is reachable from any transport; the explicit refusal
must land together with C so a quoted-but-uncommittable candidate is never disguised as checkout-ready.

Proof: Offer.Endpoints build 0 errors; Host build 0 errors (48 pre-existing warnings elsewhere); 8/8 new focused
domain facts; Offer suite 92/93 with the single failure being a PRE-EXISTING missing-file drift
(`ProductWorkspace/.../GetProductWorkspaceHandler.cs` is absent from git) unrelated to this task.
Not run: 119/188/194/full-frontend/backend/solution suites, migration apply, browser, live storefront.
Actual ≈82 min vs HARD40 — recorded as NONCOMPLIANT, NOT a process PASS.

NEXT (Architect): apply the migration on store-alpha via the real runner plan; then build the server plan and
feed each group's own result into the existing cards behind a default-OFF read opt-in, with the captured
selection/commit refusal in the same change. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE; no 229 envelope.
TB185 paused, default-OFF, PendingAllocation preserved; no money/rate/week/publication/geography change.

## SUPERSEDED — Architect issued Storefront228 — 2026-10-27

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

# CURRENT — Architect reviewed 227 — 2026-10-27

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

# CURRENT — 227 executed: in-popup search + pixel-stable destination modal — 2026-10-27

Read docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227/{RESULT.md,TASK.md}.
Base `2d744fc67df34dc9427b2f0db56e2946c1344784` (fast-forward; the three backend line-ending drifts never staged).
Status: EXECUTED, NOT accepted. 225-R2 money and 226 mobile week stay accepted; both untouched.

A — the shake was MEASURED on the unchanged source: dialog top 299 → 271 → 257 (42px travel) and height
303 → 387 → 359 (84px swing) when the country's provinces were read; page scroll and the internal scroller
stayed 0. Cause: conditional path/branch-note/deepest/added/loading/failure rows inside a native
UA-centred `<dialog>`. Now the four fields are always rendered with their existing identities and every
transient line shares ONE reserved status slot (min-height 3.25rem), so the dialog height is a function of
the four fields only: 2230 sampled frames at 1440 show a single top (273) and height (355); 390 → 171/503
and 360 → 139/503 with ZERO travel, page scroll 0, inner scroll 0.

B — search lives INSIDE each dropdown popup (never an external input), filters ONLY the current verified
branch's loaded rows, prints `{shown} / {total}`, keeps inactive places visible-but-unselectable and
separates "this branch is empty" from "this query matched nothing". The popup is portalled INTO the open
`<dialog>` (a body portal is inert and cannot be focused — observed, then corrected) with fixed coordinates
from its trigger: never resizes or is clipped by the dialog, flips up when there is no room, always scrolls
inside the viewport (313/314/284 px wide at 1440/390/360). Live real input: `1 از 1` → `zz` → `0 از 1`,
Persian `ته` → `1 از 12` → Enter picks تهران, Escape closes ONLY the dropdown (dialog stays open), country
clear empties and disables all descendants, service area under مرند honestly `0 از 0`.

HONEST LIMITS: this installation has no service area under any city, so the fourth-stage OPTIONS are not
live-provable (branch proven honestly empty; selectable path test-proven only); the longest
no-provinces note still needs 66px at ≤390 (one recorded +14px shift when a branch settles empty); the dead
`authoring-destination-picker.tsx` was left untouched. ONE clean tsc, 7 new + 62 affected = 69/69.
No money/rate/week/publication/backend/geography change, no Apply/PUT/seed/migration/restart, no user tab
touched, draft revision and pointer untouched, default-OFF/TB185 paused/PendingAllocation preserved.
Actual ≈15 min vs HARD25. NO user Visual ACCEPT claimed.

NEXT PRIORITY (Architect issues it, user need not repeat authorization): concrete Storefront integration PER
SHIPPING GROUP using the EXISTING storefront cards/controls/DOM/CSS — 219 selected immutable revision +
218 composer + real trusted fulfillment owner/type/membership + existing projection/selection/commit; never
browser owner or price, never seller=fulfillment inference, unknown physical facts stay null. Storefront is
currently NOT wired/live. Compact order history remains mandatory and unimplemented: shared immutable
capture, compact per-order-group reference and final money/receiving facts, no full authored JSON per order,
no historical repricing.
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

## SUPERSEDED — 225 executed (worker report): destination cascade modal, rate-row modal, IRR integer UI, mobile week — 2026-10-26

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225/{RESULT.md,TASK.md}`.
Base `fb2999cfa880ceafa985c57eede47ff5107ba44a`. Active task NONE; next NOT ISSUED — Architect review comes
before any user trial.

A — the inline picker/level selector is REPLACED by ONE `Dialog` with four fixed labelled cascade fields
(country → administrative area → city → service area). Dependents start disabled, a parent change clears every
dependent stage, superseded completions are dropped by the existing generation-settled decoder, and `Add`
submits ONLY the deepest verified terminal as ONE `(kind,id)`; ancestors are the path, never extra coverage.
Duplicate/overlap is refused only on the explicit Add through the existing MessageBox, with the modal and the
chosen path kept; staging is not an addition and does not dirty the form. Stored chips rebuild their breadcrumb
after reload from the EXISTING Geography reads in bounded set-based queries (one per parent level, no per-chip
N+1, no fabricated segment, country-wide null-admin cities keep their honest two-segment path, a country
selection is never `ایران › ایران`). HONEST GAP (live): this installation's real `تهران` city has NO service
areas, so the fourth stage is honestly empty («برای این شهر ناحیهای ثبت نشده است.») and the service-area Add is
test-proven only. No Geography schema/data change, no seed.
B — rate rows remain inside the area card as compact summaries with Edit/Delete; Add/Edit open an isolated
modal draft and Confirm validates against the OTHER rows of the same area (weight/count/merchandise AND within a
row, half-open `[min,max)`, touching endpoints lawful, exact duplicate tuple refused even with different prices,
self excluded by stable row key — never by index), zero freight/packaging valid and empty not; a failed Confirm
shows the MessageBox with the owning-field reason and keeps the typed values; the parent Apply stays the only
persistence call. Mirror lives in `model/editor-rates.ts`; backend enforcement is untouched.
C — money: `interpretMoneyTyping` vs the strict paste/commit path fixes the screenshot's grouping corruption
(`1,200,000`, never `1,200000`) while `-100`/`1e3`/`12,34` stay exactly as typed with `aria-invalid`; IRR is
integer-rial in the UI (`120000.00` → `120,000`, raw wire token untouched) and a stored NON-zero fraction is
refused only on edit, never rounded or truncated; no Number/parseFloat money math, currency stays server-owned.
D — weekly view/edit is compact at 360/390: one aligned day+status line, working hours paired with From/To
context on one line, a closed day as a single badge with no empty inputs (live edit mode exposes exactly 2 time
inputs for the one open day); both modals fit the viewport and scroll internally (390: 352×566 inside 844;
`scrollWidth == clientWidth` at 390 and at 360). Desktop shared column alignment unchanged.
Proof: 22/22 new focused facts; ALL feature tests enumerated recursively 188/188; ONE clean `tsc --noEmit`;
architecture guards 10/11 files with `FE-SIZE` failing only on the four documented pre-existing drifts (none
touched; edited screen 798 LOC). Live on the real rev2 draft with real traffic: cascade with disabled
dependents, no phantom chip and Apply still disabled after staging, breadcrumb `کشور: ایران`, duplicate rate
condition refused with values kept, `-100` refused with the integer-rial reason, clean Cancel raised NO discard
dialog and the stored baseline (`1 محدوده`, `1..3`, `120,000/5,000`, one destination) was intact. NO
Apply/PUT/publication/catalog/migration/seed; draft revision and pointer untouched.
Screens: `screens/225-1440-destination-modal.png`, `225-390-rate-modal.png`, `225-390-rate-refusal.png`,
`225-390-week-view.png`, `225-390-week-edit.png`.
NOT claimed: user Visual ACCEPT, live service-area/legacy-conflict rendering, any Storefront/checkout consumer
(`Selected` is still selection intent), price/activation/readiness. Elapsed exceeded the HARD45 box across a
chat-loss and correction rounds — recorded, NOT a process PASS. Compact-order-evidence decision stays locked and
unimplemented; TB185 paused, default-OFF, PendingAllocation preserved. STOP_FOR_ARCHITECT.

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
line-ending drifts and a stray 0-byte root file were never staged — the stray file was deleted).

A — the TYPED area range is now the form state that owns dirty/issues/Apply/discard. `ui/use-area-range-edits.ts`
is DELETED; `model/editor-area-range.ts` adds `AreaRangeDrafts`/`areaRangeEdit`/`areaRangeDraftIssues`/
`areaRangeDraftsDirty`/`createAreaRangeDrafts`, `buildAuthoringIntent(..., areaRanges)` folds blank, partial and
invalid text into the blocking issues, `authoring-screen.tsx` keeps `areaRanges` in state and reseeds form +
groups + rates + area ranges + week from the SAME baseline through ONE `applyLocalDrafts` helper (fresh load and
confirmed cancel), and `AreaDeliveryRange` is fully controlled with an area-scoped message — a row-less area
says «یک ردیف اضافه کنید یا بازه را خالی کنید» instead of silently persisting nothing. HONEST BOUNDARY: the
DTO/capture still repeats the days per row; a true group-level persisted duration remains a schema change, NOT
done.

B — `model/money-text.ts` no longer rewrites unsupported money, and its grouping no longer swallows a separator
Backspace (found by this task's OWN mounted gate: a Backspace right after a comma deleted only the separator,
grouping re-inserted it with the same caret, so the key did nothing forever; `interpretMoneyBackspace` now
removes the digit with it — `120,000` → `12,000`, `1,000` → `000` visibly invalid). `readMoneyText` normalizes only Persian/
Arabic-Indic digits, the Arabic decimal separator, correctly formed 3-digit comma grouping and boundary
whitespace; signs, letters, exponent, multiple/malformed separators keep their raw text with `valid: false`,
render `aria-invalid` and block Apply with `amountInvalid`. `MoneyCaret { whole, fraction, inFraction }` +
`caretAnchorAt`/`caretOffsetFor` replace digit-count-only anchoring, so `12` → `.` → `5` really yields `12.5`.

RED measured on the reviewed baseline (probe, not memory): 8/8 defect facts — `-100`→`100`, `1e3`→`13`,
`12.3.4`→`12.34`, `12,34`→`1234`, `abc100`→`100`, digit-only caret with no decimal side, and NO typed-range form
state. GREEN: 11/11 new R1 facts, 166/166 feature tests (all files recursive), one clean `tsc`; guards 7/7
except `FE-SIZE` on the four documented pre-existing drifts (none touched; the edited screen is 798 LOC ≤ 800).

Mounted (FE3000 PID28288 / Host5088 `D:\temp\tb221-build\out`, both health 200, no restart, isolated new tab —
the user's two tabs were never reloaded): a partial maximum disabled Apply with a visible area issue while a
valid packaging edit was already staged; `2.5` was refused; a row-less area's staged `4..9` blocked Apply on its
own, then moved into the first row; freight `12` → `12.` (invalid, blocked) → `12.5` with caret 4, never `125`;
pasted `-100`, `1e3`, `12,34` stayed exactly as typed with `aria-invalid` and a disabled Apply; reject-cancel
kept every draft, confirm-discard restored the exact stored baseline (1 area, `1..3`, `120,000.00`/`5,000.00`),
re-entry was identical and a clean cancel raised no dialog. Publication stayed view-only, the level picker and
closed-day inputs were retained. NO Apply/PUT/publication/catalog/migration/seed; a read-only editor-document GET
returned `revision 2`, `updatedAt 2026-10-24T19:05:52.452036Z` — the draft and the selected pointer are untouched.
Screenshots stored (1440×900 and 390×844, `scrollWidth == clientWidth`, `overflowX 0`).
NOT claimed: user Visual ACCEPT, Architect acceptance, legacy-conflict live rendering, any Storefront effect.
Actual ≈50 min vs HARD25 — recorded, NOT a process PASS.
Active task: NONE. Next: Architect review; user must NOT be called to test before acceptance.
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
Base `d62bab9a4f31dc1966aed8c06b4f7ea08a366df3` (untracked 223 evidence folder collided with that commit's
tracked TASK.md: moved aside, fast-forwarded, artifact restored; no reset/stash/rebase/force-push).

A — publication is BACK, compact. The model and transport were recovered BYTE-EXACT from 221 (exact
`expectedRevision`+`expectedSelectionVersion`, strict decode, synchronous in-flight gate, stale/unknown latches
cleared only by an explicit successful read, no editor reseed, no auto-retry). The new panel is deliberately
small: one status line, at most one short reason, one honest note, Publish/Withdraw + confirmation. Snapshot id,
selection counter and format version are NOT in the normal UI. Live trial on the real draft
`01a16eeb-70b3-7000-87c2-564c82835496` (view mode): `Selected`, matches current revision ⇒ Publish DISABLED with
«همین بازبینی از قبل منتشر شده است.» (same capture is not republished), Withdraw available. NO write sent; the
draft revision and pointer were not touched by this task.

B — the TB222 single-option owner combo is GONE (live DOM `combo:false`, `ownerNote:false`). A separate route
`/admin/shipping-services/marketplace` renders the SAME list feature (`surface="marketplace"`), separate saved
views/grid namespace, store list at `/admin/shipping-services` (`store` surface) with a link to it. The route is
a UI boundary only: the authorized owner (`/v1/admin/shipping-services/authoring-owner`) is read FIRST and no
drafts are read when that owner is not the surface's owner. SingleStore live: marketplace renders the honest
«مارکت‌پلیس در این نصب در دسترس نیست» state with no list and no fabricated owner; the reverse mismatch also has
its own honest state. No owner id from query/body, no edition change, no seller inference.

C — the compact order invariant stays as recorded in
`docs/architecture/shipping/SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md`
(one shared immutable snapshot per owner+draft+revision; only compact per-order-group reference/final money/
receiving facts; no full authored JSON per order; no historical repricing). NOT implemented by this task.

Proof: one clean `tsc`; 144/144 feature tests (restored publication facts + 3 new owner-surface facts); 7/7
architecture guards; live FE3000/Host5088 with no restarts; 1440 + 390 screenshots with `overflowX 0`; closed-day
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
verbatim in `TASK.md`) and `219/ARCHITECT-REVIEW.md`. The two source-confirmed defects are repaired; the 219
schema/architecture/Contracts/owner-resolver are retained.
(1) `ShippingSelectedCapturedRevisionDirectory` now obtains the AUTHORITATIVE persisted pointer through a new
private `LoadPersistedPointerAsync` — the one owner-scoped tracked entry is explicitly `ReloadAsync`ed BEFORE
any expected-version or no-op decision (identity preserved, unrelated caller state untouched); a vanished row
is treated as absent (only that entry detached) and a stale leftover for a provably absent row is detached so
it can never act as a live token. `RequireCleanTracker` still refuses dirty caller work first (no
refresh/flush/clear); the real CAS token and exact unique matcher still guard writes.
(2) `Select`/`Clear` compute and validate the whole next state BEFORE any assignment, so a rejection at an
exhausted version leaves target/version/`UpdatedAt` unchanged, the entry `Unchanged`, a later `SaveChanges` at
0 and the DB unchanged; the adapter maps that rejection to a normal `selected_capture.invalid` result.
Executed on the task-owned disposable `127.0.0.1:15432`/`shipping_tb214`/`tb214` (identity verified before
DDL, container removed, port silent): RED on unchanged production 3 failed / 9 passed → GREEN 12/12 (9
accepted 219 facts + stale-select, stale-clear and max-version no-leak facts, each with an independent DB
read and preserved unrelated tracked identity). Build 0 errors; only the affected storage class executed.
**NOT WIRED / NOT LIVE**: no route, no admin publication UI, no Order consumer, no
projection/selection/commit; selection remains publication intent only.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next = real publication/selected-plan
integration + real fulfillment owner/type/group membership and measurement authority, then
projection/selection/commit; not another audit).
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
persisted binding is **derived** from the verified payload (missing/foreign/corrupt ⇒ `snapshot_unavailable`,
declared-binding mismatch ⇒ `binding_mismatch`). Create only from version 0; stale expected version ⇒
`selected_capture.conflict` before any mutation; same target at the current version is a no-op preserving
version AND `UpdatedAt`; clear retains row/version and advances once when changed; a pre-clear version cannot
resurrect; rollback = selecting an OLDER existing capture at the current pointer version. Unrelated dirty
tracker refused first; one `SaveChanges`; only the exact 23505 on the named unique index and
`DbUpdateConcurrencyException` map to conflict, and the attempted entry is detached on failure while caller
exception/token propagate. 216's owner/edition/tenant coherence mapping was extracted to
`ShippingCapturedOwnerScopeResolver`, reused by 216+219 only (216 unchanged, class re-executed 9/9).
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

## CURRENT —217 group goods money authority done; NOT wired —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-GROUP-GOODS-QUOTE-FACTS-217/RESULT.md`. 217 added ONE
read-only internal Order seam `ICheckoutShippingGroupGoodsFactsReader` (Application.Checkout.Contracts),
implemented on the EXISTING `CheckoutDirectory` and registered scoped to the SAME instance in
`OrderModule.AddServices`. `PreviewAsync` was split into `LoadPreviewCartAsync` (guard/access/status/empty)
and `QuotePreviewSnapshotAsync` (ONE whole-cart Pricing→Promotion→Tax quote, no persistence); legacy
signature/return/behavior unchanged. The reader validates server-side cart membership as a complete,
non-ambiguous partition (blank/untrimmed/duplicate group key, empty group, unknown/duplicate/shared/omitted
line, mixed seller ⇒ `checkout.shipping_groups.invalid`) and requires `ExpectedCartVersion == real.Version`
(stale ⇒ `PRICE_CHANGED`) BEFORE any quote call, then sums the EXACT per-group members'
`TaxInclusiveSnapshot` via the existing `_orderLineIdByCartLineId` map (no OfferId join, no proportional
allocation, no second tax/discount). Sole cart currency must match every line; negative authored amounts ⇒
`checkout.shipping_groups.amount_invalid`; explicit zero stays valid. Physical `GroupWeightGrams` and
`PhysicalItemCount` are `null` (no authoritative measurement source in this slice) and `QuantityTotal` is
decimal metadata only — never a Shipping item count. Executed: build 0 errors, 9/9 new focused facts
(incl. 99.00/198.00 worked example, duplicate-Offer identity, fail-before-quote call counts, no DB/reserve/
convert, same-scoped-instance DI) and 194/194 retained Storefront + infra organization/boundary subset.
**NOT WIRED**: no consumer, no published/selected captured revision, no fulfillment owner/type/membership,
no projection/selection/commit; Storefront/checkout runtime unchanged. No schema/migration/HTTP/MediatR/
foreign-Contracts/FE/flag/seed/user-draft change; no Host5088/FE3000 restart.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next slice = wire captured revision
selection + these group facts + accepted 213 calendar through projection/selection/commit; remaining gaps
are publication/activation ownership, fulfillment owner/type/membership and real measurement sources, not
another audit). TB185 paused/default-OFF/PendingAllocation unchanged; §29 decisions and 211/213/214/215/216
not reopened; 9 applied migrations and the real user draft/unsaved tab preserved. Actual time exceeded the
25-minute box (recorded, not process PASS).

## SUPERSEDED —216 boundedACCEPT; only217 real same-group goods amount authority —2026-10-25

Read216/ARCHITECT-REVIEW.md. Source5f7aed605ea0225888a7d66e13e51b8399402b9c exactcapturedContracts+verifiedGeo+scopedDIaccepted;worker9new/11legacy/23compatgreen. Source-reviewedbranches!=fullrealGeoexecution;time43>25notPASS. NoStorefrontconsumer/publishselectionyet.
ROOTFOUNDrealexistingmoneyauthority: CheckoutDirectory.PreviewAsync -> QuoteSellerOrdersAsync -> Pricing/Promotion -> Tax; TaxInclusiveSnapshot ispostdiscounttaxinclusivegoodslineamount. _orderLineIdByCartLineId isexactexistingmap. NoCartSubtotalExclusive substitution, noOfferIdjoin/proportionalallocation.
Onlyissued TB-SHIPPING-GROUP-GOODS-QUOTE-FACTS-217,parent216;25min/stop17. NewreadonlyinternalOrderpreviewgroupfactsreaderreusesSAMEquotepipeline, authenticcart/version/completepartition/cartlineidentity, pergroupsumalreadycomputedTaxInclusiveSnapshot; unknownphysicalmeasurementsremainnull. No losslycopy/schema/OrderliveShippingconsumer/flag yet. Nextwirecapturedselection+realgroupfacts+213calendaracrossprojection/selection/commit afterreview; noanotheraudit.
§29decisionsresolved,211FE119/213calendar/214capture/215manualquoteaccepted; no replay. Realuserdraft/revision/unsavedtabs andHost5088/FE3000preserved;214tablenotsharedapplied.9oldmigrationsapplied, no routineupdate. TB185paused/defaultOFF/PendingAllocation. Await217result;olderCURRENTsuperseded.

## CURRENT —215 boundedACCEPT; only216 exact-capture Contracts quote edge + verified destination —2026-10-25

Read215/ARCHITECT-REVIEW.md. fff92bb4552d0a9e5c48aa362ddd36c9059fb5cf manualengineextensionaccepted;worker11new+23compatpass. Same-rowfreight+packaging,3ANDaxes,taxinclusivegroupamountinput,Prepaid/CODsplit. No active/runtimeconsumer yet;Free/Carrierunavailable, EligibilityNotEvaluated.
Onlyissued TB-SHIPPING-CAPTURED-QUOTE-CONTRACTS-EDGE-216,parent215;25min/stop17. ThinShippingContractsrequest/result+scopedadapter exactowner/draft/rev, no latestfallback, actualGeographyparentclosure/nulladmin/inactive+identityvalidation, existingengineonly. Correctpotentialmissingalternativecase andverifieddeepadminpathinthissameedge task. Focusedtests, no anotheraudit/replay.
NOpublicpublish/pointer/Orderconsumer/sharedmigration/flagflip yet. Carttaxdiscountgroupamount/measurementauthority and activationselection stillopen, notsyntheticdefaults. §29userdecisionsresolved,213calendar/214capture/211FE119accepted, no reasking. Preserveactualdraft/revision/unsavedtabs,Host5088/FE3000,9appliedmigrations;214tableNOTsharedapplied. TB185paused/defaultOFF/PendingAllocation. Await216result;olderCURRENTsuperseded.

## CURRENT —214-R2 boundedACCEPT; only215 captured MANUAL quote evaluation —2026-10-25

Read214-R2/ARCHITECT-REVIEW.md. Source8790165bd95bc48f64ecdc69a8b03d777edf3110 ownentrycleanup/dispose, actualstalectx/concurrency/sentinel/cancel/realuniqueproofaccepted;worker8/8realPostgres. Capturefoundationclosedforboundedbehavior, noR3loop. R2timeover12notprocessPASS. NewdisposalhelperneedsEXACTtype-onlyguardentry+narrowcompatcheckin215, nofileallowlist.
Onlyissued TB-SHIPPING-CAPTURED-MANUAL-QUOTE-215,parent214-R2;25min/stop17. ExtendexistingShippingrateenginewithpureexactcapture evaluation, 3ANDaxes/perrowfreightANDpackaging/PrepaidCODsplit/metadata, meaningfulfocusedtests. No readliveDraft/latestfallback, no lossyconversion/newpriceengine/Orderliveconsumer/publishpointer. Puretrustedinputonly; taxinclgroupamountcomesfromownerlater, no fakeweight.
Nextafterreview: trustedowner+selectedcapturedrevisionContractsedge and realgroupprice/measurementsource thenprojection/selection/commit connection. Notanothersourceaudit. §29decisionsresolved,213calendar/211119FEtestsacceptedandnotreplayed. CapturesNOTLIVE, tableNOTsharedapplied; preserveactualdraft/revision/unsavedtab, Host5088/FE3000 and9appliedmigrations. TB185paused/defaultOFF/PendingAllocationunchanged. Await215result;olderCURRENTsuperseded.

## CURRENT — 216 captured quote Contracts edge done; NOT connected — 2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-CAPTURED-QUOTE-CONTRACTS-EDGE-216/RESULT.md`. 216 added the
Contracts edge over the EXISTING 215 authority: `IShippingCapturedRevisionQuoteLookup.QuoteAsync` +
typed request/result in `Contracts/Rates/ShippingCapturedRevisionQuoteContracts.cs`, implemented once,
scoped, in `Infrastructure/Rates/ShippingCapturedRevisionQuoteLookup.cs` and registered in
`ShippingModule.AddServices`. Exact owner+draft+revision read (no latest/current/active), owner/edition
coherence from `ICurrentEdition`+`ICurrentTenant`, verified Geography path via
`IGeographyPlaceSnapshotReader` (active country/city, full active same-country admin chain, declared area
must equal the city's real parent, service area must belong to the declared city, duplicate/outside rows and
provider failures fail closed, null admin city lawful), then ONE `QuoteCapturedRevision` call and an explicit
Domain→Contracts map. Two narrow Domain corrections executed: `matches==1 && undetermined>0` now returns
`MeasurementRequired` (known-outside control unchanged), and `AdministrativeAreaAncestors` (additive optional,
root..leaf, verified) lets a stored ROOT admin area cover a deep city without any per-ancestor pricing loop;
ancestor+descendant in two groups stays `GroupAmbiguous`. Executed: build 0 errors, 9/9 new edge cases,
11/11 exact 215 class, 23/23 exact `OperationalQuoteCompatibilityTests`. No schema/migration/HTTP/DI-host/
Order/FE/Host change, no shared DB, no gate flip. NOT CONNECTED: Storefront/checkout still legacy, owner key
is data identity not permission, caller-declared money/measurements are not Cart-proven, no active-capture
selection or projection/commit.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next slice = trusted owner + selected
captured revision authority and group financial/measurement authority, then projection/selection/commit).
Preserve real user draft/unsaved tab, Host5088/FE3000, 9 applied migrations; default-OFF, TB185 paused,
PendingAllocation unchanged; 213 calendar and 211 FE remain accepted-but-unwired.

## SUPERSEDED —215 captured manual quote primitive done; NOT wired —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-CAPTURED-MANUAL-QUOTE-215/RESULT.md` and
`214-R2/ARCHITECT-REVIEW.md`. 215 added ONE pure additive capability on the EXISTING rate authority:
`ShippingRatePolicyEngine.QuoteCapturedRevision(...)` over an exact decoded captured revision, with its
typed input/result in `Domain/Rates/ShippingCapturedRevisionQuote.cs`. Existing `Quote(...)` overloads are
behaviourally unchanged. Identity (owner/draft/revision) is checked first and fails closed; settings are
required (missing = NotConfigured, CarrierApi/Free = explicit unavailable, never a zero price); groups
resolve by stored area NodeKind+NodeId with one-group-required (0 = coverage missing, >1 = ambiguous);
currency must match exactly with no FX; the three axes (weight/count/merchandise amount after discount and
tax) are AND within a row and alternatives across rows, half-open `[min,max)`, blank axis unconditional,
explicit 0 distinct from missing, invalid bounds/config fail closed; exactly one row wins (0 = not matched
or measurement required, >1 = ambiguous even with equal amounts); freight AND packaging come from that one
row, with the module's defined payer split (packaging always checkout, only freight can be carrier
collect). Result carries group/row ids, draft/revision/format provenance, raw type id, min+max delivery
days, stored settings and the stored week — no preparation/lead-day conversion, no label guessing.
`Eligibility` is always `NotEvaluated`: monetary answer only, no activation/readiness claim.
Integration hygiene: `DisposeCaptureTransactionAsync` is now the single named type-only member in
`AllowedTypeMarkerMembers` (begin still forbidden there; narrow negative proof executed). Executed: build
0 errors, 11/11 new quote cases, exact `OperationalQuoteCompatibilityTests` class 23/23 once. No
schema/migration/Contracts reader/DI/endpoint/Order/FE/Host change, no shared DB, no user-draft write, no
gate flip. Ambiguous/invalid-bound cases are rebuilt with `with` because today's authoring guard refuses
them; the evaluator fails closed instead of resolving them.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT (next concrete slice = Contracts-only
reader/selection edge + group money authority, then projection/selection/commit; not another audit).
Preserve real user draft/unsaved tab, Host5088/FE3000, 9 applied migrations; 213 calendar still
accepted-but-unwired; 211 FE 119 tests and §29 owner decisions not reopened. TB185 paused, default-OFF,
PendingAllocation unchanged.

## SUPERSEDED —214-R2 exactcleanup executed; capture still staging-only —2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R2/RESULT.md` plus the
appended correction note in `214-R1/RESULT.md`. Only `CaptureRevisionAsync` changed: every non-committing
path detaches ONLY this method's attempted snapshot (never the caller's tracker/root/children), and the
exact-23505 catch disposes its OWN transaction BEFORE the single winner recovery read, so no completed
`CurrentTransaction` and no pending failed insert can survive a later `SaveChanges`. Exception and caller
token identity preserved; `SaveAuthoringReplacementAsync`, the exact unique identity filter and all
no-write preflight rejections unchanged; capture still returns provider-persisted facts.
Two 214-R1 overclaims are now real: the successful B-token capture runs through the ACTUAL stale context
(old identities/revision/rows preserved, payload is B's full graph) and the duplicate-capture fact uses
`Task.Run` plus a bounded gate with an asserted exactly-2 arrivals. Three failure facts executed (sentinel
before save, real caller cancellation at the write boundary, REAL 23505 via an independent task-owned
winner) each yield no row, no pending entry, later `SaveChanges` = 0; the unique case also proves
`CurrentTransaction == null` and returns the winner id. Executed only on the task-owned disposable
`127.0.0.1:15432` (`shipping_tb214`/`tb214`; container removed, port silent): build 0 errors, 8/8 storage
facts. Timing exceeded the 12-minute box (recorded, not a process PASS).
No 215 issued; staged capture is NOT live (no publish route/pointer/consumer, default-OFF, TB185 paused,
PendingAllocation unchanged). Preserve real user draft/unsaved tab, Host5088/FE3000, 9 applied migrations.
213 calendar remains accepted-but-unwired; 211 FE 119 tests and §29 owner decisions not reopened.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT.

## SUPERSEDED —214-R1 partialrepairs; only214-R2 exactcleanup + truthful concurrencyfacts —2026-10-25

Read214-R1/ARCHITECT-REVIEW.md. Rootobservedmainfixes but testsoverclaim: B-tokenfreshcapture usesNEWfreshInA notactualstaleA;duplicatecapturebarrierblocks synchronouslybeforeawait and ignoresSignalAndWaitbool. CaptureownAddedentrysurvivesrollback(false);exactunique catchnullstransactionwithoutDisposebeforeread. Fixonlythose, no newarchitecture.
Only issued TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R2,parent214-R1;12min/stop7/setup<=3. Existingisolated15432target; no schema/newAPI/flags/userdata/sharedmigration/Hostrestart. Trueconcurrenttasks+gatearrivalassertions, actualstalecontext, fault/cancel/uniquerecovery leavescallerstatebutnoattemptedentry/laterSave0. Awaitresult; no215yet.
43/43+18/18workerreportednotblanketproof;R1time48>20recorded. §29usersemanticsresolved;213calendar/211FE119accepted, no replay. CaptureNOTLIVE, no lossylegacyconversion/defaultflip. Preserveactualdraft/unsavedtab,Host5088/FE3000,9appliedmigrations;TB185paused/PendingAllocation. OlderCURRENTsuperseded.

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

## CURRENT — 213 customer receiving-calendar primitive done / NOT wired — 2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-STOREFRONT-CUSTOMER-RECEIVING-CALENDAR-213/RESULT.md`.
Owner decisions in rules §29 are now implemented as ONE additive pure primitive in the existing
`StorefrontShippingCalculator`: `BuildCustomerReceivingCalendar(referenceLocalOrderDate,
minimumWorkingDayOffset, StorefrontShippingReceivingWeek, deliveryHorizonDays)` returning
`StorefrontShippingReceivingCalendar(EarliestEligibleDate?, AvailableDates[date + that day's interval])`,
with four cohesive records in `Storefront/Models/StorefrontOrderModels.cs`. Receipt promise measured from the
order date (offset 0 = reference only if working), closed days skipped in counting AND in offers, each date
carries its own interval (no static 9-12, no browser clock), all-closed week = explicit no availability,
malformed week/intervals/offset/horizon rejected via existing `shipping.delivery.invalid` (no clamp/fallback).
Legacy members byte-behavior unchanged. Build clean; 11/11 focused tests in
`Tooba.Order.Tests/Storefront/StorefrontShippingReceivingCalendarTests.cs`. **Not wired**: no projection/
selection/commit consumer, no published-week source, gate still default-OFF, no storefront integration claim.
Next single step (NOT issued): reviewed wiring of this calendar from the PUBLISHED method week per shipping
group, replacing static windows only after published-revision authority (TB212-R1 slice) exists.
Remaining: published-revision authority, owner/edition authorization (Marketplace/Seller), same-group goods
amount after discount+tax from pricing/Cart/Order, tax+discount allocation, money/day mapping for legacy.
Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT. TB185 paused; user Visual ACCEPT for 210 PENDING.

## CURRENT — 212-R1 corrected source facts posted / awaiting Architect review — 2026-10-25

Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212-R1/RESULT.md`
and `.../212/ARCHITECT-REVIEW.md`. 212-R1 is docs-only source correction under the 10min box.
Retracted: "Shipping Contracts only / no implementation". Actual registrations in
`ShippingModule.AddServices` include `IShippingPackagingFeeLookup`, `IShippingShipmentCostLookup`,
`IShippingOperationalQuoteLookup`; `Infrastructure/Operations/ShippingOperationalQuoteLookup.cs` loads the
four module-owned slices and delegates to `ShippingOperationalQuoteEvaluator.Evaluate` (freight+packaging,
payer split, stable reason codes). Applicability (service/option/geography/currency) is NOT seller/tenant
authorization; tenant binding is real via `ShippingDbContext`+`ICurrentCommerceContext`; seller scope is
the remaining gap. Operating schedule = carrier pickup/dispatch windows, NOT a delivery promise.
Conversion to legacy aggregates is provably lossy (per-row packaging vs scope-only resolver, merchandise
bounds, delivery range vs prep/lead, CalculationMode Free/CarrierApi, owner/seller, week semantics).
Chosen next slice: Shipping-owned immutable published revision snapshot (owner identity + per-row freight
and packaging + conditions + delivery range + week + settings) read through new Shipping Contracts, one
transaction, `ExpectedRevision` guard, rollback by pointer, **no consumer/activation** so no policy is
guessed. Proposed 213 (copy-into-legacy publish) withdrawn; no publication implementation authorized.
Named blockers: week semantics, CalculationMode runtime semantics, seller/marketplace authorization,
activation semantics. Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT.
Storefront gate stays default-OFF, TB185 paused, no src/data/flag change; nothing connected, no
storefront certification claim. User Visual ACCEPT for 210 remains PENDING.

## CURRENT — 212 publication proposal REJECTED; only212-R1 exact source correction — 2026-10-25

Root independently read ShippingModule+OperationalQuoteLookup: packaging/operational implementations and scopedDI ALREADY EXIST.212 claimed none: false. Root also read operational contract: week means carrierpickup, not customer delivery promise. Don't duplicate adapters or implement proposed213 snapshotcopy.
Read212/ARCHITECT-REVIEW.md. Other blockers: per-row packaging cannot flatten onepolicy/currency, owner != geography and tenantcontext isolation exists, IsActive flags don't prove snapshot provenance/rollback, partial publication of unsupported delivery/amount/mode is forbidden. Store-only restriction is not approval for Marketplace/Seller architecture.
Only issued TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212-R1,parent212;10min/stop6, source-only targetedcorrection. NO213implementation authorized. Await result, then one concrete safe runtime implementation slice, not generic audit. Runtime gateOFF/TB185paused remain.
211119/119featuretests/typecheck/7guards and contrast accepted; no replay. Actual draft/user unsaved data, Host5088/FE3000 and applied9migrations preserved. Prior explicit localdev authorization persists. User needs quick correct integration, not unsafe partial price. Older CURRENT headings below superseded.

﻿## SUPERSEDED — 212 draft→runtime bridge contract / proposed213 withdrawn by 212-R1 — 2026-10-25

212 is source-only (no src/runtime/data change). Decision: do NOT add a second quote engine or let Order read
draft tables. The runtime quote authority already exists — Shipping.Contracts.Rates.IShippingRateQuoteLookup
→ ShippingRateQuoteLookup → ShippingRatePolicyEngine, consumed by Order's gated
StorefrontShippingOwnedRateResolver (parcel always null, fail-closed); IShippingPackagingFeeLookup and
IShippingOperationalQuoteLookup exist as contracts with no implementation/consumer. Publishing a draft must
be a **write** into Shipping's existing aggregates (RatePolicy, CoverageGrant, MethodCoverages,
PackagingPolicy, OperatingSchedule) via the existing Shipping commands, as a revision-keyed snapshot;
active state reuses existing IsActive (no new flag). Freight and packaging stay separate (freight =
ChargeAmount, packaging = packaging policy); destinations = one row per node; week = OperatingSchedule.
Not representable today: merchandise-amount condition, CalculationMode, per-language rate labels, seller
ownership in RatePolicy, and a 1:1 min/maxDeliveryDays → PreparationDays/LeadDays mapping (undecided).
Next slice (issued nowhere yet): TB-SHIPPING-ADMIN-DRAFT-PUBLISH-SNAPSHOT-213 — Shipping-only publish command
+ Contracts result with UnrepresentableAxes[], Store-owned only, no Order change. Read
`docs/architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212/RESULT.md`.
211's "config-only" and "read-only publish" wording is corrected there. Active task: NONE. Next task: NONE —
await Architect review; User Visual ACCEPT PENDING; TB185 paused/default-OFF intact.
## CURRENT — 211 accepted; only212 runtime bridge contract/reuse decision — 2026-10-25

211 contrast accepted after source+desktopPNG review. Worker119/119all14recursive feature files, typecheck clean, guards7/7; one exact stale gram-label assertion correction. Mobile screenshot shows only first area (not proof of both colors), no extra screenshot-only task. User said better; User Visual ACCEPT not blanket granted.
Read211/ARCHITECT-REVIEW.md. Important runtime correction: StorefrontShippingService already calls gated StorefrontShippingOwnedRateResolver -> Shipping.Contracts.Rates.IShippingRateQuoteLookup; runtime is NOT config-only. Must reuse existing seam, no second engine. New authoring draft type/areas/fees/week not integrated. 'Read-only activation' and foreign Fulfillment-owned publication state NOT approved.
Only issued TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212,parent211;12min/stop8,source-only. Trace existing authority/owner/currency/measurement/week and approve one concrete minimal implementation plan from source; no code/flags/data/runtime changes. Await212 result; next implementation chosen after review, not another generic audit.
Live draft01a16eeb-70b3-7000-87c2-564c82835496, actual revision/user unsaved work preserved. Host5088/FE3000 running;9migrations already applied. Prior local nonproduction authorization persists.190–211 don't replay; TB185paused/defaultOFF/PendingAllocation unchanged. Older CURRENT headings below are superseded history.

﻿## CURRENT — 211 area contrast + editor checks posted / awaiting Architect review — 2026-10-25

User said the UI is better and asked for alternating delivery-area card backgrounds, quick editor checks and
a storefront status answer. 211 delivered: whole area card alternates by display position using the existing
theme-aware alternate-surface token (white / rgb(247,248,250) light, 22 22 26 dark) in VIEW and EDIT; nothing
else changed. All **14** feature test files enumerated recursively (incl. api/ and session/) and run by real
path: 119 tests / 119 pass / 0 fail / 0 skipped (one stale assertion updated to the truthful «Weight (g)»
label; no assertion weakened). typecheck clean; import/feature/flat-folder/test-discovery guards 7/7.
Live: one stored area, second area added locally only for contrast, discarded through the normal
«خروج بدون ذخیره» flow; stored 120000.00/5000.00 unchanged, no Apply/HTTP write/migration.
Storefront answer (source only): storefront/checkout shipping choice EXISTS but reads config
(ShippingMethodsOptions + ShippingMethodRegistry) and the Fulfillment catalog; the Order module has ZERO
references to the authoring draft, so authored type/areas/fees/week do NOT reach customers — no
publish/activation seam exists yet. Smallest next slice = a read-only publish/activation projection into what
StorefrontShippingService.ProjectAsync already reads. Read
`docs/architecture/evidence/TB-SHIPPING-ADMIN-AREA-CONTRAST-AND-EDITOR-CHECKS-211/RESULT.md`.
Active task: NONE. Next task: NONE — await Architect review; User Visual ACCEPT PENDING.
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

﻿## CURRENT — 210-R1 mobile/view week completion posted / awaiting Architect review — 2026-10-25

210 desktop acceptance retained. R1 finished the three concrete review items in the existing week editor:
narrow screens now render TWO lines per day (day+status, then paired equal-width From/To with visible
localized labels) instead of three; each bound is a label whose accessible name is "<weekday> From/To";
view mode is truthful per column (Status = working/closed text, From/To = stored bounds, closed day shows
an honest dash) with zero inputs. Desktop 210 geometry unchanged (day 481/607, start 209/120, end 81/120;
all seven rows identical). Evidence now contains real 390x844 viewport bytes with the week actually in
view, plus a bounded EN/LTR cookie check. Read
`docs/architecture/evidence/TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210-R1/RESULT.md`.
No Apply/HTTP write/migration; stored 120000.00/5000.00 unchanged. typecheck clean. Actual elapsed inside
the 12min box. Active task: NONE. Next task: NONE — await Architect review; User Visual ACCEPT PENDING.
## CURRENT — 210 desktop accepted; only 210-R1 mobile week completion — 2026-10-25

Architect viewed all four210 PNGs and source. Desktop common widths/tab geometry/seven row columns accepted within scope; User Visual ACCEPT remains PENDING after prior10/20 rejection.
Actual mobile file shows TOP General, not week; source has three lines/day and no visible mobile From/To labels. View puts time range under Status with empty From/To cells. EN evidence absent.
Only issued task TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210-R1,parent210: complete paired labelled mobile times, truthful view columns, actual week screenshot at390 plus bounded EN/LTR check; preserve desktop. Hard12min/stop7. No save/domain/backend/runtime restart/proof replay.
Read210/ARCHITECT-REVIEW.md and task. Await210-R1 result; do not issue another task meanwhile. Preserve real draft01a16eeb-70b3-7000-87c2-564c82835496, actual revision/unsaved work, Host5088 and FE3000. Local-dev authorization persists.190–209 functional accepted, do not replay. TB185paused/defaultOFF. Older CURRENT sections below are historical and superseded.

﻿## Historical — 210 visual alignment posted / Architect reviewed — 2026-10-25 (superseded above)

User rejected 209-R1/R2 visual alignment (10/20) from two real screenshots: General formed a second
content axis, type floated in its own max-w box, language chips changed geometry, week was two columns of
independent day cards. 210 repaired ONLY presentation in the existing feature: one content
width for both main panels, type on the name card's axis, chips with one box model, and a seven-row week
grid plus header on one shared column template (narrow viewport = day+status, then full-width from/to).
Read `docs/architecture/evidence/TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210/RESULT.md`.
Measured at 1440x900: both panels w=1057, type/name inner x=73 w=1023, all seven week rows identical
(day 481/607, start 209/120, end 81/120), 390x844 no horizontal overflow. No Apply/HTTP write/migration;
stored 120000.00/5000.00 unchanged. typecheck clean. Hard 20min timebox was exceeded (~32min) — recorded,
not graded PASS. User Visual ACCEPT PENDING.
# Tooba — Agent Rules

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


## CURRENT HANDOFF —209-R2 posted / awaiting Architect review —2026-10-24

209-R2 executed frontend-only under the standing local-development authorization. Read
`docs/architecture/evidence/TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R2/RESULT.md`.
Fixed the one remaining real defect: `ServerFieldIssue` now keeps `groupIndex` AND `rowIndex` from the
submitted path, dedup keys include group+row+owning input, the screen resolves the submitted index against
the current ordered groups/rows via `rateRowKey(g,r)` (no rowIndex-only map shared across groups), focus is a
scoped selector (intended group element → intended row element → actual input) with no document-wide Nth
match, out-of-range indices highlight nothing instead of row0, and a rejected optional-condition input opens
ONLY its enclosing disclosure (presentation only: no value, dirty flag or revision change).
typecheck clean; 10/10 new two-group regressions + 26/26 across the three focused 209 files.
No browser/runtime/screenshot, no save replay, no seed/migration/Host restart; real rev2 draft untouched.
Active task: NONE. Next task: NONE — wait for Architect review, then hand the same live URL to the user for
retest; do not open another automatic visual/proof loop. Host5088/FE3000 preserved; TB185 paused.
Timebox: actual ≈12 min vs 10 (reported honestly, one correction round).

## SUPERSEDED —209-R2 issued task —2026-10-24

Only issued TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R2,parent209-R1;10min/stop6.
R1 presentation/view improvements accepted after Architect viewed all4screenshots; not userVisualACCEPT.
Actual remaining bug: groupIndex discarded; row errors/focus/dedup rowIndex-only and globalNth.
Fix composite group+row ownership +open targeted optional disclosure before focus; focused tests/typecheck only.
No UI rewrite/browser/save/data/migration/runtime/proof replay. After corrected result review, user retest;
no automatic visual/proof follow-up. Read TOP recovery and209-R1/ARCHITECT-REVIEW.md.
Prior localdev authorization persists; preserve populated draft/backend/FE.


## CURRENT HANDOFF —209-R1 posted / awaiting Architect review —2026-10-24

209-R1 executed under the standing local-development authorization. Read
`docs/architecture/evidence/TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R1/RESULT.md`.
Delivered: truthful VIEW (rate rows as text via `RateRowSummary`, group title as text, zero controls in the
Regions panel) plus handler-boundary `blockedNow()` guard for view/saving/reconciling; compact professional
General (no duplicate type card, one-line owner, `max-w-4xl` content) and 4-field rate rows with a labelled
collapsible optional-conditions block whose summary always states the active condition; exact server-error
placement by path segment + exact registered code (description→description, `rows[i].minDeliveryDays`→that
rate row, timezone.required≠invalid, unknown code never guessed, codes in collapsed diagnostics);
timezone-options comment corrected to a suggestion list. typecheck clean, 10/10 new + 6/6 existing focused
tests. Live mounted read-only inspection at 1440/390 on the same rev2 draft with real GETs only; local edit
then Cancel→discard restored `120000.00`; no Apply/PUT and no domain mutation.
Active task: NONE. Next task: NONE — wait for Architect review; do not call the user for a full retest until
these defects are accepted. Host5088/FE3000 preserved; migrations/seeds/save not replayed; TB185 paused.
Timebox: actual ≈37 min vs 30 (reported honestly, one correction round).

## SUPERSEDED —209-R1 issued task —2026-10-24

Only issued task TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R1,parent209;30min/stop22.
209 live save/readback accepted; user Visual ACCEPT absent. Architect viewed actual desktop PNGs:
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


## CURRENT ISSUED TASK —209 result posted / awaiting review —2026-10-24

Read TOP canonical recovery + `docs/architecture/evidence/TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209/RESULT.md`.
209 executed under the existing local-development authorization: real root causes (chip labels from
picked rows, field-level server error mapping for `shipping.schedule.timezone.invalid`, lawful
time-zone selector, one real active `عادی` type via existing catalog API, «محدودهٔ ارسال» wording).
Real evidence on draft `01a16eeb-70b3-7000-87c2-564c82835496`: one UI Apply -> revision 2, fresh GET +
browser reload show name/type/destination/fees/schedule. Worker verdict READY_FOR_USER_RETEST.
Active task: NONE. Next task: NONE — wait for Architect review and user retest.
208 read-only handoff did NOT prove the whole workflow; this correction stands. No user Visual ACCEPT,
no activation/readiness, no Production-Functional-Acceptance. Do not replay 199–209.
Host :5088 (PID 10800) and FE :3000 left running; migrations were not re-applied.
TB185 paused; default-OFF/PendingAllocation unchanged.

## SUPERSEDED HISTORICAL —208 / 207 / 206 blocks —2026-10-24

Only issued task: TB-SHIPPING-ADMIN-LOCAL-STACK-REFRESH-AND-USER-TRIAL-208, parent207.
User confirms NON-production local DB and explicitly authorizes necessary checked-in dev migrations,
Host5088 refresh and normal existing startup seed/backfill/workers, minimal ONE trial draft via real API if absent,
and narrow relevant integration repair. Do not ask again for these same actions or retain old approval holds.
40min hardbox/implementation stop30; reuse207 build, preserve frontend3000, keep successful Host running.
Read TOP recovery and task. Target verification still required; no external/production/reset/drop/auth bypass.
Await208 result; user is called to test only after actual real API+mounted route readiness.
Old207 approval-pending paragraphs below are superseded historical context.


## CURRENT CHECKPOINT — 207 reviewed / local schema approval pending — 2026-10-24

207 build and bounded schema observations accepted with corrections in207/ARCHITECT-REVIEW.md.
Read TOP canonical recovery +207/EXECUTION-PLAN.md. Worker task NONE; next task NOT ISSUED.
Await explicit approval for ONLY nine named local Shipping/Fulfillment migrations, conditional on full
all-module pending set matching exactly, target/collision/backup checks. No Host restart/seed approval.
Runner has NO module filter: apply walks all descriptors. Seven Shipping +two Fulfillment pending are
not yet proof that every other module is current. Never apply broader changes under this permission.
New binary retained, not run; live draft/auth/readiness pending. Do not replay190–205.


## Historical issued task — 207 — 2026-10-24

Only issued task: TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207 (parent206).
206 blocker diagnosis accepted: stale Host5088 binary; live auth/schema/draft readiness unverified.
207:20min/stop14, read-only actual migration history + startup effects + ONE isolated currentHost build,
concrete refresh/rollback plan. NO shared restart, migrations/seeds, database writes or new production code.
Program.cs Development startup performs migration/seeds even with legacy bootstrap disabled.
Read TOP canonical recovery and206/ARCHITECT-REVIEW.md. Await207 result, no replay190–205.


## Historical issued task — 206 — 2026-10-24

Only active/issued task: TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206, parent205-R3.
User authorized continuing REAL existing-draft connectivity/read-only readiness before their trial.
20min hardbox, stop14. Read real runtime/auth/list/document/types and mount without mocks;
deliver usable route or first concrete blocker. No merchant writes, draft creation, migrations,
shared restarts, broad build/tests or production changes. Await206 result; do not replay190–205.
Issued is not claimed/started; worker lifecycle must establish that separately.
This block supersedes all historical NONE/await-user statements below.

## Prior checkpoint — Shipping checkpoint 2026-10-24

Read only the TOP current checkpoint first in
`docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md`, then the
205-R3 and 205-R2 ARCHITECT-REVIEW.md referenced there.
Latest Architect acceptance commit: 750b4d73e1e3561d53ddd7b2e7170136e2933f5a.
Current phase: USER_TRIAL_EXISTING_DRAFT_AUTHORING. Active task=NONE; awaiting result=NONE.
Next objective: user trial / source-grounded real-environment connectivity for an EXISTING authorized draft;
live runtime, registry/data and production acceptance are still unverified. No new task has been issued.
Do not replay 190–205, reopen accepted capabilities, invent a proof loop, or infer live readiness from synthetic browser evidence.
The recovery top section includes accepted-task ledger, actual UI/API paths, remaining limits and exact continuation.
Older active-task statements below are historical, not current authorization.

## Historical Shipping recovery — 2026-10-22

Chat-loss checkpoint: the latest section at the top of the recovery document
records accepted work, completed R8, unresolved gates and the exact continuation.
R8 is completed and Architect-accepted for metadata only; do not replay it.

For the current Shipping Admin work, start at
`docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md`.
TB-190 including R1/R2 is accepted for the bounded owner/region draft foundation.
TB-191 manual rate draft persistence is accepted through TB-191-R1.
TB-192 owner-local multilingual draft metadata is accepted through TB-192-R1.
TB-193 weekly schedule draft persistence is accepted through TB-193-R1.
TB-194 independent freight/calculation draft settings are accepted (authoring only).
TB-195 type ownership analysis is accepted with Architect corrections (rules section 26).
TB-196 Fulfillment type catalog/API/Contracts/schema/storage is ACCEPTED through R13-R1.
This is bounded catalog acceptance, not live Shipping integration, UI, release or AMSC recertification.
TB-196-R1 remains PARTIAL; its bounded Domain regressions are repaired and accepted through R2.
TB-196-R3 API evidence is accepted through R4; registry/tracing/real-DI/relational gates remain open.
TB-196-R5 DI/registry/listener evidence and its single-active correction through R6 are accepted.
TB-196-R7 naming/isolated UpDown repair is accepted; legacy/snapshot and full relational gates remain blocked.
TB-196-R8 canonical snapshot/latest TargetModel metadata alignment is accepted.
TB-196-R9 historical analysis is accepted with Architect corrections (model manifest191, not216).
TB-196-R10 pending-outbox-index migration and bounded isolated proof are accepted.
TB-196-R11 existing CHECK/name model alignment is accepted; five defaults remain.
TB-196-R12 five-default model alignment and strict schema parity are accepted with recorded deviations.
TB-196-R13-R1 repaired winner/full-tree evidence is accepted; catalog proof stage closed.
Only next issued task: TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197,
Shipping-owned assignment via existing Fulfillment.Contracts, server-authorized owner, shared Revision.
TB-197 was executed on 2026-10-22 and its result is posted to Bridge; it is NOT Architect-accepted yet.
TB-197-R1 (post-commit lookup repair + focused same-context/provider-failure/DI+parent-trace proof) was
executed on 2026-10-22 from `cc24c9c1` and posted to Bridge; NOT Architect-accepted. Await review only.
No shared deployment or quote activation. Follow rules section27; do not replay the catalog tasks.
Do not replay historical tasks or resume TB-185 performance. Fetch current HEAD
and verify actual task state;
this is a recovery pointer, not a new implementation authorization.

## Current accepted checkpoint — 2026-10-24 — LOCAL STACK REFRESHED, REAL TRIAL HANDOFF

TB196–204 bounded capabilities remain accepted through their latest reviewed repairs.
TB205-R2 successful intercepted composite-save/duplicate/locale evidence and TB205-R3 mounted
invalid/discard, canonical409 and transportUnknown/explicit-reconcile work protection are Architect-accepted
within their reviews. R3 source/evidence: d6fd1ba0c7e43fe3493f91b77a6bd5a6402c3e76.
TB206 diagnosed the stale `:5088` binary; TB207 proved the pending migrations and built the retained Host.
TB208 (base `822a8ea8`) executed under the user's explicit local-development authorization: the nine
pending Shipping/Fulfillment migrations are APPLIED via the existing tenant-scoped MigrationRunner,
`Tooba.Host` was refreshed on `:5088` (PID 10800, `D:\temp\tb207-build\out`, Development), the shared
frontend `:3000` was left untouched, and ONE real existing-service draft
(`01a16eeb-70b3-7000-87c2-564c82835496`, `post`, no option) exists for the user trial. A real
`owner.ownerKind` vs `owner.kind` decoder mismatch was repaired with a focused 7/7 adapter test.
Read the TOP canonical recovery and TB208/RESULT.md first after chat loss.
**State: READY_FOR_USER_TRIAL** for the existing-draft authoring slice; result awaiting Architect review.
Active task: NONE. Next task: NONE — wait for Architect review, do not replay 199–208.
Trial URL: /admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
No automatic draft creation; the active type catalog is genuinely empty and was not seeded.
Authoring WRITE acceptance was not exercised; Production-Functional-Acceptance remains PENDING, and
intercepted/prior evidence is not live integration, merchant mutation, deployment or user Visual ACCEPT.
Whole-method creation, activation/readiness, cutoff/carrier/funding and runtime quote are outside this slice.
TB185 paused; default-OFF/PendingAllocation unchanged.
Historical active-task paragraphs above/below are not current authorization.

## Core rules

1. One active Task only.
2. Execute only the Task actually received from Bridge on the configured channel
   after `BRIDGE-WAKE`.
3. `Worker PASS != Architect ACCEPT`.
4. Bridge is the transport/orchestration boundary; the repository is durable
   technical Source of Truth.
5. The Coding Agent is normally **IDLE / OFFLINE** between Tasks.
6. Do **not** poll Bridge continuously while idle.
7. Do **not** require permanent online Worker presence or idle heartbeat.
8. `BRIDGE-WAKE` is infrastructure control traffic, not a Task or Result.
9. `SYSTEM-BRIDGE-ALERT` is not a Result and must not be treated as one.
10. Do not emit or interpret an alert merely because the Worker is offline
    between Tasks.
11. Do not invent requirements, broaden scope, or redesign locked architecture.
12. Do not self-authorize the next Task.
13. Do not force-push, rewrite history, silently stash, or destroy unrelated
    work.
14. Do not modify unrelated files.
15. Do not mark Architect ACCEPT unless the Task explicitly authorizes it.
16. Do not replay historical task files from `docs/ai/tasks/` as current work.
17. Home and PDP are critical Shopeiva-locked surfaces. Any Task touching shared
    storefront components must run `npm run test:critical-storefront` and follow
    `docs/visual-baselines/CRITICAL-STOREFRONT-VISUAL-REVIEW.md`. Functional PASS
    does not imply Visual ACCEPT.
18. **Never POST test, probe, or placeholder payloads to Bridge `/api/results`.**
    Only the complete Task Result contract (`BEGIN_TOOBA_WORKER_RESULT` …
    `END_TOOBA_WORKER_RESULT`) after validation, commit, and push. Do not probe
    the Results API with dummy `content`.

## Current pipeline

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
```

```text
ARCHITECT
→ downloadable .task.md
→ Tampermonkey
→ Bridge Task = Pending
→ External Watchdog
→ BRIDGE-WAKE
→ Coding Agent wakes
→ claim exactly one Task
→ implement
→ Result
→ Bridge
→ Tampermonkey
→ ARCHITECT
→ ACCEPT / REPAIR / BLOCK
→ next .task.md
```

## Worker lifecycle on BRIDGE-WAKE

1. recover `main` and verify `HEAD == origin/main` on a known/safe tree;
2. read governance and recovery docs;
3. check Bridge health;
4. claim exactly one Pending Task on the configured channel;
5. persist the received downloadable `.task.md` artifact;
6. execute only that Task;
7. validate, commit, push, and post the complete Result through Bridge;
8. complete the Bridge task lifecycle;
9. return to **IDLE** and stop.

## Retired transport

The following are historical only:

- manual Architect envelopes pasted into a Coding Agent;
- same-session or same-chat continuation;
- Cursor browser/composer/send-button transport;
- requiring one specific agent product;
- **BRIDGE-V2 continuous polling while idle**;
- **permanent idle heartbeat as a normal prerequisite**;
- **Worker waiting loop between Tasks**.

Historical task/result artifacts may retain legacy syntax as evidence only.

## Source of Truth

Before normal execution:

```text
branch = main
HEAD == origin/main
known/safe working tree
```

After successful execution:

```text
local commit
push origin main
git fetch origin
HEAD == origin/main
clean working tree
```

Unsafe divergence is `RECOVERY_CONFLICT`.

## Frontend feature architecture — mandatory

For EVERY feature addition or change under `src/frontend/**`, BEFORE coding read
`.cursor/skills/tooba-frontend-feature/SKILL.md` and
`docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md` and follow its ownership,
dependency-direction, API/state/UI, type-safety, focused validation, and stop rules.
This is required even when a task only says "fix" or "add a feature"; merely asking
the agent to act as a senior architect does not replace the repository contract.
Preserve `ARCH-FE-FREEZE-001`, existing visual/API/behavior locks and Task scope.
A conflict or out-of-scope refactor requires STOP and Architect approval.
Never claim a page is certified just because a feature builds.

## Backend feature architecture — mandatory

For EVERY authorized feature addition or bug fix under `src/backend/**`, BEFORE
coding read `.cursor/skills/tooba-backend-feature/SKILL.md` and follow its
module ownership, CQRS, Result/error/localization, validation, Contracts-only,
DI, security, and focused-test rules. The feature skill prevents new debt; it
does not replace the architecture skills or authorize AMSC recertification.
Preserve existing contracts and locked state; conflicts require STOP and
Architect approval. Never silently broaden scope or weaken architectural guards.

## Deferred audit findings — opt-in only

Do NOT read or scan `docs/architecture/ARCHITECTURE-FINDINGS-REGISTER.md`
during ordinary tasks, feature development, bug fixes, or screen audits.
Do NOT automatically add audit findings to it. The register is consulted
ONLY when the Architect/user explicitly requests adding, updating, reviewing,
or resolving entries in that register. Before adding an entry, search existing
entries for duplicates and update the existing ID rather than creating another.
Keep access targeted (search relevant IDs/terms before reading large sections)
to minimize context usage. The register never authorizes tasks, AMSC waves,
baseline resets, or scope expansion.

## Agent skills

Repository-owned skills live under `.cursor/skills/<name>/SKILL.md` and are
versioned in git, so they survive a Cursor reset, cache clear, or machine change.
After a reset, read the durable registry `docs/ai/AGENT-SKILLS.md`, then open the
matching `.cursor/skills/<name>/SKILL.md` and follow it exactly.

## Shipping Admin rules — mandatory reference

Before designing or changing shipping Admin/seller rules, read
`docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md` (`SHIPPING-ADMIN-RULES-001`).
Preserve one owner-scoped control panel, shipping type above method name, one
non-overlapping multi-region selection with multiple condition/price rows beneath
it, and reuse of that geography by packaging and scheduling. Payment timing and
rate calculation mode are independent. Do not reintroduce duplicated geography,
raw location IDs, cross-owner inheritance or seller-only customer grouping.
Multilingual fields, configured locales, shared non-translated rules, and RTL/LTR
are mandatory (decision sections 10–11). The TB-186-R2 visual preview baseline was accepted by the owner on 2026-10-22.
Decision sections 13–15 add authoritative single-store/marketplace capability
handling and database-backed configured languages. Backend delivery is authorized
only through the single active Bridge task and repository AMSC/feature skills;
visual acceptance is not production activation, integration acceptance or module
certification. Do not hardcode two locales, trust browser owner/mode, or create
parallel mode/language authorities.
This records approved requirements, not implemented capability or permission to
weaken guards; report migration/contract conflicts explicitly.

## Shipping customer experience — mandatory reference

For shipping-related Admin, seller or Storefront work affecting customer delivery
groups, costs or delivery timing, first read
`docs/architecture/shipping/SHIPPING-STOREFRONT-UX-REFERENCE.md` and inspect its
linked user-provided image. Preserve `SHIPPING-STOREFRONT-UX-001`: clear delivery
groups, per-group cost and independent delivery date/window choices backed by real
availability. Never substitute raw technical codes or seller-only grouping for
this approved experience. The reference is a target, not implementation evidence
or permission to bypass existing locks. Changes to its approved UX require an
explicit product-owner decision.

## References

- `docs/ai/AGENT-SKILLS.md`
- `docs/ai/TOOBA-PIPELINE-PROTOCOL.md`
- `docs/ai/TOOBA-PIPELINE-CONTROLLER.md`
- `docs/ai/TOOBA-RECOVERY-CONTEXT.md`
- `docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md`
- `docs/PROJECT-STATE.md`
- `docs/ROADMAP.md`
