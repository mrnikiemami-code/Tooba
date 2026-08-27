# 03 — Current content backend audit (TB-P06-T013)

## Module layout

| Layer | Path | Status |
|---|---|---|
| Domain | `Tooba.Content.Domain/ContentArticle.cs` | LIVE — Body, Locale, SeoTitle, SeoDescription, Category; `Update` / `Publish` / `Unpublish→Draft` |
| Application | `Tooba.Content.Application/ContentContracts.cs` | LIVE — `IContentDirectory` expanded |
| Infrastructure | `ContentDirectory.cs`, `ContentDbContext.cs`, `ContentModule.cs` | LIVE |
| Seed | `ContentDevelopmentSeed.cs` | LIVE — seeded articles include body/SEO/category |
| Migration | `20260827043200_InitialContentExpand.cs` | LIVE |
| Host | `Tooba.Host/Content/ContentEndpoints.cs`, `ContentPanelComposer.cs` | LIVE |

## Domain fields (post-T013)

| Field | Notes |
|---|---|
| Body | string, max 50_000 |
| Locale | string, default `fa-IR`, max 16 |
| SeoTitle | optional, max 200 |
| SeoDescription | optional, max 500 |
| Category | optional taxonomy label, max 100 |
| Status | `Draft=0`, `Published=1`; Unpublish returns to Draft |

## Directory capabilities

- `ListPublishedAsync(page, pageSize, category?)` — list omits Body for payload size
- `GetPublishedBySlugAsync(slug, locale?)` — detail includes Body
- `ListPublishedForHomeAsync(limit)` — home rail (unchanged contract, richer DTO)
- Admin: `ListAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `PublishAsync`, `UnpublishAsync`

## Schema

- Schema name: `content`
- Table: `articles`
- Ownership: Content module only; no cross-module joins
