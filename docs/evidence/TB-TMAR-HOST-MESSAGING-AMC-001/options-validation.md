# options-validation — TB-TMAR-HOST-MESSAGING-AMC-001

## MessagingHostOptions fields

| Field | Class | Secret? |
|---|---|---|
| Enabled | Host deployment | no |
| Transport | protocol name (`PostgreSql`) | no |
| ConnectionReference | config key (not connection string) | non-secret key; string resolved elsewhere |
| Schema | infra schema name | non-secret topology |
| UseInProcessTestDouble | Testing gate | no |
| CanonicalTransport | const | no |

## Validator checks

- Enabled + test double conflict → Fail
- Disabled → Success early
- Transport must be PostgreSql/PostgreSQL (RabbitMQ forbidden)
- ConnectionReference required when Enabled
- Schema forbidden business names (catalog/identity/pricing/platform_probe)
- Production + empty ConnectionReference → Fail (**redundant** after prior required check when Enabled)

Hard-coded validator strings = **startup/operator configuration** prose (not user-facing runtime presentation).

## Fail-closed selection

Documented tree in `AddToobaIntegrationPublisher` matches Architect expectations; no silent in-process fallback.
