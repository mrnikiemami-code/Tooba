# TB-FE-SHIPPING-SERVICES-GRID-ROW-IDENTITY-001 — Shipping services grid row identity (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY` · **Branch:** `main`
**BASE / expected starting HEAD:** `3786aee814df6d06175e6a418e3746664b3faa96` — preflight verified `HEAD == origin/main == Expected-Starting-HEAD`, no tracked modification, no divergence.

## 1. The three recorded diagnostics

| Baseline | Diagnostic |
| --- | --- |
| line 306 | `TS2322: Type 'GridColumnDef<ShippingServiceListItem>[]' is not assignable to type 'GridColumnDef<{ id: string; }>[]'.` |
| line 307 | `TS2322: Type '(query: GridServerQuery) => Promise<GridServerPage<ShippingServiceListItem>>' is not assignable to type '(query: GridServerQuery) => Promise<{ rows: { id: string; }[]; total: number; }>'.` |
| line 345 | `TS2344: Type 'ShippingServiceListItem' does not satisfy the constraint '{ id: string; }'.` |

All three are one contract mismatch. `ShippingServiceListItem` (`shipping-services-api.ts:45-56`) exposes `shippingServiceId`, but the direct-grid contract requires a stable `id`:

- `LegacyAdminGridDirectProps<T extends { id: string }>` — `columns: GridColumnDef<T>[]`, `queryAdapter: (query: GridServerQuery) => Promise<{ rows: T[]; total: number }>` (`design-system/app-data-grid/legacy-grid-adapters.ts:17-25`).
- `useLegacyAdminGridDirectProps<T extends { id: string }>` (`legacy-grid-adapters.ts:29`).
- `<AppDataGrid<ShippingServiceListItem>>` (`shipping-services-screen.tsx:345`).

The screen was the only one of the direct-`AppDataGrid` admin screens that had not bridged its API id onto the grid's required stable `id`.

## 2. Canonical identity established from the repository, not invented

- `ShippingServiceListItem.shippingServiceId` is the canonical, server-issued identity: it is the value the screen already passes to `loadAdminShippingService(serviceId)` in `openEdit` and to `deactivateAdminShippingService(serviceId)` in `deactivate` (`shipping-services-screen.tsx:168,221`), and it is the field the `actions` column already reads as its accessor.
- The repository's **established pattern** for exactly this situation is already in production, in `app/admin/campaigns/admin-campaigns-screen.tsx:31-33,103-108`:

  ```ts
  type CampaignGridRow = AdminCampaignListItem & { id: string };
  // ...
  list.data.items.map((item) => ({ ...item, id: item.campaignId }))
  ```

  i.e. a **typed view-row projection** at the data seam, with `id` bound to the canonical API id — and the API/DTO field name left untouched. No shared grid contract needed changing and no product decision was required, so this was **not** a STOP case.

**Rejected alternatives, with reasons:** renaming or adding `id` to the DTO/API type would rewrite the canonical wire identity (forbidden); optionalizing or weakening `{ id: string }` would weaken the "global generic" (forbidden); a random/index/positional id would be a fake identity that breaks selection, saved views and export (forbidden).

## 3. Row-id semantics traced before editing (why a real id is required)

`id` is consumed by `useLegacyAdminGridDirectProps` → `buildLegacyGridBridge`/`AppDataGrid` as the row key for row selection, saved views/saved-view diffs (`design-system/app-data-grid/saved-view-*.ts`), and export (`getExportRow`). A duplicate or positional id would therefore corrupt saved views and selection — which is exactly why the identity had to be the real `shippingServiceId` rather than a synthesized value.

## 4. Exact change — production file only, canonical projection

`src/frontend/app/admin/shipping-services-screen.tsx`:

```diff
+type ShippingServiceGridRow = ShippingServiceListItem & { id: string };
+
 export function ShippingServicesScreen() {
-  const [rows, setRows] = useState<ShippingServiceListItem[]>([]);
+  const [rows, setRows] = useState<ShippingServiceGridRow[]>([]);
@@ refresh()
-    setRows(listResult.data);
+    setRows(listResult.data.map((item) => ({ ...item, id: item.shippingServiceId })));
@@ columns
-  const columns = useMemo<GridColumnDef<ShippingServiceListItem>[]>(() => [
+  const columns = useMemo<GridColumnDef<ShippingServiceGridRow>[]>(() => [
@@ grid
-            <AppDataGrid<ShippingServiceListItem> {...gridProps} />
+            <AppDataGrid<ShippingServiceGridRow> {...gridProps} />
```

