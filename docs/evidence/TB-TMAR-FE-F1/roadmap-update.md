# Roadmap update — TB-TMAR-FE-F1

## Pattern that worked

Thin App Router page + `features/<capability>/{api,components,index}` + shared `lib/admin` primitives + public-boundary guard + flat/admin-api freezes.

## Compatibility

Cross-admin `loadAdminLanguages` consumers updated cleanly. `saved-view-store` remains shared under `app/admin` for now.

## Next 3 candidate slices

1. catalog-units (`catalog-units-api` + screen)
2. shipping-services
3. content-authors (author-api + screens; moderate)

## Giants still need characterization-first decomposition

category-admin-screen, product-workspace-screen, content-article-admin-screen, landing composer, remaining admin-api debt.

Admin feature migration can continue safely via **TB-TMAR-FE-ADMIN-W1**.
