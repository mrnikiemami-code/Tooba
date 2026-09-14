# Section registry

Code-owned keys: Hero, ProductCollection, CategoryGrid, BrandStrip, PromoBanner, ArticleList, Reviews, RichText.

| Type | Status |
| --- | --- |
| Hero / PromoBanner | Controlled title/href/mediaAssetId |
| ProductCollection | Manual, Category, Brand, Newest |
| CategoryGrid / BrandStrip | Optional manual IDs; empty = published projection later |
| ArticleList | Latest only |
| Reviews | Title only; existing review source later |
| RichText | Plain text; `<` rejected |

Deferred (no fake semantics): ProductCollection BestSelling, Featured, Discounted; ArticleList Manual.
