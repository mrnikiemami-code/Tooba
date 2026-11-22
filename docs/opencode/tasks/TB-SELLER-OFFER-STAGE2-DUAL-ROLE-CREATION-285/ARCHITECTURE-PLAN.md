# ARCHITECTURE-PLAN — TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285

Protocol: TOOBA-OPENCODE-DELIVERY-V1@1.0.0
Base-SHA: 2d3230e2fbfb50b0931f4e716e300628747ce112
Task-SHA256: see `REQUIREMENTS.json`/TASK issue record.
Prepared-before-production-edit: yes; product source was not edited before this plan.

## Outcome

Stage 2 delivers one safe shared creation capability for admin and seller on the existing shells:

1. Admin selects seller A and a published Catalog variant, creates one Draft Offer, and reopens it.
2. Seller A sees the same Offer id; seller B does not.
3. View-only admin and foreign sellers are denied with zero persisted change.
4. A retry after Offer creation/price/stock failure or lost response returns the original Offer and completes only remaining steps.
5. Catalog picker searches/pages published variants beyond the newest 100 products.
6. Admin and seller Offer lists use server search/sort/page over more than 200 offers.
7. Cancel before the first send writes nothing; partial Draft is explicit, truthful and resumable.
8. Existing AdminShell/VendorShell and design-system controls are used in fa/en, RTL/LTR, desktop and 360x640.

## Architecture and owners

- Catalog owns product/variant publication and published-variant search. No Catalog mutation from Offer.
- Offer owns Offer identity, seller ownership, Draft lifecycle, request identity, admin/seller list scope.
- Pricing owns price. Inventory owns stock positions, reservations and Stage-1 StockItemId safety.
- Party owns seller directory. AccessControl owns roles, capabilities and admin/seller membership.
- Admin and seller endpoints reuse existing thin HTTP + MediatR + `ApiResponseFactory`; no second shell or form engine.

## Request and response contracts

### Creation

- Seller `POST /v1/seller/offers` body: `catalogVariantId`, `sellerSku`, `status`, `requestId`.
- Admin `POST /v1/admin/offers` body: `sellerPartyId`, `catalogVariantId`, `sellerSku`, `status`, `requestId`.
- Both dispatch the existing `CreateOfferCommand` with an explicit seller party. Admin never writes seller headers or impersonates.
- `requestId` is optional for compatibility. If present, Offer derives:
  `offerId = Guid(SHA256(sellerPartyId|variantId|requestId)[0..16])`.
  The handler first reads that id. Same seller/variant returns the original detail; a mismatched intent fails closed before any write. No schema change.
- New offers are created Draft by default in both UIs. The API keeps existing status semantics.

### Resume

- The browser writes a request id to `localStorage` keyed by shell+seller+variant before the first create and verifies read-back. It is not removed until completion.
- After the Offer exists, the page compares returned detail price/stock. It calls Pricing only if the amount differs and Inventory only if the position/aggregate differs. A failed step leaves the same Offer id and an explicit incomplete notice.
- Controlled response loss is used only for the error branch after a real write. No blind second Create.

### Authorization

- Admin endpoints use `IAdminPanelAccess` plus `IAuthorizationService` capability:
  `product.view` for list/detail/picker, `product.create` for create, `product.edit` for patch/price/inventory.
- Seller list/detail keep the existing Party view gate. Seller create requires `product.create`; seller patch/price/inventory require `product.edit` through `IPlatformEffectiveAccessReader` on the seller owner scope.
- Seller A cannot use seller B's header because the party member check remains authoritative. Admin cannot act as a seller by body/header; the admin actor is checked on the admin route.

### Server paging

- Catalog: `GET /v1/seller/catalog-variants?search&page&pageSize` and `GET /v1/admin/catalog/variants?search&page&pageSize` return `{items,totalCount,page,pageSize,hasMore}`. SQL filters `Product.Status=Published`, matches variant code or localized product name, orders by product updated time then code, and applies `CountAsync`/`Skip`/`Take`.
- Offer seller: `POST /v1/seller/offers/query` body is the existing `GridQueryRequest` shape; search/sort/page are normalized, seller scope is fixed from auth.
- Offer admin: `POST /v1/admin/offers/query` same body; optional seller filter is a server body value with admin capability, not a seller header.
- The Offer query count and page are separate SQL operations; only the page is enriched through the existing Offer read composer. No full-array load or browser filtering.
- Stable order: `updatedAt desc, offerId desc`, or explicit `sellerSku`/`status` sort with `offerId desc` tie-break.

### Admin pages

- Existing AdminShell: add an “Offers” item under the existing Products group, `/admin/offers`, with `product.view`.
- `/admin/offers`: server-paged grid.
- `/admin/offers/new`: seller picker using existing `/v1/admin/sellers/query`, published Catalog picker, Draft create and resume notice.
- `/admin/offers/[offerId]`: admin detail read and link to the seller edit surface only as a reference, not an impersonation path.

### Seller pages

