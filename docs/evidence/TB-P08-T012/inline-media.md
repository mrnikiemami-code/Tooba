# TB-P08-T012 — Inline media

## Flow

1. Toolbar «درج تصویر» → `onPickDamImage` Promise owned by screen `damPickResolveRef`.
2. Opens canonical `MediaLibraryDialog` (single).
3. Confirm resolves `{ mediaAssetId, alt }` → TipTap insert with `articleDamImageSrc` + `data-media-asset-id`.
4. HTML sanitized by `sanitizeArticleRichHtml` (storefront DAM URLs only).

## T005 seam repair

- Removed `window.__articleDamPickResolve`.
- Direct `useRef` resolver; close/cancel resolves `null`.
