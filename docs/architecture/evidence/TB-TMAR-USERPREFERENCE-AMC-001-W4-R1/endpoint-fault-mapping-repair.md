# TB-TMAR-USERPREFERENCE-AMC-001-W4-R1 — Endpoint fault mapping repair

## Defect

Admin endpoints locally catch-and-mapped `PlatformHttpException` via `api.FromPlatformException(ex)`.

## Repair

| File | Change |
|---|---|
| `Admin/UserPreferenceAdminEndpoints.cs` | Removed try/catch; `RequireAuthorizedAsync` propagates; success stays `ISender` + `ApiResponseFactory.From` |
| `Admin/UiPreferenceAdminEndpoints.cs` | Same; kept `api.FromFailure` for missing UI JSON body (Result path, not exception catch-map) |

## Post-condition

Under `Tooba.UserPreference.Endpoints`:

- `catch (PlatformHttpException` = ZERO
- `FromPlatformException(` = ZERO
- `catch (SemanticException` = ZERO
- `Results.Json` = ZERO

Authorization failures reach the global canonical exception boundary unchanged.
