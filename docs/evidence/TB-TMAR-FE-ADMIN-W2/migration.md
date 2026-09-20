# Migration — TB-TMAR-FE-ADMIN-W2

```
src/frontend/features/admin-reviews/
  index.ts
  api/reviews-api.ts
  components/reviews-screen.tsx
  admin-reviews.characterization.test.ts
```

Route `app/admin/reviews/page.tsx` → thin import of `AdminReviewsScreen`.

Removed reviews types/functions from `admin-api.ts`.
Removed `AdminReviewsScreen` + `reviewColumns` from `admin-screens.tsx`.

Shared formatters (`formatAdminDate`/`Status`) still imported from `admin-api` (SHARED_TECHNICAL).
