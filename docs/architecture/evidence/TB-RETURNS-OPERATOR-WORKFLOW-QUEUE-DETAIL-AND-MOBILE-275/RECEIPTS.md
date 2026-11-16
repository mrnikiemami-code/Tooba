# TB-275 — receipts ledger (what was actually executed)

All entries below are real executions in this working copy during this task. Nothing was replayed from memory, from
an earlier task's evidence, or from an LLM summary.

## Build / test lanes

| # | Command | Result |
| --- | --- | --- |
| 1 | `dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsOperatorWorkflow275Tests"` | **Passed 12, Failed 0, Skipped 0** (681 ms) |
| 2 | `dotnet build src/backend/Tooba.slnx -c Release` | **0 Error(s)**, 7 Warning(s) (all `NU1900` offline package-feed warnings, pre-existing) |
| 3 | `npx tsc --noEmit` (`src/frontend`) | 0 errors |
| 4 | `node --experimental-strip-types --test app/admin/admin-returns-work-queue.test.ts` | **pass 21 / fail 0** (re-run after the final receipt-form and storage-guard edits; the previous run of the same suite was 20/20 before the two in-correction test/FE edits — declared) |
| 5 | `npm run build` (`src/frontend`) | `✓ Compiled successfully`; final `BUILD_ID TaMAKtGjoy8sg6NHdnsO9` (an earlier build of this task produced `oyZqjXe6auOIgTkU-Gm6R` and was rebuilt after two small FE corrections) |

## Served artifacts

| # | Fact | Value |
| --- | --- | --- |
| 6 | Host publish (performed before the FE-only edits of this task; no backend source changed afterwards) | `artifacts/publish-primary/Tooba.Host.dll` SHA256 `02AECDC72FA4506C889C743713CD8A93A1983F25C489268B4E0E3F55BF48DD73`, written 19:58:15 while the newest backend source was 19:55:26 |
| 7 | Host process | PID `16532`, loaded module path `…\artifacts\publish-primary\Tooba.Host.dll`, module SHA256 identical to #6 |
| 8 | `/health` / `/health/ready` | `200` / `200` `{"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}` |
| 9 | Next server | PID `50724`, `BUILD_ID TaMAKtGjoy8sg6NHdnsO9` |
| 10 | Served workspace chunk | `/_next/static/chunks/692-4941a8a633b47e39.js` → 200 (169,248 B), contains `دقیقاً همانقدری`, `انتخاب کنید`, `پروندهای برای نمایش نیست`, `ذخیرهٔ محلی این دستگاه در دسترس نیست` |
| 11 | Served queue page | `GET /fa/admin/returns` → 200 (18.6 KB HTML, App Router; strip copy lives in the served queue chunk) |

## Live read-only wire probes (Host 5088, dev actor header `X-Tooba-Dev-Actor-User-Id`)

| # | Request | Result |
| --- | --- | --- |
| 12 | `POST /v1/admin/returns/query` **without** actor | `401 admin.actor.missing` |
| 13 | `POST /v1/admin/returns/query` with actor, `{}` | `200 {"items":[],"page":1,"pageSize":20,"totalCount":0,"counts":{"allOpen":0,"receiving":0,"decision":0,"refund":0,"refundFailed":0,"disposition":0,"history":0}}` |
| 14 | `…workCategory in ["refund_failed"]` | `200`, same honest empty page + counts block |
| 15 | `…workCategory in ["pending_review"]` (legacy) | `200`, translated once, no error |
| 16 | `…workCategory in ["refund_needed"]` (legacy) | `200`, translated once, no error |
| 17 | `…workCategory in ["zzz_unknown"]` | `200`, falls back to the active set (never "everything", never a 500) |
| 18 | `…workCategory in ["history"]` | `200`, empty history page + same counts |
| 19 | `advancedFilter` conditions naming `workCategory` | `400 grid.advancedFilter.field.invalid` |
| 20 | `filters[{field:"workCategory",value:"refund_failed"}]` (no operator/values) | `400 grid.filter.operator.invalid` |
| 21 | `GET /v1/admin/returns/00000000-0000-0000-0000-000000000000` | `404 return.missing` |
| 22 | `POST /v1/admin/returns/00000000-0000-0000-0000-000000000000/retry-refund` (no reviewed failure) | `404 return.missing` (case read first; no money, no attempt minted) |

## Live DOM / geometry (CDP `Runtime.evaluate`, no screenshots)

| # | Viewport | Facts |
| --- | --- | --- |
| 23 | 1440 | `documentElement.scrollWidth - clientWidth = 0`; 7 `role=tab` strips, every one 44px, all counts in Persian digits (`۰`); grid says «ردیفی برای نمایش نیست» / «نمایش ۰ تا ۰ از ۰ ردیف» |
| 24 | 390 | document overflow `0`; tab strip overflow `0` (wraps locally); min tab height `44`; grid text viewport `340` with `1652` content (scoped horizontal scroll inside the grid only) |
| 25 | 390, `/fa/admin/returns?queueFilter=refund_needed` | selected strip = «پیگیری بازگشت وجه» (legacy token translated once); no legacy+new double filter |
| 26 | user click on «سابقه» | `location.search` became `?workCategory=history` (legacy token dropped), selection moved, honest empty page |
| 27 | 390 + 1440, `/fa/admin/returns/00000000-…` | renders «پروندهای برای نمایش نیست» + «این پروندهٔ مرجوعی یافت نشد.» + two 44px «بازگشت به صف کار» links; `hasLoading=false` (no permanent loading text); overflow `0` |

## Explicitly NOT executed

- No migration, DDL, seed, backfill, reset, cleanup, restore or new database.
- No live case creation and no valid live receive / approve / reject / retry / recovery mutation.
- No provider, PSP, Wallet, Inventory, waybill or notification call.
- No screenshots; no screenshot artifact.
- No whole-Returns / Host / AMSC / Order / Fulfillment / Shipping / parity suite run; no baseline update.
- No second worker, no subagent, no historical agent resumed.
