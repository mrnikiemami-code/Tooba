# boundary-security — TB-TMAR-HOST-HEALTH-AMC-001-W1

| Boundary | State |
|---|---|
| AccessControl readiness | Contracts-only `IAuthorizationReadinessProbe` |
| AccessControl Application/Infrastructure/Domain | ZERO in Health |
| Module Application/Infrastructure/Domain | ZERO in Health |
| DbContext / Npgsql direct | ZERO in Health |
| MassTransit | Host/Health transport health only (`IBusControl.CheckHealth`) |
| MultiTenancy / Errors / Security / Admin production | UNCHANGED |
| Machine status labels | allowed; not localized |
| Hard-coded user-facing prose | ZERO |
| Sensitive disclosure | ZERO after sanitization |
| Observability additions | NONE |
| Schema change | NONE |
| Frontend | UNCHANGED |
| Certification | NOT_CERTIFIED_W2_REQUIRED |
