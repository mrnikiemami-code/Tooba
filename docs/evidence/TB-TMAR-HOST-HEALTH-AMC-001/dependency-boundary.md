# dependency-boundary — TB-TMAR-HOST-HEALTH-AMC-001

| Dependency | Classification |
|---|---|
| ASP.NET Minimal APIs / Results.Json | Host platform |
| ControlPlaneRegistry / ToobaPlatformOptions / MessagingHostOptions | Host Configuration / Messaging — read-only |
| `IAuthorizationReadinessProbe` | AccessControl.**Contracts** only — CURRENT narrow seam |
| MassTransit `IBusControl` | Direct transport dependency — **HOST_MESSAGING_SEAM_CANDIDATE** |
| Module Application/Infrastructure/Domain/DbContext | **ZERO** |

## AccessControl

Host Health does not own authorization business logic. Labels are non-secret mode/result strings from Contracts.

## MassTransit

Direct `IBusControl` + `CheckHealth()` belongs better behind Host/Messaging readiness abstraction (or optional `IBusControl?` injection). Do not relocate in Analyze.

## Configuration ownership

Health reads only. No options mutation. `CollectConnectionReferences` mirrors `PlatformOptionsValidator.CollectConfiguredConnectionReferences` (inputs for checks), not duplicate business validation.
