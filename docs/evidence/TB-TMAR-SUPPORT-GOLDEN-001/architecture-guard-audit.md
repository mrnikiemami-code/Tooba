# Architecture Guard Audit

## Upgraded `SupportArchitectureGuardTests`
Enforces:
- Endpoints project exists; path↔namespace
- Endpoint → Application only (no Host/Infrastructure/AccessControl App/Domain)
- No `ISupportDirectory` in Endpoints; uses `ISender` + `ApiResponseFactory`
- Real MediatR Commands/Queries/Handlers; use-case folders
- No `SupportEndpoints` in Host; no SupportCommands/SupportQueries dumps
- No generic InvalidOperationException swallow / manual error JSON in Endpoints
- Contracts-only Notification boundary
- No TypeForwardedTo / root dump / clock-id bypass / silent catch / localized prose
- Host SupportDbContext allowlist includes SupportDevelopmentSeedHost + ProductWorkspaceDevelopmentBootstrap

## Verdict
**Support-Architecture-Guards: ENFORCED**
