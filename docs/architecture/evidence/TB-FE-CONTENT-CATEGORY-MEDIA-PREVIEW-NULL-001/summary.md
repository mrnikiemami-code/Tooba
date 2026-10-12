# TB-FE-CONTENT-CATEGORY-MEDIA-PREVIEW-NULL-001 — nullable media preview src (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Expected-Starting-HEAD:** `fcc96e37a02bf9c430d1b409bc00d3d59a34da23` — verified `HEAD == origin/main == Expected-Starting-HEAD`, tracked worktree clean, no untracked file modified.

## 1. The recorded diagnostic

```text
app/admin/content-category-admin-screen.tsx(507,26): error TS2322: Type 'string | null' is not assignable to type 'string | Blob | undefined'.
  Type 'null' is not assignable to type 'string | Blob | undefined'.
```

## 2. Decision gate — what does a `null` preview URL actually mean?

The helper is the whole contract (`src/frontend/app/admin/media-api.ts:90-95`):

```ts
/** URL پایدار پیش‌نمایش — از طریق پروکسی Admin به Host. */
export function mediaPreviewUrl(mediaAssetId: string | null | undefined): string | null {
  const id = mediaAssetId?.trim();
  if (!id) return null;
  return `/v1/storefront/media/${id}`;
}
```

Answers to the three questions the task required me to establish:

1. **What `null` means:** the *absence of a usable media asset id*. The helper is a pure string template with no other failure mode — it cannot fail for a non-blank id, and it cannot succeed for a blank one. `null` therefore carries exactly one meaning: "there is no media to preview", not "the preview failed for some media that does exist".
2. **Can a `mediaAssetId` be present while the URL is `null`?** Only when the id is *blank or whitespace-only* (`""`, `"   "`) — precisely the case `if (!id) return null` after `.trim()`. There is no id that is genuinely present yet non-previewable. So "has id" and "has URL" are the **same predicate** for all real data; the only divergence is the blank-string edge, where the truthful rendering is "no image assigned".
3. **How the media tab already represents absent media:** line 509 renders `<p className="text-sm text-muted">تصویری اختصاص داده نشده است.</p>` — an existing, user-facing empty state that this task preserves verbatim.

**Consequence: no product decision was required, so `BLOCKED_ARCHITECT_DECISION` is not raised.** The chosen behavior for the blank-id edge is the screen's own pre-existing "no image assigned" state, which is also the state the canonical sibling screen already uses for a `null` preview — `category-admin-screen.tsx:456-480` derives `const preview = mediaPreviewUrl(mediaAssetId)`, then renders `preview ? <img src={preview} …/> : <span>هنوز … تنظیم نشده</span>`. The repair therefore follows an existing in-repo precedent rather than inventing one.

## 3. Sub-decision: what the old code actually did with a blank id

The previous guard was `mediaAsset?.mediaAssetId ?` — a *truthiness* test on the raw id. For a whitespace-only id that guard passes, and `<img src={null}>` renders **an `<img>` element with no `src` at all** (React drops a null attribute). That is a broken-image element for a value the helper already declares unusable. Deriving the URL first removes that inconsistency instead of papering over it, and must not be replaced by `?? ""`: an empty `src` is a document-relative request to the current page, which the task explicitly forbade.

## 4. Exact change

```diff
@@ -336,6 +336,10 @@ export function ContentCategoryAdminScreen() {
   const isRtl = languageCode.toLowerCase().startsWith("fa");
+  // mediaPreviewUrl تنها برای شناسهٔ غیرخالی URL می‌سازد و در غیر آن null برمی‌گرداند
+  // (media-api.ts:91). پس پیش‌نمایش را همان‌جا مشتق می‌کنیم تا هیچ‌گاه src نامعتبر/خالی
+  // به <img> نرسد و در نبود رسانهٔ قابل‌استفاده، همان حالت «تصویری اختصاص داده نشده» بماند.
+  const mediaPreview = mediaAsset ? mediaPreviewUrl(mediaAsset.mediaAssetId) : null;
@@ -503,8 +507,8 @@
               {tab === "media" ? (
                 <div className="space-y-3">
-                  {mediaAsset?.mediaAssetId ? (
-                    <img src={mediaPreviewUrl(mediaAsset.mediaAssetId)} alt="" className="max-h-40 rounded-xl border object-cover" />
+                  {mediaPreview ? (
+                    <img src={mediaPreview} alt="" className="max-h-40 rounded-xl border object-cover" />
                   ) : (
                     <p className="text-sm text-muted">تصویری اختصاص داده نشده است.</p>
                   )}
```

