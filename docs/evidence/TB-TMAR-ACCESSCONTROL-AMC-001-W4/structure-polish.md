# TB-TMAR-ACCESSCONTROL-AMC-001-W4 — Structure polish (Domain / Models / Endpoints Domain ZERO)

Mode: MIGRATE  
Slice: STRUCTURE_POLISH  
Parent: TB-TMAR-ACCESSCONTROL-AMC-001-W3

## Changes

- Domain god file removed; entities under `Domain/Aggregates/` with Contracts enums
- `AccessOwnerScopeKind` / `AccessScopeKind` live in `Contracts/Enums` (shared; Access contracts consume them)
- Application `AccessControlContracts.cs` dump split: `Models/AccessControlDtos.cs`, `Ports/IAccessControlDirectory.cs`, `Exceptions/AccessControlException.cs`
- Endpoints: **Domain ZERO** — only `Contracts.Enums` for owner/scope kinds; no Domain project reference

## Microservice

Enums and error codes stay in Contracts; Endpoints/Application no longer require Domain assembly at the HTTP boundary.
