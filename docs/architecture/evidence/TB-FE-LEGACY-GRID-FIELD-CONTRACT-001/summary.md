# TB-FE-LEGACY-GRID-FIELD-CONTRACT-001 — AG Grid `field` contract (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY` · **Branch:** `main`
**BASE / expected starting HEAD:** `e41732c0077af49d33565811199bc8bd92387ca1` — preflight verified `HEAD == origin/main == Expected-Starting-HEAD`, no tracked modification, no divergence.

## 1. The single recorded diagnostic

`design-system/app-data-grid/legacy-grid-bridge.ts(71,7): error TS2322: Type 'string' is not assignable to type 'ColDefField<T, any>'.`

Cause, confirmed in the installed AG Grid 36.1.0 types (`node_modules/ag-grid-community/dist/types/src/entities/colDef.d.ts:227,250`):

```ts
export type ColDefField<TData = any, TValue = any> = TData extends any ? NestedFieldPaths<TData, TValue, []> : never;
// ...
field?: ColDefField<TData, TValue>;
```

`ColDefField<T>` is a **string-literal union of real property paths** of `T`, not `string`, so the bridge's plain `const field = col.id` string cannot be assigned. The bridge was writing a legacy **column id** into a slot that means "path into the row object".

## 2. STOP-GATE analysis — is `field` needed by AG Grid at runtime here?

Established from source, in this order:

| Concern | Evidence in this repository | Verdict |
| --- | --- | --- |
| **Column identity / state persistence** | `colId: col.id` is set on every data column (`legacy-grid-bridge.ts:69`). AG Grid's own contract comment on `ColDef.colId` states: *"The unique ID to give the column. This is optional. If missing, the ID will default to the field. If both field and colId are missing, a unique ID will be generated. **This ID is used to identify the column in the API for sorting, filtering etc.**"* (`colDef.d.ts:240-244`) | `field` **not** used — `colId` is explicit, so identity/state/`getColumnState()` keys are unaffected |
| **Sort state → query** | `AppDataGrid.onSortChanged` reads `event.api.getColumnState()` and maps `colState.colId` → `{ columnId: colState.colId, direction }` (`AppDataGrid.tsx:352-358`) | keyed by **colId**, not `field` |
| **Sorting order** | The bridge's `queryAdapter` is `createClientGridQueryAdapter(rows, columns)` → `executeGridQuery(rows, columns, query)`, i.e. the app's own engine sorts by the **legacy `accessor`**; AG Grid receives the already-sorted page as `rowData` | order comes from the engine via accessors |
| **Filtering** | Every data column ends up with `filter: false` because the bridge always calls `applyAppGridFilterHeader(...)`, which returns `{ ...colDef, filter: false }` (plus an app-owned `headerComponent` when a kind exists) — `app-grid-filter-header.ts:18-33`; filtering is app-owned via `headerComponentParams.externalFilter` + `gridContext.externalFilters`. The bridge's own `filterMatrix` / `advancedFilterColumns` / `externalFilterFields` keep using their **own** `field` string (`legacy-grid-bridge.ts:53-62`), which is *not* `ColDef.field`. `applyAppGridFilterHeader`'s only production caller is this bridge, and it always passes an explicit spec, so its internal `String(colDef.field ?? colDef.colId ?? "")` was already unused | `field` **not** used for filtering |
| **Export** | `exportCurrent` builds rows via the bridge's `getExportRow` (accessors) and calls `exportRowsToCsv/Xlsx` (`AppDataGrid.tsx:620-629`, `legacy-grid-bridge.ts:115-121`). No `api.exportDataAsCsv`, no quick filter, no aggregation anywhere in `design-system` (grepped) | `field` **not** used for export |
| **Data access / rendering** | Every data column has a custom `cellRenderer` → `renderLegacyCell(col, params)` → `col.cell(params.data)` else `col.accessor(params.data)` (`legacy-grid-bridge.ts:26-32,80`) | `field` **not** used for values |
| **Row identity** | `<AgGridReact getRowId={(params) => params.data.id}>` (`AppDataGrid.tsx:964`) | `field` **not** used |

### 2.1 The only residual role, and why removing it is not a behaviour change

`field` was also the AG Grid **value-service path**, and AG Grid's client-side row model does apply its own sort to `rowData`. That role was already *at most* redundant and mostly inert, and it is demonstrably not the source of visible order:

- For a large share of even the **sortable** columns the id is **not** a data path, so the value was already `undefined`: `customer` (`accessor` `row.customerDisplayName`), `lines` (`row.lineCount`), `amount` (`row.payableAmount`), `created` (`row.createdAt`), `slug` (`row.pathLabel`), `available`, `credits`, `debits`, `reserved` (`app/admin/admin-screens.tsx:246-311,489-526`, `landing-pages/admin-landing-pages-screen.tsx:316-321`).
- Where the id *is* a path (`title`, `name`, `code`, `status`, `updatedAt`, `createdAt`, `reference`, `displayOrder`, …) the accessor returns that very same property, so AG Grid's secondary sort compares the same values the engine already sorted by — a stable re-sort of an already-sorted page.

So removing `field` makes AG Grid's native sort uniformly value-less (all-missing comparisons preserve incoming order) while the app's engine — accessor-based and colId-keyed — remains the single source of row order. **The visible order is unchanged**, and the type contract stops encoding a false claim ("the column id is a path into the row").