No `!`, no `as string`, no `any`, no `ts-ignore`, no unsafe cast, no empty `src`. The `alt=""`, classes, `max-h-40`, the two selection buttons ("انتخاب از کتابخانه" / "حذف اختصاص"), their `disabled={form.mode === "view"}` gates, `assignMedia`, `updateContentCategoryMedia`, `mediaOpen`, tab/permission wiring and every other tab are untouched — verified by the exact diff above (`git diff --stat`: 8 lines in this file).

**Behavior equivalence, case by case**

| State | Before | After | Same? |
| --- | --- | --- | --- |
| `mediaAsset === null` | `<p>` empty state | `<p>` empty state | identical |
| id non-blank (e.g. `aaaaaaaa-…`) | `<img src="/v1/storefront/media/{id}">` | `<img src="/v1/storefront/media/{id}">` (same string from the same helper) | identical |
| id blank / whitespace-only | `<img>` with no `src` (broken element) | `<p>` empty state | corrected edge case; the media-library dialog cannot produce this id, so no reachable screen changes |

## 5. Verification

Baseline copied verbatim from the task-specified committed baseline `docs/architecture/evidence/TB-FE-ADMIN-RECEIPTS-ACTIONS-COLUMN-WIDTH-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **22** |
| After | **21** |
| Delta | **−1** — `Compare-Object` shows exactly the two removed lines of the target TS2322 (message + continuation) and **zero** added lines |
| Introduced diagnostics | **0** |

`tsc --noEmit` still exits non-zero with **21 errors**; no greenness is claimed. Raw: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

| Focused test | Result |
| --- | --- |
| `app/admin/content-category-admin-screen.test.ts` + `app/admin/media-api.test.ts` (nearest relevant existing suites) | **6 tests, 6 pass, 0 fail** |

**Pre-existing failures observed: none** in either suite, so nothing had to be documented or chased; no retry cascade.

**One justified test amendment** (permitted only if it meaningfully asserts behavior): `media-api.test.ts` gained four assertions inside the *existing* `mediaPreviewUrl` case — `null`, `undefined`, `""`, `"   "` all → `null`. This is the contract premise the screen's derived-preview guard rests on, and it was previously untested (only the happy path at line 25). It is a real behavioral assertion on the helper's public surface, not a text match on an implementation detail, and no new file or new test case was added. Raw: `focused-test.txt`.

Not run (deliberately): full `npm test` / `test:discovered`, dependency install, build, backend suites, browser/visual verification, AMSC, other guards. No unrun test is reported as pass.

## 6. Scope, risk, rollback

- **Scope:** production change limited to `src/frontend/app/admin/content-category-admin-screen.tsx` (the only file the task allowed), plus the one justified focused-test amendment the task permitted, plus this receipt/evidence.
- **Untouched by design:** `mediaPreviewUrl` signature and the shared media API, all other `mediaPreviewUrl` callers, backend/schema, storefront/Home/PDP/Shopeiva locked surfaces, tsconfig/baseline/guards, unrelated fixes, AMSC, broad suites.
- **Residual risk: low.** For all data the media-library dialog can actually produce, the rendered markup is byte-identical; the only changed case is a blank id, which was previously a `src`-less broken image and now shows the existing empty state. No request behavior is added (`?? ""` was consciously avoided).
- **Rollback:** revert the single commit; TS2322 returns and the blank-id edge reverts to the `src`-less `<img>`.

## 7. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
