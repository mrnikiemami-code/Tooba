# TB-FE-LEGACY-GRID-FIELD-CONTRACT-001-R1 — behavioral verification of the `field` removal

**Mode:** `BOUNDED_BEHAVIORAL_VERIFICATION_WITH_STOP_GATE` · **Parent:** `TB-FE-LEGACY-GRID-FIELD-CONTRACT-001`
**Branch:** `main` · **Expected-Starting-HEAD:** `f9973b04102f9e854460fada8f2fedc5be170287` — verified `HEAD == origin/main == Expected-Starting-HEAD`.
**Production code changed in this task: NONE.** This task only verifies, and records, whether the parent change alters sorting.

## 1. Runtime seam actually used (not a synthetic mock)

`runtime-verification.ts` in this evidence folder combines the **repository's own code** with a **real AG Grid** running headless:

- `buildLegacyGridBridge` — the real bridge from `src/frontend/design-system/app-data-grid/legacy-grid-bridge.ts`;
- `executeGridQuery` — the real query engine from `src/frontend/design-system/data-grid/query-engine.ts`, i.e. exactly what `createClientGridQueryAdapter(rows, columns)` wraps (`design-system/app-data-grid/legacy-grid-adapters.ts:10-14`), which is the adapter the Landing Pages admin grid uses (`app/admin/landing-pages/admin-landing-pages-screen.tsx:415-418`);
- `ag-grid-community@36.1.0` `createGrid` + `ModuleRegistry.registerModules([AllCommunityModule])` inside **jsdom** — real AG Grid sorting, no stubs;
- the "before" variant mirrors the removed line exactly: `{ ...colDef, field: colDef.colId }` for every non-actions column;
- the sequence follows `AppDataGrid`'s real flow: the header click sets AG Grid sort state (`applyColumnState`), the app re-queries (`executeGridQuery`), and the new page is written back with `setGridOption("rowData", ...)`; the visible order is then read with `api.forEachNodeAfterFilterAndSort`.

Run from the repository root:

```text
node --experimental-strip-types docs/architecture/evidence/TB-FE-LEGACY-GRID-FIELD-CONTRACT-001-R1/runtime-verification.ts
```

Raw output is committed as `runtime-verification-output.txt`. No browser was used, so no already-running local service was touched, restarted or terminated.

## 2. Representative before/after ordered IDs and sort keys

