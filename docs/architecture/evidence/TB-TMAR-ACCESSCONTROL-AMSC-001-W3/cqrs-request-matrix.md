# CQRS request → handler → validator matrix — AccessControl (W3)

## Foundation

| Aspect | State |
| --- | --- |
| MediatR version | 12.5.0 |
| Registration | `AddToobaCqrsFoundation` |
| Second/parallel dispatcher | `ZERO` |
| `IRequest` / `IRequest<Result<…>>` requests | 20 |
| Real `IRequestHandler<,>` implementations | 20 |
| Endpoint `ISender` dispatch references | 60 |
| Endpoint direct persistence/directory call | `ZERO` |
| Host bypass | `ZERO` |

## Endpoint-reachable request inventory (19)

| # | Request | Kind | Capability folder | Handler | Validator classification |
| --- | --- | --- | --- | --- | --- |
| 1 | `CreateRoleCommand` | Command | `Roles/Commands` | ✅ | `VALIDATOR_REQUIRED` |
| 2 | `UpdateRoleCommand` | Command | `Roles/Commands` | ✅ | `VALIDATOR_REQUIRED` |
| 3 | `CloneRoleCommand` | Command | `Roles/Commands` | ✅ | `VALIDATOR_REQUIRED` |
| 4 | `ArchiveRoleCommand` | Command | `Roles/Commands` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 5 | `GetRoleQuery` | Query | `Roles/Queries` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 6 | `ListRolesQuery` | Query | `Roles/Queries` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 7 | `SetRolePermissionsCommand` | Command | `Permissions/Commands` | ✅ | `VALIDATOR_REQUIRED` |
| 8 | `GetRolePermissionsQuery` | Query | `Permissions/Queries` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 9 | `ListPermissionCatalogQuery` | Query | `Permissions/Queries` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 10 | `ListSellerPermissionCatalogQuery` | Query | `Permissions/Queries` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 11 | `AssignRoleCommand` | Command | `Assignments/Commands` | ✅ | `VALIDATOR_REQUIRED` |
| 12 | `RemoveAssignmentCommand` | Command | `Assignments/Commands` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 13 | `ListAssignmentsQuery` | Query | `Assignments/Queries` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 14 | `SetSellerCeilingCommand` | Command | `Ceiling/Commands` | ✅ | `VALIDATOR_REQUIRED` |
| 15 | `GetSellerCeilingQuery` | Query | `Ceiling/Queries` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 16 | `GetEffectiveAccessQuery` | Query | `Access/Queries` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 17 | `SearchAccessUsersQuery` | Query | `Access/Queries` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 18 | `ListScopeResourcesQuery` | Query | `Access/Queries` | ✅ | `NO_VALIDATOR_REQUIRED` |
| 19 | `EnsureAccessControlBootstrapCommand` | Command | `Bootstrap/Commands` | ✅ | `NO_VALIDATOR_REQUIRED` |

## Additional non-HTTP-reachable request (1)

| # | Request | Kind | Capability folder | Handler | Reached by |
| --- | --- | --- | --- | --- | --- |
| 20 | `GetSellerDevContextsQuery` | Query | `Development/Seller` | ✅ | `Seller/Development/SellerDevContextEndpoints.cs` (`GET /dev-contexts`) |

`GetSellerDevContextsQuery` is a seller-development-only surface. It lives in the `Development`
capability and is guarded by a `IsDevelopment()` short-circuit in the endpoint. The
`AccessControlValidatorTests` 19-request inventory deliberately covers the production
Admin/Seller surface; this 20th request is an additional development-only surface with no
validator requirement (no transport input beyond the ambient seller identity).

## Result contract

Every request returns `Result` or `Result<…>`. Expected failures are produced once in
`Application/Composition/AccessControlOperation.ExecuteAsync` as
`Result.Failure<T>(new SemanticError(ex.Code))`; unexpected exceptions propagate. No handler
returns a raw HTTP result.

## Duplicate CQRS shape check

`ZERO`. The W1 rename (`CreateAccessRoleCommand`/`UpdateAccessRoleCommand`/`CloneAccessRoleCommand`
→ `CreateRoleRequest`/`UpdateRoleRequest`/`CloneRoleRequest`) removed the only case where a
transport body shared the MediatR command vocabulary. `*Command` is now owned exclusively by
`Application/**/Commands/`. Guarded by
`AccessControlModuleAmsc001W1MigrateGuardTests.AccessControl_has_no_transport_type_shadowing_cqrs_command_vocabulary`.

## Semantic Contracts ownership

`Tooba.AccessControl.Contracts` contains only stable boundary types: `Access/`,
`Development/`, `Enums/`, `Errors/`, `Readiness/`. There is **no** mixed Application
`*Contracts.cs` dump, and no Application type is required to be referenced by another module.
