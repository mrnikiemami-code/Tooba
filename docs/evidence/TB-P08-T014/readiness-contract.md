# TB-P08-T014 — Readiness contract

- Single source: `ArticlePublicationReadinessRules.Evaluate` (Domain).
- Used by `ContentDirectory.GetPublishReadinessAsync` and `ContentDirectory.PublishAsync` (same evaluator).
- Checks: key, labelKey, required, satisfied, detail, actionTarget.
- Mandatory: title, excerpt, body, author, language active, slug, schedule validity, not archived.
- Recommended: category, featured image, SEO title/description, SEO image (or featured fallback).
- Response: canPublish, requiredMissing, recommendedMissing, optional score (UX only).
- Stable codes under `content.publish.*` / `content.preview.*` / `content.unpublish.*`.
