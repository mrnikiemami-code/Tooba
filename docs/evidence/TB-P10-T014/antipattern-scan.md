# Anti-pattern scan — TB-P10-T014

CLEAN.

- No raw palette/theme/skin keys in Admin cards (Persian labels + descriptions)
- No HTML/CSS/JS theme builder
- No raw SQL in FE
- No per-page appearance state
- No duplicate Product Card or Menu logic
- Draft public 404
- Unset Home keeps canonical Home; unset Header keeps Catalog fallback
- No polling added
- Seed is Development-only, idempotent, store-alpha via CommerceContext (not production path)
- No tests added for count
