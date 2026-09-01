# TB-P08-T001-R1 — Focused Validation

## Backend
- `dotnet test --filter LanguageDirectoryPersistenceTests` → 5/5 pass
- `dotnet build Host -o artifacts/p08-t001-r1-build` → 0 errors
- `ContentFoundationTests` updated with `PermissiveLanguageDirectory` stub

## Frontend
- `node --test` admin-order-status-cards (4), admin-order-detail-visual (2), supported-locales (1) → 7/7 pass

## Recovery
- `node docs/ai/recovery-staleness.guard.test.mjs` → 3/3 pass
