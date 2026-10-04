# Persistence / schema safety — AccessControl (W3)

## Classification

| Aspect | State |
| --- | --- |
| Module DbContexts | 1 (`AccessControlDbContext`) |
| Module schema | `access` (module-owned) |
| Migrations owned by module | ✅ (`Persistence/Migrations/`) |
| Cross-module FK | `ZERO` |
| Cross-module EF join | `ZERO` |
| Application DbContext access | `ZERO` |
| Endpoints DbContext access | `ZERO` |
| Foreign `DbSet` / foreign schema read | `ZERO` |
| `ARCH-DATA-001` | intact |

## Owned entities and tables

| Entity | Table |
| --- | --- |
| `AccessRole` | `roles` |
| `RolePermission` | `role_permissions` |
| `UserRoleAssignment` | `user_role_assignments` |
| `PlatformSellerCeiling` | `platform_seller_ceilings` |
| `AccessAuditEvent` | `access_audit_events` |

All five are same-module aggregates in `Domain/Aggregates/`. No `modelBuilder.Entity<…>` maps a
foreign aggregate, and no `HasOne`/`HasMany` navigates to a foreign table.

## Migrations (unchanged)

| File | Bytes / role |
| --- | --- |
| `20260827140753_InitialAccessControl.cs` | initial schema |
| `20260827140753_InitialAccessControl.Designer.cs` | generated designer |
| `20260827181000_AddSellerCeilingScope.cs` | seller ceiling scope |
| `AccessControlDbContextModelSnapshot.cs` | generated snapshot |

## Schema preservation proof

| Check | Result |
| --- | --- |
| Migration identifiers changed | no |
| Migration order changed | no |
| `Up`/`Down` semantics changed | no |
| Snapshot semantics changed | no |
| Tables / columns / indexes / constraints changed | no |
| Transaction behaviour changed | no |
| New migration added by the AMSC run | no |

`git diff a3ba1a4f HEAD -- …/Persistence/Migrations/` is **empty**. During W1 an EF model
re-generation was attempted as a by-product of the namespace moves and was **reverted**
(`git checkout HEAD -- …/Persistence/Migrations/`) before the W1 commit, precisely to keep schema
identical. Verified: the W1 commit `c9e009f8` contains no migration change.

## Application / Endpoints isolation

- No `AccessControlDbContext` reference exists in `Tooba.AccessControl.Application` or
  `Tooba.AccessControl.Endpoints`.
- Application reaches persistence only through the `IAccessControlDirectory` port
  (`Application/Ports/`), implemented in `Infrastructure/Directories/AccessControlDirectory.cs`.
- Endpoints dispatch exclusively via `ISender`.

## Note on the size-baseline path

The size baseline still lists
`src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlDirectory.cs`
(without `Directories/`). That path does **not** exist on disk. This is a stale *baseline* entry, not
a stale physical copy: the real file lives only at
`.../Infrastructure/Directories/AccessControlDirectory.cs` and there is no duplicate. See
`residual-debt.md`.
