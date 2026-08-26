# 13 — Database transport health proof (TB-P06-T003-R1)

## Liveness (`/health/live`)

Always returns OK — transport DB not probed on liveness.

## Readiness (`/health/ready`)

When `Tooba:Messaging:Enabled`:

- Validates messaging connection reference in PostgreSQL map
- `IBusControl.CheckHealth()` — unhealthy fails readiness
- Labels: `messaging-transport=postgresql-sql`, `messaging-schema={schema}`

When disabled: `messaging=disabled`, `messaging-transport=n/a`

## Config validation

`MessagingOptionsValidator` rejects non-PostgreSql transport values (RabbitMQ forbidden).

Production-aware validator registered via `IHostEnvironment`.
