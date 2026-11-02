# Carrier Adapter and Tipax V4 delivery plan — 2026-11-02

## Decision

Shipping talks only to provider-neutral carrier ports. `tipax` is the first plug-in. A future Post,
Chapaar or private fleet adapter is registered under another provider code and does not add a switch to
Checkout, Order, Shipping Domain or the storefront.

## Delivered foundation (247)

- `ICarrierRateProvider` and `ICarrierRateProviderRegistry` in Shipping Application.
- Neutral request/result contracts with explicit grams, centimetres, declared value, payment, pickup and
  delivery semantics.
- Tipax V4 Pricing adapter against the official `POST /api/OM/v4/Pricing` OpenAPI shape.
- Password-grant token acquisition, cached access token, official V4 refresh route and one bounded 401
  re-authentication retry for the read-only pricing call.
- Exact normalization of Tipax rate variants into carrier-neutral options and monetary components.
- Fail-closed configuration: disabled/incomplete config performs zero network calls; no checked-in secret,
  database credential column, automatic fallback or checkout activation.
- Module DI registration through `IEnumerable<ICarrierRateProvider>` so adding a carrier is additive.

## Remaining activation slices

| Slice | Concrete result | Must stay closed until complete |
|---|---|---|
| R1 — carrier account | **Software delivered (248 / 248-R1).** Owner-scoped carrier profile, secret **reference** (never secret value), connection test, service/payment/packing reference sync | Live Tipax connection still deferred |
| R2 — method and geography mapping | **Software delivered (248 / 248-R1).** Method selects provider + provider service; Geography city gets provider external code | No guessed city/service identifiers |
| R3 — checkout quote | **Software delivered (248-R1).** Captured preview calls the neutral rate port, persists exact provider quote evidence/expiry, revalidates before commit, typed refusals | No silent manual fallback and no zero-price fallback |
| R4 — shipment lifecycle | **Software delivered (248-R1 + 249 + 249-R1 + 249-R2).** Provider-neutral create/track/cancel; Tipax OmOrder/Tracking/Cancel; label unsupported; durable operation_key; provenance + capabilities; unified readiness; package/sender completions; **249-R2** completion→create unlock, shared mutation gates, execution snapshot, fulfillment eligibility | Live Tipax deferred; migrations `20261102190000` + `20261102210000` + `20261102220000` applied on canonical; disposable PG full §4 matrix still unmet (249-R2 PARTIAL) |
| R5 — operational acceptance | Tipax test account: connection, reference sync, priced cart, stale quote refusal, committed manual-payment Order, shipment create/replay, track/cancel | **Deferred by user — no test account now.** Production remains disabled until explicit merchant activation |

## Required merchant inputs for activation

- Tipax test/production username and password through the platform secret store.
- The provider numeric IDs selected from Tipax reference APIs: service, payment, pickup, distribution,
  packing and package content.
- An authoritative origin address and Tipax city/address mapping.
- Product/package weight and dimensions already required by the neutral contract.

## Official contract sources

- <https://document.tipaxapi.org/#v4-docs>
- <https://omtestapi.tipax.ir/swagger/v4/swagger.json>

The adapter deliberately does not copy all 76 Tipax endpoints. Only the endpoints required by the
rate and shipment capabilities are added, one capability at a time.