### 2.2 Gate decision

`field` is **not needed** for identity, state persistence, filtering, export, data access or row identity in this bridge, and its removal does not change the visible sorting order for the reasons above. The repair therefore proceeds — **explicitly, not silently** — and the alternatives were rejected on record:

- `field: col.id as ColDefField<T>` — a cast that asserts a **false** invariant (many ids are not row paths); explicitly forbidden ("broad cast") and it would keep feeding AG Grid a wrong/undefined path.
- `valueGetter: (params) => col.accessor(params.data)` — type-sound, but it *changes* the value source from `data[colId]` to the accessor value for every column (enabling native sorting that is inert today), and it also disables AG Grid `cellDataType` inference (documented on `ColDef.cellDataType`: inference "will also not work if the `valueGetter`, `valueParser` or `refData` properties are defined"). Larger behaviour surface than this repair needs.
- **STOP / BLOCKED_ARCHITECT_DECISION** — not required, because the runtime semantics *were* safely established above without speculation.

**Honest residual disclosure:** AG Grid's runtime bundle (`dist/package/main.cjs.js`) could not be read in this environment (the minified dist is not searchable by the available tooling), so the *stability* of AG Grid's internal comparator for all-missing values is taken from its documented null/undefined-pair semantics rather than from byte-level source. Every other claim above is grounded in source actually read in this repository or in the installed `.d.ts`.

## 3. Exact change — one production file, one property removed

`src/frontend/design-system/app-data-grid/legacy-grid-bridge.ts`:

```diff
     const base: ColDef<T> = {
       colId: col.id,
-      field,
+      // بدون `field`: هویت ستون همان `colId` است و مقدار نمایشی از `cellRenderer`
+      // و accessor خوانده می‌شود. `field` فقط یک مسیر دادهٔ AG Grid بود که برای
+      // شناسه‌های غیرمسیر (مثل options/active/customer) به undefined می‌رسید؛ فیلتر
+      // نیز app-owned است (`filter: false`) و مرتب‌سازی خروجی از موتور کوئری
+      // (accessor + colId) می‌آید، پس حذف آن رفتار visible را عوض نمی‌کند.
       headerName: col.header,
```

- The **local** `const field = col.id` remains, because the bridge's own external-filter contract still needs it (`filterMatrix[field]`, `externalFilterFields: Object.keys(filterMatrix)`, and the `{ field, kind: "none" }` spec) — that is the bridge's **app-owned filter key**, deliberately distinct from `ColDef.field`.
- Nothing else changed: `colId`, `headerName`, `sortable`, `width`, `minWidth`, `maxWidth` pass-through, `hide`, `pinned`, `cellRenderer`, `applyAppGridFilterHeader`, the actions column and `getExportRow` are byte-identical. No cast, no `any`, no suppression, no shared generic optionalized, no other file touched.
- The focused test was **not** amended: `legacy-grid-bridge.test.ts` asserts exactly the surfaces this repair must preserve (`columnDefs.length`, `externalFilterFields`, `exportHeaders`, `headerComponent`, `getExportRow`, pinned/maxWidth/width/minWidth/uncapped behaviour) and it passes unmodified, which is stronger evidence than an edited assertion.

## 4. TypeScript — measured

`cd src/frontend && npx tsc --noEmit`, baseline copied from `docs/architecture/evidence/TB-FE-SHIPPING-SERVICES-GRID-ROW-IDENTITY-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **29** |
| After | **28** |
| Delta | **−1** |
| Introduced diagnostics | **0** |
| `Compare-Object` of both outputs | no added line |
| Diagnostics left in `legacy-grid-bridge.ts` | **none** |

Overall `typecheck` remains **not green (28 errors)**; no greenness is claimed. Remaining clusters (storefront-composition renderer, language-list, landing-page composer, etc.) require their own bounded tasks.

## 5. Focused test executed

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test design-system/app-data-grid/legacy-grid-bridge.test.ts` | **6/6 pass, 0 fail** |

**Pre-existing failures: none observed.** **Tests NOT run (deliberately):** full `npm test` / `test:discovered`, any build, backend suites, visual suites, browser/live verification, AMSC. No unrun test is reported as pass. In particular no visual/browser confirmation of the grid was performed — the sorting-order reasoning in §2.1 is analytic and source-grounded, not a live observation; if the Architect wants runtime proof, a follow-up visual check on an admin grid with a sortable non-path id (e.g. orders `amount`/`customer`) is the right next artifact.

## 6. Scope, risks, rollback

- **Scope:** production change is exactly `src/frontend/design-system/app-data-grid/legacy-grid-bridge.ts` (one property removed + comment); plus the receipt and this evidence. No backend/API/schema, no storefront/Home/PDP/Shopeiva, no other design-system file, no tsconfig/CI/baseline/guard, no other TypeScript cluster.
- **Risk:** low but non-zero, and disclosed above: AG Grid's native sort value for columns whose id happened to be a real path is no longer populated. Since the engine (accessor + colId) produced the order in both cases, no visible change is expected; the residual uncertainty is AG Grid's internal all-missing comparator stability, which the Architect may verify with a live grid check.
- **Rollback:** revert the single commit; re-adding `field` restores the previous (type-erroring) state.

## 7. Stop state

`automaticNextImplementationTask = NONE`. Awaiting independent Architect review / ACCEPT.
