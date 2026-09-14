# Anti-pattern scan

CLEAN.

- Admin UI uses Persian labels, not raw MenuId/enum keys
- External URLs http/https only
- No HTML/CSS/JS in model
- Depth 3 + cycle guard
- One composer/projection
- Header fallback preserved when unset
- No free-form mega-menu builder
- No per-item public API
- No polling
- Demo seed uses MenuKey, not hard-coded StoreId
