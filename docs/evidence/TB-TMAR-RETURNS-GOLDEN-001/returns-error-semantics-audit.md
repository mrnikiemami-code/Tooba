# Returns Error Semantics Audit

- ReturnsExceptionMapper: STABLE_CODES_ONLY exact match
- No StartsWith/Contains prose heuristics
- No unknown InvalidOperationException swallowed (TryAsync rethrows)
- ParseDestination for refund destination validation
- ReturnSemanticMapper retained as ParseDestination facade only
- Endpoints use ApiResponseFactory + Result; no manual Results.Json / ex.Message
