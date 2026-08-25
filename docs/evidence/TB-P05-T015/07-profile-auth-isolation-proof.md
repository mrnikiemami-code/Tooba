# 07 — Profile Auth Isolation Proof

Task: `TB-P05-T015`

## Server authority

- Actor resolved from `CurrentAuthenticatedSession.UserId`, then Dev header, then guest actor in Dev/Testing only.
- `CustomerProfileWriteRequest` has **no** `OwnerUserId`, `Email`, `Mobile`, or `Password`.
- `PUT /v1/customer/profile` updates only the resolved actor row keyed by `OwnerUserId`.

## Isolation tests

`CustomerProfileFoundationTests`:

- `Actor_can_read_and_update_own_profile` (SkippableFact — requires Testcontainers PostgreSQL)
- `Actor_cannot_update_foreign_profile` — separate owners get separate rows; no cross-owner mutation API
- `Invalid_values_are_rejected` — bounded validation server-side
- `Http_and_write_contracts_have_no_owner_or_identity_authority` — static contract proof

## Missing actor

- Production without session → `401 customer.session.required` (same pattern as AddressBook / CustomerPanel).

## Frontend

- `customerAuthHeaders()` supplies Bearer or Dev actor header only.
- No `userId` in URL/body for profile save.
