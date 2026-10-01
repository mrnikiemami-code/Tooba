# development-boundary — W4

Admin/Development allowlist (exact 2):

- `AdminDevActorBootstrap.cs`
- `AdminDevContextEndpoints.cs`

Namespace: `Tooba.Host.Admin.Development`

Depends on BuildingBlocks + Identity.Contracts (bootstrap) only.
Foreign `.Application` / `.Infrastructure` / `.Domain` / DbContext / joins = ZERO.
CQRS: `HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION` (no MediatR for this Host Development endpoint).
