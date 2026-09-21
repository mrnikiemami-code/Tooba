# ApiResponseFactory

Production: `CreateProblemDetails(Exception)` / `FromException(Exception)` — culture from HttpContext via resolver.
Tests: `CreateProblemDetailsForTests(Exception, acceptLanguage)`.
`CreateFromMapped` avoids double Map in presentation service.
