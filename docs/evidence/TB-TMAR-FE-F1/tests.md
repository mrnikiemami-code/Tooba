# Tests — TB-TMAR-FE-F1

```text
npm run test:architecture          # 11/11 PASS
npm run test:admin-languages       # 10/10 PASS
npm run test:i18n                  # PASS
dotnet test …TmarSourceSize*       # 6/6 PASS
node scripts/run-discovered-tests.mjs --list  # 174 files
```

Canonical `npm test` now uses discovery. Pre-existing critical-storefront/typecheck debt unchanged (see legacy-failures.md).
