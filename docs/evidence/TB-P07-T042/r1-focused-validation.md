# TB-P07-T042-R1 — Focused validation

## Frontend

```text
node --test app/admin/admin-api.test.ts
→ 8/8 PASS (includes legacy payload enrichment + provider labels)
```

## Backend

```text
dotnet build Host/Tooba.Host -c Debug → 0 Error(s)
dotnet test Host/Tooba.Host.Tests --filter AdminPanelCompositionTests → 5/5 PASS
dotnet test Host/Tooba.Host.Tests --filter AdminListGridQueryEngineTests → PASS
```

## Live API smoke (after Host restart)

```text
GET /v1/admin/orders/01a0453b-6829-7000-8c77-32cfb5f5d409
→ lineCount=1, sellerCount=1
→ sellerFinancials[1], financialEvents[1]
→ financialSummary.totalReceivedFromCustomer=381500
→ financialEvents[0].paymentMethod=کیف پول
```

## Preserved

- T042 layout/CSS baseline (`admin-order-detail-screen.tsx` structure unchanged)
- User-directed UI polish commit `9d350a9a` not reverted
