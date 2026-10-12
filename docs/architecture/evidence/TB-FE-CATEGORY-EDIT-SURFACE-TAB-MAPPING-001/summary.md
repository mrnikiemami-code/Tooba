# TB-FE-CATEGORY-EDIT-SURFACE-TAB-MAPPING-001 — exhaustive tab → edit-surface mapping (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Base SHA:** `1a98fa6a33dd18bb27e5f8c84f26f1822c5e77b6` — verified `HEAD == origin/main == Expected-Starting-HEAD`, clean tracked worktree, no reset/stash/rebase/force-push.
**Gate outcome:** `"reservation"` **must** map to `null`; the stop gate passed on source evidence, so a bounded repair was implemented and `"reservation"` was **not** added to `EditSurface`. `BLOCKED_ARCHITECT_DECISION` was not required.

## 1. The recorded diagnostic

```text
app/admin/category-admin-screen.tsx(1801,5): error TS2322: Type '"general" | "translations" | "attributes" | "facets" | "mega-menu" | "reservation"' is not assignable to type 'EditSurface | null'.
```

The two contracts (screen lines 80-82):

```ts
type TabId = (typeof TABS)[number]["id"];
// = "general" | "translations" | "attributes" | "facets" | "mega-menu" | "products" | "reservation"
type EditSurface = "general" | "translations" | "attributes" | "facets" | "mega-menu";
```

and the defect:

```ts
const tabToEditSurface = (tab: TabId): EditSurface | null => {
  if (tab === "products") return null;   // only ONE non-edit tab excluded
  return tab;                            // "reservation" leaks through -> TS2322
};
```

## 2. STOP GATE — does `"reservation"` belong in `EditSurface`? No. Four independent proofs

1. **The header edit handler explicitly refuses reservation.** `handleHeaderEdit` (line 1744) branches `if (activeTab === "products") { … return; }` then **`if (activeTab === "reservation") { return; }`** — reservation is grouped with products as a *non-edit-surface* tab, and unlike products it does not even route anywhere.
2. **Reservation has its own independent editor, not an `EditSurface`.** The reservation tab renders `<CategoryReservationPanel>` (line 2193, imported line 55), which owns its own draft via `useReservationDraft` / `ReservationPolicyEditor` (`category-reservation-panel.tsx`) — a completely separate state and save path from `editSurface`.
3. **Every edit-surface consumer is an exact `editSurface === X && activeTab === X` conjunction.** Lines 1846-1850 (`isGeneralEdit` … `isMegaMenuEdit`), 1855-1856, and the header save/discard handlers 1762/1766/1772/1776/1780/1784/1788. Reservation matches **none** of them, so `editSurface` can never legitimately hold `"reservation"`.
4. **Navigation resets the surface to `"general"` for non-general tabs.** `navigateToCategory(id, tab: TabId = "general")` (line 1401) calls `setEditSurface("general")` unconditionally and builds `/…/{tab}` for any non-general tab. So tab-level concepts like products/reservation are navigational, while `editSurface` is deliberately limited to the five editing surfaces.

**Conclusion:** adding `"reservation"` to `EditSurface` would be exactly the mechanical, contract-falsifying change the task forbade — it would advertise a sixth editing surface that has no renderer, no draft, no save path and no header affordance. Mapping it to `null` is the truthful option, and it is the same treatment `"products"` already receives.

**Was any product decision needed?** No. The mapping is fully determined by existing source, so the gate passed with no escalation.

## 3. Residual "fictitious surface" audit (what the previous code actually did)

At the single call site (line ~2093, inside the tab `onClick`):

```ts
const nextSurface = tabToEditSurface(tab.id);
if (formMode.mode === "edit") {
  if (nextSurface) { setEditSurface(nextSurface); prepareSurfaceDraft(nextSurface); }
} else if (nextSurface) {
  setEditSurface(nextSurface);
} else {
  setEditSurface("general");
}
```

