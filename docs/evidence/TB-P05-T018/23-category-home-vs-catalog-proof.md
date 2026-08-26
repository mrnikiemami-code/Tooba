# 23 — Category Home vs Catalog Proof

| Surface | Behavior |
| --- | --- |
| Home rail | `homeCategories` from Host — root published categories **Take(20)** |
| API proof | Runtime: `HOME_CATS=8` while `ALLCATS=104` on same `/v1/storefront/home` |
| UI | Horizontal cards `data-testid="home-category-card"` — not `grid-cols-8` dump |
| Full taxonomy | Remains in Mega Menu (T016) + `/products` listing filters |
| Truth | Categories still loaded live from Catalog; Home only **selects** Shopeiva-shaped slots |

Home category presentation ≠ full Catalog taxonomy browser.
