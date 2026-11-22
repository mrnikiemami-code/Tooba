# RECEIPTS — TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284

## Identity

- Repository: `mrnikiemami-code/Tooba`, branch `main`.
- Issued task SHA256: `f658d3da7745d0c3297c17c6c713f371dc2fdcc72c9bde156a7388192f3f2a5c`.
- Expected base: `f67d6ecd6cb222a2a17d39d6e2d6af391218e4ad`.
- Reviewed product code: `611f97a85ec7c2e464e4f649820992202ca3754c` (unchanged by this task).
- Plan commit: `78868597554dc920bc6a11b1d9b6ef96ba07d069`.
- Plan SHA256: `de9cbd3358b70258f2923359732a700cbec8bfa4f0e9911e02196694e8b72654`.
- Evidence commit: final docs/evidence commit; see BRIDGE envelope.

## Tools and versions

- OpenSpec 2.1.1: `C:\Users\User\OpenCode-UI-Lab-20261122-064208\node_modules\.bin\openspec.cmd`.
- Playwright 1.65.1: `C:\Users\User\OpenCode-UI-Lab-20261122-064208\node_modules`; installed Chromium.
- Impeccable 4.10.0: installed guidance was available; no UI defect was demonstrated, so no UI redesign/critique change was made.
- .NET 8, Node 24, PostgreSQL 17.10.

## Isolated runtime

- Database: `tooba_offer283_connected`, dropped after proof.
- Host: `artifacts/publish-primary/Tooba.Host.dll`, PID 40848, `http://127.0.0.1:5090`, DLL SHA256 `E301B13E558899F59F19505EB7059D3BFC65518BD1DAD035D223F0DDAB321019`.
- Host environment: Development, tenant `store-alpha`, connection reference `tenant-alpha` overridden to the disposable database, messaging disabled, media root `D:\temp\tb284-media`.
- Next: isolated copy `D:\temp\tb284-fe`, BUILD ID `oWTA29FFSAnndeeVyDFsx`, `http://127.0.0.1:3100`, `TOOBA_HOST_ORIGIN=http://127.0.0.1:5090`.
- Browser: Playwright Chromium; real page `/fa/vendor-panel/products/01a2041e-bb6b-7000-9163-d736d71634d5`.
- Fixture: seller party `01a2041e-bb65-7000-9b34-1d049b3a788a`, actor `01a2041e-bca5-7000-87e7-f621a913c987`; positions `01a2041e-bbe0-7000-9897-006e14fe11c0` 3/1 and `01a2041e-bbd3-7000-9fe5-0e7d2157003e` 7/2; price 2,500,000 IRR; SKU ARM-LN-01.
- Canonical preservation: Host 5088 `/health` 200 and Next 3000 `/fa` 200 after cleanup; canonical `tooba_alpha` was never the Host target.

## Commands and actual results

### Preflight

- `docker exec postgres-db psql -U admin -d postgres -c "CREATE DATABASE tooba_offer283_connected;"`: exit 0.
- Host health: `/health` 200; `/health/ready` 200 with PostgreSQL configured and messaging disabled.
- `GET /v1/seller/dev-contexts`: 200 with the real actor and seller party.
- Real browser loaded two positions, price 2,500,000 and SKU ARM-LN-01.
- Preflight browser save produced one PATCH; direct SQL read `ARM-LN-01-PREFLIGHT`; fixture reset to `ARM-LN-01`.

### Connected acceptance

Command:

```powershell
$env:NODE_PATH='C:\Users\User\OpenCode-UI-Lab-20261122-064208\node_modules'
node docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284/evidence/connected-284.cjs
```

Result: exit 0, `ok: true`, 7/7 scenarios PASS, 38/38 checks PASS.

Key observed facts:

