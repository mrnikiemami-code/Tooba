# TB-SHIPPING-NEW-METHOD-ACTIVATION-170 — evidence

Verdict: **PARTIAL**
Base SHA: `1671349312ee0ce75759bbbabda324b7fc91bb95` (HEAD == origin/main at start and after push)

## 1. Owner map (unchanged architecture, no new module)

| Concern | Owner | Truth |
| --- | --- | --- |
| Method identity, name, activation projection | Fulfillment (`shipping_methods`, `shipping_method_translations`) | `method_code` + immutable `legacy_service_code`/`legacy_option_code`, derived server-side from the canonical code |
| Legacy service/option rows | Fulfillment (`shipping_services`, `shipping_service_options`) | Only pre-existing/backfilled methods have them; a new canonical method must not need one |
| Rate policy, coverage grant, method restriction, quote | Shipping (`shipping.*`) | Authoritative amount, currency, preparation/lead days; strict-owned-option matching |
| Customer eligibility projection | Order (`StorefrontShippingService`) | Consumes Shipping quote + Fulfillment Contracts identity only; never reads another module's tables |

TB-169's concrete gap was that a new canonical method could be named but activation was refused with
`fulfillment.shipping_method.unsupported` because no `shipping_service_options` row existed, and no
consumer could ever see it.

## 2. Implemented changes

1. **Fulfillment activation (`ShippingOneLevelCatalogActivation`)** — a method whose parent service exists
   and is active but whose mapped option row does not exist is now activated on the **method row only**
   and reports `ActivatedCanonicalOnly`. No `shipping_service_options` row is created. Parent-service
   missing/inactive still fails closed (`LegacyBindingMissing` / `LegacyParentInactive`) because provider
   registration is a real prerequisite. Legacy-bound methods keep the exact previous two-row alignment.
2. **Activation DTO** — additive `canonicalOnly` flag so Admin can see that activation is an admin
   projection, not customer readiness.
3. **New Contracts seam `IShippingOneLevelCanonicalMethodReader`** (Fulfillment.Contracts) +
   `ShippingOneLevelCanonicalMethodRead` (Fulfillment.Infrastructure, gate-aware, fail-closed):
   - `ListActiveUnboundAsync` — active canonical methods with an active parent service and **no** legacy
     option row.
   - `IsCanonicalMethodBindingAsync` — does a real canonical method bind exactly this service+option?
4. **Shipping rate key (`FulfillmentShippingMethodOptionValidator`)** — the new method identity is accepted
   as an owned rate key through the Contracts seam when the legacy option list has no such option.
   Invented codes are still refused with `shipping.service_option.unknown`; gate off ⇒ no allowance.
5. **Order storefront eligibility (`StorefrontShippingService`)** — canonical-only methods are added as
   owned-rate candidates **only when `Tooba:ShippingOwnedRateAuthority` is ON** (zero calls when off), and
   are exposed to the customer only when this same round produced a real Shipping quote. Price and lead
   days come from the quote; no fallback/free/zero price, and rejected canonical drafts are not announced
   in the legacy `unavailable` list.

## 3. Verification

- `dotnet build` — Fulfillment.Tests, Shipping.Tests, Order.Application: **0 errors**.
- Focused tests:
  - `Tooba.Fulfillment.Tests` `ShippingOneLevel*`: **109 passed / 0 failed**
    (includes new canonical-only activation, idempotency, deactivation, unbound identity reader).
  - `Tooba.Shipping.Tests` `ShippingCanonicalMethodRateKey`: **3 passed / 0 failed**.
  - `Tooba.Order.Tests` `OwnedRate`: **10 passed / 0 failed**.
- Isolated HTTP acceptance (`acceptance-170.js`, private Host `127.0.0.1:5424`, scratch PG `127.0.0.1:55489`):
  **22/22 PASS** (`assertion-matrix-http-170.json`, `http-trace-170.txt`), including:
  - create → binding `tipax`/`scratch170` → **activation applied** (`isActive=true`, `changed=true`, `canonicalOnly=true`);
  - readback persisted; **no fake legacy option row** (`before=0 after=0`);
  - idempotent re-activation (`changed=false`);
  - **owner rate key accepted** for `tipax`/`scratch170` (previously impossible);
  - **owner coverage grant accepted**;
  - invented option key still refused;
  - legacy sibling activate/deactivate parity unchanged.
- DB readback (`db-readback-170.txt`): method row active with `legacy_option_rows=0`; real Shipping rate
  `185000 IRR`, `lead_days=3`; option-scoped coverage grant present.
- Teardown (`teardown-170.txt`): host stopped, container removed, port 5424 free, shared ports untouched.

## 4. Honest gaps (why PARTIAL, not PASS)

1. **Customer storefront projection was not proven end-to-end over HTTP.** The Order-side integration is
   implemented and unit-covered at the owner boundaries, but exercising it requires a real cart with
   seller/offer/stock/checkout-priced lines. The isolated scratch carries only shipping/language/geography
   fixtures; building a catalog+offer+inventory+cart fixture is cross-module work that does not fit the
   45-minute boundary, and the task forbids broadening scope to force it.
2. **The admin operational-quote endpoint is not the strict customer gate.** It evaluates legacy
   service-level coverage semantics, so the option-scoped grant is intentionally not borrowed there; the
   run asserts the truthful result (`quoted=false`, `amount=0`, stable `shipping.coverage.missing`).
   Strict-owned-option quoting is covered by the owner test suite, not by that endpoint.
3. Admin browser UI (three row actions, readiness panel) was not re-verified this run; no UI file changed.

## 5. Exact smallest next step

Provide an isolated scratch cart fixture (one seller + one active offer + stock + checkout-priced line),
then call `GET /v1/storefront/checkout/shipping` (or the projection route) with the scratch method and the
IR/THR destination with `Tooba:ShippingOwnedRateAuthority=true`, and assert (a) the new method appears with
`priceAmount=185000`, `leadDays=3`, and (b) it is absent with the gate off.
