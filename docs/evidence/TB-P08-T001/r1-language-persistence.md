# TB-P08-T001-R1 — Language Persistence

- New module `src/backend/Modules/Localization/` (Domain/Application/Infrastructure).
- Table `localization.languages` with UUID keys, unique `code` + `url_prefix`, direction/calendar enums, audit timestamps.
- Migration `20260901220000_InitialLocalization.cs` creates schema + outbox table.
- `LocalizationModule` registered before Content in `ToobaModuleComposition`.
- Removed in-memory `SupportedLocaleRegistry`; `ILanguageDirectory` is authoritative.
