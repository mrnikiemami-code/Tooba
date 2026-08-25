# 12 — Security Field Handling Proof

Task: `TB-P05-T015`

| Field | Handling | Stored in profile module? | Notes |
| --- | --- | --- | --- |
| email | read-only display from Identity lookup | no | mutation requires OTP/verification flows |
| mobile | read-only display from Identity lookup (+ order fallback) | no | same |
| password | not on profile page | no | existing `/v1/auth/password-change` only |
| nationalCode | read-only disabled | no | deferred KYC |
| displayName/first/last/bio/birthDate | server-validated write | yes | descriptive data only |

Static proofs:

- `CustomerProfile` entity has no Email/Mobile/Password properties
- `CustomerProfileWriteRequest` excludes identity fields
- `customer-profile-api` write payload excludes email/mobile

Architectural concern (reported, not silently implemented):

- Verified email/mobile change UX not built in this task; Identity OTP flows exist but are not wired to profile page actions.
