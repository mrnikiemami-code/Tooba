# TB-P07-T043 — Focused validation

## Frontend
```text
node --test app/admin/admin-order-detail-visual.test.ts app/admin/admin-api.test.ts → 10/10 PASS
```

## Typecheck
```text
npm run typecheck → pre-existing repo errors outside T043 scope (category-admin-screen, wallet-ui, legacy-grid-bridge)
```

## Guards
```text
git diff --check → clean
node docs/ai/recovery-staleness.guard.test.mjs → PASS
```

## Smoke
```text
GET /v1/admin/orders/{paidCheckout} → lineCount + financialEvents present after Host :5088
```

## Data regression
- No changes to `admin-api.ts` enrichment/mapping logic
- T042-R1 fields (`lineCount`, `sellerFinancials`, `financialSummary`, `financialEvents`, provider labels) preserved
