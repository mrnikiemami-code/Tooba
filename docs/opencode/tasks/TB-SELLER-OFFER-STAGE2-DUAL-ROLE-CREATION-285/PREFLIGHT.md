# Preflight — TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285

Protocol: TOOBA-OPENCODE-DELIVERY-V1@1.0.0
Expected base: `26f9a7a4d24d009f25b66b4ea589df884c2a1aa1` (ancestor).
Execution base: `2d3230e2fbfb50b0931f4e716e300628747ce112`; `HEAD == origin/main` before product edits.

## Chain proved before code

- Real installed Chromium through Playwright 1.65.1.
- Served Next 3101, production build, `TOOBA_HOST_ORIGIN=http://127.0.0.1:5091`.
- Real Host 5091 from `D:\temp\tb285-host-build2\Tooba.Host.dll`, SHA256 `E2DEE16EE9AE582E9D983321FD6ACF98818997637354E56FAF202F5466444ABE`; `/health/ready` 200, Development, SingleStore, messaging disabled.
- Real Host 5092 from the same DLL, skip-bootstrap projection, `/health/ready` 200; used for the view-only admin capability.
- Isolated PostgreSQL 17.10 database `tooba_offer285_connected` in `postgres-db` on 127.0.0.1:5432.
- Seller A: `01a2045c-9b4a-7000-ba31-538e7f3c3166`, actor `01a2045c-9c6e-7000-94e8-2fd465b302fd`.
- Seller B: `01a2045c-9b4d-7000-8a84-562cac7c80e7`, actor `01a2045c-9cd5-7000-9853-9213b2936510`.
- Admin editor: `01a2045c-9d31-7000-add1-1dfc712f11b2`.
- View-only admin: `66666666-6666-4666-8666-666666666666`, platform role `55555555-5555-4555-8555-555555555555` with only `product.view`.
- Fixtures: 204 Published products, 205 variants, 206 offers at baseline; seller A 102, seller B 101; old Published product `tb285-old-product` UpdatedAt 2020 with variant `TB285-OLD-VAR`.
- Disposable DB only; no live user or canonical DB write.

## Baseline before code

- Old seller picker returned 100 variants and did **not** contain `TB285-OLD-VAR`.
- Old seller list returned all 102 rows and ignored the `?search=TB285-SKU-137` parameter.
- Admin shell had no Offer create route.
- View-only admin could read the existing `/v1/admin/sellers/query` route.

## Safety and teardown

- Product code was not edited before this preflight.
- Final teardown stops only Hosts 5091/5092 and Next 3101, then drops `tooba_offer285_connected`; canonical 5088/3000 are checked read-only.
- No Cursor file, AGENTS.md, roadmap, schema migration or live business data is changed.
