# TB-FE-PREVIEW-FILL-REAL-REVIEW-FIXTURE-001 — typed real-review fixture (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Base SHA:** `40c1526afb1cd52704d36cb5ef6d9811399a50c8` — verified `HEAD == origin/main == Expected-Starting-HEAD`, no conflicting tracked changes.
**Gate outcome:** the real item **is** admissible by production policy and its legitimate type **is** expressible cleanly, so a bounded test-only repair was implemented; `BLOCKED_ARCHITECT_DECISION` was **not** required. No production source was edited.

## 1. The recorded diagnostic

```text
app/admin/landing-pages/admin-builder-ux-r8.guard.test.ts(69,3): error TS2322: Type 'false' is not assignable to type 'true'.
```

Original code (test lines 68-70):

```ts
const real = [createFakeReview(99, "fa")];
real[0]!.previewFake = false;     // <-- TS2322
real[0]!.publicId = "real-review-1";
```

## 2. Why a factory-created fake is inappropriate as a mutable real fixture

`createFakeReview` is declared (`lib/storefront-composition/preview-fake-data.ts:112`) to return **`StorefrontFeaturedReviewItem & PreviewFakeMarked`**, and the intersection wins on the marker:

```ts
type PreviewFakeMarked = {                 // preview-fake-data.ts:53-57
  previewFake: true;
  previewSource: PreviewFakeSourceMarker;
};
…
previewFake: true,                          // :124, inside the returned literal
```

`previewFake` is therefore the **literal type `true`**, and the inferred array element type is the same intersection. `real[0]!.previewFake = false` is a write of `false` into a `true` slot — the compiler is right, and the *only* honest readings are:

- the fixture was never a real item, it was a fake item being mutated into a fake-typed-as-real; or
- the fixture should be constructed as a real item in the first place.

`PreviewFakeMarked` is a deliberate, load-bearing production marker: it is what drives `item.previewFake` discrimination in the renderer and `PREVIEW_FAKE_SOURCE` provenance assertions elsewhere in this very suite (lines 208-215). **Weakening it to `boolean`, relaxing it, or casting around it would falsify production type semantics** — exactly what the task forbade — so neither was done.

## 3. The legitimate consumer input contract — and its precedent

The consumer is `applyPreviewFill<T>({ realItems: readonly T[]; createFake: (index: number) => T; … })` (`preview-fill-policy.ts:82-88`). Its honest element contract is the storefront read model:

```ts
export interface StorefrontFeaturedReviewItem {   // app/storefront/storefront-model.ts:127-143
  publicId: string; authorDisplayName: string; rating: number; title: string | null;
  body: string; verifiedPurchase: boolean; createdAt: string;
  productTitle: string; productSlug: string;
  authorAvatarUrl?: string | null;
  /** Store-preview fill only — never persisted / never published. */
  previewFake?: boolean;                            // <-- optional BOOLEAN
  previewSource?: "PreviewFake";
}
```

**The contract already admits a real item:** `previewFake?: boolean` is optional and boolean-typed, so `previewFake: false` (and its absence) are both legitimate. Nothing needed to be invented or approved by a product/architecture decision — which is why the stop gate passed.

**In-repo precedent for constructing exactly this shape:** `industry-template-preview.ts:382-392` maps review-like rows into a `StorefrontFeaturedReviewItem` by explicitly naming the contract fields (`publicId, authorDisplayName, rating, title, body, verifiedPurchase, createdAt, productTitle, productSlug`). The repair follows that canonical pattern rather than inventing one.

## 4. Exact change — test-only, one file

```diff
   } from "../../../lib/storefront-composition/preview-fill-policy.ts";
+// قرارداد ورودی مصرفکنندهٔ fill — همان تایپی که یک آیتم «واقعی» باید داشته باشد.
+import type { StorefrontFeaturedReviewItem } from "../../../app/storefront/storefront-model.ts";
   import {

 test("partial Store data preserves real items and fills only missing slots", () => {
-  const real = [createFakeReview(99, "fa")];
-  real[0]!.previewFake = false;
-  real[0]!.publicId = "real-review-1";
+  // آیتم «واقعی» باید با قرارداد مصرفکننده تایپ شود، نه با خروجی کارخانهٔ fake:
+  // createFakeReview نوع PreviewFakeMarked را برمیگرداند که previewFake را به
+  // literal true میدوزد؛ پس mutation به false روی آن ناممکن است (TS2322) و
+  // نمیتوان آن را با cast یا سستکردن PreviewFakeMarked درست کرد.
+  // الگوی ساخت = همان نگاشت صریح فیلدهای قرارداد، مانند industry-template-preview.
+  const fakeContent = createFakeReview(99, "fa");
+  const real: StorefrontFeaturedReviewItem[] = [
+    {
+      publicId: "real-review-1",
+      authorDisplayName: fakeContent.authorDisplayName,
+      rating: fakeContent.rating,
+      title: fakeContent.title,
+      body: fakeContent.body,
+      verifiedPurchase: fakeContent.verifiedPurchase,
+      createdAt: fakeContent.createdAt,
+      productTitle: fakeContent.productTitle,
+      productSlug: fakeContent.productSlug,
+      previewFake: false,
+    },
+  ];
```

