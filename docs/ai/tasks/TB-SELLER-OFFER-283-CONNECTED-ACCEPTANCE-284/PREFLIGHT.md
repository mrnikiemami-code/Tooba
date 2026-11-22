# Connected test preflight — TB-284

Date: 2026-11-22 UTC. No product source was edited before this preflight.

## Isolation

- Canonical runtime remains Next `3000` to Host `5088`, database `tooba_alpha`. It was not restarted.
- Disposable database: `tooba_offer283_connected` on the existing Docker `postgres-db` container, PostgreSQL 17.10, user `admin`.
- Isolated Host: existing `artifacts/publish-primary/Tooba.Host.dll`, `ASPNETCORE_URLS=http://127.0.0.1:5090`, `ASPNETCORE_ENVIRONMENT=Development`, connection reference `tenant-alpha` overridden to the disposable database, messaging disabled, media root `D:\temp\tb284-media`.
- Isolated frontend: copy at `D:\temp\tb284-fe` with the repository `node_modules` junction; `TOOBA_HOST_ORIGIN=http://127.0.0.1:5090`; production build; Next start on `3100`.
- Playwright: `C:\Users\User\OpenCode-UI-Lab-20261122-064208\node_modules`, installed Chromium, real browser.

## Verified chain

- `GET http://127.0.0.1:5090/health` returned 200.
- `GET http://127.0.0.1:5090/health/ready` returned 200 with PostgreSQL configured, authorization inmemory, messaging disabled.
- `GET http://127.0.0.1:5090/v1/seller/dev-contexts` returned the real Development actor and the seller party `01a2041e-bb65-7000-9b34-1d049b3a788a`.
- The real VendorShell edit page at `http://127.0.0.1:3100/fa/vendor-panel/products/01a2041e-bb6b-7000-9163-d736d71634d5` loaded two named positions, price 2,500,000 IRR, SKU ARM-LN-01, and positions 3 and 7 with reservations 1 and 2.
- One disposable SKU save from the real page produced exactly one `PATCH /v1/seller/offers/01a2041e-bb6b-7000-9163-d736d71634d5` carrying only commercial fields and a notice. No price or inventory request was emitted.
- Direct SQL then read `ARM-LN-01-PREFLIGHT`; the fixture was reset to `ARM-LN-01`.
- Canonical Host `5088` `/health` returned 200 after the write. Canonical `tooba_alpha` was not used by the test process.

## Fixture

- Offer: `01a2041e-bb6b-7000-9163-d736d71634d5`.
- Seller party: `01a2041e-bb65-7000-9b34-1d049b3a788a`.
- Actor: `01a2041e-bca5-7000-87e7-f621a913c987`.
- Positions: `01a2041e-bbe0-7000-9897-006e14fe11c0` with OnHand 3 / Reserved 1; `01a2041e-bbd3-7000-9fe5-0e7d2157003e` with OnHand 7 / Reserved 2.
- Price: 2,500,000 IRR. SKU: ARM-LN-01.

## Plan gate disclosure

The issued task bytes were hashed as `f658d3da7745d0c3297c17c6c713f371dc2fdcc72c9bde156a7388192f3f2a5c`. The existing `check-readiness.mjs` plan gate reads requirement IDs from markdown table rows shaped as `| REQ-... |` in `TASK.md`. This issued TASK has no such table; it names `CONNECTED-01` and `CONNECTED-03` in prose and refers to original 283 REQs. The plan gate therefore exits 1 with `TASK needs unique REQ table rows` and `requirement scope differs from issued TASK`. No TASK byte was edited and no requirement was invented as issued scope.

## Cleanup

Stop only PIDs on `5090` and `3100`; remove the `D:\temp\tb284-fe` copy and temporary media/logs when the evidence is archived; drop `tooba_offer283_connected` after the final connected run. Canonical sequence, containers other than the disposable database, and user rows remain untouched.