Before the repair, clicking the reservation tab passed the truthiness test with the out-of-union value `"reservation"`, so `editSurface` received a fictitious surface. That assignment was inert in every observable way:

- `prepareSurfaceDraft` / `revertSurfaceDraft` (lines 1712-1740) act **only** on `"general"` and `"translations"` and otherwise just `setSlugFieldError(null)` — so `prepareSurfaceDraft("reservation")` was a no-op, i.e. **no draft was ever prepared for it**.
- All five `isXEdit` flags and the header save/discard handlers require `activeTab` to equal the surface too, and `activeTab` is `"reservation"` in that branch — so no header edit/save/discard affordance could appear.

After the repair, `nextSurface` is `null` for reservation:
- **edit mode:** the `if (nextSurface)` body is skipped, so `editSurface` keeps its previous valid value instead of being overwritten with a fictitious one. Since `activeTab` is now `"reservation"`, every `editSurface === X && activeTab === X` conjunction remains false in the same way — no header affordance, no draft swap.
- **non-edit mode:** falls to `setEditSurface("general")` — identical to the existing `"products"` behaviour (`navigateToCategory` also resets to `"general"`).

So the effective user-visible behaviour is unchanged, while the state can no longer hold a value the contract does not allow. **Dirty/discard semantics are untouched:** the pre-existing `if (formMode.isDirty && !formMode.confirmDiscardIfDirty()) { e.preventDefault(); return; }` guard and the `revertSurfaceDraft(editSurface)` + `clearDirty()` sequence that precede the mapping call were not modified.

## 4. Exact change — exhaustive, type-safe mapping as data

```diff
 type EditSurface = "general" | "translations" | "attributes" | "facets" | "mega-menu";
 
+/**
+ * نگاشت صریح تب → سطح ویرایش.
+ * فقط پنج تب، سطح ویرایش دارند. «محصولات» صفحهٔ مستقل است و «سیاست رزرو»
+ * ویرایشگر مستقل خودش را دارد؛ handleHeaderEdit هم برای رزرو return میکند و
+ * navigateToCategory سطح ویرایش را به general بازنشانی میکند.
+ * کلیدها به TabId قفلاند تا افزودن تب جدید در آینده بیصدا از قلم نیفتد و
+ * هیچگاه یک سطح ویرایش ساختگی (مثل «reservation») به editSurface نرسد.
+ */
+const EDIT_SURFACE_BY_TAB: Record<TabId, EditSurface | null> = {
+  general: "general",
+  translations: "translations",
+  attributes: "attributes",
+  facets: "facets",
+  "mega-menu": "mega-menu",
+  products: null,
+  reservation: null,
+};
+
 interface GeneralDraft {
@@
-  const tabToEditSurface = (tab: TabId): EditSurface | null => {
-    if (tab === "products") return null;
-    return tab;
-  };
+  const tabToEditSurface = (tab: TabId): EditSurface | null => EDIT_SURFACE_BY_TAB[tab];
```

**Why an exhaustive `Record` rather than `if (tab === "products" || tab === "reservation") return null;`** — the task preferred "an explicit, exhaustive and type-safe conversion". `Record<TabId, EditSurface | null>` makes the tab→surface contract *total*: every one of the seven `TabId` members must state its decision, so a future eighth tab is a **compile-time error** rather than a silently widening `return tab`. That is precisely the class of defect being repaired, so the fix removes the failure mode instead of only the instance. The function signature is unchanged, so the single call site needed no edit.

No cast, no `any`, no `unknown-as`, no `ts-ignore`, no added non-null assertion, no suppression, no widening of `EditSurface`, and no change to `TABS`, `TabId`, `handleHeaderEdit`, `navigateToCategory`, `CategoryReservationPanel` or the dirty/discard logic. `tsconfig` does not enable `noUncheckedIndexedAccess`, so `EDIT_SURFACE_BY_TAB[tab]` yields exactly `EditSurface | null` — matching the declared return type with no extra `undefined`.

