# TB-FE-FASHION-PREVIEW-BRAND-LOGO-NULL-001 — brand logo absence mapped to `null` (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Base SHA:** `dc3e551407876becb3e9ca5333d4ff8b87fcb042` — verified `HEAD == origin/main == Expected-Starting-HEAD`, clean tracked worktree, no reset/stash/rebase/force-push.
**Gate outcome:** `undefined` is **not** a materially distinct required state; absence is *already* `null` in this very pipeline. The minimal correct mapping was implemented. No `BLOCKED_ARCHITECT_DECISION` required. **Runtime delta: none.**

## 1. The recorded diagnostic

```text
lib/storefront-composition/fashion-demo-preview.ts(216,9): error TS2322: Type '{ brandId: string; slug: string; name: string; productCount: number; logoMediaAssetId: string | undefined; }[]' is not assignable to type 'StorefrontBrandItem[]'.
```

Contracts involved:

| Location | Contract |
| --- | --- |
| `app/storefront/storefront-model.ts:57-64` | `interface StorefrontBrandItem { … logoMediaAssetId: string \| null; previewFake?: boolean; previewSource?: "PreviewFake"; }` — consumer-owned, **not edited** |
| `fashion-demo-preview.ts:148` | Host type: `logoMediaAssetId?: string \| null;` — **optional *and* nullable** |
| `fashion-demo-preview.ts:533` | `normalizePreview`: `logoMediaAssetId: (pick(row, "logoMediaAssetId", "LogoMediaAssetId") as string \| null) ?? null` |
| `fashion-demo-preview.ts:221` (old) | `logoMediaAssetId: b.logoMediaAssetId ?? undefined` → `string \| undefined` → TS2322 |

## 2. STOP GATE — is `undefined` materially distinct from `null` for these consumers? No. Five independent proofs

**2.1 The producer in this very pipeline already normalizes absence to `null`.** `normalizePreview` (`:533`) rebuilds each Host brand row as `(pick(...) as string | null) ?? null`, so `mapContext`'s input `preview.brands[].logoMediaAssetId` is **`null`, never `undefined`, at runtime**. The old `?? undefined` was therefore *dead defensive code* — it could not produce `undefined` — and its only real effect was to lie about the type. Verified with a direct runtime probe of the same normalization expression:

```text
normalized logo values: [{"logoMediaAssetId":null},{"logoMediaAssetId":"abc"}]
any undefined? false
```

**2.2 The sibling projection in the same function already emits `null` for absent media.** `categories` (`:191`) uses `imageMediaAssetId: c.imageMediaAssetId ?? null`, and `products` (`:209`, `:211`) use `?? null` for the other nullable media/label fields. `?? null` is the established absence idiom of this mapper; the brands branch was the sole deviation.

**2.3 The Store branch of the very same preview feature already delivers `null`.** `loadFashionStorePreview` passes `loadStorefrontBrands()` straight through (`:684`, `brands = brandsRaw ?? []`), and `storefront-api.ts:406` normalizes `logoMediaAssetId: logoRaw == null ? null : asString(logoRaw)`. So **consumers of `StorefrontBrandItem` already receive `null` on the store path today** — `null` is the feature's real absence marker, so no consumer can be relying on a distinct `undefined`. Store-vs-Sample isolation is untouched: this edit is confined to the sample mapper's brand projection and changes the store path in no way.

**2.4 The consumer cannot distinguish the two even in principle.** The only consumer of `brand.logoMediaAssetId` in the storefront UI is:

```ts
// app/storefront/storefront-home-repair-sections.tsx:234
src={storefrontMediaUrl(brand.logoMediaAssetId)}
```

and `storefrontMediaUrl` is declared `(assetId: string | null | undefined): string` (`storefront-api.ts:73`) using `if (assetId?.startsWith("/images/")) …`. Both `null` and `undefined` hit the identical `?.` short-circuit and fall through to the same demo/industry lookups. Likewise `storefrontHeader` (`:165`) normalizes `logoRaw == null ? null : String(logoRaw)` before use. There is no branch anywhere that treats `undefined` differently from `null`.

**2.5 No required absent-vs-null distinction is lost, and nothing is invented.** The repair adds **no fallback asset ID**, no Preview-Fake marker, and no coercion of untrusted input into a trusted origin/preview marker. It maps the already-`null` value to `null`.

**Conclusion:** `undefined` is not a materially distinct or required state; absence in this pipeline is `null` by construction and by contract. Mapping to `null` is the minimum correct, truthful change, so the gate passed without escalation and `StorefrontBrandItem` was **not** widened.

## 3. Exact change — one production file, one line (plus a rationale comment)

