# TB-FE-GRIDCOLUMNDEF-MAXWIDTH-OPTIONAL-001 — Bounded shared-contract repair evidence

**Mode:** `IMPLEMENT_BOUNDED_REPAIR` · **Branch:** `main`
**Starting reference SHA / BASE:** `3fda1a8c01b644a180c881abaaa4287fadefe5d2` (HEAD == `origin/main` at preflight)

## 1. Contract justification (established from real code, not assumed)

`GridColumnDef.maxWidth` was declared **required** while three independent facts show the runtime contract treats it as optional:

1. `design-system/app-data-grid/legacy-grid-bridge.ts:76` (pre-change) mapped `maxWidth: col.maxWidth` straight into AG Grid `ColDef.maxWidth`.
2. AG Grid **36.1.0** resolves it as `this.maxWidth = colDef.maxWidth ?? Number.MAX_SAFE_INTEGER` (`ag-grid-community/dist/package/main.esm.mjs:2081`) and then clamps with `Math.min(actualWidth, this.maxWidth)` (`:2409`). So an **absent key and an explicit `undefined` are byte-for-byte equivalent** at runtime, and a missing `maxWidth` means *no imposed maximum* — never a forced one.
3. The same bridge already guards the actions column with `actionsColumn.maxWidth != null` and the comment "بدون سقف اجباری", and `legacy-grid-bridge.test.ts` already asserted "leaves actions maxWidth open when not provided".

Additional confirmation requested by the task about `clampWidth` with `undefined`:

- `design-system/data-grid/serialize.ts:91` → `clampWidth(width, min, max) { return Math.min(max, Math.max(min, width)); }`.
- The internal `DataGrid` resize path (`design-system/data-grid/DataGrid.tsx:534`) called `clampWidth(initial + delta, column.minWidth, column.maxWidth)`. With the *runtime* value of an uncapped column (`undefined`) this evaluated `Math.min(undefined, …)` → **`NaN`**, which was stored into `layout.widths[id]` and applied as a CSS width. The falsely-required type was hiding a real latent NaN defect, so the repair had to cover the resize seam, not only the declaration.
- Conclusion: `maxWidth` is genuinely optional **and** the `undefined` cap had to be made explicit at the clamp seam. No product behaviour requires a mandatory cap.

## 2. Change set (exact, minimal)

| File | Change |
| --- | --- |
| `src/frontend/design-system/data-grid/types.ts` | `maxWidth: number;` → `maxWidth?: number;` with a doc comment stating that absence means no imposed maximum, matching AG Grid `ColDef.maxWidth`. `width`/`minWidth` remain required; all other semantics untouched. |
| `src/frontend/design-system/app-data-grid/legacy-grid-bridge.ts` | `maxWidth: col.maxWidth` → conditional spread `...(col.maxWidth != null ? { maxWidth: col.maxWidth } : {})`, matching the existing actions-column pattern: defined caps pass through, absent caps stay absent. |
| `src/frontend/design-system/data-grid/DataGrid.tsx` | Added module constant `UNCAPPED_COLUMN_MAX_WIDTH = Number.POSITIVE_INFINITY` and used `column.maxWidth ?? UNCAPPED_COLUMN_MAX_WIDTH` in the resize clamp, so an uncapped column resizes without a maximum and can never produce `NaN`. |
| `src/frontend/design-system/app-data-grid/legacy-grid-bridge.test.ts` | Added "passes through defined caps only" and "imposes no maximum for uncapped data columns"; existing actions capped/uncapped tests kept. |
| `src/frontend/design-system/data-grid/grid-state.test.ts` | **Extra file, enumerated and justified:** this is the existing, discovery-registered DataGrid width/clamp test seam (it already imported and asserted `clampWidth`) and it is included in `npm run test:grid`; one focused regression test was added for the uncapped clamp rule used by the resize path. No new test file, no parallel implementation. |

