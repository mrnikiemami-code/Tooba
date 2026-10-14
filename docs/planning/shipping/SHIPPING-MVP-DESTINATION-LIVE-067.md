# TB-SHIPPING-MVP-DESTINATION-LIVE-067 — Geography-backed customer destination + live cart proof

**Program:** TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
**Mode:** BOUNDED_GEOGRAPHY_BACKED_STOREFRONT_DESTINATION_AND_LIVE_CART_PROOF
**Parent:** TB-SHIPPING-MVP-DESTINATION-066
**Expected-Base-SHA:** `001fa35f7599b4b8dd7d489b9cdc62836ab18ed4`
**Verdict:** PASS

---

## 1. What was actually built

### A. A real customer-readable Geography identity source (new, smallest possible)

`/v1/admin/geography/**` is guarded by the existing `fulfillment.view` permission, so it can never be
reused by a customer. Instead of leaking that capability, a new **Geography-owned, public, read-only**
surface was added:

| Route | Purpose |
| --- | --- |
| `GET /v1/storefront/geography/places/countries` | stable country ids + codes + labels |
| `GET /v1/storefront/geography/places/countries/{countryId}/administrative-areas` | IR → province ids |
| `GET /v1/storefront/geography/places/countries/{countryId}/cities?administrativeAreaId=` | province → city ids, **parent-scoped** |

`src/backend/Modules/Geography/Tooba.Geography.Endpoints/Storefront/GeographyStorefrontEndpoints.cs`
consumes only `IGeographyLookup` + `IGeographyLabelLookup` (Contracts), has **no** authorizer, **no**
write verb, **no** `DbContext`/`IQueryable`, and deliberately does **not** expose service areas — the
storefront shipping screen does not need them. It is registered by one line in `Program.cs`
(`app.MapGeographyStorefrontEndpoints();`). The admin file
(`GeographyAdminEndpoints.cs`) was **not** modified, so its four-route/authorized-only invariant holds.

### B. The existing customer shipping screen now carries stable ids

New `src/frontend/app/storefront/storefront-geography-api.ts` reads that public surface.
New pure model `src/frontend/app/storefront/storefront-shipping-destination-model.ts` maps a customer
selection to `DestinationIdentifiers` with three hard rules:

* matching is **exact** on the stable `code` (country, province) or the display label (city inside the
  already-selected province) — never fuzzy, never substring;
* **ambiguity collapses to `null`**, never to a pick;
* the hierarchy must be consistent (city's parent must be the selected area, area's parent must be the
  selected country), otherwise `null`.

`storefront-shipping.tsx`:

* loads countries once and resolves **IR by exact code** to a country id;
* resolves the selected legacy province to its Geography area **by exact code** (`alborz` → `alborz`);
* loads the cities of **that** area and uses them as the city list, so the select can no longer offer
  cities from another province;
* keeps `provinceName` = the **legacy catalogue label**, so the legacy quote/eligibility input is
  untouched, and adds `cityName` + `destination` alongside it.

### C. Header/hierarchy result (live, unauthenticated, real Development data)

```
GET /v1/storefront/geography/places/countries                  => 200
[{"id":"e3451206-…","code":"IR","label":"ایران","isActive":true}]

GET …/countries/<IR>/administrative-areas                      => 200   (12 provinces)
alborz => id=1f8050a8-… label=البرز parentId=e3451206-…  (parent == country id: True)

GET …/countries/<IR>/cities?administrativeAreaId=<alborz>      => 200   (4 cities)
کرج   => id=68255340-… code=IR-alborz-01 parentId=1f8050a8-…  (parent == alborz id: True)

country-wide cities (unscoped) = 47  → the scoped call is a strict subset, proving the parent filter
```

## 2. Live authenticated cart proof (the acceptance item that matters)

Provisioned through the **canonical development customer flow only** — no bypass:
`POST /v1/auth/register` → `POST /v1/auth/login` → `POST /v1/storefront/cart` →
`POST /v1/storefront/cart/{id}/lines` with a real catalogue offer
(`0442a1fa-1d95-4b24-b9d9-129fc19942d2`, «نایک شلوار مردانه سری 1»). No order was placed and no payment
was made.

| Call | Result |
| --- | --- |
| identity (bearer session) | accepted — the projection no longer returns 401 |
| cart | `version=1 → 3`, `itemCount=1`, real line added |
| **projection BEFORE** (`provinceName` only — the old-client shape) | 5 methods: `post:standard:120000`, `tipax:express:260000`, `tipax:standard:180000`, `store_courier:75000`, `in_person:0`; subtotal `8702542` |
| **projection AFTER** (`provinceName` + `cityName` + validated `destination` ids) | **same 5 methods, same prices, same subtotal** |
| payload comparison | **`identical projection payload: True`** |
| projection with a **malformed** city id | still the same 5 methods — no error, no trusted identity (observation-only, gate off) |

Legacy quote invariance is therefore demonstrated **live**, not only in unit tests.

## 3. Legacy safety / flags

* `StorefrontShippingService.ProjectAsync` and the whole legacy price/method/preparation/promise/draft
  path were **not modified** in this task.
