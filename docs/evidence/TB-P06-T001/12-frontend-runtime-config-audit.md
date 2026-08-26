# 12 — Frontend runtime config audit (TB-P06-T001)

| Item | Status |
|---|---|
| API base URL | `TOOBA_HOST_ORIGIN` (server-only); default `http://127.0.0.1:5088` |
| Browser API calls | Relative `/v1/*` via Next.js rewrite |
| RSC/server fetches | Absolute via `storefrontHostOrigin()` |
| `NEXT_PUBLIC_*` | **None** — no secrets in client bundle |
| `.env.example` | Added documenting `TOOBA_HOST_ORIGIN` |
| Error boundaries | Bootstrap `error.tsx` / `global-error.tsx` (digest only) |
| API failure UX | Module-level honest empty/denied states; no UI redesign |

Shopeiva visuals unchanged.
