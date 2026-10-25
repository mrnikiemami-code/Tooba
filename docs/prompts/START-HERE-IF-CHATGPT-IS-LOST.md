## CURRENT —220 bounded ACCEPT; only221 ADMIN PANEL USER TRIAL, before more Storefront work —2026-10-25

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
