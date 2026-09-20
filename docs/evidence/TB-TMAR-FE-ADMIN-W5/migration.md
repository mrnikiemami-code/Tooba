# Migration — TB-TMAR-FE-ADMIN-W5

```
src/frontend/features/admin-receipts/
  index.ts
  api/receipts-api.ts
  components/receipts-screen.tsx
  admin-receipts.characterization.test.ts
```

Route `app/admin/receipts/page.tsx` → thin public boundary.
Reservation projection helper localized in receipts-api (shared emptyReservationSummary).
