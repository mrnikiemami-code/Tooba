# 04 — Public content API (TB-P06-T013)

Task: `TB-P06-T013`

Host registration: `ContentEndpoints.MapContentEndpoints`

## Endpoints

| Method | Path | Auth | Behavior |
|---|---|---|---|
| GET | `/v1/content/articles` | Public | Paged Published articles; query: `page`, `pageSize`, `category?` |
| GET | `/v1/content/articles/{slug}` | Public | Published article by slug; query: `locale?`; **404** if missing/unpublished |

## Response shape (published)

Includes: `ArticleId`, `Slug`, `Title`, `Excerpt`, `CoverMediaAssetId`, `PublishDate`, `AuthorDisplayName`, `Tags`, `IsFeatured`, `Body` (detail only / null on list), `SeoTitle`, `SeoDescription`, `Category`, `Locale`.

## Rules

- Drafts never appear on public endpoints.
- List responses omit Body (`null`) to keep rail/list payloads small.
- Detail returns Body for rendering `/blogs/[slug]`.
- Category filter is exact optional match on published set.

## Composer

`ContentPanelComposer` maps directory DTOs to JSON without leaking Draft rows.
