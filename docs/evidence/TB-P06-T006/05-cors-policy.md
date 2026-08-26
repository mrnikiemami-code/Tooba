# 05 — CORS policy (TB-P06-T006)

## Policy registration

`Program.cs`:

```text
Policy name: ToobaCors
Origins: Tooba:AuthSecurity:CorsAllowedOrigins
Empty origins => SetIsOriginAllowed(_ => false)
Non-empty => WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()
```

Pipeline: `app.UseCors("ToobaCors")` before security headers and auth middleware.

## Endpoints with RequireCors

| Surface | File | Routes |
|---|---|---|
| Auth API | `AuthenticationHttpBoundary.cs` | `/v1/auth/*` when `enableCors: true` |
| Health | `HostHealthEndpoints.cs` | `/health/live`, `/health` when `enableCors: true` |

`Program.cs` calls `MapAuthenticationBoundary(enableCors: true)` and `HostHealthEndpoints.Map(app, enableCors: true)`.

## Development origins

`appsettings.Development.json`:

- `http://127.0.0.1:3000`, `http://127.0.0.1:3001`
- `http://localhost:3000`, `http://localhost:3001`

## Production

`appsettings.Production.json`: `CorsAllowedOrigins: []` — ops must populate explicit frontend URLs before browser cross-origin access.

Wildcard `*` rejected at startup validation.

## Test proof

`AuthSecurityHttpTests.Cors_allows_configured_origin_on_simple_request` — Origin `http://test-origin.local` receives `Access-Control-Allow-Origin` echo.
