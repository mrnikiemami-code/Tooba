# Deliverable B — genuine customer quote provenance (TB-158)

## What was read

Real HTTP attempt, task-owned Host `http://127.0.0.1:5418`, Development seam actor header
`X-Tooba-Dev-Actor-User-Id`, scratch cart id `9e0f5b1a-1111-4a2b-8c3d-000000000158`:

```text
POST /v1/storefront/shipping/projection
-> HTTP 401 {"errorCode":"checkout.authentication_required","instance":"/v1/storefront/shipping/projection"}
```

The storefront checkout actor is resolved by `HostOrderStorefrontActor.ResolvePlacementActor` /
`HostOrderStorefrontCheckoutIdentityGate` (`CheckoutIdentityGate.EnsureCheckoutActorAsync`); the
Development header seam only covers the *placement/list* actor, not the checkout identity gate. A
genuine authenticated customer quote therefore requires a real customer session (a genuine auth
flow), which is outside the 45-minute bounded acceptance and is not fabricated here.

Response body saved verbatim: `storefront-shipping-projection-158.json`.

## Code-level call chain actually used by the customer shipping price

```text
POST /v1/storefront/shipping/projection
  -> Tooba.Order.Application/Storefront/Shipping/Queries/ProjectStorefrontShipping/ProjectStorefrontShippingHandler
     -> StorefrontShippingService.ProjectAsync                       (Application/Storefront/Services/StorefrontShippingService.cs:64)
        -> LoadEligibleMethodsAsync(_shippingCatalog.ListAsync())     (:644)   legacy provider/service/option catalog
           -> TryAddMethod                                            (:723)
              -> StorefrontShippingCalculator.ResolveRate(methodCode, ShippingMethodsOptions)
              -> StorefrontShippingCalculator.QuotePrice(rate, cartSubtotal)  (:740)  => StorefrontShippingMethodView.PriceAmount
        -> projection.SelectedShippingAmount = selected?.PriceAmount ?? 0m  (:145)
```

* The customer price authority is `StorefrontShippingCalculator` over the Host configuration section
  `Tooba:ShippingMethods` (`ShippingMethodsOptions`), fed by the legacy catalog reader.
* The one-level integration in the storefront is **label-only**: `ApplyOneLevelDisplayNamesAsync`
  changes only `Label`; eligibility and price intentionally stay on the legacy source
  (`StorefrontShippingService.cs:683-693`).
* `Shipping` owner rate rows (`rate_policies`) are **never read** on this path. Their only storefront
  consumer is the observe-only shadow:
  * `StorefrontShippingRateAuthorityOptions` (`ShadowEnabled=false`, `PrerequisitesConfirmed=false`
    by default; section `Tooba:ShippingRateAuthority`) — the isolated overlay deliberately leaves it
    unset and the isolation guard asserts `shippingRateAuthoritySet=false`.
  * `StorefrontShippingRateAuthorityPolicy` — `Disabled` unless both flags are explicitly true.
  * `StorefrontShippingShadowObserver` / `StorefrontShippingRateShadowComparer` — comparison/telemetry
    only; the returned projection is untouched.
* Customer payment quote (`CartPaymentQuoteBridge` -> `StorefrontShippingService`) has no additional
  shipping money seam beyond the same `ShippingMethodsOptions` pricing.

## Conclusion

The Admin-owned `tipax:standard` amount (185,000 then 205,000 IRR, both verified server-side and in the
browser) does **not** propagate to the storefront customer quote. The quote is priced from
`Tooba:ShippingMethods` via `StorefrontShippingCalculator`.

## Smallest necessary seam (NOT implemented — out of bounded task scope)

1. Bind the existing, already-defined `Tooba:ShippingRateAuthority` options in DI (no new options type).
2. Add the consumer side that resolves the *selected* method's owned rate rows through the Shipping
   Contracts read surface instead of `ShippingMethodsOptions` when — and only when — the gate reports
   `Allowed` (`ShadowEnabled && PrerequisitesConfirmed`).
3. Keep the fail-closed mapping: no owned rule -> `NeedsConfiguration` -> no price (never 0/FREE);
   legacy service-level rows -> `LegacySharedUnresolved` -> never this method's price.

That change alters live customer pricing and is therefore not self-authorized here.
