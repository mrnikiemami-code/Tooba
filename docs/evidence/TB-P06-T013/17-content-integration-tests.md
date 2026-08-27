# 17 — Content integration tests (TB-P06-T013)

Task: `TB-P06-T013`

## Backend — `ContentFoundationTests.cs`

| Case | Asserts |
|---|---|
| `Content_module_boundary_static_checks` | Schema `content`; directory methods `ListPublishedAsync`, `GetPublishedBySlugAsync`, `UnpublishAsync`; Draft enum name |
| `Draft_publish_unpublish_slug_and_home_listing_behave` (SkippableFact / Testcontainers) | Draft invisible publicly; publish visible by slug with Body/SEO/Category; list omits Body; home listing Published-only; unpublish hides; slug uniqueness |

## Frontend — `content-api.test.ts`

| Case | Asserts |
|---|---|
| `mapContentArticle` | PascalCase Host payload maps slug/category/body |
| `mapAdminContentArticle` | Status + id mapping |
| `formatContentDate` | Localized non-empty date |

## Notes

- Docker required for full Postgres SkippableFact; static boundary test always runs.
- No claim of full HTTP e2e suite beyond foundation + client mappers in this evidence.