- REQ-01: exactly one PATCH to `/v1/seller/offers/01a2041e-bb6b-7000-9163-d736d71634d5`; zero price and zero inventory writes; GET 200; SQL new SKU; price 2,500,000; positions 7/2 and 3/1; reservations unchanged; inventory outbox 27 -> 27.
- REQ-03: A emitted one `PUT /price` with amount 2,750,000 and zero PATCH; B stayed on 2,500,000 before its save; B emitted one PATCH and zero price writes; SQL and GET showed 2,750,000 with SKU ARM-LN-01-C03; positions and reservations unchanged.
- REQ-02: Draft stayed Draft; limits cleared from 2/9 to null.
- REQ-04: position changed 7 -> 8, first stayed 3, total 11; one inventory PUT; ambiguous 400, foreign 400, below-reserved 409; zero change and outbox unchanged.
- REQ-05: wrong identity 200 response produced no success and zero SQL change; lost response came after a real write, independent GET showed the new SKU, and no automatic second write occurred.
- REQ-06: 360x640, document scrollWidth 360 = innerWidth, overflow 0; Save, Cancel and position present; dirty cancel emitted zero writes; reopen showed ARM-LN-01.
- REQ-07: no-change Save emitted zero writes; forced 409 preserved price draft 2,800,000; next Save persisted SKU ARM-LN-01-C07 and price 2,800,000.

Evidence hashes:

- `evidence/connected-284.cjs`: `ebf1f993594684a08bed26d8be9aab87420d2522465cf3f4434597f5a535dc36` (23171 bytes)
- `evidence/connected-284.json`: `0a155bcc2fa5396b8d981e3246036523f70800a3bdf2e02db5c08fb89528afc1` (49275 bytes)
- `evidence/connected-284.log`: `29454955b7d09a3999d18a0bae42a4c4ba35b38c82eb66c887e32cfb595acf3a` (1294 bytes)

### Static and focused checks

- OpenSpec strict: `Change 'seller-offer-safe-edit-283' is valid`, exit 0.
- `npm run test:seller`: 12 tests, 12 pass, 0 fail.
- `npx tsc --noEmit`: exit 0.
- Next production build in isolated copy: success, BUILD ID `oWTA29FFSAnndeeVyDFsx`.
- `dotnet build src/backend/Tooba.slnx -c Release`: exit 0, 0 errors, 717 warnings.
- `dotnet test` filters `SellerOfferSafeEdit283RelationalTests` and `OfferSkuPatch283RelationalTests`: 2 pass, 0 fail.

### Gate outputs

Plan gate:

```text
node docs/ai/delivery/check-readiness.mjs docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284 --stage plan --task-sha f658d3da7745d0c3297c17c6c713f371dc2fdcc72c9bde156a7388192f3f2a5c
EXIT=1
TASK needs unique REQ table rows
requirement scope differs from issued TASK
```

Review gate:

```text
node docs/ai/delivery/check-readiness.mjs docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284 --stage review --task-sha f658d3da7745d0c3297c17c6c713f371dc2fdcc72c9bde156a7388192f3f2a5c
EXIT=1
TASK needs unique REQ table rows
requirement scope differs from issued TASK
wave refers to unissued requirement: REQ-01 ... REQ-07
```

Gate logs:

- `evidence/plan-gate.log`: `a54d062d9f53df4254e5399297eae27e6afbcc3ab5051ae0adb6a0815eda029d`
- `evidence/review-gate.log`: `c64c231f70e1a63a622a337a2dbaeb2047b47e86112e0ba06ff9f2d1961a4fc8`

## Cleanup

- Stopped Host PID 40848 on 5090 and Next PID 18652 on 3100.
- Dropped `tooba_offer283_connected`; query for that name returned no row.
- Ports 5090 and 3100 no longer listen.
- Canonical Host 5088 `/health` 200 and Next 3000 `/fa` 200 after cleanup.
- No production source, migration, DDL, grant, seed, reset, live business write, provider, payment, order or notification.
- No candidate governance file was created because the structural review gate did not PASS.
