# TB-SHIPPING-STOREFRONT-RATE-AUTHORITY-159 — owner-authority blocker (precise blocked shape)

Baseline: `bfb9245bb6fce0f8a4b65b40c68090879413e667` (HEAD == origin/main == Expected-Base-SHA).
Production diff shipped by this task: **NONE** (see `no-production-diff-159.txt`).

## Confirmed call chain (unchanged, re-verified this run)

```text
POST /v1/storefront/shipping/projection
  -> ProjectStorefrontShippingHandler
     -> StorefrontShippingService.ProjectAsync
        -> LoadEligibleMethodsAsync
           -> StorefrontShippingCalculator.ResolveRate(methodCode, ShippingMethodsOptions)  <-- legacy price source
           -> StorefrontShippingCalculator.QuotePrice(rate, cartSubtotal)                   <-- legacy amount
        -> StorefrontShippingService.SaveShippingAsync (draft/commit path)
           -> LoadEligibleMethodsAsync (same list, same m.PriceAmount)
           -> CartShippingGroupPlanner.Plan(... eligible[m.PriceAmount], soleCurrency ...)
```

* Customer price authority today is `Tooba:ShippingMethods` via `StorefrontShippingCalculator`
  (`StorefrontShippingService.cs:733-740`).
* `Shipping`-owned `rate_policies` are never read on that path; their only storefront consumer is the
  observe-only `StorefrontShippingShadowObserver` (default off).
* The only owner read seam that exists is `Tooba.Shipping.Contracts.Rates.IShippingRateQuoteLookup`
  (implemented by `ShippingRateQuoteLookup`, registered scoped in `ShippingModule.cs:57`, resolved into
  Order DI at `OrderModule.cs:131` as a shadow-only dependency).

## Why the seam could NOT be shipped safely (semantic authority gap)

The intended smallest owner-correct wiring was: when an explicit, default-off production gate is open,
re-price each eligible method from `IShippingRateQuoteLookup` using the method's own canonical identity
`(ServiceCode, OptionCode)` and drop any method that is not `Quoted` (fail-closed).

That wiring is **not** owner-correct with the current owner contract, because the owner quote engine
treats a provider/service-level row as a valid fallback for an option-specific method:

```startLine:147:endLine:149:src/backend/Modules/Shipping/Tooba.Shipping.Domain/Aggregates/ShippingRatePolicy.cs
    public bool MatchesOption(string? optionCode)
        => OptionCode is null
           || string.Equals(OptionCode, optionCode?.Trim(), StringComparison.OrdinalIgnoreCase);
```

```startLine:86:endLine:90:src/backend/Modules/Shipping/Tooba.Shipping.Domain/Rates/ShippingRatePolicyEngine.cs
        // Option identity is resolved inside the winning tier only: the request's own option-specific
        // tier when one exists, otherwise the documented service-level fallback tier. A service-level
        // policy therefore keeps working unchanged for single-option and no-option services.
        var candidates = covering.Any(x => x.IsOptionSpecific)
            ? covering.Where(x => x.IsOptionSpecific).ToArray()
            : covering;
```

Consequence: a single provider-level row `ServiceCode = "tipax", OptionCode = null` would price **both**
`tipax:standard` and `tipax:express` through the same lookup. That is exactly provider-level tariff
inheritance across sibling methods, which is explicitly forbidden here and already recorded by the owner's
own Admin read model as an unattributable row:

* `SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md` §C — "Hard method isolation — NO sibling or provider policy
  inheritance … Old provider/service-level policies that cannot be unequivocally attributed to one
  canonical method MUST be marked `LEGACY_SHARED_UNRESOLVED`".
* This task — "no provider-level tariff inherit", "no fallback to legacy configuration or
  sibling/service-level tariffs in that mode".
* TB-158 proved the same case as `LegacySharedUnresolved` (`financial-status-standard-after-legacy-158.json`).

```startLine:64:endLine:82:src/backend/Modules/Shipping/Tooba.Shipping.Application/Rates/ReadModels/ShippingMethodFinancialStatusEvaluator.cs
        // The optionless method's own rules ARE the service-level rows; they are never "shared legacy"
        // for that method, because no other option identity exists to leak from.
        if (owned.Length == 0)
        {
            if (option is not null && serviceLevel.Length > 0)
            {
                return new ShippingMethodFinancialStatusView(
                    service,
                    option,
                    ShippingMethodFinancialState.LegacySharedUnresolved,
                    ...
                    ShippingMethodFinancialReasonCodes.LegacyServiceLevelShared);
```

So the owner module already states the correct rule (a service-level row proves nothing for an
option-specific method) while its *quote* engine still honours the opposite fallback. A production
customer-pricing seam built on the quote engine as-is would ship a forbidden price the first time a
provider-level row exists — therefore it was **not shipped**. The Order side may not re-implement the
owner rule to compensate: the contract is explicit that the seam "deliberately does not return the policy;
only the final quote result — so that the consumer (Order) cannot re-implement business rules"
(`ShippingRatePolicyContracts.cs:344-350`).

## Two further facts that make the "rate row alone" claim insufficient

1. **A stored rate row alone does not price a customer.** The owner engine requires an active coverage
   grant as well; the TB-158 Admin-created `tipax:standard` row has
   `activeCoverageGrantCount: 0` (`financial-status-standard-after-ui-create-158.json`), so the engine
   returns `NotCovered` for it. A "FinanciallyConfigured" Admin badge is therefore provably not a
   customer quote — exactly as this task warned.
2. **The storefront has no trusted parcel measurement.** `Parcel` is intentionally not sent
   (`ProjectStorefrontShippingQuery.cs:63-71`), so any conditioned (weight/count) owned policy fails
   closed with `MeasurementRequired`. Correct, but it must be handled explicitly rather than discovered.

## Required owner-side change (separate scoped task; NOT self-authorized here)

1. Additive change in `Tooba.Shipping.Contracts.Rates` so a caller can demand **strict option identity**
   (a service-level row must never satisfy an option-specific request), implemented in the owner engine
   `ShippingRatePolicyEngine` — the semantic owner of the rule, not Order.
2. Keep the documented no-option/single-option behaviour byte-compatible when strict identity is not
   requested, and cover it with owner tests (`Tooba.Shipping.Tests`).
3. Then, and only then, wire the Order seam: explicit default-off production gate (independent from the
   observe-only `Tooba:ShippingRateAuthority`), re-price from the strict lookup, and drop any non-`Quoted`
   method (fail-closed, no legacy/sibling/service-level fallback, no zero→FREE).
4. Decide and document coverage-grant creation for customer-purchasable methods, and the parcel
   measurement contract (or explicit `MeasurementRequired` UX).
5. Only after that: authenticated-customer runtime proof (TB-157/TB-158 harness) for a two-seller cart.
