# Tooba — TB-P02-GATE Evidence

Gate:

```text
TB-P02-GATE
```

Date:

```text
2026-08-23
```

Predecessor SHA:

```text
120086689101670f4758ee1206940dce88da16a0
```

Recommendation:

```text
P02_GATE_PASS
```

This is Cursor evidence for Architect review. It is not Architect ACCEPT and does not close P02.

## Validation (run during this Gate)

| Check | Result |
| --- | --- |
| `dotnet restore` Tooba.slnx | pass |
| `dotnet build` | pass, 0 warnings, 0 errors |
| `dotnet test` | pass, 95 passed, 0 failed, 0 skipped |
| PostgreSQL Testcontainers | exercised (Identity, Party, Outbox, MassTransit, Auth HTTP) |
| SpiceDB Testcontainers | exercised (`SpiceDbIntegrationTests`) |
| MassTransit SQL Transport tests | exercised |
| `npm ci` in `src/frontend` | pass |
| `npm run typecheck` | pass |
| `npm run lint` | pass |
| `npm run build` | pass (Next.js 15.5.23) |
| `git diff --check` | pass |

## Identity

- Typed identifiers with kind-specific normalization and uniqueness (`IdentityFoundationTests`).
- Passwords hashed with ASP.NET Identity hasher; plaintext is not persisted.
- Disabled/locked accounts cannot authenticate; public HTTP error is `identity.authentication.failed`.
- Identity module has no Party/Seller/Agency/Customer identifiers in source (architecture tests).
- No Authzed.Net reference in Identity projects.

## SpiceDB / Authorization

- Host adapter uses Authzed.Net 1.6.0; Domain/Application/ModuleContracts do not reference Authzed types.
- Real schema write, relationship write, and permission check covered by `SpiceDbIntegrationTests`.
- Allow / Deny / Unavailable mapped; non-Allow including unknown/conditional permissionship defaults to Deny (`SpiceDbAuthorizationAdapter`).
- Adapter fail-closed on outage (`AuthorizationDecision.Unavailable`).
- Authentication HTTP middleware does not call SpiceDB.

## Party / Membership

- Person/Organization are Party aggregates; UserId is an opaque link, not an Identity FK.
- Membership `RelationCode` is not a permission column.
- SpiceDB projection is Outbox-driven (`PartyMembershipProjectionHandler`), not in the Party SaveChanges transaction.

## Session / credentials

- Refresh stored as SHA-256 hash; rotation and reuse detection covered by `IdentityLifecycleTests`.
- Revoke current / revoke-all / security-stamp bump covered.
- Password reset and identifier verification are durable PostgreSQL challenges, hashed, expiring, single-use, attempt-limited.
- Public reset request does not leak account existence.

## Authentication HTTP

Routes under `/v1/auth`: register, login, refresh, logout, logout-all, password-reset request/complete, identifier-verification request/complete, password-change, me.

- Access token is the opaque session GUID, not a custom JWT.
- Enumeration-safe login and reset (`AuthenticationHttpTests`).
- Tenant from Host commerce resolution only; header/query/cookie/body tenant spoof → `identity.tenant.untrusted`.
- Tenant A session cannot authenticate on Tenant B.
- ProblemDetails and captured logs contain no passwords, refresh secrets, OTP, or Bearer values.
- `IAuthenticationThrottleSeam` is a no-op seam, not an anti-abuse product.

## Tenant / security

- Single-Store Host routing remains fail-closed for unknown hosts.
- Marketplace does not derive database from request Host.
- Technical JSON logs are not the Identity security-event sink.

## Package audit

| Package | Version |
| --- | --- |
| .NET (TFM) | net8.0 |
| EF Core | 8.0.11 |
| Npgsql | 8.0.7 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 |
| MassTransit | 8.5.10 |
| MassTransit.SqlTransport.PostgreSQL | 8.5.10 |
| Authzed.Net | 1.6.0 |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.15.3 |
| OpenTelemetry.Instrumentation.AspNetCore/Http/Runtime | 1.12.0 |
| Next.js | 15.5.23 (resolved); package.json `^15.1.6` |
| React | 19 (package.json `^19.0.0`) |
| Tailwind CSS | 3.4.17 (package.json `^3.4.17`) |

Absent: RabbitMQ transport packages, MassTransit 9, Redis authz cache, Keycloak/OIDC client packages.

## Persian documentation

- `Directory.Build.props`: `GenerateDocumentationFile=true`, `WarningsAsErrors` includes CS1591 for non-test projects.
- Host/module build in this Gate: 0 CS1591 warnings.
- EF generated exclusions remain narrow (`.editorconfig` / Migrations Designer + snapshot).

## Concern classification

| Item | Class |
| --- | --- |
| OTel exporter 1.15.3 vs instrumentation 1.12.0 | DEFERRED_NON_BLOCKING |
| `/__platform-*` diagnostic routes (Testing/Development) | DEFERRED_NON_BLOCKING |
| Config-backed tenant registry (not a control plane) | DEFERRED_NON_BLOCKING |
| Npgsql / MassTransit NodaTime wiring | DEFERRED_NON_BLOCKING |
| SQL Transport admin vs runtime credential split | DEFERRED_NON_BLOCKING |
| Durable Inbox / dedup beyond current outbox | DEFERRED_NON_BLOCKING |
| MassTransit delayed redelivery / scheduler | DEFERRED_NON_BLOCKING |
| Future T006 Outbox vs MassTransit EF Outbox | DEFERRED_NON_BLOCKING |
| Process-local cache until Redis | DEFERRED_NON_BLOCKING |
| Identity OTP sender is capture/fake | DEFERRED_NON_BLOCKING |
| Keycloak / OIDC | DEFERRED_NON_BLOCKING |
| WebAuthn / passkeys | DEFERRED_NON_BLOCKING |
| Rate-limit / anti-fraud product | DEFERRED_NON_BLOCKING |
| CONDITIONAL_PERMISSION caveats (mapped to Deny) | DEFERRED_NON_BLOCKING |
| Redis authorization cache | DEFERRED_NON_BLOCKING |
| Commercial login UI / Shopeiva / Data Grid / Design System | DEFERRED_NON_BLOCKING (mandatory before serious UI) |

No BLOCKER and no REPAIR_BEFORE_P03 found in this Gate pass.

## Mandatory future UX sequence (preserved)

Deep Shopeiva Study → reuse map → Design System → Professional Data Grid → workspace patterns → visual evidence → Architect visual ACCEPT.

Backend/module boundary is not the UI boundary. Weak UI remains a product failure, not a P02 code defect.

## Final recommendation

```text
P02_GATE_PASS
```

Architect ACCEPT is still required before P02 is COMPLETE and before any P03 envelope.
