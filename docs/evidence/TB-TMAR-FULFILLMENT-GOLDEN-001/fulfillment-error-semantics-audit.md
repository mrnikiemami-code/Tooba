# Fulfillment Error Semantics Audit

- FulfillmentExceptionMapper: exact stable codes only; no Contains/StartsWith prose heuristics; unknowns rethrow.
- SellerMutate + shipping write handlers use mapper TryAsync.
- IsShippingSemantic StartsWith removed.
- Endpoints use ApiResponseFactory; no ex.Message envelopes; no PlatformHttpException for business outcomes.
- Unexpected exception swallow: NONE
