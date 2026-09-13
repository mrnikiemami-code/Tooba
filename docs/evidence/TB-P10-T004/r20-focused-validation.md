# R20 focused validation

Repairs (minimal):

1. Hide raw exception type names in `toCustomerCartMessage`.
2. Do not overlay payment-deadline localMessage on an active held+pay card.
3. `RetryUnpaidCoreAsync` only reopens payment when status is Expired (Failed + reservation retry no longer throws after reacquire).
4. FA reservation effective captions: بازنویسی‌شده / ارث از {sourceLabelFa}.
5. Host Development: `MigrateSchemaOnlyAsync` when legacy Catalog bootstraps are skipped.

Focused tests: PASS (24 frontend + 4 recovery guard).

- `src/frontend/app/storefront/storefront-pending-payment-api.test.ts`
- `src/frontend/app/admin/admin-reservation-cycle.test.ts`
- `src/frontend/app/admin/reservation-policy-admin.test.ts`
- `src/frontend/app/storefront/storefront-cart-api.test.ts` (exception copy)
- `docs/ai/recovery-staleness.guard.test.mjs`
- R19 Host gate tests not required (no reservation contract change beyond Host retry reopen guard).