* `ShadowEnabled` / `PrerequisitesConfirmed` remain **false**; no appsettings change, so an explicit
  `destination` is inert metadata.
* The frontend keeps sending the legacy province label, so `AllowedProvinces` matching and method
  eligibility are unchanged.
* Old clients that send neither `cityName` nor `destination` still receive the identical payload
  (proven both by test and by the live BEFORE call).
* If the public Geography read fails, the screen falls back to the legacy static city catalogue.

## 4. Focused validation

| Check | Result |
| --- | --- |
| `dotnet build Tooba.Geography.Endpoints` | 0 warnings / 0 errors |
| `dotnet build Tooba.Host` | 0 warnings / 0 errors |
| `Tooba.Shipping.Tests` geography ownership guards (4 new) | 8 passed / 0 failed |
| `Tooba.Order.Tests` storefront shadow observer (066 regression) | 19 passed / 0 failed |
| new destination model + shipping API + 066 admin picker race | 16 passed / 0 failed |
| `npx tsc --noEmit` | clean |
| import-boundary / flat-folder / feature-boundary / test-discovery guards | pass |
| `frontend-source-size.guard.test.ts` | pre-existing failure in 4 untouched files; baseline not widened |

New tests cover exactly the required cases: one valid city path end-to-end to ids; old client with no
ids; malformed/foreign hierarchy rejected (never trusted); ambiguous or fuzzy matches collapsed; the
065/066 admin picker stale-response guard still green.

## 5. Process, provenance and safety

* **Restarted process**: one only, the positively identified Development host
  (`Tooba.Host.exe`, PID **38716**, started `2026-10-15 02:27:45`, listening `127.0.0.1:5088` and
  `[::1]:5088`, the **only** `Tooba.Host` process on the machine). Reason: it hosted the pre-066/067
  binary and locked the Host output DLLs (the build failed with `MSB3027 … locked by Tooba.Host (38716)`).
  Graceful stop observed `True`, remaining `Tooba.Host` processes `0`. Relaunched with the recorded 065
  command (`ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_URLS=http://localhost:5088`,
  `dotnet run --project src\backend\Host\Tooba.Host --no-launch-profile --no-build`);
  new PID **11540**, `GET /health` → `200 {"status":"ok"}`. No other process was killed. The frontend dev
  server was not restarted.
* **Database**: only the authorized Development `tooba_alpha`. Writes are **additive task-owned
  artifacts** (disposable `067.probe.*`/`067.cleanup.*@tooba.local` customers and their guest carts).
  No pre-existing row was modified; no order/payment; no Shipping rate/coverage row; no migration,
  schema, seed or appsettings change. See `log-test-artifacts-and-cleanup.txt`.
* No scratch/parallel database, no reset, no stash, no rebase, no force push. Unrelated dirty files
  preserved.

## 6. Changed paths

| Path | Change |
| --- | --- |
| `src/backend/Modules/Geography/Tooba.Geography.Endpoints/Storefront/GeographyStorefrontEndpoints.cs` | **new** public read-only customer Geography routes |
| `src/backend/Host/Tooba.Host/Program.cs` | one registration line + one using |
| `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/GeographyStorefrontReadOwnershipTests.cs` | **new** ownership/security guard tests |
| `src/frontend/app/storefront/storefront-geography-api.ts` | **new** customer Geography read |
| `src/frontend/app/storefront/storefront-shipping-destination-model.ts` | **new** pure exact-match/hierarchy model |
| `src/frontend/app/storefront/storefront-shipping-destination-model.test.ts` | **new** focused tests |
| `src/frontend/app/storefront/storefront-shipping.tsx` | Geography-backed area/city selection + ids in the existing projection call |
| `src/frontend/app/storefront/storefront-shipping-api.ts` | projection body accepts the optional `destination` |
| `docs/planning/shipping/SHIPPING-MVP-DESTINATION-LIVE-067.md`, `docs/architecture/evidence/TB-SHIPPING-MVP-DESTINATION-LIVE-067/**` | this document + evidence |

## 7. Remaining limitations (honest)

* The `store_courier:75000` / `in_person:0` methods already existed in the legacy catalogue; this task
  neither added nor removed any commercial tariff.
* The projection in the proof shows `selectedMethodCode` empty because the request intentionally passed
  `methodCode: null` (no method chosen yet). That is the unchanged legacy behaviour, not a regression.
* `DestinationIdentifiers` remains observation-only: with the gate off it is inert. No shadow parity and
  no checkout cutover is claimed, and flipping those flags was explicitly out of scope.
* City matching inside the selected province uses the display label because the Geography city `code`
  values are synthetic (`IR-alborz-01`), not legacy-readable names. Labels are unique inside a scoped
  province, and ambiguity still collapses to `null`.
* The disposable proof customers/carts were left in the Development database: the platform exposes no
  customer-delete surface, and a direct database delete would exceed this task's write authority.
* **Elapsed: the wall clock exceeded the 45-minute budget** — the Host rebuild alone burned ~8.5 minutes
  in `MSB3027` copy retries against the locked running host before the authorized restart. Work was
  stopped immediately after the live proof, validation capture, and evidence were complete.
