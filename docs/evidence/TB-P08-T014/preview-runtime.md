# TB-P08-T014 — Preview runtime

- Renders storefront-like article (title, excerpt, sanitized body, author, category, tags, cover, publish metadata).
- Body uses `ArticleBodyHtml` + `sanitizeArticleRichHtml` (same as public Article).
- Unsaved behavior: **require Save before Preview** (toast if dirty). Documented choice — no silent stale preview.
