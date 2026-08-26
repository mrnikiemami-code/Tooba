# 13 — CSRF unit tests (TB-P06-T007)

## Test file

`src/frontend/lib/auth/csrf.test.ts`

## Cases

| Test | Assertion |
|---|---|
| `csrf validation accepts matching header and cookie` | `validateCsrf` returns true |
| `csrf validation rejects missing header` | `validateCsrf` returns false |

## Run

```powershell
cd src/frontend
npm run test -- lib/auth/csrf.test.ts
```

## Result (Worker PASS)

6 tests pass (includes other frontend unit tests in default npm test scope); csrf tests 2/2 pass.
