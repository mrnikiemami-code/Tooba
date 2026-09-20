# Feature boundary — TB-TMAR-FE-ADMIN-W2

Public: `features/admin-reviews/index.ts`

Guard updated: `frontend-feature-boundary.guard.test.ts` covers `admin-languages` + `admin-promotions` + `admin-reviews` (no deep external imports into `api/`/`components/`).

Route and consumers import through public boundary only.
