# Data source contract — TB-P10-T018

| Kind | Support | Truth |
|---|---|---|
| Manual | Supported | Explicit IDs |
| Category | Supported | Category query |
| Brand | Supported | Brand query |
| Newest | Supported | Newest listing |
| LatestArticles | Supported | Content latest |
| ApprovedReviews | Supported | Approved reviews |
| MostViewed | HeuristicHomeOnly | Home sorts by ReviewCount — not dedicated API |
| Discounted | HeuristicHomeOnly | Home SpecialOffers/PromotionLabel heuristic |
| Featured | HeuristicHomeOnly | Home FeaturedProducts listing slice |
| BestSelling | HeuristicHomeOnly | Home category buckets; Landing unsupported |
| HotTrending | Deferred | No backend |

Do not expose Heuristic/Deferred as truthful Landing ranking sources.
