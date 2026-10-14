# TB-SHIPPING-MVP-CHECKOUT-SHADOW-057 — real storefront projection → shadow observation

Status: **PASS** (worker). Worker PASS != Architect ACCEPT. No live/runtime evidence (no Host restart,
forbidden). No cutover attempted and none is created by this task.

- Parent task: `TB-SHIPPING-MVP-GEOGRAPHY-ADMIN-056`
- Expected base SHA: `d734802ed6a7c823c778714cabd6376762d3fa6f`
- Program: `TOOBA-SHIPPING-MVP-FINAL-THREE` (second of the final three)
- Mode: `BOUNDED_CHECKOUT_SHADOW_INTEGRATION_NO_UNPROVEN_CUTOVER`
- Automatic next implementation task: **NONE**

## 1. The real call graph before this task (traced, unchanged in behaviour)

```text
StorefrontOrderEndpoints  (POST /storefront/shipping/projection, body StorefrontShippingProjectionRequest)
  → ProjectStorefrontShippingQuery { CartId, GuestSecret, ProvinceName, MethodCode, Language }
      → StorefrontShippingService.ProjectAsync          (Order.Application/Storefront/Services)
          → LoadEligibleMethodsAsync                    (legacy: IShippingCatalogReader + ShippingMethodRegistry)
              → TryAddMethod → StorefrontShippingCalculator.ResolveRate / IsDestinationAllowed / QuotePrice
                                                      (legacy rate authority = ShippingMethodsOptions config)
```

Legacy destination input on this path is **`ProvinceName` only** — a free-text/code string that the
Fulfillment `AllowedProvinces` list and `StorefrontIranGeography.Provinces` compare by exact name. There is
**no** country, city or service-area input on the projection path, and the cart subtotal/currency come from
the `StorefrontShippingProjection` result (`SubtotalExclusiveOfTax`, `Currency`).

`049` had installed the shadow evaluator but nothing in the live composition reached it (its own remarks said
"not connected to the composition root"). That is the observation/binding gap this task closes.

## 2. What this task adds (IMPLEMENTED)

Two new Order-owned files, one edited handler, one edited composition root — all inside the allowed paths.

| Path | Role |
| --- | --- |
| `Order.Application/Storefront/Shipping/RateAuthority/StorefrontShippingShadowObservation.cs` | Observation input/candidate, `StorefrontShippingShadowObservationCode` (6 stable values) and result. |
| `Order.Application/Storefront/Shipping/RateAuthority/StorefrontShippingShadowObserver.cs` | Gate short-circuit → deterministic legacy-destination resolution (Geography Contracts only) → per-candidate `StorefrontShippingRateShadowEvaluator` (Shipping Contracts only) → one stable outcome code + safe structured log. |
| `Order.Application/Storefront/Shipping/Queries/ProjectStorefrontShipping/ProjectStorefrontShippingQuery.cs` | The **real** projection handler now observes the shadow after a successful projection and returns the projection result untouched. |
| `Order.Infrastructure/OrderModule.cs` | Existing Order composition root binds the gate options section and registers the evaluator + observer exactly once. |

### Contracts-only, no new authority

* Destination identity comes only from `Tooba.Geography.Contracts.Places.IGeographyLookup.ResolveLegacyAsync`.
  No `GeographyDbContext`, no Geography entity, no cross-schema FK.
* Rate/coverage comes only from `Tooba.Shipping.Contracts.Rates.IShippingRateQuoteLookup`. No
  `ShippingDbContext`, no re-implementation of rate rules, no Fulfillment CRUD/identity change.
* The `StorefrontShippingProjection` returned to the caller is the same object produced by the legacy path;
  the observer result is discarded. `SelectedMethodCode`, `SelectedShippingAmount`, delivery dates, the
  cart shipping draft, `CheckoutSnapshot` and payment allocations are not read or written by this seam.

### Gate: permanently OFF by default

