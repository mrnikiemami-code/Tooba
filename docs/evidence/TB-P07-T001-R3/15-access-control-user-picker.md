# 15 — Access Control user picker

- GUID text inputs removed from assign/effective flows.
- `UserPicker` calls `GET /v1/admin/access-control/users?q=` via `AccApi.searchUsers`.
- Host `AdminSearchUsersAsync` enriches `AccessUserHitDto` with `DisplayName`, `Email`, `Mobile`
  from `IIdentityContactLookup` + `IOperatorProfileDirectory` (composition in Host only).
- Email/phone query can resolve users not yet assigned via `IIdentityAuthenticationService`.

Proof: Users tab + Role Members add-member use searchable picker (`data-testid=user-picker`).
