# Tooba — Identity & Authentication Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T005
```

Documentation only. Do not treat this file as a schema, Keycloak plan, or SpiceDB model.

Related:

```text
docs/architecture/01-capability-domain-map.md
docs/architecture/02-edition-tenant-deployment.md
docs/architecture/03-data-ownership-and-module-contracts.md
```

Hard rules preserved:

```text
Identity != Party
Authentication != Authorization
Login Identifier != Authentication Method
```

## A. Terminology & Separation of Concerns

| Term | Meaning |
| --- | --- |
| Identity | Login principal: the subject that can authenticate. Not a customer profile and not an organization. |
| Party | Business-domain person or organization (profile, membership, commercial actor). Not a password row. |
| Account | Lifecycle wrapper around an Identity (enabled/disabled, created/revoked). Not a store or company. |
| Login Identifier | Typed, normalizable handle used to *find* an Identity (username, email, phone, future types). |
| Credential | Secret or proof material bound to an Identity for a method (password hash, OTP challenge, IdP binding). |
| Authentication Method | How proof is produced (password, OTP, external IdP, future passkey). Independent of identifier type. |
| Verification | Proof that an identifier is under the claimant's control. Separate from identifier existence. |
| MFA Factor | Additional independent factor after primary authentication. Not the same as OTP-as-primary-login. |
| Session | Server-recognized authenticated period, independent of the credential that created it. |
| Device | Optional label/metadata for a session family; not a second Identity. |
| External Identity | Binding to an IdP using stable `issuer + subject`, not email-as-key. |
| Authorization | What an authenticated subject may do. Owned by SpiceDB / Party relationships, not Identity. |

```text
Authentication != Authorization
```

SpiceDB answers authorization. It does not validate passwords, OTPs, or IdP tokens.

```text
Identity != Party
```

A person or organization in commerce must not be reduced to an authentication record.

## B. Identity Ownership

Identity owns conceptually:

```text
identity/account lifecycle
login identifiers
credential references (never reversible secrets)
verification state of identifiers
authentication method enrollments
MFA enrollment
session lifecycle
external identity bindings
security events
recovery mechanisms
```

Identity must **not** own:

```text
customer profile business data
organization business data
pricing
orders
seller commercial data
SpiceDB relationship graph as business authorization truth
```

Party owns business identity/profile concepts. Linking is an explicit contract (`IdentityId` ↔ `PartyId`), not table sharing.

## C. Extensible Login Identifier Model

Confirmed: users must be able to authenticate with username, email, phone, and future identifiers (example: national ID).

Do **not** implement login as fixed columns `UserName` / `Email` / `Phone` with hardcoded branching.

Conceptual model (not a persistence schema):

| Aspect | Role |
| --- | --- |
| Identifier Type | Registered type key (email, phone, username, national_id, …) |
| Normalized Value | Canonical lookup key after type-specific normalization |
| Display Value | What the user entered / should see |
| Verification State | Unverified / verified / revoked / pending reverification |
| Primary / Preferred | Optional UX hint; not a uniqueness substitute |
| Uniqueness Policy | Per type and identity namespace (see below) |
| Tenant / Deployment Scope | Which identity namespace the identifier lives in |
| Created / Revoked | Lifecycle; revoked identifiers must not authenticate |

Lookup is: trusted context + type + normalized value → Identity (or none). Adding a type is a handler registration, not a core rewrite.

### Uniqueness scope

Possible scopes:

```text
global
per deployment
per tenant/store
per identifier type
```

**Direction (not invented product policy):** identifier uniqueness is never installation-global across unrelated Tooba deployments. Within one deployment, uniqueness is at least **per identifier type** inside the **identity namespace** of that request (see L). Exact rules (whether Marketplace emails are unique marketplace-wide; whether Single-Store emails collide across stores; whether username is unique per tenant) are:

```text
NEEDS_LATER_P00_DETAIL
```

Do not treat “same email string” as “same Party” across stores unless later business policy says so.

## D. Identifier Normalization

Each type has its own normalizer. Do not fold all types through one generic string transform.

Examples:

```text
email -> case/domain normalization policy
phone -> E.164-style canonicalization
username -> canonical case/Unicode policy
national ID -> country-specific validation/normalization
```

Conceptual strategy:

```text
ILoginIdentifierTypeHandler
```

Responsibilities (conceptual): recognize/parse input, normalize, validate shape, contribute uniqueness key, optionally contribute verification channel hints. Names are conceptual, not implementation requirements.

National ID and similar types are sensitive (see S).

## E. Authentication Methods

Methods are independent of identifiers:

```text
Password
OTP
External Identity Provider
Future Passkey/WebAuthn
Future Enterprise Federation
```

```text
Login Identifier != Authentication Method
```

Representable without redesign:

```text
phone + password
phone + OTP
email + password
email + OTP
username + password
external provider
```

Enrollment records which methods are enabled for an Identity. Policy may require or forbid combinations later; that policy is not locked here.

## F. Password Authentication

Principles:

- passwords are never stored reversibly;
- use modern adaptive password hashing;
- hashing algorithm is replaceable and versioned;
- rehash on successful login when the configured algorithm/parameters change;
- password policy is security/config policy, not UI-only validation;
- breached/common password checks may be added later;
- secrets never enter logs or telemetry;
- reset tokens are short-lived and single-use.

Exact algorithm parameters are later security configuration, not this task.

## G. OTP Authentication

Separate:

```text
OTP Challenge
Delivery Channel
OTP Provider
Verification Attempt
Rate-Limit / Abuse Policy
```

Channels: SMS, Email, future channels. Identity must not couple to one SMS vendor. Providers sit behind internal adapters (same ACL pattern as T004).

OTP requirements: expiration; one-time consumption; bounded retries; replay prevention; rate limiting; resend policy; correlation to intended identifier/action; tenant/store branding context where relevant; no OTP value in logs.

OTP-as-primary-login is a method. It is not automatically MFA.

## H. MFA / 2FA

Optional additional independent factor after primary authentication.

Allowed factor directions:

```text
TOTP authenticator
OTP via verified channel
Future WebAuthn / passkey
Recovery codes
```

Do not equate OTP login with MFA.

Conceptual:

```text
Authentication Assurance Level
```

Sensitive operations may later require step-up to a higher assurance for a bounded duration or action. Exact AAL scale is `NEEDS_LATER_P00_DETAIL`.

## I. Verification

Identifier existence ≠ verified control.

Examples: email verified, phone verified, future high-assurance identifier verified.

Analyze (no schema):

```text
verified-at
verification method
revocation/reverification
change-of-identifier workflow
```

Change-of-identifier: verify the new value, then swap/revoke the old under audit; do not silently alias.

Whether unverified identifiers may log in for a given method is policy (`NEEDS_LATER_P00_DETAIL`); fail closed where uncertainty would leak tenants or accounts.

## J. Session Architecture

Sessions are independent of credentials. Creating a session does not keep the password/OTP in the session. Revoking a session does not delete the Identity.

Requirements:

```text
multiple devices/sessions
session revocation
logout current session
logout all sessions
session rotation
security-sensitive reauthentication
session metadata
```

Safe metadata direction:

```text
SessionId
IdentityId
CreatedAt
LastSeenAt
Authentication Method / Assurance
Tenant/Deployment Context
Device label / user agent summary
RevokedAt
```

Do not store raw sensitive telemetry unnecessarily. Cookie/JWT library choice is out of scope.

## K. Cookie vs Token Strategy

Do not choose JWT because it is popular.

| Candidate | Revocation | XSS | CSRF | Shared hosting | Mobile/API | Keycloak later |
| --- | --- | --- | --- | --- | --- | --- |
| Server-managed session cookie (opaque) | Strong (server record) | Cookie flags help; still XSS-sensitive | Needs CSRF strategy | Fits reverse-proxy apps | Awkward as-is | Can sit beside IdP session |
| Opaque session token (non-cookie) | Strong | Bearer theft risk | CSRF lower if not cookie | Needs TLS + storage | Fits APIs | Adapter can mint local session |
| JWT access token | Weak unless denylist/short TTL | Bearer theft | CSRF lower if not cookie | Stateless tempting, revocation hard | Common | Maps to IdP tokens |
| Refresh token | Needs rotation/reuse detection | High value if stolen | Same as bearer | Needs store | Common | IdP often owns |
| Hybrid | Mix | Mix | Mix | Possible | Likely later | Likely later |

**Recommended direction for first-party web UX (storefront + admin in Tooba process):** server-managed **opaque session** delivered as a **secure, HttpOnly, SameSite-aware cookie**, with CSRF protection for cookie-authenticated mutating requests. Prefer this over JWT-as-session so logout/revoke/all-devices is a server fact, not a denylist afterthought.

Redis may later back session store; architecture must work without Redis initially (process/DB-backed store is enough conceptually).

Mobile/API and Keycloak-issued tokens: `NEEDS_LATER_P00_DETAIL` / hybrid later. Final library and cookie attributes:

```text
RECOMMENDED_FOR_ADR
```

No code.

## L. Tenant / Deployment Scope

Must work for Marketplace deployment and Single-Store shared deployment.

Possible identity namespaces:

```text
deployment-wide
tenant-specific
shared-across-tenants
```

Do not invent business policy. Security rule: authentication resolution must **never** cause cross-tenant data leakage.

For Single-Store, tenant/store context is already resolved from **trusted host routing** (platform, T003) before tenant-sensitive authentication policy or data access. Identity must not parse Host. Unknown Host remains fail-closed at the platform edge.

**Direction without locking product rules:**

- Treat the authenticated lookup as scoped to the **already resolved** deployment + tenant/store identity namespace.
- Marketplace: one marketplace identity namespace is the natural candidate (one marketplace DB).
- Single-Store: default **fail-closed** to the current store’s namespace so a login on store A cannot resolve identities or sessions of store B.
- Do not equate `same email` with `same Party across all stores` unless later policy explicitly decides that.

Exact “can one human reuse one Identity across many Single-Store tenants” is:

```text
NEEDS_LATER_P00_DETAIL
```

## M. Party Link

Boundary:

```text
Identity = who can log in
Party = who they are in the business
```

Relationship:

```text
IdentityId -> PartyId
```

through an explicit contract/reference, not a shared table or ORM navigation.

Future B2B:

```text
one human identity
belongs to / acts for
multiple organizations
```

Authorization to act for an organization belongs to SpiceDB / Party membership, not Identity credentials. Identity must not store “current company” as a hidden role matrix.

## N. External Identity Providers

Ready for Keycloak, OIDC, OAuth 2.x, Enterprise IdP, social login if later required.

Anti-corruption: domains consume an internal IdP contract; vendor SDKs stay in adapters.

Do **not** make Keycloak the canonical owner of all Tooba business identity unless explicitly decided later.

Two future operating modes:

```text
Tooba-managed authentication
External-IdP-managed authentication
```

Continuity: Tooba still keeps an internal Identity (and Party link). External-IdP-managed mode means credentials/challenges may live at the IdP, while Tooba stores the binding and issues/recognizes a local session if the architecture requires a first-party session for the modular monolith. Switching modes later must not require rewriting Party/Order.

## O. External Identity Binding

Conceptual binding (no schema):

```text
Provider
Issuer
Subject
IdentityId
LinkedAt
Status
```

Do not trust email alone as a stable external identity key.

OIDC-style:

```text
issuer + subject
```

is the stable external identifier. Email may be a hint for linking policy, never the unique key.

## P. Account Recovery

Recovery is independent of normal login. Future policies:

```text
verified email recovery
verified phone recovery
recovery codes
support-assisted high-assurance recovery
```

Principles: prevent account enumeration; rate limit; notify on sensitive changes; invalidate/rotate sessions when appropriate; audit recovery attempts/outcomes. Tokens short-lived and single-use (see F).

## Q. Security Events / Audit

Identity records security-relevant facts, for example:

```text
login success
login failure
OTP requested
OTP failed
password changed
identifier added/removed
MFA enabled/disabled
external identity linked/unlinked
session revoked
recovery completed
```

Distinguish:

```text
security audit event
```

from:

```text
technical application log
```

No passwords, OTPs, tokens, or secrets in either. Technical logs may include request ids and opaque Identity/Session ids. Audit events are identity-owned facts for security review, not a substitute for OpenTelemetry traces.

## R. Abuse Protection

Requirements:

```text
rate limiting
credential stuffing defense
brute-force protection
OTP abuse
identifier enumeration defense
suspicious login detection hooks
CAPTCHA / challenge escalation hooks
temporary lock/throttle
```

No CAPTCHA vendor. Rate limiting may later use Redis; architecture must not depend on Redis existing initially (local/store counters are acceptable conceptually). Enumeration-safe responses: same customer-visible outcome for unknown identifier vs wrong password where feasible (see U).

## S. Privacy / Sensitive Data

- authentication secrets never logged;
- national ID or similar future identifiers are sensitive;
- telemetry must avoid raw identifier values unless explicitly safe/necessary;
- consider hashed/pseudonymous dimensions for security analytics;
- data retention and deletion policies need later detail;
- external provider tokens must be protected.

No extra compliance certifications claimed here.

## T. Authentication Flow Examples

### Password Login

```text
resolve tenant/deployment context
identify identifier type
normalize identifier
resolve identity
verify password
evaluate MFA/step-up policy
create/rotate session
emit security event
```

### OTP Login

```text
resolve context
normalize identifier
issue OTP challenge
deliver via adapter
verify challenge
evaluate assurance policy
create/rotate session
emit security event
```

### External IdP Login

```text
resolve context
redirect/challenge
validate issuer/signature/state/nonce
resolve issuer+subject binding
link/create internal identity according to policy
evaluate authorization separately
create local session if architecture requires
```

### Sensitive Operation Step-Up

```text
existing authenticated session
policy requires stronger assurance
challenge additional factor
upgrade assurance for bounded duration/action
audit
```

## U. Failure Matrix

| Case | Customer-visible direction | Fail closed? | Audit/security event? | Enumeration-safe? |
| --- | --- | --- | --- | --- |
| Unknown Identifier | Generic authentication failure (no “user not found”) | Yes (no identity) | Yes (failure, no identifier raw dump) | Yes |
| Wrong Password | Generic authentication failure | Yes | Yes | Align with unknown-identifier |
| Expired OTP | OTP invalid/expired; offer limited resend | Yes | Yes | N/A after challenge issued |
| Too Many OTP Attempts | Throttle/lock messaging; no extra guesses | Yes | Yes | N/A |
| Unverified Identifier | Block or limited path per later policy; default conservative | Yes if policy unset | Yes | Do not reveal other accounts |
| Disabled Identity | Access denied; no credential hints | Yes | Yes | Avoid confirming which identifier maps |
| Suspended Tenant | Unavailable; no cross-tenant fallback | Yes | Yes | N/A |
| MFA Required | Prompt additional factor; do not issue full session yet | Yes until satisfied | Yes | N/A |
| External IdP Unavailable | Fail closed; no local password bypass unless that method is enrolled | Yes | Yes | N/A |
| External Binding Conflict | Do not auto-merge identities | Yes | Yes | Do not leak the other Identity |
| Session Revoked | Re-authenticate | Yes | Yes | N/A |
| Recovery Token Expired | Restart recovery; no account existence extra detail | Yes | Yes | Yes |

Exact copy is not specified.

## V. Relationship with SpiceDB

```text
Identity proves who is authenticated.
SpiceDB answers what that subject may do.
```

Flow:

```text
Authentication
→ authenticated subject/IdentityId
→ Party/organization relationships
→ SpiceDB authorization decision
```

Identity must not contain a fixed role matrix that duplicates SpiceDB. Bootstrap/system roles may exist later only by explicit design. Decision architecture: `docs/architecture/05-spicedb-authorization.md`.

## W. Relationship with B2B

Preserve:

```text
Identity: human login principal
Party: human/business party
Organization: business entity
Membership/Delegation: relationship
SpiceDB: authorization graph
```

One person may act for multiple organizations with different permissions. Do not model one account = one company.

## X. Decision Summary

### RECOMMENDED_FOR_ADR

1. Identity separate from Party.
2. Authentication separate from Authorization.
3. Extensible typed Login Identifier model.
4. Identifier normalization by type (`ILoginIdentifierTypeHandler` or equivalent).
5. Authentication Method separate from Identifier.
6. Password + OTP + MFA extensibility.
7. External IdP adapter/binding model.
8. `issuer + subject` for external identity binding.
9. Session lifecycle independent of credentials.
10. Security events separated from technical logs.
11. Tenant/deployment context resolved before tenant-sensitive auth.
12. No fixed role model inside Identity duplicating SpiceDB.
13. First-party web: opaque server-managed session cookie (not JWT-as-session); final implementation `RECOMMENDED_FOR_ADR`.

### NEEDS_LATER_P00_DETAIL

- Exact uniqueness policy per identifier type and edition
- Whether one Identity may span multiple Single-Store tenants
- Login allowed on unverified identifiers
- Authentication Assurance Level scale and step-up catalog
- Password hashing parameters
- OTP/SMS/email providers
- Session store (DB vs later Redis)
- Mobile/API token profile and Keycloak operating mode choice
- Recovery UX policy and support-assisted process
- Retention/deletion of identity data
- Suspicious-login scoring

### DEFERRED

- Implementation, schemas, migrations
- Login UI
- Keycloak/OIDC configuration
- SpiceDB schema
- Party implementation
- CAPTCHA vendor
- Final ADR document
- Shopeiva / template login screens as requirements