`StorefrontShippingRateAuthorityOptions.ShadowEnabled` and `PrerequisitesConfirmed` both stay `false`, and
`appsettings*.json` is untouched and contains **no** `Tooba:ShippingRateAuthority` section, so the bound
options are the safe defaults. `StorefrontShippingRateAuthorityPolicy` requires **both** flags; a single
flag cannot enable anything. There is no code path in this task that lets the observed price or coverage
become the customer price.

### Failure containment, privacy, cancellation

* `Disabled` path returns before any Geography or Shipping call — zero extra queries, no logging.
* Geography failure/absent schema → `ShadowUnavailable`; Shipping failure/absent tables → per-candidate
  `ShippingUnavailable`; neither escapes the seam. `OperationCanceledException` with a cancelled token is
  re-thrown so cancellation semantics are preserved.
* The structured log carries only the observation code, candidate count and the bounded set of outcome
  names (`none` when empty). No address, no person name, no raw customer text, no destination identifiers.

## 3. Exact source gap / bounded decisions (documented, not guessed)

1. **Province is the only destination input.** The legacy storefront never carries a country code; the
   observer passes the documented legacy country key (`LegacyCountryCode = "IR"`, the country the legacy
   catalog is explicitly limited to) plus the province text **verbatim**. Geography validates it; a key that
   does not exist yields `Unresolved` → `DestinationNotComparable`. The `047` seed proves the contract:
   country `IR`, area codes/labels identical to `StorefrontIranGeography` (`tehran`, `تهران`, …).
2. **No province ⇒ not comparable.** When the visitor has not chosen a province there is no destination
   request to compare; Geography could only answer at country level, and a country-level rate comparison
   against a destination-free legacy offer is not a valid observation. This branch is
   `DestinationNotComparable` and **no** lookup is issued. Nothing is inferred from free text.
3. **Ambiguous/Unresolved ⇒ not comparable.** The resolver's `Ambiguous`/`Unresolved` outcomes are reported,
   never collapsed to a single place.
4. **`LegacyDestinationAllowed`** is `true` for every candidate because each candidate arrives from the
   already destination-filtered legacy projection — a source fact, not an inference.
5. **No city / service-area input exists on this path**, so no city-level or service-area-level comparison is
   attempted; the resolved identity is area-scoped (country + administrative area).

## 4. Prerequisite data and tests before customer-facing Shipping authority

The following must exist and be proven **before** any cutover (this task deliberately performs none):

1. explicit, non-empty Shipping coverage grants and rate policies for every active service code used by the
   storefront (today the coverage set is empty and the engine is fail-closed);
2. parity evidence from this shadow seam showing **zero** mismatches over representative destinations and
   service codes (mismatch = cutover blocker);
3. a city/service-area-bearing destination input on the checkout path (the legacy projection carries only a
   province), so the request is as precise as the rate policies;
4. live evidence collected with `ShadowEnabled` + `PrerequisitesConfirmed` enabled under a separate,
   Architect-authorized task — and a tested explicit cutover flag, which this task intentionally does **not**
   introduce.

## 5. Validation performed

| Check | Result |
| --- | --- |
| `dotnet build Tooba.Order.Infrastructure` | Build succeeded, 0 errors (1 pre-existing CS0105 warning). |
| `dotnet test Tooba.Order.Tests` (whole module) | **281 passed**, 0 failed. |
| New shadow observer suite (`StorefrontShippingShadowObserverTests`, `…CompositionGuardTests`) | **18 passed**. |
| `dotnet test Tooba.Shipping.Tests` | **82 passed**, 0 failed. |
| `dotnet build Tooba.Host.Tests -o %TEMP%` (redirected, Host bin locked) | Build succeeded, 0 errors. |
| Host architecture guard tests | **Not run** — the running Host locks `Tooba.Host/bin`; the redirected output path breaks the guards' repo-root walk (1383 path-relative failures, not real). Requires a Host restart, which this task forbids. |

No database was read or written, no migration/registry/seed/appsettings/Host file was touched, no process was
started, stopped or restarted.
