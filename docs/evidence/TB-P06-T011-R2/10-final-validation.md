# 10 — Final validation (TB-P06-T011-R2)

## Frontend (`src/frontend`)

```
npm run typecheck  → pass
npm run lint       → pass (0 warnings/errors)
npm run test       → pass (incl. test:returns)
npm run build      → pass
```

## Backend (`src/backend`)

```
dotnet test Tooba.slnx → 236 passed, 0 failed, 0 skipped
Warnings during test with Host running: file lock (Host stopped for final count)
git diff --check → clean (evidence commit only)
```

## Capture script

```
node scripts/capture-t011-r2-visual-evidence.mjs → 15 PNG + motion-proof.json
```
