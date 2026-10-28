# RESULT — TB-SHIPPING-MARKETPLACE-FIRST-LIVE-ACCEPTANCE-239

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-MARKETPLACE-FIRST-LIVE-ACCEPTANCE-239
Parent-Task: TB-SHIPPING-LIVE-COUPON-MARKETPLACE-COMPLETION-238
Channel: tooba-main
Worker-Id: tooba-worker-01
Bridge-Task-Row: 0326ae9f-5fa8-45ab-b33f-79e743cb7fcb
Status: PARTIAL
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 4567c1ad598f4215e61941cf74b7a45d02e1a101
Base-SHA: 4567c1ad598f4215e61941cf74b7a45d02e1a101
Code-SHA: 4567c1ad598f4215e61941cf74b7a45d02e1a101
Evidence-SHA: (filled by the evidence commit)

## 1. Milestone 1 — a REAL Marketplace target now exists and answers real API reads

| item | actual value |
|---|---|
| Edition | `Marketplace` (`Tooba__Edition`), DeploymentId `tb239-marketplace` |
| Host | `http://127.0.0.1:5190` (PID 40544), `/health` **200 `{"status":"ok"}`** |
| Binary | `D:\temp\tb238-build\out\Tooba.Host.dll`, SHA256 prefix `2A2485DC2107` (built from `600a96f1a`, no rebuild) |
| DB | NEW `tooba_marketplace_tb239` on `127.0.0.1:5432` (docker `postgres-db`), created empty, no existing DB touched |
| Tenant | `NULL` (Marketplace resolves the marketplace connection, no tenant) |
| Gates local ON | `StorefrontCapturedPreview` Enabled/SelectionEnabled/CheckoutEnabled + `AvailableDateCount=7`, `ShippingOneLevelCatalog.Enabled=true`; checked-in gates stay OFF |

Real reads (normal HTTP, no interception):
- Before migrations: `GET /v1/storefront/home` **500 `platform.unexpected`** — `42P01 catalog.categories`.
- The Development Marketplace bootstrap migrates only 12 schemas (party, identity, order, payment, settlement,
  notification, access_control, content, media, page_composition, story) and seeds the two dev sellers; it does
  NOT create catalog/offer/inventory/promotion/shipping/fulfillment/geography/localization/tax/pricing/cart/
  address_book.
- Repair through the OFFICIAL runner (no hand DDL): `Tooba.MigrationRunner plan` then `apply` against the
  marketplace connection → **all module migrations succeeded**, 33 schemas present.
- After: `GET /v1/storefront/home?language=fa` **200**, 1803 bytes, real empty Catalog payload
  (`categories:[]`, hero from live config). This is a real Marketplace-edition API read.

Real seller identities on this DB (normal login endpoint, seeded dev passwords, no invented auth):
`POST /v1/auth/login` as `seller-actor-a@tooba.local` **200** (userId `01a18178-67bc-7000-9662-fb5cd519fd58`)
and `seller-actor-b@tooba.local` (userId `01a18178-6820-7000-a04e-01f81bcca3a4`).

Real permission boundary (not weakened): the same seller access token calling the official Development catalog
seed `POST /v1/admin/catalog/demo/reset-and-seed` returns **403 `admin.authorization.denied`**
(trace `4d6dff0edd21c01a9f87b48d766718bc`). No role/header injection was attempted.

## 2. BLOCKER that stopped the owner/order path (exact, on an actual target)

The fresh Marketplace DB contains `identity.users=2` (the two dev sellers), `party.parties=0`,
`catalog.products=0`, `offer.offers=0`. Marketplace Development bootstrap seeds auth tuples for a platform admin
actor (`cccccccc-…-0001`) and one seller actor but creates **no admin identity/credential**, so there is no
authorized principal able to author the Platform + seller publications, offers, stock, shipping drafts and
coupons. Creating that principal or that data would require inventing an identity/grant or direct business SQL —
both forbidden. The sanctioned demo seed requires that missing admin identity (403 above).

Consequence — NOT produced in this run: Marketplace Platform group + two Seller business scopes on one binding,
per-group owner responses/captures, mixed 3-group cart read → save → payment preview → Order, per-group coupon
applicability, post-order publication immutability receipt, and the payment-page 390/360 native captures.
SingleStore channel is NOT relabelled as Marketplace.

## 3. Corrections to 238 (required by this task)

- **Invalid-coupon read**: 238's "invalid coupon: read stays 200 (tolerant)" was a 200 **without any priced
  group**; it is not a priced acceptance. The verified behaviour is only the selection/commit refusal with
  `checkout.coupon.not_applicable` (409). Restated.
- **SQLSTATE 22001 attribution**: 238 blamed `inventory.outbox_messages.correlation_id varchar(128)`.
  Source-confirmed normalization (`CorrelationIdContext.TryNormalize` via `MessagingCorrelation`) accepts a GUID
  and emits **32 chars**, well under 128, so that attribution is **NOT source-confirmed**; no saved
  `PostgresException` Schema/Table/Column/parameter metadata existed to attribute it. Nothing was enlarged,
  truncated, swallowed or bypassed, and simultaneous idempotency is NOT certified. The 238 client-side
  `payInFlightRef` remains, but it cannot close a server-side failure.

## 4. Checks / time / runtime

- No source change was required (the 239 issuance commit is docs-only; Code-SHA == Base-SHA).
- No migration/schema/baseline/seed authored; the new DB was created and migrated with the official runner only.
- No test suite replayed (no source change); `/health` and the real endpoints above are the evidence.
- Elapsed: claimed 07:42:43Z, result ≈08:25Z → ≈42 min vs HARD35 — NONCOMPLIANT, recorded. Two corrections used
  (bootstrap schema gap → official runner; seller-only admin attempt).
- Preserved untouched: original `3000`/`5088`, handed-off `3100`/`5188` with profile
  `D:\temp\tb238-handoff-chrome`, the 230 DEV publication/draft/rates/week/pointer, TB185 paused/default-OFF,
  and all 231/232/236 shared applied state on `tooba_alpha`. The new Marketplace runtime is left running on 5190.
