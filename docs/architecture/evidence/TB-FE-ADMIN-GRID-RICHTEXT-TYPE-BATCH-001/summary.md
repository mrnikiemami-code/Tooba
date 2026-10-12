# TB-FE-ADMIN-GRID-RICHTEXT-TYPE-BATCH-001 — two independent Admin clusters (evidence)

**Mode:** `IMPLEMENT_BOUNDED_MULTI_ERROR_REPAIR_WITH_INDEPENDENT_STOP_GATES` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Base SHA:** `286489b74fba6520299f9dbb66d81bec2935e6ab` — verified `HEAD == origin/main == Expected-Starting-HEAD`, clean tracked worktree, no stash/reset/rebase/force-push.
**Gate verdicts:** Scope A **FIXED** (all three named properties demonstrably inert, plus one masked sibling — see §2.4) · Scope B **FIXED** via the library's own public type. Neither scope was BLOCKED; no `BLOCKED_ARCHITECT_DECISION` was required.

**Typecheck: 14 → 10, four diagnostic keys removed, zero added.** `tsc` still exits non-zero with 10 pre-existing errors in `shared-composition-renderer.tsx` (9) and `storefront-landing-blocks.tsx` (1) — typecheck green is **not** claimed.

---

## 1. Baseline (14)

```text
app/admin/product-rich-text-editor.tsx(162,9) TS2345   ← Scope B
app/storefront/storefront-landing-blocks.tsx(722,9) TS2740          (untouched)
features/admin-languages/components/language-list.tsx(41,102) TS2353  ← Scope A (flex)
features/admin-languages/components/language-list.tsx(42,111) TS2353  ← Scope A (flex)
features/admin-languages/components/language-list.tsx(112,7) TS2353   ← Scope A (pinned)
lib/storefront-composition/shared-composition-renderer.tsx … 9 errors (untouched)
```

Only the two permitted files were edited; the 10 storefront/composition diagnostics were deliberately not chased.

---

## 2. SCOPE A — `language-list.tsx`: the three named properties are dead legacy AG Grid fields

### 2.1 Decision gate: what does the file actually feed?

`AdminLanguagesScreen` builds `columns` and hands them to `useLegacyAdminGridDirectProps` (`:188`) and `createClientGridQueryAdapter` (`:183`). The only path to the grid is:

```text
useLegacyAdminGridDirectProps (legacy-grid-adapters.ts:39)
  → buildLegacyGridBridge(columns, direction)      // direction defaults to "rtl"
  → returns { columnDefs, advancedFilterColumns, externalFilterFields, getExportRow, exportHeaders }
```

`legacy-grid-adapters.ts:42-56` forwards **only** `bridge.columnDefs` and the filter/export helpers — the raw `GridColumnDef[]` array never reaches AG Grid. So a property is live only if `buildLegacyGridBridge` reads it.

### 2.2 `flex` — never read anywhere in the frontend

- `GridColumnDef` (`design-system/data-grid/types.ts:87-111`) declares exactly: `id, header, accessor, cell, sortable, filterKind, filterable, resizable, reorderable, hideable, sticky, align, width, minWidth, maxWidth, exportable, enumOptions, defaultVisible`. **No `flex`.**
- `buildLegacyGridBridge` (`legacy-grid-bridge.ts:68-92`) constructs each `ColDef<T>` from an explicit allow-list — `colId, headerName, sortable, width, minWidth, maxWidth (only when != null), hide, pinned (from `sticky`), cellRenderer` — plus the filter header. **No `flex`, and no spread of the input**, so nothing can leak through.
- A repo-wide search for a reader of a column's `flex` (`.flex`, `col.flex`, `column.flex`, `flex ??`) across `design-system`, `app` and `lib` returns **zero matches**. `flex` appears only as Tailwind/CSS class text and as `ColDef.flex`-era leftovers.

So `flex: 1.2` / `flex: 1.4` never influenced AG Grid: the columns always rendered at their explicit `width`/`minWidth` (140/120 and 160/140), which remain untouched.

### 2.3 `pinned` — the actions column is pinned by the bridge, direction-aware, unconditionally

`buildLegacyGridBridge` strips `id === "actions"` out of the data columns (`:49-50`) and appends its own hard-coded definitions (`:96-112`):

```ts
const actionsPin = pinnedGridEdge(direction);
columnDefs.push({
  colId: "actions",
  …
  sortable: false,
  filter: false,
  resizable: true,
  lockVisible: true,
  lockPinned: true,
  lockPosition: actionsPin,
  pinned: actionsPin,
  …
});
```

