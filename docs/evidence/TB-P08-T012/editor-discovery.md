# TB-P08-T012 — Editor discovery

## Stack inspected

- `product-rich-text-editor.tsx` — TipTap Product composition (StarterKit H2–H4, Underline, TextAlign, Link, Table, FontFamily/FontSize, optional DAM Image).
- `article-rich-html.ts` — Article sanitizer (DOMPurify + DAM-only img allowlist).
- `product-rich-html.ts` — Product sanitizer (no img).
- CKEditor not present; GPL avoidance preserved.

## Gap vs T012

- Article screen reused Product editor with a small toolbar (no H4 button, no strike UI, no paragraph selector, small canvas `max-h-80`).
- Paste only sanitized on `onUpdate`, not `transformPastedHTML`.
- DAM pick used `window.__articleDamPickResolve` (T005 seam).

## Decision

Create Content-specific TipTap composition `content-article-rich-text-editor.tsx` (no CKEditor). Reuse TipTap packages + `sanitizeArticleRichHtml`. Replace window resolve with component `useRef` callback.
