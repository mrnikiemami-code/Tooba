# 12 — OTP delivery integration tests (TB-P06-T007)

## Test file

`src/backend/Host/Tooba.Host.Tests/OtpDeliveryProviderTests.cs`

## Cases

| Test | Assertion |
|---|---|
| `Capturing_provider_succeeds_without_logging_code` | `OtpDeliveryOutcomeKind.Succeeded`; code in `LastCode` for test only |
| `Fail_closed_provider_returns_misconfigured` | `OtpDeliveryOutcomeKind.Misconfigured` |
| `Sender_maps_misconfigured_to_identity_error_code` | `InvalidOperationException` message `identity.otp.delivery.unconfigured` |

## Run

```powershell
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~OtpDeliveryProviderTests
```

## Notes

- Tests exercise provider layer directly; no OTP code in assertion output beyond controlled `LastCode` property.
- Webhook provider covered by unit/integration wiring in Production config path (manual ops verification).
