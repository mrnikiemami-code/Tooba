# context-parity — TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1

Preserved enrichment fields:

| Field | Source | State |
|---|---|---|
| correlationId | `GetCorrelationId` → Items fallback → `EnsureCorrelationId` | **CANONICAL_PRESERVED** |
| requestId | `context.TraceIdentifier` | **PRESERVED** |
| tenantId | `commerce.Current?.Tenant?.TenantId.Value` | **PRESERVED** |
| storeId | `= tenantId` (logging mirror) | **PRESERVED** |
| actorId | session UserId Guid non-empty → `"N"` | **PRESERVED** |
| httpMethod | `context.Request.Method` | **PRESERVED** |
| httpPath | `context.Request.Path.Value` only | **PRESERVED** |

Marketplace null tenant ⇒ tenant/store absent from scope.
Anonymous session ⇒ actor absent.
Canonical API: `ObservabilityLogScope.CreateState` + `Begin` only.
