# r2-fail-closed-policy

ContentAdminAccess capability check is fail-closed:
- Allow → proceed
- Deny → 403 `admin.authorization.denied`
- Unavailable / indeterminate → 503 `admin.authorization.unavailable`

tenant#view alone never authorizes Content mutations.
No Support/Wallet refactor; Support fail-open remains a platform Architectural Concern.
