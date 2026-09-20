# Migration — TB-TMAR-FE-ADMIN-W1

```text
src/frontend/features/admin-promotions/
  index.ts
  api/promotions-api.ts
  components/promotions-screen.tsx
  admin-promotions.characterization.test.ts
app/admin/promotions/page.tsx  → public boundary
```

Removed promotions types/functions from `admin-api.ts`.
Removed `AdminPromotionsScreen` + columns from `admin-screens.tsx`.
Shared formatters (`formatAdminMoney`/`Date`/`Status`) still imported from `admin-api` (SHARED_TECHNICAL).
No duplicate live implementation left behind.