## 5. Diagnostics — 17 → 16, normalized

Baseline copied from the committed evidence the task named: `docs/architecture/evidence/TB-FE-PREVIEW-FILL-REAL-REVIEW-FIXTURE-001/typecheck-after-tsc.txt`.

| | Errors |
| --- | --- |
| Before (baseline) | **17** |
| After | **16** |
| Delta | **−1** |
| Introduced diagnostics | **0** |

Normalized by file/line/code; exactly one removed entry, no added entries:

```text
<= app/admin/category-admin-screen.tsx(1801,5) TS2322
```

Per-file tallies (before → after) — only the target file cleared:

```text
app/admin/category-admin-screen.tsx                          1 → 0   ← target TS2322 gone
app/admin/product-rich-text-editor.tsx                       1 → 1
app/storefront/storefront-landing-blocks.tsx                 1 → 1
app/template-preview/fashion/fashion-template-preview-view.tsx 1 → 1
features/admin-languages/components/language-list.tsx        3 → 3
lib/storefront-composition/fashion-demo-preview.ts           1 → 1
lib/storefront-composition/shared-composition-renderer.tsx    9 → 9
```

`tsc --noEmit` still exits non-zero with **16 errors**; no greenness is claimed. Raw: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

## 6. Focused tests — two runs, all green

**Run 1 — the reservation-policy guards** (`reservation-policy-admin.test.ts` + `admin-reservation-cycle.test.ts`), which are the suites most directly concerned with the separate reservation editing flow this repair must preserve:

```text
→ tests 11, pass 11, fail 0
```

**Run 2 — the category screen guards** (`category-admin-form.test.ts` + `t038-category-grid-ux.test.ts`), which read `category-admin-screen.tsx` itself (`path.join(root, "app/admin/category-admin-screen.tsx")`, line 26):

```text
→ tests 28, pass 28, fail 0
```

Notably `category-admin-form.test.ts:312` still asserts `/id: "reservation", label: "سیاست رزرو", implemented: true/` and passes — confirming the reservation *tab* is untouched and still declared, while only its edit-surface mapping changed. **Pre-existing failures observed: none** in either run, so nothing needed disclosure or chasing; no test loop.

**No test was added or amended:** the task allowed a test change only if it guards genuine behaviour without ceremonial source assertions, and the contract here is enforced by the compiler (the `Record` key set) plus the existing screen/reservation guards, which pass. A new source-text assertion would be ceremonial, which the task forbade. Raw: `focused-tests.txt`, `focused-tests-2.txt`.

Not run (deliberately): full `npm test` / `test:discovered`, build, browser/visual verification, AMSC, other guards. No unrun test is reported as pass.

## 7. Proof summary for the task's third verification clause

- **Reservation keeps its independent editor:** `EDIT_SURFACE_BY_TAB.reservation = null`, `handleHeaderEdit`'s reservation `return` and the `activeTab === "reservation"` → `<CategoryReservationPanel>` branch (line 2193) are all unmodified; the reservation-policy guard suites pass 11/11.
- **No fictitious new `EditSurface`:** `EditSurface` is unchanged (still the five members) and the mapping is total over `TabId`, so the compiler now rejects any tab that lacks an explicit surface decision.
- **Tab switching / editing state:** the dirty-check/confirm and `revertSurfaceDraft` + `clearDirty()` logic preceding the mapping call is byte-identical; the only state difference is that `editSurface` can no longer hold `"reservation"`.

## 8. Risk and rollback

- **Risk: very low.** Compile-time guarantee added; runtime deltas are limited to §3 and are observably inert (both affected helper functions ignore any surface other than general/translations, and `activeTab` gates every surface comparison). No navigation, draft, permission, backend, schema, ShopEva Home/PDP or shared-grid change.
- **Rollback:** revert the single commit; TS2322 returns at line 1801 and the previous (fictitious-surface) runtime behaviour resumes.

## 9. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
