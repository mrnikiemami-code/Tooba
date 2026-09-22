# Result and errors
Handlers return canonical `Result` with stable `SemanticError` codes. Endpoints use `ApiResponseFactory`; migrated code has no `PlatformHttpException` or `ex.Message` classification.
