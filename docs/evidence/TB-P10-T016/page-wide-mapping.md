# Page-wide mapping — TB-P10-T016

One store appearance. Wrappers map to global roles; cards/header/footer stay independent.

| Family | Mapping |
| --- | --- |
| Shell | PageBackground (`bg-page`, `data-storefront-canvas`) |
| Home | categories/most-viewed/brands = SectionSurface; flash = SectionAccent (inner primary chrome kept); best-sellers/new/articles/testimonials/mid-banners = SectionAlternate |
| Landing | `landingSectionSurfaceRole`: Hero/PromoBanner=accent; ProductCollection/ArticleList=alternate; CategoryGrid/BrandStrip/Reviews/RichText/NavigationMenu/unknown=section |
| PLP/listing | SectionSurface |
| PDP | PageBackground + SectionSurface marker; product cards remain CardSurface |
| Blogs | listing/detail mains = SectionSurface |
| Cart | PageBackground + Card/Elevated summaries |
| Shipping/Checkout/Payment | PageBackground (removed full-page white island); cards stay white |
| Account/Wishlist | PageBackground shell; wishlist section marker; cards stay CardSurface |
| Header/Footer | HeaderSurface / FooterSurface; not forced into section roles |
