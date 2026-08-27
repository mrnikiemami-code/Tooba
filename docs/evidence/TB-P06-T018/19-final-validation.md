# 19 — Final validation (TB-P06-T018)

## Frontend

```text
npm run typecheck → pass
npm run lint      → No ESLint warnings or errors
npm run test:customer → pass 25 (includes panel-nav-integrity)
npm run test:seller   → pass 8 (includes panel-nav-integrity)
npm run build     → success
git diff --check  → clean (CRLF warnings only)
```

## Backend

No backend changes in Wave 1 (frontend-only nav/settings honesty).

## Browser proof

```text
node scripts/prove-t06-t018-panels.mjs → pass: true
captures 01–05 under docs/evidence/TB-P06-T018/captures/
_acceptance-proof.json
```

## Readiness claim

`COMMERCIAL_PANEL_WAVE1_LIVE` — not `PRODUCT_FULLY_READY`.
