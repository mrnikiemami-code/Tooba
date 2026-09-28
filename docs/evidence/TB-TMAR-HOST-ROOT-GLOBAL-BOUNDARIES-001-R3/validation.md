# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R3 — Validation

## 1. Focused builds

| Project | Result |
| ------- | ------ |
| `Tooba.AccessControl.Contracts` | Build succeeded, 0 errors |
| `Tooba.AccessControl.Infrastructure` | Build succeeded, 0 errors |
| `Tooba.Order.Infrastructure` | Build succeeded, 0 errors |
| `Tooba.Host` | Build succeeded, 0 errors |
| `Tooba.Host.Tests` | Build succeeded, 0 errors |

## 2. Focused tests

| Filter | Result |
| ------ | ------ |
| `TmarCompleteReferenceStructureGateTests` (structure/manifest guard) | PASS |
| `HostRootGlobalBoundariesGuardTests` | PASS |
| `HostAuthorizationEvacuationGuardTests` | PASS |
| Host focused total | **16 / 16 PASS** |
| `OrderInfrastructureForeignLayerBoundaryGuardTests` | **4 / 4 PASS** |

## 3. Certification checks

| Check | State |
| ----- | ----- |
| `Access/AccessControlEffectiveAccessContracts.cs` namespace = `Tooba.AccessControl.Contracts.Access` | EXACT |
| namespace alias workaround | ZERO |
| duplicate contract file | ZERO |
| stale old namespace references (`Tooba.AccessControl.Contracts` outside `.Access` / assembly name / project path) | ZERO in sources |
| AccessControl module manifest entries | 1 (ONE) |
| manifest projects include `Tooba.AccessControl.Contracts` | YES |
| rootAllowlist matches disk | YES (`[]`) |
| `Tooba.AccessControl.Contracts` foreign Application/Infrastructure/Domain | ZERO |
| `Order.Infrastructure` foreign Application/Infrastructure/Domain | ZERO |
| Authorization 7-file Infrastructure slice present | YES (7 files) |
| `Host/Authorization` absent | YES |
| six Host root files absent | YES (all six `False`) |

## 4. Repair iterations

`MAX_REPAIR_ITERATIONS = 1` — no deterministic failure occurred; 0 repair iterations needed.
