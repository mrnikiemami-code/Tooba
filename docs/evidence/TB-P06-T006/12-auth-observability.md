# 12 — Auth observability (TB-P06-T006)

## Metric

| Name | Type | Tags |
|---|---|---|
| `tooba.authentication.event` | Counter | `outcome`, `operation` |

Implementation: `AuthenticationInstrumentation` (`src/backend/Host/Tooba.Host/AuthenticationInstrumentation.cs`)

Meter: `ToobaTelemetry.Meter` (shared OpenTelemetry pipeline in `Program.cs`).

## Recorded outcomes

| Method | outcome | operation |
|---|---|---|
| `RecordThrottled(operation)` | `throttled` | e.g. `login`, `refresh` |
| `Record(outcome, operation)` | caller-defined | extensible |

Rate-limit exceed path calls `RecordThrottled` from `AuthenticationRateLimitThrottleSeam`.

## Secret safety

Class documentation: *"متریک‌های احراز بدون password/token/OTP."*

No metric dimensions include identifiers, IP addresses, or credential material.

## OpenTelemetry export

When `Tooba:Observability:EnableMetrics=true` and OTLP endpoint configured, meter exported via `AddMeter(ToobaTelemetry.MeterName)`.

Login success/failure uses structured log events only (`Tooba.Auth` logger), not the authentication event counter in current implementation.
