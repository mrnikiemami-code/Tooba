# TB-276 — Receipts

Every command below was executed once after the final source change unless the row says otherwise. Paths are relative to
the repository root. No credential, token or secret is reproduced here (the local dev connection string is the canonical
alpha one already recorded in earlier task evidence).

## Backend

| Step | Command | Result |
| --- | --- | --- |
| focused suite | `dotnet test Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsOperatorWorkflow275Tests" --nologo` | `Passed! Failed: 0, Passed: 15, Skipped: 0, Total: 15, Duration: 665 ms` |
| module guard | `dotnet test Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsModuleAmsc001" --nologo` | `Passed! Failed: 0, Passed: 34, Skipped: 0, Total: 34, Duration: 141 ms` |
| solution build | `dotnet build Tooba.slnx -c Release --nologo` (in `src/backend`) | `220 Warning(s)`, **`0 Error(s)`** |
| first publish attempt | `dotnet publish Host/Tooba.Host -c Release -o …\artifacts\publish-primary` | **FAILED** `MSB3021/MSB3027` — `Tooba.Host.dll` / `Tooba.Catalog.Infrastructure.dll` locked by `.NET Host (48820)` (the running service). Declared, not hidden. |
| graceful stop | `CloseMainWindow()` on PID 48820, wait, then terminate | port 5088 closed, `Test-NetConnection` → False |
| publish (final) | `dotnet publish Host/Tooba.Host -c Release -o D:\Users\User\source\repos\SarvNewVer\artifacts\publish-primary --nologo` | **0 errors**, `Tooba.Host -> …\artifacts\publish-primary\` |

## Frontend

| Step | Command | Result |
| --- | --- | --- |
| typecheck | `npx tsc --noEmit` (in `src/frontend`) | **0** |
| queue/mapping/label suite | `npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-returns-work-queue.test.ts` | `pass 26 / fail 0` |
| mounted workspace suite | `npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-276.mount.test.ts` | `pass 10 / fail 0` |
| error map | `npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-error-map.test.ts` | `pass 8 / fail 0` |
| production build (final) | `npm run build` | **0**, `BUILD_ID gC-lsQo8rzo30IeWT6MwF` |

Earlier in the task the same three FE suites were also run together (`44/44`) and the queue suite alone after each
display edit; the numbers above are the final runs. Two earlier `npm run build` invocations produced
`ZvJnPqlGxLcxFxJo5yIzi` and `wxGdeC94Q5VqyhF4fct3D` before the product/unit column was added; those builds were
superseded and are not the served one.

## Served identity

| Artifact | Value |
| --- | --- |
| Host process | PID `8572`, `dotnet`, started 2026-11-17 03:38:44 local, `--urls http://127.0.0.1:5088`, `ASPNETCORE_ENVIRONMENT=Development` |
| Host dll | `artifacts\publish-primary\Tooba.Host.dll` SHA256 `D998ECBC22459E127166C9C03747CA688475557D58937CBE516D1876BB32838E` |
| `/health` | 200 |
| `/health/ready` | `{"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}` |
| `/__platform-commerce` | `{"edition":"SingleStore","deploymentId":"dev-singlestore","tenantId":"store-alpha","connectionReference":"tenant-alpha",…}` |
| Next process | PID `21544` on `3000` |
| served build | `/_next/static/gC-lsQo8rzo30IeWT6MwF/_buildManifest.js` → 200; `/fa` 200; `/admin/returns` 200 |

## Live read-only wire (dev actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db` from `/v1/admin/dev-context`)

```
GET  /v1/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490            -> 200
     status=Requested origin=CustomerDelivered items=1 receipts=0 decision=null
     receiving.received=0 remaining=1.000000 work=Receiving
     items[0]: productLabel=لنوو اولترابوک مدل 2 unitLabel=pcs quantity=1.000000
GET  /v1/admin/returns/01a1e6b0-…  (no actor header)                  -> 401 admin.actor.missing
POST /v1/admin/returns/query {"page":1,"pageSize":10,"sort":[]}       -> 200 totalCount=1
     counts.allOpen=1 receiving=1 decision=0
POST /v1/admin/returns/{random-guid}/retry-refund                     -> 404 return.missing
POST /v1/admin/returns/{random-guid}/retry-refund?expectedFailedAttemptId={same} -> 404 return.missing
```

## Database (read-only, canonical `tooba_alpha` on `127.0.0.1:5432`, `docker exec postgres-db psql -U admin`)

```
returns.return_requests  : 01a1e6b0-f33a-7000-80ee-beffb6344490 | 01a1db2f-e375-7000-8197-9657a380a505 |
                           01a1db2f-e36e-7000-b7d3-afb2fe86e81a | Requested | CustomerDelivered | 2407839.0000 | IRR
returns.return_items     : 01a1e6b0-f33f-7000-9d63-58fe9922572c | 01a1db2f-e379-7000-80f5-91d1401c225c |
                           1.000000 | Usable | None
counts                   : customer_return_receipts 0 | customer_return_receipt_lines 0 |
                           return_request_decisions 0 | refund_attempts 0
cases for that checkout  : 1
returns totals           : return_requests 1 | status Requested 1
outbox                   : 1 row, event_type return.requested.v1, processed, attempt_count 1
order."seller_orders"    : 01a1db2f-e375-7000-8197-9657a380a505 -> Paid (unchanged)
```

## Browser measurement (headless Chrome over CDP, DOM/text only, no screenshots)

```
/fa/admin/returns @1440         docOverflow=0  strips=7 (each 44px)  grid 1071/1652
   row RET-TB-20261114175151-01-7d5f59-1 | ۱ عدد | دریافت کالا | در انتظار بررسی | بدون بازگشت وجه
   after scrolling the grid scroller: «۵ روز باقی‌مانده · مهلت ۳۰ آبان ۱۴۰۵» and «۱۴۰۵/۰۸/۲۶, ۰۲:۵۸»
/fa/admin/returns @390          docOverflow=0  strips wrap  grid 340/1652
/fa/admin/returns/{case} @1440  docOverflow=0  لنوو اولترابوک مدل 2 | واحد: عدد | ۱ ۰ ۰ ۰ | — |
                                «ثبت دریافت کالا» only
/fa/admin/returns/{case} @390   docOverflow=0  same content, single-column
receipt form open/cancel        @1440 and @390: 4 controls, min height 44, quantity "", condition "",
                                cancel closes the form and re-enables the action, no request sent
```