```diff
   const brands: StorefrontBrandItem[] = preview.brands.map((b) => ({
     brandId: b.brandId,
     slug: b.slug,
     name: b.name,
     productCount: b.productCount,
-    logoMediaAssetId: b.logoMediaAssetId ?? undefined,
+    logoMediaAssetId: b.logoMediaAssetId ?? null,
   }));
```

No `any`, cast, `as unknown as`, non-null assertion, `ts-ignore` or suppression. No edit to `StorefrontBrandItem`, shared schema, API, Host or backend; no change to the composition renderer, brand carousel layout, template-catalog provenance, sample/store isolation, Preview-Fake contract, Home/PDP/Shopeiva visuals, or tsconfig/CI/baseline/guards. No test added or amended: the change is **not runtime-visible** (see §4), and the module's brand projection has no existing behavioral seam reachable without widening production exports or mocking `fetch`, both of which the task forbade — so the honest outcome is no new test rather than a ceremonial one.

## 4. Intentional runtime difference: none

The task asked to be transparent about the `undefined → null` delta. The delta is **zero**:

- `mapContext`'s input is already `null` for absence (§2.1, probe output above), so `b.logoMediaAssetId ?? null` and `b.logoMediaAssetId ?? undefined` both evaluate to `null` for absent logos and to the same asset-id string when present.
- Therefore the value reaching `storefrontMediaUrl` is byte-for-byte identical before and after, for every previously possible input — including a hypothetical `undefined`, which the same `?.` consumer path already collapses (§2.4).

This is why no behavioral test was warranted: there is no runtime-observable behavior to assert.

## 5. Diagnostics — 15 → 14, normalized

Baseline copied from the file the task named: `docs/architecture/evidence/TB-FE-FASHION-PREVIEW-ORIGIN-TYPE-001/typecheck-after-tsc.txt`.

| | Errors |
| --- | --- |
| Before (baseline) | **15** |
| After | **14** |
| Delta | **−1** |
| Introduced diagnostics | **0** |

Normalized by file/line/error-code; exactly one removed entry, no added entries:

```text
<= lib/storefront-composition/fashion-demo-preview.ts(216,9) TS2322
```

Per-file tallies after — `fashion-demo-preview.ts` is now **fully clean** (its remaining count was 1 and it was this error):

```text
app/admin/product-rich-text-editor.tsx                       1
app/storefront/storefront-landing-blocks.tsx                 1
features/admin-languages/components/language-list.tsx        3
lib/storefront-composition/shared-composition-renderer.tsx    9
```

`tsc --noEmit` still exits non-zero with **14 errors**; compiler green is **not** claimed. Raw: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

## 6. Focused tests — 19/19 green

One bounded run over the nearest directly relevant pre-existing suites: `admin-builder-ux-r8.guard.test.ts` (imports this module; asserts Fashion provenance/Preview-Fake media isolation and store-vs-sample media paths), `lib/storefront-composition/industry-templates.guard.test.ts` (the sibling industry brand/mapper precedent), `app/storefront/storefront-home.test.ts` (the `StorefrontBrandItem` consumer contract, including a brand-logo assertion at `:77`).

```text
→ tests 19, pass 19, fail 0
```

**Pre-existing failures in the selected set: none.** `admin-builder-ux-r4.guard.test.ts` and `r7.guard.test.ts` were deliberately **not** re-run: they abort at import on stale backend paths (`…/Catalog/Tooba.Catalog.Domain/TemplateCatalog.cs`, `…/Media/Tooba.Media.Domain/MediaAsset.cs`, both since moved into subfolders) — a pre-existing condition already documented in `TB-FE-FASHION-PREVIEW-ORIGIN-TYPE-001` evidence, entirely unrelated to brands or to a `.ts` mapping line, and the task instructed not to chase unrelated stale paths. **Not run:** full `npm test` / `test:discovered`, build, browser/visual verification, AMSC, and all other guards; no suite is reported as pass except the three above. Raw: `focused-tests.txt`.

**Limitation stated honestly:** no existing test exercises `mapContext`'s brand branch directly (it is reachable only through `loadFashionTemplatePreview`, which performs a real Host HTTP fetch, and neither `mapContext` nor `normalizePreview` is exported). Direct behavioral coverage of this projection therefore does not exist; the equivalence argument in §4 rests on the producer normalization at `:533` plus the consumer signature at `storefront-api.ts:73`, both cited and probe-verified.

## 7. Risk and rollback

- **Risk: negligible.** One-token change of a fallback value in a type-only mismatch, with the fallback provably unreachable at runtime (§2.1) and provably indistinguishable to the sole consumer (§2.4). No locked surface, contract, provenance or isolation semantics touched.
- **Rollback:** revert the single commit; TS2322 returns at `fashion-demo-preview.ts:216` and the previous (type-lying but runtime-identical) `?? undefined` resumes.

## 8. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
