# 01 — Recovery classification (TB-P06-T003-R1)

Predecessor: `7fca9aef27df5c55286d2b0c8b247cedbd4241a2`

| Path | Tracked | Summary | RabbitMQ? | Party inbox? | Unrelated? | Action |
|---|---|---|---|---|---|---|
| docker-compose.yml | M | rabbitmq service | Yes | No | No | `git checkout` |
| Tooba.Host.csproj | M | MassTransit.RabbitMQ pkg | Yes | No | No | reverted |
| Tooba.Host.Tests.csproj | M | Testcontainers.RabbitMq | Yes | No | No | reverted |
| MessagingRegistration.cs | M | UsingRabbitMq branch | Yes | No | No | reverted |
| MessagingHostOptions.cs | M | RabbitMq options | Yes | No | No | reverted |
| HostReadinessEvaluator.cs | M | rabbitmq readiness | Yes | No | No | reverted |
| Program.cs | M | validator DI | Partial | No | No | reverted then re-applied correctly |
| appsettings*.json | M | RabbitMq sections | Yes | No | No | reverted |
| MassTransitFoundationTests.cs | M | expect RabbitMQ pkg | Yes | No | No | reverted |
| MassTransitPostgresTests.cs | M | transport param | Yes | No | No | reverted |
| PartyMembershipProjectionHandler.cs | M | inbox dedup | No | Yes | No | reverted |
| PartyDbContext.cs | M | membership_inbox | No | Yes | No | reverted |
| MessagingTransportKind.cs | ?? | enum | Yes | No | No | deleted |
| RabbitMqHostOptions.cs | ?? | config | Yes | No | No | deleted |
| MessagingRetryConfigurator.cs | ?? | retry helper | No | No | No | deleted (re-created in R1) |
| PartyMembershipInboxRecord.cs | ?? | inbox entity | No | Yes | No | deleted |
| 20260826220000_PartyMembershipInbox.cs | ?? | migration | No | Yes | No | deleted |
| host-dev*.log / next-dev*.log | ?? | dev logs | No | No | Yes | kept untracked |

No unrelated preexisting user work found. No RECOVERY_CONFLICT.
