# Section discovery

- **PageComposition `PageSection`**: Home-only (`PageKeys.Home`), schema `page_composition`, order/visibility/labels. `sourceKind` is stored, not fetched. Not locale+slug landing pages.
- **StoreLandingPage (T010)**: Catalog landing shell; no sections yet.
- Home commercial payloads: `StorefrontComposer.GetHomeAsync`. Rails are listing slices; BestSelling/Featured are not real sales/featured flags.
- Admin pickers already exist for products/categories/brands/articles; reuse later. No landing builder UI in this task.

Decision: new Catalog `StoreLandingPageSection` owned by `StoreLandingPage.PageId`. Do not reuse PageComposition.
