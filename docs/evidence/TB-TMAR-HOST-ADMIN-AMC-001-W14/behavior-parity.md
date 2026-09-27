# Behavior parity — W14 ProductSeo

## Routes (exact)

- GET `/v1/admin/products/{productId}/seo`
- PUT `/v1/admin/products/{productId}/seo`
- GET `/v1/admin/products/{productId}/seo/readiness`

## JSON shapes preserved

Detail: ProductId, Locale, Slug, SeoTitle, SeoDescription, ProductName, TitleFallback, PublicPath, Readiness, UpdatedAt

Readiness: HasValidSlug, HasSeoTitleOrFallback, HasSeoDescription, HasLocalizedIdentity, IsReady, MessageFa

PUT body: Locale, Slug, SeoTitle, SeoDescription, ExpectedUpdatedAt

## Semantics preserved

- locale default/normalize via ProductSeoRules
- localized name fallback; fa-IR SeoTitleSeam
- blank slug → SlugifyFromName; invalid → workspace.product.slug.invalid
- duplicate slug → workspace.product.slug.duplicate
- stale ExpectedUpdatedAt → workspace.catalog.stale
- public path via BuildPublicPath
- view-scope GET allow / PUT deny
- EventSeoChanged with actor
