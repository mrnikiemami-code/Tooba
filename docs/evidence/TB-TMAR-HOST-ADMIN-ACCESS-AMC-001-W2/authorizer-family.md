# authorizer-family — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2

## In-scope Host authorizers

| File | Change |
| --- | --- |
| HostOrderAdminAuthorizer.cs | SemanticException: `order.authorization.unavailable` (503) / `order.operation.denied` (403); no hard-coded titles |
| HostSupportAdminAuthorizer.cs | SemanticException: `support.authorization.unavailable` (503) / `admin.authorization.denied` (403 Foundation) |
| HostWalletAdminAuthorizer.cs | SemanticException: `wallet.authorization.unavailable` (503) / `admin.authorization.denied` (403 Foundation) |

## Pass-through (untouched behaviorally)

HostLocalizationAdminAuthorizer, HostOperatorProfileAdminAuthorizer, HostPaymentAdminAuthorizer, HostReturnAdminAuthorizer, HostSettlementAdminAuthorizer, HostUserPreferenceAdminAuthorizer, HostOrderAdminEffectiveAccessReader — no hard-coded runtime throw titles.

## After W2

- Active Host authorizer adapters: 10
- Dead Host authorizers: 0
- Authorizer-family hard-coded runtime user-facing titles: ZERO
- Core W1 AdminPanelAccess / HostAdminPanelAccess: PRESERVED