- `mapPinned()` — the only consumer of a pinning declaration — is applied **exclusively to data columns** (`:85`), so `sticky`/`pinned` on the actions def is never read.
- `pinnedGridEdge("rtl")` = `"left"`, `pinnedGridEdge("ltr")` = `"right"` (`grid-direction.ts`).

**Pinning intent is retained and, in fact, corrected.** The literal `pinned: "left"` agrees with the RTL result but would have been wrong under LTR, where the bridge pins `"right"`. The canonical, direction-aware behaviour is the bridge's, and it is untouched.

### 2.4 A masked fourth property: `filter`

TypeScript reports only the **first** excess property per object literal. Removing `pinned` therefore surfaced `filter: false` at `:120` as the next TS2353 on the same actions literal. `filter` is not part of `GridColumnDef` either, and the bridge never reads it — it **hard-codes `filter: false` for the actions column itself** (`legacy-grid-bridge.ts:105`). Leaving it would merely have swapped one diagnostic for another, so it was removed with the same class of evidence. This is the only item beyond the three the task named; it is the same object literal, the same file and the same dead-legacy class.

### 2.5 Runtime probe (evidence, not inspection)

`probe-inert-legacy-fields.ts` builds the bridge twice from the pre-repair and post-repair column shapes, for both directions, and asserts:

```text
[rtl] … {"colId":"actions", … ,"lockPinned":true,"lockPosition":"left","pinned":"left", … }
[ltr] … {"colId":"actions", … ,"lockPinned":true,"lockPosition":"right","pinned":"right", … }
PROBE OK — flex/pinned/filter are inert; pinning, filtering and widths unchanged.
```

- The produced `columnDefs` are **structurally identical** with and without `flex`/`pinned`/`filter` (compared as serialized projections; only the per-call `cellRenderer` closure differs, which is why a raw `deepStrictEqual` false-positives and was replaced).
- No produced `ColDef` carries a `flex` key.
- Actions `pinned`/`lockPosition` follow the direction (`left` for `rtl`, `right` for `ltr`); `filter` is `false`; widths stay `140/120`, `110/100`, `88/72/96`.

**Before/after behaviour:** identical. **Before:** three (four) properties were silently discarded by the bridge. **After:** the same `ColDef`s are produced from a contract-valid input. No visible layout regression; `data-testid`/actions access/`onEdit` wiring untouched.

---

## 3. SCOPE B — `product-rich-text-editor.tsx`: use the library's own `Extensions` type

### 3.1 Decision gate: the error is an inference artefact, not a version mismatch

The array literal was left un-annotated, so TypeScript inferred its element type as a **union of the concrete extension types** (`Extension<any,any> | Extension<StarterKitOptions,any> | Mark<UnderlineOptions,any> | Mark<TextStyleOptions,any> | … 5 more …`). `Image.configure(...)` returns `Node<ImageOptions, any>`, and a `Node` is not assignable to any member of that union (the sibling classes are only related through their common base), hence TS2345 at `:162`.

The installed `@tiptap/core@3.30.5` exports exactly the right type (`node_modules/@tiptap/core/dist/index.d.ts`):

```ts
1387: type AnyExtension = Extendable;
1388: type Extensions = AnyExtension[];

 504: declare class Mark<…>      extends Extendable<…> { … }
 802: declare class Node<…>      extends Extendable<…> { … }
1325: declare class Extension<…> extends Extendable<…> { … }

1644:   extensions: Extensions;      // inside EditorOptions — what useEditor consumes
```

`Extensions` is in the package's public export list. So `Node<ImageOptions, any>` **is** assignable to `Extensions`, and the declared array type is simultaneously the type `useEditor({ extensions })` expects. No cast, no `any`, no `unknown-as`, no suppression and no dependency change: the array simply declares the type it always had at runtime.

### 3.2 Change and preserved behaviour

```diff
-import { Extension } from "@tiptap/core";
+import { Extension, type Extensions } from "@tiptap/core";
…
-    const base = [
+    const base: Extensions = [
```

Type-only: both the import modifier and the annotation are erased at emit, so the compiled output is byte-identical. Explicitly preserved: extension **ordering** (StarterKit → Underline → TextStyle → FontFamily → FontSize → TextAlign → Link → Table → TableRow → TableHeader → TableCell → Placeholder → Image), the DAM gating (`if (onPickDamImage)` still guards the `push`), and the image flags `inline: false`, `allowBase64: false`, `HTMLAttributes: { class: "article-dam-image" }`. The `useMemo` dependency list `[onPickDamImage, placeholder]` is unchanged. No HTML serialization or sanitizer change (`onChange(sanitizeHtml(ed.getHTML()))` untouched) and no relaxation of image restrictions.

