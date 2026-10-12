# TB-FE-FASHION-PREVIEW-ORIGIN-TYPE-001 — Fashion preview origin state typed to its true contract (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Base SHA:** `d62b1ea2ed8e29124f2d4364e1605cd9f1046fc7` — verified `HEAD == origin/main == Expected-Starting-HEAD`, clean tracked worktree, no reset/stash/rebase/force-push.
**Gate outcome:** the display-only branch of the DECISION GATE was **proven**, so a bounded truthful repair was implemented. No `BLOCKED_ARCHITECT_DECISION` was required. `origin` is retained and **no provenance semantics were weakened**.

## 1. The recorded diagnostic

```text
app/template-preview/fashion/fashion-template-preview-view.tsx(60,19): error TS2345: Argument of type 'string' is not assignable to parameter of type 'SetStateAction<"operational-store-catalog" | "fashion-template-catalog-persisted">'.
```

The producer/consumer contracts (exact locations):

| Location | Contract |
| --- | --- |
| `fashion-demo-preview.ts:28` | `export const FASHION_DEMO_ORIGIN = "fashion-template-catalog-persisted" as const;` |
| `fashion-demo-preview.ts:29` | `export const FASHION_STORE_ORIGIN = "operational-store-catalog" as const;` |
| `fashion-demo-preview.ts:583-589` | `loadFashionTemplatePreview(): Promise<{ kind: "sample"; …; origin: string; }>` — **sample loader declares `origin: string`** |
| `fashion-demo-preview.ts:664-672` | `loadFashionStorePreview(...): Promise<{ kind: "store"; …; origin: typeof FASHION_STORE_ORIGIN; }>` — **store loader declares the literal** |
| `fashion-demo-preview.ts:469` | `origin: String(pick(raw, "origin", "Origin") ?? FASHION_DEMO_ORIGIN)` — sample origin is read out of the **Host response JSON** and stringified |
| `fashion-demo-preview.ts:610` | `origin: preview.origin \|\| FASHION_DEMO_ORIGIN` — returned to the view |
| `fashion-template-preview-view.tsx:44` (old) | `useState(source === "store" ? FASHION_STORE_ORIGIN : FASHION_DEMO_ORIGIN)` — **both branches are literal consts**, so TS infers `SetStateAction<"operational-store-catalog" \| "fashion-template-catalog-persisted">` |
| `fashion-template-preview-view.tsx:60` | `setOrigin(result.origin)` — the sample loader's `string` is not assignable to that inferred literal union → TS2345 |

**Is an arbitrary origin string legitimate API provenance, or should it be constrained?** It is legitimate *as produced*: the sample endpoint's `origin` comes from Host data (`String(pick(raw, "origin", "Origin") ?? …)`), so the loader is right to declare `string`. The error is therefore in the **consumer's inferred state type**, not in the producer.

## 2. DECISION GATE — is `origin` display-only provenance, and not a trust discriminator? Proven, six ways

The task's own condition for typing this state `string` is that `origin` must be provenance **display/test metadata** only. The complete set of `origin` uses in the view is exactly **three** lines (verified by repo-wide search): declaration `:44`, the setter `:60`, and `data-demo-origin={origin}` at `:119`. There is **no conditional, no comparison, no loader argument and no authorization** derived from it.

