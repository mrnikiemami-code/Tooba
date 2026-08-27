# 05 — Provider secure configuration

**Task:** TB-P06-T022  
**Section:** `Payment:Gateway`

## Required production secrets / URLs

| Setting | Required for Webhook mode | Exposed to browser? |
|---|---|---|
| `WebhookSigningSecret` | Yes | No |
| `InitiateBaseUrl` | Yes | Only resulting redirect URL (not secret) |
| `StatusQueryBaseUrl` | Yes | No |
| `StatusQueryApiKey` | Optional | No |
| `AllowedStatusQueryHosts` | Recommended for harness | No |

## Fail-closed rules

1. Production default `Mode=Disabled` → `FailClosedPaymentGateway` throws `payment.gateway.unconfigured`.
2. `Mode=Webhook` without all three required fields → Initiate throws `payment.gateway.unconfigured`; Verify returns `GATEWAY_MISCONFIGURED`.
3. Sandbox / Fake gateways are **not** registered in Production (`PaymentModule` branch).
4. Missing webhook signing secret rejects callbacks (fail-closed authenticity).

## SSRF / outbound safety

- Absolute http(s) URLs only.
- Without allowlist: loopback/private hosts rejected; https required.
- With `AllowedStatusQueryHosts`: only listed hosts accepted.

## Logging / browser

- Secrets must not appear in storefront payloads or admin JSON.
- Metrics use outcome tags only (see observability evidence).
- No secrets invented or printed in this evidence pack.

## Environment separation

| Environment | Mode | Providers |
|---|---|---|
| Development / Testing | Sandbox | Fake (+ fake refund) |
| Production | Disabled (until configured) | FailClosed; Webhook when Mode=Webhook + secrets |
