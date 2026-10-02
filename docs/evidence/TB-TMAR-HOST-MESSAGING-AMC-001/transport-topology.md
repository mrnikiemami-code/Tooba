# transport-topology — TB-TMAR-HOST-MESSAGING-AMC-001

## SQL Transport registration (MessagingRegistration)

- `SqlTransportOptions` via `IDatabaseConnectionResolver` + ConnectionReference + Schema mapper
- `AddPostgresMigrationHostedService` (CreateDatabase=false, CreateInfrastructure=true)
- `MassTransitHostOptions` WaitUntilStarted + StopTimeout 30s
- Singleton `NpgsqlDataSource` from SqlTransportOptions connection string
- `AddMassTransit` + `UsingPostgres` + consumer endpoint `tooba-integration` + AutoStart
- Retry: `MessagingRetryConfigurator` Immediate(2) + intervals 5s/15s/30s — consumer only; no infinite retry in Messaging

## Envelope (Host/Transport)

`ToobaIntegrationTransportMessage`: EventType, Version, EventId, OccurredAt, TenantId, Edition, DeploymentId, CorrelationId, PayloadJson.

MassTransit publisher builds envelope + headers (event-type, tenant, edition, deployment, event-id, correlation, traceparent/tracestate). No payload in headers. No connection refs/secrets in headers.

## Serialization

`IIntegrationEventSerializer` / `JsonIntegrationEventSerializer` owned by Persistence; Messaging consumes only.
