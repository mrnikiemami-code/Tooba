# TB-P09-T002-R1 — Actor resolution

## Semantics

| Kind | `actorDisplayName` | `actorDisplayFa` | `actorDisplayEn` |
|------|--------------------|------------------|------------------|
| system | سیستم | توسط سیستم | By system |
| user (resolved) | {display} | توسط {display} | By {display} |
| user (missing) | کاربر نامشخص | توسط کاربر نامشخص | By unknown user |

Fallback order (Host): usable OperatorProfile.DisplayName → First+Last → Identity email → mobile → missing fallback.

Unusable DisplayName values that are only `?` / whitespace are skipped so corrupted encoding does not block email fallback.

## DTO

Notes/history now include `actorKind`, `actorDisplayName`, plus existing `actorDisplayFa` / `actorDisplayEn`.
`createdByUserId` remains for internal/compatibility only; FE continues to render `actorDisplayFa`.

## Tests

`AdminOrderCompletenessTests`:

- system / resolvable / missing labels without GUID or truncated id
- repeated actor ids reuse one map entry
