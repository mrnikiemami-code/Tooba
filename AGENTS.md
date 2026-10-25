## CURRENT —219 review: two bounded write defects; only219-R1 issued —2026-10-25

Source eca53dae9c56bfb4b7f2be91eb54da64e1464319 independently compared with4972c6c8af39c000bceaebe4f0846540ec37f107. Main verified exact fullSHA (worker's HEAD line was truncated typo). Retain selected-pointer architecture/schema/Contracts reader and existing219 proofs, but do not close219 before R1.
Defect1: SelectAsync/ClearAsync use tracking ScopedPointer queries; a clean stale tracked pointer survives materialization. After another context advances it, an old expected version can return false no-op success (Select old target or Clear stale-cleared) and a current expected version can be rejected incorrectly.
Defect2: Domain Select/Clear mutate SelectedSnapshotId BEFORE NextVersion can throw at long.MaxValue. Exception occurs before TrySave cleanup; later caller SaveChanges can leak the rejected target/clear without a version advance.
Only issued TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219-R1,parent219: authoritative fresh pointer before comparison + exception-atomic domain mutation, focused real15432 regressions. Preserve tracked object identity/unrelated caller work; refresh only clean owned pointer deliberately, never ChangeTracker.Clear.
Active219-R1, await result. No220. No generic proof loop or new publication architecture. 219 worker9 real PostgreSQL +9 affected216 class reported green; root source review only. New cases absent from those tests. Hard30 respected total29 but implementation-stop20 exceeded; no process PASS.
Still NOTLIVE: no admin publication action/Order selected-plan consumer/projection-selection-commit. After repair close219 and proceed actual publication/fulfillment-plan integration, no new calculator. Physical measurements unknown; §29/211–218 remain closed.
214 and219 migrations NOT shared-applied, nine old migrations applied. Preserve actual draft/revision/unsaved tabs, Host5088/FE3000, TB185paused/defaultOFF/PendingAllocation. Local development authorization remains valid later. No Bridge/historical post-result script/external message; exact paths only. Historical CURRENT blocks below are superseded.

## CURRENT —219 selected-capture pointer + Contracts read done; NOT wired / NOT live —2026-10-25

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
