# TB-280-R1 — RECEIPTS

## Claim / provenance

```
Claim-ID: de2ab11e-f6c5-45a0-b100-35eab415ee69
Task-ID: TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1
Claimed-At-UTC: 2026-11-18T22:03:38.9629182
Expected-Base-SHA: 9075e2fa4325a2de60cbd17ebfe908d9d9ee74ed
Claim-HEAD: 4ce50d00690f3abdb058289f4a82d63275d136fb
Ancestor-Check: merge-base --is-ancestor Expected-Base Claim-HEAD => 0
Incoming-delta: docs-only issuance (TASK.md + issuance-receipt); zero source/config/schema drift at claim
Issued-TASK SHA256 (issuance-receipt): 0822eff463bc3b143b5e6f905ac579cfd207183b5a6ea2ca79a1638d119e596a (14765 bytes)
Transport: Bridge claim row present; exact issued TASK.md used from repo paths
```

## Focused proof commands (final code)

```
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-280-r1.mount.test.ts
  -> 7/7 pass (M1–M7)

npx tsx --tsconfig tsconfig.mount-tests.json --test \
  app/admin/admin-return-workspace-280-r1.mount.test.ts \
  app/admin/admin-return-workspace-280.mount.test.ts \
  app/admin/admin-return-workspace-279.mount.test.ts \
  app/admin/admin-return-workspace-277.mount.test.ts \
  app/admin/admin-return-workspace-276.mount.test.ts
  -> 34/34 pass
  (first retained run: 277 start-refund fixture lacked approvedAmount under UX1; 279 notice regex expected
   old «دریافت‌شده» copy — both fixtures/assertions updated to the corrected authoritative behaviour, then green)

npx tsx --test app/admin/admin-returns-work-queue.test.ts app/admin/admin-error-map.test.ts \
  app/returns/return-quantity.test.ts app/returns/return-api.test.ts
  -> 49/49 pass

npx tsc --noEmit -> 0 (re-run after next build regenerated .next/types; earlier parallel run hit TS6053 on missing generated types during rebuild — declared intermediate)

npm run build -> 0 errors, BUILD_ID xUhUnq_b5Okl6j0RYf2Jl
```

## Served refresh (frontend only)

```
Host 5088 PID 31652 preserved (no republish)
  dll SHA256 147D033E79F774EE2F24925C501CF0DCE823571529F3ACE0004EBA37C096605B
  GET /health -> 200 {"status":"ok"}
  GET /health/ready -> 200 SingleStore + messaging Healthy

Stop-Process prior Next PID 26856
node node_modules/next/dist/bin/next start -p 3000
  -> Next PID 37500, BUILD_ID xUhUnq_b5Okl6j0RYf2Jl
GET /fa/admin/returns -> 200
GET /fa/admin/returns/01a1eefb-ea96-7000-838d-9a0b37661c8b -> 200
GET /_next/static/chunks/7944-ff7f57f1e093b6b2.js -> 200
  carries: admin-return-received-summary, «هنوز کالایی دریافت نشده است»,
           «مبلغ تأییدشده برای بازگشت وجه», «این قلم دریافت نشده است»
  absent: «برنامهٔ تفکیک», «نسخهٔ بررسی»
```

## Live read-only checks (no POST, no write)

```
GET /v1/admin/returns/01a1eefb-ea96-7000-838d-9a0b37661c8b
  (X-Tooba-Dev-Actor-User-Id=01a036c2-970e-7000-8eb7-94bf5cc2d8db)
  -> status=Requested updatedAt=2026-11-18T14:07:29.430866Z refundAmount=2407839 approvedAmount=null
     receiving.received=0 attempts=0 receipts=0 decisionVersion=0
     canPublishResolution=false canRecordReceipt=true actionCodes=record_receipt category=receiving

POST /v1/admin/returns/query page=1
  -> totalCount=1 counts{allOpen:1,receiving:1,decision:0,refund:0,refundFailed:0,disposition:0,history:3}

DOM 1440: overflow 0; summary «هنوز کالایی دریافت نشده است»; actions=[admin-return-action-record-receipt];
          alerts 0; history closed; no تفکیک/نسخهٔ in working copy
DOM 390 : overflow 0; primary 316×68; open receipt -> 1 dialog, alerts 0, product «لنوو», unit «عدد»;
          cancel -> 0 open dialogs, sessionStorage intent empty, 0 POST
```

## Data / schema / backend

```
zero migration · zero DDL · zero seed/backfill/reset/cleanup/restore
zero live business write · zero POST to any Returns/Order/Payment/Wallet/Inventory/provider route
zero backend source change · Host dll unchanged · no key operation
the stored case keeps its original updated_at and zero receipts/attempts/decisions
```
