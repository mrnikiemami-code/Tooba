# TB-277-R1 — receipts (commands actually executed, final state)

All commands were run from `D:\Users\User\source\repos\SarvNewVer` after the final code change, unless a line is marked
`[declared: earlier]`.

## Backend

| # | Command | Result |
| --- | --- | --- |
| 1 | `dotnet test src\backend\Modules\Returns\Tooba.Returns.Tests\Tooba.Returns.Tests.csproj -c Release --no-restore` | **139 passed / 0 failed / 0 skipped** |
| 2 | `dotnet test src\backend\Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~ReturnsModuleAmsc001"` | **34 passed / 0 failed** |
| 3 | `dotnet test …Tooba.Host.Tests.csproj -c Release --no-restore --no-build --filter "FullyQualifiedName~ReturnsCustomerReceipts274"` | **10 passed / 0 failed / 0 skipped** (real `tooba_alpha`) |
| 4 | `dotnet build src\backend\Tooba.slnx -c Release --no-restore` | Build succeeded, **0 errors**, 372 warnings |
| 5 | `dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release --no-restore -o artifacts\publish-primary` | succeeded, 0 errors |

Failure → fix record (declared):
- Run 1 first failed 2 tests: `ReturnsCharacterizationTests.Create_approve_refund_preserves_amount_currency_and_is_idempotent`
  and `…Wallet_destination_credits_wallet_without_psp_refund` expected `Completed` from `ApproveAsync`. TB-277 already made
  approval decision-only, so these characterizations were stale at the issued base; they now assert `Approved` + zero
  gateway/Wallet/Inventory effect and then the separate `StartFirstRefundAsync` performing the money. A first compile of
  that edit used the wrong overload; corrected to `StartReturnRefundCommand`. The passing run above is post-fix.
- Run 2 first failed 9 guard facts: TB-277 added `POST …/start-refund`, `StartReturnRefundCommand`,
  `StartReturnRefundCommandValidator` and `return.receipt.unit_quantity_invalid` without updating the AMSC matrices. Pins
  were extended to 14 routes / 7 validators / 45 HTTP-reachable + 1 platform codes / 45 catalog entries, and the new
  validator's registration is now asserted through the real container. The passing run above is post-fix.

## Frontend

| # | Command | Result |
| --- | --- | --- |
| 6 | `node --experimental-strip-types --test app/returns/return-quantity.test.ts` | **7 passed / 0 failed** |
| 7 | `node --experimental-strip-types --test app/admin/admin-returns-work-queue.test.ts` | **26 passed / 0 failed** |
| 8 | `node --experimental-strip-types --test app/admin/admin-error-map.test.ts` | **8 passed / 0 failed** |
| 9 | `npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-277.mount.test.ts` | **8 passed / 0 failed** |
| 10 | `npm run typecheck` | exit 0 |
| 11 | `npm run build` | exit 0, `BUILD_ID 0RrDo7CGZ60FPsYrWxQ1a` |

Failure → fix record (declared):
- Run 7 first failed one assertion because the test expected `over_ceiling` where the unit gate legitimately fires
  `precision` first (`2.000001` with 0 decimals). The fixture now uses `3` against a 2-piece ceiling for the ceiling case.
- Run 9 first failed because its helper built ceilings in whole units while the entry function now takes exact millionths;
  the helper and the three ceiling assertions were corrected to `10_000_000`.
- Run 8 first failed because `return.receipt.unit_quantity_invalid` had no localized copy; Persian/English copy was added
  to `admin-error-map.ts`, after which the build (and therefore the served BUILD_ID) changed. Build `4WEybweeGXoEhN4By0im4`
  was superseded by `0RrDo7CGZ60FPsYrWxQ1a`; only the final build is served.
- An earlier `npm run build` died with a Windows build-worker crash (`0xC0000409`) during static generation; the identical
  command re-run succeeded. No code change was made between those two runs.

## Deployment / runtime

| # | Action | Result |
| --- | --- | --- |
| 12 | `Stop-Process -Id 49764` (old Host) | stopped, port 5088 free |
| 13 | `dotnet …\artifacts\publish-primary\Tooba.Host.dll --urls http://127.0.0.1:5088` | PID **21060** |
| 14 | `GET /health` | **200** |
| 15 | `GET /health/ready` | **200** `{"edition":"SingleStore","postgresql":"configured","messaging":"Healthy"}` |
| 16 | `GET /__platform-commerce` | **200** `store-alpha` |
| 17 | Host dll SHA256 | `8AA71DD82E6D737F9698A93D0F99EE7A77EB07E02916E4185DE9C271890E7948` |
| 18 | `Stop-Process -Id 9544` (old Next) → `next start -p 3000` | PID **7864** |
| 19 | `GET /fa` | **200** |
| 20 | `GET /_next/static/0RrDo7CGZ60FPsYrWxQ1a/_buildManifest.js` | **200** |
| 21 | `GET /_next/static/chunks/1323-979bf525b6515616.js` | **200**, contains the unit-quantity refusal copy |
| 22 | `GET /_next/static/chunks/7952-040a265ac5604fbd.js` | **200**, contains the `start-refund` route and the retained ceiling copy |
| 23 | `GET /fa/admin/returns` and `/fa/admin/returns/01a1e6b0-…` | **200 / 200** |
| 24 | `GET /v1/admin/dev-context` | 200 → dev admin actor used for the read-only checks |

## Live read-only wire (GET only — no write was attempted)

| # | Request | Result |
| --- | --- | --- |
| 25 | `GET /v1/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490` without actor | 403 `admin.authorization.denied` (authorization runs before the read) |
| 26 | Same with the dev admin actor header | **200**; `status=Completed`, `refundAmount=2407839.0000`, `paymentId` present, item `unitLabel=pcs`, `unitDecimalPlaces=0`, `condition=Usable`, `disposition=SaleableRestock`, `saleableRestockedQuantity=1.000000`, one `Succeeded` attempt |
| 27 | `GET /v1/admin/returns` grid / case pages | 200 (served HTML) |

## Not run (deliberately out of scope)

Whole `Tooba.Returns` + whole Host project runs were limited to the focused filters above; no Shipping/Order/parity/stress
suite, no whole-module AMSC recertification, no migration plan/apply, no fixture or cleanup script.
