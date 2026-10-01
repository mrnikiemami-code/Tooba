# api-result — W3
Dashboard: Result.Success(summary) → ApiResponseFactory.From.
Results.Json on dashboard path = ZERO.
Local ToError / PlatformHttpException catch on dashboard = ZERO.
Auth failures propagate via global exception boundary.
Dev-context retains legacy Results.Json (DEFERRED_EXISTING_DEV_CONTEXT_ONLY_NOT_DASHBOARD_PATH).
