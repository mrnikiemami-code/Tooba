# TB212 — draft → existing runtime bridge: reuse decision and first slice

Source-only contract decision. No src/package/config/flag/schema/runtime/browser change; no test/build/DB run.
Base `51f65febfc15bdf1c9fdbaade5215def2ca88a3e`.

## 1. Who owns runtime quoting today (exact members)

| concern | authoritative member | owner |
| --- | --- | --- |
| rate/coverage decision | `Shipping.Contracts.Rates.IShippingRateQuoteLookup.QuoteAsync(ShippingQuoteRequest)` → `Shipping.Infrastructure/Rates/ShippingRateQuoteLookup` → `Domain/Rates/ShippingRatePolicyEngine.Quote` | Shipping (reads own `ShippingDbContext.RatePolicies` / `CoverageGrants` / `MethodCoverages`) |
| request shape | `ShippingQuoteRequest(ServiceCode, Destination: DestinationIdentifiers, CartSubtotalExclusive, CurrencyCode?, OptionCode?, Parcel?)` + init `OptionMatchMode` (default `LegacyFallback`) | Shipping |
| measured inputs | `ShippingMeasuredParcel(WeightGrams?, ItemCount?)`; empty ⇒ conditioned rule fails closed | Shipping |
| packaging fee | `Shipping.Contracts.Packaging.IShippingPackagingFeeLookup` (contract exists; **no implementation registered**) + `Domain/Packaging/ShippingPackagingFeeResolver` | Shipping |
| combined freight+packaging+payer split | `Shipping.Contracts.Operations.IShippingOperationalQuoteLookup.QuoteAsync` (contract + `ShippingOperationalQuote` exist; **no implementation registered**, no consumer) | Shipping |
| runtime write authorities | `CreateShippingRatePolicyCommand` / `UpdateShippingRatePolicyCommand` / `SetShippingRatePolicyActiveCommand`, `CreateShippingCoverageGrantCommand`, `CreateShippingPackagingPolicyCommand` / `Update…`, `UpsertShippingOperatingScheduleCommand` / `SetShippingOperatingScheduleActiveCommand` | Shipping.Application |
| storefront consumer | `Order.Application/Storefront/Services/StorefrontShippingService.LoadEligibleMethodsAsync` → `Shipping/OwnedRate/StorefrontShippingOwnedRateResolver.ResolveAsync`, gated by `StorefrontShippingOwnedRateOptions.Enabled` (default OFF) | Order |

Candidate identity in Order: `StorefrontShippingOwnedRateCandidate(MethodCode "service:option", ServiceCode, OptionCode)`; the resolver re-checks currency, negative amount, `IsQuoted`, and sends `Parcel = null` always. Destination comes from `DestinationIdentifiers` or legacy province/city resolution through `IGeographyLookup`.

**Binding owner for a quote = Shipping's `ServiceCode` + `OptionCode` + geographic scope + currency.** There is no Store/Marketplace/Seller column in `ShippingRatePolicy`; ownership is geographic scope + service identity only.

Selection/commit parity: `ProjectAsync` and `CommitAsync` both route through `LoadEligibleMethodsAsync` (`CommitAsync` literally replays `SaveSelectionAsync` with the saved group membership), so projection/selection/commit share one price path — parity is structural, not UI-only.

## 2. Draft vs runtime: what the existing engine can represent

| authored (Shipping.Domain/Authoring) | runtime counterpart | verdict |
| --- | --- | --- |
| group destination nodes (country/province/city/service area), one non-overlapping multi-region set | `CoverageGrant.Scope` + `RatePolicy.Scope` (Country/AdministrativeAreaId/CityId/ServiceAreaId) | **exact** — one policy+grant row per node, same amounts |
| `FreightAmount` + `PackagingAmount` separate | rate `ChargeAmount` (freight) + packaging policy `FeeAmount`; sum only in the operational seam | **exact** — must NOT be summed into `ChargeAmount` |
| `min/maxWeightGrams`, `min/maxItemCount` | `RatePolicy.Condition` weight/count tiers | **exact** (runtime needs a real parcel; storefront resolver sends none today) |
| `min/maxMerchandiseAmount` | none — no merchandise-amount axis in `ShippingRatePolicy` | **absent** |
| `min/maxDeliveryDays` range | `PreparationDays` + `LeadDays` (two discrete ints) | **not 1:1** — needs an explicit policy mapping, do not invent |
| weekly schedule: timezone + 7 weekday windows | `ShippingOperatingSchedule` (`TimeZoneId`, windows with ISO weekday) | **exact** |
| method settings `FreightPaymentMode` | `ShippingPackagingPolicy.FreightPayerMode` (Prepaid/CollectOnDelivery) | **exact** |
| method settings `CalculationMode` | none | **absent** (authoring-only) |
| `ShippingTypeId` (Fulfillment catalog) | no runtime role in rate/coverage/quote | **presentation only** |
| per-language group titles | runtime labels come from the Fulfillment catalog (`IShippingCatalogReader` translations) | **absent** for rate; not authored here |
| owner `OwnerKind` Store/Marketplace/Seller + tenant | no owner column in `RatePolicy` / `CoverageGrant` / `OperatingSchedule` | **absent** — seller-scoped pricing is not representable |
| currencies per group | `RatePolicy.CurrencyCode` (single) | exact per policy |

## 3. Save vs publish

Current `SAVE` writes only `ShippingAuthoringDraft` + child aggregates (groups, rate rows, weekly schedule, method settings, presentation, type assignment); `Shipping.Application/Authoring/*` has no publish/activate/promote command, and Order has no reference to authoring. So save is **authoring-only** and **no approved publication boundary exists** in code or in the decision docs — nothing currently turns a draft into customer-visible pricing.

