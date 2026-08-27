# 04 — Role model

Task: TB-P06-T024  
Status: LIVE (dynamic; no `if role ==` product engine)

## Domain

`src/backend/Modules/AccessControl/Tooba.AccessControl.Domain/AccessControlDomain.cs`

| Entity | Key fields |
|--------|------------|
| `AccessRole` | Id, TenantId, OwnerScopeKind (Platform\|Seller), OwnerScopeId, Name, Code, Description, IsSystem, IsMutable, IsArchived, CreatedAt, UpdatedAt |
| `RolePermission` | RoleId, PermissionId, ScopeKind, ScopeResourceId, Enabled |
| `UserRoleAssignment` | UserId, RoleId, OwnerScopeKind, OwnerScopeId, AssignedAt |
| `PlatformSellerCeiling` | SellerPartyId, PermissionId, Enabled |

## Persistence

- DbContext: `AccessControlDbContext` schema `access_control`
- Migration: `…/Persistence/Migrations/20260827140753_InitialAccessControl.cs`
- Directory SoT: `AccessControlDirectory` (PG config + SpiceDB sync)

## System roles (bootstrap)

| Code | Owner | Mutable |
|------|-------|---------|
| `platform-admin` | Platform | No (system) |
| `seller-owner` | per Seller | No (system) |

Custom roles: create / update / clone / archive via directory APIs. System roles reject mutate/archive.

## Owner isolation

Seller roles scoped by `OwnerScopeId = SellerPartyId`. Foreign owner → `access.role.not_found`.
