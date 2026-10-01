# error-localization — W4

Unavailable path:

- stable code = `admin.dev.unavailable`
- HTTP = 404 (`ErrorClassification.NotFound`)
- presentation = `SemanticError` → `ApiResponseFactory.From(Result.Failure<...>)`
- descriptor owner = `FoundationErrorCatalogContributor` (exactly once)
- LocalizationKey = `admin.dev.unavailable` (Foundation resource path)
- hard-coded `"Not Found"` on this route = ZERO
- local `Results.Json` error object = ZERO
- `ex.Message` presentation = ZERO
