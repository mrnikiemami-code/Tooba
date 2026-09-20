# Error boundary
- OfferSellerEndpoints catch SemanticException → localized title + errorCode
- SellerPanelComposer maps SemanticException → PlatformHttpException with OfferSemanticLocalizer
- No translation text in Domain/Application
