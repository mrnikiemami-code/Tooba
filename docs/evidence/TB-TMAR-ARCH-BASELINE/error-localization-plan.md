# Error + Localization Plan

Anti-pattern: `InvalidOperationException("…Persian…")` in Domain.

Target:

- semantic `ErrorCode` (e.g. `catalog.category.invalid_slug`)
- typed Domain/Application errors/results
- Host maps to localized ProblemDetails for unlimited locales
- FluentValidation messages via pipeline localization

Unlimited locales — not FA/EN-only design.
