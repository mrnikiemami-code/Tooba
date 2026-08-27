# 06 — Content SEO proof (TB-P06-T013)

Task: `TB-P06-T013`

## Backend

| Field | Persistence | Public detail |
|---|---|---|
| SeoTitle | `articles.SeoTitle` (max 200) | Returned on published detail |
| SeoDescription | `articles.SeoDescription` (max 500) | Returned on published detail |
| Locale | default `fa-IR` | Returned; optional filter on get-by-slug |
| Slug | stable URL key | Canonical path segment |

## Frontend metadata

| Route | Metadata source |
|---|---|
| `/blogs` | Static title/description + `alternates.canonical: /blogs` |
| `/blogs/[slug]` | `generateMetadata` from `loadPublishedArticleBySlug`: `title = seoTitle \|\| title`, `description = seoDescription \|\| excerpt`, canonical `/blogs/{slug}`, OpenGraph `type: article`, `locale: fa_IR` |
| Missing slug | `robots: { index: false, follow: false }` |

## Honesty

- No fabricated structured-data / sitemap ownership claim in this task.
- SEO fields are first-class on Content articles; full SEO platform (hreflang matrix, sitemap ownership) remains architecture-tracked elsewhere.
