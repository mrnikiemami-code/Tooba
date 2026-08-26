# 03 — OTP delivery inventory (TB-P06-T007)

## New abstractions

| Type | Path |
|---|---|
| `IOtpDeliveryProvider` | `src/backend/Modules/Identity/Tooba.Identity.Application/OtpDeliveryContracts.cs` |
| `OtpDeliveryMessage` / `OtpDeliveryOutcome` | same |
| `OtpDeliveryProviderSender` (`IOtpSender` adapter) | `src/backend/Modules/Identity/Tooba.Identity.Infrastructure/OtpDeliveryProviderSender.cs` |
| `OtpDeliveryOptions` | `src/backend/Modules/Identity/Tooba.Identity.Infrastructure/OtpDeliveryOptions.cs` |
| `OtpDeliveryInstrumentation` | `src/backend/Modules/Identity/Tooba.Identity.Infrastructure/OtpDeliveryInstrumentation.cs` |

## Provider implementations

| Provider | Path | Environment |
|---|---|---|
| `CapturingOtpDeliveryProvider` | `.../CapturingOtpDeliveryProvider.cs` | Non-Production |
| `FailClosedOtpDeliveryProvider` | `.../FailClosedOtpDeliveryProvider.cs` | Production default |
| `WebhookOtpDeliveryProvider` | `.../WebhookOtpDeliveryProvider.cs` | Production when Mode=Webhook |

## DI wiring

`IdentityModule.cs` registers provider by environment/mode; `IOtpSender` → `OtpDeliveryProviderSender`.

## Configuration

Section: `Identity:OtpDelivery` in `src/backend/Host/Tooba.Host/appsettings.json` and `appsettings.Production.json`.
