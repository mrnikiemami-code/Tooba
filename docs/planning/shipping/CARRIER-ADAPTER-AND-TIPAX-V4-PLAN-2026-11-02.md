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
| R1 — carrier account | Owner-scoped carrier profile, secret **reference** (never secret value), connection test, service/payment/packing reference sync | Checkout remains on existing manual/free behavior |
| R2 — method and geography mapping | Method selects provider + provider service; Geography city gets provider external code; origin address/coordinates become authoritative | No guessed city/service identifiers |
| R3 — checkout quote | Captured preview calls the neutral rate port, persists exact provider quote evidence/expiry, revalidates before commit, and surfaces typed unavailable/unauthorized/no-service errors | No silent manual fallback and no zero-price fallback |
| R4 — shipment lifecycle | Provider-neutral create/track/cancel ports plus Tipax order/tracking/cancel adapter and Order-owned provider receipt | No shipment call before committed Order; idempotency required |
| R5 — operational acceptance | Tipax test account: connection, reference sync, priced cart, stale quote refusal, committed manual-payment Order, shipment create/replay, track/cancel | Production remains disabled until explicit merchant activation |

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
