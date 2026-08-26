# 11 — Endpoint naming contract (TB-P06-T003-R1)

| Name | Type | Purpose |
|---|---|---|
| `tooba-integration` | Receive endpoint | Single durable adapter for all integration events |

Requirements met:

- Deterministic (`MessagingRegistration.IntegrationEndpointName` constant)
- Environment-safe (not hostname/tenant derived)
- Module handlers dispatched inside adapter (extraction-ready envelope)
- Maps to SQL transport subscription for `ToobaIntegrationTransportMessage`

No rename performed — avoids orphaning production messages.
