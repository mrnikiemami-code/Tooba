# error-seam-repair

## Before
String-matched InvalidOperationException offer.not_found in OfferSellerEndpoints; produced by Pricing/Inventory seller gateways.

## After
SemanticException with stable codes. Endpoint catch list: PlatformHttpException, SemanticException only.

Residual magic-seam hits: NONE