- Existing VendorShell product list: DataGrid server adapter to `/v1/seller/offers/query`.
- Existing seller create page: searchable/paged Catalog picker, Draft-first, request identity, partial/resume state, zero-write cancel.

## Scenario matrix

| ID | Requirement | Action | Expected effect | Proof |
|---|---|---|---|---|
| S-01 | REQ-01 | Admin creates Draft for seller A and reopens | One Offer id, seller A, seller A read, B denied, Catalog unchanged | Connected admin+seller browser, GET, SQL |
| S-02 | REQ-02 | View-only admin + seller B direct write attempts | 403/denied, zero rows/counters | Real Host + SQL |
| S-03 | REQ-03 | Lost response after real Offer write; retry same request id | Same Offer id, no second Offer; remaining steps only | Browser controlled loss + GET/SQL |
| S-04 | REQ-04 | Search old `TB285-OLD-VAR`, page picker | Old variant appears, non-published absent, selection retained | Browser + request trace + SQL |
| S-05 | REQ-05 | Seller/admin search and page 200+ offers | Server page/count; rows beyond first page | Browser + request trace + SQL |
| S-06 | REQ-06 | Cancel before write; malformed response; denial; retry | Zero write / no false success / recoverable Draft | Connected error branches + GET/SQL |
| S-07 | REQ-07 | 360x640 and desktop, fa/en, keyboard | No overflow, 44px primary, readable and reachable | Chromium geometry + Impeccable critique |
| S-08 | REQ-08 | Diff/contract/schema/verifier audit | No unsafe write, no Stage 3/discount, strict spec and verifier negative tests | Diff + OpenSpec + verifier |

## Internal waves

| Wave | Depends on | Requirement | Work | Exit |
|---|---|---|---|---|
| W1 | preflight | REQ-02, REQ-08 | Admin/seller authorizers, explicit seller scope, no impersonation; inspect contracts | Denials proven |
| W2 | W1 | REQ-01, REQ-03, REQ-06 | Draft-first create, deterministic request identity, resume/error truth, admin create/read | Same Offer id and no duplicate |
| W3 | W2 | REQ-04, REQ-05 | Catalog picker SQL search/page and Offer list SQL search/page for both roles | 150+/200+ fixtures proven |
| W4 | W3 | REQ-07 | Admin/seller shared-shell UI, picker/list adapters, fa/en/mobile, Impeccable critique and post-check | Usable UI |
| W5 | W4 | all | Connected 8-REQ matrix, negatives, builds, strict spec, verifier negative tests, cleanup, diff audit | READY or BLOCKED |

## Preflight evidence

- `git fetch origin`; HEAD/origin `2d3230e2fbfb50b0931f4e716e300628747ce112`; expected base `26f9a7a4d24d009f25b66b4ea589df884c2a1aa1` is an ancestor.
- Disposable PostgreSQL 17.10 database `tooba_offer285_connected` in `postgres-db` on 127.0.0.1:5432.
- Host A 5091 (PID 20152), full Development seed; Host B 5092 (PID 19296), skip-bootstrap projection for the view-only admin.
- Host binary SHA256 `E301B13E558899F59F19505EB7059D3BFC65518BD1DAD035D223F0DDAB321019`; both `/health/ready` 200, SingleStore, messaging disabled.
- Isolated Next 3101 (PID 49588) built against Host 5091; BUILD ID `YygLRZmLHbbt-c5Syylii`; seller create route 200.
- Real Development actors:
  - seller A `01a2045c-9b4a-7000-ba31-538e7f3c3166`, actor `01a2045c-9c6e-7000-94e8-2fd465b302fd`;
  - seller B `01a2045c-9b4d-7000-8a84-562cac7c80e7`, actor `01a2045c-9cd5-7000-9853-9213b2936510`;
  - admin editor `01a2045c-9d31-7000-add1-1dfc712f11b2`;
  - admin view-only `66666666-6666-4666-8666-666666666666` with platform role `55555555-5555-4555-8555-555555555555` containing only `product.view`.
- Fixtures: 204 Published products, 205 variants, 206 offers; seller A 102 / seller B 101; old published variant `TB285-OLD-VAR` with product UpdatedAt 2020.
- Baseline reproduced before code: seller picker returns 100 variants and excludes `TB285-OLD-VAR`; seller list returns all 102 offers and ignores `?search=TB285-SKU-137`; view-only admin can read `/v1/admin/sellers/query`.
- Cleanup: drop the disposable database and stop only 5091/5092/3101 after evidence. Canonical 5088/3000 untouched.

## Tool plan

- OpenSpec change `seller-offer-dual-role-creation-285`: strict validation before code.
- Impeccable: actual critique of the current admin/seller create flow, explicit decisions, implementation, post-check; record in `IMPECCABLE-CRITIQUE.md`.
- Playwright real Chromium against served 3101/5091 and direct 5092 for view-only denial; independent GET/SQL.
- OpenCode verifier with negative self-tests (tamper, missing REQ, changed expected fact).