1. **The only consumer is a data attribute.** `data-demo-origin={origin}` (`:119`) — a test/evidence marker on the preview root, alongside `data-preview-source={source}`. Nothing reads it to decide sourcing.
2. **Sample-vs-store source selection does not use `origin`.** It is `source === "store" ? loadFashionStorePreview(uiLocale, { fixture: storeFixture }) : loadFashionTemplatePreview()` (`:54-56`), driven purely by `previewContext.sourceMode` (`:39`, `:44` initial only).
3. **Trust/purity is not derived from `origin`.** `setPurityOk(result.kind === "sample" ? result.purity.isPure : false)` (`:63`) — from the loader's discriminated `kind` and server-reported `purity`, never from `origin`.
4. **`isStore` inside the loader never sees this state.** `buildFashionTemplatePage` computes `const isStore = options.origin === FASHION_STORE_ORIGIN` (`fashion-demo-preview.ts:300`), but its `options.origin` is always a module const supplied by the loader itself — `FASHION_DEMO_ORIGIN` (`:266`) or `FASHION_STORE_ORIGIN` (`:700`). The view's state is never passed into the loader, so no Host-provided string can change what the composition treats as store data.
5. **No escaping of an untrusted value into a trusted marker.** The repair introduces **no fallback and no coercion** — the value that already flowed to the attribute continues to flow through unchanged, and the state simply stops pretending to be a two-literal union.
6. **The identical sibling already behaves this way.** The shared industry view has the *same* code path — `:74-76` `useState(source === "store" ? INDUSTRY_STORE_ORIGIN : industryDemoOrigin(templateKey))` with `:92 setOrigin(result.origin)` — and compiles only because `industryDemoOrigin(key): string` (`industry-demo-media.ts:96`) happens to widen the union to `string`. So `string` is the *de-facto, existing* canonical type for this display marker, and this repair makes the Fashion view consistent with its shared counterpart instead of relying on an accident of one helper's return type.

**Conclusion:** `origin` is provenance display/test metadata only, not a source authorization or sample/store trust discriminator. The smallest truthful remedy is to give the state its real type. Constraining it to the two literals would have required either a cast/suppression (forbidden) or inventing a producer-side normalization of a Host-supplied value into a trusted marker (explicitly forbidden, and it would presuppose a product/security decision the task did not authorize).

## 3. Exact change — one production file

```diff
-  const [origin, setOrigin] = useState(source === "store" ? FASHION_STORE_ORIGIN : FASHION_DEMO_ORIGIN);
+  /**
+   * منبع نمایشی پیشنمایش — فقط در data-demo-origin مصرف میشود و هیچگاه مبنای
+   * انتخاب داده یا تشخیص اعتماد sample/store نیست؛ آن تصمیمها از sourceMode و
+   * result.kind/result.purity میآیند. چون loadFashionTemplatePreview مقدار origin را
+   * از Host بهصورت string میخواند (fashion-demo-preview.ts:588)، این state صریحاً
+   * string است؛ همان چیزی که view مشترک صنایع هم عملاً دارد.
+   */
+  const [origin, setOrigin] = useState<string>(
+    source === "store" ? FASHION_STORE_ORIGIN : FASHION_DEMO_ORIGIN,
+  );
```

The initial value still selects between the two canonical consts, exactly as before — `source === "store"` still primes the state with `FASHION_STORE_ORIGIN`, which is the correct optimistic marker before the loader resolves. No cast, no `any`, no `as unknown as`, no added `!`, no `ts-ignore`, no suppression, no fallback, no producer/loader edit, no change to source selection, purity, fixtures, rendering, context, data attributes, backend DTO, SSR, locale behaviour, or any locked Home/PDP/Shopeiva surface. No test was added or amended — the task permitted that only for a genuine provenance boundary without source-text ceremony, and the contract here is enforced by the compiler.

## 4. Diagnostics — 16 → 15, normalized

Baseline copied from the file the task named: `docs/architecture/evidence/TB-FE-CATEGORY-EDIT-SURFACE-TAB-MAPPING-001/typecheck-after-tsc.txt`.

| | Errors |
| --- | --- |
| Before (baseline) | **16** |
| After | **15** |
| Delta | **−1** |
| Introduced diagnostics | **0** |

Normalized by file/line/code; exactly one removed entry, no added entries:

```text
<= app/template-preview/fashion/fashion-template-preview-view.tsx(60,19) TS2345
```

Per-file tallies after (the target file is gone; the unrelated `fashion-demo-preview.ts` TS2322 at line 216 remains, out of scope):

```text
app/admin/product-rich-text-editor.tsx                       1
app/storefront/storefront-landing-blocks.tsx                 1
features/admin-languages/components/language-list.tsx        3
lib/storefront-composition/fashion-demo-preview.ts           1   (separate: :216 TS2322 on StorefrontBrandItem[], not this task's error)
lib/storefront-composition/shared-composition-renderer.tsx    9
```

`tsc --noEmit` still exits non-zero with **15 errors**; no greenness is claimed. Raw: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

## 5. Focused tests — one run of the closest provenance guards, honest failures

