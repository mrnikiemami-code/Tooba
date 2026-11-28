# TB-296-PRICING-FINAL-UAT-CLOSURE — RESULT

Status: **READY_FOR_ARCHITECT** (implementation + real connected verification). Worker PASS is not architect ACCEPT; owner UAT remains separate.
Base branch/head: `opencode/iso-agents-002` / `1d777e2e0590581c5ab256901d9d7ce06a325f3a` (fetched, inspected, fast-forwarded).
No new Host or database. Existing DEV Host 5088 / Next 3000 / PostgreSQL `tooba_alpha` reused.

## Delivered (MC-01..MC-06)
- **Pricing-owned authoritative currency reference**: trusted `Pricing:Currencies` commercial config (`appsettings.json` + `Development`) with IRR(0), USD, EUR, AED, GBP, TRY, JPY(0), CNY, KRW(0). `SystemCurrencyReference` validates codes/duplicates/precision against the kernel `AuthoredCurrency` scale and fails closed; empty config keeps single-currency behavior.
- **Multi-currency authored prices**: `SetSellerOfferPrice` now accepts any active reference currency for the trusted store market (client no longer supplies market); per-currency advisory-lock CAS, durable facts and outbox unchanged. `ListBasePricesAsync` returns one current base row per currency with independent revisions.
- **Offer read model/endpoints**: detail exposes `Prices`; `GET /v1/admin/offers/currencies` and `GET /v1/seller/offers/currencies` serve the reference through Offer.Application (no foreign DbContext).
- **Shared workspace**: pricing tab lists all authored base prices and offers a currency selector + amount editor; a currency with no row is authored with the `absent` CAS sentinel; per-currency revision/dirty/save-verification; IRR integer input/no decimals; no market column.
- **Registry locale/RTL**: new labels in fa/en/ru; shared registry direction preserved.
- **UAT corrections preserved** (1d777e2e): no market column, no duplicated variant labels, no excess copy, contained header borders, authorized edit-mode inventory position provisioning.
- **OpenSpec**: `openspec/changes/offer-multicurrency-pricing-296-r11` (proposal/design/tasks/spec) validated strict.
- **Migration**: not required — no schema/DDL change; existing `pricing.prices` key already supports (offer, market, channel, currency, qualifier). Startup migration ran unchanged on `tooba_alpha`.

## Verification (actual)
- Backend: Offer.Tests **144/144**, Pricing.Tests **18/18**, Typecheck **0**.
- Frontend: model **15/15**, mounted Offer workspace **29/29**, i18n **20/20**, `test:architecture` baseline rules only: **27 pre-existing violations, 0 Offer-owned**.
- OpenSpec strict: exit 0 ("Change is valid").
- Impeccable: context loaded (existing visual system, scoped extension); manual detector `detect --json --changed` returned `[]` (no findings). No screenshots.
- **Connected PASS** (`evidence/connected-verification.json`), real Host/tooba_alpha/Playwright:
  - admin and seller `/offers/currencies` return the authoritative list (IRR 0 decimals, USD 2, EUR present);
  - dedicated offer recorded independent IRR=100000 and USD=12.34→13.50 prices, stale USD revision denied (4xx), IRR fractional 100000.75 normalized to integer;
  - SQL: exactly 3 active base rows (IRR integer, USD 13.5, EUR 20.5) after adding EUR from the UI;
  - Playwright `/fa/admin/offers/<id>`: RTL/fa, pricing tab, no market header, IRR/USD rows, EUR selectable; selected EUR (no authored row), saved 20.5, reload persisted; `/en/` LTR/en;
  - edit-mode inventory position provisioned through the existing owner endpoint;
  - exact fixture cleanup: 0 offers/prices/facts/positions remaining.
- Regression: existing Offer backend suite, mounted workspace regression, i18n, seller/admin API typecheck all green; no prior offer behavior changed.

## Architecture (AQ)
- AQ-01 registry locale/RTL: messages fa/en/ru + shared registry; connected fa RTL/en LTR.
- AQ-02/AQ-04/AQ-08: Pricing owns currency/price; Offer reads via `IOfferPriceHistoryReader`/`ISellerOfferPricingGateway` contracts only; endpoints use `ISender`; `GetOfferCurrenciesQuery` added to the endpoint-request guard manifest; host config only.
- AQ-05 shared components reused (no new grid/notification/locale system).
- AQ-07: touched `offer-workspace.tsx` kept below the 800-line guard by extracting `useOfferCurrencies`; no new Offer-owned size violation.
- Pre-existing 27 unrelated frontend size violations remain baseline debt (not remediated, not waived).

## Evidence
- `evidence/connected-verification.json` (raw HTTP requests/bodies, SQL rows, browser facts, cleanup counts).
- `evidence/evidence-identity.json` (source/base, served DLL SHA-256, Next BUILD_ID, DB, migration decision).
- `evidence/offer-tests.log`, `pricing-tests.log`, `typecheck.log`, `frontend-model-tests.log`, `mount-tests.log`, `i18n-tests.log`, `architecture.log`, `openspec-strict.log`, `impeccable-detect.json`, `build-identity.log`.
- `evidence/SHA256SUMS.txt`.

## Limits
No discount engine. No unrelated domain changes. No screenshots. The authoritative currency list is commercial configuration and must be maintained there. Pre-existing architecture baseline debt is unchanged.
