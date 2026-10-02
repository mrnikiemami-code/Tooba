# exception-boundary — TB-TMAR-HOST-PERSISTENCE-AMC-001

## Current throw

```text
PlatformHttpException(503, "Service Unavailable", "platform.connection.unconfigured")
```

## Coupling verdict

**ACCEPTED_AS_CANONICAL_HOST_PLATFORM_FAIL_CLOSED**

Reasons:

1. BuildingBlocks `IDatabaseConnectionResolver` XML docs **require** `PlatformHttpException` for fail-closed.
2. `PlatformHttpException` is the platform HTTP-mappable technical seam (not domain `SemanticException`).
3. `SafeErrorMapper` / `ExceptionPresentationService` / `ApiResponseFactory.FromPlatformException` map by `ErrorCode` + status — not by prose.
4. Same pattern is used by `ToobaNpgsql` and Outbox interceptor for other `platform.*` codes.
5. Moving to a parallel neutral config exception would invent a second platform failure type without clearing HTTP mapping debt.

## HTTP vs background

| Path | Behavior |
| --- | --- |
| HTTP (tenant middleware / module endpoints) | Mapped to ProblemDetails via presentation pipeline |
| Background (Outbox / messaging startup) | Exception type carries status/title/code; workers isolate/log by type — title is operator metadata, not connection leakage |
| MigrationRunner | Process tooling — fail-closed before DDLs |

HTTP title/prose does **not** embed connection strings or references.

## Hard-coded title

`"Service Unavailable"` = HTTP presentation title constant (English operator/canonical title), **not** localized user copy.

Localized user-facing text comes from Foundation catalog key `platform.connection.unconfigured` (EN/FA resx) when presentation resolves the code.

Quality note (non-blocking for KEEP): title string is duplicated across platform throw sites; CERT may optionally tighten, but Analyze does **not** require W1 exception-type rewrite.

## Exception message classification

Required ZERO of:

- `ex.Message` branching
- `Message.Contains` / `StartsWith` on parser text
- Interpreting Npgsql parser messages

**State: ZERO** (catch discards `ArgumentException` details).
