# Message classification ZERO — Localization AMC-001-R1

LocaleAdminEndpoints source contains ZERO of:

- `ex.Message` / `ioe.Message` code selection
- `StartsWith` / `Contains` on exception text
- `TryMapLanguageFault`
- broad `InvalidOperationException` business remap

Guards: HostLocalizationAmcGuardTests + LocalizationFailureSemanticsTests.