- **Column definitions, query rows and `<AppDataGrid>` now all use the same row type** `ShippingServiceGridRow`, as the task requires.
- Because the view row **extends** `ShippingServiceListItem`, every existing accessor (`row.code`, `row.name`, `row.optionCount`, `row.activeOptionCount`, `row.isActive`, `row.iconKey`, `row.colorKey`) and every row action (`openEdit(row.shippingServiceId)`, `deactivate(row.shippingServiceId)`) is unchanged — **edit/deactivate still target `shippingServiceId`**, not `id`.
- **No DTO/API change**, no rename, no fake/random/positional id, no `any`, no `as unknown as`, no `@ts-ignore`, no non-null mask, no tsconfig/guard/baseline change, no grid refactor. `rows.at(-1)?.sortOrder` and the `GRID_VIEW_KEY = "admin-shipping-services"` saved-view key are untouched, so saved-view identity and semantics are preserved.

## 5. TypeScript — measured

`cd src/frontend && npx tsc --noEmit`, baseline copied from `docs/architecture/evidence/TB-FE-CATALOG-ATTRIBUTE-GRID-QUERY-TYPES-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **32** |
| After (final tree, including the test amendment) | **29** |
| Delta | **−3** (expected `32 → 29`) |
| New diagnostics | **0** |
| `Compare-Object` of both outputs | no added line |
| Diagnostics left in `shipping-services-screen.tsx` | **none** |

Exactly the three targeted diagnostics disappeared. Overall `typecheck` remains **not green (29 errors)** and no greenness is claimed. For the Architect's next steps the remaining 29 are concentrated in `lib/storefront-composition/shared-composition-renderer.tsx` (9 across four codes), `app/admin/landing-pages/admin-landing-page-composer.tsx` (2), `features/admin-languages/components/language-list.tsx` (3) and one each in ~14 other files.

## 6. Focused test — one command, minimal in-pattern amendment

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test app/admin/admin-grid-migration.test.ts` | **7/7 pass, 0 fail** |

`app/admin/admin-grid-migration.test.ts` is the repository's existing direct-`AppDataGrid` migration guard: it already asserts, for catalog attributes, gift cards, stories and `admin-screens`, that the screen uses `AppDataGrid` + `createClientGridQueryAdapter` and does **not** use `LegacyAppDataGrid`. It is therefore the one directly relevant existing focused grid test, and this screen was conspicuous by its absence from it.

It was **minimally amended** (one `readFileSync` plus one test, in the file's own established style) rather than adding a new file, because this task's whole content is "this screen must satisfy the direct-grid contract":

```ts
const shippingServices = fs.readFileSync(path.join(root, "app/admin/shipping-services-screen.tsx"), "utf8");

test("shipping services list keeps its stable row identity and direct grid wiring", () => {
  assert.match(shippingServices, /AppDataGrid<ShippingServiceGridRow>/);
  assert.match(shippingServices, /createClientGridQueryAdapter/);
  assert.doesNotMatch(shippingServices, /LegacyAppDataGrid/);
  // هویت ردیف گرید باید روی shippingServiceId سوار شود، نه یک id ساختگی.
  assert.match(shippingServices, /type ShippingServiceGridRow = ShippingServiceListItem & \{ id: string \}/);
  assert.match(shippingServices, /id: item\.shippingServiceId/);
});
```

**Honest limitation, per the task's own preference for genuine checks over ceremonial regex:** two of the five assertions (`id: item.shippingServiceId` and the row-type declaration) are the *genuine* identity binding and are what a fake/positional id would break; the remaining three are wiring assertions in the file's existing style. The **authoritative** identity verification for this task is nevertheless the compiler comparison in §5 — the type system is what proves `columns`, the query adapter's rows and `<AppDataGrid>` now share one row type whose `id` is the canonical `shippingServiceId`, and TypeScript would reject a positional or missing id. There is no component-level runtime harness for this screen (the projection is a component-internal state mapping), so no stronger behavioural test was available without exporting a production seam, which is out of scope.

**Tests NOT run:** the full `npm test` / `test:discovered`, any build, backend suites, visual suites, browser/live check, AMSC. **Pre-existing failures: none observed** in the test command run.

## 7. Scope, risks, rollback

- **Scope:** production change is exactly `src/frontend/app/admin/shipping-services-screen.tsx` (one type alias, three type substitutions, one id projection); plus the minimal amendment to the existing focused guard test, the receipt and this evidence. No backend/schema, no DTO, no shared grid contract, no Shopeiva/Home/PDP, no tsconfig/baseline/guard weakening.
- **Residual risk: low.** The projection adds one own-property `id` per row and is now also present in the object passed to the grid; the mapping is total (`shippingServiceId` is non-optional on the API item, so `id` is always a non-empty string) and stable across refreshes, and all column accessors and row actions still read the API fields they read before.
- **Rollback:** revert the single commit; the change is five lines plus one test hunk.

## 8. Stop state

`automaticNextImplementationTask = NONE`. Awaiting independent Architect review; 29 frontend diagnostics remain in other audited clusters and require their own bounded tasks.
