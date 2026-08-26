# 04 — Structural Test Proof

```bash
cd src/frontend
npm run test:critical-storefront
```

Suites:

- `app/storefront/home-structure.guard.test.ts`
- `app/storefront/storefront-home.test.ts` (existing mapper)
- `app/storefront/pdp-structure.guard.test.ts`

Wired into `package.json` as `test:home`, `test:pdp-guard`, `test:critical-storefront`, and included from `npm test`.
