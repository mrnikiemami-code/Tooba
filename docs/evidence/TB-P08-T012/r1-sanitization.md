# TB-P08-T012-R1 — Sanitization

File: `src/frontend/app/admin/article-rich-html.ts`

Allowlist updates for CKEditor output only:

- Tags unchanged (p/h2–h4/lists/quote/a/table/figure/figcaption/img/span/…).
- Attrs: added `width`, `height`; kept `data-media-asset-id`, `class`, safe `style`.
- Classes: only `article-dam-image`, `image*`, `image-style-*`, `table`, `ck-*`, `text-*`.
- Styles: `text-align`, `font-family`, `font-size`, `width`, `height`, `margin-left/right`, `float`.
- img `src` must be `/v1/storefront/media/{guid}`; data:/external rejected.
- Forbidden: script, style tag, iframe/object/embed, event handlers, javascript:, base64.
