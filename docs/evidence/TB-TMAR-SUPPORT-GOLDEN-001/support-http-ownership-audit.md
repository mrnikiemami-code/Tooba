# Support HTTP Ownership Audit

## Before
- HTTP ownership: `Host/Tooba.Host/Support/SupportEndpoints.cs`
- Direct `ISupportDirectory` calls from Host
- Manual `{ title, errorCode }` JSON + `PlatformHttpException` catches
- AccessControl capability checks embedded in Host endpoint file

## After
- HTTP ownership: `Modules/Support/Tooba.Support.Endpoints/`
  - `SupportEndpointModule.MapSupportEndpoints()`
  - `Customer/SupportCustomerEndpoints.cs`
  - `Seller/SupportSellerEndpoints.cs`
  - `Admin/SupportAdminEndpoints.cs`
- Host calls only `app.MapSupportEndpoints()` (+ presentation/authorizer registration)
- `Host/Support/SupportEndpoints.cs` **deleted**
- Host `Support/` retains only `SupportDevelopmentSeedHost.cs` (Development bootstrap)

## Routes preserved
| Audience | Verbs/Paths |
|----------|-------------|
| Customer | GET/POST `/v1/customer/support/tickets`, GET `{id}`, POST replies/close/reopen |
| Seller | same under `/v1/seller/support/tickets` |
| Admin | GET list/get, POST replies, PATCH, GET `/v1/admin/support/demo-preview` |

## Verdict
**Support-HTTP-Ownership: MODULE_ENDPOINTS**
**Support-Endpoints-State: REAL_PROJECT_PRESENT**
**Support-Host-Endpoints: REMOVED**
