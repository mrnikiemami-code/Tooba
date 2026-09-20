# Migration — TB-TMAR-FE-ADMIN-W6

```
src/frontend/features/admin-dashboard/
  index.ts
  api/dashboard-api.ts
  components/dashboard-screen.tsx
  admin-dashboard.characterization.test.ts
```

Route `app/admin/page.tsx` → thin public boundary import.
Removed dashboard types/functions from `admin-api.ts` and screen from `admin-screens.tsx`.
Restored local import of shared admin-result actor constants (re-export-only left them unbound under Node ESM).
