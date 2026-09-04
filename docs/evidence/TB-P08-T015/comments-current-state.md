# Comments current state (TB-P08-T015)

## Inspected

- `ProductReview` (Reviews module): product-scoped, Pending/Published/Rejected, rating 1–5 — **not** Article comments.
- Story modal comments: local UI-only, not Content.
- Public blog detail (`blog-detail-ui.tsx`): **no** comment render or submit form.
- Content module: **no** prior ArticleComment entity.

## Decision

Add **Content-owned `ArticleComment`**. Do not reuse ProductReview.

## Public compatibility

No public comment form invented. Admin can create Pending comments for smoke/moderation only.