| Case | Sort | Engine order (query key) | Visible with `field` (before) | Visible without `field` (after) | Delta |
| --- | --- | --- | --- | --- | --- |
| **A** id is **not** a row property (`customer` → `customerDisplayName`, `amount` → `payableAmount`; the orders-like shape named by the Architect) | `customer:asc` | `o2,o3,o1` | `o2,o3,o1` = engine | `o2,o3,o1` = engine | **NONE** |
| **A** | `amount:desc` | `o1,o3,o2` | `o1,o3,o2` = engine | `o1,o3,o2` = engine | **NONE** |
| **B** id **is** a real row property and the accessor returns it (`reference`) | `reference:asc` | `o2,o3,o1` | `o2,o3,o1` = engine | `o2,o3,o1` = engine | **NONE** |
| **B** | `reference:desc` | `o1,o3,o2` | `o1,o3,o2` = engine | `o1,o3,o2` = engine | **NONE** |
| **C** id **is** a real row property but the accessor returns a *different* value (landing pages `slug` → `pathLabel`, where Home's label is `""`) | `slug:asc` | `h,a,z` | `a,h,z` — **DIFFERS from engine** | `h,a,z` = engine | **PRESENT** |
| **D** `row.<id> ?? ""` accessor with a `null` value | `reference:asc` | `o2,o3,o1` | `o2,o3,o1` = engine | `o2,o3,o1` = engine | **NONE** |

Sort keys recorded in the same run: the AG Grid sort state is `customer:asc`, `amount:desc`, `reference:asc|desc`, `slug:asc` in **both** variants, i.e. the query key (`colState.colId` → `GridSort.columnId`, `AppDataGrid.tsx:352-358`) and the sort indicator are unchanged by this repair. What changed is only whether AG Grid applied an additional local sort on top of the engine result.

**Case A directly confirms the parent task's claim:** for a column whose id is not a row property, AG Grid's sort was already a no-op before the repair (identical visible order), because the removed `field` resolved to `undefined`. **Case B confirms the second claim:** where the id *is* the property the accessor returns, the old AG Grid second sort compared exactly the same values the engine had already sorted by, so it reproduced the engine order rather than fighting it.

## 3. The one real delta (Case C) — exhaustively scoped

`column-accessor-parity-scan.txt` (raw output of a read-only scan over every `GridColumnDef` literal in `app/`, `features/` and `design-system/`):

- **173** legacy columns scanned.
- **111** have `accessor` equal to `row.<id>` (or `row.<id> ?? ""`) → Case B/D shapes → no delta.
- **62** have an `accessor` that differs from `row.<id>`; of those, **15** are `sortable: true`.
- Of those 15, **14** have ids that are **not** row properties (`customer`, `supply`, `reservation`, `seller`, `available`, `credits`, `debits`, `reserved`, `created`, receipts `reference`/`customer`/`supply`/`reservation`/`created`) → Case A shape → no delta, the old AG Grid sort was already inert.
- Exactly **1** sortable column has an id that *is* a real row property while the accessor returns something else: **`slug` on the Landing Pages admin grid** (`id: "slug"`, `accessor: (row) => row.pathLabel`, `AdminLandingPage` really does declare `slug`, and `publicPathForStorePage` returns `""` for Home and `/landing/${slug}` otherwise — `app/admin/landing-pages/admin-landing-pages-api.ts:423-426`).

**Magnitude of the delta on that one grid:** for every non-Home row the label is `/landing/` + slug, i.e. a constant prefix, so ordering by label is *identical* to ordering by slug; the only row that can move is the single Home row, whose displayed address is empty (`""`). Before: Home was placed by its hidden `slug` in the middle of a list whose visible value is blank. After: Home is placed by the value it actually displays (empty → first ascending). The sort indicator and the engine query are unchanged in both cases.

**Is that a regression?** No. The delta moves the visible order *toward* the application's own sort contract (colId → engine sort by accessor) and toward what the column displays, and it removes a state where AG Grid silently overrode the engine with a key the user cannot see. Reverting the parent change would restore that hidden-key override *and* reintroduce TS2322.

**Can the old hidden-key order be reproduced type-safely, as the gate requires if a correction is warranted?** No. Reproducing it requires either writing a legacy id string into `ColDefField<T>` (a cast asserting the false invariant that every id is a row path — explicitly forbidden), or a dynamic `valueGetter` reading `data[id]`, which needs an index-signature cast. Every type-sound alternative (`valueGetter` from the accessor, per-column `field: "…"` literals) either changes the value source for all columns or is a broad redesign outside the allowed scope. Therefore the gate's "regression proven → smallest architecture-safe correction" branch has no admissible correction, and the "equivalence established → no production change, PASS" branch applies to the task's specified cases (A and B), with Case C reported as a bounded, documented, directionally-correct delta rather than hidden.

## 4. Validation run

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test design-system/app-data-grid/legacy-grid-bridge.test.ts` | **6/6 pass, 0 fail** |
| `node --experimental-strip-types --test design-system/data-grid/grid-state.test.ts` | **9/9 pass, 0 fail** |
| `tsc --noEmit` | **not run — no production code changed in this task** (baseline remains 28 errors from the parent task) |

**Pre-existing failures: none observed** in the two focused suites. **Not run (deliberately):** the full `npm test` / `test:discovered` suites, any build, backend suites, visual suites, browser/live verification, AMSC, and any `tsc` run (no production change justified one). No unrun test is reported as pass.

## 5. Limitations (explicit)

1. Verification is against a **headless real AG Grid in jsdom**, not a browser. jsdom cannot reproduce real pointer interaction with a header; the sort was applied through `applyColumnState`, which is the same grid state a header click produces, and the row order was read from the grid's own sorted row model.
2. The engine used is the **client** adapter (`createClientGridQueryAdapter` → `executeGridQuery`), which is exactly what the Landing Pages grid uses (the Case C site). For **server-executed** admin grids the engine key is whatever the Host returns for that `columnId`; the AG Grid-side change (no more local second sort) applies identically, so the class-A/class-B/class-D conclusions carry over, while a server column whose Host key differs from a same-named row property would behave like Case C.
3. The scan classifies accessors **syntactically** (regex over `id:` / `accessor:` pairs plus an "identifier present as a property declaration" check), not through the TypeScript type checker; it is an exhaustive *candidate* enumeration whose only "property-like" hit (`slug`) was then verified by reading `AdminLandingPage`, `publicPathForStorePage` and the screen's adapter wiring. The 14 non-property hits are consistent with the raw row shapes inspected in the parent task's evidence and with the runtime result of Case A.

## 6. Stop state

`Automatic-Next-Implementation-Task: NONE`. Verification PASS; production code untouched; the decision on the one documented delta belongs to the Architect.
