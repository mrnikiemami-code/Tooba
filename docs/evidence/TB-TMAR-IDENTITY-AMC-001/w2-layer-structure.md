# TB-TMAR-IDENTITY-AMC-001-W2 — Layer structure

Folder-Granularity-State (touched layers): PROFESSIONAL_SHALLOW (Domain/Application/Contracts)

## Changes
- Domain: `IdentityDomain.cs` → `Aggregates/`, `Enums/`, `Rules/`, `Events/`
- Application: `IdentityContracts.cs` → `Ports/`, `Options/`, `Models/`
- Contracts: root actor dumps → `Contacts/`, `Actors/`
- Path ↔ namespace EXACT
- Behavior/schema unchanged

Durable guard: `IdentityModuleAmcW2StructureGuardTests`
