# Atomicity / history — W14 ProductSeo

## Atomic update (preserved)

One logical persistence operation:

1. Upsert localized `seo_title`
2. Upsert localized `seo_description`
3. Touch slug / SeoTitleSeam (fa-IR) / UpdatedAt
4. Queue `ProductHistoryRules.EventSeoChanged`
5. Single `SaveChangesAsync`

No partial commits.

## Actor

PUT binds `CatalogActorRequestBinding` before command so history stores ActorUserId / DisplayName from Admin authorizer session.
