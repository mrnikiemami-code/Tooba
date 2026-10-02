# Capability map

| Capability | Commands | Queries | Validators |
| --- | --- | --- | --- |
| Roles | CreateRole, UpdateRole, CloneRole, ArchiveRole | GetRole, ListRoles | Create/Update/CloneRole |
| Assignments | AssignRole, RemoveAssignment | ListAssignments | AssignRole |
| Permissions | SetRolePermissions | GetRolePermissions, ListPermissionCatalog, ListSellerPermissionCatalog | SetRolePermissions |
| Ceiling | SetSellerCeiling | GetSellerCeiling | SetSellerCeiling |
| Access | (none) | GetEffectiveAccess, SearchAccessUsers, ListScopeResources | (none) |
| Bootstrap | EnsureAccessControlBootstrap | (none) | (none) |
| Development/Seller | (dev store + query) | GetSellerDevContexts | (none) |

Shared roots retained: Authorization, Composition, Exceptions, Models, Ports, Validators (FluentRules/ValidationCodes), Permissions/PermissionCatalog.cs
