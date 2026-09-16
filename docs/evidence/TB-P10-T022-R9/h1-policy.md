# H1 Policy — TB-P10-T022-R9

## Primary H1 strategy

1. Each Store Page (Home or Landing) renders **exactly one** primary `<h1>`.
2. Source priority:
   - `PrimaryH1` when set on the page SEO model
   - otherwise page `Title`
3. Implementation: `StorefrontLandingSections` renders `<h1 class="sr-only" data-testid="store-page-primary-h1">`.
4. Section titles (Hero, rails, etc.) must use `h2`+ via shared composition variants — they must **not** introduce a second `<h1>`.

## Duplicate H1 prevention

- Page-level H1 is owned by the page shell, not by individual sections.
- Admin SEO panel documents optional Primary H1 override.
- Default Shopeiva Home (`StorefrontShopeivaHome`) already uses a single sr-only H1 for hero title when custom Home is not selected.

## Structured data alignment

- Landing: typed `WebPage` + `BreadcrumbList`
- Custom Home: typed `WebSite` + `WebPage`
- No raw JSON structured-data editor for ordinary Admin users (LOCK-SF-323)
