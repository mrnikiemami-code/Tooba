# Tooba — Identity & Authentication Foundation

Status:

```text
IN_PROGRESS — TB-P02-T001 awaiting Architect ACCEPT
```

Task:

```text
TB-P02-T001
```

```text
Authentication != Authorization
Identity/User != Party/Organization
```

This document locks the P02 Identity implementation. P00 design remains in `docs/architecture/04-identity-authentication.md`. SpiceDB remains `docs/architecture/05-spicedb-authorization.md`. Party remains `docs/architecture/06-party-organization-b2b-foundation.md`.

## Identity vs Party

`UserAccount` is the login principal. It has no CustomerId, SellerId, OrganizationId, address, loyalty, tax, or company fields. Future commerce linkage is an explicit Party/Membership contract, not a column on User.

User account != Customer profile != Seller organization != Tenant.

## User aggregate

`UserId` (UUID v7), `Status` (`Active` / `Disabled` / `Locked`), `CreatedAt`, `UpdatedAt`, optional password credential, owned login identifiers, optional MFA enrollments, optional external IdP bindings.

Disabled and locked users must not authenticate.

## Login identifiers

Identifiers are first-class rows (`login_identifiers`), not fixed Username/Email/Phone columns on User.

Kinds now: Username, Email, Phone. Reserved for later: NationalId, ExternalProvider.

Each row: type, display value, type-specific normalized value, verification state, preferred marker, timestamps.

## Normalization

Type-specific and testable:

- Email: trim + invariant lower; no provider-specific plus/dot collapsing.
- Username: trim + invariant lower (not email rules).
- Phone: optional leading `+` plus digits only; no Iran country default.

## Uniqueness scope

Unique index on `(Kind, NormalizedValue)` inside the Identity schema of **the current database**.

- Marketplace: one marketplace database → marketplace-wide uniqueness.
- SingleStore: identity tables live in each tenant database → uniqueness is per tenant store.

Hostname is not an identity scope. Identity code does not parse Host; connection comes from existing commerce context.

## Password credential

ASP.NET Core `PasswordHasher<object>` (PBKDF2 via Identity.Core). Only hash metadata is stored. Plaintext, hash, OTP, and tokens are not logged.

`SuccessRehashNeeded` upgrades the stored hash on successful login.

Policy is `Identity:PasswordPolicy` (minimum length now; complexity / breach / history deferred).

## OTP / MFA / external IdP / session

- `IOtpChallengeService` + `IOtpSender` with `OtpPurpose` (login, identifier verification, password reset, MFA). In-memory/fake sender only.
- `IMfaEnrollmentStore` records OTP/TOTP/WebAuthn/external step-up enrollments without UI.
- `IExternalIdentityDirectory` maps `issuer + subject` → internal `UserId`. No Keycloak package.
- `AuthenticationTicket` is an internal session handle (UserId + SessionHandle). No custom JWT.

## Tenant / edition

Identity DbContext uses `ToobaNpgsql.ResolveForContext`. Marketplace identity is in the marketplace DB. SingleStore identity is in that tenant’s DB. No shared SingleStore identity database.

## Audit seam

`IIdentitySecurityEventSink` records login success/failure, credential change, account lock/disable. This is not the technical log and not a full Security Audit store.

## Authorization handoff

Authentication result exposes stable `UserId` for later SpiceDB. This module does not implement roles, RBAC-as-final-model, or SpiceDB schema.

## Persistence

Module-owned `IdentityDbContext`, schema `identity`, Identity-owned migrations. No cross-module FK. Outbox translates only `UserRegisteredDomainEvent` → `identity.user_registered.v1`.

## Deferred

SpiceDB; Party module; Keycloak; real SMS/email; WebAuthn UI; identifier-verification-required login policy; durable OTP store; cookie/refresh token issuance; password history / HIBP.
