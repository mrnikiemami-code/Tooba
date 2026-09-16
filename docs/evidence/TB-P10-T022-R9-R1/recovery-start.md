# Recovery Start — TB-P10-T022-R9-R1

- Branch: `main`
- HEAD: `28b000ebd571e12bc76d25042daa2fa9fa8463bb`
- origin/main: `28b000ebd571e12bc76d25042daa2fa9fa8463bb` (match)
- git status: clean tracked tree; unrelated local `?? .tmp-*` / evidence scratch preserved (not staged)
- FE :3000 — Next.js 15.5.23 Ready
- Host :5088 — health `{"status":"ok"}`
- Routes: `/landing/{slug}`, `/`, metadata via `generateMetadata`, Host `GET /v1/storefront/pages/{slug}`, `home-selection`, `home`, `products`
- Caching before repair: FE `cache: "no-store"` on page/home/selection; Host memory cache Store-scoped for page/home
- Unrelated local changes: preserved (not touched)

Expected previous HEAD matched. No RECOVERY_CONFLICT.
