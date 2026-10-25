# TB212-R1 — corrected runtime source facts and the lossless bridge decision

Source-only correction. Base `3296121d7e34d4f692dcfd0f86c794b269e66a1d`. No src/config/flag/schema/DB/build/test/browser/probe run.

## 1. Correction: the operational/packaging seams ARE implemented and registered

`Infrastructure/DependencyInjection/ShippingModule.cs` (`AddServices`) registers, among others:

- `IShippingRateQuoteLookup → ShippingRateQuoteLookup` (reads `RatePolicies`/`CoverageGrants`/`MethodCoverages`)
- `IShippingPackagingFeeLookup → ShippingPackagingFeeLookup`
- `IShippingShipmentCostLookup → ShippingShipmentCostLookup` (freight component + packaging component, never summed optimistically)
- `IShippingOperationalQuoteLookup → ShippingOperationalQuoteLookup`
- `IShippingRateCoverageDirectory` / `IShippingMethodCoverageDirectory` / `IShippingPackagingPolicyDirectory` / `IShippingOperatingScheduleDirectory` (the admin write seams)
- `ShippingDbContext` bound through `ICurrentCommerceContext` + `IDatabaseConnectionResolver` (tenant/database isolation is real)

`Infrastructure/Operations/ShippingOperationalQuoteLookup.cs` loads four module-owned slices (`RatePolicies`, `CoverageGrants`, `PackagingPolicies`, `OperatingSchedules` with `Windows`) with a service+country slice and delegates to `Domain/Operations/ShippingOperationalQuoteEvaluator.Evaluate`, which computes freight + packaging + total, splits `AmountPayableAtCheckout` vs `AmountCollectOnDelivery` by `FreightPayerMode`, and returns stable reason codes (`FreightNotQuoted`, `PackagingAmbiguous`, `CurrencyMismatch`, …). So 212's "contracts only / no implementation" is **retracted**; 211's "config + catalog only" is retracted with it.

Two further corrections accepted from review: (a) service/option/geography/currency identify **applicability**, not tenant or seller **authorization** — tenant binding lives in the DbContext, so "no owner column" ≠ "no isolation"; (b) `ShippingOperationalQuoteRequest.RequestedPickupAtUtc` / the schedule evaluation is **carrier pickup/dispatch availability**, explicitly NOT a customer delivery promise.

## 2. Reuse map and semantic compatibility (conversion is NOT proven)

| authored (per-row, group-level) | existing runtime structure | verdict |
| --- | --- | --- |
| destinations per group | `RatePolicy`/`CoverageGrant` scope (country/province/city/service area) | compatible |
| freight per row | `RatePolicy.ChargeAmount` + weight/count `Condition` | compatible |
| **packaging per row** (`PackagingAmount` on the row) | `PackagingPolicy.FeeAmount` resolved by **identity + destination only**, deepest scope wins, `Ambiguous` when tied | **NOT compatible** — two rows in one area with different packaging fees cannot both be represented; a single policy per area/currency silently reprices the other rows (decision §133 requires packaging adjacent to each rate row) |
| **merchandise min/max** | no merchandise axis in `RatePolicy` | **not representable** |
| **delivery min/max days per row** | `PreparationDays` + `LeadDays` (two ints); schedule is pickup windows | **no approved mapping** — inventing prep/lead split is policy, and the pickup schedule is not a delivery promise |
| **`CalculationMode` Manual / CarrierApi / Free** | `Manual` maps to rows; `Free` has no runtime field (zero `ChargeAmount` would lose the real cost + supplier, decision §82/§83); `CarrierApi` has no provider integration | **not representable** |
| editor week (7 days, per-day hours, timezone) | `OperatingSchedule` = carrier pickup/dispatch | **mechanically equal, semantically different** — not interchangeable without a product decision |
| owner Store / Marketplace / Seller | no owner/seller field on runtime aggregates; tenant isolation exists, seller **authorization** does not | **not representable** for seller/marketplace |

Therefore a "copy draft into runtime aggregates" publish is **provably lossy**, and per the review a lossy operation must be **rejected in full before any mutation** — not activated partially with warnings. Existing `IsActive` flags also carry no draft-revision provenance, no manifest and no owned rollback, so flipping them is not publication.