**What this achieves, point by point against the task's constraints**

| Requirement | How it is met |
| --- | --- |
| Truthful type-safe construction | The fixture is annotated with `StorefrontFeaturedReviewItem[]` — the actual consumer input contract — and declares `previewFake: false`, which that contract permits (`previewFake?: boolean`). |
| Retains necessary real review properties | All nine contract fields are present and carry the same values the previous code used (`fakeContent.*` from `createFakeReview(99, "fa")`), so no fixture content changed. |
| Distinct `publicId` | `publicId: "real-review-1"` — unchanged from before, still outside the `PREVIEW_FAKE_ID_PREFIX` namespace. |
| No falsifying production semantics | `PreviewFakeMarked` is **not** touched, relaxed or widened; no production file changed. |
| No casts / any / unknown-as / ts-ignore / added `!` | None used. The two pre-existing `real[0]!` non-null assertions are **gone** (the fixture is no longer mutated after declaration), so assertions were reduced, not added. |
| No contradiction in the fixture | `previewSource` is deliberately **omitted** rather than copied, so the "real" item no longer claims `previewSource: "PreviewFake"` while `previewFake: false` — the copy is internally consistent. |
| Assertions preserved | Untouched, in the same order: `realCount === 1`, `fakeCount >= 1`, `items[0].publicId === "real-review-1"`, `items[0].previewFake === false`, `items.slice(1).every(previewFake)`. All still exercise the real-then-fake boundary. |

**Generic inference held.** Both `realItems` (now `StorefrontFeaturedReviewItem[]`) and `createFake` (returning the intersection) still resolve `T = StorefrontFeaturedReviewItem`, because that type is the common supertype of both candidates. The compiler confirms it (0 errors in this file) and the test passes at runtime.

## 5. Diagnostics — 18 → 17, normalized

Baseline copied from the prior committed evidence, as the task specified: `docs/architecture/evidence/TB-FE-CONTENT-ACTIONS-SLOTS-CONTRACT-001/typecheck-after-tsc.txt`.

| | Errors |
| --- | --- |
| Before (baseline) | **18** |
| After | **17** |
| Delta | **−1** |
| Introduced diagnostics | **0** |

Compared **normalized by file/line/code**; the diff contains exactly one removed entry and no added entries:

```text
<= app/admin/landing-pages/admin-builder-ux-r8.guard.test.ts(69,3) TS2322
```

Per-file tallies (before → after) — only the target file cleared:

```text
app/admin/landing-pages/admin-builder-ux-r8.guard.test.ts  1 → 0   ← target TS2322 gone
app/admin/category-admin-screen.tsx                        1 → 1
app/admin/product-rich-text-editor.tsx                     1 → 1
app/storefront/storefront-landing-blocks.tsx               1 → 1
app/template-preview/fashion/fashion-template-preview-view.tsx 1 → 1
features/admin-languages/components/language-list.tsx      3 → 3
lib/storefront-composition/fashion-demo-preview.ts         1 → 1
lib/storefront-composition/shared-composition-renderer.tsx  9 → 9
```

`tsc --noEmit` still exits non-zero with **17 errors**; no greenness is claimed. Raw: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

## 6. Focused test — the target file, run alone

```text
node --experimental-strip-types --test app/admin/landing-pages/admin-builder-ux-r8.guard.test.ts
→ tests 14, pass 14, fail 0
```

**Pre-existing failures observed: none** — every assertion in the suite (including the R8 / R8-R2 lock registrations, the fake-media isolation checks and `assert.doesNotMatch` source guards) passes, so nothing had to be disclosed or chased; exactly one run, no cascade.

Notably, the repaired test is the one that verifies the real/fake boundary the task named: it passes with the new fixture, confirming `realCount === 1`, `items[0]!.previewFake === false` and an all-fake remainder still hold. Raw: `focused-tests.txt`.

Not run (deliberately): full `npm test` / `test:discovered`, build, browser/visual verification, backend suites, AMSC, other guards. No unrun test is reported as pass. **No test was weakened or removed** — only the fixture's construction and its type annotation changed.

## 7. Scope, risk, rollback

- **Scope:** exactly one file — `src/frontend/app/admin/landing-pages/admin-builder-ux-r8.guard.test.ts` (`git diff --name-only` confirms a single path). Plus the receipt and evidence the task required.
- **Untouched:** `preview-fake-data.ts` (`PreviewFakeMarked` intact), `preview-fill-policy.ts`, `storefront-model.ts` and all other production sources, locked Shopeiva Home/PDP surfaces, storefront composition, backend/schemas, tsconfig/CI/baseline.
- **Risk: none to runtime.** This is a test-file type/fixture correction; no shipped behaviour can change. The only semantic change is that the test's "real" fixture is now labelled real by construction instead of by post-hoc mutation.
- **Limitations:** no browser render (irrelevant to a `node:test` suite); the characterization remains structural, as it was before.
- **Rollback:** revert the single commit; TS2322 returns at test line 69 and the suite's assertions are unaffected.

## 8. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
