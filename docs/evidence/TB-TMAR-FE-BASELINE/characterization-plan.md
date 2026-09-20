# Characterization before refactor — TB-TMAR-FE-BASELINE

For later FE-F4 / FE-ADMIN-W1. **No decomposition now.**

## Top 5 highest-risk giants

### 1. `category-admin-screen.tsx` (~2215)
- User-visible: tree CRUD, tabs, attributes/products/facets/mega-menu panels.
- Route/query: admin category routes + tab segments.
- Mutations: category save, publish-ish flows via admin-api.
- Locale/RTL: admin FA labels.
- SEO: N/A (admin).
- Needed tests: expand category-admin-form / panel contracts; screen-level smoke for tab switching.

### 2. `product-workspace-screen.tsx` (~1570)
- Behavior: multi-panel product editor (attributes, variants, media, SEO, publishing, history, translations).
- Existing: `test:product-workspace` suite — extend before split.
- Mutations: product save pipelines.
- SEO panel: admin only but affects storefront metadata fields.

### 3. `content-article-admin-screen.tsx` (~1528)
- Article editor + CKEditor + publish date.
- Mutations: content APIs.
- Characterize create/edit/publish before split.

### 4. `admin-landing-page-composer.tsx` (~1440)
- Composition sections catalog; preview coupling.
- Guards already under composition-engine — extend composer contracts.

### 5. `admin-api.ts` (~1326)
- Cross-capability HTTP — characterize by capability groups before file split (not one giant mock).

