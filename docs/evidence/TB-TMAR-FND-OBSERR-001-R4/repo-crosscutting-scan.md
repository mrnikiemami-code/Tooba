# Repo cross-cutting scan — TB-TMAR-FND-OBSERR-001-R4

Scanned `src/backend` `*.cs`, excluding `bin`, `obj`, and `.vs`.

Patterns: `AcceptLanguage`, `StartsWith("fa"`, `StartsWith("en"`, `PlatformHttpException`, `SemanticException`, `ProblemDetails`, `Results.Json`, `ex.Message`, `exception.Message`, `Guid.NewGuid`, `ActivitySource`, `StartActivity`, `CorrelationIdContext`, `X-Correlation-ID`.

Machine-readable rows: `repo-crosscutting-scan.json` (2903 hits).

| Classification | Hits |
| --- | ---: |
| FOUNDATION_CANONICAL | 114 |
| OFFER_CANONICAL | 24 |
| LEGACY_HOST | 1536 |
| LEGACY_OTHER_MODULE | 201 |
| TEST_ONLY | 1028 |
| GENERATED | 0 in this pattern set |
| VIOLATION | 0 |

Offer production hits are catalog, resources, semantic errors, and success `Results.Json`. They are not local mappers. Foundation hits are the canonical pipeline. Host and other-module hits are pre-existing legacy error handling (`PlatformHttpException`, `ex.Message` in Story, Fulfillment, Admin, and similar). R4 did not migrate them.

No Foundation or Offer violation remained after the seed clock repair.

Verdict: PASS.
