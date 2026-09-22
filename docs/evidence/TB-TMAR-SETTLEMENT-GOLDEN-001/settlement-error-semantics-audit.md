# Settlement error semantics audit

- HTTP use cases return Result / Result&lt;T&gt; via ApiResponseFactory.From.
- SettlementExceptionMapper: STABLE_CODES_ONLY exact Ordinal match.
- Removed StartsWith("settlement.") / StartsWith("payout.") heuristics and unknown→PayoutRejected swallow.
- Unknown InvalidOperationException rethrows (propagates).
- No PlatformHttpException as Settlement business outcome in Endpoints.
- No manual {title,errorCode,detail}, raw Results.Json, ex.Message, Contains prose heuristics.
- Error catalog remains Infrastructure SettlementErrorCatalogContributor.
- Settlement-Result-Adoption: HTTP_USE_CASES_ADOPTED
- Settlement-Error-Classification: STABLE_CODES_ONLY
- Settlement-Prose-Mapping: NONE
- Settlement-Unexpected-Exception-Swallow: NONE
