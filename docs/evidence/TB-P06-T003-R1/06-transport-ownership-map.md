# 06 — Transport ownership map (TB-P06-T003-R1)

| Layer | Owner | Notes |
|---|---|---|
| SQL transport tables | MassTransit infrastructure (`transport` schema) | Created by `AddPostgresMigrationHostedService` |
| Module business schemas | Each module Infrastructure | No transport table ownership |
| Module outbox tables | Each module Infrastructure | `{schema}.outbox_messages` |
| Host adapter | Tooba.Host | `ToobaIntegrationTransportMessage` envelope only |

Forbidden: business modules querying/joining transport tables; cross-module transport SQL.
