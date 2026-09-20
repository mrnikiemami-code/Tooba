# Migration — TB-TMAR-FE-ADMIN-W3

```
src/frontend/features/admin-sellers/
  index.ts
  api/sellers-api.ts
  components/sellers-screen.tsx
  admin-sellers.characterization.test.ts
```

Route `app/admin/sellers/page.tsx` → thin public boundary.
Removed sellers list types/functions from admin-api; screen/columns from admin-screens.
