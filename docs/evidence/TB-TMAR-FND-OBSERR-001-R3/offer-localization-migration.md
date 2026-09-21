# Offer localization migration — TB-TMAR-FND-OBSERR-001-R3

## Deleted

- `OfferEndpointLocalizer` (+ bilingual en/fa switch / non-en⇒fa)
- Dead Host `OfferSemanticLocalizer`

## Replaced with

- `Tooba.Offer.Endpoints/Resources/OfferErrors.resx` (English default)
- `OfferErrors.fa.resx` (Persian)
- `OfferErrorResourceSet` → `IErrorResourceSet` / `ResourceErrorMessageLocalizer`
- Culture from central `IRequestLocaleResolver` only

## Unlimited locale

- `tr-TR` with no Turkish resource ⇒ ResourceManager fallback to default English (not Persian)
- `en-US` / `fa-IR` parent chain works
