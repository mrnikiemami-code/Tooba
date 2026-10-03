# TB-TMAR-LOCALIZATION-AMC-001 — W2 Layer structure

Folder-Granularity-State (touched layers): PROFESSIONAL_SHALLOW

## Changes
- Domain: `Language.cs` → `Aggregates/` + `Enums/LanguageDirection` + `Enums/LanguageCalendarPolicy`
- Application: `LanguageContracts.cs` → `Ports/` + `Models/` + `Composition/LanguageMappings`
- Infrastructure: `Languages/`, `Adapters/`, `Bootstrap/`, Outbox → `Persistence/`
- Contracts: ports → `Ports/` (namespace `Tooba.Localization.Contracts.Ports`); Errors shell retained
- Consumer usings updated (Contracts.Ports / Application.Models|Ports) — Contracts-only boundary preserved

Durable guard: `LocalizationModuleAmcW2StructureGuardTests`
