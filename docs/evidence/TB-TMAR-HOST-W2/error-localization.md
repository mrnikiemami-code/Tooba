# TB-TMAR-HOST-W2 Error / Localization

NEW Application code uses FluentValidation error codes (`menu.title.required`, `menu.key.required`).
Directory preserves existing PlatformHttpException semantic codes (menu.key.duplicate, menu.missing, …).
No new hardcoded Domain user-facing localized messages introduced beyond moving existing Host exception strings into Infrastructure Directory (same codes/messages as before; not a Domain layer addition).
Unlimited-locale-safe: Host/API continues ProblemDetails mapping via existing error codes.
