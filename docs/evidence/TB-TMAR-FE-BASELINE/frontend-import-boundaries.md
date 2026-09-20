# Frontend import / layer boundaries — TB-TMAR-FE-BASELINE

## Target direction

`app(route) → features → design-system|lib`

## Observed debt

Automated relative/alias import edges: **1686**.
Suspicious shared→feature (`design-system|lib` → `app/admin|app/storefront`): **24** (mostly composition preview + design-system tests + appearance context).

## Guard

- Baseline: `frontend-import-boundary-baseline.json` (unique `from -> to` pairs shrink-only).
- Test: `lib/architecture/frontend-import-boundary.guard.test.ts`.
- New reverse edges fail; existing listed pairs allowed until deliberately removed.

## Cross-feature internals

Deep admin screen↔panel imports are currently normal inside flat `app/admin`. Future FE-BOUNDARY-002: new cross-feature imports require public feature boundary (no giant barrel redesign now).
