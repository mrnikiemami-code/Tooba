# 04 — Production OTP provider policy (TB-P06-T007)

## Mode matrix

| Mode | Environment | Provider | Behavior |
|---|---|---|---|
| `Capturing` | Development | `CapturingOtpDeliveryProvider` | In-memory capture for dev/test |
| `Disabled` | Production (default) | `FailClosedOtpDeliveryProvider` | Returns `Misconfigured` → `identity.otp.delivery.unconfigured` |
| `Webhook` | Production (configured) | `WebhookOtpDeliveryProvider` | POST JSON to `WebhookUrl` with optional Bearer `WebhookApiKey` |

## Error code mapping (`OtpDeliveryProviderSender`)

| Outcome | Host error code |
|---|---|
| Misconfigured | `identity.otp.delivery.unconfigured` |
| RateLimited | `identity.otp.delivery.rate_limited` |
| InvalidDestination | `identity.otp.delivery.invalid_destination` |
| Unavailable | `identity.otp.delivery.unavailable` |

## Production config (`appsettings.Production.json`)

```json
"OtpDelivery": {
  "Mode": "Disabled",
  "WebhookUrl": "",
  "WebhookApiKey": "",
  "TimeoutSeconds": 10
}
```

Set `Mode=Webhook` and env-inject `WebhookUrl` / `WebhookApiKey` before enabling OTP flows in Production.

## Observability

Metric: `tooba.identity.otp.delivery` — tag `outcome` only; no destination or OTP code logged.
