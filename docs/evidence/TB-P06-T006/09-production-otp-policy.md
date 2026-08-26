# 09 — Production OTP policy (TB-P06-T006)

## Implementation

| Class | Path | Environment |
|---|---|---|
| `ProductionOtpSender` | `src/backend/Modules/Identity/Tooba.Identity.Infrastructure/ProductionOtpSender.cs` | Production |
| `CapturingOtpSender` | Identity.Infrastructure (existing) | Development, Testing, Staging |

## ProductionOtpSender behavior

```csharp
Task.FromException<InvalidOperationException>(
    new InvalidOperationException("identity.otp.delivery.unconfigured"));
```

Fail-closed: does **not** capture OTP in memory or log the code.

## IdentityModule wiring

```text
if (environment.IsProduction())
    IOtpSender -> ProductionOtpSender
else
    IOtpSender -> CapturingOtpSender
```

## Operational implication

Password-reset and identifier-verification flows that call `IOtpSender.SendAsync` will fail in Production until an external SMS/email provider implements `IOtpSender`.

This is intentional — silent dev capture must not ship to Production.

## Test proof

`AuthSecurityHttpTests.Production_otp_sender_is_fail_closed` — unit test on `ProductionOtpSender` asserts `identity.otp.delivery.unconfigured`.
