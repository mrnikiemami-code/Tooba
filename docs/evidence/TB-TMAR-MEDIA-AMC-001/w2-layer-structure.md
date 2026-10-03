# TB-TMAR-MEDIA-AMC-001 — W2 Layer structure

Folder-Granularity-State (touched layers): PROFESSIONAL_SHALLOW

## Changes
- Domain: `MediaAsset.cs` → `Aggregates/` + `Enums/MediaAssetStatus`
- Application: `MediaContracts.cs` → `Ports/` + `Models/`
- Infrastructure: `MediaDirectory` → `Assets/`; `LocalFileMediaStore` → `Storage/`; Migrations → `Persistence/Migrations/`
- Contracts: `Errors/MediaErrorCodes.cs` shell (catalog wiring in W3)
- Path ↔ namespace EXACT for moved types

Durable guard: `MediaModuleAmcW2StructureGuardTests`
