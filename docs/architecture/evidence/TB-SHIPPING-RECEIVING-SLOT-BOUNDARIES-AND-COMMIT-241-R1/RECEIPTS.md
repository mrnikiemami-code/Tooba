# TB-241-R1 RECEIPTS

## A — Commit-500 owning cause (prior primary log)

- TraceId `66fcc1116ad89db2014e4a4f6f6fb073` / CorrelationId `8907b5c98f02443f935b137bb8841284`
- Response body (`D:\temp\tb241-commit.json`): HTTP 500 `platform.unexpected` detail `ContractOperationException`
- Host exception: `storefront.captured_selection.intent_invalid` at
  `CheckoutDirectory.AcceptCapturedShippingCoreAsync` line 148 (`TryParseSelectionRevision` on null/blank)
- Wire defect: `CommitStorefrontShippingCommand` / handler passed `expectedSelectionRevision: null` always;
  `StorefrontShippingCommitRequest` lacked the field. Content-Length 136 on the failing call matched a body without revision.

## B — Focused tests

```
dotnet test …Order.Tests --filter FullyQualifiedName~StorefrontShippingReceivingCalendarTests
→ Passed! 24/24

dotnet test …Shipping.Tests --filter FullyQualifiedName~ShippingAuthoringRevisionSnapshotTests
→ Passed! 12/12
```

## C — Live primary HTTP (task-owned session)

Script: `D:\temp\tb241r1-flow.mjs` → `D:\temp\tb241r1-flow.json`

| Step | Status | Code / id |
|------|--------|-----------|
| register-host | 201 | userId 01a1848e-20c6-7000-96f6-d791dfe7f189 |
| login BFF | 200 | same userId |
| projection | 200 | selectionRevision "0", slots 09–10… |
| selection | 200 | selectionRevision "1" |
| commit missing revision | **400** | `storefront.captured_selection.intent_invalid` |
| commit with revision "1" | **200** | checkout 01a1848e-22ce-7000-8346-e2a0bf170206 |
| start-payment | **200** | payment 01a1848e-248c-7000-bd1d-33c876d5c480 Pending |
| start-payment same key | **200** | SAME checkoutId + paymentId |

DB (`order.order_shipping_groups` for that checkout):

| day | window | open_ticks | close_ticks | fmt |
|-----|--------|------------|-------------|-----|
| 2026-11-01 | 09:00-10:00 | 324000000000 | 360000000000 | 2 |
| 2026-11-01 | 09:00-10:00 | 324000000000 | 360000000000 | 2 |

## D — Primary runtime after swap

- Host 5088 PID 41232 `D:\temp\tb241-primary\Tooba.Host.exe` (staged from `D:\temp\tb241r1-build\out`, SHA256 prefix 99DF6473B65A3638)
- FE 3000 PID 18832 unchanged
- User Chrome PID 47688 preserved
- Code-SHA `03b24cc35a4307f220278bc9e79c6df79da4f6f0` on origin/main

## E — Not re-proven here

- Publication duration change then original Order GET immutability (accepted in 241 scope; not re-run in R1 box)
- FE production rebuild of `commitShippingToPayment` helper (source landed; Host owns the 500 class of defect)
