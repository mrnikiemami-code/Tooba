# Tooba — Session, Token & Credential Lifecycle Foundation

Status:

```text
IN_PROGRESS — TB-P02-T004 awaiting Architect ACCEPT
```

Task:

```text
TB-P02-T004
```

```text
Authentication != Authorization
Session != User
Refresh credential != Password credential
Reset token != Login token
Verification challenge != MFA enrollment
```

This document locks the P02 session/credential lifecycle. Identity account model remains `37-identity-authentication-foundation.md`. SpiceDB remains `38-spicedb-authorization-foundation.md`. Party remains `39-party-organization-membership-foundation.md`.

## Session model

`AuthSession` is Identity-owned persistence in schema `identity` (`auth_sessions`).

Fields: SessionId, UserId, CreatedAt, ExpiresAt, LastUsedAt, RevokedAt, RevocationReason, CredentialVersion (copy of `UserAccount.SecurityStamp` at issue), Edition, TenantId (Single-Store only), optional ClientLabel, RefreshSecretHash, PreviousRefreshSecretHash, RefreshFamilyId.

Session is not a User, not a Party, and not an authorization tuple.

Marketplace sessions live in the marketplace identity database. Single-Store sessions live in that tenant’s identity database. Identity lifecycle code does not parse Host.

## Refresh secret storage and rotation

Refresh secrets are high-entropy `RandomNumberGenerator` values. Only SHA-256 hashes are stored. The raw secret is returned only on establish/rotate.

Successful refresh:

```text
refresh A presented
→ A hash matches current
→ current becomes previous
→ refresh B issued
```

If a rotated secret A is presented again, reuse is detected, all sessions for that User are revoked, and `refresh_reuse_detected` is recorded.

Revoked, expired, or stamp-mismatched sessions cannot refresh.

## Revocation

Supported:

- one session
- all sessions for a User
- security-sensitive events (password change, password reset completion, disable, lock)

`UserAccount.SecurityStamp` is the durable credential/session version. Bumping it makes prior session rows fail `CredentialVersion` checks even if a hash still existed.

## Access-token boundary

`IAccessCredentialBoundary` / `SessionAccessCredentialBoundary` exposes the session handle without minting a custom JWT. Future cookie, JWT, BFF, or IdP exchange can sit on the same session. No Tooba-invented JWT cryptography.

## Password reset

`RequestPasswordResetAsync` always returns a public accepted result. ChallengeId is populated only when a matching identifier exists (test/internal). Unknown accounts are not distinguishable on the public `Accepted` flag.

Reset secrets are hashed, expiring, purpose-bound (`PasswordReset`), and single-use. Completion replaces the password hash, bumps the security stamp, and revokes sessions. No real email/SMS provider.

## Identifier verification

Email/Phone verification uses purpose `IdentifierVerification`. Issuing a code does not mark the identifier verified. Completion is single-use and then sets `IdentifierVerificationState.Verified`.

## Durable OTP / challenges

`auth_challenges` replaces the production in-memory OTP store. `IOtpChallengeService` is implemented by `IdentityLifecycleService` against PostgreSQL.

Purposes are the closed `OtpPurpose` enum: Login, IdentifierVerification, PasswordReset, Mfa. Client-supplied arbitrary purposes are rejected.

Persisted: ChallengeId, optional UserId, IdentifierHash, Purpose, SecretHash, CreatedAt, ExpiresAt, ConsumedAt, LockedAt, AttemptCount.

Plaintext OTP/reset secrets are never persisted.

## Attempt limiting

`Identity:Lifecycle:MaxChallengeAttempts` (default 5) locks that challenge (`LockedAt`). Full commercial rate-limit/anti-fraud is deferred.

## Credential change

Authenticated password change verifies the current password, validates policy, replaces the hash, bumps `SecurityStamp`, revokes sessions, and records `password_changed`. Passwords and hashes are not logged.

## Tenant behavior

Tenant A cannot read Tenant B sessions or challenges because each Single-Store tenant uses its own database. Marketplace uses the marketplace database.

## Security audit / event seams

`IIdentitySecurityEventSink` records `session_created`, `session_revoked`, `password_changed`, `password_reset_completed`, `identifier_verified`, `refresh_reuse_detected` without secrets. This is not the technical log and not a full Security Audit store.

## Deferred

Keycloak/OIDC product, WebAuthn/passkeys, commercial rate limiting, login UI, JWT product format.
