# 14 — Customer API test updates (TB-P06-T007)

## Updated tests

| File | Change |
|---|---|
| `src/frontend/app/customer-panel/customer-api.test.ts` | Asserts `/api/customer/wishlist/product-intent` BFF path |
| `src/frontend/app/customer-panel/customer-address-api.test.ts` | Asserts `/api/customer/addresses` CRUD paths |

## Pattern

Tests mock `fetch` and verify:

- URLs target `/api/customer/*` (not direct Host origin)
- `credentials: "include"` on requests
- CSRF header present on mutating calls where applicable

## Run

```powershell
cd src/frontend
npm run test -- app/customer-panel/customer-api.test.ts app/customer-panel/customer-address-api.test.ts
```
