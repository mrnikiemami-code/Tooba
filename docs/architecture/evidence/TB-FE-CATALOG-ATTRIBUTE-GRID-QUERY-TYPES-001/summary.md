# TB-FE-CATALOG-ATTRIBUTE-GRID-QUERY-TYPES-001 — Grid query parameter types (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY` · **Branch:** `main`
**BASE / expected starting HEAD:** `be5492c617709ceb690aff6a8424d9e12f58dd10` — preflight verified `HEAD == origin/main == Expected-Starting-HEAD`, no tracked modification, no divergence.

## 1. The two recorded diagnostics

| Baseline | Diagnostic | Site |
| --- | --- | --- |
| line 237 | `TS7006: Parameter 'query' implicitly has an 'any' type.` | `definitionQueryAdapter` callback |
| line 833 | `TS7006: Parameter 'query' implicitly has an 'any' type.` | `schemaQueryAdapter` callback |

Both are the same defect: the parameter of a `useCallback`-wrapped adapter is unannotated, and `useCallback` cannot infer it from the consumer, because the contextual type lives on `useLegacyAdminGridDirectProps(...).queryAdapter` **one expression later**, not on the callback itself. So the parameter silently degraded to `any` and the adapter stopped being type-checked against the grid contract at all.

## 2. Canonical ownership verified before choosing the type (no new type invented)

- `GridServerQuery` is declared once, at `src/frontend/design-system/data-grid/types.ts:73` (`page`, `pageSize`, `sorts`, `filters`, optional `search`, `advancedFilter`) — grepping the repository shows a single declaration; every other hit is an import from these `./types` modules.
- It is re-exported by the design-system barrel at `src/frontend/design-system/data-grid/index.ts:13`, which is the module this file **already** imports `GridColumnDef` from.
- `createClientGridQueryAdapter<T>(rows, columns)` returns `async (query: GridServerQuery) => executeGridQuery(rows, columns, query)` (`design-system/app-data-grid/legacy-grid-adapters.ts:10-15`).
- `LegacyAdminGridDirectProps<T>.queryAdapter` is declared `(query: GridServerQuery) => Promise<{ rows: T[]; total: number }>` (`legacy-grid-adapters.ts:21`), and `DataGridProps.onServerExport` / the runtime `useState<GridServerQuery>` use the same type (`data-grid/DataGrid.tsx:51,73`).

So `GridServerQuery` is unambiguously the canonical UI-side query contract, and it is already reachable through the file's existing type-only barrel import. **No duplicate grid type was created, and no `design-system` file was touched.**

The `any` was also actively hiding the adapter from the grid contract: annotating it now type-checks the identity adapter against `(query: GridServerQuery) => Promise<GridServerPage<T>>` and passes.

## 3. Exact change — one production file, three lines

`src/frontend/app/admin/catalog-attribute-ui.tsx`:

```diff
-import type { GridColumnDef } from "../../design-system/data-grid";
+import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";

   const definitionQueryAdapter = useCallback(
-    async (query) => createClientGridQueryAdapter(definitionGridRows, definitionColumns)(query),
+    async (query: GridServerQuery) => createClientGridQueryAdapter(definitionGridRows, definitionColumns)(query),
     [definitionGridRows, definitionColumns],
   );

   const schemaQueryAdapter = useCallback(
-    async (query) => createClientGridQueryAdapter(schemaGridRows, schemaColumns)(query),
+    async (query: GridServerQuery) => createClientGridQueryAdapter(schemaGridRows, schemaColumns)(query),
     [schemaGridRows, schemaColumns],
   );
```

- **Type-only** import extended on the existing statement (`import type`, so no runtime module edge is added and no bundling changes); no new import statement, no new dependency.
- **No cast, no `any`, no `@ts-ignore`, no suppression** — the annotation is the real contract.
- **Zero runtime delta:** annotations are erased at compile time. The callbacks, their dependency arrays, `createClientGridQueryAdapter`, `useLegacyAdminGridDirectProps`, `AppDataGrid`, the column definitions, widths and every search/sort/filter code path are byte-identical at runtime.

## 4. TypeScript — measured, one check

`cd src/frontend && npx tsc --noEmit` against the committed baseline copied from `docs/architecture/evidence/TB-FE-SHIPMENT-METADATA-OPTIONALS-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **34** |
| After | **32** |
| Delta | **−2** (expected `34 → 32`) |
| New diagnostics | **0** |
| `Compare-Object` of both outputs | no added line |
| Diagnostics left in `app/admin/catalog-attribute-ui.tsx` | **none** |

Exactly the two TS7006 diagnostics disappeared. Overall `typecheck` remains **not green (32 errors)** and no greenness is claimed.

## 5. Focused tests executed

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test app/admin/admin-grid-migration.test.ts app/admin/variant-axis-capability.test.ts` | **11/11 pass, 0 fail** |

`admin-grid-migration.test.ts` is the nearest existing focused catalog-attribute/grid test: it reads `catalog-attribute-ui.tsx` and asserts the direct-`AppDataGrid` wiring — `admin-attribute-definitions-grid`, `admin-category-schema-grid`, `createClientGridQueryAdapter` present and `LegacyAppDataGrid` absent — which is exactly the surface this task is forbidden from disturbing. `variant-axis-capability.test.ts` reads the same file and passes unmodified. No test file was amended, and no new test was added, because the change is a pure annotation with no behaviour to regress.

**Honest caveat:** both tests are source-contract assertions, and the existing suite has no runtime harness for these two adapters (the local `useCallback` wrappers are component-internal). The authoritative verification for this task is therefore the compiler comparison in §4, not the focused tests.

**Pre-existing failures:** none observed in these files. **Tests NOT run:** the full `npm test` / `test:discovered`, `test:admin*` and `test:critical-storefront` suites, any build, backend suite, browser/live check, and any AMSC wave. No unrun test is reported as pass.

## 6. Scope, risks, rollback

- **Scope:** production change is exactly `src/frontend/app/admin/catalog-attribute-ui.tsx`. No grid implementation, adapter, column, visual, search/sort/filter semantic, backend/Host/schema file, Home/PDP/Shopeiva surface, `tsconfig`, baseline, guard or unrelated cluster was touched, and no test file changed.
- **Residual risk: none observable.** Three erased type annotations in one file; runtime behaviour is provably unchanged.
- **Rollback:** revert the single commit.

## 7. Stop state

`automaticNextImplementationTask = NONE`. 32 frontend diagnostics remain in the other audited clusters and require their own bounded tasks.