**Run 1 — the three closest fashion preview/provenance guards** (`admin-builder-ux-r4.guard.test.ts`, `admin-builder-ux-r7.guard.test.ts`, `admin-builder-ux-r8.guard.test.ts`):

```text
→ tests 16, pass 14, fail 2
```

**The 2 failures are PRE-EXISTING and unrelated to this task — they are stale backend paths, and they abort at *import* time before any assertion.** Both suites `readFileSync` backend sources that have since been moved into subfolders:

- `admin-builder-ux-r4.guard.test.ts:25` → `Error: ENOENT … src/backend/Modules/Catalog/Tooba.Catalog.Domain/TemplateCatalog.cs`
- `admin-builder-ux-r7.guard.test.ts:23` → `Error: ENOENT … src/backend/Modules/Media/Tooba.Media.Domain/MediaAsset.cs`

Actual current locations (`git ls-files`): `src/backend/Modules/Catalog/Tooba.Catalog.Domain/TemplateCatalog/TemplateCatalog.cs` and `src/backend/Modules/Media/Tooba.Media.Domain/Aggregates/MediaAsset.cs`. Neither guard file was touched by this task (last commits `4485e3e9a` and `e535e3cfd`), nor was any backend file; my change is a one-line state typing in a `.tsx` view, and an ENOENT on a backend path cannot be caused or cured by it. **Not chased and not fixed** — unrelated files are out of scope by the task's own FORBIDDEN/NOT conditions. Consequence to disclose: these two suites' `fashionView` source-text assertions therefore **cannot execute on the current tree at all**, pre-existing.

**Run 2 — the same-class guards that load cleanly** (`admin-builder-ux-r8.guard.test.ts`, `admin-builder-ux-r12a.guard.test.ts`, `lib/storefront-composition/industry-templates.guard.test.ts`), i.e. the fashion-demo origin/provenance assertions that are actually runnable:

```text
→ tests 23, pass 23, fail 0
```

This run includes `admin-builder-ux-r8.guard.test.ts`'s provenance assertions over the fashion composition — `{ origin: FASHION_STORE_ORIGIN }` must not emit `/fashion-template-catalog-persisted/` (`:108-110`) and `{ origin: FASHION_DEMO_ORIGIN }` (`:113`, `:244`) — which is exactly the sample/store provenance-marker boundary this repair must not disturb, and it is green. Raw: `focused-tests.txt` (run 1, with the two pre-existing ENOENT failures) and `focused-tests-passing.txt` (run 2).

**Pre-existing failures observed: 2** (both disclosed above, both stale-backend-path ENOENT at import). **Tests not run:** full `npm test` / `test:discovered`, build, browser/visual verification, AMSC, and every other guard — none is reported as pass.

## 6. Proof that source selection and provenance output are retained

- **Source selection:** untouched — `:39` `source = previewContext.sourceMode`, `:54-56` the `source === "store" ? … : …` loader choice, and `:46` `purityOk` seeding are byte-identical. `origin` participates in none of them (§2.1-2.3).
- **Provenance marker output for all previously accepted runtime values:** the value written to `data-demo-origin` is the *same expression* as before (`result.origin` for both loaders). Widening a state type cannot change a runtime value, so every value previously rendered — `"operational-store-catalog"`, `"fashion-template-catalog-persisted"`, and any Host-supplied string — still renders identically.
- **Store/sample isolation:** unaffected — `isStore` is computed inside `buildFashionTemplatePage` from loader-owned consts (`fashion-demo-preview.ts:266`, `:300`, `:700`), never from this state, so a Host-provided string still cannot promote sample data to store data or vice versa.
- **Rendering/data attributes:** `data-demo-origin` remains present as a plain attribute; `data-preview-source`, `data-preview-locale`, `data-preview-device`, `data-preview-safe` are untouched.

## 7. Risk and rollback

- **Risk: very low, and lower than before the change.** The state previously claimed a two-literal union it could not honour at runtime (the loader can legitimately return any Host string); it now states its real, already-observed type. **Behaviour delta: none** — a type annotation cannot alter runtime values (React's `useState` returns the initial value unchanged regardless of the annotation).
- **Rollback:** revert the single commit; TS2345 returns at line 60 and the inferred literal-union annotation resumes.

## 8. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
