# security-observability — TB-TMAR-HOST-MULTITENANCY-AMC-001

## Hard-coded runtime user-facing text

ZERO on MultiTenancy error path (Errors W1): SemanticException + Foundation codes only. No `"Not Found"` / `"Service Unavailable"` titles remain.

## Exception message classification

ZERO — no `ex.Message` / `Message.Contains` routing.

## Sensitive data

| Signal | Classification |
|---|---|
| Operational warning: TraceId, ErrorCode, Path | Allowed (non-secret) |
| BeginScope: Edition, DeploymentId, TenantId | Allowed operational identifiers |
| Activity tags: edition, deployment, tenant_id | Allowed |
| Connection string / connection reference in response | ZERO (not written) |
| Connection resolver may throw PlatformHttpException with code only | Persistence Host impl — out of MultiTenancy cert; MultiTenancy discards resolved string after probe |

## Logging duplication

Middleware `LogWarning` on SemanticException + canonical `ExceptionPresentationService` warning/error — intentional dual signal documented in Errors W1 (operational vs presentation). Do not redesign in Analyze.

## Custom telemetry

No custom ActivitySource/Meter in MultiTenancy. Uses `Activity.Current` tags only.
