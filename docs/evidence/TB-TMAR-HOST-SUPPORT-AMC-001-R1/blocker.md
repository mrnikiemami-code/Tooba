# Blocker — Support AMC R1

Parent TB-TMAR-HOST-SUPPORT-AMC-001 left `SupportDevelopmentSeedHost` importing:

- `Tooba.AccessControl.Application.Development.Seller` (`ISellerDevContextStore`)
- `Tooba.AccessControl.Application.Models` (`IAccessControlDirectory`, `AccessOwnerScope`)
- `Tooba.AccessControl.Domain` (`AccessOwnerScopeKind`)

Host composition must not depend on foreign module Application/Domain authority.