**No other file was modified.** Specifically untouched: backend, schema, localization, Admin screens, Home/PDP, `SectionDisplayConfig`, the other 11 typecheck clusters, the opt-in findings register, `tsconfig`, baselines and guards. No `any`, no `@ts-ignore`, no casts, no exclusions, no gate relaxation, no broad edit to the 95 screen column literals.

## 3. Measured outcome (not assumed)

`cd src/frontend && npx tsc --noEmit` (`node_modules/.bin/tsc`, TypeScript 5.9.3):

| | Errors |
| --- | --- |
| Before (BASE `3fda1a8c0`) | **155** |
| After | **56** |
| Removed | **99** |
| **Added (regressions)** | **0** |

Machine-computed diagnostic diff (`removed=99 added=0`, compared by file+line+code against the audit's raw baseline):

- Removed 99 = the 96-error `maxWidth` root plus 3 diagnostics that were masked by it (2 excess-property `TS2353` on half-migrated `flex` columns and 1 `TS2322` array-literal assignability error).
- Remaining 56 are exactly the other audit clusters, now unblocked: `shared-composition-renderer` 9, `category-admin-screen` 6, `settings/page` 5, `admin-create-shipment-modal` 4, `menus/admin-menus-api` 3, `shipping-services-screen` 3, `language-list` 3, and the smaller local clusters.
- Expected was "near 59"; the measured 56 is better because of the 3 unmasked diagnostics. No new error appeared, and no error moved to a different file.

## 4. Tests actually run

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test design-system/app-data-grid/legacy-grid-bridge.test.ts` | **6/6 pass, 0 fail** |
| `node --experimental-strip-types --test design-system/data-grid/grid-state.test.ts` | **9/9 pass, 0 fail** |
| `npm run test:grid` | **97 pass / 1 fail** (98 tests) — the single failure is pre-existing and unrelated (see below) |
| `npm run test:architecture` | **11/11 pass, 0 fail** |

**Pre-existing failure, proven not caused by this change:** `design-system/app-data-grid/column-manager.test.ts` → `test("page size options include 1000")` asserts `AppDataGrid.tsx` matches `/PAGE_SIZES = \[10, 25, 50, 100, 1000\]/`, but `AppDataGrid.tsx` contains `const PAGE_SIZES = [10, 20, 25, 50, 100, 1000];` **at HEAD** (`git show HEAD:…` confirms). Both the test file and `AppDataGrid.tsx` are unmodified by this task, so the assertion failure is deterministic and independent of the change; it belongs to the admin-grid canonical cluster, outside this task's permitted scope, and was not "fixed".

**Tests deliberately NOT run / not claimed:** `npm run test:critical-storefront`, `npm run test:discovered` (full suite), `npm run test:home`/`pdp-guard`/`listing-guard`, `npm run test:admin*`, `npm run build`, backend build/tests and any AMSC wave. **Overall `typecheck` is NOT green and is not declared green** (56 errors remain). No unrun test is reported as PASS.

**Critical-storefront scope note:** this change is confined to `design-system/**` grid internals and their focused tests. No Home/PDP/listing/storefront or shared-composition file was touched, no visual primitive was changed, and no column cap was added or removed for any existing screen — so the critical visual suite was not required. Capped columns keep identical caps; uncapped columns were already uncapped at runtime by AG Grid (`?? MAX_SAFE_INTEGER`), and the bridge's absent-vs-`undefined` key is provably equivalent in AG Grid 36.1.0.

## 5. Risk and rollback

- **Residual risk: low.** Contract-only plus one NaN-hardening line in the legacy internal grid. Zero visual delta for capped columns; zero runtime delta for AG Grid columns; the only behaviour change is that an uncapped legacy `DataGrid` column now resizes to the dragged width instead of `NaN`.
- **Rollback:** revert this single commit; it touches only `design-system` grid files and two focused test files, with no schema, migration or persisted-state change.

## 6. Stop state

`automaticNextImplementationTask = NONE`. Remaining 56 diagnostics are the other audit clusters and require their own bounded tasks (next recommended: the narrowing + admin-listing bug cluster). This task does not continue into them.