---

## 4. Diagnostics — 14 → 10, normalized by file/line/error-code

Baseline copied from the file the task named: `docs/architecture/evidence/TB-FE-FASHION-PREVIEW-BRAND-LOGO-NULL-001/typecheck-after-tsc.txt`.

| | Errors |
| --- | --- |
| Before (baseline) | **14** |
| After | **10** |
| Delta | **−4** |
| Introduced diagnostic keys | **0** |

Removed keys (exactly four, no additions):

```text
<= app/admin/product-rich-text-editor.tsx(162,9) TS2345
<= features/admin-languages/components/language-list.tsx(112,7) TS2353   (pinned)
<= features/admin-languages/components/language-list.tsx(41,102) TS2353  (flex)
<= features/admin-languages/components/language-list.tsx(42,111) TS2353  (flex)
ADDED: (none)
```

Per-file tallies after — both target files are now **fully clean**:

```text
app/storefront/storefront-landing-blocks.tsx                1
lib/storefront-composition/shared-composition-renderer.tsx  9
```

**Transparency about `tsc` runs.** The task asked for one run after the final edits. The intended single verification run was executed and it surfaced the masked `filter` diagnostic described in §2.4, so after that one further edit the final run was repeated. Exactly **one** run is the recorded final verification; the intermediate run is disclosed rather than hidden, and no run was used to chase unrelated files.

---

## 5. Focused tests — one run per area, all green, no pre-existing failures

**Scope A** (`focused-tests-scope-a.txt`) — the bridge contract guard plus the languages screen characterization suite:

```text
design-system/app-data-grid/legacy-grid-bridge.test.ts
features/admin-languages/admin-languages.characterization.test.ts
→ tests 11, pass 11, fail 0
```

`legacy-grid-bridge.test.ts` is the directly relevant guard: it asserts actions pinning (`assert.equal(actions?.pinned, "left")` at `:61`), width/minWidth/maxWidth pass-through and the uncapped-actions rule — i.e. the very behaviour Scope A had to preserve.

**Scope B** (`focused-tests-scope-b.txt`) — the product create flow, the only existing suite that exercises the rich-text editor's consumers:

```text
app/admin/product-create-flow.test.ts
→ tests 3, pass 3, fail 0
```

**Pre-existing failures observed in the selected sets: none.** Tests were run at most once each; no retry cascade. **Not run (and not reported as pass):** full `npm test` / `test:discovered`, build, browser/visual verification, AMSC, and all other guards. No test was added or amended — Scope B is type-only (nothing runtime-observable to assert), and for Scope A the runtime inertness is proven by the probe plus the existing bridge guard rather than by a new ceremonial source-text assertion.

---

## 6. Runtime / visual impact

- **Scope A: none.** Runtime probe §2.5 shows byte-identical `ColDef` output (serialized-equal, modulo the intentionally fresh `cellRenderer` closure) before and after, in both directions. Column widths, actions pinning, actions filtering and RTL behaviour are unchanged. `flex` never reached AG Grid, so it cannot have caused layout change either before or after.
- **Scope B: none.** Type-only; erased at emit. Extension list, order, image flags and DAM gating are textually unchanged apart from the annotation line.

---

## 7. Outstanding risks and rollback

- **Sibling screens still carry the same dead fields.** `flex: n` appears in ~10 other admin screens (`product-list.tsx:144,167`, `content-list.tsx:228`, `content-author-list.tsx:128`, `shipping-services-screen.tsx:248`, `catalog-units-screen.tsx:179`, `category-products-panel.tsx:404,460,599,619`). Those files do **not** currently error because their arrays are contextually typed through `useMemo<GridColumnDef<…>[]>` rather than a direct annotated `return`, which suppresses the excess-property report — the properties are equally dead there. **Deliberately out of scope**; recorded as an observation, not repaired.
- **Residual risk for this task: negligible.** Only contract-invalid, unread properties were removed from one literal, and one type annotation was added.
- **Rollback:** revert the single commit. Scope A's three (four) TS2353 diagnostics and Scope B's TS2345 return; runtime behaviour is identical either way, because the removed properties were never read.

## 8. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
