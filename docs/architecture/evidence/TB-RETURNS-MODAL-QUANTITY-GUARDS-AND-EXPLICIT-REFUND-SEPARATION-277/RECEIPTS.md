# TB-277 — RECEIPTS

Every command below was executed in this workspace on the final code. Nothing is estimated.

## 1. Focused backend tests (final code, one run after the final edit)

```
dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj -c Release
  --filter "FullyQualifiedName~ReturnsExplicitRefund277Tests|FullyQualifiedName~ReturnsCustomerReceipts274|FullyQualifiedName~ReturnsOperatorWorkflow275Tests"

Passed!  - Failed: 0, Passed: 61, Skipped: 0, Total: 61, Duration: 645 ms - Tooba.Returns.Tests.dll (net8.0)
```

## 2. Mounted real workspace

```
cd src/frontend
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-277.mount.test.ts

tests 8   pass 8   fail 0
```

Facts: no inline form; one dialog per action; over-ceiling refused in-dialog with zero requests; unit precision
(whole pieces vs 3-decimal grams) + Persian digit/separator normalization; usable+damaged sharing one ceiling;
approve → Order operations only with no refund route touched, first refund → `/start-refund`; rejection reason
< 3 chars refused; undisposed goods read «تعیین‌نشده» and the settled line reads «برگشت به موجودی قابل فروش (۱)».

## 3. Typecheck

```
npx tsc --noEmit        → exit 0
```

## 4. Frontend production build

```
npm run build           → exit 0
.next/BUILD_ID          → VAx5DNtd8dMTFmyI5e2OR
chunk carrying the workspace copy → /_next/static/chunks/3693-1ab82267656ab504.js
  (contains "admin-return-settle-form" = true, "برگشت به موجودی قابل فروش" = true)
```

## 5. Backend build + publish

```
dotnet build src/backend/Tooba.slnx -c Release     → 0 Error(s), 217 Warning(s)
dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary
                                                   → Tooba.Host -> artifacts/publish-primary/ (0 errors)
artifacts/publish-primary/Tooba.Host.dll SHA256    → 64C74398E1A65D95D8F99F98BA88D47DAC8E0B7CEA6D3B7AFF3D414F8C2F9EB0
```

The publish was re-run after the only source edit made after the first publish (a brace formatting fix in
`ReturnsRequestValidators.cs`); it produced the **identical** DLL hash, confirming no IL change. Host was stopped
gracefully before publish and restarted afterwards.

## 6. Served artifacts

```
Host  : port 5088, PID 49764
        dotnet artifacts\publish-primary\Tooba.Host.dll --contentRoot artifacts\publish-primary
        /health            → 200 {"status":"ok"}
        /health/ready      → 200 {"status":"ready", edition SingleStore, authorization inmemory,
                                  messaging-transport postgresql-sql, messaging Healthy}
        /__platform-commerce → 200 {"edition":"SingleStore","tenantId":"store-alpha",...}

Next  : port 3000, PID 7020  (node node_modules/next/dist/bin/next start -p 3000)
        BUILD_ID           → VAx5DNtd8dMTFmyI5e2OR
        /fa/admin/returns  → 200
        /_next/static/<BUILD_ID>/_buildManifest.js → 200
```

## 7. Live wire (read-only + one disclosed zero-effect replay)

```
no actor, POST /v1/admin/returns/{unknown}/start-refund
    → 401 admin.actor.missing
actor 01a036c2-970e-7000-8eb7-94bf5cc2d8db, POST /v1/admin/returns/00000000-0000-4000-8000-000000000000/start-refund {}
    → 404 return.missing
actor, GET /v1/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490
    → 200 (recovery.phase Settled, financialComplete true, physicalComplete true,
           item disposition SaleableRestock + saleableRestockedQuantity 1,
           work = settled / canStartRefund false / actionCodes [])
actor, POST /v1/admin/returns/query {}
    → 200 {"items":[],"totalCount":0,
           "counts":{"allOpen":0,"receiving":0,"decision":0,"refund":0,"refundFailed":0,"disposition":0,"history":1}}
actor, POST /v1/admin/returns/01a1e6b0-…/start-refund {}
    → 200, REPLAY of the stored Succeeded attempt (disclosed; zero effect, see §8)
```

## 8. Zero-effect audit of the exercised case (read-only SQL, immediately after the probe)

```
requests 1  max(updated_at) = 2026-11-17 00:31:26.384627+00
items    1
refund_attempts 1  01a1e6ea-7094-7000-a106-a425bcf04977  Succeeded
                   key refund-01a1e6b0f33a700080eebeffb6344490  created 2026-11-17 00:31:26.356101+00
customer_return_receipts 1 / lines 1
return_request_decisions 1
returns.outbox_messages: return.requested.v1 (2026-11-14 ×2, 2026-11-16 23:28:38.720117+00),
                         return.approved.v1 (2026-11-17 00:31:26.356049+00),
                         refund.succeeded.v1 (2026-11-17 00:31:26.402753+00)
                         → NOTHING after 00:31:26; no row, key or event from this worker
```

## 9. Live served DOM (GET only, no screenshot)

```
case 1440/390 : «تسویه‌شده», «همهٔ آثار مالی و فیزیکی این پرونده ثبت شده است.»
                «مبلغ مرجوعی ۲٬۴۰۷٬۸۳۹ ریال», «تصمیم ثبت‌شده تأیید»
                «لنوو اولترابوک مدل 2 / واحد: عدد», requested ۱ received ۱ usable ۱ damaged ۰
                disposition «برگشت به موجودی قابل فروش (۱)»
                review actions rendered: 0   open dialogs: 0
                document overflow: 0 at 1440 and 390
                raw ISO/IANA text present: false
queue        : «۰ پروندهٔ باز», stage counts all ۰, «سابقه ۱»,
               «ردیفی برای نمایش نیست», document overflow 0
```

## 10. Schema / data

```
migrations: none added or applied; zero DDL/backfill/seed/reset/cleanup/restore
databases:  one canonical tooba_alpha
live business writes by this task: NONE (see §8)
provider / PSP / Wallet / Inventory / waybill / notification calls: NONE
```