Decision taken here (justified, minimal): **publishing must copy the draft into Shipping's existing runtime aggregates** (`RatePolicy`, `CoverageGrant`, `MethodCoverages`, `PackagingPolicy`, `OperatingSchedule`) through the existing Shipping commands — a snapshot per publish, keyed by draft `Revision`, not a live read of draft tables. Reasons: (a) runtime readers stay untouched (`IShippingRateQuoteLookup` never sees drafts); (b) unsaved/unpublished edits cannot leak to customers; (c) rollback = reactivate prior snapshot rows, which the `…ActiveCommand`s already support; (d) direct draft reads would give Order a second price authority and require Shipping to expose draft internals. `Activated` state = the existing `IsActive` flags on those aggregates, **not** a new flag.

## 4. Next executable slice (one, bounded)

**`TB-SHIPPING-ADMIN-DRAFT-PUBLISH-SNAPSHOT-213`** — Shipping module only, no Order change.

- Contract: `Shipping.Contracts.Authoring.ShippingAuthoringPublishContracts` — `PublishShippingAuthoringDraftRequest(Guid DraftId, long ExpectedRevision)` → `ShippingAuthoringPublishResult(Revision, PublishedRatePolicyIds, PublishedPackagingPolicyIds, PublishedScheduleId, UnrepresentableAxes[])`. New interface in Contracts, implemented in `Shipping.Infrastructure` over the module's own directory ports.
- Application: `Authoring/Commands/PublishShippingAuthoringDraftCommand` (+ handler) that, in ONE transaction, for the draft's `ServiceCode`/`OptionCode`: upserts `RatePolicy` per group×destination-node (freight as `ChargeAmount`, weight/count condition), `CoverageGrant` per node, one `PackagingPolicy` per currency (packaging amount + `FreightPayerMode`), one `OperatingSchedule` (timezone + weekday windows), and sets prior snapshot rows inactive. Revision guard: reject when `ExpectedRevision != draft.Revision`; reuse `ShippingAuthoringOwnerScope` for the actor check.
- Explicitly **rejected by design**: merchandise-amount and delivery-range and `CalculationMode` are reported in `UnrepresentableAxes[]` (stable codes), never silently dropped or guessed; no seller-owner publishing until ownership is decided (below).
- Consumer: none (no Order change). Acceptance: focused tests that a published draft is quoted by `ShippingRatePolicyEngine` through the existing `IShippingRateQuoteLookup` with the same amounts/currency/destination, that an unpublished revision changes nothing, and that a stale revision is rejected.
- Remaining slices after that: (a) populate a trustworthy parcel in `StorefrontShippingOwnedRateResolver` (weight/item-count conditioned rows stay fail-closed until a cart physical-measurement source exists); (b) an admin publish UX reusing the existing editor/wizard; (c) real local end-to-end Order→Shipping proof with the gate ON for one seller; (d) only then customer UI review — storefront Shopeiva DOM/CSS unchanged.

**One genuinely undecided product policy (must not be guessed):** whether Store/Marketplace/Seller ownership and per-language labels become first-class runtime fields, or drafts stay per-store (single owner) with type/labels remaining Fulfillment-catalog concerns. Safest bounded path: publish only `Store`-owned drafts in slice 1 and reject others with a stable code, leaving seller-scoped pricing out until decided. Also undecided: the `min/maxDeliveryDays` → `PreparationDays`/`LeadDays` mapping rule.

## Correction to 211

211's "runtime is config + catalog only" was incomplete: a gated `IShippingRateQuoteLookup` path already replaces authoritative prices (currency/destination validated, fail-closed, `StrictOwnedOption`), and an operational seam with payer split already exists as contract. 211's "read-only publish/activation" wording is withdrawn: publication is a **write** to Shipping-owned runtime aggregates; choosing `Fulfillment.Contracts` to own Shipping draft publication is rejected.

## Bounds/limits

Docs only. No quote engine built, no flag enabled, TB185 paused, default-OFF intact. No storefront functionality certification — nothing is connected yet.

## Correction appended by TB212-R1 (2026-10-25) — history above not rewritten

Architect review `ARCHITECT-REVIEW.md` corrected two source claims in this report:

1. "Contracts only / no implementation registered" is **FALSE**. `ShippingModule.AddServices` registers `IShippingPackagingFeeLookup`, `IShippingShipmentCostLookup` and `IShippingOperationalQuoteLookup`, and `Infrastructure/Operations/ShippingOperationalQuoteLookup.cs` really loads `RatePolicies`, `CoverageGrants`, `PackagingPolicies`, `OperatingSchedules` and delegates to `ShippingOperationalQuoteEvaluator.Evaluate`. Do not build duplicates.
2. Service/option/geography/currency identity is **applicability**, not tenant/seller **authorization**; tenant binding exists via `ShippingDbContext` + `ICurrentCommerceContext`. Seller scope is the real remaining gap.

Also withdrawn here: the proposed "copy draft into legacy `RatePolicy`/`CoverageGrant`/`PackagingPolicy`/`OperatingSchedule`" publication. Actual source shows that conversion is lossy (per-row packaging vs scope-only resolver, merchandise bounds, delivery range vs prep/lead, `CalculationMode` Free/CarrierApi, editor week vs carrier pickup windows, owner/seller identity), and the operating schedule is a carrier pickup window, not a delivery promise. TB212-R1 chooses instead a Shipping-owned immutable published revision snapshot read through Contracts, with no Order access to draft tables and no consumer/activation in that slice. **Task 213 as proposed is withdrawn; no publication implementation is authorized.** See `../TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212-R1/RESULT.md`.