## 3. Chosen minimal next slice: Shipping-owned published revision snapshot (read via Contracts, no Order draft access)

Not a second quote authority: Shipping gains an immutable, owner-scoped **published revision** of the method it already owns, and the existing quote path is *extended* to be able to read it; Order still reads only Shipping Contracts.

- Domain (`Shipping.Domain/Publication/`): `ShippingPublishedMethod` — immutable row set created from one draft revision: owner identity (`OwnerKind`, tenant, seller party when applicable), `ServiceCode`/`OptionCode`, `Revision`, and children `ShippingPublishedArea`, `ShippingPublishedRateRow` (freight **and** packaging, merchandise bounds, min/max delivery days, matched conditions), `ShippingPublishedWeek` (timezone + windows as authored), `ShippingPublishedSettings` (`FreightPaymentMode`, `CalculationMode`). Lossless by construction: every authored field has a column, including the ones the legacy aggregates cannot hold.
- Contracts (`Shipping.Contracts/Publication/`): `IShippingPublishedMethodReader` (by service/option/destination/currency, owner-scoped) + snapshot DTOs with explicit `PublishedRevision`.
- Infrastructure: `ShippingPublishedMethodDirectory` (EF mapping + migration, exact DB-validated), one transaction per publish, `ExpectedRevision` optimistic guard, owner scope from `ShippingAuthoringOwnerScope`.
- Publication is a **write**: insert immutable revision + move a single `PublishedRevision` pointer; rollback = repoint to a prior revision. Pre-existing runtime rows are never deactivated.
- Consumers: **none in this slice** (`StorefrontShippingOwnedRateResolver` unchanged), so no activation policy is guessed, no flag flips, gate stays default OFF.

Acceptance (focused, when implemented): round-trip fidelity per authored field (incl. two rows with different packaging fees in one area, merchandise bounds, delivery range, Free/Manual mode, week); stale `ExpectedRevision` rejected; seller/marketplace draft rejected with a stable code until ownership policy exists; no mutation on rejection.

Still-undecided product policy (blocker, named not guessed): (1) whether week means customer delivery or carrier pickup in the new model; (2) `CalculationMode` runtime semantics (`Free` cost/supplier reporting, `CarrierApi` provider); (3) seller/marketplace ownership authorization; (4) activation/go-live semantics for a published revision. None of these are needed to build the snapshot + reader above, which is why it is the safe next code step; connecting it to Order stays a later slice.

Proposed 213 (copy-into-legacy-aggregates publish) is **withdrawn**.

## References

`ShippingModule.cs:AddServices`, `Operations/ShippingOperationalQuoteLookup.cs`, `Domain/Operations/ShippingOperationalQuoteEvaluator.cs`, `Domain/Packaging/ShippingPackagingFeeResolver.cs`, `Domain/Aggregates/ShippingRatePolicy.cs`, `Contracts/Operations/ShippingOperationalQuoteContracts.cs`, `Contracts/Rates/ShippingRatePolicyContracts.cs`, `Contracts/Authoring/ShippingAuthoringDraftContracts.cs` (`ShippingAuthoringCalculationMode`), `docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md` §33–34, 82–83, 114, 131–133, 151, 165.

## Bounds/timing

Docs/evidence only; no code, flag, data, runtime or browser change; no gate enable, TB185 stays paused, default-OFF unchanged; nothing connected so no storefront completion claim. Claimed 08:49:04Z, analysis complete ~08:55, evidence/commit ~08:59 — inside the 10-minute box with the minute-6 analysis stop respected. STOP_FOR_ARCHITECT.

Deviation (reported, not hidden): the commit used `git add -A`, so pre-existing untracked evidence artifacts from earlier tasks (`post-result.js`, `result-contract.txt`, task receipts/worker-result files under `docs/architecture/evidence/**`) were included alongside this task's files. They are documentation/evidence only — no `src/`, config, flag, schema or data file was touched — and no history rewrite/force-push was performed. Commit `94d059e2e33e7e0e9060529c8195119024d817d4`.
