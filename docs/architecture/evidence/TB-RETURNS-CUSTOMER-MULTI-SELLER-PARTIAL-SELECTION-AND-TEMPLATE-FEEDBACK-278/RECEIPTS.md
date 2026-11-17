# TB-278 — receipts

All commands run from `D:\Users\User\source\repos\SarvNewVer` (or the stated folder), once after the final edit, except
where a re-run is declared.

## 1. Wake, claim and envelope provenance

```
GET  http://127.0.0.1:17321/api/tasks?channelId=tooba-main
     -> 650b8b3d-83d1-4d39-a8fb-1b9be2640416  TB-…-278  Pending  createdAtUtc 2026-11-17T14:20:46Z
GET  /api/tasks?… row keys = id,channelId,taskId,status,createdAtUtc,claimedAtUtc,completedAtUtc  -> no content field
     (declared: the Bridge cannot serve the envelope text; TASK.md is reconstructed from the durable 278-279 plan,
      SOURCE-AUDIT and the AGENTS clarification block)
git  base = 1dc7494f47a416d6bd65b9dce9b4eda971476782  (== Expected-Base-SHA)
git  code commit = d7eadc17c956d472786b5b8df23961a553b49faa  (pushed to origin/main)
```

## 2. Backend — focused owning proofs

```
dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj -c Release --no-build
  Passed!  - Failed: 0, Passed: 148, Skipped: 0, Total: 148, Duration: 540 ms

dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Release --no-build
           --filter "FullyQualifiedName~ReturnsModuleAmsc"
  Passed!  - Failed: 0, Passed: 34, Skipped: 0, Total: 34, Duration: 388 ms

dotnet build src/backend/Tooba.slnx -c Release --nologo
  216 Warning(s)  0 Error(s)
```

`ReturnsCustomerSelection278Tests` (`9/9`, inside the 148) covers:

1. eligibility bound to the owning customer, per-line remaining + unit
2. a foreign customer refused before any line/product fact is exposed
3. an unknown seller order is a typed `return.missing`
4. an unanswered owning precision blocks the line instead of guessing a scale
5. an expired line reports the window blocker and its last returnable instant
6. a whole-piece line refuses a fraction before any row is written
7. a gram line accepts its own quantum and still refuses a finer fraction
8. an unanswered owning precision never grants a fraction but keeps whole pieces lawful
9. an exact replay is answered before the quantum gate reads metadata again

## 3. Frontend — unit + mounted modal

```
cd src/frontend
npx tsx --tsconfig tsconfig.mount-tests.json --test \
    app/returns/return-submission.test.ts app/returns/return-request-modal-278.mount.test.ts
  tests 11  pass 11  fail 0   (7 submission + 4 mounted)

npm run test:returns
  tests 7  pass 7  fail 0

npx tsc --noEmit      -> exit 0
npm run build         -> success  BUILD_ID 0chXf5Nh9mayZ6KZAKbUC
```

Mounted facts (`return-request-modal-278.mount.test.ts`):

1. «the modal shows the authoritative per-seller eligibility and preselects nothing»
2. «a whole-piece fraction and an over-ceiling amount are refused locally; lawful multi-seller selection is sent»
   — one checkout, two seller orders, socks `2` of `3` accepted and the damaged other-seller line sent as its own child;
   the correct item is absent from every request body
3. «a partially successful act keeps its success, reports the uncertain child and re-sends identical bytes»
4. «a checkout with no eligible line offers no selection at all»

Submission facts (`return-submission.test.ts`): one child per selected line across sellers frozen before send; the
line's own unit bounds local validation with no clamp/round; storage is verified (a lying/unavailable storage refuses to
send); unreadable evidence is explicit; only body-dictated refusals are "no effect"; a recorded child is terminal and an
unknown child keeps its bytes; only the exact current act clears.

## 4. Served artifacts (read-only)

```
Host  5088  PID 31444
  dll      artifacts\publish-primary\Tooba.Host.dll
  SHA256   292C479DE80B8B21CEFDA693BBE293E972F86F5789D4A59A18CC9A665406BC27
  /health                 200 {"status":"ok"}
  /health/ready           200 edition SingleStore, postgresql configured, messaging-transport postgresql-sql, messaging Healthy
  /__platform-commerce    200 edition SingleStore / dev-singlestore / store-alpha / tenant-alpha

Next  3000  PID 45076   BUILD_ID 0chXf5Nh9mayZ6KZAKbUC
  /fa                                                      200
  /_next/static/chunks/app/customer-panel/orders/%5BcheckoutId%5D/page-7821fa6a631ec3c4.js
        200 len 31322, contains «قلمی برای مرجوعی واجد شرایط نیست» and «دلیل مرجوعی این قلم انتخاب نشده است»
  /_next/static/chunks/9627-0b0f29163bba673b.js            200 len 35559, carries the react-toastify runtime
```

Live read-only wire:

```
GET /v1/customer/returns/eligibility/11111111-2222-3333-4444-555555555555
  without a customer session -> 401 customer.session.required   (route registered, no fact exposed)
```

No POST, no case, no receipt, no decision, no refund, no stock/hold/provider call, no reset.

## 5. Change set

Modified (20): Returns contracts/domain/application/infrastructure/endpoints/error catalog + resources, three AMSC pins,
`ReturnsCreateIntegrity272Tests`, and FE `return-api.ts`, `return-ui.tsx` (old form removed), `return-request-modal.tsx`
is new, customer order page, admin workspace toast.

Added: `CustomerReturnEligibilitySnapshot.cs`, `ICustomerReturnEligibilityQuery.cs`,
`GetCustomerReturnEligibilityQuery.cs`, `CustomerReturnEligibilityQueryEngine.cs`, `ReturnsCustomerSelection278Tests.cs`,
`return-submission.ts`, `return-submission.test.ts`, `return-request-modal.tsx`,
`return-request-modal-278.mount.test.ts`, plus this evidence set and `docs/ai/tasks/…278/TASK.md`.

No migration, no DDL, no seed/backfill, no schema file change.
