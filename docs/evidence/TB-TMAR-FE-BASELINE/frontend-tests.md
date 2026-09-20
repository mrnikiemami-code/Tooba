# Frontend tests architecture — TB-TMAR-FE-BASELINE

## Runner

Node built-in test runner via `package.json` scripts (`node --experimental-strip-types --test …`).
Tests are **manually enumerated** in npm scripts (not glob discovery).

## Coverage areas (existing)

grid, workspace, category-tree, product-workspace, admin, seller, fulfillment, returns, settlement, content, stories, composition, i18n, customer, storefront, critical-storefront.

## Discovery assessment

Manual enumeration is incomplete relative to all `*.test.ts` files on disk risk — but many guards are explicitly listed.
**Repair in this task (low-risk):** add `test:architecture` script for source-size, import-boundary, test-discovery sanity, SEO guard — does not change product behavior.

## Architecture test layer (new)

- `lib/architecture/frontend-source-size.guard.test.ts`
- `lib/architecture/frontend-import-boundary.guard.test.ts`
- `lib/architecture/frontend-test-discovery.guard.test.ts`
- `lib/architecture/seo-rendering.guard.test.ts`

No broad snapshots.
