# behavior-parity — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1

| Branch | Before | After |
| --- | --- | --- |
| Allow | return actor | return actor |
| Unavailable | 503 + admin.authorization.unavailable | same code via SemanticException → catalog 503 |
| Deny | 403 + admin.authorization.denied | same |
| Actor missing | 401 + admin.actor.missing | same |
| Tenant missing | 503 + admin.tenant.missing | same |
| DevActorHeader | Development-only, session wins | preserved |
| MarketplacePlatformTenantId | marketplace-platform | preserved |
| ControlPlaneRegistry edition gate | unchanged | unchanged |

Presentation: catalog/localizer titles replace hard-coded exception Title strings. Codes + HTTP statuses preserved.
