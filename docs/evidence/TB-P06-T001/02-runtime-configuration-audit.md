# 02 — Runtime configuration audit (TB-P06-T001)

| Key / section | Classification | Notes |
|---|---|---|
| `Tooba:Edition` | ENV_REQUIRED (Production) | `Unset` allowed only non-Production |
| `Tooba:PostgreSQL:ConnectionReferences` | SECRET_REQUIRED / ENV_REQUIRED | Values via env override; never in git for prod |
| `Tooba:Messaging:*` | PRODUCTION_REQUIRED when enabled | ConnectionReference + schema |
| `Tooba:Authorization:SpiceDb:Token` | SECRET_REQUIRED | Required when Mode=SpiceDb |
| `Tooba:Observability:OtlpEndpoint` | ENV_REQUIRED (prod observability) | Empty = no export (safe default) |
| `Tooba:TrustedProxies` | PRODUCTION_REQUIRED behind proxy | Empty = no forwarded headers |
| `appsettings.Development.json` credentials | DEV_ONLY | Local Postgres passwords; not production |
| `Identity:*` | SAFE_DEFAULT | Session/challenge policy |
| `Tooba:Cache` | SAFE_DEFAULT | Memory provider |
| MassTransit SQL transport | PRODUCTION_REQUIRED when messaging enabled | Auto infra migration hosted service |
| Frontend `TOOBA_HOST_ORIGIN` | ENV_REQUIRED | Server-only rewrite target; see `.env.example` |

Hierarchy: `appsettings.json` → `appsettings.{Environment}.json` → environment variables (`Tooba__Section__Key`).

No secrets recorded in this evidence file.
