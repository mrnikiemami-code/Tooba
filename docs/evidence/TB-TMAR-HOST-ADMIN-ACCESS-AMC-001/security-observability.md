# security-observability — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001

## DevActorHeader

| Item | Finding |
| --- | --- |
| Header name | `X-Tooba-Dev-Actor-User-Id` (`AdminPanelAccess.DevActorHeader`) |
| Production authority? | **NO** — only when `environment.IsDevelopment()` AND session unauthenticated AND Guid parse succeeds AND non-empty |
| Production bypass risk | Header ignored outside Development; Bearer/session wins when authenticated |
| Logging of header value | **NONE** observed in Access tree |

DevActorHeader-Security-State: DEVELOPMENT_ONLY_FAIL_CLOSED_OUTSIDE_DEV

## Sensitive data audit

| Surface | State |
| --- | --- |
| Password / token logging | ZERO |
| Cookie logging | ZERO |
| Authorization header logging | ZERO |
| Actor/tenant id logging | ZERO (used only in AuthorizationCheck, not logged) |
| IP logging | ZERO |
| Unsafe exception detail leak | Titles are user-facing prose (presentation debt), not stack dumps |

Sensitive-Logging-State: CLEAN_NO_SENSITIVE_LOGS

## Observability

| Item | State |
| --- | --- |
| Custom ActivitySource | ABSENT |
| Custom Meter | ABSENT |
| Manual traceparent | ABSENT |
| Custom correlation | ABSENT |
| Logging scope | ABSENT |

Observability-State: NO_CUSTOM_TELEMETRY_IN_ACCESS (do not add in analyze)

## Path / namespace / cohesion

| Check | State |
| --- | --- |
| Namespaces | EXACT `Tooba.Host.Admin.Access` / `Tooba.Host.Admin.Access.Authorizers` |
| TypeForwardedTo / aliases / shims | ZERO in Access |
| Duplicate type names | ZERO |
| Flat misplaced files | ZERO |
| God-file / mixed responsibility | HostAdminPanelAccess edition branch is cohesive platform seam; AdminPanelAccess is Single-Store helper — acceptable split |

Path-Namespace-State: EXACT  
Cohesion-State: ACCEPTABLE_PLATFORM_SEAM_PLUS_THIN_ADAPTERS
