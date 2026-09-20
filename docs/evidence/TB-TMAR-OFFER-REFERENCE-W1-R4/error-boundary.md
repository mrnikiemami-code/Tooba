# Error boundary

Application uses canonical `OfferErrorCodes` and `SemanticException`. Endpoint localization is centralized. Not-found maps to 404, duplicate/invariant conflicts to 409, and validation failures to 400.
