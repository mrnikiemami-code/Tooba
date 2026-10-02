# options-retry — TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT

MessagingHostOptions fields/defaults certified: Enabled, Transport, ConnectionReference, Schema, UseInProcessTestDouble, CanonicalTransport=PostgreSql.

Validator certified: Enabled+test-double conflict; disabled success; PostgreSql/PostgreSQL only; ConnectionReference required when enabled; reserved schemas rejected; redundant Production duplicate branch absent.

Retry certified: Immediate(2) + intervals 5s/15s/30s; no infinite retry; global consumer transport policy only.
