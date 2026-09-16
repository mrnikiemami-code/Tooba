# Recovery Start — TB-P10-T022-R8-R2

Recorded at claim/start of repair.

## Git

- branch: main
- HEAD: bef0932b460a87a5f37e7738981c9c688d9f2b5b
- origin/main: bef0932b460a87a5f37e7738981c9c688d9f2b5b (HEAD == origin/main)
- git status: clean tracked tree; many unrelated local `.tmp-*` untracked artifacts (not committed)
- unrelated stash preserved: `stash@{0}: unrelated-user-r8r2-preflight` (admin-builder-ux-r3.guard.test.ts + admin-template-selection-workspace.tsx) — DO NOT commit
- additional stash present: `stash@{1}: temp-before-push` — preserved

## Current PreviewFillPolicy

- Module: `src/frontend/lib/storefront-composition/preview-fill-policy.ts`
- `applyPreviewFill`: real items first; fill only missing Variant slots
- `isStorePreviewFillEnabled`: `preview && previewSource === 'store'` only
- Sample / published: fill disabled

## Current preview-fake-data provider (pre-repair)

- Module: `src/frontend/lib/storefront-composition/preview-fake-data.ts`
- DEFECT: `demoImage()` imported `FASHION_IMAGES` from `fashion-demo-media.ts`
- All fake product/category/brand/article/banner/hero/promo/story media reused `/images/fashion-template/*.jpg`

## Preview-Fake media sources (pre-repair)

- `FASHION_IMAGES` → `/images/fashion-template/1.jpg` … `8.jpg`

## Template Catalog media sources

- `src/frontend/lib/storefront-composition/fashion-demo-media.ts` → `FASHION_IMAGES`, `TEMPLATE_MEDIA_GUIDS`
- Static files: `src/frontend/public/images/fashion-template/*.jpg`
- Sample loader: `buildFashionTemplatePage(..., { origin: FASHION_DEMO_ORIGIN })` via fashion-demo-preview
- Store loader: operational store catalog + PreviewFillPolicy fakes (when empty/partial)

## Shared constants

- `FASHION_IMAGES` / `TEMPLATE_MEDIA_GUIDS` in `fashion-demo-media.ts` (Template/Sample only after repair)

## Unrelated user work

- Stash `unrelated-user-r8r2-preflight` preserved; not applied; not committed.

## Recovery decision

- Expected previous HEAD matches. No RECOVERY_CONFLICT.
