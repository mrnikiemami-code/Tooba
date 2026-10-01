# Blocker — Localization AMC-001-R1

LocaleAdminEndpoints classified business failures via exception message text:

- `InvalidOperationException` → `ioe.Message`
- `StartsWith("localization.language.")`

This is message-text classification and is forbidden under TMAR failure-semantics locks.
